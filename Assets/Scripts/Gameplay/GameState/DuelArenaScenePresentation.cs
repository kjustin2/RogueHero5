using UnityEngine;
using UnityEngine.SceneManagement;

namespace Unity.BossRoom.Gameplay.GameState
{
    static class DuelArenaScenePresentation
    {
        const string k_DuelBossSubscene = "DungeonBossRoom";
        static bool s_DuelArenaLoaded;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void Install()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
            SceneManager.sceneLoaded += OnSceneLoaded;

            Camera.onPreCull -= OnCameraPreCull;
            Camera.onPreCull += OnCameraPreCull;
            s_DuelArenaLoaded = false;
        }

        static void OnSceneLoaded(Scene scene, LoadSceneMode loadSceneMode)
        {
            if (scene.name != k_DuelBossSubscene)
            {
                return;
            }

            s_DuelArenaLoaded = true;
            SceneManager.SetActiveScene(scene);
            DisableGameplayCameraOcclusion();

            int visibleRendererCount = 0;
            foreach (var renderer in Object.FindObjectsByType<Renderer>(FindObjectsSortMode.None))
            {
                if (renderer.gameObject.scene == scene && renderer.enabled && renderer.gameObject.activeInHierarchy)
                {
                    visibleRendererCount++;
                }
            }

            Debug.Log($"Duel arena presentation activated '{scene.name}' as active scene with {visibleRendererCount} visible renderers.");
        }

        static void OnCameraPreCull(Camera camera)
        {
            if (!s_DuelArenaLoaded || camera == null)
            {
                return;
            }

            camera.useOcclusionCulling = false;
        }

        static void DisableGameplayCameraOcclusion()
        {
            foreach (var camera in Object.FindObjectsByType<Camera>(FindObjectsSortMode.None))
            {
                camera.useOcclusionCulling = false;
            }
        }
    }
}
