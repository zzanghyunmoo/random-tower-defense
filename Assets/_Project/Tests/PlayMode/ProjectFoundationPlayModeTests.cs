using System.Collections;
using NUnit.Framework;
using RandomTowerDefense.Core;
using UnityEngine.TestTools;

namespace RandomTowerDefense.Tests.PlayMode
{
    public sealed class ProjectFoundationPlayModeTests
    {
        [UnityTest]
        public IEnumerator CoreAssembly_IsAvailableAtRuntime()
        {
            Assert.That(typeof(CoreAssemblyMarker).Assembly, Is.Not.Null);
            yield return null;
        }
    }
}
