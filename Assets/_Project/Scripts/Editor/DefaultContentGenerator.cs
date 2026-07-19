#nullable enable

using System;
using RandomTowerDefense.Data.Definitions;
using UnityEditor;
using UnityEngine;

namespace RandomTowerDefense.Editor
{
    public static class DefaultContentGenerator
    {
        public const string DefaultRoot = "Assets/_Project/Data";
        public const string DefaultStagePath = DefaultRoot + "/Stages/Stage01.asset";

        [MenuItem("Tools/Random Tower Defense/Generate Default Content")]
        public static void GenerateDefaultContent()
        {
            StageDefinitionAsset stage = GenerateAt(DefaultRoot);
            if (!UnityEngine.Application.isBatchMode)
            {
                Selection.activeObject = stage;
            }

            Debug.Log($"Default content generated at {DefaultRoot}.", stage);
        }

        public static StageDefinitionAsset GenerateAt(string root)
        {
            string normalizedRoot = NormalizeRoot(root);
            EnsureFolder(normalizedRoot);
            EnsureFolder(normalizedRoot + "/Enemies");
            EnsureFolder(normalizedRoot + "/Projectiles");
            EnsureFolder(normalizedRoot + "/Towers");
            EnsureFolder(normalizedRoot + "/Economy");
            EnsureFolder(normalizedRoot + "/Waves");
            EnsureFolder(normalizedRoot + "/Stages");

            EnemyDefinitionAsset smallSlime = LoadOrCreate<EnemyDefinitionAsset>(
                normalizedRoot + "/Enemies/EnemySlimeSmall.asset");
            smallSlime.ConfigureForEditor(
                "enemy_slime_small",
                maxHealth: 18f,
                moveSpeed: 1.2f,
                endpointDamage: 1,
                killReward: 2);

            EnemyDefinitionAsset tankSlime = LoadOrCreate<EnemyDefinitionAsset>(
                normalizedRoot + "/Enemies/EnemySlimeTank.asset");
            tankSlime.ConfigureForEditor(
                "enemy_slime_tank",
                maxHealth: 45f,
                moveSpeed: 0.7f,
                endpointDamage: 2,
                killReward: 5);

            ProjectileDefinitionAsset arrowProjectile = LoadOrCreate<ProjectileDefinitionAsset>(
                normalizedRoot + "/Projectiles/ProjectileArrow.asset");
            arrowProjectile.ConfigureForEditor("projectile_arrow", speed: 7f);

            ProjectileDefinitionAsset emberProjectile = LoadOrCreate<ProjectileDefinitionAsset>(
                normalizedRoot + "/Projectiles/ProjectileEmber.asset");
            emberProjectile.ConfigureForEditor("projectile_ember", speed: 5f);

            TowerDefinitionAsset arrowTower = LoadOrCreate<TowerDefinitionAsset>(
                normalizedRoot + "/Towers/TowerArrow.asset");
            arrowTower.ConfigureForEditor(
                "tower_arrow",
                range: 3.5f,
                attackIntervalSeconds: 0.8f,
                damage: 5f,
                arrowProjectile);

            TowerDefinitionAsset emberTower = LoadOrCreate<TowerDefinitionAsset>(
                normalizedRoot + "/Towers/TowerEmber.asset");
            emberTower.ConfigureForEditor(
                "tower_ember",
                range: 2.8f,
                attackIntervalSeconds: 1.3f,
                damage: 9f,
                emberProjectile);

            EconomyDefinitionAsset economy = LoadOrCreate<EconomyDefinitionAsset>(
                normalizedRoot + "/Economy/EconomyDefault.asset");
            economy.ConfigureForEditor("economy_default", startingCurrency: 30, summonCost: 10);

            WaveDefinitionAsset wave1 = LoadOrCreate<WaveDefinitionAsset>(
                normalizedRoot + "/Waves/WaveStage0101.asset");
            wave1.ConfigureForEditor("wave_stage_01_01", smallSlime, enemyCount: 6, spawnIntervalSeconds: 0.8f);

            WaveDefinitionAsset wave2 = LoadOrCreate<WaveDefinitionAsset>(
                normalizedRoot + "/Waves/WaveStage0102.asset");
            wave2.ConfigureForEditor("wave_stage_01_02", smallSlime, enemyCount: 8, spawnIntervalSeconds: 0.65f);

            WaveDefinitionAsset wave3 = LoadOrCreate<WaveDefinitionAsset>(
                normalizedRoot + "/Waves/WaveStage0103.asset");
            wave3.ConfigureForEditor("wave_stage_01_03", tankSlime, enemyCount: 5, spawnIntervalSeconds: 0.9f);

            StageDefinitionAsset stage = LoadOrCreate<StageDefinitionAsset>(normalizedRoot + "/Stages/Stage01.asset");
            stage.ConfigureForEditor(
                "stage_01",
                startingHealth: 10,
                economy,
                new[]
                {
                    new Vector2(-7f, 0f),
                    new Vector2(-3f, 0f),
                    new Vector2(0f, 2f),
                    new Vector2(4f, 2f),
                    new Vector2(7f, 0f),
                },
                new[] { wave1, wave2, wave3 },
                new[]
                {
                    new TowerSlotData("slot_01", new Vector2(-5f, 1.5f), 0),
                    new TowerSlotData("slot_02", new Vector2(-3f, -1.5f), 1),
                    new TowerSlotData("slot_03", new Vector2(-1f, 3.5f), 2),
                    new TowerSlotData("slot_04", new Vector2(1f, 0f), 3),
                    new TowerSlotData("slot_05", new Vector2(3f, 4f), 4),
                    new TowerSlotData("slot_06", new Vector2(4f, 0f), 5),
                    new TowerSlotData("slot_07", new Vector2(6f, 3f), 6),
                    new TowerSlotData("slot_08", new Vector2(6f, -1f), 7),
                },
                new[]
                {
                    new TowerPoolEntryData(arrowTower, weight: 3),
                    new TowerPoolEntryData(emberTower, weight: 2),
                });

            MarkDirty(
                smallSlime,
                tankSlime,
                arrowProjectile,
                emberProjectile,
                arrowTower,
                emberTower,
                economy,
                wave1,
                wave2,
                wave3,
                stage);
            AssetDatabase.SaveAssets();
            return stage;
        }

