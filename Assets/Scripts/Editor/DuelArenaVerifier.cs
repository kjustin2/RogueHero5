using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Unity.BossRoom.Editor
{
    public static class DuelArenaVerifier
    {
        const string k_BaseScenePath = "Assets/Scenes/BossRoom.unity";
        const string k_ArenaScenePath = "Assets/Scenes/BossRoom/DungeonBossRoom.unity";

        [MenuItem("Boss Room/Verify Duel Arena")]
        public static void RunFromMenu()
        {
            Verify();
        }

        public static void Run()
        {
            try
            {
                Verify();
                EditorApplication.Exit(0);
            }
            catch (Exception e)
            {
                Debug.LogError($"Duel arena verification failed: {e}");
                EditorApplication.Exit(1);
            }
        }

        static void Verify()
        {
            EditorSceneManager.OpenScene(k_BaseScenePath, OpenSceneMode.Single);
            EditorSceneManager.OpenScene(k_ArenaScenePath, OpenSceneMode.Additive);

            RequireSceneLoaded("BossRoom");
            RequireSceneLoaded("DungeonBossRoom");

            var arenaRoot = RequireObject("BossRoom");
            var staticObjects = RequireObject("BossRoomStaticNetworkObjects");
            var bossSpawner = RequireObject("NetworkObjectSpawner(ImpBoss)");

            var renderers = UnityEngine.Object.FindObjectsByType<MeshRenderer>(FindObjectsSortMode.None);
            var visibleRendererCount = 0;
            var hasVisibleBounds = false;
            var visibleBounds = new Bounds();
            foreach (var renderer in renderers)
            {
                if (!renderer.enabled || !renderer.gameObject.activeInHierarchy)
                {
                    continue;
                }

                visibleRendererCount++;
                if (!hasVisibleBounds)
                {
                    visibleBounds = renderer.bounds;
                    hasVisibleBounds = true;
                }
                else
                {
                    visibleBounds.Encapsulate(renderer.bounds);
                }
            }

            if (!hasVisibleBounds || visibleRendererCount < 50)
            {
                throw new InvalidOperationException($"Expected a populated arena, found only {visibleRendererCount} visible mesh renderers.");
            }

            if (!visibleBounds.Contains(bossSpawner.transform.position))
            {
                throw new InvalidOperationException($"ImpBoss spawn anchor {bossSpawner.transform.position} is outside visible arena bounds {visibleBounds}.");
            }

            Debug.Log(
                $"Duel arena verification passed. ArenaRoot={arenaRoot.name}, StaticRoot={staticObjects.name}, " +
                $"ImpBossSpawn={bossSpawner.transform.position}, VisibleRenderers={visibleRendererCount}, Bounds={visibleBounds}.");
        }

        static void RequireSceneLoaded(string sceneName)
        {
            var scene = SceneManager.GetSceneByName(sceneName);
            if (!scene.isLoaded)
            {
                throw new FileNotFoundException($"Scene '{sceneName}' was not loaded.");
            }
        }

        static GameObject RequireObject(string objectName)
        {
            var gameObject = GameObject.Find(objectName);
            if (!gameObject)
            {
                throw new MissingReferenceException($"Required object '{objectName}' was not found.");
            }

            return gameObject;
        }
    }
}
