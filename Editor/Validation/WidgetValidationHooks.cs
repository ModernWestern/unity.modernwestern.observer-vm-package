using UnityEditor;
using UnityEngine;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine.SceneManagement;
using System.Collections.Generic;

namespace ModernWestern.UI.ObserverVM.EditorTools
{
    /// <summary>
    /// Checks required [BindWidget] fields when entering Play mode.
    /// </summary>
    [InitializeOnLoad]
    internal static class PlayModeWidgetValidation
    {
        static PlayModeWidgetValidation()
        {
            EditorApplication.playModeStateChanged += OnPlayModeStateChanged_Internal;
        }

        private static void OnPlayModeStateChanged_Internal(PlayModeStateChange change)
        {
            var policy = ObserverVMSettings.instance.OnPlay;

            if (change != PlayModeStateChange.ExitingEditMode || policy == MissingWidgetPolicy.Ignore)
            {
                return;
            }

            var issues = BindWidgetValidator.ValidateOpenScenes();

            if (issues.Count == 0)
            {
                return;
            }

            foreach (var issue in issues)
            {
                Debug.LogWarning($"[ObserverVM] {issue.Message}", issue.Component);
            }

            if (policy != MissingWidgetPolicy.Block)
            {
                return;
            }

            EditorApplication.isPlaying = false;

            Debug.LogError($"[ObserverVM] Play was stopped: {issues.Count} required widget reference(s) are empty. Fill them (or change Project Settings > ObserverVM > When pressing Play).");
        }
    }

    /// <summary>
    /// Checks required [BindWidget] fields in every scene of the build, and in prefabs if enabled.
    /// </summary>
    internal sealed class BuildWidgetValidation : IPreprocessBuildWithReport, IProcessSceneWithReport
    {
        public int callbackOrder => 0;

        public void OnPreprocessBuild(BuildReport report)
        {
            var settings = ObserverVMSettings.instance;

            if (settings.OnBuild == MissingWidgetPolicy.Ignore || !settings.CheckPrefabsOnBuild)
            {
                return;
            }

            Report_Internal(BindWidgetValidator.ValidatePrefabs(), "prefabs");
        }

        public void OnProcessScene(Scene scene, BuildReport report)
        {
            // Also called when entering Play mode (report == null): that case is handled by PlayModeWidgetValidation.
            if (report == null || ObserverVMSettings.instance.OnBuild == MissingWidgetPolicy.Ignore)
            {
                return;
            }

            Report_Internal(BindWidgetValidator.ValidateScene(scene), $"scene {scene.path}");
        }

        private static void Report_Internal(List<BindWidgetIssue> issues, string what)
        {
            if (issues.Count == 0)
            {
                return;
            }

            var message = $"[ObserverVM] {issues.Count} required widget reference(s) are empty in {what}:\n{BindWidgetValidator.Describe(issues)}";

            if (ObserverVMSettings.instance.OnBuild == MissingWidgetPolicy.Block)
            {
                throw new BuildFailedException(message + "Fill them, or change Project Settings > ObserverVM > When building.");
            }

            Debug.LogWarning(message);
        }
    }
}
