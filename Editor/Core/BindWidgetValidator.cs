using System.Text;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections.Generic;

namespace ModernWestern.UI.ObserverVM.EditorTools
{
    /// <summary>
    /// A required <see cref="BindWidgetAttribute"/> field that is empty.
    /// </summary>
    public readonly struct BindWidgetIssue
    {
        public BindWidgetIssue(Component component, BindWidgetField field, string location)
        {
            Component = component;

            Field = field;

            Location = location;
        }

        public Component Component { get; }

        public BindWidgetField Field { get; }

        /// <summary>
        /// Scene or prefab path the component lives in.
        /// </summary>
        public string Location { get; }

        public string HierarchyPath => BindWidgetValidator.HierarchyPath(Component.transform);

        public string Message => $"{HierarchyPath} ({Component.GetType().Name}): \"{Field.DisplayName}\" is empty ({Field.Type.Name} needed). Drag one in, or use Auto-assign by name.";
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
        }

        public static void ValidateHierarchy(GameObject root, string location, List<BindWidgetIssue> issues)
        {
            foreach (var component in root.GetComponentsInChildren<MonoBehaviour>(true))
            {
                if (component != null && BindWidgetFields.HasAny(component.GetType()))
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
