#nullable enable

using System.Collections;
using NUnit.Framework;
using RandomTowerDefense.Core.Towers;
using RandomTowerDefense.UnityAdapters.MonoBehaviours;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace RandomTowerDefense.Tests.PlayMode
{
    public sealed class TowerSummonPlayModeTests
    {
        [UnityTest]
        public IEnumerator SummonSpendsOnceCreatesOneViewAndRepeatsForTheSameSeed()
        {
            yield return LoadMainScene();
            GameSessionBehaviour firstSession = FindSessionAndStopAutomaticTicks();

            Assert.That(firstSession.TowerBoard.SlotViewCount, Is.EqualTo(8));
            Assert.That(firstSession.TowerBoard.TowerViewCount, Is.Zero);
            Assert.That(firstSession.Session.Economy.Balance, Is.EqualTo(30));

            TowerSummonResult first = firstSession.TrySummonTower();
            AssertSuccessfulSummon(firstSession, first, expectedBalance: 20, expectedTowerCount: 1);

            string firstSlotId = first.SlotId!;
            string firstTowerId = first.Tower!.Definition.Id;

            TowerSummonResult second = firstSession.TrySummonTower();
            AssertSuccessfulSummon(firstSession, second, expectedBalance: 10, expectedTowerCount: 2);
            TowerSummonResult third = firstSession.TrySummonTower();
            AssertSuccessfulSummon(firstSession, third, expectedBalance: 0, expectedTowerCount: 3);

            TowerSummonResult rejected = firstSession.TrySummonTower();
            Assert.That(rejected.Succeeded, Is.False);
            Assert.That(rejected.Failure, Is.EqualTo(TowerSummonFailure.InsufficientCurrency));
            Assert.That(firstSession.Session.Economy.Balance, Is.Zero);
            Assert.That(firstSession.TowerBoard.TowerViewCount, Is.EqualTo(3));

            yield return LoadMainScene();
            GameSessionBehaviour repeatedSession = FindSessionAndStopAutomaticTicks();
            TowerSummonResult repeated = repeatedSession.TrySummonTower();

            Assert.That(repeated.SlotId, Is.EqualTo(firstSlotId));
            Assert.That(repeated.Tower!.Definition.Id, Is.EqualTo(firstTowerId));
        }

        private static IEnumerator LoadMainScene()
        {
            AsyncOperation load = SceneManager.LoadSceneAsync("Main");
            Assert.That(load, Is.Not.Null);
            yield return load;
            yield return null;
        }

        private static GameSessionBehaviour FindSessionAndStopAutomaticTicks()
        {
            GameSessionBehaviour session = Object.FindFirstObjectByType<GameSessionBehaviour>();
            Assert.That(session, Is.Not.Null);
            session.enabled = false;
            return session;
        }

        private static void AssertSuccessfulSummon(
            GameSessionBehaviour session,
            TowerSummonResult result,
            int expectedBalance,
            int expectedTowerCount)
        {
            Assert.That(result.Succeeded, Is.True);
            Assert.That(result.CostSpent, Is.EqualTo(10));
            Assert.That(session.Session.Economy.Balance, Is.EqualTo(expectedBalance));
            Assert.That(session.TowerBoard.TowerViewCount, Is.EqualTo(expectedTowerCount));
            Assert.That(
                session.TowerBoard.GetTowerPosition(result.Tower!.PlacementOrder),
                Is.EqualTo(new Vector3(result.Tower.Position.X, result.Tower.Position.Y, 0f)));
        }
    }
}
