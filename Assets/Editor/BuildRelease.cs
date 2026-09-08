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
        private const string DefaultWindowsOutputPath = "Builds/Windows/IfYouWereCockroach.exe";
        private const string DefaultWebGlOutputPath = "Builds/WebGL";

        [MenuItem("If You Were Cockroach/Build Windows Release")]
        public static void BuildWindowsReleaseMenu()
        {
            BuildWindowsReleaseFromCommandLine();
        }

        [MenuItem("If You Were Cockroach/Build WebGL Release")]
        public static void BuildWebGLReleaseMenu()
        {
            BuildWebGLReleaseFromCommandLine();
        }

        public static void BuildWindowsReleaseFromCommandLine()
        {
            string outputPath = (GetCommandLineValue("-outputPath") ?? DefaultWindowsOutputPath).Replace('\\', '/');
            string directory = Path.GetDirectoryName(outputPath);
            if (!string.IsNullOrEmpty(directory))
            {
                Directory.CreateDirectory(directory);
            }

            ApplyCommonPlayerSettings();
            EditorUserBuildSettings.SwitchActiveBuildTarget(BuildTargetGroup.Standalone, BuildTarget.StandaloneWindows64);

            BuildPlayerOptions options = CreateBuildOptions(outputPath, BuildTarget.StandaloneWindows64);
            RunBuild(options, "Windows release build");
        }

        public static void BuildWebGLReleaseFromCommandLine()
        {
            string outputPath = (GetCommandLineValue("-outputPath") ?? DefaultWebGlOutputPath).Replace('\\', '/');
            Directory.CreateDirectory(outputPath);

            ApplyCommonPlayerSettings();
            PlayerSettings.WebGL.compressionFormat = WebGLCompressionFormat.Disabled;
            PlayerSettings.WebGL.decompressionFallback = true;
            PlayerSettings.WebGL.dataCaching = true;
            EditorUserBuildSettings.SwitchActiveBuildTarget(BuildTargetGroup.WebGL, BuildTarget.WebGL);

            BuildPlayerOptions options = CreateBuildOptions(outputPath, BuildTarget.WebGL);
            RunBuild(options, "WebGL release build");
        }

        private static void ApplyCommonPlayerSettings()
        {
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
        }

        private static BuildPlayerOptions CreateBuildOptions(string outputPath, BuildTarget target)
        {
            return new BuildPlayerOptions
            {
                scenes = new[] { ScenePath },
                locationPathName = outputPath,
                target = target,
                options = BuildOptions.None
            };
        }

        private static void RunBuild(BuildPlayerOptions options, string label)
        {
            BuildReport report = BuildPipeline.BuildPlayer(options);
            BuildSummary summary = report.summary;
            Debug.Log($"{label} result: {summary.result}; size: {summary.totalSize} bytes; output: {options.locationPathName}");

            if (summary.result != BuildResult.Succeeded)
            {
                throw new InvalidOperationException($"{label} failed with result {summary.result}");
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