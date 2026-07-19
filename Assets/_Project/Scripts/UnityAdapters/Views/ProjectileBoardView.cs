#nullable enable

using System;
using System.Collections.Generic;
using RandomTowerDefense.Core.Combat;
using UnityEngine;

namespace RandomTowerDefense.UnityAdapters.Views
{
    [DisallowMultipleComponent]
    public sealed class ProjectileBoardView : MonoBehaviour
    {
        private readonly Dictionary<long, ProjectileView> _views = new Dictionary<long, ProjectileView>();
        private readonly HashSet<long> _activeOrders = new HashSet<long>();
        private readonly List<long> _removedOrders = new List<long>();

        private Sprite? _sprite;

        public int ActiveViewCount => _views.Count;

        public void Initialize()
        {
            Clear();
            _sprite = CreateSprite();
        }

        public void Render(IReadOnlyList<ProjectileState> projectiles)
        {
            if (projectiles == null)
            {
                throw new ArgumentNullException(nameof(projectiles));
            }

            if (_sprite == null)
            {
                throw new InvalidOperationException("The projectile board has not been initialized.");
            }

            _activeOrders.Clear();
            foreach (ProjectileState projectile in projectiles)
            {
                _activeOrders.Add(projectile.Order);
                if (!_views.TryGetValue(projectile.Order, out ProjectileView view))
                {
                    view = CreateView(projectile, _sprite);
                    _views.Add(projectile.Order, view);
                }

                view.Render(projectile);
            }

            _removedOrders.Clear();
            foreach (long order in _views.Keys)
            {
                if (!_activeOrders.Contains(order))
                {
                    _removedOrders.Add(order);
                }
            }

            foreach (long order in _removedOrders)
            {
                ProjectileView view = _views[order];
                _views.Remove(order);
                DestroyObject(view.gameObject);
            }
        }

        public Vector3 GetProjectilePosition(long order)
        {
            if (!_views.TryGetValue(order, out ProjectileView view))
            {
                throw new ArgumentOutOfRangeException(nameof(order), order, "Projectile view was not found.");
            }

            return view.transform.position;
        }

        public void Clear()
        {
            foreach (ProjectileView view in _views.Values)
            {
                DestroyObject(view.gameObject);
            }

            _views.Clear();
            _activeOrders.Clear();
            _removedOrders.Clear();

            if (_sprite != null)
            {
                DestroyObject(_sprite);
                _sprite = null;
            }
        }

        private void OnDestroy()
        {
            Clear();
        }

        private ProjectileView CreateView(ProjectileState projectile, Sprite sprite)
        {
            var projectileObject = new GameObject($"Projectile_{projectile.Order:D3}");
            projectileObject.transform.SetParent(transform, worldPositionStays: false);
            ProjectileView view = projectileObject.AddComponent<ProjectileView>();
            view.Initialize(projectile, sprite);
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
            sprite.name = "Runtime Projectile Sprite";
            return sprite;
        }

        private static void DestroyObject(UnityEngine.Object target)
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
