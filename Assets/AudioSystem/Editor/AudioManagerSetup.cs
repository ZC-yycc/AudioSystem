#if UNITY_EDITOR
using System;
using System.Reflection;
using AudioSystem;
using UnityEditor;
using UnityEngine;
using UnityEngine.Audio;

namespace AudioSystem.Editor
{
    /// <summary>
    /// Editor工具：快速在场景中创建 AudioManager、AudioSettingsSO、AudioMixer
    /// </summary>
    public static class AudioManagerSetup
    {
        private const string                    MENU_ROOT = "GameObject/AudioSystem/";
        private const string                    RESOURCES_PATH = "Assets/AudioSystem/Resources";
        private const string                    MIXER_PATH = "Assets/AudioSystem/Resources/AudioMixer.mixer";
        private const string                    SETTINGS_PATH = "Assets/AudioSystem/Resources/AudioSettings.asset";
        private const string                    CLIP_DATA_PATH = "Assets/AudioSystem/Resources/AudioClipData.asset";

        [MenuItem(MENU_ROOT + "一键创建完整 AudioSystem", false, 0)]
        public static void CreateFullAudioSystem()
        {
            // 1. 确保 Resources 目录存在
            EnsureResourcesFolder();

            // 2. 创建 AudioMixer
            AudioMixer mixer = CreateOrGetAudioMixer();

            // 3. 创建 AudioSettingsSO 并关联 mixer
            AudioSettingsSO settings = CreateOrGetAudioSettings(mixer);

            // 4. 创建 AudioManager
            CreateAudioManagerInternal(settings);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            EditorUtility.DisplayDialog("AudioSystem",
                "完整 AudioSystem 已创建！\n\n" +
                $"  AudioMixer: {(mixer != null ? "已创建" : "创建失败")}\n" +
                $"  AudioSettings: {(settings != null ? "已创建" : "创建失败")}\n" +
                $"  AudioManager: 已放入场景\n\n" +
                "现在你可以在 Audio Mixer 窗口中调整各组音量和效果器。",
                "确定");
        }

        [MenuItem(MENU_ROOT + "创建 AudioManager", false, 10)]
        public static void CreateAudioManager()
        {
            // 检查是否已存在
            AudioManager existing = UnityEngine.Object.FindAnyObjectByType<AudioManager>();
            if (existing != null)
            {
                Selection.activeGameObject = existing.gameObject;
                EditorUtility.DisplayDialog("AudioSystem", "场景中已存在 AudioManager，已为你选中。", "确定");
                return;
            }

            // 尝试加载 Settings
            AudioSettingsSO settings = AssetDatabase.LoadAssetAtPath<AudioSettingsSO>(SETTINGS_PATH);
            if (settings == null)
            {
                settings = CreateOrGetAudioSettings(null);
            }

            CreateAudioManagerInternal(settings);
        }

        [MenuItem(MENU_ROOT + "创建 AudioSettingsSO", false, 11)]
        public static void CreateAudioSettings()
        {
            AudioMixer mixer = AssetDatabase.LoadAssetAtPath<AudioMixer>(MIXER_PATH);
            CreateOrGetAudioSettings(mixer);
        }

        [MenuItem(MENU_ROOT + "创建 AudioMixer", false, 12)]
        public static void CreateAudioMixer()
        {
            CreateOrGetAudioMixer();
        }

        [MenuItem(MENU_ROOT + "场景设置：添加 AudioManager 到首场景", false, 20)]
        public static void QuickSetupScene()
        {
            EditorBuildSettingsScene[] scenes = EditorBuildSettings.scenes;
            if (scenes.Length == 0)
            {
                EditorUtility.DisplayDialog("AudioSystem",
                    "未在 Build Settings 中找到任何场景。请先添加场景。",
                    "确定");
                return;
            }

            CreateAudioManager();
        }

        #region Internal

