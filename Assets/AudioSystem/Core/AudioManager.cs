using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;

namespace AudioSystem
{
    /// <summary>
    /// 音频播放句柄，用于控制单个播放实例（停止、检查状态等）
    /// </summary>
    public struct AudioHandle
    {
        public AudioSource Source { get; internal set; }
        public EAudioGroup Group { get; internal set; }
        public readonly bool IsValid => Source != null;
        public readonly bool IsPlaying => Source != null && Source.isPlaying;

        /// <summary>
        /// 停止播放（会回收到池中）
        /// </summary>
        public readonly void Stop()
        {
            if (Source != null)
            {
                Source.Stop();
                Source.clip = null;
                Source.loop = false;
            }
        }

        /// <summary>
        /// 暂停
        /// </summary>
        public readonly void Pause()
        {
            if (Source != null)
                Source.Pause();
        }

        /// <summary>
        /// 恢复
        /// </summary>
        public readonly void Resume()
        {
            if (Source != null)
                Source.UnPause();
        }
    }

    /// <summary>
    /// 音频管理器，全局单例，跨场景不销毁
    /// 提供按分组播放、音量控制、音调随机、3D/2D支持
    /// 优先使用 AudioMixer 进行音量管理和音效路由，无 mixer 时退回代码方案
    /// </summary>
    public class AudioManager : MonoBehaviour
    {
        #region Singleton

        private static AudioManager instance_;

        public static AudioManager Instance
        {
            get
            {
                if (instance_ == null)
                {
                    instance_ = FindAnyObjectByType<AudioManager>();
                    if (instance_ == null)
                    {
                        GameObject go = new GameObject("[AudioManager]");
                        instance_ = go.AddComponent<AudioManager>();
                    }
                }
                return instance_;
            }
        }

        #endregion

        [Header("配置")]
        [SerializeField] private AudioSettingsSO                    settings_;
        [SerializeField] private AudioClipDataSO                    clip_data_;
        [SerializeField] private int                                pool_initial_capacity_ = 10;
        [SerializeField] private int                                pool_max_capacity_ = 30;

        private AudioPool                                           pool_;
        private readonly Dictionary<EAudioGroup, List<AudioSource>>  group_sources_ = new();

        public AudioSettingsSO Settings => settings_;
        public AudioClipDataSO ClipData => clip_data_;

        #region Unity Lifecycle

        private void Awake()
        {
            if (instance_ != null && instance_ != this)
            {
                Destroy(gameObject);
                return;
            }
            instance_ = this;
            DontDestroyOnLoad(gameObject);
            Initialize();
        }

        private void OnDestroy()
        {
            if (instance_ == this)
            {
                pool_?.Destroy();
                instance_ = null;
            }
        }

        private void Initialize()
        {
            pool_ = new AudioPool(transform, pool_initial_capacity_, pool_max_capacity_);

            // 如果没有手动指定配置，尝试从 Resources 加载
            if (settings_ == null)
            {
                settings_ = Resources.Load<AudioSettingsSO>("AudioSettings");
                if (settings_ == null)
                {
                    Debug.LogWarning("[AudioManager] 未找到 AudioSettings，使用默认值。请在 Resources 下创建 AudioSettings 或手动拖入。");
                }
            }

            // 初始化 AudioMixer 的音量 snapshot
            if (settings_ != null && settings_.HasMixer)
            {
                ApplyAllMixerVolumes();
            }
        }

        #endregion

        #region Public API - Play

        /// <summary>
        /// 通过 AudioClipDataSO 中配置的音频 ID 播放音频（2D）
        /// </summary>
        public AudioHandle Play(string audio_id)
        {
            return Play(audio_id, Vector3.zero);
        }

        /// <summary>
        /// 通过 AudioClipDataSO 中配置的音频 ID 在指定位置播放 3D 音频
        /// </summary>
        public AudioHandle Play(string audio_id, Vector3 position)
        {
            if (clip_data_ == null)
            {
                Debug.LogWarning($"[AudioManager] AudioClipDataSO 未赋值，无法通过 ID 播放: {audio_id}");
                return default;
            }

            if (!clip_data_.TryGetEntry(audio_id, out AudioClipEntry entry))
            {
                Debug.LogWarning($"[AudioManager] 未找到音频 ID: {audio_id}");
                return default;
            }

            if (entry.clip == null)
            {
                Debug.LogWarning($"[AudioManager] 音频 ID '{audio_id}' 的 Clip 未赋值");
                return default;
            }

            float base_volume = GetEffectiveVolume(entry.group);
            float final_volume = base_volume * entry.volume;

            return Play(entry.clip, entry.group, position, final_volume, entry.pitch, false);
        }

