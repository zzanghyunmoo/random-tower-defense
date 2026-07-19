#nullable enable

using UnityEngine;

namespace RandomTowerDefense.Data.Definitions
{
    [CreateAssetMenu(menuName = "Random Tower Defense/Tower Definition", fileName = "TowerDefinition")]
    public sealed class TowerDefinitionAsset : DefinitionAsset
    {
        [SerializeField]
        private float _range;

        [SerializeField]
        private float _attackIntervalSeconds;

        [SerializeField]
        private float _damage;

        [SerializeField]
        private ProjectileDefinitionAsset? _projectile;

        public float Range => _range;

        public float AttackIntervalSeconds => _attackIntervalSeconds;

        public float Damage => _damage;

        public ProjectileDefinitionAsset? Projectile => _projectile;

#if UNITY_EDITOR
        public void ConfigureForEditor(
            string id,
            float range,
            float attackIntervalSeconds,
            float damage,
            ProjectileDefinitionAsset projectile)
        {
            SetIdForEditor(id);
            _range = range;
            _attackIntervalSeconds = attackIntervalSeconds;
            _damage = damage;
            _projectile = projectile;
        }
#endif
    }
}
