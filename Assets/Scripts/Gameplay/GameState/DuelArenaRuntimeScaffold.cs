using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.SceneManagement;

namespace Unity.BossRoom.Gameplay.GameState
{
    public static class DuelArenaRuntimeScaffold
    {
        public const string ArenaRootName = "DuelArenaVerticalSlice";
        public const string CameraAnchorName = "DuelArenaCameraAnchor";

        static readonly Vector3 k_Center = new Vector3(0f, 0f, 0f);
        static readonly Vector3 k_PlayerPosition = new Vector3(-8f, 0f, 0f);
        static readonly Vector3 k_OpponentPosition = new Vector3(8f, 0f, 0f);
        static readonly Vector3 k_ArenaSize = new Vector3(30f, 0.35f, 22f);

        static GameObject s_ArenaRoot;
        static GameObject s_CameraAnchor;
        static NavMeshDataInstance s_NavMeshDataInstance;

        public static DuelArenaPlacement Placement => new DuelArenaPlacement(
            k_Center,
            k_PlayerPosition,
            k_OpponentPosition,
            $"{ArenaRootName} runtime arena");

        public static Transform EnsureArena(Scene targetScene = default)
        {
            if (s_ArenaRoot && s_ArenaRoot.scene.isLoaded)
            {
                return s_ArenaRoot.transform;
            }

            var existingRoot = GameObject.Find(ArenaRootName);
            if (existingRoot)
            {
                s_ArenaRoot = existingRoot;
                EnsureCameraAnchor(targetScene);
                return s_ArenaRoot.transform;
            }

            s_ArenaRoot = new GameObject(ArenaRootName);
            MoveToSceneIfValid(s_ArenaRoot, targetScene);

            var groundLayer = LayerMask.NameToLayer("Ground");
            if (groundLayer < 0)
            {
                groundLayer = 0;
            }

            var stoneMaterial = CreateMaterial("DuelArenaStone", new Color(0.45f, 0.38f, 0.33f));
            var rimMaterial = CreateMaterial("DuelArenaRim", new Color(0.16f, 0.13f, 0.15f));
            var accentMaterial = CreateMaterial("DuelArenaAccent", new Color(0.08f, 0.42f, 0.85f));

            var floor = CreateCube("DuelArenaFloor", k_Center + new Vector3(0f, -0.18f, 0f), k_ArenaSize, stoneMaterial, s_ArenaRoot.transform);
            floor.layer = groundLayer;
            floor.isStatic = true;

            CreateFloorTiles(stoneMaterial, accentMaterial, groundLayer);
            CreateArenaRim(rimMaterial);
            CreateCornerBeacons(accentMaterial);
            EnsureArenaLighting(targetScene);
            EnsureCameraAnchor(targetScene);
            BuildRuntimeNavMesh(groundLayer);

            Debug.Log(
                $"Duel arena runtime scaffold created at {k_Center}. " +
                $"Player={k_PlayerPosition}, Opponent={k_OpponentPosition}, NavMeshPlayer={HasNavMesh(k_PlayerPosition)}, NavMeshOpponent={HasNavMesh(k_OpponentPosition)}.");
            return s_ArenaRoot.transform;
        }

        public static Transform EnsureCameraAnchor(Scene targetScene = default)
        {
            if (s_CameraAnchor && s_CameraAnchor.scene.isLoaded)
            {
                return s_CameraAnchor.transform;
            }

            var existingAnchor = GameObject.Find(CameraAnchorName);
            if (existingAnchor)
            {
                s_CameraAnchor = existingAnchor;
                return s_CameraAnchor.transform;
            }

            s_CameraAnchor = new GameObject(CameraAnchorName);
            s_CameraAnchor.transform.SetPositionAndRotation(k_Center + new Vector3(0f, 1.1f, 0f), Quaternion.identity);
            MoveToSceneIfValid(s_CameraAnchor, targetScene);
            return s_CameraAnchor.transform;
        }

        static void CreateFloorTiles(Material stoneMaterial, Material accentMaterial, int groundLayer)
        {
            const int columns = 5;
            const int rows = 3;
            var tileSize = new Vector3(5.35f, 0.08f, 5.35f);
            var startX = -10.7f;
            var startZ = -5.35f;

            for (int z = 0; z < rows; z++)
            {
                for (int x = 0; x < columns; x++)
                {
                    var material = (x + z) % 2 == 0 ? stoneMaterial : accentMaterial;
                    var tile = CreateCube(
                        $"DuelArenaTile_{x}_{z}",
                        new Vector3(startX + (x * 5.35f), 0.03f, startZ + (z * 5.35f)),
                        tileSize,
                        material,
                        s_ArenaRoot.transform);
                    tile.layer = groundLayer;
                    tile.isStatic = true;
                }
            }
        }

