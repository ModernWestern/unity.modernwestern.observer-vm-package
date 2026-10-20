using System;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEditorInternal;
using System.Collections.Generic;
using Object = UnityEngine.Object;

namespace ModernWestern.UI.ObserverVM.EditorTools
{
    /// <summary>
    /// The "View Bindings" list of the widget inspector: ViewModel member → widget property, without code (like UMG's View Bindings).
    /// The property menu only offers what fits the member's type, and each row says in plain words what is wrong, if anything.
    /// </summary>
    internal sealed class ViewBindingsSection
    {
        private const float Gap = 4f;

        private readonly SerializedProperty _bindings;

        private readonly ReorderableList _list;

        private readonly Type _viewModelType;

        private readonly List<ViewBindingSource> _sources;

        private static GUIStyle _problemStyle;

        private static GUIStyle _arrowStyle;

        public ViewBindingsSection(SerializedObject serializedObject, Type viewModelType)
        {
            _viewModelType = viewModelType;

            _sources = ViewBindingSource.Of(viewModelType);

            _bindings = serializedObject.FindProperty("viewBindings");

            _list = new ReorderableList(serializedObject, _bindings, true, false, true, true)
            {
                drawElementCallback = DrawElement_Internal,
                elementHeightCallback = Height_Internal,
                onAddCallback = Add_Internal,
                drawNoneElementCallback = rect => EditorGUI.LabelField(rect, "None yet. Press + to bind a ViewModel member to a widget.", ObserverVMStyles.MutedLabel)
            };
        }

        public void Draw(bool multipleTargets)
        {
            ObserverVMStyles.Section("View Bindings");

            if (multipleTargets)
            {
                EditorGUILayout.LabelField("Select a single widget to edit its view bindings.", ObserverVMStyles.MutedLabel);

                return;
            }

            if (_sources.Count == 0)
            {
                EditorGUILayout.LabelField($"{_viewModelType.Name} has no public Observable, IReadOnlyObservable or Command members to bind.", ObserverVMStyles.MutedLabel);

                if (_bindings.arraySize == 0)
                {
                    return;
                }
            }
            else
            {
                EditorGUILayout.LabelField("Bind ViewModel members to widgets without code. They are applied right after OnBind.", ObserverVMStyles.MutedLabel);
            }

            _list.DoLayoutList();
        }

#region Internal

        // Fixed height (ReorderableList caches row heights): a problem replaces the options on the second line.
        private float Height_Internal(int index)
        {
            return 2f * EditorGUIUtility.singleLineHeight + Gap + 6f;
        }

        private void Add_Internal(ReorderableList list)
        {
            var index = _bindings.arraySize;

            _bindings.InsertArrayElementAtIndex(index);

            var element = _bindings.GetArrayElementAtIndex(index);

            element.FindPropertyRelative("source").stringValue = string.Empty;

            element.FindPropertyRelative("target").objectReferenceValue = null;

            element.FindPropertyRelative("property").stringValue = string.Empty;

            element.FindPropertyRelative("format").stringValue = string.Empty;

            element.FindPropertyRelative("invert").boolValue = false;

            list.index = index;
        }

        private void DrawElement_Internal(Rect rect, int index, bool isActive, bool isFocused)
        {
            var element = _bindings.GetArrayElementAtIndex(index);

            var source = element.FindPropertyRelative("source");

            var target = element.FindPropertyRelative("target");

            var property = element.FindPropertyRelative("property");

            var line = EditorGUIUtility.singleLineHeight;

            var problem = Problem_Internal(element);

            if (problem != null)
            {
                // Same language as the [BindWidget] drawer: red mark and a light red tint on the broken row.
                EditorGUI.DrawRect(new Rect(rect.x - 4f, rect.y + 1f, rect.width + 6f, rect.height - 2f), new Color(ObserverVMStyles.Error.r, ObserverVMStyles.Error.g, ObserverVMStyles.Error.b, 0.12f));

                EditorGUI.DrawRect(new Rect(rect.x - 6f, rect.y + 3f, 2f, rect.height - 6f), ObserverVMStyles.Error);
            }

            rect.y += 3f;

            var half = rect.width * 0.45f;

            // Line 1: member → target.
            DrawSource_Internal(new Rect(rect.x, rect.y, half, line), source);

            _arrowStyle ??= new GUIStyle(EditorStyles.label) { alignment = TextAnchor.MiddleCenter };

            GUI.Label(new Rect(rect.x + half, rect.y, 24f, line), "→", _arrowStyle);

            EditorGUI.ObjectField(new Rect(rect.x + half + 24f, rect.y, rect.width - half - 24f, line), target, typeof(Object), GUIContent.none);

            // Line 2: property and its options.
            var y = rect.y + line + Gap;

            var binder = DrawProperty_Internal(new Rect(rect.x, y, half, line), source, target, property);

            var optionRect = new Rect(rect.x + half + 24f, y, rect.width - half - 24f, line);

            if (problem != null)
            {
                _problemStyle ??= new GUIStyle(EditorStyles.label) { normal = { textColor = ObserverVMStyles.Error }, clipping = TextClipping.Clip };

                var text = char.ToUpperInvariant(problem[0]) + problem.Substring(1);

                EditorGUI.LabelField(optionRect, new GUIContent(text, text), _problemStyle);
            }
            else if (binder != null && binder.Has(ViewBinderFeatures.Format))
            {
                var format = element.FindPropertyRelative("format");

                EditorGUI.PropertyField(optionRect, format, new GUIContent("Format", "Optional, e.g. \"{0}\", \"{0:2}\" (2 decimals), \"Score {0}\"."));
            }
            else if (binder != null && binder.Has(ViewBinderFeatures.Invert))
            {
                EditorGUI.PropertyField(optionRect, element.FindPropertyRelative("invert"), new GUIContent("Invert", "Use the opposite value (e.g. Muted → not interactable)."));
            }
            else if (binder != null && binder.Has(ViewBinderFeatures.TwoWay) && ViewBindingSource.Find(_viewModelType, source.stringValue).Writable)
            {
                EditorGUI.LabelField(optionRect, "Two-way: edits go back to the ViewModel.", ObserverVMStyles.MutedLabel);
            }
        }

