#nullable enable

using System.Collections;
using NUnit.Framework;
using RandomTowerDefense.Presentation.UI;
using RandomTowerDefense.UnityAdapters.MonoBehaviours;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace RandomTowerDefense.Tests.PlayMode
{
    public sealed class MobileHudPlayModeTests : InputTestFixture
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

            AssertSafeArea(hud.SafeArea, new Rect(96f, 54f, 1728f, 972f), 1920, 1080);
            AssertSafeArea(hud.SafeArea, new Rect(132f, 0f, 2076f, 1080f), 2340, 1080);
            AssertSafeArea(hud.SafeArea, new Rect(0f, 48f, 2048f, 1440f), 2048, 1536);
        }

        [UnityTest]
        public IEnumerator MouseAndTouchUseTheSameSingleTapSummonBoundary()
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

            Canvas.ForceUpdateCanvases();
            RectTransform buttonRect = hud.SummonButton.GetComponent<RectTransform>();
            Vector2 buttonCenter = RectTransformUtility.WorldToScreenPoint(
                null,
                buttonRect.TransformPoint(buttonRect.rect.center));

            Mouse mouse = InputSystem.AddDevice<Mouse>();
            Move(mouse.position, buttonCenter);
            yield return null;
            Press(mouse.leftButton);
            yield return null;
            Release(mouse.leftButton);
            yield return null;

            AssertSingleSummon(session);

            InputSystem.RemoveDevice(mouse);
            session.RestartFromPlayer();
            hud.Refresh();
            Assert.That(session.Currency, Is.EqualTo(30));
            Assert.That(session.TowerBoard.TowerViewCount, Is.Zero);

            Touchscreen touchscreen = InputSystem.AddDevice<Touchscreen>();
            BeginTouch(touchId: 1, position: buttonCenter, screen: touchscreen);
            yield return null;
            EndTouch(touchId: 1, position: buttonCenter, screen: touchscreen);
            yield return null;

            AssertSingleSummon(session);
        }

        private static void AssertSafeArea(
            SafeAreaFitter safeArea,
            Rect safeAreaPixels,
            int screenWidth,
            int screenHeight)
        {
            safeArea.Apply(safeAreaPixels, screenWidth, screenHeight);

            Vector2 expectedMin = new(
                safeAreaPixels.xMin / screenWidth,
                safeAreaPixels.yMin / screenHeight);
            Vector2 expectedMax = new(
                safeAreaPixels.xMax / screenWidth,
                safeAreaPixels.yMax / screenHeight);

            Assert.That(safeArea.Target.anchorMin.x, Is.EqualTo(expectedMin.x).Within(0.0001f));
            Assert.That(safeArea.Target.anchorMin.y, Is.EqualTo(expectedMin.y).Within(0.0001f));
            Assert.That(safeArea.Target.anchorMax.x, Is.EqualTo(expectedMax.x).Within(0.0001f));
            Assert.That(safeArea.Target.anchorMax.y, Is.EqualTo(expectedMax.y).Within(0.0001f));
            Assert.That(safeArea.Target.offsetMin, Is.EqualTo(Vector2.zero));
            Assert.That(safeArea.Target.offsetMax, Is.EqualTo(Vector2.zero));
        }

        private static void AssertSingleSummon(GameSessionBehaviour session)
        {
            Assert.That(session.Currency, Is.EqualTo(20));
            Assert.That(session.TowerBoard.TowerViewCount, Is.EqualTo(1));
        }
    }
}
