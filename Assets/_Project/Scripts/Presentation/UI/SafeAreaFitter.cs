#nullable enable

using System;
using UnityEngine;

namespace RandomTowerDefense.Presentation.UI
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(RectTransform))]
    public sealed class SafeAreaFitter : MonoBehaviour
    {
        private RectTransform? _rectTransform;
        private Rect _lastSafeArea = new Rect(-1f, -1f, -1f, -1f);
        private int _lastScreenWidth = -1;
        private int _lastScreenHeight = -1;

        public RectTransform Target => _rectTransform
            ?? throw new InvalidOperationException("The safe area fitter has not been initialized.");

        private void Awake()
        {
            _rectTransform = GetComponent<RectTransform>();
            ApplyCurrentSafeArea();
        }

        private void Update()
        {
            if (Screen.safeArea != _lastSafeArea ||
                Screen.width != _lastScreenWidth ||
                Screen.height != _lastScreenHeight)
            {
                ApplyCurrentSafeArea();
            }
        }

        public void Apply(Rect safeArea, int screenWidth, int screenHeight)
        {
            if (screenWidth <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(screenWidth));
            }

            if (screenHeight <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(screenHeight));
            }

            Vector2 anchorMin = new Vector2(
                Mathf.Clamp01(safeArea.xMin / screenWidth),
                Mathf.Clamp01(safeArea.yMin / screenHeight));
            Vector2 anchorMax = new Vector2(
                Mathf.Clamp01(safeArea.xMax / screenWidth),
                Mathf.Clamp01(safeArea.yMax / screenHeight));
            if (anchorMax.x < anchorMin.x || anchorMax.y < anchorMin.y)
            {
                throw new ArgumentException("Safe area bounds must not be inverted.", nameof(safeArea));
            }

            Target.anchorMin = anchorMin;
            Target.anchorMax = anchorMax;
            Target.offsetMin = Vector2.zero;
            Target.offsetMax = Vector2.zero;

            _lastSafeArea = safeArea;
            _lastScreenWidth = screenWidth;
            _lastScreenHeight = screenHeight;
        }

        private void ApplyCurrentSafeArea()
        {
            Apply(Screen.safeArea, Screen.width, Screen.height);
        }
    }
}