        /// <summary>
        /// 播放一个音频（2D），音调范围从 AudioClipDataSO 获取
        /// </summary>
        public AudioHandle Play(AudioClip clip, EAudioGroup group)
        {
            return Play(clip, group, Vector3.zero, GetEffectiveVolume(group), new Vector2(1f, 1f), false);
        }

        /// <summary>
        /// 播放一个音频（2D），指定音调随机范围
        /// </summary>
        public AudioHandle Play(AudioClip clip, EAudioGroup group, Vector2 pitch_range)
        {
            return Play(clip, group, Vector3.zero, GetEffectiveVolume(group), pitch_range, false);
        }

        /// <summary>
        /// 播放一个音频，指定音量倍率（叠加在分组音量之上）
        /// </summary>
        public AudioHandle Play(AudioClip clip, EAudioGroup group, float volume_multiplier)
        {
            float base_volume = GetEffectiveVolume(group);
            return Play(clip, group, Vector3.zero, base_volume * volume_multiplier, new Vector2(1f, 1f), false);
        }

        /// <summary>
        /// 播放一个音频，指定音量倍率和音调随机范围
        /// </summary>
        public AudioHandle Play(AudioClip clip, EAudioGroup group, float volume_multiplier, Vector2 pitch_range)
        {
            float base_volume = GetEffectiveVolume(group);
            return Play(clip, group, Vector3.zero, base_volume * volume_multiplier, pitch_range, false);
        }

        /// <summary>
        /// 播放3D音频，在世界坐标位置
        /// </summary>
        public AudioHandle PlayAtPosition(AudioClip clip, EAudioGroup group, Vector3 position, float volume_multiplier = 1f)
        {
            float base_volume = GetEffectiveVolume(group);
            return Play(clip, group, position, base_volume * volume_multiplier, new Vector2(1f, 1f), false);
        }

        /// <summary>
        /// 播放3D音频，在世界坐标位置，指定音调随机范围
        /// </summary>
        public AudioHandle PlayAtPosition(AudioClip clip, EAudioGroup group, Vector3 position, Vector2 pitch_range, float volume_multiplier = 1f)
        {
            float base_volume = GetEffectiveVolume(group);
            return Play(clip, group, position, base_volume * volume_multiplier, pitch_range, false);
        }

        /// <summary>
        /// 播放循环音频（BGM等）
        /// </summary>
        public AudioHandle PlayLoop(AudioClip clip, EAudioGroup group, float volume_multiplier = 1f)
        {
            float base_volume = GetEffectiveVolume(group);
            return Play(clip, group, Vector3.zero, base_volume * volume_multiplier, new Vector2(1f, 1f), true);
        }

        /// <summary>
        /// 播放循环音频（BGM等），指定音调随机范围
        /// </summary>
        public AudioHandle PlayLoop(AudioClip clip, EAudioGroup group, Vector2 pitch_range, float volume_multiplier = 1f)
        {
            float base_volume = GetEffectiveVolume(group);
            return Play(clip, group, Vector3.zero, base_volume * volume_multiplier, pitch_range, true);
        }

        /// <summary>
        /// 播放循环音频（3D）
        /// </summary>
        public AudioHandle PlayLoopAtPosition(AudioClip clip, EAudioGroup group, Vector3 position, float volume_multiplier = 1f)
        {
            float base_volume = GetEffectiveVolume(group);
            return Play(clip, group, position, base_volume * volume_multiplier, new Vector2(1f, 1f), true);
        }

        /// <summary>
        /// 播放循环音频（3D），指定音调随机范围
        /// </summary>
        public AudioHandle PlayLoopAtPosition(AudioClip clip, EAudioGroup group, Vector3 position, Vector2 pitch_range, float volume_multiplier = 1f)
        {
            float base_volume = GetEffectiveVolume(group);
            return Play(clip, group, position, base_volume * volume_multiplier, pitch_range, true);
        }

        /// <summary>
        /// 完整的播放控制（高级API），接受 pitch 随机范围
        /// </summary>
        public AudioHandle Play(AudioClip clip, EAudioGroup group, Vector3 position, float volume, Vector2 pitch_range, bool loop)
        {
            float pitch = GetPitchFromRange(pitch_range);
            return PlayInternal(clip, group, position, volume, pitch, loop);
        }

        /// <summary>
        /// 完整的播放控制（高级API），接受固定 pitch
        /// </summary>
        public AudioHandle Play(AudioClip clip, EAudioGroup group, Vector3 position, float volume, float pitch, bool loop)
        {
            return PlayInternal(clip, group, position, volume, pitch, loop);
        }

