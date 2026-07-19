#nullable enable

using System.Collections;
using NUnit.Framework;
using RandomTowerDefense.Core.Combat;
using RandomTowerDefense.Core.Enemies;
using RandomTowerDefense.UnityAdapters.MonoBehaviours;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace RandomTowerDefense.Tests.PlayMode
{
    public sealed class WaveBoardPlayModeTests
    {
        [UnityTest]
        public IEnumerator MainSceneSpawnsMovesAndRemovesEndpointEnemies()
        {
            AsyncOperation load = SceneManager.LoadSceneAsync("Main");
            Assert.That(load, Is.Not.Null);
            yield return load;
            yield return null;

            GameSessionBehaviour sessionBehaviour =
                Object.FindFirstObjectByType<GameSessionBehaviour>();
            Assert.That(sessionBehaviour, Is.Not.Null);
            sessionBehaviour.enabled = false;

            Assert.That(sessionBehaviour.Session.ActiveEnemies, Has.Count.EqualTo(1));
            Assert.That(sessionBehaviour.Board.ActiveViewCount, Is.EqualTo(1));

            EnemyState firstEnemy = sessionBehaviour.Session.ActiveEnemies[0];
            Vector3 initialPosition = sessionBehaviour.Board.GetEnemyPosition(firstEnemy.SpawnOrder);
            sessionBehaviour.AdvanceSession(1f);
            Vector3 movedPosition = sessionBehaviour.Board.GetEnemyPosition(firstEnemy.SpawnOrder);

            Assert.That(Vector3.Distance(movedPosition, initialPosition), Is.GreaterThan(0.1f));

            int startingHealth = sessionBehaviour.Session.CurrentHealth;
            for (int tick = 0; tick < 30 && sessionBehaviour.Session.CurrentHealth == startingHealth; tick++)
            {
                sessionBehaviour.AdvanceSession(1f);
            }

            Assert.That(sessionBehaviour.Session.CurrentHealth, Is.LessThan(startingHealth));
            Assert.That(
                sessionBehaviour.Board.ActiveViewCount,
                Is.EqualTo(sessionBehaviour.Session.ActiveEnemies.Count));
            Assert.That(sessionBehaviour.Session.Status, Is.EqualTo(GameSessionStatus.Running));
        }
    }
}
