using UnityEngine;

namespace AudioSystem
{
    /// <summary>
    /// 跨场景音量持久化，使用 PlayerPrefs 存储
    /// 在场景加载时自动恢复音量设置
    /// </summary>
    public static class AudioPersistentSettings
    {
        private const string KEY_MASTER      = "Audio_Master";
        private const string KEY_BGM         = "Audio_BGM";
        private const string KEY_BATTLE      = "Audio_Battle";
        private const string KEY_UI          = "Audio_UI";
        private const string KEY_ENVIRONMENT = "Audio_Environment";
        private const string KEY_DIALOGUE    = "Audio_Dialogue";
        private const string KEY_PITCH_MIN   = "Audio_PitchMin";
        private const string KEY_PITCH_MAX   = "Audio_PitchMax";

        private const float DEFAULT_VOLUME = 1f;
        private const float DEFAULT_PITCH_MIN = 0.85f;
        private const float DEFAULT_PITCH_MAX = 1.15f;

        /// <summary>
        /// 将所有音量保存到 PlayerPrefs
        /// </summary>
        public static void Save(AudioSettingsSO settings)
        {
            if (settings == null) return;

            PlayerPrefs.SetFloat(KEY_MASTER,     settings.MasterVolume);
            PlayerPrefs.SetFloat(KEY_BGM,        settings.BgmVolume);
            PlayerPrefs.SetFloat(KEY_BATTLE,     settings.BattleVolume);
            PlayerPrefs.SetFloat(KEY_UI,         settings.UIVolume);
            PlayerPrefs.SetFloat(KEY_ENVIRONMENT, settings.EnvironmentVolume);
            PlayerPrefs.SetFloat(KEY_DIALOGUE,   settings.DialogueVolume);
            PlayerPrefs.SetFloat(KEY_PITCH_MIN,  settings.PitchMin);
            PlayerPrefs.SetFloat(KEY_PITCH_MAX,  settings.PitchMax);

            PlayerPrefs.Save();
        }

        /// <summary>
        /// 从 PlayerPrefs 加载音量到 Settings
        /// </summary>
        public static void Load(AudioSettingsSO settings)
        {
            if (settings == null) return;

            settings.MasterVolume      = PlayerPrefs.GetFloat(KEY_MASTER,     settings.MasterVolume);
            settings.BgmVolume         = PlayerPrefs.GetFloat(KEY_BGM,        settings.BgmVolume);
            settings.BattleVolume      = PlayerPrefs.GetFloat(KEY_BATTLE,     settings.BattleVolume);
            settings.UIVolume          = PlayerPrefs.GetFloat(KEY_UI,         settings.UIVolume);
            settings.EnvironmentVolume = PlayerPrefs.GetFloat(KEY_ENVIRONMENT, settings.EnvironmentVolume);
            settings.DialogueVolume    = PlayerPrefs.GetFloat(KEY_DIALOGUE,   settings.DialogueVolume);
            settings.PitchMin          = PlayerPrefs.GetFloat(KEY_PITCH_MIN,  settings.PitchMin);
            settings.PitchMax          = PlayerPrefs.GetFloat(KEY_PITCH_MAX,  settings.PitchMax);
        }

        /// <summary>
        /// 检查是否有已保存的数据
        /// </summary>
        public static bool HasSavedData()
        {
            return PlayerPrefs.HasKey(KEY_MASTER);
        }

        /// <summary>
        /// 清除所有音频持久化数据
        /// </summary>
        public static void Clear()
        {
            PlayerPrefs.DeleteKey(KEY_MASTER);
            PlayerPrefs.DeleteKey(KEY_BGM);
            PlayerPrefs.DeleteKey(KEY_BATTLE);
            PlayerPrefs.DeleteKey(KEY_UI);
            PlayerPrefs.DeleteKey(KEY_ENVIRONMENT);
            PlayerPrefs.DeleteKey(KEY_DIALOGUE);
            PlayerPrefs.DeleteKey(KEY_PITCH_MIN);
            PlayerPrefs.DeleteKey(KEY_PITCH_MAX);
            PlayerPrefs.Save();
        }
    }
}