        private AudioHandle PlayInternal(AudioClip clip, EAudioGroup group, Vector3 position, float volume, float pitch, bool loop)
        {
            if (clip == null)
            {
                Debug.LogWarning("[AudioManager] 尝试播放空的 AudioClip");
                return default;
            }

            AudioSource source = pool_.Get();
            Transform source_transform = source.transform;

            if (position != Vector3.zero)
            {
                source_transform.position = position;
                source.spatialBlend = 1f; // 3D
            }
            else
            {
                source_transform.localPosition = Vector3.zero;
                source.spatialBlend = 0f; // 2D
            }

            source.clip = clip;
            source.loop = loop;
            source.Play();

            // AudioMixer 路由优先
            if (settings_ != null && settings_.HasMixer)
            {
                AudioMixerGroup mixer_group = settings_.GetMixerGroup(group);
                if (mixer_group != null)
                {
                    source.outputAudioMixerGroup = mixer_group;
                    // volume 由 mixer 控制，pitch 仍由 AudioSource 控制
                    source.volume = volume;
                    source.pitch = pitch;
                }
                else
                {
                    source.volume = volume;
                    source.pitch = pitch;
                }
            }
            else
            {
                source.volume = volume;
                source.pitch = pitch;
            }

            // 记录分组
            RegisterGroupSource(group, source);

            return new AudioHandle { Source = source, Group = group };
        }

        #endregion

        #region Public API - Volume Control

        /// <summary>
        /// 运行时设置音量并保存到 Settings
        /// 优先通过 AudioMixer exposed parameter 设置，无 mixer 时退回代码方案
        /// </summary>
        public void SetVolume(EAudioGroup group, float volume)
        {
            volume = Mathf.Clamp01(volume);
            if (settings_ == null) return;

            switch (group)
            {
                case EAudioGroup.Master:      settings_.MasterVolume = volume; break;
                case EAudioGroup.BGM:         settings_.BgmVolume = volume; break;
                case EAudioGroup.Battle:      settings_.BattleVolume = volume; break;
                case EAudioGroup.UI:          settings_.UIVolume = volume; break;
                case EAudioGroup.Environment: settings_.EnvironmentVolume = volume; break;
                case EAudioGroup.Dialogue:    settings_.DialogueVolume = volume; break;
            }

            ApplyVolume(group);
        }

        /// <summary>
        /// 获取当前有效的最终音量
        /// 当使用 AudioMixer 时返回 code-driven 的计算值（仅用于显示），
        /// 实际音量由 mixer 控制
        /// </summary>
        public float GetVolume(EAudioGroup group)
        {
            if (settings_ == null) return 1f;

            // 如果有 mixer，尝试读取 exposed parameter 的实际值
            if (settings_.HasMixer)
            {
                float db;
                if (settings_.Mixer.GetFloat(AudioSettingsSO.GetExposedParameterName(group), out db))
                {
                    return AudioSettingsSO.DecibelToLinear(db);
                }
            }

            return settings_.GetFinalVolume(group);
        }

        /// <summary>
        /// 获取分组原始音量（不含主音量）
        /// </summary>
        public float GetGroupVolume(EAudioGroup group)
        {
            return settings_?.GetGroupVolume(group) ?? 1f;
        }

        /// <summary>
        /// 将所有分组音量重置为1
        /// </summary>
        public void ResetAllVolumes()
        {
            if (settings_ == null) return;
            settings_.MasterVolume = 1f;
            settings_.BgmVolume = 1f;
            settings_.BattleVolume = 1f;
            settings_.UIVolume = 1f;
            settings_.EnvironmentVolume = 1f;
            settings_.DialogueVolume = 1f;
            ApplyAllVolumes();
        }

        /// <summary>
        /// 手动刷新所有音源的音量
        /// 优先通过 AudioMixer 刷新，无 mixer 时遍历 AudioSource
        /// </summary>
        public void ApplyAllVolumes()
        {
            if (settings_ == null) return;

            if (settings_.HasMixer)
            {
                ApplyAllMixerVolumes();
            }
            else
            {
                foreach (var kvp in group_sources_)
                {
                    ApplyVolumeToGroup(kvp.Key);
                }
            }
        }

        #endregion

        #region Public API - Stop

        /// <summary>
        /// 停止指定分组的所有音效
        /// </summary>
        public void StopGroup(EAudioGroup group)
        {
            if (group_sources_.TryGetValue(group, out var sources))
            {
                for (int i = sources.Count - 1; i >= 0; i--)
                {
                    if (sources[i] != null)
                    {
                        sources[i].Stop();
                        sources[i].clip = null;
                        sources[i].loop = false;
                    }
                }
                group_sources_.Remove(group);
            }
        }

