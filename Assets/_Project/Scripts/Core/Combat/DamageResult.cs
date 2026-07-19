namespace RandomTowerDefense.Core.Combat
{
    public readonly struct DamageResult
    {
        public DamageResult(float appliedDamage, bool killed, int killReward)
        {
            AppliedDamage = appliedDamage;
            Killed = killed;
            KillReward = killReward;
        }

        public float AppliedDamage { get; }

        public bool Killed { get; }

        public int KillReward { get; }

        public static DamageResult Ignored => new DamageResult(0f, false, 0);
    }
}
