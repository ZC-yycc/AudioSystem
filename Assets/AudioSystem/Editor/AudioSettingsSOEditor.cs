#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace AudioSystem.Editor
{
    /// <summary>
    /// AudioSettingsSO 的自定义 Inspector，提供更直观的编辑体验
    /// </summary>
    [CustomEditor(typeof(AudioSettingsSO))]
    public class AudioSettingsSOEditor : UnityEditor.Editor
    {
        private SerializedProperty                              master_volume_prop_;
        private SerializedProperty                              bgm_volume_prop_;
        private SerializedProperty                              battle_volume_prop_;
        private SerializedProperty                              ui_volume_prop_;
        private SerializedProperty                              environment_volume_prop_;
        private SerializedProperty                              dialogue_volume_prop_;

        private void OnEnable()
        {
            master_volume_prop_      = serializedObject.FindProperty("master_volume_");
            bgm_volume_prop_         = serializedObject.FindProperty("bgm_volume_");
            battle_volume_prop_      = serializedObject.FindProperty("battle_volume_");
            ui_volume_prop_          = serializedObject.FindProperty("ui_volume_");
            environment_volume_prop_ = serializedObject.FindProperty("environment_volume_");
            dialogue_volume_prop_    = serializedObject.FindProperty("dialogue_volume_");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            EditorGUILayout.Space(10);
            EditorGUILayout.LabelField("音频系统设置", EditorStyles.boldLabel);
            EditorGUILayout.Space(5);

            // 主音量
            EditorGUILayout.LabelField("主音量 (Master)", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(master_volume_prop_, new GUIContent("Master Volume"));
            EditorGUILayout.Space(30);

            // 分组音量
            EditorGUILayout.LabelField("分组音量", EditorStyles.boldLabel);
            EditorGUI.indentLevel++;

            EditorGUILayout.PropertyField(bgm_volume_prop_, new GUIContent("BGM / 背景音乐"));

            EditorGUILayout.PropertyField(battle_volume_prop_, new GUIContent("Battle / 战斗音效"));

            EditorGUILayout.PropertyField(ui_volume_prop_, new GUIContent("UI / 界面音效"));

            EditorGUILayout.PropertyField(environment_volume_prop_, new GUIContent("Environment / 环境音效"));

            EditorGUILayout.PropertyField(dialogue_volume_prop_, new GUIContent("Dialogue / 对话音效"));

            EditorGUI.indentLevel--;
            EditorGUILayout.Space(10);

            // 快速操作
            EditorGUILayout.LabelField("快速操作", EditorStyles.boldLabel);
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("全部重置为1"))
            {
                master_volume_prop_.floatValue      = 1f;
                bgm_volume_prop_.floatValue         = 1f;
                battle_volume_prop_.floatValue      = 1f;
                ui_volume_prop_.floatValue          = 1f;
                environment_volume_prop_.floatValue = 1f;
                dialogue_volume_prop_.floatValue    = 1f;
            }
            if (GUILayout.Button("全部静音"))
            {
                master_volume_prop_.floatValue      = 0f;
                bgm_volume_prop_.floatValue         = 0f;
                battle_volume_prop_.floatValue      = 0f;
                ui_volume_prop_.floatValue          = 0f;
                environment_volume_prop_.floatValue = 0f;
                dialogue_volume_prop_.floatValue    = 0f;
            }
            EditorGUILayout.EndHorizontal();

            serializedObject.ApplyModifiedProperties();
        }
    }
}
#endif