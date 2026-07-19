#nullable enable

using System;
using System.IO;
using System.Linq;
using RandomTowerDefense.Data.Validation;
using UnityEditor;
using UnityEditor.Android;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace RandomTowerDefense.Editor.Build
{
    public static class MobileBuildPipeline
    {
        public const string AndroidOutputRelativePath =
            "Builds/Validation/Android/RandomTowerDefense.apk";

        public const string IosOutputRelativePath = "Builds/Validation/iOS";

        public static void ValidateData()
        {
            DataValidationResult result = DataValidationMenu.ValidateProjectDataSet();
            if (!result.IsValid)
            {
                throw new BuildFailedException(
                    $"Data validation failed with {result.Issues.Count} error(s):\n" +
                    string.Join("\n", result.Issues));
            }

            Debug.Log("AUTOMATION_DATA_VALIDATION_SUCCEEDED");
        }

        public static void BuildAndroid()
        {
            ValidateData();
            RequireTargetSupport(BuildTargetGroup.Android, BuildTarget.Android);
            if (PlayerSettings.GetScriptingBackend(NamedBuildTarget.Android) !=
                ScriptingImplementation.IL2CPP)
            {
                throw new BuildFailedException("Android must use the IL2CPP scripting backend.");
            }

            if (PlayerSettings.Android.targetArchitectures != AndroidArchitecture.ARM64)
            {
                throw new BuildFailedException("Android must target ARM64 only.");
            }

            AndroidToolSettingsSnapshot? previousTools = ConfigureAndroidToolsFromEnvironment();
            try
            {
                string outputPath = ResolveValidationPath(AndroidOutputRelativePath);
                Directory.CreateDirectory(Path.GetDirectoryName(outputPath)!);
                if (File.Exists(outputPath))
                {
                    File.Delete(outputPath);
                }

                EditorUserBuildSettings.buildAppBundle = false;
                BuildPlayer(BuildTarget.Android, outputPath);
                if (!File.Exists(outputPath) || new FileInfo(outputPath).Length == 0)
                {
                    throw new BuildFailedException(
                        $"Android build did not create '{outputPath}'.");
                }
            }
            finally
            {
                previousTools?.Restore();
            }
        }

        public static void ExportIos()
        {
            ValidateData();
            RequireTargetSupport(BuildTargetGroup.iOS, BuildTarget.iOS);

            string outputPath = ResolveValidationPath(IosOutputRelativePath);
            if (Directory.Exists(outputPath))
            {
                Directory.Delete(outputPath, recursive: true);
            }

            BuildPlayer(BuildTarget.iOS, outputPath);
            string projectFile = Path.Combine(outputPath, "Unity-iPhone.xcodeproj", "project.pbxproj");
            if (!File.Exists(projectFile))
            {
                throw new BuildFailedException(
                    $"iOS export did not create the expected Xcode project '{projectFile}'.");
            }
        }

        private static void BuildPlayer(BuildTarget target, string outputPath)
        {
            string[] scenes = EditorBuildSettings.scenes
                .Where(scene => scene.enabled)
                .Select(scene => scene.path)
                .ToArray();
            if (scenes.Length == 0)
            {
                throw new BuildFailedException("At least one enabled build scene is required.");
            }

            var options = new BuildPlayerOptions
            {
                scenes = scenes,
                locationPathName = outputPath,
                target = target,
                options = BuildOptions.Development | BuildOptions.DetailedBuildReport,
            };
            BuildReport report = BuildPipeline.BuildPlayer(options);
            BuildSummary summary = report.summary;
            if (summary.result != BuildResult.Succeeded)
            {
                throw new BuildFailedException(
                    $"{target} build finished with {summary.result}, " +
                    $"{summary.totalErrors} error(s), and {summary.totalWarnings} warning(s).");
            }

            Debug.Log(
                $"AUTOMATION_PLAYER_BUILD_SUCCEEDED target={target} " +
                $"bytes={summary.totalSize} duration={summary.totalTime} output={outputPath}");
        }

        private static void RequireTargetSupport(BuildTargetGroup group, BuildTarget target)
        {
            if (!BuildPipeline.IsBuildTargetSupported(group, target))
            {
                throw new BuildFailedException($"Unity Build Support for {target} is not installed.");
            }
        }

        private static AndroidToolSettingsSnapshot? ConfigureAndroidToolsFromEnvironment()
        {
            string? sdkRoot = Environment.GetEnvironmentVariable("UNITY_ANDROID_SDK_ROOT");
            if (string.IsNullOrWhiteSpace(sdkRoot))
            {
                return null;
            }

            string ndkRoot = RequireEnvironmentDirectory("UNITY_ANDROID_NDK_ROOT");
            string jdkRoot = RequireEnvironmentDirectory("UNITY_ANDROID_JDK_ROOT");
            string validatedSdkRoot = RequireDirectory(sdkRoot, "Android SDK");
            var previousTools = new AndroidToolSettingsSnapshot(
                AndroidExternalToolsSettings.sdkRootPath,
                AndroidExternalToolsSettings.ndkRootPath,
                AndroidExternalToolsSettings.jdkRootPath);
            AndroidExternalToolsSettings.sdkRootPath = validatedSdkRoot;
            AndroidExternalToolsSettings.ndkRootPath = ndkRoot;
            AndroidExternalToolsSettings.jdkRootPath = jdkRoot;
            Debug.Log(
                $"AUTOMATION_ANDROID_TOOLS_CONFIGURED sdk={sdkRoot} ndk={ndkRoot} jdk={jdkRoot}");
            return previousTools;
        }

        private static string RequireEnvironmentDirectory(string variableName)
        {
            string? value = Environment.GetEnvironmentVariable(variableName);
            if (string.IsNullOrWhiteSpace(value))
            {
                throw new BuildFailedException(
                    $"{variableName} is required when UNITY_ANDROID_SDK_ROOT is set.");
            }

            return RequireDirectory(value, variableName);
        }

        private static string RequireDirectory(string path, string label)
        {
            string fullPath = Path.GetFullPath(path);
            if (!Directory.Exists(fullPath))
            {
                throw new BuildFailedException($"{label} directory '{fullPath}' does not exist.");
            }

            return fullPath;
        }

        private static string ResolveValidationPath(string relativePath)
        {
            string projectRoot = Path.GetFullPath(
                Path.Combine(UnityEngine.Application.dataPath, ".."));
            string validationRoot = Path.GetFullPath(
                Path.Combine(projectRoot, "Builds", "Validation"));
            string outputPath = Path.GetFullPath(Path.Combine(projectRoot, relativePath));
            string requiredPrefix = validationRoot + Path.DirectorySeparatorChar;
            if (!outputPath.StartsWith(requiredPrefix, StringComparison.OrdinalIgnoreCase))
            {
                throw new BuildFailedException(
                    $"Build output '{outputPath}' must remain under '{validationRoot}'.");
            }

            return outputPath;
        }

        private readonly struct AndroidToolSettingsSnapshot
        {
            private readonly string _sdkRoot;
            private readonly string _ndkRoot;
            private readonly string _jdkRoot;

            public AndroidToolSettingsSnapshot(string sdkRoot, string ndkRoot, string jdkRoot)
            {
                _sdkRoot = sdkRoot;
                _ndkRoot = ndkRoot;
                _jdkRoot = jdkRoot;
            }

            public void Restore()
            {
                AndroidExternalToolsSettings.sdkRootPath = _sdkRoot;
                AndroidExternalToolsSettings.ndkRootPath = _ndkRoot;
                AndroidExternalToolsSettings.jdkRootPath = _jdkRoot;
            }
        }
    }
}
