using UnityEditor;
using UnityEngine;

namespace ModernWestern.UI.ObserverVM.EditorTools
{
    public enum MissingWidgetPolicy
    {
        [InspectorName("Ignore")] Ignore,
        [InspectorName("Warn in the Console")] Warn,
        [InspectorName("Stop (block)")] Block
    }

    /// <summary>
    /// Project-wide ObserverVM options, stored in ProjectSettings/ObserverVMSettings.asset (Project Settings > ObserverVM).
    /// </summary>
    [FilePath("ProjectSettings/ObserverVMSettings.asset", FilePathAttribute.Location.ProjectFolder)]
    public sealed class ObserverVMSettings : ScriptableSingleton<ObserverVMSettings>
    {
        [SerializeField, Tooltip("What happens when you press Play and an open scene has a required BindWidget left empty.")]
        private MissingWidgetPolicy onPlay = MissingWidgetPolicy.Warn;

        [SerializeField, Tooltip("What happens when you build and a scene in the build (or a prefab, if checked below) has a required BindWidget left empty. Block makes the build fail, like Unreal refusing to compile the widget.")]
        private MissingWidgetPolicy onBuild = MissingWidgetPolicy.Block;

        [SerializeField, Tooltip("Also check every prefab under Assets when building, not only the scenes in the build.")]
        private bool checkPrefabsOnBuild = true;

        public MissingWidgetPolicy OnPlay
        {
            get => onPlay;
            set => Set_Internal(ref onPlay, value);
        }

        public MissingWidgetPolicy OnBuild
        {
            get => onBuild;
            set => Set_Internal(ref onBuild, value);
        }

        public bool CheckPrefabsOnBuild
        {
            get => checkPrefabsOnBuild;
            set => Set_Internal(ref checkPrefabsOnBuild, value);
        }

        private void Set_Internal<T>(ref T field, T value)
        {
            if (Equals(field, value))
            {
                return;
            }

            field = value;

            Save(true);
        }
    }

    internal static class ObserverVMSettingsProvider
    {
        [SettingsProvider]
        public static SettingsProvider Create()
        {
            return new SettingsProvider("Project/ObserverVM", SettingsScope.Project)
            {
                label = "ObserverVM",
                keywords = new[] { "ObserverVM", "BindWidget", "Widget", "Validation", "Binding" },
                guiHandler = _ =>
                {
                    var settings = ObserverVMSettings.instance;

                    EditorGUILayout.LabelField("Required widgets ([BindWidget])", EditorStyles.boldLabel);

                    EditorGUILayout.HelpBox("A required [BindWidget] field left empty is like a missing BindWidget in Unreal: the widget cannot work. Choose how strict the editor is about it.", MessageType.None);

                    settings.OnPlay = (MissingWidgetPolicy)EditorGUILayout.EnumPopup(new GUIContent("When pressing Play", "Block keeps you in Edit mode until the references are filled."), settings.OnPlay);

                    settings.OnBuild = (MissingWidgetPolicy)EditorGUILayout.EnumPopup(new GUIContent("When building", "Block makes the build fail with the list of empty fields."), settings.OnBuild);

                    settings.CheckPrefabsOnBuild = EditorGUILayout.Toggle(new GUIContent("Check prefabs on build", "Also check every prefab under Assets, not only the scenes in the build."), settings.CheckPrefabsOnBuild);

                    EditorGUILayout.Space();

                    if (GUILayout.Button("Open the Validation window", GUILayout.Width(220)))
                    {
                        ValidationWindow.Open();
                    }
                }
            };
        }
    }
}
