using System.Linq;
using UnityEditor;
using UnityEngine;

namespace ModernWestern.UI.ObserverVM.EditorTools
{
    /// <summary>
    /// In Play mode, lists every live widget with its ViewModel and bindings. Click one (or select it in the Hierarchy) to inspect it.
    /// </summary>
    internal sealed class DebuggerWindow : EditorWindow
    {
        private Widget[] _widgets = new Widget[0];

        private Widget _selected;

        private Vector2 _listScroll;

        private Vector2 _detailScroll;

        private double _nextRefresh;

        [MenuItem("Window/ObserverVM/Debugger", false, 2001)]
        public static void Open()
        {
            var window = GetWindow<DebuggerWindow>();

            window.titleContent = new GUIContent("ObserverVM Debugger");

            window.minSize = new Vector2(600, 260);

            window.OnSelectionChange();
        }

        // Follows the selection: selecting a widget in the Hierarchy shows its ViewModel here.
        private void OnSelectionChange()
        {
            var selected = Selection.activeGameObject != null ? Selection.activeGameObject.GetComponent<Widget>() : null;

            if (selected != null)
            {
                _selected = selected;

                Repaint();
            }
        }

        private void Update()
        {
            if (!Application.isPlaying)
            {
                return;
            }

            if (EditorApplication.timeSinceStartup >= _nextRefresh)
            {
                _widgets = FindObjectsByType<Widget>(FindObjectsInactive.Include, FindObjectsSortMode.None).OrderBy(widget => widget.name).ToArray();

                _nextRefresh = EditorApplication.timeSinceStartup + 0.5;
            }

            Repaint();
        }

        private void OnGUI()
        {
            if (!Application.isPlaying)
            {
                EditorGUILayout.Space(6);

                EditorGUILayout.LabelField("Enter Play mode to see the live widgets and their ViewModels.", ObserverVMStyles.MutedLabel);

                return;
            }

            using (new EditorGUILayout.HorizontalScope())
            {
                using (var scroll = new EditorGUILayout.ScrollViewScope(_listScroll, GUILayout.Width(210)))
                {
                    _listScroll = scroll.scrollPosition;

                    ObserverVMStyles.Section($"Widgets ({_widgets.Length})");

                    foreach (var widget in _widgets)
                    {
                        if (widget == null)
                        {
                            continue;
                        }

                        DrawListItem_Internal(widget);
                    }
                }

                using (var scroll = new EditorGUILayout.ScrollViewScope(_detailScroll))
                {
                    _detailScroll = scroll.scrollPosition;

                    if (_selected == null)
                    {
                        EditorGUILayout.LabelField("Pick a widget on the left or select one in the Hierarchy.", ObserverVMStyles.MutedLabel);
                    }
                    else
                    {
                        ObserverVMStyles.Section(BindWidgetValidator.HierarchyPath(_selected.transform));

                        ViewModelInspector.Draw(_selected.BoundViewModel, _selected.BindingCount);
                    }
                }
            }
        }

        private void DrawListItem_Internal(Widget widget)
        {
            var viewModel = widget.BoundViewModel == null ? "no ViewModel" : widget.ViewModelType.Name;

            var content = new GUIContent($"{widget.name}\n{viewModel} · {widget.BindingCount} binding(s)", BindWidgetValidator.HierarchyPath(widget.transform));

            var rect = GUILayoutUtility.GetRect(content, ObserverVMStyles.Wrapped);

            if (widget == _selected)
            {
                EditorGUI.DrawRect(rect, new Color(ObserverVMStyles.Accent.r, ObserverVMStyles.Accent.g, ObserverVMStyles.Accent.b, 0.25f));
            }

            GUI.Label(rect, content, ObserverVMStyles.Wrapped);

            if (Event.current.type == EventType.MouseDown && rect.Contains(Event.current.mousePosition))
            {
                _selected = widget;

                Selection.activeGameObject = widget.gameObject;

                Event.current.Use();
            }
        }
    }
}
