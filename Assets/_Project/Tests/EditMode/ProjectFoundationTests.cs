using System.Linq;
using NUnit.Framework;
using RandomTowerDefense.Core;
using RandomTowerDefense.Editor;
using UnityEditor;
using UnityEditor.Build;
using UnityEngine;

namespace RandomTowerDefense.Tests.EditMode
{
    public sealed class ProjectFoundationTests
    {
        [Test]
        public void CoreAssembly_DoesNotReferenceUnityEngine()
        {
            bool referencesUnityEngine = typeof(CoreAssemblyMarker)
                .Assembly
                .GetReferencedAssemblies()
                .Any(reference => reference.Name.StartsWith("UnityEngine"));

            Assert.That(referencesUnityEngine, Is.False);
        }

        [Test]
        public void ProjectSettings_MatchMobileFoundation()
        {
            Assert.That(PlayerSettings.companyName, Is.EqualTo(ProjectSettingsConfigurator.CompanyName));
            Assert.That(PlayerSettings.productName, Is.EqualTo(ProjectSettingsConfigurator.ProductName));
            Assert.That(
                PlayerSettings.GetApplicationIdentifier(NamedBuildTarget.Android),
                Is.EqualTo(ProjectSettingsConfigurator.BundleIdentifier));
            Assert.That(
                PlayerSettings.GetApplicationIdentifier(NamedBuildTarget.iOS),
                Is.EqualTo(ProjectSettingsConfigurator.BundleIdentifier));
            Assert.That(
                PlayerSettings.GetScriptingBackend(NamedBuildTarget.Android),
                Is.EqualTo(ScriptingImplementation.IL2CPP));
            Assert.That(PlayerSettings.Android.targetArchitectures, Is.EqualTo(AndroidArchitecture.ARM64));
            Assert.That(PlayerSettings.Android.minSdkVersion, Is.EqualTo(AndroidSdkVersions.AndroidApiLevel25));
            Assert.That(
                PlayerSettings.iOS.targetOSVersionString,
                Is.EqualTo(ProjectSettingsConfigurator.IosMinimumVersion));
            Assert.That(PlayerSettings.iOS.targetDevice, Is.EqualTo(iOSTargetDevice.iPhoneAndiPad));
            Assert.That(PlayerSettings.defaultInterfaceOrientation, Is.EqualTo(UIOrientation.AutoRotation));
        }

        [Test]
        public void MobileBuildTargets_AreInstalled()
        {
            Assert.That(
                BuildPipeline.IsBuildTargetSupported(BuildTargetGroup.Android, BuildTarget.Android),
                Is.True,
                "Android Build Support is not installed.");
            Assert.That(
                BuildPipeline.IsBuildTargetSupported(BuildTargetGroup.iOS, BuildTarget.iOS),
                Is.True,
                "iOS Build Support is not installed.");
        }

        [Test]
        public void InputSystem_IsTheActiveInputBackend()
        {
            Object[] settingsAssets =
                AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/ProjectSettings.asset");

            Assert.That(settingsAssets, Is.Not.Empty);

            using var serializedSettings = new SerializedObject(settingsAssets[0]);
            SerializedProperty activeInputHandler =
                serializedSettings.FindProperty("activeInputHandler");

            Assert.That(activeInputHandler, Is.Not.Null);
            Assert.That(
                activeInputHandler.intValue,
                Is.EqualTo(ProjectSettingsConfigurator.ActiveInputHandlerInputSystemOnly));
        }
    }
}
