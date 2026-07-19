using UnityEngine;

namespace RandomTowerDefense.Data.Definitions
{
    [CreateAssetMenu(menuName = "Random Tower Defense/Enemy Definition", fileName = "EnemyDefinition")]
    public sealed class EnemyDefinitionAsset : DefinitionAsset
    {
        [SerializeField]
        private float _maxHealth;

        [SerializeField]
        private float _moveSpeed;

        [SerializeField]
        private int _endpointDamage;

        [SerializeField]
        private int _killReward;

        public float MaxHealth => _maxHealth;

        public float MoveSpeed => _moveSpeed;

        public int EndpointDamage => _endpointDamage;

        public int KillReward => _killReward;

#if UNITY_EDITOR
        public void ConfigureForEditor(
            string id,
            float maxHealth,
            float moveSpeed,
            int endpointDamage,
            int killReward)
        {
            SetIdForEditor(id);
            _maxHealth = maxHealth;
            _moveSpeed = moveSpeed;
            _endpointDamage = endpointDamage;
            _killReward = killReward;
        }
#endif
    }
}
