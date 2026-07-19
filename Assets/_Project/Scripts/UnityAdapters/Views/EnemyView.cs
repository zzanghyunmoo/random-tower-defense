#nullable enable

using System;
using RandomTowerDefense.Core.Enemies;
using UnityEngine;

namespace RandomTowerDefense.UnityAdapters.Views
{
    [DisallowMultipleComponent]
    public sealed class EnemyView : MonoBehaviour
    {
        private long _spawnOrder;
        private SpriteRenderer? _spriteRenderer;

        public void Initialize(EnemyState enemy, Sprite sprite)
        {
            if (enemy == null)
            {
                throw new ArgumentNullException(nameof(enemy));
            }

            if (sprite == null)
            {
                throw new ArgumentNullException(nameof(sprite));
            }

            _spawnOrder = enemy.SpawnOrder;
            _spriteRenderer = gameObject.AddComponent<SpriteRenderer>();
            _spriteRenderer.sprite = sprite;
            _spriteRenderer.color = ColorFor(enemy.Definition.Id);
            _spriteRenderer.sortingOrder = 10;

            float diameter = 0.45f + Mathf.Clamp(Mathf.Sqrt(enemy.Definition.MaxHealth) * 0.035f, 0f, 0.3f);
            transform.localScale = new Vector3(diameter, diameter, 1f);
            Render(enemy);
        }

        public void Render(EnemyState enemy)
        {
            if (enemy == null)
            {
                throw new ArgumentNullException(nameof(enemy));
            }

            if (enemy.SpawnOrder != _spawnOrder)
            {
                throw new InvalidOperationException("An enemy view cannot be rebound to another spawn order.");
            }

            transform.position = new Vector3(enemy.Position.X, enemy.Position.Y, 0f);
        }

        private static Color ColorFor(string id)
        {
            uint hash = 2166136261;
            foreach (char character in id)
            {
                hash = (hash ^ character) * 16777619;
            }

            float hue = (hash % 360) / 360f;
            return Color.HSVToRGB(hue, 0.55f, 0.92f);
        }
    }
}