        private static void CreateAudioManagerInternal(AudioSettingsSO settings)
        {
            // 预加载或创建 AudioClipDataSO（统一处理，避免变量名冲突）
            AudioClipDataSO clip_data = AssetDatabase.LoadAssetAtPath<AudioClipDataSO>(CLIP_DATA_PATH);
            if (clip_data == null)
            {
                clip_data = ScriptableObject.CreateInstance<AudioClipDataSO>();
                AssetDatabase.CreateAsset(clip_data, CLIP_DATA_PATH);
                AssetDatabase.SaveAssets();
            }

            AudioManager existing = UnityEngine.Object.FindAnyObjectByType<AudioManager>();
            if (existing != null)
            {
                Selection.activeGameObject = existing.gameObject;
                // 更新 settings 引用
                SerializedObject so = new SerializedObject(existing);
                if (settings != null)
                {
                    so.FindProperty("settings_").objectReferenceValue = settings;
                }
                so.FindProperty("clip_data_").objectReferenceValue = clip_data;
                so.FindProperty("pool_initial_capacity_").intValue = 10;
                so.FindProperty("pool_max_capacity_").intValue = 30;
                so.ApplyModifiedProperties();
                EditorUtility.DisplayDialog("AudioSystem", "场景中已存在 AudioManager，已更新 Settings 和 ClipData 引用。", "确定");
                return;
            }

            GameObject go = new GameObject("[AudioManager]");
            AudioManager manager = go.AddComponent<AudioManager>();

            SerializedObject manager_so = new SerializedObject(manager);
            if (settings != null)
            {
                manager_so.FindProperty("settings_").objectReferenceValue = settings;
            }
            manager_so.FindProperty("clip_data_").objectReferenceValue = clip_data;
            manager_so.FindProperty("pool_initial_capacity_").intValue = 10;
            manager_so.FindProperty("pool_max_capacity_").intValue = 30;
            manager_so.ApplyModifiedProperties();

            Selection.activeGameObject = go;

            EditorUtility.DisplayDialog("AudioSystem",
                $"AudioManager 已创建！\n\n" +
                $"Settings: {(settings != null ? settings.name : "未赋值")}\n" +
                $"ClipData: {(clip_data != null ? clip_data.name : "未赋值")}\n" +
                $"请将 AudioManager 放置在首场景中，它将自动 DontDestroyOnLoad。",
                "确定");
        }

        private static AudioSettingsSO CreateOrGetAudioSettings(AudioMixer mixer)
        {
            EnsureResourcesFolder();

            // 尝试加载已有
            AudioSettingsSO settings = AssetDatabase.LoadAssetAtPath<AudioSettingsSO>(SETTINGS_PATH);

            if (settings == null)
            {
                settings = ScriptableObject.CreateInstance<AudioSettingsSO>();
                AssetDatabase.CreateAsset(settings, SETTINGS_PATH);
            }

            // 关联 mixer 和 groups
            if (mixer != null)
            {
                SerializedObject settings_so = new SerializedObject(settings);
                settings_so.FindProperty("mixer_").objectReferenceValue = mixer;

                // 自动查找并关联各 subgroup
                AudioMixerGroup[] groups = mixer.FindMatchingGroups("");
                foreach (AudioMixerGroup g in groups)
                {
                    string gname = g.name.ToLowerInvariant();
                    if (gname == "master")
                        settings_so.FindProperty("master_group_").objectReferenceValue = g;
                    else if (gname == "bgm")
                        settings_so.FindProperty("bgm_group_").objectReferenceValue = g;
                    else if (gname == "battle")
                        settings_so.FindProperty("battle_group_").objectReferenceValue = g;
                    else if (gname == "ui")
                        settings_so.FindProperty("ui_group_").objectReferenceValue = g;
                    else if (gname == "environment")
                        settings_so.FindProperty("environment_group_").objectReferenceValue = g;
                    else if (gname == "dialogue")
                        settings_so.FindProperty("dialogue_group_").objectReferenceValue = g;
                }

                settings_so.ApplyModifiedProperties();
            }

            EditorUtility.SetDirty(settings);
            AssetDatabase.SaveAssets();
            Selection.activeObject = settings;

            EditorUtility.DisplayDialog("AudioSystem",
                $"AudioSettingsSO {(mixer != null ? "已创建并关联 AudioMixer" : "已创建")}：{SETTINGS_PATH}",
                "确定");

            return settings;
        }

        private static AudioMixer CreateOrGetAudioMixer()
        {
            EnsureResourcesFolder();

            // 尝试加载已有
            AudioMixer mixer = AssetDatabase.LoadAssetAtPath<AudioMixer>(MIXER_PATH);
            if (mixer != null)
            {
                EnsureMixerGroups(mixer);
                Selection.activeObject = mixer;
                EditorUtility.DisplayDialog("AudioSystem", $"AudioMixer 已存在：{MIXER_PATH}", "确定");
                return mixer;
            }

            // 程序化创建 AudioMixer
            mixer = CreateAudioMixerAsset(MIXER_PATH);
            if (mixer != null)
            {
                EnsureMixerGroups(mixer);
                Selection.activeObject = mixer;
                AssetDatabase.SaveAssets();
                EditorUtility.DisplayDialog("AudioSystem", $"AudioMixer 已创建：{MIXER_PATH}\n\n各组和 Exposed Parameters 已自动配置。", "确定");
            }
            else
            {
                EditorUtility.DisplayDialog("AudioSystem",
                    "无法程序化创建 AudioMixer。\n请手动创建：右键 Project 窗口 → Create → Audio Mixer，\n然后保存到 Assets/AudioSystem/Resources/AudioMixer.mixer",
                    "确定");
            }

            return mixer;
        }

