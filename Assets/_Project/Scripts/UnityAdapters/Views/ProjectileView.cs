#nullable enable

using System;
using RandomTowerDefense.Core.Combat;
using UnityEngine;

namespace RandomTowerDefense.UnityAdapters.Views
{
    [DisallowMultipleComponent]
    public sealed class ProjectileView : MonoBehaviour
    {
        private long _order;

        public void Initialize(ProjectileState projectile, Sprite sprite)
        {
            if (projectile == null)
            {
                throw new ArgumentNullException(nameof(projectile));
            }

            if (sprite == null)
            {
                throw new ArgumentNullException(nameof(sprite));
            }

            _order = projectile.Order;
            SpriteRenderer renderer = gameObject.AddComponent<SpriteRenderer>();
            renderer.sprite = sprite;
            renderer.color = new Color(1f, 0.86f, 0.3f, 1f);
            renderer.sortingOrder = 8;
            transform.localScale = new Vector3(0.18f, 0.18f, 1f);
            Render(projectile);
        }

        public void Render(ProjectileState projectile)
        {
            if (projectile == null)
            {
                throw new ArgumentNullException(nameof(projectile));
            }

            if (projectile.Order != _order)
            {
                throw new InvalidOperationException("A projectile view cannot be rebound to another order.");
            }

            transform.position = new Vector3(projectile.Position.X, projectile.Position.Y, 0f);
        }
    }
}
