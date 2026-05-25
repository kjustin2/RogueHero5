using System;
using System.IO;
using Unity.BossRoom.Gameplay.GameState;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.AI;
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
            var bossState = RequireObject("BossRoomState");
            var runtimeArena = DuelArenaRuntimeScaffold.EnsureArena(SceneManager.GetSceneByName("BossRoom"));

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

            var placement = DuelArenaRuntimeScaffold.Placement;
            RequireNavMesh(placement.PlayerPosition, "player");
            RequireNavMesh(placement.OpponentPosition, "opponent");

            if (!visibleBounds.Contains(runtimeArena.position))
            {
                throw new InvalidOperationException($"Runtime arena {runtimeArena.position} is outside visible arena bounds {visibleBounds}.");
            }

            var campaignBossPrefab = FindSerializedPropertyOnComponents(bossState, "m_CampaignBossPrefab");
            if (campaignBossPrefab == null || campaignBossPrefab.objectReferenceValue == null)
            {
                throw new MissingReferenceException("BossRoomState is missing m_CampaignBossPrefab for campaign duel boss spawning.");
            }

            Debug.Log(
                $"Duel arena verification passed. ArenaRoot={arenaRoot.name}, StaticRoot={staticObjects.name}, " +
                $"RuntimeArena={runtimeArena.position}, CampaignBossPrefab={campaignBossPrefab.objectReferenceValue.name}, " +
                $"VisibleRenderers={visibleRendererCount}, Bounds={visibleBounds}.");
        }

        static void RequireNavMesh(Vector3 position, string label)
        {
            if (!NavMesh.SamplePosition(position, out _, 1.5f, NavMesh.AllAreas))
            {
                throw new InvalidOperationException($"Runtime duel arena has no NavMesh for {label} near {position}.");
            }
        }

        static SerializedProperty FindSerializedPropertyOnComponents(GameObject gameObject, string propertyName)
        {
            foreach (var component in gameObject.GetComponents<MonoBehaviour>())
            {
                if (!component)
                {
                    continue;
                }

                var serializedObject = new SerializedObject(component);
                var property = serializedObject.FindProperty(propertyName);
                if (property != null)
                {
                    return property;
                }
            }

            return null;
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
