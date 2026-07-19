#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;
using RandomTowerDefense.Data.Definitions;
using RandomTowerDefense.UnityAdapters.MonoBehaviours;
using RandomTowerDefense.UnityAdapters.Views;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace RandomTowerDefense.Editor
{
    public static class MainSceneGenerator
    {
        public const string ScenePath = "Assets/_Project/Scenes/Main.unity";

        [MenuItem("Tools/Random Tower Defense/Generate Main Scene")]
        public static void GenerateMainScene()
        {
            if (!UnityEngine.Application.isBatchMode &&
                !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            {
                return;
            }

            StageDefinitionAsset stage = AssetDatabase.LoadAssetAtPath<StageDefinitionAsset>(
                DefaultContentGenerator.DefaultStagePath);
            if (stage == null)
            {
                throw new InvalidOperationException("Generate the default content before the main scene.");
            }

            EnsureFolder("Assets/_Project/Scenes");
            Scene scene = LoadOrCreateScene();

            ConfigureCamera(GetOrCreateRootObject(scene, "Main Camera"));
            GameObject boardObject = GetOrCreateRootObject(scene, "Enemy Board");
            EnemyBoardView board = GetOrAddComponent<EnemyBoardView>(boardObject);
            SetSerializedName(board, "Enemy Board View");

            GameObject sessionObject = GetOrCreateRootObject(scene, "Game Session");
            GameSessionBehaviour session = GetOrAddComponent<GameSessionBehaviour>(sessionObject);
            SetSerializedName(session, "Game Session Behaviour");
            session.ConfigureForEditor(stage, board, seed: 42);

            EditorSceneManager.MarkSceneDirty(scene);
            if (!EditorSceneManager.SaveScene(scene, ScenePath))
            {
                throw new InvalidOperationException($"Could not save the main scene at '{ScenePath}'.");
            }

            ConfigureBuildSettings();
            AssetDatabase.SaveAssets();
            Debug.Log($"Main scene generated at {ScenePath}.");
        }

        private static Scene LoadOrCreateScene()
        {
            return AssetDatabase.LoadAssetAtPath<SceneAsset>(ScenePath) != null
                ? EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single)
                : EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        }

        private static void ConfigureCamera(GameObject cameraObject)
        {
            cameraObject.tag = "MainCamera";
            cameraObject.transform.position = new Vector3(0f, 1.25f, -10f);
            cameraObject.transform.rotation = Quaternion.identity;

            Camera camera = GetOrAddComponent<Camera>(cameraObject);
            camera.orthographic = true;
            camera.orthographicSize = 5.25f;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.055f, 0.075f, 0.11f, 1f);
            GetOrAddComponent<AudioListener>(cameraObject);
        }

        private static GameObject GetOrCreateRootObject(Scene scene, string name)
        {
            GameObject[] matches = scene.GetRootGameObjects()
                .Where(rootObject => string.Equals(rootObject.name, name, StringComparison.Ordinal))
                .ToArray();
            if (matches.Length > 1)
            {
                throw new InvalidOperationException($"Scene contains more than one root object named '{name}'.");
            }

            if (matches.Length == 1)
            {
                return matches[0];
            }

            var created = new GameObject(name);
            SceneManager.MoveGameObjectToScene(created, scene);
            return created;
        }

        private static T GetOrAddComponent<T>(GameObject gameObject)
            where T : Component
        {
            T component = gameObject.GetComponent<T>();
            return component != null ? component : gameObject.AddComponent<T>();
        }

        private static void SetSerializedName(Component component, string name)
        {
            using var serializedObject = new SerializedObject(component);
            SerializedProperty nameProperty = serializedObject.FindProperty("m_Name");
            if (nameProperty == null)
            {
                throw new InvalidOperationException($"Could not name component '{component.GetType().Name}'.");
            }

            nameProperty.stringValue = name;
            serializedObject.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void ConfigureBuildSettings()
        {
            var scenes = new List<EditorBuildSettingsScene>
            {
                new EditorBuildSettingsScene(ScenePath, enabled: true),
            };
            scenes.AddRange(
                EditorBuildSettings.scenes.Where(
                    scene => !string.Equals(scene.path, ScenePath, StringComparison.Ordinal)));
            EditorBuildSettings.scenes = scenes.ToArray();
        }

        private static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path))
            {
                return;
            }

            int separator = path.LastIndexOf('/');
            string parent = path.Substring(0, separator);
            EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, path.Substring(separator + 1));
        }
    }
}
