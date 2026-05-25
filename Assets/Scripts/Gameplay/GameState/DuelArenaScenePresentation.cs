using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

namespace Unity.BossRoom.Gameplay.GameState
{
    static class DuelArenaScenePresentation
    {
        const string k_DuelBossSubscene = "DungeonBossRoom";
        const int k_WindowsHighQualityIndex = 2;
        const string k_RuntimeFillLightName = "DuelArenaRuntimeFillLight";
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
            EnsurePlayableQualityProfile();
            SceneManager.SetActiveScene(scene);
            DisableGameplayCameraOcclusion();

            var hasBounds = TryGetSceneRendererBounds(scene, out var visibleRendererCount, out var visibleBounds, out var layerSummary);
            EnsureArenaLighting(scene, hasBounds ? visibleBounds : default);
            LogPresentationState(scene, visibleRendererCount, hasBounds, visibleBounds, layerSummary);
        }

        static void EnsurePlayableQualityProfile()
        {
#if UNITY_EDITOR || UNITY_STANDALONE
            if (QualitySettings.names == null || QualitySettings.names.Length <= k_WindowsHighQualityIndex)
            {
                return;
            }

            var currentQuality = QualitySettings.GetQualityLevel();
            if (currentQuality == k_WindowsHighQualityIndex)
            {
                return;
            }

            var currentQualityName = currentQuality >= 0 && currentQuality < QualitySettings.names.Length
                ? QualitySettings.names[currentQuality]
                : "unknown";
            Debug.LogWarning(
                $"Duel arena switched quality from '{currentQualityName}' ({currentQuality}) " +
                $"to '{QualitySettings.names[k_WindowsHighQualityIndex]}' ({k_WindowsHighQualityIndex}) for the Windows arena renderer.");
            QualitySettings.SetQualityLevel(k_WindowsHighQualityIndex, true);
#endif
        }

        static bool TryGetSceneRendererBounds(Scene scene, out int visibleRendererCount, out Bounds visibleBounds, out string layerSummary)
        {
            visibleRendererCount = 0;
            var layerCounts = new int[32];
            var hasBounds = false;
            visibleBounds = default;

            foreach (var renderer in Object.FindObjectsByType<Renderer>(FindObjectsSortMode.None))
            {
                if (renderer.gameObject.scene == scene && renderer.enabled && renderer.gameObject.activeInHierarchy)
                {
                    visibleRendererCount++;
                    layerCounts[renderer.gameObject.layer]++;
                    if (!hasBounds)
                    {
                        visibleBounds = renderer.bounds;
                        hasBounds = true;
                    }
                    else
                    {
                        visibleBounds.Encapsulate(renderer.bounds);
                    }
                }
            }

            layerSummary = BuildLayerSummary(layerCounts);
            return hasBounds;
        }

        static string BuildLayerSummary(int[] layerCounts)
        {
            var summary = string.Empty;
            for (int i = 0; i < layerCounts.Length; i++)
            {
                if (layerCounts[i] == 0)
                {
                    continue;
                }

                var layerName = LayerMask.LayerToName(i);
                if (string.IsNullOrEmpty(layerName))
                {
                    layerName = i.ToString();
                }

                if (summary.Length > 0)
                {
                    summary += ", ";
                }

                summary += $"{layerName}:{layerCounts[i]}";
            }

            return summary.Length > 0 ? summary : "none";
        }

        static void EnsureArenaLighting(Scene scene, Bounds visibleBounds)
        {
            RenderSettings.ambientMode = AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = new Color(0.23f, 0.25f, 0.31f);
            RenderSettings.ambientEquatorColor = new Color(0.18f, 0.14f, 0.12f);
            RenderSettings.ambientGroundColor = new Color(0.08f, 0.06f, 0.05f);
            RenderSettings.ambientIntensity = 1f;

            var fillLight = GameObject.Find(k_RuntimeFillLightName);
            if (!fillLight)
            {
                fillLight = new GameObject(k_RuntimeFillLightName);
                SceneManager.MoveGameObjectToScene(fillLight, scene);
                var light = fillLight.AddComponent<Light>();
                light.type = LightType.Directional;
                light.color = new Color(1f, 0.82f, 0.62f);
                light.intensity = 1.1f;
                light.shadows = LightShadows.None;
                light.cullingMask = ~0;
            }

            if (visibleBounds.size != Vector3.zero)
            {
                fillLight.transform.position = visibleBounds.center + new Vector3(0f, visibleBounds.extents.y + 20f, 0f);
            }

            fillLight.transform.rotation = Quaternion.Euler(55f, -35f, 0f);
        }

        static void LogPresentationState(Scene scene, int visibleRendererCount, bool hasBounds, Bounds visibleBounds, string layerSummary)
        {
            var qualityIndex = QualitySettings.GetQualityLevel();
            var qualityName = QualitySettings.names != null && qualityIndex >= 0 && qualityIndex < QualitySettings.names.Length
                ? QualitySettings.names[qualityIndex]
                : "unknown";
            var pipelineName = QualitySettings.renderPipeline ? QualitySettings.renderPipeline.name : "default";
            var cameraSummary = BuildCameraSummary();
            var boundsSummary = hasBounds ? visibleBounds.ToString() : "none";

            Debug.Log(
                $"Duel arena presentation activated '{scene.name}'. " +
                $"Renderers={visibleRendererCount}, Bounds={boundsSummary}, Layers=[{layerSummary}], " +
                $"Quality={qualityName}({qualityIndex}), Pipeline={pipelineName}, Cameras=[{cameraSummary}].");
        }

        static string BuildCameraSummary()
        {
            var summary = string.Empty;
            foreach (var camera in Object.FindObjectsByType<Camera>(FindObjectsSortMode.None))
            {
                if (summary.Length > 0)
                {
                    summary += "; ";
                }

                summary +=
                    $"{camera.name}:pos={camera.transform.position},rot={camera.transform.eulerAngles}," +
                    $"mask={camera.cullingMask},occ={camera.useOcclusionCulling},near={camera.nearClipPlane},far={camera.farClipPlane}";
            }

            return summary.Length > 0 ? summary : "none";
        }

        static void OnCameraPreCull(Camera camera)
        {
            if (!s_DuelArenaLoaded || camera == null)
            {
                return;
            }

            camera.useOcclusionCulling = false;
            camera.cullingMask = ~0;
        }

        static void DisableGameplayCameraOcclusion()
        {
            foreach (var camera in Object.FindObjectsByType<Camera>(FindObjectsSortMode.None))
            {
                camera.useOcclusionCulling = false;
                camera.cullingMask = ~0;
            }
        }
    }
}