        private static string NormalizeRoot(string root)
        {
            if (string.IsNullOrWhiteSpace(root))
            {
                throw new ArgumentException("Content root is required.", nameof(root));
            }

            string normalized = root.Replace('\\', '/').TrimEnd('/');
            if (!normalized.StartsWith("Assets/", StringComparison.Ordinal) || normalized.Contains(".."))
            {
                throw new ArgumentException("Content root must be a child of Assets.", nameof(root));
            }

            return normalized;
        }

        private static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path))
            {
                return;
            }

            string parent = path.Substring(0, path.LastIndexOf('/'));
            EnsureFolder(parent);
            string name = path.Substring(path.LastIndexOf('/') + 1);
            AssetDatabase.CreateFolder(parent, name);
        }

        private static T LoadOrCreate<T>(string path)
            where T : ScriptableObject
        {
            T asset = AssetDatabase.LoadAssetAtPath<T>(path);
            if (asset != null)
            {
                return asset;
            }

            UnityEngine.Object existingAsset = AssetDatabase.LoadMainAssetAtPath(path);
            if (existingAsset != null)
            {
                throw new InvalidOperationException($"Asset at '{path}' is not a {typeof(T).Name}.");
            }

            asset = ScriptableObject.CreateInstance<T>();
            AssetDatabase.CreateAsset(asset, path);
            return asset;
        }

        private static void MarkDirty(params UnityEngine.Object[] assets)
        {
            foreach (UnityEngine.Object asset in assets)
            {
                EditorUtility.SetDirty(asset);
            }
        }
    }
}
