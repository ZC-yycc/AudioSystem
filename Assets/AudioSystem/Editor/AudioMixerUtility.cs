using System;
using System.IO;
using UnityEngine;
using UnityEngine.Audio;
using UnityEditor;

namespace AudioSystem.Editor
{
    public static class AudioMixerUtility
    {
        // 模板存放路径（隐藏在 Editor 文件夹内）
        private const string TEMPLATE_PATH = "Assets/AudioSystem/Editor/Templates/AudioMixerTemplate.mixer";
        
        /// <summary>
        /// 创建 AudioMixer（从模板复制）
        /// </summary>
        public static AudioMixer CreateAudioMixer(string folder_path)
        {
            if (File.Exists(TEMPLATE_PATH))
            {
                // 验证是否有效
                var existing = AssetDatabase.LoadAssetAtPath<AudioMixer>(TEMPLATE_PATH);
                if (existing == null)
                {
                    AssetDatabase.DeleteAsset(TEMPLATE_PATH);
                    Debug.LogError($"模板文件无效，已删除: {TEMPLATE_PATH}\n请重新创建一个 AudioMixer 模板并保存到该路径");
                    return null;
                }
            }
            
            string target_path = folder_path;
            target_path = AssetDatabase.GenerateUniqueAssetPath(target_path);

            if (!AssetDatabase.CopyAsset(TEMPLATE_PATH, target_path))
            {
                Debug.LogError($"复制模板失败: {TEMPLATE_PATH} -> {target_path}");
                return null;
            }
            
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            AudioMixer mixer = AssetDatabase.LoadAssetAtPath<AudioMixer>(target_path);
            if (mixer != null)
            {
                Selection.activeObject = mixer;
                Debug.Log($"✓ AudioMixer 创建成功: {target_path}");
            }
            else
            {
                Debug.LogError($"加载创建的 AudioMixer 失败: {target_path}");
            }
            
            return mixer;
        }
    }
}