using System;
using System.Collections.Generic;
using UnityEngine;

namespace AudioSystem
{
    /// <summary>
    /// 单条音频数据的定义
    /// </summary>
    [Serializable]
    public struct AudioClipEntry
    {
        [Tooltip("唯一标识符，用于代码中引用")]
        public string                               audio_id;

        [Tooltip("音频资源")]
        public AudioClip                            clip;

        [Tooltip("所属音频分组")]
        public EAudioGroup                           group;

        [Tooltip("音量倍率（叠加在分组音量之上）")]
        [Range(0f, 2f)]
        public float                                volume;

        [Tooltip("音调随机范围（x=最小值, y=最大值），相等即为固定值")]
        public Vector2                              pitch;

        /// <summary>
        /// 是否使用固定音调（x 与 y 相等）
        /// </summary>
        public readonly bool UseFixedPitch => Mathf.Approximately(pitch.x, pitch.y);

        /// <summary>
        /// 从音调范围中获取随机音调值
        /// </summary>
        public readonly float GetRandomPitch()
        {
            if (UseFixedPitch)
                return pitch.x;
            return UnityEngine.Random.Range(pitch.x, pitch.y);
        }
    }

    /// <summary>
    /// 音频资源表 ScriptableObject，存储 audio_id 到 AudioClip 的映射关系
    /// 通过 Resources 加载后，AudioManager 可通过 ID 直接播放音频
    /// </summary>
    [CreateAssetMenu(fileName = "AudioClipData", menuName = "AudioSystem/AudioClipData")]
    public class AudioClipDataSO : ScriptableObject
    {
        [SerializeField, Tooltip("音频条目列表")]
        private List<AudioClipEntry>                    entries_ = new();

        private Dictionary<string, AudioClipEntry>      lookup_;

        public IReadOnlyList<AudioClipEntry> Entries => entries_;

        /// <summary>
        /// 构建 lookup 字典（首次访问或数据变更后自动调用）
        /// </summary>
        public void BuildLookup()
        {
            lookup_ = new Dictionary<string, AudioClipEntry>();
            foreach (var entry in entries_)
            {
                if (string.IsNullOrEmpty(entry.audio_id))
                    continue;
                lookup_[entry.audio_id] = entry;
            }
        }

        /// <summary>
        /// 通过 audio_id 查找条目
        /// </summary>
        public bool TryGetEntry(string audio_id, out AudioClipEntry entry)
        {
            if (lookup_ == null)
                BuildLookup();

            return lookup_.TryGetValue(audio_id, out entry);
        }

        /// <summary>
        /// 添加或更新条目
        /// </summary>
        public void SetEntry(AudioClipEntry entry)
        {
            for (int i = 0; i < entries_.Count; i++)
            {
                if (entries_[i].audio_id == entry.audio_id)
                {
                    entries_[i] = entry;
                    BuildLookup();
                    return;
                }
            }
            entries_.Add(entry);
            BuildLookup();
        }

        /// <summary>
        /// 删除条目
        /// </summary>
        public void RemoveEntry(string audio_id)
        {
            entries_.RemoveAll(e => e.audio_id == audio_id);
            BuildLookup();
        }

        private void OnValidate()
        {
            BuildLookup();
        }
    }
}