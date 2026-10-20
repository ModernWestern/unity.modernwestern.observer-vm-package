using System;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;
using System.Collections.Generic;

namespace ModernWestern.UI.ObserverVM.EditorTools
{
    public enum AutoAssignOutcome
    {
        Assigned,
        AlreadySet,
        NotFound,
        Ambiguous
    }

    public readonly struct AutoAssignEntry
    {
        public AutoAssignEntry(BindWidgetField field, AutoAssignOutcome outcome, IReadOnlyList<GameObject> matches)
        {
            Field = field;

            Outcome = outcome;

            Matches = matches;
        }

        public BindWidgetField Field { get; }

        public AutoAssignOutcome Outcome { get; }

        public IReadOnlyList<GameObject> Matches { get; }
    }

    public sealed class AutoAssignReport
    {
        public AutoAssignReport(IReadOnlyList<AutoAssignEntry> entries)
        {
            Entries = entries;
        }

        public IReadOnlyList<AutoAssignEntry> Entries { get; }

        public int Count(AutoAssignOutcome outcome) => Entries.Count(entry => entry.Outcome == outcome);

        public string Summary
        {
            get
            {
                var text = new StringBuilder();

                text.Append($"Assigned {Count(AutoAssignOutcome.Assigned)}");

                var notFound = Entries.Where(entry => entry.Outcome == AutoAssignOutcome.NotFound && !entry.Field.Optional).Select(entry => entry.Field.DisplayName).ToArray();

                if (notFound.Length > 0)
                {
                    text.Append($". No object named like: {string.Join(", ", notFound)}");
                }

                var ambiguous = Entries.Where(entry => entry.Outcome == AutoAssignOutcome.Ambiguous).Select(entry => $"{entry.Field.DisplayName} ({entry.Matches.Count} matches)").ToArray();

                if (ambiguous.Length > 0)
                {
                    text.Append($". More than one match, left empty: {string.Join(", ", ambiguous)}");
                }

                return text.Append('.').ToString();
            }
        }
    }

    /// <summary>
    /// Fills empty <see cref="BindWidgetAttribute"/> fields with the child whose name matches the field, like Unreal's BindWidget.
    /// "healthBar" matches "Health Bar", "HealthBar", "health_bar" and also "Health Bar Image" (field name plus a widget suffix).
    /// Children that belong to another <see cref="Widget"/> are not searched. Supports Undo.
    /// </summary>
    public static class BindWidgetAutoAssigner
    {
        private static readonly string[] Suffixes =
        {
            "text", "label", "title", "button", "btn", "image", "img", "icon", "slider", "toggle", "checkbox", "dropdown",
            "input", "inputfield", "field", "scrollbar", "panel", "group", "root", "container", "widget"
        };

        public static AutoAssignReport Assign(Component owner)
        {
            var candidates = new List<GameObject>();

            Collect_Internal(owner.transform, owner, candidates);

            var serializedObject = new SerializedObject(owner);

            var entries = new List<AutoAssignEntry>();

            foreach (var field in BindWidgetFields.Of(owner.GetType()))
            {
                if (field.IsAssigned(owner))
                {
                    entries.Add(new AutoAssignEntry(field, AutoAssignOutcome.AlreadySet, Array.Empty<GameObject>()));

                    continue;
                }

                var matches = candidates.Where(candidate => Matches(candidate.name, field) && Resolve_Internal(candidate, field.Type) != null).ToArray();

                if (matches.Length == 1)
                {
                    serializedObject.FindProperty(field.Name).objectReferenceValue = Resolve_Internal(matches[0], field.Type);

                    entries.Add(new AutoAssignEntry(field, AutoAssignOutcome.Assigned, matches));
                }
                else
                {
                    entries.Add(new AutoAssignEntry(field, matches.Length == 0 ? AutoAssignOutcome.NotFound : AutoAssignOutcome.Ambiguous, matches));
                }
            }

            serializedObject.ApplyModifiedProperties();

            return new AutoAssignReport(entries);
        }

        /// <summary>
        /// Whether a GameObject name matches a field by Unreal-style naming rules.
        /// </summary>
        public static bool Matches(string objectName, BindWidgetField field)
        {
            var name = Normalize(objectName);

            var fieldName = Normalize(field.Name);

            if (name.Length == 0 || fieldName.Length == 0)
            {
                return false;
            }

            if (name == fieldName)
            {
                return true;
            }

            var typeName = Normalize(field.Type.Name);

            // "TMP_Text" and "TMP_InputField" read as "Text" and "InputField".
            if (typeName.StartsWith("tmp") && typeName.Length > 3)
            {
                typeName = typeName.Substring(3);
            }

            return IsNamePlusSuffix_Internal(name, fieldName, typeName) || IsNamePlusSuffix_Internal(fieldName, name, typeName);
        }

        public static string Normalize(string value)
        {
            var text = new StringBuilder(value.Length);

            foreach (var character in value)
            {
                if (char.IsLetterOrDigit(character))
                {
                    text.Append(char.ToLowerInvariant(character));
                }
            }

            return text.ToString();
        }

#region Internal

        // longer == shorter + (type name | known suffix), e.g. "volumeslider" == "volume" + "slider".
        private static bool IsNamePlusSuffix_Internal(string longer, string shorter, string typeName)
        {
            if (longer.Length <= shorter.Length || !longer.StartsWith(shorter, StringComparison.Ordinal))
            {
                return false;
            }

            var suffix = longer.Substring(shorter.Length);

            return suffix == typeName || Array.IndexOf(Suffixes, suffix) >= 0;
        }

        private static void Collect_Internal(Transform transform, Component owner, List<GameObject> candidates)
        {
            candidates.Add(transform.gameObject);

            foreach (Transform child in transform)
            {
                var nestedWidget = child.GetComponent<Widget>();

                if (nestedWidget != null && nestedWidget != owner)
                {
                    // Another widget owns its children: only the nested widget itself can be bound.
                    candidates.Add(child.gameObject);

                    continue;
                }

                Collect_Internal(child, owner, candidates);
            }
        }

        private static UnityEngine.Object Resolve_Internal(GameObject candidate, Type type)
        {
            return type == typeof(GameObject) ? candidate : candidate.GetComponent(type);
        }

#endregion
    }
}
