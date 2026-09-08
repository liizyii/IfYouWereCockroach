#if UNITY_EDITOR
using System;
using System.IO;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace IfYouWereCockroach.EditorTools
{
    public static class BuildRelease
    {
        private const string ScenePath = "Assets/Scenes/SampleScene.unity";
        private const string DefaultOutputPath = "Builds/Windows/IfYouWereCockroach.exe";

        [MenuItem("If You Were Cockroach/Build Windows Release")]
        public static void BuildWindowsReleaseMenu()
        {
            BuildWindowsReleaseFromCommandLine();
        }

        public static void BuildWindowsReleaseFromCommandLine()
        {
            string outputPath = GetCommandLineValue("-outputPath") ?? DefaultOutputPath;
            outputPath = outputPath.Replace('\\', '/');
            string directory = Path.GetDirectoryName(outputPath);
            if (!string.IsNullOrEmpty(directory))
            {
                Directory.CreateDirectory(directory);
            }

            PlayerSettings.companyName = "liizyii";
            PlayerSettings.productName = "If You Were Cockroach";
            PlayerSettings.bundleVersion = "0.2.0";
            PlayerSettings.fullScreenMode = FullScreenMode.Windowed;
            PlayerSettings.defaultScreenWidth = 1280;
            PlayerSettings.defaultScreenHeight = 720;
            PlayerSettings.resizableWindow = true;
            PlayerSettings.runInBackground = true;

            EditorBuildSettings.scenes = new[]
            {
                new EditorBuildSettingsScene(ScenePath, true)
            };

            EditorUserBuildSettings.SwitchActiveBuildTarget(BuildTargetGroup.Standalone, BuildTarget.StandaloneWindows64);

            var options = new BuildPlayerOptions
            {
                scenes = new[] { ScenePath },
                locationPathName = outputPath,
                target = BuildTarget.StandaloneWindows64,
                options = BuildOptions.None
            };

            BuildReport report = BuildPipeline.BuildPlayer(options);
            BuildSummary summary = report.summary;
            Debug.Log($"Release build result: {summary.result}; size: {summary.totalSize} bytes; output: {outputPath}");

            if (summary.result != BuildResult.Succeeded)
            {
                throw new InvalidOperationException($"Windows release build failed with result {summary.result}");
            }
        }

        private static string GetCommandLineValue(string key)
        {
            string[] args = Environment.GetCommandLineArgs();
            for (int i = 0; i < args.Length - 1; i++)
            {
                if (args[i].Equals(key, StringComparison.OrdinalIgnoreCase))
                {
                    return args[i + 1];
                }
            }

            return null;
        }
    }
}
#endif