#nullable enable

using System.Collections;
using NUnit.Framework;
using RandomTowerDefense.Presentation.UI;
using RandomTowerDefense.UnityAdapters.MonoBehaviours;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace RandomTowerDefense.Tests.PlayMode
{
    public sealed class SessionResultPlayModeTests
    {
        [UnityTest]
        public IEnumerator ResultsBlockInputAndRestartRestoresTheInitialSession()
        {
            yield return LoadMainScene();

            GameSessionBehaviour session = FindSession();
            GameHudView hud = FindHud();
            session.enabled = false;

            for (int tick = 0; tick < 2400 && session.IsRunning; tick++)
            {
                while (session.CanSummon)
                {
                    Assert.That(session.TrySummonFromPlayer(), Is.True);
                }

                session.AdvanceSession(0.1f);
            }

            hud.Refresh();
            Assert.That(session.IsRunning, Is.False, "The default stage did not finish within 240 seconds.");
            Assert.That(session.HasVictory, Is.True);
            AssertResult(hud, "Victory");
            Assert.That(hud.RestartButton.GetComponent<RectTransform>().rect.height, Is.GreaterThanOrEqualTo(48f));

            hud.RestartButton.onClick.Invoke();

            Assert.That(session.IsRunning, Is.True);
            Assert.That(session.CurrentHealth, Is.EqualTo(10));
            Assert.That(session.Currency, Is.EqualTo(30));
            Assert.That(session.TowerBoard.TowerViewCount, Is.Zero);
            Assert.That(session.ProjectileBoard.ActiveViewCount, Is.Zero);
            Assert.That(session.Board.ActiveViewCount, Is.EqualTo(1));
            Assert.That(hud.ResultOverlay.activeSelf, Is.False);
            Assert.That(hud.WaveText.text, Is.EqualTo("Wave 1 / 3"));
            Assert.That(hud.HealthText.text, Is.EqualTo("HP 10"));
            Assert.That(hud.CurrencyText.text, Is.EqualTo("Gold 30"));

            for (int tick = 0; tick < 2400 && session.IsRunning; tick++)
            {
                session.AdvanceSession(0.1f);
            }

            hud.Refresh();
            Assert.That(session.IsRunning, Is.False);
            Assert.That(session.HasVictory, Is.False);
            AssertResult(hud, "Defeat");
        }

        [UnityTest]
        public IEnumerator ApplicationPauseStopsTicksAndResumeContinuesOnce()
        {
            yield return LoadMainScene();

            GameSessionBehaviour session = FindSession();
            Assert.That(session.Session.ActiveEnemies, Is.Not.Empty);

            session.SendMessage("OnApplicationPause", true);
            Assert.That(session.IsApplicationPaused, Is.True);
            float pausedDistance = session.Session.ActiveEnemies[0].DistanceTravelled;

            yield return null;
            yield return null;

            Assert.That(session.Session.ActiveEnemies[0].DistanceTravelled, Is.EqualTo(pausedDistance));

            session.SendMessage("OnApplicationPause", false);
            Assert.That(session.IsApplicationPaused, Is.False);
            yield return null;

            float resumedDeltaSeconds = Time.deltaTime;
            Assert.That(resumedDeltaSeconds, Is.GreaterThan(0f));
            float expectedDistance = pausedDistance
                + (session.Session.ActiveEnemies[0].Definition.MoveSpeed * resumedDeltaSeconds);
            Assert.That(
                session.Session.ActiveEnemies[0].DistanceTravelled,
                Is.EqualTo(expectedDistance).Within(0.0001f));
        }

        private static IEnumerator LoadMainScene()
        {
            AsyncOperation load = SceneManager.LoadSceneAsync("Main");
            Assert.That(load, Is.Not.Null);
            yield return load;
            yield return null;
        }

        private static GameSessionBehaviour FindSession()
        {
            GameSessionBehaviour session = Object.FindFirstObjectByType<GameSessionBehaviour>();
            Assert.That(session, Is.Not.Null);
            return session;
        }

        private static GameHudView FindHud()
        {
            GameHudView hud = Object.FindFirstObjectByType<GameHudView>();
            Assert.That(hud, Is.Not.Null);
            return hud;
        }

        private static void AssertResult(GameHudView hud, string expectedText)
        {
            Assert.That(hud.ResultOverlay.activeSelf, Is.True);
            Assert.That(hud.ResultOverlay.GetComponent<Image>().raycastTarget, Is.True);
            Assert.That(hud.ResultText.text, Is.EqualTo(expectedText));
            Assert.That(hud.SummonButton.interactable, Is.False);
        }
    }
}
