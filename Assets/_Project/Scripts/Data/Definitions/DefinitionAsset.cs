using RandomTowerDefense.Data.Validation;
using UnityEngine;

namespace RandomTowerDefense.Data.Definitions
{
    public abstract class DefinitionAsset : ScriptableObject
    {
        [SerializeField]
        private string _id = string.Empty;

        public string Id => _id;

        protected virtual void OnValidate()
        {
            DataValidationResult result = DataValidator.ValidateDefinition(this);
            foreach (DataValidationIssue issue in result.Issues)
            {
                Debug.LogError(issue.ToString(), this);
            }
        }

#if UNITY_EDITOR
        public void SetIdForEditor(string id)
        {
            _id = id;
        }
#endif
    }
}
