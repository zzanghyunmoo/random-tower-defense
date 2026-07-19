using System;
using System.Collections.Generic;
using RandomTowerDefense.Data.Definitions;
using RandomTowerDefense.Data.Validation;
using UnityEditor;
using UnityEngine;

namespace RandomTowerDefense.Editor
{
    public static class DataValidationMenu
    {
        [MenuItem("Tools/Random Tower Defense/Validate Data")]
        public static void ValidateProjectData()
        {
            var definitions = new List<DefinitionAsset>();
            string[] assetGuids = AssetDatabase.FindAssets("t:DefinitionAsset");
            Array.Sort(assetGuids, StringComparer.Ordinal);
            foreach (string assetGuid in assetGuids)
            {
                string path = AssetDatabase.GUIDToAssetPath(assetGuid);
                DefinitionAsset definition = AssetDatabase.LoadAssetAtPath<DefinitionAsset>(path);
                if (definition != null)
                {
                    definitions.Add(definition);
                }
            }

            DataValidationResult result = DataValidator.Validate(definitions);
            if (result.IsValid)
            {
                Debug.Log($"Data validation passed for {definitions.Count} definitions.");
                return;
            }

            foreach (DataValidationIssue issue in result.Issues)
            {
                Debug.LogError(issue.ToString(), issue.Source);
            }

            Debug.LogError($"Data validation failed with {result.Issues.Count} error(s).");
        }
    }
}
