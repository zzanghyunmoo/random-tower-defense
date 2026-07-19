using UnityEngine;

namespace RandomTowerDefense.Data.Definitions
{
    public abstract class DefinitionAsset : ScriptableObject
    {
        [SerializeField]
        private string _id = string.Empty;

        public string Id => _id;

#if UNITY_EDITOR
        public void SetIdForEditor(string id)
        {
            _id = id;
        }
#endif
    }
}
