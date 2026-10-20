using System.Text;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections.Generic;

namespace ModernWestern.UI.ObserverVM.EditorTools
{
    /// <summary>
    /// A required <see cref="BindWidgetAttribute"/> field that is empty, or a View Binding that cannot work.
    /// </summary>
    public readonly struct BindWidgetIssue
    {
        public BindWidgetIssue(Component component, BindWidgetField field, string location)
        {
            Component = component;

            Field = field;

            Binding = null;

            Problem = null;

            Location = location;
        }

        public BindWidgetIssue(Component component, ViewBinding binding, string problem, string location)
        {
            Component = component;

            Field = null;

            Binding = binding;

            Problem = problem;

            Location = location;
        }

        public Component Component { get; }

        /// <summary>
        /// The empty [BindWidget] field, or null for a View Binding issue.
        /// </summary>
        public BindWidgetField Field { get; }

        /// <summary>
        /// The broken View Binding, or null for an empty [BindWidget] field.
        /// </summary>
        public ViewBinding Binding { get; }

        public string Problem { get; }

        /// <summary>
        /// Scene or prefab path the component lives in.
        /// </summary>
        public string Location { get; }

        public string HierarchyPath => BindWidgetValidator.HierarchyPath(Component.transform);

        public string Title => Field != null ? $"\"{Field.DisplayName}\" is empty ({Field.Type.Name} needed)" : $"View binding \"{Binding}\": {Problem}";

        public string Message => Field != null
            ? $"{HierarchyPath} ({Component.GetType().Name}): \"{Field.DisplayName}\" is empty ({Field.Type.Name} needed). Drag one in, or use Auto-assign by name."
            : $"{HierarchyPath} ({Component.GetType().Name}): view binding \"{Binding}\" cannot work: {Problem} Fix it in the View Bindings of the widget.";
    }

    /// <summary>
    /// Finds required <see cref="BindWidgetAttribute"/> fields left empty, like Unreal refusing to compile a widget with a missing BindWidget.
    /// </summary>
    public static class BindWidgetValidator
    {
        public static void Validate(Component component, string location, List<BindWidgetIssue> issues)
        {
            if (component == null)
            {
                return;
            }

            foreach (var field in BindWidgetFields.Of(component.GetType()))
            {
                if (!field.Optional && !field.IsAssigned(component))
                {
                    issues.Add(new BindWidgetIssue(component, field, location));
                }
            }

            if (component is not Widget widget)
            {
                return;
            }

            foreach (var binding in widget.ViewBindings)
            {
                if (ViewBindingValidation.Problem(widget.ViewModelType, binding) is { } problem)
                {
                    issues.Add(new BindWidgetIssue(component, binding, problem, location));
                }
            }
        }

        public static void ValidateHierarchy(GameObject root, string location, List<BindWidgetIssue> issues)
        {
            foreach (var component in root.GetComponentsInChildren<MonoBehaviour>(true))
            {
                if (component != null && (component is Widget || BindWidgetFields.HasAny(component.GetType())))
                {
                    Validate(component, location, issues);
                }
            }
        }

        public static List<BindWidgetIssue> ValidateScene(Scene scene)
        {
            var issues = new List<BindWidgetIssue>();

            foreach (var root in scene.GetRootGameObjects())
            {
                ValidateHierarchy(root, scene.path, issues);
            }

            return issues;
        }

        public static List<BindWidgetIssue> ValidateOpenScenes()
        {
            var issues = new List<BindWidgetIssue>();

            for (var i = 0; i < SceneManager.sceneCount; i++)
            {
                var scene = SceneManager.GetSceneAt(i);

                if (scene.isLoaded)
                {
                    issues.AddRange(ValidateScene(scene));
                }
            }

            return issues;
        }

        public static List<BindWidgetIssue> ValidatePrefabs()
        {
            var issues = new List<BindWidgetIssue>();

            foreach (var guid in AssetDatabase.FindAssets("t:Prefab", new[] { "Assets" }))
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);

                if (AssetDatabase.LoadAssetAtPath<GameObject>(path) is { } prefab)
                {
                    ValidateHierarchy(prefab, path, issues);
                }
            }

            return issues;
        }

        public static string HierarchyPath(Transform transform)
        {
            var path = new StringBuilder(transform.name);

            for (var parent = transform.parent; parent != null; parent = parent.parent)
            {
                path.Insert(0, parent.name + "/");
            }

            return path.ToString();
        }

        /// <summary>
        /// One readable block listing the issues (used by build errors and logs).
        /// </summary>
        public static string Describe(IReadOnlyList<BindWidgetIssue> issues, int max = 20)
        {
            var text = new StringBuilder();

            for (var i = 0; i < issues.Count && i < max; i++)
            {
                text.Append("- ").Append(issues[i].Location).Append(": ").AppendLine(issues[i].Message);
            }

            if (issues.Count > max)
            {
                text.AppendLine($"...and {issues.Count - max} more. Open Window > ObserverVM > Validation to see them all.");
            }

            return text.ToString();
        }
    }
}
