using UnityEditor;
using UnityEngine;

namespace ModernWestern.UI.ObserverVM.Drawers
{
    [CustomPropertyDrawer(typeof(BindWidgetAttribute))]
    internal sealed class BindWidgetDrawer : PropertyDrawer
    {
        private const string BoundTag = "<color=#5BC236>[Bind]</color> ";

        private const string MissingTag = "<color=#FF5555>[Bind]</color> ";

        private static GUIStyle _richLabel;

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            _richLabel ??= new GUIStyle(EditorStyles.label)
            {
                richText = true
            };

            var bindWidget = (BindWidgetAttribute)attribute;

            var isMissing = !bindWidget.Optional && property.propertyType == SerializedPropertyType.ObjectReference && property.objectReferenceValue == null;

            var content = new GUIContent((isMissing ? MissingTag : BoundTag) + label.text, isMissing ? "BindWidget: this reference is required." : "BindWidget");

            EditorGUI.BeginProperty(position, label, property);

            var labelRect = new Rect(position.x, position.y, EditorGUIUtility.labelWidth, position.height);

            var fieldRect = new Rect(position.x + EditorGUIUtility.labelWidth, position.y, position.width - EditorGUIUtility.labelWidth, position.height);

            EditorGUI.LabelField(labelRect, content, _richLabel);

            var previousColor = GUI.backgroundColor;

            if (isMissing)
            {
                GUI.backgroundColor = new Color(1f, 0.45f, 0.45f);
            }

            EditorGUI.PropertyField(fieldRect, property, GUIContent.none);

            GUI.backgroundColor = previousColor;

            EditorGUI.EndProperty();
        }
    }
}
