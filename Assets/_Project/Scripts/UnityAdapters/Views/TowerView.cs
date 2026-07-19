#nullable enable

using System;
using RandomTowerDefense.Core.Towers;
using UnityEngine;

namespace RandomTowerDefense.UnityAdapters.Views
{
    [DisallowMultipleComponent]
    public sealed class TowerView : MonoBehaviour
    {
        private long _placementOrder;

        public void Initialize(TowerState tower, Sprite sprite)
        {
            if (tower == null)
            {
                throw new ArgumentNullException(nameof(tower));
            }

            if (sprite == null)
            {
                throw new ArgumentNullException(nameof(sprite));
            }

            _placementOrder = tower.PlacementOrder;
            SpriteRenderer renderer = gameObject.AddComponent<SpriteRenderer>();
            renderer.sprite = sprite;
            renderer.color = RuntimeViewPalette.ColorForId(tower.Definition.Id);
            renderer.sortingOrder = 5;
            transform.localScale = new Vector3(0.62f, 0.62f, 1f);
            transform.localRotation = Quaternion.Euler(0f, 0f, 45f);
            Render(tower);
        }

        public void Render(TowerState tower)
        {
            if (tower == null)
            {
                throw new ArgumentNullException(nameof(tower));
            }

            if (tower.PlacementOrder != _placementOrder)
            {
                throw new InvalidOperationException("A tower view cannot be rebound to another placement order.");
            }

            transform.position = new Vector3(tower.Position.X, tower.Position.Y, 0f);
        }
    }
}
