#nullable enable

using System;
using RandomTowerDefense.UnityAdapters.MonoBehaviours;
using UnityEngine;
using UnityEngine.UI;

namespace RandomTowerDefense.Presentation.UI
{
    [DisallowMultipleComponent]
    public sealed class GameHudView : MonoBehaviour
    {
        [SerializeField]
        private GameSessionBehaviour? _session;

        private Text? _waveText;
        private Text? _healthText;
        private Text? _currencyText;
        private Text? _summonLabel;
        private Button? _summonButton;
        private SafeAreaFitter? _safeArea;
        private GameObject? _resultOverlay;
        private Text? _resultText;
        private Button? _restartButton;
        private int _displayedWaveNumber = int.MinValue;
        private int _displayedTotalWaveCount = int.MinValue;
        private int _displayedHealth = int.MinValue;
        private int _displayedCurrency = int.MinValue;
        private int _displayedSummonCost = int.MinValue;
        private int _displayedEmptySlotCount = int.MinValue;
        private bool _displayedCanSummon;
        private bool _displayedIsRunning;
        private bool _hasDisplayedSummonState;
        private bool _displayedResultIsRunning;
        private bool _displayedHasVictory;
        private bool _hasDisplayedResultState;

        public Text WaveText => Require(_waveText, "Wave text");

        public Text HealthText => Require(_healthText, "Health text");

        public Text CurrencyText => Require(_currencyText, "Currency text");

        public Text SummonLabel => Require(_summonLabel, "Summon label");

        public Button SummonButton => Require(_summonButton, "Summon button");

        public SafeAreaFitter SafeArea => Require(_safeArea, "Safe area fitter");

        public GameObject ResultOverlay => Require(_resultOverlay, "Result overlay");

        public Text ResultText => Require(_resultText, "Result text");

        public Button RestartButton => Require(_restartButton, "Restart button");

        private GameSessionBehaviour Session => Require(_session, "Game session");

        private void Awake()
        {
            if (_session == null)
            {
                throw new InvalidOperationException("A game session behaviour is required.");
            }

            BuildUi();
        }

        private void OnEnable()
        {
            if (_summonButton != null)
            {
                _summonButton.onClick.AddListener(OnSummonClicked);
            }

            if (_restartButton != null)
            {
                _restartButton.onClick.AddListener(OnRestartClicked);
            }
        }

        private void Start()
        {
            Refresh();
        }

        private void LateUpdate()
        {
            Refresh();
        }

        private void OnDisable()
        {
            if (_summonButton != null)
            {
                _summonButton.onClick.RemoveListener(OnSummonClicked);
            }

            if (_restartButton != null)
            {
                _restartButton.onClick.RemoveListener(OnRestartClicked);
            }
        }

        public void Refresh()
        {
            int waveNumber = Session.CurrentWaveNumber;
            int totalWaveCount = Session.TotalWaveCount;
            int health = Session.CurrentHealth;
            int currency = Session.Currency;
            int summonCost = Session.SummonCost;
            int emptySlotCount = Session.EmptyTowerSlotCount;
            bool canSummon = Session.CanSummon;
            bool isRunning = Session.IsRunning;
            bool hasVictory = Session.HasVictory;
            bool currencyChanged = currency != _displayedCurrency;

            if (waveNumber != _displayedWaveNumber || totalWaveCount != _displayedTotalWaveCount)
            {
                WaveText.text = $"Wave {waveNumber} / {totalWaveCount}";
                _displayedWaveNumber = waveNumber;
                _displayedTotalWaveCount = totalWaveCount;
            }

            if (health != _displayedHealth)
            {
                HealthText.text = $"HP {health}";
                _displayedHealth = health;
            }

            if (currencyChanged)
            {
                CurrencyText.text = $"Gold {currency}";
                _displayedCurrency = currency;
            }

            if (!_hasDisplayedSummonState ||
                canSummon != _displayedCanSummon ||
                isRunning != _displayedIsRunning ||
                currencyChanged ||
                summonCost != _displayedSummonCost ||
                emptySlotCount != _displayedEmptySlotCount)
            {
                SummonButton.interactable = canSummon;
                SummonLabel.text = canSummon
                    ? $"Summon\n{summonCost}"
                    : UnavailableSummonLabel(isRunning, currency, summonCost, emptySlotCount);
                _displayedCanSummon = canSummon;
                _displayedIsRunning = isRunning;
                _displayedSummonCost = summonCost;
                _displayedEmptySlotCount = emptySlotCount;
                _hasDisplayedSummonState = true;
            }

            if (!_hasDisplayedResultState ||
                isRunning != _displayedResultIsRunning ||
                hasVictory != _displayedHasVictory)
            {
                ResultOverlay.SetActive(!isRunning);
                if (!isRunning)
                {
                    ResultText.text = hasVictory ? "Victory" : "Defeat";
                }

                _displayedResultIsRunning = isRunning;
                _displayedHasVictory = hasVictory;
                _hasDisplayedResultState = true;
            }
        }

        private void OnSummonClicked()
        {
            Session.TrySummonFromPlayer();
            Refresh();
        }

        private void OnRestartClicked()
        {
            Session.RestartFromPlayer();
            Refresh();
        }

        private static string UnavailableSummonLabel(
            bool isRunning,
            int currency,
            int summonCost,
            int emptySlotCount)
        {
            if (!isRunning)
            {
                return "Battle Ended";
            }

            if (currency < summonCost)
            {
                return $"Need {summonCost} Gold";
            }

            if (emptySlotCount == 0)
            {
                return "Slots Full";
            }

            return "Summon Unavailable";
        }

