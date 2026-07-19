#nullable enable

using System;
using System.Collections.Generic;
using RandomTowerDefense.Core.Enemies;
using RandomTowerDefense.Core.Towers;
using RandomTowerDefense.Core.Waves;

namespace RandomTowerDefense.Core.Combat
{
    public sealed class GameSessionDefinition
    {
        public GameSessionDefinition(
            string id,
            Path2D enemyPath,
            int startingHealth,
            int startingCurrency,
            int summonCost,
            IReadOnlyList<WaveDefinition> waves,
            IReadOnlyList<TowerSlotDefinition> towerSlots,
            IReadOnlyList<TowerSummonPoolEntry> summonPool)
        {
            Id = Guard.DefinitionId(id, nameof(id));
            EnemyPath = enemyPath ?? throw new ArgumentNullException(nameof(enemyPath));
            StartingHealth = Guard.Positive(startingHealth, nameof(startingHealth));
            StartingCurrency = Guard.NonNegative(startingCurrency, nameof(startingCurrency));
            SummonCost = Guard.NonNegative(summonCost, nameof(summonCost));
            Waves = Copy(waves, nameof(waves));
            if (Waves.Count == 0)
            {
                throw new ArgumentException("At least one wave is required.", nameof(waves));
            }

            ValidateMaximumCurrency(StartingCurrency, Waves, nameof(waves));

            TowerSlots = Copy(towerSlots, nameof(towerSlots));
            SummonPool = Copy(summonPool, nameof(summonPool));
        }

        public string Id { get; }

        public Path2D EnemyPath { get; }

        public int StartingHealth { get; }

        public int StartingCurrency { get; }

        public int SummonCost { get; }

        public IReadOnlyList<WaveDefinition> Waves { get; }

        public IReadOnlyList<TowerSlotDefinition> TowerSlots { get; }

        public IReadOnlyList<TowerSummonPoolEntry> SummonPool { get; }

        private static IReadOnlyList<T> Copy<T>(IReadOnlyList<T> source, string parameterName)
            where T : class
        {
            if (source == null)
            {
                throw new ArgumentNullException(parameterName);
            }

            var values = new T[source.Count];
            for (int index = 0; index < source.Count; index++)
            {
                values[index] = source[index]
                    ?? throw new ArgumentException("Definition collection must not contain null.", parameterName);
            }

            return Array.AsReadOnly(values);
        }

        private static void ValidateMaximumCurrency(
            int startingCurrency,
            IReadOnlyList<WaveDefinition> waves,
            string parameterName)
        {
            long maximumKillReward = 0;
            try
            {
                foreach (WaveDefinition wave in waves)
                {
                    maximumKillReward = checked(
                        maximumKillReward + ((long)wave.EnemyCount * wave.Enemy.KillReward));
                }
            }
            catch (OverflowException exception)
            {
                throw new ArgumentException("Maximum stage kill reward exceeded Int64.MaxValue.", parameterName, exception);
            }

            if (maximumKillReward > int.MaxValue - (long)startingCurrency)
            {
                throw new ArgumentException(
                    "Starting currency plus maximum stage kill rewards must fit in Int32.",
                    parameterName);
            }
        }
    }
}