        /// <summary>
        /// 使用 Unity 内部 API 创建 AudioMixer asset
        /// </summary>
        private static AudioMixer CreateAudioMixerAsset(string path)
        {
            // 方法1：反射调用 UnityEditor.Audio.AudioMixerController.CreateAudioMixerAssetAtPath
            try
            {
                Assembly editor_assembly = Assembly.GetAssembly(typeof(EditorWindow));
                if (editor_assembly != null)
                {
                    Type controller_type = editor_assembly.GetType("UnityEditor.Audio.AudioMixerController");
                    if (controller_type != null)
                    {
                        MethodInfo create_method = controller_type.GetMethod(
                            "CreateAudioMixerAssetAtPath",
                            BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic,
                            null,
                            new[] { typeof(string) },
                            null);

                        if (create_method != null)
                        {
                            create_method.Invoke(null, new object[] { path });
                            AssetDatabase.Refresh();
                            AssetDatabase.SaveAssets();

                            AudioMixer mixer = AssetDatabase.LoadAssetAtPath<AudioMixer>(path);
                            if (mixer != null)
                                return mixer;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[AudioManagerSetup] 反射创建 AudioMixer 失败：{ex.Message}");
            }

            // 方法2：尝试直接创建（部分 Unity 版本支持）
            try
            {
                // 某些版本的 Unity 允许通过 CreateInstance 创建
                MethodInfo create_instance = typeof(ScriptableObject).GetMethod(
                    "CreateInstance",
                    BindingFlags.Static | BindingFlags.Public,
                    null,
                    new[] { typeof(Type) },
                    null);

                if (create_instance != null)
                {
                    var mixer_obj = create_instance.Invoke(null, new object[] { typeof(AudioMixer) });
                    if (mixer_obj != null && mixer_obj is AudioMixer)
                    {
                        AssetDatabase.CreateAsset(mixer_obj as AudioMixer, path);
                        AssetDatabase.Refresh();
                        AssetDatabase.SaveAssets();
                        return AssetDatabase.LoadAssetAtPath<AudioMixer>(path);
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[AudioManagerSetup] CreateInstance 创建 AudioMixer 失败：{ex.Message}");
            }

            return null;
        }

        /// <summary>
        /// 确保 AudioMixer 包含所有必需的分组和 exposed parameters
        /// </summary>
        private static void EnsureMixerGroups(AudioMixer mixer)
        {
            if (mixer == null) return;

            AudioMixerGroup master_group = FindOrCreateMixerGroup(mixer, "Master", null);

            string[] group_names = { "BGM", "Battle", "UI", "Environment", "Dialogue" };
            foreach (string name in group_names)
            {
                FindOrCreateMixerGroup(mixer, name, master_group);
            }

            // 创建 exposed parameters
            string[] volume_params =
            {
                AudioSettingsSO.EXPOSED_MASTER,
                AudioSettingsSO.EXPOSED_BGM,
                AudioSettingsSO.EXPOSED_BATTLE,
                AudioSettingsSO.EXPOSED_UI,
                AudioSettingsSO.EXPOSED_ENVIRONMENT,
                AudioSettingsSO.EXPOSED_DIALOGUE,
            };

            foreach (string param in volume_params)
            {
                ExposeMixerParameter(mixer, param);
            }

            // 为每个分组设置 volume parameter（如果有对应的 group，则暴露为 exposed parameter 并关联 attenuation）
            AudioMixerGroup[] groups = mixer.FindMatchingGroups("");
            foreach (AudioMixerGroup g in groups)
            {
                string param_name = GetGroupParamName(g.name);
                if (!string.IsNullOrEmpty(param_name))
                {
                    ExposeMixerParameter(mixer, param_name);
                    // 注意：无法通过公共 API 直接设置 group 的 attenuation 链接到 parameter
                    // 这需要在 Audio Mixer 窗口中手动设置
                }
            }

            EditorUtility.SetDirty(mixer);
        }

        private static AudioMixerGroup FindOrCreateMixerGroup(AudioMixer mixer, string name, AudioMixerGroup parent)
        {
            // 先查找
            AudioMixerGroup[] existing = mixer.FindMatchingGroups(name);
            foreach (AudioMixerGroup g in existing)
            {
                // 精确名称匹配
                AudioMixerGroup[] parents = mixer.FindMatchingGroups(g.name);
                if (g.name.Equals(name, StringComparison.OrdinalIgnoreCase))
                {
                    return g;
                }
            }

            // 通过 SerializedObject 创建子组
            try
            {
                AudioMixerGroup new_group = CreateMixerGroupViaSerializedObject(mixer, name, parent);
                if (new_group != null) return new_group;
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[AudioManagerSetup] 无法创建 MixerGroup '{name}'：{ex.Message}");
            }

            return null;
        }

        /// <summary>
        /// 通过 SerializedObject 操作 mixer 来创建子混音组
        /// </summary>
        private static AudioMixerGroup CreateMixerGroupViaSerializedObject(AudioMixer mixer, string name, AudioMixerGroup parent)
        {
            SerializedObject mixer_so = new SerializedObject(mixer);
            SerializedProperty groups_prop = mixer_so.FindProperty("m_GroupGUIDs");
            SerializedProperty groups_array = mixer_so.FindProperty("m_GroupNames");

            if (groups_prop == null || groups_array == null)
                return null;

            // 生成唯一 GUID
            string guid = Guid.NewGuid().ToString("N");

            // 添加到 groups 列表
            int index = groups_prop.arraySize;
            groups_prop.InsertArrayElementAtIndex(index);
            groups_prop.GetArrayElementAtIndex(index).stringValue = guid;

            groups_array.InsertArrayElementAtIndex(index);
            groups_array.GetArrayElementAtIndex(index).stringValue = name;

            mixer_so.ApplyModifiedProperties();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            // 重新加载并查找新创建的组
            AudioMixerGroup[] all_groups = mixer.FindMatchingGroups("");
            foreach (AudioMixerGroup g in all_groups)
            {
                if (g.name.Equals(name, StringComparison.OrdinalIgnoreCase))
                {
                    return g;
                }
            }

            return null;
        }

        /// <summary>
        /// 暴露 mixer 的 parameter（如果尚未暴露）
        /// </summary>
        private static void ExposeMixerParameter(AudioMixer mixer, string param_name)
        {
            float dummy;
            if (mixer.GetFloat(param_name, out dummy))
                return; // 已存在

            SerializedObject mixer_so = new SerializedObject(mixer);
            SerializedProperty params_prop = mixer_so.FindProperty("m_ExposedParameters");

            if (params_prop == null)
                return;

            // 检查是否已存在
            for (int i = 0; i < params_prop.arraySize; i++)
            {
                SerializedProperty elem = params_prop.GetArrayElementAtIndex(i);
                SerializedProperty name_prop = elem.FindPropertyRelative("name");
                if (name_prop != null && name_prop.stringValue == param_name)
                    return;
            }

            // 添加新的 exposed parameter
            int index = params_prop.arraySize;
            params_prop.InsertArrayElementAtIndex(index);
            SerializedProperty new_elem = params_prop.GetArrayElementAtIndex(index);
            SerializedProperty new_name = new_elem.FindPropertyRelative("name");
            if (new_name != null)
                new_name.stringValue = param_name;

            mixer_so.ApplyModifiedProperties();
            AssetDatabase.SaveAssets();

            // 设置默认值 (0 dB = 1.0 linear)
            mixer.SetFloat(param_name, 0f);
        }

        private static string GetGroupParamName(string group_name)
        {
            return group_name.ToLowerInvariant() switch
            {
                "master" => AudioSettingsSO.EXPOSED_MASTER,
                "bgm" => AudioSettingsSO.EXPOSED_BGM,
                "battle" => AudioSettingsSO.EXPOSED_BATTLE,
                "ui" => AudioSettingsSO.EXPOSED_UI,
                "environment" => AudioSettingsSO.EXPOSED_ENVIRONMENT,
                "dialogue" => AudioSettingsSO.EXPOSED_DIALOGUE,
                _ => null
            };
        }

        private static void EnsureResourcesFolder()
        {
            if (!AssetDatabase.IsValidFolder("Assets/AudioSystem"))
                AssetDatabase.CreateFolder("Assets", "AudioSystem");
            if (!AssetDatabase.IsValidFolder("Assets/AudioSystem/Resources"))
                AssetDatabase.CreateFolder("Assets/AudioSystem", "Resources");
        }

        #endregion
    }
}
#endif