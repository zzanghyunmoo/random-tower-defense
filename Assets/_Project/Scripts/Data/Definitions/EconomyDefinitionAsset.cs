#nullable enable

using UnityEngine;

namespace RandomTowerDefense.Data.Definitions
{
    [CreateAssetMenu(menuName = "Random Tower Defense/Economy Definition", fileName = "EconomyDefinition")]
    public sealed class EconomyDefinitionAsset : DefinitionAsset
    {
        [SerializeField]
        private int _startingCurrency;

        [SerializeField]
        private int _summonCost;

        public int StartingCurrency => _startingCurrency;

        public int SummonCost => _summonCost;

#if UNITY_EDITOR
        public void ConfigureForEditor(string id, int startingCurrency, int summonCost)
        {
            SetIdForEditor(id);
            _startingCurrency = startingCurrency;
            _summonCost = summonCost;
        }
#endif
    }
}
