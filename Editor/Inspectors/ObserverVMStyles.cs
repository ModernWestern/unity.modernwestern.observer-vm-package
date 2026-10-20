using UnityEditor;
using UnityEngine;

namespace ModernWestern.UI.ObserverVM.EditorTools
{
    /// <summary>
    /// Shared look of the ObserverVM editor UI: sober, readable in the light and dark editor themes, one solid accent.
    /// </summary>
    internal static class ObserverVMStyles
    {
        public static readonly Color Accent = new Color32(0x7C, 0x5C, 0xFF, 0xFF);

        public static readonly Color Error = new Color32(0xE5, 0x48, 0x4D, 0xFF);

        private static GUIStyle _sectionTitle;

        private static GUIStyle _chip;

        private static GUIStyle _muted;

        private static GUIStyle _wrapped;

        public static Color Muted => EditorGUIUtility.isProSkin ? new Color(1f, 1f, 1f, 0.45f) : new Color(0f, 0f, 0f, 0.5f);

        public static Color Line => EditorGUIUtility.isProSkin ? new Color(1f, 1f, 1f, 0.08f) : new Color(0f, 0f, 0f, 0.12f);

        public static GUIStyle SectionTitle => _sectionTitle ??= new GUIStyle(EditorStyles.boldLabel) { margin = new RectOffset(3, 3, 10, 2) };

        public static GUIStyle Chip => _chip ??= new GUIStyle(EditorStyles.miniLabel)
        {
            alignment = TextAnchor.MiddleCenter,
            padding = new RectOffset(8, 8, 1, 2),
            fontStyle = FontStyle.Bold
        };

        public static GUIStyle MutedLabel => _muted ??= new GUIStyle(EditorStyles.miniLabel) { wordWrap = true, normal = { textColor = Muted } };

        public static GUIStyle Wrapped => _wrapped ??= new GUIStyle(EditorStyles.label) { wordWrap = true };

        public static void Section(string title)
        {
            EditorGUILayout.LabelField(title, SectionTitle);

            var rect = GUILayoutUtility.GetRect(1f, 1f, GUILayout.ExpandWidth(true));

            EditorGUI.DrawRect(rect, Line);

            EditorGUILayout.Space(2);
        }

        /// <summary>
        /// A small tinted label, e.g. "10/10 bound".
        /// </summary>
        public static void DrawChip(Rect rect, string text, Color color)
        {
            EditorGUI.DrawRect(rect, new Color(color.r, color.g, color.b, EditorGUIUtility.isProSkin ? 0.22f : 0.15f));

            var previous = Chip.normal.textColor;

            Chip.normal.textColor = color;

            GUI.Label(rect, text, Chip);

            Chip.normal.textColor = previous;
        }

        public static Vector2 ChipSize(string text) => Chip.CalcSize(new GUIContent(text));
    }
}
