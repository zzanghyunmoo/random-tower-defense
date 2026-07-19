namespace RandomTowerDefense.Core.Enemies
{
    public readonly struct EnemyAdvanceResult
    {
        public EnemyAdvanceResult(float distanceMoved, bool reachedEndpoint, int endpointDamage)
        {
            DistanceMoved = distanceMoved;
            ReachedEndpoint = reachedEndpoint;
            EndpointDamage = endpointDamage;
        }

        public float DistanceMoved { get; }

        public bool ReachedEndpoint { get; }

        public int EndpointDamage { get; }

        public static EnemyAdvanceResult NoChange => new EnemyAdvanceResult(0f, false, 0);
    }
}
