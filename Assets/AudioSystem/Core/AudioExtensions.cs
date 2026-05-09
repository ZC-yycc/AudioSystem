using UnityEngine;

namespace AudioSystem
{
    /// <summary>
    /// MonoBehaviour 便捷扩展方法，简化常见音频播放调用
    /// </summary>
    public static class AudioExtensions
    {
        /// <summary>
        /// 在 AudioManager 上播放音效
        /// </summary>
        public static AudioHandle AudioPlay(this MonoBehaviour self, AudioClip clip, AudioGroup group)
        {
            return AudioManager.Instance.Play(clip, group);
        }

        /// <summary>
        /// 在 AudioManager 上播放音效，指定音量倍率
        /// </summary>
        public static AudioHandle AudioPlay(this MonoBehaviour self, AudioClip clip, AudioGroup group, float volume_multiplier)
        {
            return AudioManager.Instance.Play(clip, group, volume_multiplier);
        }

        /// <summary>
        /// 在指定世界位置播放3D音效
        /// </summary>
        public static AudioHandle AudioPlayAtPosition(this MonoBehaviour self, AudioClip clip, AudioGroup group, Vector3 position, float volume_multiplier = 1f)
        {
            return AudioManager.Instance.PlayAtPosition(clip, group, position, volume_multiplier);
        }

        /// <summary>
        /// 播放循环音效（常用于BGM）
        /// </summary>
        public static AudioHandle AudioPlayLoop(this MonoBehaviour self, AudioClip clip, AudioGroup group, float volume_multiplier = 1f)
        {
            return AudioManager.Instance.PlayLoop(clip, group, volume_multiplier);
        }

        /// <summary>
        /// 播放跟随Transform的3D音效
        /// </summary>
        public static AudioHandle AudioPlayAttached(this MonoBehaviour self, AudioClip clip, AudioGroup group, Transform target, float volume_multiplier = 1f)
        {
            return AudioManager.Instance.PlayAttached(clip, group, target, volume_multiplier);
        }
    }

    /// <summary>
    /// AudioManager 扩展方法（静态方法，无需 MonoBehaviour 上下文）
    /// </summary>
    public static class AudioManagerExtensions
    {
        /// <summary>
        /// 快速播放音效
        /// </summary>
        public static AudioHandle PlaySFX(this AudioManager manager, AudioClip clip, AudioGroup group)
        {
            return manager.Play(clip, group);
        }

        /// <summary>
        /// 获取分组音量
        /// </summary>
        public static float Volume(this AudioManager manager, AudioGroup group)
        {
            return manager.GetVolume(group);
        }
    }
}