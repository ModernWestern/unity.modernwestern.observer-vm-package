using System;
using UnityEditor;
using UnityEngine;
using System.Reflection;
using System.Collections.Generic;

namespace ModernWestern.UI.ObserverVM.EditorTools
{
    /// <summary>
    /// A field marked with <see cref="BindWidgetAttribute"/>.
    /// </summary>
    public sealed class BindWidgetField
    {
        internal BindWidgetField(FieldInfo field, BindWidgetAttribute attribute)
        {
            Field = field;

            Optional = attribute.Optional;

            DisplayName = ObjectNames.NicifyVariableName(field.Name);
        }

        public FieldInfo Field { get; }

        /// <summary>
        /// Serialized property path (the field name).
        /// </summary>
        public string Name => Field.Name;

        public string DisplayName { get; }

        public Type Type => Field.FieldType;

        public bool Optional { get; }

        public bool IsAssigned(Component owner)
        {
            return Field.GetValue(owner) is UnityEngine.Object value && value != null;
        }
    }

    /// <summary>
    /// Finds the <see cref="BindWidgetAttribute"/> fields of a component type (including private and inherited ones). Cached per type.
    /// </summary>
    public static class BindWidgetFields
    {
        private const BindingFlags Flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly;

        private static readonly Dictionary<Type, BindWidgetField[]> Cache = new();

        public static IReadOnlyList<BindWidgetField> Of(Type componentType)
        {
            if (Cache.TryGetValue(componentType, out var fields))
            {
                return fields;
            }

            var found = new List<BindWidgetField>();

            for (var type = componentType; type != null && type != typeof(MonoBehaviour); type = type.BaseType)
            {
                foreach (var field in type.GetFields(Flags))
                {
                    if (field.GetCustomAttribute<BindWidgetAttribute>() is { } attribute && IsSupported_Internal(field.FieldType))
                    {
                        found.Add(new BindWidgetField(field, attribute));
                    }
                }
            }

            fields = found.ToArray();

            Cache[componentType] = fields;

            return fields;
        }

        public static bool HasAny(Type componentType) => Of(componentType).Count > 0;

        private static bool IsSupported_Internal(Type fieldType)
        {
            return fieldType == typeof(GameObject) || typeof(Component).IsAssignableFrom(fieldType);
        }
    }
}
