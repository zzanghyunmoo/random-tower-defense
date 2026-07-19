#nullable enable

using System.Collections;
using NUnit.Framework;
using RandomTowerDefense.Presentation.UI;
using RandomTowerDefense.UnityAdapters.MonoBehaviours;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace RandomTowerDefense.Tests.PlayMode
{
    public sealed class MobileHudPlayModeTests
    {
        [UnityTest]
        public IEnumerator HudDisplaysStateUsesOneListenerAndFitsSafeArea()
        {
            AsyncOperation load = SceneManager.LoadSceneAsync("Main");
            Assert.That(load, Is.Not.Null);
            yield return load;
            yield return null;

            GameSessionBehaviour session = Object.FindFirstObjectByType<GameSessionBehaviour>();
            GameHudView hud = Object.FindFirstObjectByType<GameHudView>();
            Assert.That(session, Is.Not.Null);
            Assert.That(hud, Is.Not.Null);
            session.enabled = false;

            Assert.That(hud.WaveText.text, Is.EqualTo("Wave 1 / 3"));
            Assert.That(hud.HealthText.text, Is.EqualTo("HP 10"));
            Assert.That(hud.CurrencyText.text, Is.EqualTo("Gold 30"));
            Assert.That(hud.SummonButton.interactable, Is.True);
            Assert.That(hud.SummonButton.GetComponent<RectTransform>().rect.height, Is.GreaterThanOrEqualTo(48f));

            hud.enabled = false;
            hud.enabled = true;
            hud.enabled = false;
            hud.enabled = true;
            hud.SummonButton.onClick.Invoke();

            Assert.That(session.Currency, Is.EqualTo(20));
            Assert.That(session.TowerBoard.TowerViewCount, Is.EqualTo(1));
            Assert.That(hud.CurrencyText.text, Is.EqualTo("Gold 20"));

            hud.SummonButton.onClick.Invoke();
            hud.SummonButton.onClick.Invoke();
            Assert.That(session.Currency, Is.Zero);
            Assert.That(session.TowerBoard.TowerViewCount, Is.EqualTo(3));
            Assert.That(hud.SummonButton.interactable, Is.False);
            Assert.That(hud.SummonLabel.text, Is.EqualTo("Need 10 Gold"));

            hud.SafeArea.Apply(new Rect(96f, 54f, 1728f, 972f), screenWidth: 1920, screenHeight: 1080);
            Assert.That(hud.SafeArea.Target.anchorMin.x, Is.EqualTo(0.05f).Within(0.0001f));
            Assert.That(hud.SafeArea.Target.anchorMin.y, Is.EqualTo(0.05f).Within(0.0001f));
            Assert.That(hud.SafeArea.Target.anchorMax.x, Is.EqualTo(0.95f).Within(0.0001f));
            Assert.That(hud.SafeArea.Target.anchorMax.y, Is.EqualTo(0.95f).Within(0.0001f));
        }
    }
}
