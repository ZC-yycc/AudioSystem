#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace AudioSystem.Editor
{
    /// <summary>
    /// AudioClipDataSO 的自定义 Inspector，提供直观的音频条目编辑体验
    /// </summary>
    [CustomEditor(typeof(AudioClipDataSO))]
    public class AudioClipDataSOEditor : UnityEditor.Editor
    {
        private SerializedProperty                  entries_prop_;
        private Vector2                             scroll_pos_;
        private string                              search_filter_ = "";
        private bool                                show_help_ = true;
        private string                              new_audio_id_ = "";

        private void OnEnable()
        {
            entries_prop_ = serializedObject.FindProperty("entries_");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            EditorGUILayout.Space(8);
            EditorGUILayout.LabelField("音频数据编辑器", EditorStyles.boldLabel);
            EditorGUILayout.Space(4);

            // 帮助面板
            show_help_ = EditorGUILayout.Foldout(show_help_, "使用说明");
            if (show_help_)
            {
                EditorGUI.indentLevel++;
                EditorGUILayout.HelpBox(
                    "每个条目包含：\n" +
                    "• Audio ID — 唯一标识符，代码中通过此 ID 播放音频\n" +
                    "• Audio Clip — 音频资源\n" +
                    "• Group — 所属音频分组（控制路由和音量）\n" +
                    "• Volume — 音量倍率（叠加在分组音量之上）\n" +
                    "• Pitch — 音调随机范围（x=最小, y=最大），相等即为固定值",
                    MessageType.Info);
                EditorGUI.indentLevel--;
                EditorGUILayout.Space(4);
            }

            // 搜索和快速创建
            EditorGUILayout.BeginHorizontal();
            search_filter_ = EditorGUILayout.TextField("搜索 ID", search_filter_);
            if (GUILayout.Button("清除", GUILayout.Width(50)))
                search_filter_ = "";
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            new_audio_id_ = EditorGUILayout.TextField("新 ID", new_audio_id_);
            if (GUILayout.Button("快速添加", GUILayout.Width(80)) && !string.IsNullOrEmpty(new_audio_id_))
            {
                AddNewEntry(new_audio_id_);
                new_audio_id_ = "";
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space(8);

            // 条目数量统计
            int total_entries = entries_prop_.arraySize;
            int shown_entries = CountFilteredEntries();
            EditorGUILayout.LabelField(
                string.IsNullOrEmpty(search_filter_)
                    ? $"条目总数：{total_entries}"
                    : $"匹配条目：{shown_entries} / {total_entries}",
                EditorStyles.miniLabel);

            // 排序按钮
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("按 ID 排序", GUILayout.Width(100)))
                SortEntriesById();
            if (GUILayout.Button("按分组排序", GUILayout.Width(100)))
                SortEntriesByGroup();
            GUILayout.FlexibleSpace();
            if (GUILayout.Button("清空所有条目", GUILayout.Width(100)))
            {
                if (EditorUtility.DisplayDialog("确认", "确定要清空所有音频条目吗？此操作不可撤销。", "确定", "取消"))
                {
                    entries_prop_.ClearArray();
                }
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space(4);
            DrawHorizontalLine();

            // 条目列表
            scroll_pos_ = EditorGUILayout.BeginScrollView(scroll_pos_);

            for (int i = 0; i < entries_prop_.arraySize; i++)
            {
                SerializedProperty entry_prop = entries_prop_.GetArrayElementAtIndex(i);
                SerializedProperty id_prop = entry_prop.FindPropertyRelative("audio_id");

                // 搜索过滤
                if (!string.IsNullOrEmpty(search_filter_) &&
                    !id_prop.stringValue.ToLower().Contains(search_filter_.ToLower()))
                    continue;

                DrawEntry(entry_prop, i);
                EditorGUILayout.Space(2);
            }

            EditorGUILayout.EndScrollView();

            serializedObject.ApplyModifiedProperties();

            // 如果数据有变更，重建 lookup
            if (GUI.changed)
            {
                ((AudioClipDataSO)target).BuildLookup();
                EditorUtility.SetDirty(target);
            }
        }

        private void DrawEntry(SerializedProperty entry_prop, int index)
        {
            SerializedProperty id_prop = entry_prop.FindPropertyRelative("audio_id");
            SerializedProperty clip_prop = entry_prop.FindPropertyRelative("clip");
            SerializedProperty group_prop = entry_prop.FindPropertyRelative("group");
            SerializedProperty volume_prop = entry_prop.FindPropertyRelative("volume");
            SerializedProperty pitch_prop = entry_prop.FindPropertyRelative("pitch");

            // 整体背景
            Rect bg_rect = EditorGUILayout.BeginVertical(EditorStyles.helpBox);

            // 标题行：ID + 删除按钮
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField($"#{index}", GUILayout.Width(30));

            GUI.SetNextControlName($"audio_id_{index}");
            string old_id = id_prop.stringValue;
            EditorGUILayout.PropertyField(id_prop, new GUIContent("ID"));
            string new_id = id_prop.stringValue;

            GUI.color = Color.red;
            if (GUILayout.Button("✕", GUILayout.Width(24), GUILayout.Height(18)))
            {
                DeleteEntry(index);
                GUI.color = Color.white;
                EditorGUILayout.EndHorizontal();
                EditorGUILayout.EndVertical();
                return;
            }
            GUI.color = Color.white;

            EditorGUILayout.EndHorizontal();

            // 音频剪辑
            EditorGUILayout.PropertyField(clip_prop, new GUIContent("Clip"));

            // 分组选择
            EditorGUILayout.PropertyField(group_prop, new GUIContent("Group"));

            // 音量和音调
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.PropertyField(volume_prop, new GUIContent("Volume"), GUILayout.MinWidth(80));
            EditorGUILayout.PropertyField(pitch_prop, new GUIContent("Pitch (x=min, y=max)"), GUILayout.MinWidth(80));
            EditorGUILayout.EndHorizontal();

            // 音量滑块
            Rect slider_rect = EditorGUILayout.GetControlRect(false, 16f);
            volume_prop.floatValue = EditorGUI.Slider(slider_rect, volume_prop.floatValue, 0f, 2f);

            // 预览按钮
            if (clip_prop.objectReferenceValue != null)
            {
                EditorGUILayout.BeginHorizontal();
                GUI.enabled = !EditorApplication.isPlaying ||
                              (clip_prop.objectReferenceValue is AudioClip ac && ac.loadType != AudioClipLoadType.DecompressOnLoad);
                if (GUILayout.Button("▶ 试听", GUILayout.Width(80)))
                {
                    PlayPreview(clip_prop.objectReferenceValue as AudioClip, volume_prop.floatValue, pitch_prop.vector2Value);
                }
                GUI.enabled = true;

                if (GUILayout.Button("■ 停止", GUILayout.Width(80)))
                {
                    StopPreview();
                }
                EditorGUILayout.EndHorizontal();
            }

            // 复制 ID 按钮
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("复制 ID", GUILayout.Width(100)))
            {
                GUIUtility.systemCopyBuffer = id_prop.stringValue;
                Debug.Log($"[AudioClipDataSO] 已复制音频 ID: {id_prop.stringValue}");
            }
            if (GUILayout.Button("生成代码片段", GUILayout.Width(120)))
            {
                GenerateCodeSnippet(id_prop.stringValue);
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.EndVertical();
        }

        private void AddNewEntry(string audio_id)
        {
            entries_prop_.InsertArrayElementAtIndex(entries_prop_.arraySize);
            SerializedProperty new_entry = entries_prop_.GetArrayElementAtIndex(entries_prop_.arraySize - 1);
            new_entry.FindPropertyRelative("audio_id").stringValue = audio_id;
            new_entry.FindPropertyRelative("clip").objectReferenceValue = null;
            new_entry.FindPropertyRelative("group").enumValueIndex = 0;
            new_entry.FindPropertyRelative("volume").floatValue = 1f;
            new_entry.FindPropertyRelative("pitch").vector2Value = Vector2.one;

            serializedObject.ApplyModifiedProperties();
            ((AudioClipDataSO)target).BuildLookup();
            EditorUtility.SetDirty(target);
        }

        private void DeleteEntry(int index)
        {
            entries_prop_.DeleteArrayElementAtIndex(index);
            serializedObject.ApplyModifiedProperties();
            ((AudioClipDataSO)target).BuildLookup();
            EditorUtility.SetDirty(target);
        }

        private int CountFilteredEntries()
        {
            if (string.IsNullOrEmpty(search_filter_))
                return entries_prop_.arraySize;

            int count = 0;
            for (int i = 0; i < entries_prop_.arraySize; i++)
            {
                SerializedProperty entry = entries_prop_.GetArrayElementAtIndex(i);
                string id = entry.FindPropertyRelative("audio_id").stringValue;
                if (id.ToLower().Contains(search_filter_.ToLower()))
                    count++;
            }
            return count;
        }

        private void SortEntriesById()
        {
            AudioClipDataSO data = (AudioClipDataSO)target;
            var entries = new List<AudioClipEntry>(data.Entries);
            entries.Sort((a, b) => string.CompareOrdinal(a.audio_id, b.audio_id));

            entries_prop_.ClearArray();
            for (int i = 0; i < entries.Count; i++)
            {
                entries_prop_.InsertArrayElementAtIndex(i);
                SerializedProperty entry_prop = entries_prop_.GetArrayElementAtIndex(i);
                entry_prop.FindPropertyRelative("audio_id").stringValue = entries[i].audio_id;
                entry_prop.FindPropertyRelative("clip").objectReferenceValue = entries[i].clip;
                entry_prop.FindPropertyRelative("group").enumValueIndex = (int)entries[i].group;
                entry_prop.FindPropertyRelative("volume").floatValue = entries[i].volume;
                entry_prop.FindPropertyRelative("pitch").vector2Value = entries[i].pitch;
            }

            serializedObject.ApplyModifiedProperties();
            data.BuildLookup();
            EditorUtility.SetDirty(target);
        }

        private void SortEntriesByGroup()
        {
            AudioClipDataSO data = (AudioClipDataSO)target;
            var entries = new List<AudioClipEntry>(data.Entries);
            entries.Sort((a, b) =>
            {
                int group_compare = a.group.CompareTo(b.group);
                if (group_compare != 0) return group_compare;
                return string.CompareOrdinal(a.audio_id, b.audio_id);
            });

            entries_prop_.ClearArray();
            for (int i = 0; i < entries.Count; i++)
            {
                entries_prop_.InsertArrayElementAtIndex(i);
                SerializedProperty entry_prop = entries_prop_.GetArrayElementAtIndex(i);
                entry_prop.FindPropertyRelative("audio_id").stringValue = entries[i].audio_id;
                entry_prop.FindPropertyRelative("clip").objectReferenceValue = entries[i].clip;
                entry_prop.FindPropertyRelative("group").enumValueIndex = (int)entries[i].group;
                entry_prop.FindPropertyRelative("volume").floatValue = entries[i].volume;
                entry_prop.FindPropertyRelative("pitch").vector2Value = entries[i].pitch;
            }

            serializedObject.ApplyModifiedProperties();
            data.BuildLookup();
            EditorUtility.SetDirty(target);
        }

        private void GenerateCodeSnippet(string audio_id)
        {
            string snippet = $"AudioManager.Instance.Play(\"{audio_id}\");";
            GUIUtility.systemCopyBuffer = snippet;
            Debug.Log($"[AudioClipDataSO] 已复制代码片段: {snippet}");
        }

        private static void PlayPreview(AudioClip clip, float volume, Vector2 pitch_range)
        {
            if (clip == null) return;

            StopPreview();

            AudioClipEntry temp_entry = default;
            temp_entry.pitch = pitch_range;
            float preview_pitch = temp_entry.GetRandomPitch();

            GameObject preview_go = EditorUtility.CreateGameObjectWithHideFlags(
                "AudioPreview", HideFlags.HideAndDontSave);
            AudioSource source = preview_go.AddComponent<AudioSource>();
            source.clip = clip;
            source.volume = Mathf.Clamp01(volume);
            source.pitch = preview_pitch;
            source.Play();

            // 播放完成后自动销毁
            EditorApplication.delayCall += () =>
            {
                if (preview_go != null && source != null && !source.isPlaying)
                {
                    DestroyImmediate(preview_go);
                }
            };
        }

        private static void StopPreview()
        {
            GameObject existing = GameObject.Find("AudioPreview");
            if (existing != null)
                DestroyImmediate(existing);
        }

        private static void DrawHorizontalLine()
        {
            Rect rect = EditorGUILayout.GetControlRect(false, 1f);
            rect.height = 1f;
            EditorGUI.DrawRect(rect, new Color(0.5f, 0.5f, 0.5f, 0.5f));
        }
    }
}
#endif