        private void DrawSource_Internal(Rect rect, SerializedProperty source)
        {
            var current = source.stringValue;

            var labels = new List<string> { "(pick a ViewModel member)" };

            labels.AddRange(_sources.Select(each => each.Describe()));

            var selected = _sources.FindIndex(each => each.Name == current) + 1;

            var missing = selected == 0 && !string.IsNullOrEmpty(current);

            if (missing)
            {
                labels.Insert(1, $"{current} (missing)");

                selected = 1;
            }

            var chosen = EditorGUI.Popup(rect, selected, labels.ToArray());

            if (chosen == selected)
            {
                return;
            }

            var sourceIndex = chosen - (missing ? 2 : 1);

            source.stringValue = sourceIndex >= 0 && sourceIndex < _sources.Count ? _sources[sourceIndex].Name : string.Empty;
        }

        // The menu lists every compatible property of the target GameObject and its components ("Image/Fill", "GameObject/Active").
        private ViewBinder DrawProperty_Internal(Rect rect, SerializedProperty source, SerializedProperty target, SerializedProperty property)
        {
            var gameObject = target.objectReferenceValue is Component component ? component.gameObject : target.objectReferenceValue as GameObject;

            var found = ViewBindingSource.Find(_viewModelType, source.stringValue);

            if (gameObject == null || found.ValueType == null && !found.IsCommand)
            {
                using (new EditorGUI.DisabledScope(true))
                {
                    EditorGUI.Popup(rect, 0, new[] { gameObject == null ? "(pick a target first)" : "(pick a member first)" });
                }

                return null;
            }

            var options = new List<(Object candidate, ViewBinder binder)>();

            foreach (var candidate in new Object[] { gameObject }.Concat(gameObject.GetComponents<Component>()))
            {
                if (candidate == null)
                {
                    continue;
                }

                options.AddRange(ViewBinders.For(candidate.GetType(), found).Select(binder => (candidate, binder)));
            }

            var labels = new List<string> { "(pick what to bind)" };

            labels.AddRange(options.Select(option => $"{option.candidate.GetType().Name}/{option.binder.Id}{(option.binder.Has(ViewBinderFeatures.TwoWay) && found.Writable ? " (two-way)" : string.Empty)}"));

            var selected = options.FindIndex(option => option.candidate == target.objectReferenceValue && option.binder.Id == property.stringValue) + 1;

            var unavailable = selected == 0 && !string.IsNullOrEmpty(property.stringValue);

            if (unavailable)
            {
                labels.Insert(1, $"{property.stringValue} (not available)");

                selected = 1;
            }

            var chosen = EditorGUI.Popup(rect, selected, labels.ToArray());

            var optionIndex = chosen - (unavailable ? 2 : 1);

            if (chosen != selected && optionIndex >= 0 && optionIndex < options.Count)
            {
                target.objectReferenceValue = options[optionIndex].candidate;

                property.stringValue = options[optionIndex].binder.Id;

                return options[optionIndex].binder;
            }

            return target.objectReferenceValue == null ? null : ViewBinders.Find(target.objectReferenceValue.GetType(), property.stringValue, found);
        }

        private string Problem_Internal(SerializedProperty element)
        {
            return ViewBindingValidation.Problem(_viewModelType,
                                                 element.FindPropertyRelative("source").stringValue,
                                                 element.FindPropertyRelative("target").objectReferenceValue,
                                                 element.FindPropertyRelative("property").stringValue);
        }

#endregion
    }
}
