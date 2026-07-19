#nullable enable

using System;
using System.Collections.Generic;
using RandomTowerDefense.Core.Waves;

namespace RandomTowerDefense.Core.Combat
{
    public sealed class GameSessionTickResult
    {
        internal GameSessionTickResult(
            EnemyRemovalResult movement,
            IReadOnlyList<ProjectileState> firedProjectiles,
            IReadOnlyList<ProjectileAdvanceResult> projectileResults,
            EnemyRemovalResult deadEnemyCleanup,
            EnemySpawnPhaseResult? spawnPhase,
            GameSessionStatus statusBefore,
            GameSessionStatus statusAfter)
        {
            Movement = movement ?? throw new ArgumentNullException(nameof(movement));
            DeadEnemyCleanup = deadEnemyCleanup ?? throw new ArgumentNullException(nameof(deadEnemyCleanup));
            FiredProjectiles = CopyReferences(firedProjectiles, nameof(firedProjectiles));
            ProjectileResults = CopyValues(projectileResults, nameof(projectileResults));
            SpawnPhase = spawnPhase;
            StatusBefore = statusBefore;
            StatusAfter = statusAfter;
        }

        public EnemyRemovalResult Movement { get; }

        public IReadOnlyList<ProjectileState> FiredProjectiles { get; }

        public IReadOnlyList<ProjectileAdvanceResult> ProjectileResults { get; }

        public EnemyRemovalResult DeadEnemyCleanup { get; }

        public EnemySpawnPhaseResult? SpawnPhase { get; }

        public GameSessionStatus StatusBefore { get; }

        public GameSessionStatus StatusAfter { get; }

        public bool StatusChanged => StatusBefore != StatusAfter;

        private static IReadOnlyList<ProjectileState> CopyReferences(
            IReadOnlyList<ProjectileState> source,
            string parameterName)
        {
            if (source == null)
            {
                throw new ArgumentNullException(parameterName);
            }

            var values = new ProjectileState[source.Count];
            for (int index = 0; index < source.Count; index++)
            {
                values[index] = source[index]
                    ?? throw new ArgumentException("Projectile collection must not contain null.", parameterName);
            }

            return Array.AsReadOnly(values);
        }

        private static IReadOnlyList<ProjectileAdvanceResult> CopyValues(
            IReadOnlyList<ProjectileAdvanceResult> source,
            string parameterName)
        {
            if (source == null)
            {
                throw new ArgumentNullException(parameterName);
            }

            var values = new ProjectileAdvanceResult[source.Count];
            for (int index = 0; index < source.Count; index++)
            {
                values[index] = source[index];
            }

            return Array.AsReadOnly(values);
        }
    }
}
