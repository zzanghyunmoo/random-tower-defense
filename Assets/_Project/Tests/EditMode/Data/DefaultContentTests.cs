#nullable enable

using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using RandomTowerDefense.Core.Combat;
using RandomTowerDefense.Data.Definitions;
using RandomTowerDefense.Data.Runtime;
using RandomTowerDefense.Data.Validation;
using RandomTowerDefense.Editor;
using UnityEditor;

namespace RandomTowerDefense.Tests.EditMode.Data
{
    public sealed class DefaultContentTests
    {
        private const string TemporaryRoot = "Assets/_Project/Tests/GeneratedContentTemp";

        [TearDown]
        public void TearDown()
        {
            AssetDatabase.DeleteAsset(TemporaryRoot);
        }

        [Test]
        public void CommittedDefaultContentIsCompleteValidAndPlayable()
        {
            IReadOnlyList<DefinitionAsset> definitions = LoadDefinitions(DefaultContentGenerator.DefaultRoot);
            StageDefinitionAsset stage = AssetDatabase.LoadAssetAtPath<StageDefinitionAsset>(
                DefaultContentGenerator.DefaultStagePath);

            Assert.That(stage, Is.Not.Null, "The committed default stage is missing.");
            Assert.That(definitions.OfType<EnemyDefinitionAsset>().Count(), Is.EqualTo(2));
            Assert.That(definitions.OfType<ProjectileDefinitionAsset>().Count(), Is.EqualTo(2));
            Assert.That(definitions.OfType<TowerDefinitionAsset>().Count(), Is.EqualTo(2));
            Assert.That(definitions.OfType<WaveDefinitionAsset>().Count(), Is.EqualTo(3));
            Assert.That(definitions.OfType<EconomyDefinitionAsset>().Count(), Is.EqualTo(1));
            Assert.That(definitions.OfType<StageDefinitionAsset>().Count(), Is.EqualTo(1));

            DataValidationResult validation = DataValidator.Validate(definitions);
            Assert.That(validation.IsValid, Is.True, string.Join("\n", validation.Issues));

            GameSessionDefinition coreDefinition = GameDataMapper.ToCore(stage!);
            var session = new GameSession(coreDefinition, seed: 42);
            GameSessionTickResult firstTick = session.Advance(0f);

            Assert.That(coreDefinition.Waves, Has.Count.EqualTo(3));
            Assert.That(coreDefinition.TowerSlots, Has.Count.EqualTo(8));
            Assert.That(coreDefinition.SummonPool, Has.Count.EqualTo(2));
            Assert.That(session.CanSummon, Is.True);
            Assert.That(firstTick.SpawnPhase!.SpawnedEnemies, Has.Count.EqualTo(1));

            SummonWhileAvailable(session);
            for (int tick = 0; tick < 2400 && session.Status == GameSessionStatus.Running; tick++)
            {
                session.Advance(0.1f);
                SummonWhileAvailable(session);
            }

            Assert.That(session.Status, Is.EqualTo(GameSessionStatus.Victory));
        }

        [Test]
        public void GeneratorCanRunTwiceWithoutReplacingAssets()
        {
            StageDefinitionAsset firstStage = DefaultContentGenerator.GenerateAt(TemporaryRoot);
            IReadOnlyDictionary<string, string> firstGuids = LoadGuids(TemporaryRoot);

            StageDefinitionAsset secondStage = DefaultContentGenerator.GenerateAt(TemporaryRoot);
            IReadOnlyDictionary<string, string> secondGuids = LoadGuids(TemporaryRoot);

            Assert.That(secondStage, Is.SameAs(firstStage));
            Assert.That(secondGuids, Is.EqualTo(firstGuids));
            Assert.That(LoadDefinitions(TemporaryRoot).Count, Is.EqualTo(11));
        }

        private static IReadOnlyList<DefinitionAsset> LoadDefinitions(string root)
        {
            return AssetDatabase.FindAssets("t:DefinitionAsset", new[] { root })
                .Select(AssetDatabase.GUIDToAssetPath)
                .OrderBy(path => path, System.StringComparer.Ordinal)
                .Select(AssetDatabase.LoadAssetAtPath<DefinitionAsset>)
                .Where(definition => definition != null)
                .ToArray()!;
        }

        private static IReadOnlyDictionary<string, string> LoadGuids(string root)
        {
            return AssetDatabase.FindAssets("t:DefinitionAsset", new[] { root })
                .Select(guid => new
                {
                    Path = AssetDatabase.GUIDToAssetPath(guid),
                    Guid = guid,
                })
                .OrderBy(item => item.Path, System.StringComparer.Ordinal)
                .ToDictionary(item => item.Path, item => item.Guid);
        }

        private static void SummonWhileAvailable(GameSession session)
        {
            while (session.CanSummon)
            {
                Assert.That(session.TrySummonTower().Succeeded, Is.True);
            }
        }
    }
}
