using UnityEngine;

namespace RandomTowerDefense.Data.Definitions
{
    [CreateAssetMenu(menuName = "Random Tower Defense/Projectile Definition", fileName = "ProjectileDefinition")]
    public sealed class ProjectileDefinitionAsset : DefinitionAsset
    {
        [SerializeField]
        private float _speed;

        public float Speed => _speed;

#if UNITY_EDITOR
        public void ConfigureForEditor(string id, float speed)
        {
            SetIdForEditor(id);
            _speed = speed;
        }
#endif
    }
}
