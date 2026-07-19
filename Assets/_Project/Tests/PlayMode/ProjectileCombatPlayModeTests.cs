#nullable enable

using System.Collections;
using System.Linq;
using NUnit.Framework;
using RandomTowerDefense.Core.Combat;
using RandomTowerDefense.Core.Towers;
using RandomTowerDefense.UnityAdapters.MonoBehaviours;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace RandomTowerDefense.Tests.PlayMode
{
    public sealed class ProjectileCombatPlayModeTests
    {
        [UnityTest]
        public IEnumerator ProjectileViewTracksCoreAndTargetLossRewardsOnce()
        {
            AsyncOperation load = SceneManager.LoadSceneAsync("Main");
            Assert.That(load, Is.Not.Null);
            yield return load;
            yield return null;

            GameSessionBehaviour session = Object.FindFirstObjectByType<GameSessionBehaviour>();
            Assert.That(session, Is.Not.Null);
            session.enabled = false;

            for (int summon = 0; summon < 3; summon++)
            {
                TowerSummonResult result = session.TrySummonTower();
                Assert.That(result.Succeeded, Is.True);
            }

            int firedCount = 0;
            for (int tick = 0; tick < 6000 && session.Session.ActiveProjectiles.Count == 0; tick++)
            {
                GameSessionTickResult result = session.AdvanceSession(0.01f);
                firedCount += result.FiredProjectiles.Count;
            }

            Assert.That(firedCount, Is.GreaterThan(0));
            Assert.That(session.Session.ActiveProjectiles, Is.Not.Empty);
            Assert.That(
                session.ProjectileBoard.ActiveViewCount,
                Is.EqualTo(session.Session.ActiveProjectiles.Count));

            ProjectileState tracked = session.Session.ActiveProjectiles[0];
            Assert.That(
                session.ProjectileBoard.GetProjectilePosition(tracked.Order),
                Is.EqualTo(new Vector3(tracked.Position.X, tracked.Position.Y, 0f)));

            int balanceBeforeKill = session.Session.Economy.Balance;
            int expectedReward = tracked.Target.Definition.KillReward;
            DamageResult forcedKill = tracked.Target.ApplyDamage(tracked.Target.CurrentHealth);
            Assert.That(forcedKill.Killed, Is.True);

            GameSessionTickResult cleanup = session.AdvanceSession(0f);

            Assert.That(tracked.Status, Is.EqualTo(ProjectileStatus.TargetLost));
            Assert.That(
                session.Session.ActiveProjectiles.Any(projectile => projectile.Order == tracked.Order),
                Is.False);
            Assert.That(
                session.ProjectileBoard.ActiveViewCount,
                Is.EqualTo(session.Session.ActiveProjectiles.Count));
            Assert.That(cleanup.DeadEnemyCleanup.KillReward, Is.EqualTo(expectedReward));
            Assert.That(session.Session.Economy.Balance, Is.EqualTo(balanceBeforeKill + expectedReward));

            int balanceAfterCleanup = session.Session.Economy.Balance;
            GameSessionTickResult repeatedCleanup = session.AdvanceSession(0f);
            Assert.That(repeatedCleanup.DeadEnemyCleanup.KillReward, Is.Zero);
            Assert.That(session.Session.Economy.Balance, Is.EqualTo(balanceAfterCleanup));
        }
    }
}
