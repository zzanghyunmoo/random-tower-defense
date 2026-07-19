using System;
using System.Collections.Generic;

namespace RandomTowerDefense.Core.Waves
{
    public sealed class EnemyRemovalResult
    {
        internal EnemyRemovalResult(
            IReadOnlyList<long> removedEnemyOrders,
            int playerDamageApplied,
            int killReward,
            bool defeatedThisPhase)
        {
            if (removedEnemyOrders == null)
            {
                throw new ArgumentNullException(nameof(removedEnemyOrders));
            }

            var orders = new long[removedEnemyOrders.Count];
            for (int index = 0; index < removedEnemyOrders.Count; index++)
            {
                orders[index] = removedEnemyOrders[index];
            }

            RemovedEnemyOrders = Array.AsReadOnly(orders);
            PlayerDamageApplied = playerDamageApplied;
            KillReward = killReward;
            DefeatedThisPhase = defeatedThisPhase;
        }

        public IReadOnlyList<long> RemovedEnemyOrders { get; }

        public int PlayerDamageApplied { get; }

        public int KillReward { get; }

        public bool DefeatedThisPhase { get; }
    }
}
