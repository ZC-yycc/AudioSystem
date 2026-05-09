using UnityEngine;
using UnityEngine.Audio;

namespace AudioSystem
{
    /// <summary>
    /// 音频配置 ScriptableObject，存储各分组的音量、音调随机范围以及 AudioMixer 路由
    /// 跨场景共享，通过 Resources 或引用加载
    /// </summary>
    [CreateAssetMenu(fileName = "AudioSettings", menuName = "AudioSystem/AudioSettings")]
    public class AudioSettingsSO : ScriptableObject
    {
        #region Code-Driven Volume (降级方案)

        [Header("Code-Driven 音量（无 AudioMixer 时使用）")]
        [SerializeField, Range(0f, 1f)]
        private float                           master_volume_ = 1f;

        [SerializeField, Range(0f, 1f)]
        private float                           bgm_volume_ = 1f;

        [SerializeField, Range(0f, 1f)]
        private float                           battle_volume_ = 1f;

        [SerializeField, Range(0f, 1f)]
        private float                           ui_volume_ = 1f;

        [SerializeField, Range(0f, 1f)]
        private float                           environment_volume_ = 1f;

        [SerializeField, Range(0f, 1f)]
        private float                           dialogue_volume_ = 1f;

        #endregion

        #region AudioMixer

        [Header("AudioMixer（推荐）")]
        [SerializeField, Tooltip("AudioMixer 资源引用")]
        private AudioMixer                       mixer_;

        [SerializeField, Tooltip("主混音组")]
        private AudioMixerGroup                  master_group_;

        [SerializeField, Tooltip("BGM 混音组")]
        private AudioMixerGroup                  bgm_group_;

        [SerializeField, Tooltip("战斗音效混合组")]
        private AudioMixerGroup                  battle_group_;

        [SerializeField, Tooltip("UI 音效混合组")]
        private AudioMixerGroup                  ui_group_;

        [SerializeField, Tooltip("环境音效混合组")]
        private AudioMixerGroup                  environment_group_;

        [SerializeField, Tooltip("对话音效混合组")]
        private AudioMixerGroup                  dialogue_group_;

        /// <summary>
        /// AudioMixer 中 exposed parameter 的统一命名约定
        /// 自动创建时使用这些名称
        /// </summary>
        public const string EXPOSED_MASTER       = "MasterVolume";
        public const string EXPOSED_BGM          = "BgmVolume";
        public const string EXPOSED_BATTLE       = "BattleVolume";
        public const string EXPOSED_UI           = "UIVolume";
        public const string EXPOSED_ENVIRONMENT  = "EnvironmentVolume";
        public const string EXPOSED_DIALOGUE     = "DialogueVolume";
        public const string EXPOSED_PITCH        = "Pitch";

        #endregion

        [Header("音调随机范围")]
        [SerializeField, Min(0f)]
        private float                           pitch_min_ = 0.85f;

        [SerializeField, Min(0f)]
        private float                           pitch_max_ = 1.15f;

        #region Public Properties

        public bool HasMixer => mixer_ != null;

        public AudioMixer Mixer
        {
            get => mixer_;
            set => mixer_ = value;
        }

        public AudioMixerGroup MasterGroup
        {
            get => master_group_;
            set => master_group_ = value;
        }

        public AudioMixerGroup BgmGroup
        {
            get => bgm_group_;
            set => bgm_group_ = value;
        }

        public AudioMixerGroup BattleGroup
        {
            get => battle_group_;
            set => battle_group_ = value;
        }

        public AudioMixerGroup UiGroup
        {
            get => ui_group_;
            set => ui_group_ = value;
        }

        public AudioMixerGroup EnvironmentGroup
        {
            get => environment_group_;
            set => environment_group_ = value;
        }

        public AudioMixerGroup DialogueGroup
        {
            get => dialogue_group_;
            set => dialogue_group_ = value;
        }

        public float MasterVolume
        {
            get => master_volume_;
            set => master_volume_ = Mathf.Clamp01(value);
        }

