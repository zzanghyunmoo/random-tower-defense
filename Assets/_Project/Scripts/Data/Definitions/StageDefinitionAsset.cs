#nullable enable

using System;
using System.Collections.Generic;
using UnityEngine;

namespace RandomTowerDefense.Data.Definitions
{
    [Serializable]
    public sealed class TowerSlotData
    {
        [SerializeField]
        private string _id = string.Empty;

        [SerializeField]
        private Vector2 _position;

        [SerializeField]
        private long _placementOrder;

        public TowerSlotData(string id, Vector2 position, long placementOrder)
        {
            _id = id;
            _position = position;
            _placementOrder = placementOrder;
        }

        public string Id => _id;

        public Vector2 Position => _position;

        public long PlacementOrder => _placementOrder;
    }

    [Serializable]
    public sealed class TowerPoolEntryData
    {
        [SerializeField]
        private TowerDefinitionAsset? _tower;

        [SerializeField]
        private int _weight;

        public TowerPoolEntryData(TowerDefinitionAsset tower, int weight)
        {
            _tower = tower;
            _weight = weight;
        }

        public TowerDefinitionAsset? Tower => _tower;

        public int Weight => _weight;
    }

    [CreateAssetMenu(menuName = "Random Tower Defense/Stage Definition", fileName = "StageDefinition")]
    public sealed class StageDefinitionAsset : DefinitionAsset
    {
        [SerializeField]
        private int _startingHealth;

        [SerializeField]
        private EconomyDefinitionAsset? _economy;

        [SerializeField]
        private List<Vector2> _pathPoints = new List<Vector2>();

        [SerializeField]
        private List<WaveDefinitionAsset> _waves = new List<WaveDefinitionAsset>();

        [SerializeField]
        private List<TowerSlotData> _towerSlots = new List<TowerSlotData>();

        [SerializeField]
        private List<TowerPoolEntryData> _summonPool = new List<TowerPoolEntryData>();

        public int StartingHealth => _startingHealth;

        public EconomyDefinitionAsset? Economy => _economy;

        public IReadOnlyList<Vector2> PathPoints => _pathPoints;

        public IReadOnlyList<WaveDefinitionAsset> Waves => _waves;

        public IReadOnlyList<TowerSlotData> TowerSlots => _towerSlots;

        public IReadOnlyList<TowerPoolEntryData> SummonPool => _summonPool;

#if UNITY_EDITOR
        public void ConfigureForEditor(
            string id,
            int startingHealth,
            EconomyDefinitionAsset economy,
            IReadOnlyList<Vector2> pathPoints,
            IReadOnlyList<WaveDefinitionAsset> waves,
            IReadOnlyList<TowerSlotData> towerSlots,
            IReadOnlyList<TowerPoolEntryData> summonPool)
        {
            SetIdForEditor(id);
            _startingHealth = startingHealth;
            _economy = economy;
            _pathPoints = new List<Vector2>(pathPoints);
            _waves = new List<WaveDefinitionAsset>(waves);
            _towerSlots = new List<TowerSlotData>(towerSlots);
            _summonPool = new List<TowerPoolEntryData>(summonPool);
        }
#endif
    }
}
