using UnityEditor;
using UnityEngine;
using System.Collections.Generic;

namespace ModernWestern.UI.ObserverVM.EditorTools
{
    /// <summary>
    /// Lists every required [BindWidget] field left empty in the open scenes and/or prefabs, with buttons to select or fix each one.
    /// </summary>
    internal sealed class ValidationWindow : EditorWindow
    {
        private readonly List<BindWidgetIssue> _issues = new();

        private Vector2 _scroll;

        private string _checked;

        private bool _lastScenes;

        private bool _lastPrefabs;

        [MenuItem("Window/ObserverVM/Validation", false, 2000)]
        public static void Open()
        {
            var window = GetWindow<ValidationWindow>();

            window.titleContent = new GUIContent("ObserverVM Validation");

            window.minSize = new Vector2(420, 200);

            window.Check_Internal(scenes: true, prefabs: false);
        }

        private void OnGUI()
        {
            using (new EditorGUILayout.HorizontalScope(EditorStyles.toolbar))
            {
                if (GUILayout.Button("Check open scenes", EditorStyles.toolbarButton))
                {
                    Check_Internal(scenes: true, prefabs: false);
                }

                if (GUILayout.Button("Check prefabs", EditorStyles.toolbarButton))
                {
                    Check_Internal(scenes: false, prefabs: true);
                }

                if (GUILayout.Button("Check both", EditorStyles.toolbarButton))
                {
                    Check_Internal(scenes: true, prefabs: true);
                }

                GUILayout.FlexibleSpace();

                if (GUILayout.Button("Settings", EditorStyles.toolbarButton))
                {
                    SettingsService.OpenProjectSettings("Project/ObserverVM");
                }
            }

            EditorGUILayout.Space(6);

            if (_checked == null)
            {
                EditorGUILayout.LabelField("Choose what to check.", ObserverVMStyles.MutedLabel);

                return;
            }

            var rect = EditorGUILayout.GetControlRect();

            var text = _issues.Count == 0 ? $"All required widgets are bound in {_checked}" : $"{_issues.Count} required widget(s) empty in {_checked}";

            var size = ObserverVMStyles.ChipSize(text);

            ObserverVMStyles.DrawChip(new Rect(rect.x, rect.y, size.x, rect.height), text, _issues.Count == 0 ? ObserverVMStyles.Accent : ObserverVMStyles.Error);

            EditorGUILayout.Space(4);

            _scroll = EditorGUILayout.BeginScrollView(_scroll);

            for (var i = 0; i < _issues.Count; i++)
            {
                DrawIssue_Internal(_issues[i]);
            }

            EditorGUILayout.EndScrollView();
        }

#region Internal

        private void DrawIssue_Internal(BindWidgetIssue issue)
        {
            if (issue.Component == null)
            {
                return;
            }

            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                EditorGUILayout.LabelField($"\"{issue.Field.DisplayName}\" is empty ({issue.Field.Type.Name} needed)", EditorStyles.boldLabel);

                EditorGUILayout.LabelField($"{issue.HierarchyPath}  ·  {issue.Component.GetType().Name}  ·  {issue.Location}", ObserverVMStyles.MutedLabel);

                using (new EditorGUILayout.HorizontalScope())
                {
                    GUILayout.FlexibleSpace();

                    if (GUILayout.Button("Select", EditorStyles.miniButton, GUILayout.Width(70)))
                    {
                        Selection.activeObject = issue.Component.gameObject;

                        EditorGUIUtility.PingObject(issue.Component.gameObject);
                    }

                    if (GUILayout.Button("Auto-assign by name", EditorStyles.miniButton, GUILayout.Width(140)))
                    {
                        BindWidgetAutoAssigner.Assign(issue.Component);

                        if (EditorUtility.IsPersistent(issue.Component))
                        {
                            AssetDatabase.SaveAssetIfDirty(issue.Component);
                        }

                        Recheck_Internal();

                        GUIUtility.ExitGUI();
                    }
                }
            }
        }

        private void Check_Internal(bool scenes, bool prefabs)
        {
            _lastScenes = scenes;

            _lastPrefabs = prefabs;

            _issues.Clear();

            if (scenes)
            {
                _issues.AddRange(BindWidgetValidator.ValidateOpenScenes());
            }

            if (prefabs)
            {
                _issues.AddRange(BindWidgetValidator.ValidatePrefabs());
            }

            _checked = scenes && prefabs ? "open scenes and prefabs" : scenes ? "open scenes" : "prefabs";

            Repaint();
        }

        private void Recheck_Internal()
        {
            Check_Internal(_lastScenes, _lastPrefabs);
        }

#endregion
    }
}
