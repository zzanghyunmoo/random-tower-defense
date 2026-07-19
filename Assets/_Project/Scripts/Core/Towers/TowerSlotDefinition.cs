using RandomTowerDefense.Core.Combat;

namespace RandomTowerDefense.Core.Towers
{
    public sealed class TowerSlotDefinition
    {
        public TowerSlotDefinition(string id, Point2 position, long placementOrder)
        {
            Id = Guard.DefinitionId(id, nameof(id));
            Position = position;
            PlacementOrder = Guard.NonNegative(placementOrder, nameof(placementOrder));
        }

        public string Id { get; }

        public Point2 Position { get; }

        public long PlacementOrder { get; }
    }
}