        /// <summary>
        /// 停止所有音效
        /// </summary>
        public void StopAll()
        {
            pool_.StopAll();
            group_sources_.Clear();
        }

        #endregion

        #region 3D Audio Support

        /// <summary>
        /// 让一个跟随目标移动的3D音效跟随Transform
        /// </summary>
        public AudioHandle PlayAttached(AudioClip clip, EAudioGroup group, Transform follow_target, float volume_multiplier = 1f)
        {
            AudioHandle handle = PlayAtPosition(clip, group, follow_target.position, volume_multiplier);
            if (handle.IsValid)
            {
                AudioFollower follower = handle.Source.gameObject.AddComponent<AudioFollower>();
                follower.target_ = follow_target;
                follower.handle_ = handle;
            }
            return handle;
        }

        #endregion

        #region Internal

        /// <summary>
        /// 从 Vector2 范围中获取随机或固定音调值
        /// </summary>
        private static float GetPitchFromRange(Vector2 pitch_range)
        {
            if (Mathf.Approximately(pitch_range.x, pitch_range.y))
                return pitch_range.x;
            return Random.Range(pitch_range.x, pitch_range.y);
        }

        /// <summary>
        /// 获取有效基础音量（不含 volume_multiplier）
        /// 使用 mixer 时，AudioSource.volume 保持为1，音量完全由 mixer 控制
        /// 不使用 mixer 时，使用 code-driven 计算值
        /// </summary>
        private float GetEffectiveVolume(EAudioGroup group)
        {
            if (settings_ == null) return 1f;

            if (settings_.HasMixer)
            {
                // mixer 控制音量，AudioSource 层面保持 1.0（或由用户 multiplier 调整）
                return 1f;
            }

            return settings_.GetFinalVolume(group);
        }

        private void RegisterGroupSource(EAudioGroup group, AudioSource source)
        {
            if (!group_sources_.ContainsKey(group))
                group_sources_[group] = new List<AudioSource>();
            group_sources_[group].Add(source);
        }

        /// <summary>
        /// 应用单个分组的音量（code-driven 降级方案：遍历 AudioSource）
        /// </summary>
        private void ApplyVolumeToGroup(EAudioGroup group)
        {
            if (settings_ == null) return;
            float final_volume = settings_.GetFinalVolume(group);

            if (group_sources_.TryGetValue(group, out var sources))
            {
                for (int i = sources.Count - 1; i >= 0; i--)
                {
                    if (sources[i] == null)
                    {
                        sources.RemoveAt(i);
                        continue;
                    }
                    sources[i].volume = final_volume;
                }
            }
        }

        /// <summary>
        /// 通过 AudioMixer exposed parameter 应用单个分组的音量
        /// </summary>
        private void ApplyMixerVolume(EAudioGroup group)
        {
            if (settings_ == null || !settings_.HasMixer) return;

            string param = AudioSettingsSO.GetExposedParameterName(group);
            float db;

            if (group == EAudioGroup.Master)
            {
                db = AudioSettingsSO.LinearToDecibel(settings_.MasterVolume);
            }
            else
            {
                float final_volume = settings_.GetFinalVolume(group);
                db = AudioSettingsSO.LinearToDecibel(final_volume);
            }

            settings_.Mixer.SetFloat(param, db);
        }

        /// <summary>
        /// 通过 AudioMixer exposed parameter 应用所有分组的音量
        /// </summary>
        private void ApplyAllMixerVolumes()
        {
            ApplyMixerVolume(EAudioGroup.Master);
            ApplyMixerVolume(EAudioGroup.BGM);
            ApplyMixerVolume(EAudioGroup.Battle);
            ApplyMixerVolume(EAudioGroup.UI);
            ApplyMixerVolume(EAudioGroup.Environment);
            ApplyMixerVolume(EAudioGroup.Dialogue);
        }

        /// <summary>
        /// 应用音量到指定分组（自动选择 mixer 或 code-driven 方案）
        /// </summary>
        private void ApplyVolume(EAudioGroup group)
        {
            if (settings_ == null) return;

            if (settings_.HasMixer)
            {
                ApplyMixerVolume(group);
            }
            else
            {
                ApplyVolumeToGroup(group);
            }
        }

        #endregion
    }

    /// <summary>
    /// 辅助组件：使 AudioSource 跟随目标 Transform 移动
    /// </summary>
    internal class AudioFollower : MonoBehaviour
    {
        public Transform target_;
        public AudioHandle handle_;

        private void LateUpdate()
        {
            if (target_ == null)
            {
                if (handle_.IsValid && !handle_.IsPlaying)
                {
                    Destroy(this);
                }
                return;
            }
            transform.position = target_.position;
        }
    }
}