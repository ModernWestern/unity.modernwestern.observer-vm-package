using System.Linq;
using UnityEditor;
using UnityEngine;
using System.Collections.Generic;

namespace ModernWestern.UI.ObserverVM.EditorTools
{
    /// <summary>
    /// Inspector of every <see cref="Widget{TViewModel}"/>: the [BindWidget] references with their status and Auto-assign by name,
    /// the rest of the fields, and in Play mode a live view of the bound ViewModel.
    /// </summary>
    [CustomEditor(typeof(Widget), true), CanEditMultipleObjects]
    internal sealed class WidgetEditor : UnityEditor.Editor
    {
        private AutoAssignReport _lastReport;

        public override bool RequiresConstantRepaint() => Application.isPlaying;

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            var widget = (Widget)target;

            var fields = BindWidgetFields.Of(widget.GetType());

            using (new EditorGUI.DisabledScope(true))
            {
                EditorGUILayout.PropertyField(serializedObject.FindProperty("m_Script"));
            }

            DrawStatus_Internal(widget, fields);

            if (fields.Count > 0)
            {
                DrawWidgets_Internal(fields);
            }

            var excluded = fields.Select(field => field.Name).Append("m_Script").ToArray();

            if (HasOtherFields_Internal(excluded))
            {
                ObserverVMStyles.Section("Settings");

                DrawPropertiesExcluding(serializedObject, excluded);
            }

            serializedObject.ApplyModifiedProperties();

            ObserverVMStyles.Section("ViewModel");

            if (!Application.isPlaying)
            {
                EditorGUILayout.LabelField($"Expects a {widget.ViewModelType.Name}. Enter Play mode to see its live values here.", ObserverVMStyles.MutedLabel);
            }
            else if (targets.Length > 1)
            {
                EditorGUILayout.LabelField("Select a single widget to inspect its ViewModel.", ObserverVMStyles.MutedLabel);
            }
            else
            {
                ViewModelInspector.Draw(widget.BoundViewModel, widget.BindingCount);
            }
        }

#region Internal

        private void DrawStatus_Internal(Widget widget, IReadOnlyList<BindWidgetField> fields)
        {
            var required = fields.Where(field => !field.Optional).ToArray();

            var empty = targets.Cast<Widget>().Sum(each => required.Count(field => !field.IsAssigned(each)));

            EditorGUILayout.Space(4);

            var rect = EditorGUILayout.GetControlRect();

            var text = required.Length == 0 ? "No required widgets" : empty == 0 ? "All required widgets bound" : $"{empty} required widget(s) empty";

            var size = ObserverVMStyles.ChipSize(text);

            var chip = new Rect(rect.xMax - size.x, rect.y, size.x, rect.height);

            EditorGUI.LabelField(new Rect(rect.x, rect.y, rect.width - size.x - 6f, rect.height), "View of", widget.ViewModelType.Name, EditorStyles.boldLabel);

            ObserverVMStyles.DrawChip(chip, text, empty == 0 ? ObserverVMStyles.Accent : ObserverVMStyles.Error);
        }

        private void DrawWidgets_Internal(IReadOnlyList<BindWidgetField> fields)
        {
            ObserverVMStyles.Section("Widgets");

            foreach (var field in fields)
            {
                var property = serializedObject.FindProperty(field.Name);

                if (property == null)
                {
                    EditorGUILayout.LabelField(field.DisplayName, "Not serialized: add [SerializeField] or make it public.", ObserverVMStyles.MutedLabel);

                    continue;
                }

                EditorGUILayout.PropertyField(property, true);
            }

            EditorGUILayout.Space(4);

            using (new EditorGUILayout.HorizontalScope())
            {
                GUILayout.FlexibleSpace();

                if (GUILayout.Button(new GUIContent("Auto-assign by name", "Fills the empty fields with the child whose name matches the field (\"healthBar\" finds \"Health Bar\" or \"Health Bar Image\"), like Unreal's BindWidget. Never overwrites a field that is already set."), GUILayout.Width(170)))
                {
                    serializedObject.ApplyModifiedProperties();

                    _lastReport = null;

                    foreach (var each in targets.Cast<Component>())
                    {
                        _lastReport = BindWidgetAutoAssigner.Assign(each);
                    }

                    serializedObject.Update();
                }
            }

            if (_lastReport != null)
            {
                EditorGUILayout.HelpBox(_lastReport.Summary, MessageType.None);
            }
        }

        private bool HasOtherFields_Internal(string[] excluded)
        {
            var iterator = serializedObject.GetIterator();

            for (var enterChildren = true; iterator.NextVisible(enterChildren); enterChildren = false)
            {
                if (!excluded.Contains(iterator.name))
                {
                    return true;
                }
            }

            return false;
        }

#endregion
    }
}