        public float BgmVolume
        {
            get => bgm_volume_;
            set => bgm_volume_ = Mathf.Clamp01(value);
        }

        public float BattleVolume
        {
            get => battle_volume_;
            set => battle_volume_ = Mathf.Clamp01(value);
        }

        public float UIVolume
        {
            get => ui_volume_;
            set => ui_volume_ = Mathf.Clamp01(value);
        }

        public float EnvironmentVolume
        {
            get => environment_volume_;
            set => environment_volume_ = Mathf.Clamp01(value);
        }

        public float DialogueVolume
        {
            get => dialogue_volume_;
            set => dialogue_volume_ = Mathf.Clamp01(value);
        }

        public float PitchMin
        {
            get => pitch_min_;
            set => pitch_min_ = Mathf.Max(0f, value);
        }

        public float PitchMax
        {
            get => pitch_max_;
            set => pitch_max_ = Mathf.Max(0f, value);
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// 根据分组获取混音组
        /// </summary>
        public AudioMixerGroup GetMixerGroup(AudioGroup group)
        {
            if (mixer_ == null) return null;
            return group switch
            {
                AudioGroup.Master      => master_group_,
                AudioGroup.BGM         => bgm_group_,
                AudioGroup.Battle      => battle_group_,
                AudioGroup.UI          => ui_group_,
                AudioGroup.Environment => environment_group_,
                AudioGroup.Dialogue    => dialogue_group_,
                _ => null
            };
        }

        /// <summary>
        /// 根据分组获取 exposed parameter 名称
        /// </summary>
        public static string GetExposedParameterName(AudioGroup group)
        {
            return group switch
            {
                AudioGroup.Master      => EXPOSED_MASTER,
                AudioGroup.BGM         => EXPOSED_BGM,
                AudioGroup.Battle      => EXPOSED_BATTLE,
                AudioGroup.UI          => EXPOSED_UI,
                AudioGroup.Environment => EXPOSED_ENVIRONMENT,
                AudioGroup.Dialogue    => EXPOSED_DIALOGUE,
                _ => EXPOSED_MASTER
            };
        }

        /// <summary>
        /// 根据分组获取对应音量（不含主音量）
        /// </summary>
        public float GetGroupVolume(AudioGroup group)
        {
            return group switch
            {
                AudioGroup.Master      => master_volume_,
                AudioGroup.BGM         => bgm_volume_,
                AudioGroup.Battle      => battle_volume_,
                AudioGroup.UI          => ui_volume_,
                AudioGroup.Environment => environment_volume_,
                AudioGroup.Dialogue    => dialogue_volume_,
                _ => 1f
            };
        }

        /// <summary>
        /// 计算最终音量 = 主音量 * 分组音量（code-driven 降级方案）
        /// </summary>
        public float GetFinalVolume(AudioGroup group)
        {
            if (group == AudioGroup.Master)
                return master_volume_;
            return master_volume_ * GetGroupVolume(group);
        }

        /// <summary>
        /// 随机音调
        /// </summary>
        public float GetRandomPitch()
        {
            return Random.Range(pitch_min_, pitch_max_);
        }

        /// <summary>
        /// 将线性音量 (0~1) 转换为分贝 (-80~0 dB)
        /// 用于 AudioMixer 的 exposed parameter
        /// </summary>
        public static float LinearToDecibel(float linear)
        {
            linear = Mathf.Clamp01(linear);
            if (linear <= 0.0001f)
                return -80f;
            return 20f * Mathf.Log10(linear);
        }

        /// <summary>
        /// 将分贝 (-80~0 dB) 转换为线性音量 (0~1)
        /// </summary>
        public static float DecibelToLinear(float db)
        {
            if (db <= -80f)
                return 0f;
            return Mathf.Pow(10f, db / 20f);
        }

        #endregion

        private void OnValidate()
        {
            if (pitch_min_ > pitch_max_)
                pitch_min_ = pitch_max_;
        }
    }
}