        static void CreateArenaRim(Material rimMaterial)
        {
            CreateCube("DuelArenaNorthWall", new Vector3(0f, 0.55f, 11.3f), new Vector3(31f, 1.1f, 0.7f), rimMaterial, s_ArenaRoot.transform);
            CreateCube("DuelArenaSouthWall", new Vector3(0f, 0.55f, -11.3f), new Vector3(31f, 1.1f, 0.7f), rimMaterial, s_ArenaRoot.transform);
            CreateCube("DuelArenaWestWall", new Vector3(-15.3f, 0.55f, 0f), new Vector3(0.7f, 1.1f, 22f), rimMaterial, s_ArenaRoot.transform);
            CreateCube("DuelArenaEastWall", new Vector3(15.3f, 0.55f, 0f), new Vector3(0.7f, 1.1f, 22f), rimMaterial, s_ArenaRoot.transform);
        }

        static void CreateCornerBeacons(Material accentMaterial)
        {
            CreateBeacon(new Vector3(-13.3f, 1.2f, -9.3f), accentMaterial);
            CreateBeacon(new Vector3(-13.3f, 1.2f, 9.3f), accentMaterial);
            CreateBeacon(new Vector3(13.3f, 1.2f, -9.3f), accentMaterial);
            CreateBeacon(new Vector3(13.3f, 1.2f, 9.3f), accentMaterial);
        }

        static void CreateBeacon(Vector3 position, Material accentMaterial)
        {
            var beacon = CreateCube("DuelArenaBeacon", position, new Vector3(1.1f, 2.4f, 1.1f), accentMaterial, s_ArenaRoot.transform);
            var light = beacon.AddComponent<Light>();
            light.type = LightType.Point;
            light.color = new Color(0.28f, 0.68f, 1f);
            light.intensity = 3f;
            light.range = 9f;
            light.shadows = LightShadows.None;
        }

        static GameObject CreateCube(string name, Vector3 position, Vector3 scale, Material material, Transform parent)
        {
            var cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
            cube.name = name;
            cube.transform.SetParent(parent, false);
            cube.transform.SetPositionAndRotation(position, Quaternion.identity);
            cube.transform.localScale = scale;

            if (cube.TryGetComponent<Renderer>(out var renderer))
            {
                renderer.sharedMaterial = material;
            }

            return cube;
        }

        static Material CreateMaterial(string name, Color color)
        {
            var shader = Shader.Find("Universal Render Pipeline/Lit");
            if (!shader)
            {
                shader = Shader.Find("Standard");
            }

            var material = new Material(shader)
            {
                name = name,
                color = color
            };

            if (material.HasProperty("_BaseColor"))
            {
                material.SetColor("_BaseColor", color);
            }

            return material;
        }

        static void EnsureArenaLighting(Scene targetScene)
        {
            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = new Color(0.18f, 0.2f, 0.25f);
            RenderSettings.ambientEquatorColor = new Color(0.17f, 0.13f, 0.11f);
            RenderSettings.ambientGroundColor = new Color(0.06f, 0.05f, 0.05f);
            RenderSettings.ambientIntensity = 1.15f;

            var keyLight = new GameObject("DuelArenaKeyLight");
            MoveToSceneIfValid(keyLight, targetScene);
            keyLight.transform.SetPositionAndRotation(new Vector3(-8f, 12f, -8f), Quaternion.Euler(58f, -35f, 0f));
            var light = keyLight.AddComponent<Light>();
            light.type = LightType.Directional;
            light.color = new Color(1f, 0.86f, 0.65f);
            light.intensity = 1.35f;
            light.shadows = LightShadows.Soft;
        }

        static void BuildRuntimeNavMesh(int groundLayer)
        {
            if (s_NavMeshDataInstance.valid)
            {
                NavMesh.RemoveNavMeshData(s_NavMeshDataInstance);
            }

            var sources = new List<NavMeshBuildSource>();
            var markups = new List<NavMeshBuildMarkup>();
            var bounds = new Bounds(k_Center, new Vector3(36f, 8f, 28f));
            NavMeshBuilder.CollectSources(
                s_ArenaRoot.transform,
                1 << groundLayer,
                NavMeshCollectGeometry.PhysicsColliders,
                0,
                markups,
                sources);

            var settings = NavMesh.GetSettingsCount() > 0 ? NavMesh.GetSettingsByIndex(0) : new NavMeshBuildSettings();
            var navMeshData = NavMeshBuilder.BuildNavMeshData(settings, sources, bounds, Vector3.zero, Quaternion.identity);
            if (navMeshData != null)
            {
                s_NavMeshDataInstance = NavMesh.AddNavMeshData(navMeshData);
            }
        }

        static bool HasNavMesh(Vector3 position)
        {
            return NavMesh.SamplePosition(position, out _, 1.5f, NavMesh.AllAreas);
        }

        static void MoveToSceneIfValid(GameObject gameObject, Scene targetScene)
        {
            if (targetScene.IsValid() && targetScene.isLoaded)
            {
                SceneManager.MoveGameObjectToScene(gameObject, targetScene);
            }
        }

        public readonly struct DuelArenaPlacement
        {
            public DuelArenaPlacement(Vector3 center, Vector3 playerPosition, Vector3 opponentPosition, string source)
            {
                Center = center;
                PlayerPosition = playerPosition;
                OpponentPosition = opponentPosition;
                Source = source;
            }

            public Vector3 Center { get; }
            public Vector3 PlayerPosition { get; }
            public Vector3 OpponentPosition { get; }
            public string Source { get; }
        }
    }
}
