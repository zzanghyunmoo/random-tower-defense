#nullable enable

using System;
using System.Collections.Generic;
using RandomTowerDefense.Core.Enemies;
using RandomTowerDefense.Data.Definitions;
using UnityEngine;

namespace RandomTowerDefense.UnityAdapters.Views
{
    [DisallowMultipleComponent]
    public sealed class EnemyBoardView : MonoBehaviour
    {
        private readonly Dictionary<long, EnemyView> _views = new Dictionary<long, EnemyView>();
        private readonly HashSet<long> _activeSpawnOrders = new HashSet<long>();
        private readonly List<long> _removedSpawnOrders = new List<long>();

        private LineRenderer? _pathRenderer;
        private Material? _pathMaterial;
        private Sprite? _enemySprite;

        public int ActiveViewCount => _views.Count;

        public void Initialize(StageDefinitionAsset stage)
        {
            if (stage == null)
            {
                throw new ArgumentNullException(nameof(stage));
            }

            Clear();
            _enemySprite = CreateEnemySprite();
            ConfigurePath(stage.PathPoints);
        }

        public void Render(IReadOnlyList<EnemyState> enemies)
        {
            if (enemies == null)
            {
                throw new ArgumentNullException(nameof(enemies));
            }

            if (_enemySprite == null)
            {
                throw new InvalidOperationException("The enemy board has not been initialized.");
            }

            _activeSpawnOrders.Clear();
            foreach (EnemyState enemy in enemies)
            {
                _activeSpawnOrders.Add(enemy.SpawnOrder);
                if (!_views.TryGetValue(enemy.SpawnOrder, out EnemyView view))
                {
                    view = CreateEnemyView(enemy, _enemySprite);
                    _views.Add(enemy.SpawnOrder, view);
                }

                view.Render(enemy);
            }

            _removedSpawnOrders.Clear();
            foreach (long spawnOrder in _views.Keys)
            {
                if (!_activeSpawnOrders.Contains(spawnOrder))
                {
                    _removedSpawnOrders.Add(spawnOrder);
                }
            }

            foreach (long spawnOrder in _removedSpawnOrders)
            {
                EnemyView view = _views[spawnOrder];
                _views.Remove(spawnOrder);
                DestroyObject(view.gameObject);
            }
        }

        public Vector3 GetEnemyPosition(long spawnOrder)
        {
            if (!_views.TryGetValue(spawnOrder, out EnemyView view))
            {
                throw new ArgumentOutOfRangeException(nameof(spawnOrder), spawnOrder, "Enemy view was not found.");
            }

            return view.transform.position;
        }

        public void Clear()
        {
            foreach (EnemyView view in _views.Values)
            {
                DestroyObject(view.gameObject);
            }

            _views.Clear();
            _activeSpawnOrders.Clear();
            _removedSpawnOrders.Clear();

            if (_enemySprite != null)
            {
                DestroyObject(_enemySprite);
                _enemySprite = null;
            }

            if (_pathMaterial != null)
            {
                DestroyObject(_pathMaterial);
                _pathMaterial = null;
            }

            if (_pathRenderer != null)
            {
                _pathRenderer.positionCount = 0;
            }
        }

        private void OnDestroy()
        {
            Clear();
        }

        private void ConfigurePath(IReadOnlyList<Vector2> pathPoints)
        {
            _pathRenderer = GetComponent<LineRenderer>();
            if (_pathRenderer == null)
            {
                _pathRenderer = gameObject.AddComponent<LineRenderer>();
            }

            Shader shader = Shader.Find("Sprites/Default");
            if (shader == null)
            {
                throw new InvalidOperationException("The Sprites/Default shader is required.");
            }

            _pathMaterial = new Material(shader)
            {
                name = "Runtime Path Material",
            };
            _pathRenderer.sharedMaterial = _pathMaterial;
            _pathRenderer.useWorldSpace = true;
            _pathRenderer.widthMultiplier = 0.12f;
            _pathRenderer.startColor = new Color(0.38f, 0.49f, 0.62f, 1f);
            _pathRenderer.endColor = new Color(0.38f, 0.49f, 0.62f, 1f);
            _pathRenderer.numCapVertices = 4;
            _pathRenderer.numCornerVertices = 4;
            _pathRenderer.sortingOrder = 0;
            _pathRenderer.positionCount = pathPoints.Count;

            for (int index = 0; index < pathPoints.Count; index++)
            {
                _pathRenderer.SetPosition(index, new Vector3(pathPoints[index].x, pathPoints[index].y, 0f));
            }
        }

        private EnemyView CreateEnemyView(EnemyState enemy, Sprite sprite)
        {
            var enemyObject = new GameObject($"Enemy_{enemy.SpawnOrder:D3}");
            enemyObject.transform.SetParent(transform, worldPositionStays: false);
            EnemyView view = enemyObject.AddComponent<EnemyView>();
            view.Initialize(enemy, sprite);
            return view;
        }

        private static Sprite CreateEnemySprite()
        {
            Texture2D texture = Texture2D.whiteTexture;
            Sprite sprite = Sprite.Create(
                texture,
                new Rect(0f, 0f, texture.width, texture.height),
                new Vector2(0.5f, 0.5f),
                texture.width);
            sprite.name = "Runtime Enemy Sprite";
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
