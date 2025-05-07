using UnityEditor;
using UnityEngine;

namespace ModernWestern.UI.ObserverVM.Drawers
{
    [CustomPropertyDrawer(typeof(BindableAttribute))]
    public class BindableAttributeDrawer : PropertyDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            var key = ((BindableAttribute)attribute).Key;

            var displayKey = string.IsNullOrEmpty(key) ? property.name : key;

            var labelWidth = EditorGUIUtility.labelWidth;

            var fieldWidth = position.width - labelWidth;

            var labelRect = new Rect(position.x, position.y, labelWidth, position.height);

            var fieldRect = new Rect(position.x + labelWidth, position.y, fieldWidth, position.height);

            var richLabel = new GUIStyle(EditorStyles.label)
            {
                richText = true
            };

            EditorGUI.LabelField(labelRect, $"<color=green><Bind></color> {displayKey}", richLabel);

            EditorGUI.PropertyField(fieldRect, property, GUIContent.none);
        }
    }

}
