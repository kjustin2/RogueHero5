using System;
using System.IO;
using System.Linq;
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
        const string k_MainMenuScenePath = "Assets/Scenes/MainMenu.unity";
        static readonly string[] k_EnabledBuildScenes =
        {
            "Assets/Scenes/Startup.unity",
            "Assets/Scenes/MainMenu.unity",
            "Assets/Scenes/BossRoom.unity"
        };

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
            RequireTinyBuildSettings();
            RequireMainMenuDefaultAvatar();

            EditorSceneManager.OpenScene(k_BaseScenePath, OpenSceneMode.Single);

            RequireSceneLoaded("BossRoom");

            var bossState = RequireObject("BossRoomState");
            var runtimeArena = DuelArenaRuntimeScaffold.EnsureArena(SceneManager.GetSceneByName("BossRoom"));

            var renderers = runtimeArena.GetComponentsInChildren<MeshRenderer>(true);
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

            if (!hasVisibleBounds || visibleRendererCount < 12)
            {
                throw new InvalidOperationException($"Expected the tiny runtime arena, found only {visibleRendererCount} visible mesh renderers.");
            }

            var placement = DuelArenaRuntimeScaffold.Placement;
            RequireNavMesh(placement.PlayerPosition, "player");
            RequireNavMesh(placement.OpponentPosition, "opponent");

            if (!visibleBounds.Contains(placement.PlayerPosition) || !visibleBounds.Contains(placement.OpponentPosition))
            {
                throw new InvalidOperationException($"Runtime arena bounds {visibleBounds} do not contain both duel spawn positions.");
            }

            var campaignBossPrefab = FindSerializedPropertyOnComponents(bossState, "m_CampaignBossPrefab");
            if (campaignBossPrefab == null || campaignBossPrefab.objectReferenceValue == null)
            {
                throw new MissingReferenceException("BossRoomState is missing m_CampaignBossPrefab for campaign duel boss spawning.");
            }

            Debug.Log(
                $"Tiny duel slice verification passed. RuntimeArena={runtimeArena.position}, CampaignBossPrefab={campaignBossPrefab.objectReferenceValue.name}, " +
                $"VisibleRenderers={visibleRendererCount}, Bounds={visibleBounds}.");
        }

        static void RequireTinyBuildSettings()
        {
            var enabledScenes = EditorBuildSettings.scenes
                .Where(scene => scene.enabled)
                .Select(scene => scene.path)
                .ToArray();

            if (!enabledScenes.SequenceEqual(k_EnabledBuildScenes))
            {
                throw new InvalidOperationException(
                    $"Expected enabled build scenes [{string.Join(", ", k_EnabledBuildScenes)}], " +
                    $"found [{string.Join(", ", enabledScenes)}].");
            }
        }

        static void RequireMainMenuDefaultAvatar()
        {
            EditorSceneManager.OpenScene(k_MainMenuScenePath, OpenSceneMode.Single);

            var menuState = RequireObject("MainMenuState");
            var defaultAvatar = FindSerializedPropertyOnComponents(menuState, "m_DefaultCampaignAvatar");
            if (defaultAvatar == null || defaultAvatar.objectReferenceValue == null)
            {
                throw new MissingReferenceException("MainMenuState is missing m_DefaultCampaignAvatar for fixed TankBoy campaign startup.");
            }

            if (defaultAvatar.objectReferenceValue.name != "TankBoy")
            {
                throw new InvalidOperationException($"Expected TankBoy as the fixed campaign avatar, found {defaultAvatar.objectReferenceValue.name}.");
            }
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
