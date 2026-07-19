#nullable enable

using System;
using System.Collections.Generic;
using RandomTowerDefense.Core.Towers;
using RandomTowerDefense.Data.Definitions;
using UnityEngine;

namespace RandomTowerDefense.UnityAdapters.Views
{
    [DisallowMultipleComponent]
    public sealed class TowerBoardView : MonoBehaviour
    {
        private readonly List<GameObject> _slotViews = new List<GameObject>();
        private readonly Dictionary<long, TowerView> _towerViews = new Dictionary<long, TowerView>();
        private readonly HashSet<long> _activePlacementOrders = new HashSet<long>();
        private readonly List<long> _removedPlacementOrders = new List<long>();

        private Sprite? _sprite;

        public int SlotViewCount => _slotViews.Count;

        public int TowerViewCount => _towerViews.Count;

        public void Initialize(StageDefinitionAsset stage)
        {
            if (stage == null)
            {
                throw new ArgumentNullException(nameof(stage));
            }

            Clear();
            _sprite = CreateSprite();
            for (int index = 0; index < stage.TowerSlots.Count; index++)
            {
                TowerSlotData slot = stage.TowerSlots[index];
                var slotObject = new GameObject($"Tower Slot {slot.Id}");
                slotObject.transform.SetParent(transform, worldPositionStays: false);
                slotObject.transform.position = new Vector3(slot.Position.x, slot.Position.y, 0f);
                slotObject.transform.localScale = new Vector3(0.32f, 0.32f, 1f);

                SpriteRenderer renderer = slotObject.AddComponent<SpriteRenderer>();
                renderer.sprite = _sprite;
                renderer.color = new Color(0.35f, 0.43f, 0.53f, 0.55f);
                renderer.sortingOrder = 2;
                _slotViews.Add(slotObject);
            }
        }

        public void Render(IReadOnlyList<TowerState> towers)
        {
            if (towers == null)
            {
                throw new ArgumentNullException(nameof(towers));
            }

            if (_sprite == null)
            {
                throw new InvalidOperationException("The tower board has not been initialized.");
            }

            _activePlacementOrders.Clear();
            foreach (TowerState tower in towers)
            {
                _activePlacementOrders.Add(tower.PlacementOrder);
                if (!_towerViews.TryGetValue(tower.PlacementOrder, out TowerView view))
                {
                    view = CreateTowerView(tower, _sprite);
                    _towerViews.Add(tower.PlacementOrder, view);
                }

                view.Render(tower);
            }

            _removedPlacementOrders.Clear();
            foreach (long placementOrder in _towerViews.Keys)
            {
                if (!_activePlacementOrders.Contains(placementOrder))
                {
                    _removedPlacementOrders.Add(placementOrder);
                }
            }

            foreach (long placementOrder in _removedPlacementOrders)
            {
                TowerView view = _towerViews[placementOrder];
                _towerViews.Remove(placementOrder);
                DestroyRuntimeObject(view.gameObject);
            }
        }

        public Vector3 GetTowerPosition(long placementOrder)
        {
            if (!_towerViews.TryGetValue(placementOrder, out TowerView view))
            {
                throw new ArgumentOutOfRangeException(
                    nameof(placementOrder),
                    placementOrder,
                    "Tower view was not found.");
            }

            return view.transform.position;
        }

        public void Clear()
        {
            foreach (TowerView view in _towerViews.Values)
            {
                DestroyRuntimeObject(view.gameObject);
            }

            foreach (GameObject slotView in _slotViews)
            {
                DestroyRuntimeObject(slotView);
            }

            _towerViews.Clear();
            _slotViews.Clear();
            _activePlacementOrders.Clear();
            _removedPlacementOrders.Clear();

            if (_sprite != null)
            {
                DestroyRuntimeObject(_sprite);
                _sprite = null;
            }
        }

        private void OnDestroy()
        {
            Clear();
        }

        private TowerView CreateTowerView(TowerState tower, Sprite sprite)
        {
            var towerObject = new GameObject($"Tower_{tower.PlacementOrder:D2}");
            towerObject.transform.SetParent(transform, worldPositionStays: false);
            TowerView view = towerObject.AddComponent<TowerView>();
            view.Initialize(tower, sprite);
            return view;
        }

        private static Sprite CreateSprite()
        {
            Texture2D texture = Texture2D.whiteTexture;
            Sprite sprite = Sprite.Create(
                texture,
                new Rect(0f, 0f, texture.width, texture.height),
                new Vector2(0.5f, 0.5f),
                texture.width);
            sprite.name = "Runtime Tower Sprite";
            return sprite;
        }

        private static void DestroyRuntimeObject(UnityEngine.Object target)
        {
            if (UnityEngine.Application.isPlaying)
            {
                Destroy(target);
            }
            else
            {
                DestroyImmediate(target);
            }
        }
    }
}
