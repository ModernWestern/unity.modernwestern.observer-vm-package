using UnityEditor;
using UnityEngine;

namespace ModernWestern.UI.ObserverVM.EditorTools
{
    /// <summary>
    /// Draws [BindWidget] fields with a status mark: accent when bound, red when a required one is empty, grey when an optional one is empty.
    /// </summary>
    [CustomPropertyDrawer(typeof(BindWidgetAttribute))]
    internal sealed class BindWidgetDrawer : PropertyDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            var optional = ((BindWidgetAttribute)attribute).Optional;

            var isEmpty = property.propertyType == SerializedPropertyType.ObjectReference && property.objectReferenceValue == null && !property.hasMultipleDifferentValues;

            var typeName = fieldInfo.FieldType.IsArray ? fieldInfo.FieldType.GetElementType()?.Name : fieldInfo.FieldType.Name;

            Color mark;

            if (!isEmpty)
            {
                mark = ObserverVMStyles.Accent;

                label.tooltip = $"Bound widget ({typeName}).";
            }
            else if (optional)
            {
                mark = ObserverVMStyles.Muted;

                label.tooltip = $"Optional widget ({typeName}): it can stay empty.";
            }
            else
            {
                mark = ObserverVMStyles.Error;

                label.tooltip = $"Required widget: drag a {typeName} here, or use \"Auto-assign by name\" in the widget header.";

                EditorGUI.DrawRect(new Rect(position.x - 2f, position.y, position.width + 2f, position.height), new Color(mark.r, mark.g, mark.b, 0.12f));
            }

            EditorGUI.DrawRect(new Rect(position.x - 6f, position.y + 2f, 2f, position.height - 4f), mark);

            EditorGUI.PropertyField(position, property, label);
        }
    }
}
