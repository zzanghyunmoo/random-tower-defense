using System;
using UnityEditor;
using UnityEditor.Build;
using UnityEngine;

namespace RandomTowerDefense.Editor
{
    public static class ProjectSettingsConfigurator
    {
        public const string CompanyName = "zzanghyunmoo";
        public const string ProductName = "Random Tower Defense";
        public const string BundleIdentifier = "com.zzanghyunmoo.randomtowerdefense";
        public const string IosMinimumVersion = "15.0";
        public const int ActiveInputHandlerInputSystemOnly = 1;

        public static void Apply()
        {
            EditorSettings.serializationMode = SerializationMode.ForceText;
            VersionControlSettings.mode = "Visible Meta Files";

            PlayerSettings.companyName = CompanyName;
            PlayerSettings.productName = ProductName;
            PlayerSettings.SetApplicationIdentifier(NamedBuildTarget.Android, BundleIdentifier);
            PlayerSettings.SetApplicationIdentifier(NamedBuildTarget.iOS, BundleIdentifier);

            PlayerSettings.defaultInterfaceOrientation = UIOrientation.AutoRotation;
            PlayerSettings.allowedAutorotateToPortrait = false;
            PlayerSettings.allowedAutorotateToPortraitUpsideDown = false;
            PlayerSettings.allowedAutorotateToLandscapeLeft = true;
            PlayerSettings.allowedAutorotateToLandscapeRight = true;

            PlayerSettings.SetScriptingBackend(
                NamedBuildTarget.Android,
                ScriptingImplementation.IL2CPP);
            PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARM64;
            PlayerSettings.Android.minSdkVersion = AndroidSdkVersions.AndroidApiLevel25;
            PlayerSettings.Android.targetSdkVersion = AndroidSdkVersions.AndroidApiLevelAuto;

            PlayerSettings.iOS.targetOSVersionString = IosMinimumVersion;
            PlayerSettings.iOS.targetDevice = iOSTargetDevice.iPhoneAndiPad;

            SetActiveInputHandlerToInputSystem();

            AssetDatabase.SaveAssets();
            Debug.Log("Random Tower Defense project settings applied.");
        }

        private static void SetActiveInputHandlerToInputSystem()
        {
            UnityEngine.Object[] settingsAssets =
                AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/ProjectSettings.asset");

            if (settingsAssets.Length == 0)
            {
                throw new InvalidOperationException("Could not load ProjectSettings.asset.");
            }

            using var serializedSettings = new SerializedObject(settingsAssets[0]);
            SerializedProperty activeInputHandler =
                serializedSettings.FindProperty("activeInputHandler");

            if (activeInputHandler == null)
            {
                throw new InvalidOperationException(
                    "Could not find the activeInputHandler project setting.");
            }

            activeInputHandler.intValue = ActiveInputHandlerInputSystemOnly;
            serializedSettings.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}
