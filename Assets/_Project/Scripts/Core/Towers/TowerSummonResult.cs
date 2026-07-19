#nullable enable

namespace RandomTowerDefense.Core.Towers
{
    public sealed class TowerSummonResult
    {
        private TowerSummonResult(
            bool succeeded,
            TowerSummonFailure failure,
            string? slotId,
            TowerState? tower,
            int costSpent)
        {
            Succeeded = succeeded;
            Failure = failure;
            SlotId = slotId;
            Tower = tower;
            CostSpent = costSpent;
        }

        public bool Succeeded { get; }

        public TowerSummonFailure Failure { get; }

        public string? SlotId { get; }

        public TowerState? Tower { get; }

        public int CostSpent { get; }

        internal static TowerSummonResult Success(string slotId, TowerState tower, int costSpent)
        {
            return new TowerSummonResult(true, TowerSummonFailure.None, slotId, tower, costSpent);
        }

        internal static TowerSummonResult Failed(TowerSummonFailure failure)
        {
            return new TowerSummonResult(false, failure, null, null, 0);
        }
    }
}
