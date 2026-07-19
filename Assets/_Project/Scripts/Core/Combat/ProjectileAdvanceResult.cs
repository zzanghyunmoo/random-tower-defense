namespace RandomTowerDefense.Core.Combat
{
    public readonly struct ProjectileAdvanceResult
    {
        public ProjectileAdvanceResult(
            long projectileOrder,
            float distanceMoved,
            bool resolvedThisAdvance,
            ProjectileStatus status,
            DamageResult damage)
        {
            ProjectileOrder = projectileOrder;
            DistanceMoved = distanceMoved;
            ResolvedThisAdvance = resolvedThisAdvance;
            Status = status;
            Damage = damage;
        }

        public long ProjectileOrder { get; }

        public float DistanceMoved { get; }

        public bool ResolvedThisAdvance { get; }

        public ProjectileStatus Status { get; }

        public DamageResult Damage { get; }
    }
}
