using System;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace Reforged.EditorTools
{
    /// <summary>
    /// Headless build entry point for CI (GameCI's buildMethod). The graybox scene is generated
    /// from code rather than committed, so this builds it first, then the player for whatever
    /// target the editor was launched with (-buildTarget), to -customBuildPath.
    /// </summary>
    public static class CIBuild
    {
        public static void Build()
        {
            var target = EditorUserBuildSettings.activeBuildTarget;
            var path = Arg("-customBuildPath") ?? "build/" + target + "/AvengersReforged";
            Debug.Log("[CIBuild] " + target + " -> " + path);

            EnableInputSystem();
            PlayerSettings.companyName = "Modern Gentlemen";
            PlayerSettings.productName = "Avengers Re:Forged (graybox)";
            if (target == BuildTarget.WebGL)
            {
                // GitHub Pages can't serve pre-compressed Unity files, so ship them uncompressed.
                PlayerSettings.WebGL.compressionFormat = WebGLCompressionFormat.Disabled;
                PlayerSettings.WebGL.decompressionFallback = false;
            }

            GrayboxSceneBuilder.BuildScene();
            AssetDatabase.SaveAssets();

            var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
            {
                scenes = new[] { GrayboxSceneBuilder.ScenePath },
                locationPathName = path,
                target = target,
                options = BuildOptions.None,
            });
            var summary = report.summary;
            Debug.Log("[CIBuild] " + summary.result + ": " + summary.totalErrors + " errors, " + summary.totalSize / (1024 * 1024) + " MB");
            if (summary.result != BuildResult.Succeeded) EditorApplication.Exit(1);
        }

        /// <summary>
        /// The project ships without a full ProjectSettings.asset, so the Input System backend isn't
        /// switched on yet. Set "Active Input Handling" to Both (2) so PlayerInput works in builds.
        /// </summary>
        static void EnableInputSystem()
        {
            var assets = AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/ProjectSettings.asset");
            if (assets.Length == 0) return;
            var settings = new SerializedObject(assets[0]);
            var handler = settings.FindProperty("activeInputHandler");
            if (handler == null || handler.intValue == 2) return;
            handler.intValue = 2;
            settings.ApplyModifiedPropertiesWithoutUndo();
        }

        static string Arg(string name)
        {
            var args = Environment.GetCommandLineArgs();
            for (int i = 0; i < args.Length - 1; i++)
                if (args[i] == name) return args[i + 1];
            return null;
        }
    }
}
