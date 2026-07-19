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
            DataValidationResult result = ValidateProjectDataSet(out int definitionCount);
            if (result.IsValid)
            {
                Debug.Log($"Data validation passed for {definitionCount} definitions.");
                return;
            }

            foreach (DataValidationIssue issue in result.Issues)
            {
                Debug.LogError(issue.ToString(), issue.Source);
            }

            Debug.LogError($"Data validation failed with {result.Issues.Count} error(s).");
        }

        public static DataValidationResult ValidateProjectDataSet()
        {
            return ValidateProjectDataSet(out _);
        }

        private static DataValidationResult ValidateProjectDataSet(out int definitionCount)
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

            definitionCount = definitions.Count;
            return DataValidator.Validate(definitions);
        }
    }
}