        private void BuildUi()
        {
            var canvasObject = new GameObject(
                "HUD Canvas",
                typeof(RectTransform),
                typeof(Canvas),
                typeof(CanvasScaler),
                typeof(GraphicRaycaster));
            canvasObject.transform.SetParent(transform, worldPositionStays: false);

            Canvas canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 100;

            CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;

            RectTransform safeAreaRoot = CreateRect("Safe Area", canvasObject.transform);
            Stretch(safeAreaRoot);
            _safeArea = safeAreaRoot.gameObject.AddComponent<SafeAreaFitter>();

            RectTransform statusStrip = CreateRect("Status Strip", safeAreaRoot);
            statusStrip.anchorMin = new Vector2(0f, 1f);
            statusStrip.anchorMax = Vector2.one;
            statusStrip.pivot = new Vector2(0.5f, 1f);
            statusStrip.offsetMin = new Vector2(20f, -100f);
            statusStrip.offsetMax = new Vector2(-20f, -20f);
            Image statusBackground = statusStrip.gameObject.AddComponent<Image>();
            statusBackground.color = new Color(0.055f, 0.075f, 0.11f, 0.88f);

            _waveText = CreateText("Wave", statusStrip, new Vector2(0f, 0f), new Vector2(0.34f, 1f));
            _healthText = CreateText("Health", statusStrip, new Vector2(0.34f, 0f), new Vector2(0.67f, 1f));
            _currencyText = CreateText("Currency", statusStrip, new Vector2(0.67f, 0f), Vector2.one);

            RectTransform buttonRect = CreateRect("Summon Button", safeAreaRoot);
            buttonRect.anchorMin = new Vector2(0.5f, 0f);
            buttonRect.anchorMax = new Vector2(0.5f, 0f);
            buttonRect.pivot = new Vector2(0.5f, 0f);
            buttonRect.sizeDelta = new Vector2(360f, 112f);
            buttonRect.anchoredPosition = new Vector2(0f, 40f);

            Image buttonImage = buttonRect.gameObject.AddComponent<Image>();
            buttonImage.color = new Color(0.12f, 0.65f, 0.62f, 1f);
            _summonButton = buttonRect.gameObject.AddComponent<Button>();
            _summonButton.targetGraphic = buttonImage;

            _summonLabel = CreateText("Label", buttonRect, Vector2.zero, Vector2.one);
            _summonLabel.fontSize = 30;

            BuildResultOverlay(safeAreaRoot);
        }

        private void BuildResultOverlay(Transform safeAreaRoot)
        {
            RectTransform overlayRect = CreateRect("Result Overlay", safeAreaRoot);
            Stretch(overlayRect);
            Image overlayImage = overlayRect.gameObject.AddComponent<Image>();
            overlayImage.color = new Color(0.025f, 0.035f, 0.055f, 0.94f);
            overlayImage.raycastTarget = true;
            _resultOverlay = overlayRect.gameObject;

            _resultText = CreateText(
                "Result",
                overlayRect,
                new Vector2(0.2f, 0.55f),
                new Vector2(0.8f, 0.82f));
            _resultText.fontSize = 64;
            _resultText.resizeTextMaxSize = 72;

            RectTransform restartRect = CreateRect("Restart Button", overlayRect);
            restartRect.anchorMin = new Vector2(0.5f, 0.35f);
            restartRect.anchorMax = new Vector2(0.5f, 0.35f);
            restartRect.pivot = new Vector2(0.5f, 0.5f);
            restartRect.sizeDelta = new Vector2(360f, 112f);

            Image restartImage = restartRect.gameObject.AddComponent<Image>();
            restartImage.color = new Color(0.95f, 0.62f, 0.16f, 1f);
            _restartButton = restartRect.gameObject.AddComponent<Button>();
            _restartButton.targetGraphic = restartImage;

            Text restartLabel = CreateText("Label", restartRect, Vector2.zero, Vector2.one);
            restartLabel.text = "Restart";
            restartLabel.fontSize = 30;

            _resultOverlay.SetActive(false);
        }

        private static RectTransform CreateRect(string name, Transform parent)
        {
            var gameObject = new GameObject(name, typeof(RectTransform));
            var rectTransform = (RectTransform)gameObject.transform;
            rectTransform.SetParent(parent, worldPositionStays: false);
            return rectTransform;
        }

        private static Text CreateText(
            string name,
            Transform parent,
            Vector2 anchorMin,
            Vector2 anchorMax)
        {
            RectTransform rectTransform = CreateRect(name, parent);
            rectTransform.anchorMin = anchorMin;
            rectTransform.anchorMax = anchorMax;
            rectTransform.offsetMin = new Vector2(8f, 4f);
            rectTransform.offsetMax = new Vector2(-8f, -4f);

            Text text = rectTransform.gameObject.AddComponent<Text>();
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.color = Color.white;
            text.alignment = TextAnchor.MiddleCenter;
            text.fontSize = 32;
            text.resizeTextForBestFit = true;
            text.resizeTextMinSize = 16;
            text.resizeTextMaxSize = 34;
            return text;
        }

        private static void Stretch(RectTransform rectTransform)
        {
            rectTransform.anchorMin = Vector2.zero;
            rectTransform.anchorMax = Vector2.one;
            rectTransform.offsetMin = Vector2.zero;
            rectTransform.offsetMax = Vector2.zero;
        }

        private static T Require<T>(T? value, string label)
            where T : class
        {
            return value ?? throw new InvalidOperationException($"{label} is not available.");
        }

#if UNITY_EDITOR
        public void ConfigureForEditor(GameSessionBehaviour session)
        {
            _session = session;
        }
#endif
    }
}
