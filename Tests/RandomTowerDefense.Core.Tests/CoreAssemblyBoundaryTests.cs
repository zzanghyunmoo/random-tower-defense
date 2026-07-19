using System;
using System.IO;
using System.Linq;
using NUnit.Framework;
using RandomTowerDefense.Core;

namespace RandomTowerDefense.Core.Tests
{
    public sealed class CoreAssemblyBoundaryTests
    {
        [Test]
        public void CoreAssembly_DoesNotReferenceUnityEngine()
        {
            bool referencesUnityEngine = typeof(CoreAssemblyMarker)
                .Assembly
                .GetReferencedAssemblies()
                .Any(reference => reference.Name?.StartsWith("UnityEngine", StringComparison.Ordinal) == true);

            Assert.That(referencesUnityEngine, Is.False);
        }

        [Test]
        public void UnityProject_UsesPinnedEditorVersion()
        {
            string repositoryRoot = FindRepositoryRoot();
            string versionFile = Path.Combine(repositoryRoot, "ProjectSettings", "ProjectVersion.txt");

            Assert.That(File.ReadAllText(versionFile), Does.Contain("6000.3.20f1"));
        }

        private static string FindRepositoryRoot()
        {
            DirectoryInfo? directory = new DirectoryInfo(AppContext.BaseDirectory);

            while (directory != null)
            {
                string versionFile = Path.Combine(directory.FullName, "ProjectSettings", "ProjectVersion.txt");
                if (File.Exists(versionFile))
                {
                    return directory.FullName;
                }

                directory = directory.Parent;
            }

            throw new DirectoryNotFoundException("Could not locate the Unity project root.");
        }
    }
}
