using System.Collections.Generic;
using RogueHero5;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace RogueHero5.Editor
{
    public static class RogueHero5SceneBuilder
    {
        private const string RootFolder = "Assets/RogueHero5";
        private const string DataFolder = RootFolder + "/Data";
        private const string MaterialsFolder = RootFolder + "/Materials";
        private const string ScenesFolder = RootFolder + "/Scenes";
        private const string GeneratedFolder = RootFolder + "/Generated";
        private const string PresentationFolder = DataFolder + "/Presentation";
        private const string ScenePath = ScenesFolder + "/DuelArena.unity";

        [MenuItem("Rogue Hero 5/Build/Rebuild Playable Duel Scene")]
        public static void RebuildPlayableDuelScene()
        {
            EnsureFolders();

            Material floorMaterial = CreateMaterial("Arena_Floor", new Color(0.10f, 0.12f, 0.14f, 1f));
            Material borderMaterial = CreateMaterial("Arena_Border", new Color(0.24f, 0.27f, 0.31f, 1f));
            Material playerMaterial = CreateMaterial("Player_Blue", new Color(0.10f, 0.50f, 1.00f, 1f));
            Material bossMaterial = CreateMaterial("Duelist_Red", new Color(0.85f, 0.12f, 0.08f, 1f));
            Material telegraphMaterial = CreateMaterial("Telegraph_Red", new Color(1.00f, 0.05f, 0.02f, 0.58f));
            Material projectileMaterial = CreateMaterial("Void_Spear", new Color(0.50f, 0.35f, 1.00f, 1f));
            Material generatedFloor = AssetDatabase.LoadAssetAtPath<Material>("Assets/RogueHero5/Generated/Materials/ArcaneArenaFloor.mat");
            if (generatedFloor != null)
            {
                floorMaterial = generatedFloor;
            }

            MoveDefinition[] moves = CreateMoveDefinitions();
            AudioClip combatSfx = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/RogueHero5/Generated/Audio/ArcaneCombatSfx.wav");
            AbilityPresentationDefinition[] abilityPresentations = CreateAbilityPresentations(combatSfx);
            BossAttackDefinition[] bossAttacks = CreateBossAttacks(combatSfx);
            DuelSlicePresentationProfile profile = CreatePresentationProfile(abilityPresentations, bossAttacks, floorMaterial, borderMaterial, telegraphMaterial, playerMaterial, bossMaterial, combatSfx);

            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            scene.name = "DuelArena";

            CreateLighting();
            ApplyGeneratedSkybox();
            CreateArena(floorMaterial, borderMaterial);

            GameObject player = CreateFighter("Player", new Vector3(0f, 1f, -5.5f), playerMaterial, ActorTeam.Player, 100, 0.65f, true);
            GameObject boss = CreateFighter("The Duelist", new Vector3(0f, 1.15f, 4.8f), bossMaterial, ActorTeam.Enemy, 220, 0.85f, false);

            FighterActor playerActor = player.GetComponent<FighterActor>();
            FighterActor bossActor = boss.GetComponent<FighterActor>();

            Camera camera = CreateCamera(player.transform, out ThirdPersonCameraRig cameraRig);
            cameraRig.SetSecondaryTarget(boss.transform);

            Image screenFlash;
            HudController hud = CreateHud(playerActor.Health, bossActor.Health, null, abilityPresentations, out screenFlash);

            GameObject audioObject = new GameObject("Duel Audio");
            AudioSource sfxSource = audioObject.AddComponent<AudioSource>();
            sfxSource.playOnAwake = false;
            AudioSource ambientSource = audioObject.AddComponent<AudioSource>();
            ambientSource.clip = combatSfx;
            ambientSource.loop = true;
            ambientSource.volume = 0.12f;
            ambientSource.playOnAwake = combatSfx != null;

            GameObject feedbackObject = new GameObject("Combat Feedback");
            CombatFeedbackService feedback = feedbackObject.AddComponent<CombatFeedbackService>();
            feedback.Configure(abilityPresentations, sfxSource, screenFlash, cameraRig);

            MoveRunner moveRunner = player.AddComponent<MoveRunner>();
            moveRunner.Configure(playerActor, moves, projectileMaterial, feedback);
            hud.ConfigureRuntimeMoveRunner(moveRunner);

            ThirdPersonPlayerController playerController = player.AddComponent<ThirdPersonPlayerController>();
            playerController.Configure(playerActor, moveRunner, cameraRig, camera);

            DuelistBossController bossController = boss.AddComponent<DuelistBossController>();
            bossController.Configure(bossActor, playerActor, telegraphMaterial, 55, bossAttacks);

            GameObject directorObject = new GameObject("Fight Director");
            FightDirector director = directorObject.AddComponent<FightDirector>();
            director.Configure(playerActor.Health, bossActor.Health, playerController, hud, profile, sfxSource);

            camera.tag = "MainCamera";
            Selection.activeGameObject = player;

            EditorSceneManager.SaveScene(scene, ScenePath);
            RegisterSceneInBuildSettings();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log($"Rogue Hero 5 playable duel scene rebuilt: {ScenePath}");
        }

        [MenuItem("Rogue Hero 5/Build/Rebuild Duel Scene And Play")]
        public static void RebuildPlayableDuelSceneAndPlay()
        {
            RebuildPlayableDuelScene();
            EditorSceneManager.OpenScene(ScenePath);
            EditorApplication.delayCall += EnterPlayModeWhenReady;
        }

        private static void EnterPlayModeWhenReady()
        {
            if (EditorApplication.isCompiling || EditorApplication.isUpdating)
            {
                EditorApplication.delayCall += EnterPlayModeWhenReady;
                return;
            }

            EditorApplication.isPlaying = true;
            Debug.Log("Rogue Hero 5 duel scene entered Play Mode.");
        }

        private static void EnsureFolders()
        {
            EnsureFolder("Assets", "RogueHero5");
            EnsureFolder(RootFolder, "Data");
            EnsureFolder(DataFolder, "Presentation");
            EnsureFolder(RootFolder, "Materials");
            EnsureFolder(RootFolder, "Scenes");
            EnsureFolder(RootFolder, "Generated");
            EnsureFolder(GeneratedFolder, "Audio");
            EnsureFolder(GeneratedFolder, "Concept");
            EnsureFolder(GeneratedFolder, "Materials");
            EnsureFolder(GeneratedFolder, "Models");
            EnsureFolder(GeneratedFolder, "Skyboxes");
            EnsureFolder(GeneratedFolder, "UI");
        }

        private static void EnsureFolder(string parent, string child)
        {
            string path = parent + "/" + child;
            if (!AssetDatabase.IsValidFolder(path))
            {
                AssetDatabase.CreateFolder(parent, child);
            }
        }

        private static Material CreateMaterial(string name, Color color)
        {
            string path = $"{MaterialsFolder}/{name}.mat";
            Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material == null)
            {
                Shader shader = Shader.Find("Universal Render Pipeline/Lit");
                if (shader == null)
                {
                    shader = Shader.Find("Standard");
                }

                material = new Material(shader);
                AssetDatabase.CreateAsset(material, path);
            }

            material.color = color;
            if (color.a < 0.99f)
            {
                material.SetFloat("_Surface", 1f);
                material.SetFloat("_AlphaClip", 0f);
                material.SetFloat("_SrcBlend", (float)UnityEngine.Rendering.BlendMode.SrcAlpha);
                material.SetFloat("_DstBlend", (float)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                material.SetFloat("_ZWrite", 0f);
                material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
                material.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
            }

            EditorUtility.SetDirty(material);
            return material;
        }

        private static MoveDefinition[] CreateMoveDefinitions()
        {
            return new[]
            {
                CreateMove("SpearDash", "Spear Dash", MoveSlot.Primary, MoveKind.DashStrike, 1.20f, 0.07f, 0.16f, 0.18f, 18, 3.7f, 0.85f, 3.9f, 0.16f, 0f, 0.08f, new Color(0.25f, 0.80f, 1f, 1f)),
                CreateMove("VoidSpear", "Void Spear", MoveSlot.Secondary, MoveKind.Projectile, 1.55f, 0.12f, 0.08f, 0.16f, 15, 12f, 0.65f, 0f, 0f, 18f, 0f, new Color(0.56f, 0.36f, 1f, 1f)),
                CreateMove("ForwardDodge", "Forward Dodge", MoveSlot.Mobility, MoveKind.Dodge, 1.05f, 0f, 0.16f, 0.12f, 0, 0f, 0.6f, 3.2f, 0.16f, 0f, 0.34f, new Color(0.75f, 0.85f, 1f, 1f)),
                CreateMove("CounterBurst", "Counter Burst", MoveSlot.Defensive, MoveKind.CounterBurst, 2.30f, 0.04f, 0.20f, 0.18f, 20, 0f, 2.0f, 0f, 0f, 0f, 0.32f, new Color(1f, 0.88f, 0.35f, 1f)),
                CreateMove("MeteorKick", "Meteor Kick", MoveSlot.Ultimate, MoveKind.LeapSlam, 5.50f, 0.18f, 0.14f, 0.34f, 38, 0f, 2.25f, 4.4f, 0.28f, 0f, 0.12f, new Color(1f, 0.38f, 0.12f, 1f))
            };
        }

        private static MoveDefinition CreateMove(
            string id,
            string displayName,
            MoveSlot slot,
            MoveKind kind,
            float cooldown,
            float startup,
            float active,
            float recovery,
            int damage,
            float range,
            float radius,
            float movementDistance,
            float movementSeconds,
            float projectileSpeed,
            float invulnerability,
            Color color)
        {
            string path = $"{DataFolder}/{id}.asset";
            MoveDefinition definition = AssetDatabase.LoadAssetAtPath<MoveDefinition>(path);
            if (definition == null)
            {
                definition = ScriptableObject.CreateInstance<MoveDefinition>();
                AssetDatabase.CreateAsset(definition, path);
            }

            definition.Configure(id, displayName, slot, kind, cooldown, startup, active, recovery, damage, range, radius, movementDistance, movementSeconds, projectileSpeed, invulnerability, color);
            bool isCommitMove = kind == MoveKind.DashStrike || kind == MoveKind.Dodge || kind == MoveKind.LeapSlam;
            definition.ConfigureCommitment(isCommitMove, isCommitMove, 0.16f);
            EditorUtility.SetDirty(definition);
            return definition;
        }

        private static AbilityPresentationDefinition[] CreateAbilityPresentations(AudioClip combatSfx)
        {
            return new[]
            {
                CreateAbilityPresentation("SpearDash_Presentation", MoveSlot.Primary, "Spear Dash", "Assets/RogueHero5/Generated/UI/Icon_SpearDash.png", new Color(0.20f, 0.72f, 1f, 1f), new Color(0.65f, 0.95f, 1f, 1f), combatSfx, 0.8f, 1.15f, 0.18f, 0.035f, 0.08f, 0.85f),
                CreateAbilityPresentation("VoidSpear_Presentation", MoveSlot.Secondary, "Void Spear", "Assets/RogueHero5/Generated/UI/Icon_VoidSpear.png", new Color(0.52f, 0.32f, 1f, 1f), new Color(0.82f, 0.62f, 1f, 1f), combatSfx, 0.7f, 1.0f, 0.10f, 0.025f, 0.055f, 0.55f),
                CreateAbilityPresentation("ForwardDodge_Presentation", MoveSlot.Mobility, "Dodge", "Assets/RogueHero5/Generated/UI/Icon_Dodge.png", new Color(0.72f, 0.88f, 1f, 1f), new Color(0.95f, 1f, 1f, 1f), combatSfx, 0.65f, 0.7f, 0.20f, 0.0f, 0.03f, 0.35f),
                CreateAbilityPresentation("CounterBurst_Presentation", MoveSlot.Defensive, "Counter", "Assets/RogueHero5/Generated/UI/Icon_CounterBurst.png", new Color(1f, 0.78f, 0.22f, 1f), new Color(1f, 0.96f, 0.55f, 1f), combatSfx, 1.2f, 2.0f, 0.12f, 0.045f, 0.12f, 1.1f),
                CreateAbilityPresentation("MeteorKick_Presentation", MoveSlot.Ultimate, "Meteor Kick", "Assets/RogueHero5/Generated/UI/Icon_MeteorKick.png", new Color(1f, 0.42f, 0.10f, 1f), new Color(1f, 0.76f, 0.20f, 1f), combatSfx, 0.9f, 2.35f, 0.24f, 0.06f, 0.18f, 1.4f)
            };
        }

        private static AbilityPresentationDefinition CreateAbilityPresentation(
            string assetName,
            MoveSlot slot,
            string displayName,
            string iconPath,
            Color primaryColor,
            Color secondaryColor,
            AudioClip combatSfx,
            float castRadius,
            float impactRadius,
            float trailSeconds,
            float hitStopSeconds,
            float cameraShake,
            float fovKick)
        {
            string path = $"{PresentationFolder}/{assetName}.asset";
            AbilityPresentationDefinition definition = AssetDatabase.LoadAssetAtPath<AbilityPresentationDefinition>(path);
            if (definition == null)
            {
                definition = ScriptableObject.CreateInstance<AbilityPresentationDefinition>();
                AssetDatabase.CreateAsset(definition, path);
            }

            Sprite icon = AssetDatabase.LoadAssetAtPath<Sprite>(iconPath);
            definition.Configure(slot, displayName, icon, primaryColor, secondaryColor, combatSfx, combatSfx, castRadius, impactRadius, trailSeconds, hitStopSeconds, cameraShake, fovKick);
            EditorUtility.SetDirty(definition);
            return definition;
        }

        private static BossAttackDefinition[] CreateBossAttacks(AudioClip combatSfx)
        {
            return new[]
            {
                CreateBossAttack("DashSlash_Attack", "dash_slash", "Dash Slash", BossAttackKind.DashSlash, 0.72f, 0.20f, 0.52f, 14, 5.5f, 0.85f, new Color(1f, 0.05f, 0.02f, 0.7f), new Color(1f, 0.18f, 0.08f, 1f), combatSfx),
                CreateBossAttack("TripleCut_Attack", "triple_cut", "Triple Cut", BossAttackKind.Combo, 0.40f, 0.92f, 0.66f, 9, 1.65f, 1.05f, new Color(1f, 0.20f, 0.08f, 0.65f), new Color(1f, 0.45f, 0.15f, 1f), combatSfx),
                CreateBossAttack("ArenaLunge_Attack", "arena_lunge", "Arena Lunge", BossAttackKind.Lunge, 1.05f, 0.30f, 0.78f, 22, 8.0f, 0.95f, new Color(1f, 0.02f, 0.15f, 0.72f), new Color(1f, 0.10f, 0.20f, 1f), combatSfx)
            };
        }

        private static BossAttackDefinition CreateBossAttack(
            string assetName,
            string attackId,
            string displayName,
            BossAttackKind kind,
            float telegraph,
            float active,
            float recovery,
            int damage,
            float range,
            float radius,
            Color telegraphColor,
            Color impactColor,
            AudioClip combatSfx)
        {
            string path = $"{PresentationFolder}/{assetName}.asset";
            BossAttackDefinition definition = AssetDatabase.LoadAssetAtPath<BossAttackDefinition>(path);
            if (definition == null)
            {
                definition = ScriptableObject.CreateInstance<BossAttackDefinition>();
                AssetDatabase.CreateAsset(definition, path);
            }

            definition.Configure(attackId, displayName, kind, telegraph, active, recovery, damage, range, radius, telegraphColor, impactColor, combatSfx, combatSfx);
            EditorUtility.SetDirty(definition);
            return definition;
        }

        private static DuelSlicePresentationProfile CreatePresentationProfile(
            AbilityPresentationDefinition[] abilities,
            BossAttackDefinition[] bossAttacks,
            Material floor,
            Material border,
            Material telegraph,
            Material player,
            Material boss,
            AudioClip combatSfx)
        {
            string path = $"{PresentationFolder}/DuelSlicePresentationProfile.asset";
            DuelSlicePresentationProfile profile = AssetDatabase.LoadAssetAtPath<DuelSlicePresentationProfile>(path);
            if (profile == null)
            {
                profile = ScriptableObject.CreateInstance<DuelSlicePresentationProfile>();
                AssetDatabase.CreateAsset(profile, path);
            }

            profile.Configure(abilities, bossAttacks, floor, border, telegraph, player, boss, null, combatSfx, combatSfx, 12f, new Color(0.06f, 0.08f, 0.11f, 1f));
            EditorUtility.SetDirty(profile);
            return profile;
        }

        private static void CreateLighting()
        {
            GameObject lightObject = new GameObject("Key Light");
            Light light = lightObject.AddComponent<Light>();
            light.type = LightType.Directional;
            light.intensity = 2.0f;
            light.shadows = LightShadows.Soft;
            lightObject.transform.rotation = Quaternion.Euler(48f, -35f, 0f);

            GameObject rimObject = new GameObject("Cool Rim Light");
            Light rim = rimObject.AddComponent<Light>();
            rim.type = LightType.Directional;
            rim.color = new Color(0.35f, 0.62f, 1f, 1f);
            rim.intensity = 0.7f;
            rimObject.transform.rotation = Quaternion.Euler(22f, 150f, 0f);

            RenderSettings.ambientLight = new Color(0.08f, 0.10f, 0.13f);
            RenderSettings.fog = true;
            RenderSettings.fogColor = new Color(0.035f, 0.045f, 0.060f);
            RenderSettings.fogDensity = 0.014f;
        }

        private static void ApplyGeneratedSkybox()
        {
            string path = $"{MaterialsFolder}/ArcaneVoidSkybox.mat";
            Cubemap cubemap = AssetDatabase.LoadAssetAtPath<Cubemap>("Assets/RogueHero5/Generated/Skyboxes/ArcaneVoidArena.png");
            Texture2D panorama = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/RogueHero5/Generated/Skyboxes/ArcaneVoidArena.png");
            Shader shader = cubemap != null ? Shader.Find("Skybox/Cubemap") : Shader.Find("Skybox/Panoramic");
            if (shader == null || (cubemap == null && panorama == null))
            {
                return;
            }

            Material skybox = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (skybox == null)
            {
                skybox = new Material(shader);
                AssetDatabase.CreateAsset(skybox, path);
            }

            skybox.shader = shader;
            if (cubemap != null)
            {
                skybox.SetTexture("_Tex", cubemap);
            }
            else
            {
                skybox.SetTexture("_MainTex", panorama);
                skybox.SetFloat("_Exposure", 0.72f);
                skybox.SetColor("_Tint", new Color(0.42f, 0.58f, 0.72f, 1f));
            }

            RenderSettings.skybox = skybox;
            EditorUtility.SetDirty(skybox);
        }

        private static void CreateArena(Material floorMaterial, Material borderMaterial)
        {
            GameObject floor = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            floor.name = "Round Arena Floor";
            floor.transform.position = Vector3.zero;
            floor.transform.localScale = new Vector3(13.5f, 0.08f, 13.5f);
            SetMaterial(floor, floorMaterial);
            floor.AddComponent<ArcaneArenaPulse>();

            for (int i = 0; i < 28; i++)
            {
                float angle = i * Mathf.PI * 2f / 28f;
                Vector3 position = new Vector3(Mathf.Cos(angle) * 13.2f, 0.55f, Mathf.Sin(angle) * 13.2f);
                GameObject pillar = GameObject.CreatePrimitive(PrimitiveType.Cube);
                pillar.name = "Arena Boundary";
                pillar.transform.position = position;
                pillar.transform.rotation = Quaternion.Euler(0f, -angle * Mathf.Rad2Deg, 0f);
                pillar.transform.localScale = i % 4 == 0 ? new Vector3(0.8f, 1.9f, 0.35f) : new Vector3(0.9f, 0.9f, 0.28f);
                SetMaterial(pillar, borderMaterial);
            }

            GameObject centerLine = GameObject.CreatePrimitive(PrimitiveType.Cube);
            centerLine.name = "Center Duel Line";
            centerLine.transform.position = new Vector3(0f, 0.07f, 0f);
            centerLine.transform.localScale = new Vector3(0.08f, 0.02f, 12f);
            SetMaterial(centerLine, borderMaterial);

            for (int i = 0; i < 16; i++)
            {
                float angle = i * Mathf.PI * 2f / 16f;
                GameObject glyph = GameObject.CreatePrimitive(PrimitiveType.Cube);
                glyph.name = "Arcane Floor Glyph";
                glyph.transform.position = new Vector3(Mathf.Cos(angle) * 6.5f, 0.11f, Mathf.Sin(angle) * 6.5f);
                glyph.transform.rotation = Quaternion.Euler(0f, -angle * Mathf.Rad2Deg + 90f, 0f);
                glyph.transform.localScale = new Vector3(0.08f, 0.018f, 1.15f);
                SetMaterial(glyph, borderMaterial);
            }
        }

        private static GameObject CreateFighter(string name, Vector3 position, Material material, ActorTeam team, int maxHealth, float radius, bool isPlayer)
        {
            GameObject fighter = new GameObject(name);
            fighter.name = name;
            fighter.transform.position = position;

            Rigidbody rigidbody = fighter.AddComponent<Rigidbody>();
            rigidbody.isKinematic = true;
            rigidbody.useGravity = false;

            CapsuleCollider capsule = fighter.AddComponent<CapsuleCollider>();
            capsule.radius = radius;
            capsule.height = isPlayer ? 2.0f : 2.35f;
            capsule.center = Vector3.zero;

            fighter.AddComponent<InvulnerabilityWindow>();
            Health health = fighter.AddComponent<Health>();
            health.Initialize(maxHealth);

            FighterActor actor = fighter.AddComponent<FighterActor>();
            actor.Configure(team, radius);

            GameObject generatedPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(isPlayer ? "Assets/RogueHero5/Generated/Models/PlayerArcDuelist.prefab" : "Assets/RogueHero5/Generated/Models/TheDuelistBoss.prefab");
            if (generatedPrefab != null)
            {
                GameObject visual = (GameObject)PrefabUtility.InstantiatePrefab(generatedPrefab);
                visual.name = isPlayer ? "Player Generated Visual" : "Duelist Generated Visual";
                visual.transform.SetParent(fighter.transform, false);
                visual.transform.localPosition = Vector3.zero;
                visual.transform.localRotation = Quaternion.identity;
                visual.transform.localScale = Vector3.one * (isPlayer ? 1.0f : 1.18f);
            }
            else
            {
                CreateProceduralFighterVisual(fighter.transform, material, isPlayer);
            }

            return fighter;
        }

        private static void CreateProceduralFighterVisual(Transform parent, Material material, bool isPlayer)
        {
            GameObject body = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            body.name = isPlayer ? "Player Body" : "Duelist Body";
            body.transform.SetParent(parent, false);
            body.transform.localPosition = Vector3.zero;
            body.transform.localScale = isPlayer ? Vector3.one : new Vector3(1.15f, 1.18f, 1.15f);
            SetMaterial(body, material);
            Object.DestroyImmediate(body.GetComponent<Collider>());

            GameObject weapon = GameObject.CreatePrimitive(PrimitiveType.Cube);
            weapon.name = isPlayer ? "Arc Spear" : "Duelist Blade";
            weapon.transform.SetParent(parent, false);
            weapon.transform.localPosition = isPlayer ? new Vector3(0.55f, 0.12f, 0.25f) : new Vector3(-0.60f, 0.15f, 0.25f);
            weapon.transform.localRotation = Quaternion.Euler(isPlayer ? 18f : -18f, 0f, isPlayer ? 16f : -20f);
            weapon.transform.localScale = isPlayer ? new Vector3(0.08f, 0.08f, 2.2f) : new Vector3(0.12f, 0.08f, 1.8f);
            SetMaterial(weapon, material);
            Object.DestroyImmediate(weapon.GetComponent<Collider>());

            GameObject shoulder = GameObject.CreatePrimitive(PrimitiveType.Cube);
            shoulder.name = isPlayer ? "Clean Shoulder Accent" : "Sharp Shoulder Accent";
            shoulder.transform.SetParent(parent, false);
            shoulder.transform.localPosition = new Vector3(0f, 0.55f, 0f);
            shoulder.transform.localScale = isPlayer ? new Vector3(1.1f, 0.12f, 0.42f) : new Vector3(1.35f, 0.16f, 0.5f);
            SetMaterial(shoulder, material);
            Object.DestroyImmediate(shoulder.GetComponent<Collider>());
        }

        private static Camera CreateCamera(Transform target, out ThirdPersonCameraRig rig)
        {
            GameObject cameraObject = new GameObject("Third Person Camera");
            Camera camera = cameraObject.AddComponent<Camera>();
            camera.fieldOfView = 60f;
            camera.nearClipPlane = 0.1f;
            camera.farClipPlane = 120f;
            camera.allowHDR = true;
            cameraObject.AddComponent<AudioListener>();
            rig = cameraObject.AddComponent<ThirdPersonCameraRig>();
            rig.Configure(target);
            cameraObject.transform.position = new Vector3(0f, 5f, -13f);
            cameraObject.transform.LookAt(target.position + Vector3.up * 1.2f);
            return camera;
        }

        private static HudController CreateHud(Health playerHealth, Health bossHealth, MoveRunner moveRunner, AbilityPresentationDefinition[] presentations, out Image screenFlash)
        {
            Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            if (font == null)
            {
                font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            }

            GameObject canvasObject = new GameObject("HUD");
            Canvas canvas = canvasObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            CanvasScaler scaler = canvasObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            canvasObject.AddComponent<GraphicRaycaster>();

            Image playerFill = CreateBar(canvasObject.transform, "Player Health Fill", new Vector2(32f, -36f), new Vector2(520f, 24f), new Color(0.12f, 0.55f, 1f, 1f));
            Image bossFill = CreateBar(canvasObject.transform, "Boss Health Fill", new Vector2(-552f, -36f), new Vector2(520f, 24f), new Color(0.9f, 0.1f, 0.08f, 1f), true);

            Text playerText = CreateText(canvasObject.transform, "Player Health Text", font, new Vector2(32f, -68f), new Vector2(520f, 34f), TextAnchor.MiddleLeft, 20, Color.white);
            Text bossText = CreateText(canvasObject.transform, "Boss Health Text", font, new Vector2(-552f, -68f), new Vector2(520f, 34f), TextAnchor.MiddleRight, 20, Color.white, true);
            Text cooldownText = CreateText(canvasObject.transform, "Move Cooldowns", font, new Vector2(0f, 84f), new Vector2(980f, 36f), TextAnchor.LowerCenter, 16, new Color(0.70f, 0.78f, 0.90f, 0.85f));
            Text messageText = CreateText(canvasObject.transform, "Fight Message", font, new Vector2(0f, -116f), new Vector2(900f, 54f), TextAnchor.MiddleCenter, 30, Color.white);

            AbilityHudSlot[] abilitySlots = CreateAbilityBar(canvasObject.transform, font, presentations);
            screenFlash = CreateScreenFlash(canvasObject.transform);

            HudController hud = canvasObject.AddComponent<HudController>();
            hud.Configure(playerHealth, bossHealth, moveRunner, playerFill, bossFill, playerText, bossText, cooldownText, messageText, abilitySlots);
            return hud;
        }

        private static AbilityHudSlot[] CreateAbilityBar(Transform parent, Font font, AbilityPresentationDefinition[] presentations)
        {
            AbilityHudSlot[] slots = new AbilityHudSlot[presentations.Length];
            float totalWidth = presentations.Length * 92f;
            for (int i = 0; i < presentations.Length; i++)
            {
                AbilityPresentationDefinition presentation = presentations[i];
                float x = -totalWidth * 0.5f + i * 92f + 46f;

                GameObject root = new GameObject(presentation.DisplayName + " HUD Slot");
                root.transform.SetParent(parent, false);
                RectTransform rootRect = root.AddComponent<RectTransform>();
                rootRect.anchorMin = new Vector2(0.5f, 0f);
                rootRect.anchorMax = new Vector2(0.5f, 0f);
                rootRect.pivot = new Vector2(0.5f, 0f);
                rootRect.anchoredPosition = new Vector2(x, 24f);
                rootRect.sizeDelta = new Vector2(76f, 96f);

                Image back = root.AddComponent<Image>();
                back.color = new Color(0.02f, 0.03f, 0.045f, 0.82f);

                GameObject iconObject = new GameObject("Icon");
                iconObject.transform.SetParent(root.transform, false);
                Image icon = iconObject.AddComponent<Image>();
                icon.sprite = presentation.Icon;
                icon.color = presentation.Icon == null ? presentation.PrimaryColor : Color.white;
                RectTransform iconRect = iconObject.GetComponent<RectTransform>();
                iconRect.anchorMin = new Vector2(0.5f, 1f);
                iconRect.anchorMax = new Vector2(0.5f, 1f);
                iconRect.pivot = new Vector2(0.5f, 1f);
                iconRect.anchoredPosition = new Vector2(0f, -8f);
                iconRect.sizeDelta = new Vector2(56f, 56f);

                GameObject fillObject = new GameObject("Cooldown Fill");
                fillObject.transform.SetParent(iconObject.transform, false);
                Image fill = fillObject.AddComponent<Image>();
                fill.color = new Color(0f, 0f, 0f, 0.65f);
                fill.type = Image.Type.Filled;
                fill.fillMethod = Image.FillMethod.Radial360;
                fill.fillAmount = 0f;
                RectTransform fillRect = fillObject.GetComponent<RectTransform>();
                fillRect.anchorMin = Vector2.zero;
                fillRect.anchorMax = Vector2.one;
                fillRect.offsetMin = Vector2.zero;
                fillRect.offsetMax = Vector2.zero;

                Text key = CreateChildText(root.transform, "Key", font, new Vector2(0f, 16f), new Vector2(76f, 18f), TextAnchor.MiddleCenter, 14, new Color(0.86f, 0.90f, 1f, 0.95f));
                key.text = KeyLabel(presentation.Slot);

                Text cooldown = CreateChildText(root.transform, "Cooldown", font, new Vector2(0f, 48f), new Vector2(76f, 24f), TextAnchor.MiddleCenter, 18, Color.white);

                slots[i] = new AbilityHudSlot
                {
                    Slot = presentation.Slot,
                    Icon = icon,
                    CooldownFill = fill,
                    Label = key,
                    CooldownText = cooldown,
                    Presentation = presentation
                };
            }

            return slots;
        }

        private static string KeyLabel(MoveSlot slot)
        {
            switch (slot)
            {
                case MoveSlot.Primary:
                    return "LMB";
                case MoveSlot.Secondary:
                    return "RMB";
                case MoveSlot.Mobility:
                    return "SPACE";
                case MoveSlot.Defensive:
                    return "E";
                case MoveSlot.Ultimate:
                    return "R";
                default:
                    return string.Empty;
            }
        }

        private static Text CreateChildText(Transform parent, string name, Font font, Vector2 anchoredPosition, Vector2 size, TextAnchor alignment, int fontSize, Color color)
        {
            GameObject textObject = new GameObject(name);
            textObject.transform.SetParent(parent, false);
            Text text = textObject.AddComponent<Text>();
            text.font = font;
            text.fontSize = fontSize;
            text.color = color;
            text.alignment = alignment;
            text.raycastTarget = false;
            RectTransform rect = textObject.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0f);
            rect.anchorMax = new Vector2(0.5f, 0f);
            rect.pivot = new Vector2(0.5f, 0f);
            rect.anchoredPosition = anchoredPosition;
            rect.sizeDelta = size;
            return text;
        }

        private static Image CreateScreenFlash(Transform parent)
        {
            GameObject flash = new GameObject("Screen Flash");
            flash.transform.SetParent(parent, false);
            Image image = flash.AddComponent<Image>();
            image.color = Color.clear;
            image.raycastTarget = false;
            RectTransform rect = flash.GetComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            return image;
        }

        private static Image CreateBar(Transform parent, string name, Vector2 anchoredPosition, Vector2 size, Color color, bool anchorRight = false)
        {
            GameObject background = new GameObject(name + " Background");
            background.transform.SetParent(parent, false);
            Image backgroundImage = background.AddComponent<Image>();
            backgroundImage.color = new Color(0f, 0f, 0f, 0.55f);
            RectTransform backgroundRect = background.GetComponent<RectTransform>();
            SetTopAnchor(backgroundRect, anchoredPosition, size, anchorRight);

            GameObject fill = new GameObject(name);
            fill.transform.SetParent(background.transform, false);
            Image image = fill.AddComponent<Image>();
            image.color = color;
            image.type = Image.Type.Filled;
            image.fillMethod = Image.FillMethod.Horizontal;
            image.fillOrigin = anchorRight ? (int)Image.OriginHorizontal.Right : (int)Image.OriginHorizontal.Left;
            RectTransform fillRect = fill.GetComponent<RectTransform>();
            fillRect.anchorMin = Vector2.zero;
            fillRect.anchorMax = Vector2.one;
            fillRect.offsetMin = new Vector2(3f, 3f);
            fillRect.offsetMax = new Vector2(-3f, -3f);
            return image;
        }

        private static Text CreateText(Transform parent, string name, Font font, Vector2 anchoredPosition, Vector2 size, TextAnchor alignment, int fontSize, Color color, bool anchorRight = false)
        {
            GameObject textObject = new GameObject(name);
            textObject.transform.SetParent(parent, false);
            Text text = textObject.AddComponent<Text>();
            text.font = font;
            text.fontSize = fontSize;
            text.color = color;
            text.alignment = alignment;
            text.raycastTarget = false;
            RectTransform rect = textObject.GetComponent<RectTransform>();
            SetTopAnchor(rect, anchoredPosition, size, anchorRight);
            return text;
        }

        private static void SetTopAnchor(RectTransform rect, Vector2 anchoredPosition, Vector2 size, bool anchorRight)
        {
            rect.anchorMin = anchorRight ? new Vector2(1f, 1f) : new Vector2(0f, 1f);
            rect.anchorMax = anchorRight ? new Vector2(1f, 1f) : new Vector2(0f, 1f);
            rect.pivot = anchorRight ? new Vector2(1f, 1f) : new Vector2(0f, 1f);
            rect.anchoredPosition = anchoredPosition;
            rect.sizeDelta = size;
        }

        private static void RegisterSceneInBuildSettings()
        {
            List<EditorBuildSettingsScene> scenes = new List<EditorBuildSettingsScene>
            {
                new EditorBuildSettingsScene(ScenePath, true)
            };

            EditorBuildSettings.scenes = scenes.ToArray();
        }

        private static void SetMaterial(GameObject gameObject, Material material)
        {
            Renderer renderer = gameObject.GetComponent<Renderer>();
            if (renderer != null)
            {
                renderer.sharedMaterial = material;
            }
        }
    }
}
