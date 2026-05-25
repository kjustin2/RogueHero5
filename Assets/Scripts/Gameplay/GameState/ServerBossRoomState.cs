using System;
using System.Collections;
using System.Collections.Generic;
using Unity.BossRoom.ConnectionManagement;
using Unity.BossRoom.Gameplay.GameplayObjects;
using Unity.BossRoom.Gameplay.GameplayObjects.Character;
using Unity.BossRoom.Gameplay.Messages;
using Unity.BossRoom.Infrastructure;
using Unity.BossRoom.Utils;
using Unity.Multiplayer.Samples.BossRoom;
using Unity.Multiplayer.Samples.Utilities;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Assertions;
using UnityEngine.SceneManagement;
using UnityEngine.Serialization;
using VContainer;
using Random = UnityEngine.Random;

namespace Unity.BossRoom.Gameplay.GameState
{
    /// <summary>
    /// Server specialization of core BossRoom game logic.
    /// </summary>
    [RequireComponent(typeof(NetcodeHooks))]
    public class ServerBossRoomState : GameStateBehaviour
    {
        [FormerlySerializedAs("m_NetworkWinState")]
        [SerializeField]
        PersistentGameState persistentGameState;

        [SerializeField]
        NetcodeHooks m_NetcodeHooks;

        [SerializeField]
        [Tooltip("Make sure this is included in the NetworkManager's list of prefabs!")]
        private NetworkObject m_PlayerPrefab;

        [SerializeField]
        [Tooltip("Campaign boss prefab for the 1v1 arena vertical slice. Make sure this is included in the NetworkManager's list of prefabs!")]
        private NetworkObject m_CampaignBossPrefab;

        [SerializeField]
        [Tooltip("A collection of locations for spawning players")]
        private Transform[] m_PlayerSpawnPoints;

        private List<Transform> m_PlayerSpawnPointsList = null;
        readonly HashSet<ulong> m_ConfiguredCampaignBossIds = new HashSet<ulong>();
        bool m_DuelPlayersPositioned;
        bool m_DuelSceneLoadingPrepared;
        bool m_DuelBossSceneLoadRequested;
        bool m_DuelInitialSpawnStarted;
        bool m_GameOverTriggered;
        bool m_HasDuelCampaignBoss;
        ulong m_DuelCampaignBossId;

        public override GameState ActiveState { get { return GameState.BossRoom; } }

        // Wait time constants for switching to post game after the game is won or lost
        private const float k_WinDelay = 7.0f;
        private const float k_LoseDelay = 2.5f;
        const string k_DuelBossSubscene = "DungeonBossRoom";

        /// <summary>
        /// Has the ServerBossRoomState already hit its initial spawn? (i.e. spawned players following load from character select).
        /// </summary>
        public bool InitialSpawnDone { get; private set; }

        /// <summary>
        /// Keeping the subscriber during this GameState's lifetime to allow disposing of subscription and re-subscribing
        /// when despawning and spawning again.
        /// </summary>
        [Inject] ISubscriber<LifeStateChangedEventMessage> m_LifeStateChangedEventMessageSubscriber;

        [Inject] ConnectionManager m_ConnectionManager;
        [Inject] PersistentGameState m_PersistentGameState;
        [Inject] DuelSessionState m_DuelSessionState;

        protected override void Awake()
        {
            base.Awake();
            m_NetcodeHooks.OnNetworkSpawnHook += OnNetworkSpawn;
            m_NetcodeHooks.OnNetworkDespawnHook += OnNetworkDespawn;
        }

        void OnNetworkSpawn()
        {
            if (!NetworkManager.Singleton.IsServer)
            {
                enabled = false;
                return;
            }
            m_PersistentGameState.Reset();
            m_ConfiguredCampaignBossIds.Clear();
            m_DuelPlayersPositioned = false;
            m_DuelSceneLoadingPrepared = false;
            m_DuelBossSceneLoadRequested = false;
            m_DuelInitialSpawnStarted = false;
            m_GameOverTriggered = false;
            m_HasDuelCampaignBoss = false;
            m_DuelCampaignBossId = 0;
            m_LifeStateChangedEventMessageSubscriber.Subscribe(OnLifeStateChangedEventMessage);

            NetworkManager.Singleton.OnConnectionEvent += OnConnectionEvent;
            NetworkManager.Singleton.SceneManager.OnLoadEventCompleted += OnLoadEventCompleted;
            NetworkManager.Singleton.SceneManager.OnSynchronizeComplete += OnSynchronizeComplete;

            SessionManager<SessionPlayerData>.Instance.OnSessionStarted();
        }

        void OnNetworkDespawn()
        {
            if (m_LifeStateChangedEventMessageSubscriber != null)
            {
                m_LifeStateChangedEventMessageSubscriber.Unsubscribe(OnLifeStateChangedEventMessage);
            }

            NetworkManager.Singleton.OnConnectionEvent -= OnConnectionEvent;
            NetworkManager.Singleton.SceneManager.OnLoadEventCompleted -= OnLoadEventCompleted;
            NetworkManager.Singleton.SceneManager.OnSynchronizeComplete -= OnSynchronizeComplete;
        }

        protected override void OnDestroy()
        {
            if (m_LifeStateChangedEventMessageSubscriber != null)
            {
                m_LifeStateChangedEventMessageSubscriber.Unsubscribe(OnLifeStateChangedEventMessage);
            }

            if (m_NetcodeHooks)
            {
                m_NetcodeHooks.OnNetworkSpawnHook -= OnNetworkSpawn;
                m_NetcodeHooks.OnNetworkDespawnHook -= OnNetworkDespawn;
            }

            base.OnDestroy();
        }

        void OnSynchronizeComplete(ulong clientId)
        {
            if (InitialSpawnDone && !PlayerServerCharacter.GetPlayerServerCharacter(clientId))
            {
                //somebody joined after the initial spawn. This is a Late Join scenario. This player may have issues
                //(either because multiple people are late-joining at once, or because some dynamic entities are
                //getting spawned while joining. But that's not something we can fully address by changes in
                //ServerBossRoomState.
                SpawnPlayer(clientId, true);
                if (EnsureDuelModeFromSessionSettings())
                {
                    StartCoroutine(ConfigureDuelEncounter());
                }
            }
        }

        void OnLoadEventCompleted(string sceneName, LoadSceneMode loadSceneMode, List<ulong> clientsCompleted, List<ulong> clientsTimedOut)
        {
            if (!InitialSpawnDone && loadSceneMode == LoadSceneMode.Single)
            {
                bool isDuelMode = EnsureDuelModeFromSessionSettings();
                if (isDuelMode)
                {
                    PrepareDuelSceneLoading();
                    StartCoroutine(SpawnInitialDuelPlayersWhenArenaReady());
                    return;
                }

                InitialSpawnDone = true;
                SpawnInitialPlayers(false);
            }
            else if (EnsureDuelModeFromSessionSettings() && loadSceneMode == LoadSceneMode.Additive)
            {
                if (sceneName == k_DuelBossSubscene)
                {
                    StartCoroutine(SpawnInitialDuelPlayersWhenArenaReady());
                    StartCoroutine(ConfigureDuelEncounter());
                }
                else
                {
                    StartCoroutine(ConfigureDuelEncounter());
                }
            }
        }

        void OnConnectionEvent(NetworkManager networkManager, ConnectionEventData connectionEventData)
        {
            if (connectionEventData.EventType == ConnectionEvent.ClientDisconnected)
            {
                if (connectionEventData.ClientId != networkManager.LocalClientId)
                {
                    // If a client disconnects, check for game over in case all other players are already down
                    StartCoroutine(WaitToCheckForGameOver());
                }
            }
        }

        IEnumerator WaitToCheckForGameOver()
        {
            // Wait until next frame so that the client's player character has despawned
            yield return null;
            CheckForGameOver();
        }

        IEnumerator SpawnInitialDuelPlayersWhenArenaReady()
        {
            if (m_DuelInitialSpawnStarted || InitialSpawnDone)
            {
                yield break;
            }

            m_DuelInitialSpawnStarted = true;
            PrepareDuelSceneLoading();
            DuelArenaRuntimeScaffold.EnsureArena(SceneManager.GetSceneByName(ActiveState.ToString()));

            var stopWaitingAt = Time.time + 3f;
            float nextWarningTime = Time.time + 1.5f;
            while (!IsSceneLoaded(k_DuelBossSubscene) && Time.time < stopWaitingAt)
            {
                if (Time.time >= nextWarningTime)
                {
                    Debug.LogWarning($"Duel arena is waiting for '{k_DuelBossSubscene}', but the runtime arena is already available.");
                    nextWarningTime = Time.time + 1.5f;
                    if (!m_DuelBossSceneLoadRequested)
                    {
                        StartCoroutine(RequestDuelBossRoomLoad());
                    }
                }

                yield return null;
            }

            if (!IsSceneLoaded(k_DuelBossSubscene))
            {
                Debug.LogWarning($"Duel arena continuing without '{k_DuelBossSubscene}' because the runtime vertical-slice arena is ready.");
            }

            yield return null;

            DuelArenaRuntimeScaffold.EnsureArena(SceneManager.GetActiveScene());
            InitialSpawnDone = true;
            SpawnInitialPlayers(false);
            Debug.Log($"Duel arena spawned {NetworkManager.Singleton.ConnectedClients.Count} player(s) after arena scene ready={IsSceneLoaded(k_DuelBossSubscene)}.");
            StartCoroutine(ConfigureDuelEncounter());
        }

        void SpawnInitialPlayers(bool lateJoin)
        {
            foreach (var kvp in NetworkManager.Singleton.ConnectedClients)
            {
                if (PlayerServerCharacter.GetPlayerServerCharacter(kvp.Key))
                {
                    continue;
                }

                SpawnPlayer(kvp.Key, lateJoin);
            }
        }

        void SpawnPlayer(ulong clientId, bool lateJoin)
        {
            Transform spawnPoint = null;

            if (m_PlayerSpawnPointsList == null || m_PlayerSpawnPointsList.Count == 0)
            {
                m_PlayerSpawnPointsList = new List<Transform>(m_PlayerSpawnPoints);
            }

            Debug.Assert(m_PlayerSpawnPointsList.Count > 0,
                $"PlayerSpawnPoints array should have at least 1 spawn points.");

            int index = Random.Range(0, m_PlayerSpawnPointsList.Count);
            spawnPoint = m_PlayerSpawnPointsList[index];
            m_PlayerSpawnPointsList.RemoveAt(index);

            var playerNetworkObject = NetworkManager.Singleton.SpawnManager.GetPlayerNetworkObject(clientId);

            var newPlayer = Instantiate(m_PlayerPrefab, Vector3.zero, Quaternion.identity);

            var newPlayerCharacter = newPlayer.GetComponent<ServerCharacter>();

            var physicsTransform = newPlayerCharacter.physicsWrapper.Transform;

            if (spawnPoint != null)
            {
                physicsTransform.SetPositionAndRotation(spawnPoint.position, spawnPoint.rotation);
            }

            var persistentPlayerExists = playerNetworkObject.TryGetComponent(out PersistentPlayer persistentPlayer);
            Assert.IsTrue(persistentPlayerExists,
                $"Matching persistent PersistentPlayer for client {clientId} not found!");

            // pass character type from persistent player to avatar
            var networkAvatarGuidStateExists =
                newPlayer.TryGetComponent(out NetworkAvatarGuidState networkAvatarGuidState);

            Assert.IsTrue(networkAvatarGuidStateExists,
                $"NetworkCharacterGuidState not found on player avatar!");

            // if reconnecting, set the player's position and rotation to its previous state
            if (lateJoin)
            {
                SessionPlayerData? sessionPlayerData = SessionManager<SessionPlayerData>.Instance.GetPlayerData(clientId);
                if (sessionPlayerData is { HasCharacterSpawned: true })
                {
                    physicsTransform.SetPositionAndRotation(sessionPlayerData.Value.PlayerPosition, sessionPlayerData.Value.PlayerRotation);
                }
            }

            // instantiate new NetworkVariables with a default value to ensure they're ready for use on OnNetworkSpawn
            networkAvatarGuidState.AvatarGuid = new NetworkVariable<NetworkGuid>(persistentPlayer.NetworkAvatarGuidState.AvatarGuid.Value);

            // pass name from persistent player to avatar
            if (newPlayer.TryGetComponent(out NetworkNameState networkNameState))
            {
                networkNameState.Name = new NetworkVariable<FixedPlayerName>(persistentPlayer.NetworkNameState.Name.Value);
            }

            // spawn players characters with destroyWithScene = true
            newPlayer.SpawnWithOwnership(clientId, true);
        }

        void OnLifeStateChangedEventMessage(LifeStateChangedEventMessage message)
        {
            switch (message.CharacterType)
            {
                case CharacterTypeEnum.Tank:
                case CharacterTypeEnum.Archer:
                case CharacterTypeEnum.Mage:
                case CharacterTypeEnum.Rogue:
                    // Every time a player's life state changes to fainted we check to see if game is over
                    if (message.NewLifeState == LifeState.Fainted)
                    {
                        CheckForGameOver();
                    }

                    break;
                case CharacterTypeEnum.ImpBoss:
                    if (message.NewLifeState == LifeState.Dead)
                    {
                        BossDefeated();
                    }
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        void CheckForGameOver()
        {
            // Check the life state of all players in the scene
            int playerCount = 0;
            int alivePlayerCount = 0;
            foreach (var serverCharacter in PlayerServerCharacter.GetPlayerServerCharacters())
            {
                playerCount++;
                if (serverCharacter && serverCharacter.LifeState == LifeState.Alive)
                {
                    alivePlayerCount++;
                }
            }

            if (m_DuelSessionState.IsPvp && playerCount >= 2 && alivePlayerCount <= 1)
            {
                StartGameOver(k_LoseDelay, WinState.DuelComplete);
                return;
            }

            if (alivePlayerCount > 0)
            {
                return;
            }

            // If we made it this far, all players are down! switch to post game
            StartGameOver(k_LoseDelay, WinState.Loss);
        }

        void BossDefeated()
        {
            // Boss is dead - set game won to true
            StartGameOver(k_WinDelay, WinState.Win);
        }

        void StartGameOver(float wait, WinState winState)
        {
            if (m_GameOverTriggered)
            {
                return;
            }

            m_GameOverTriggered = true;
            StartCoroutine(CoroGameOver(wait, winState));
        }

        IEnumerator CoroGameOver(float wait, WinState winState)
        {
            m_PersistentGameState.SetWinState(winState);

            // wait 5 seconds for game animations to finish
            yield return new WaitForSeconds(wait);

            SceneLoaderWrapper.Instance.LoadScene("PostGame", useNetworkSceneManager: true);
        }

        bool EnsureDuelModeFromSessionSettings()
        {
            if (m_DuelSessionState.IsCampaign || m_DuelSessionState.IsPvp)
            {
                return true;
            }

            if (m_ConnectionManager.MaxConnectedPlayers == DuelSessionState.CampaignMaxPlayers)
            {
                Debug.LogWarning("BossRoom entered with no duel mode set. Recovering as Campaign because the connection cap is 1.");
                m_DuelSessionState.StartCampaign();
                return true;
            }

            if (m_ConnectionManager.MaxConnectedPlayers == DuelSessionState.PvpMaxPlayers)
            {
                Debug.LogWarning("BossRoom entered with no duel mode set. Recovering as PvP because the connection cap is 2.");
                m_DuelSessionState.StartPvp();
                return true;
            }

            return false;
        }

        void PrepareDuelSceneLoading()
        {
            DuelArenaRuntimeScaffold.EnsureArena(SceneManager.GetActiveScene());

            if (m_DuelSceneLoadingPrepared)
            {
                return;
            }

            m_DuelSceneLoadingPrepared = true;

            foreach (var sceneLoader in FindObjectsByType<ServerAdditiveSceneLoader>(FindObjectsSortMode.None))
            {
                sceneLoader.enabled = false;
                if (sceneLoader.TryGetComponent<Collider>(out var loaderCollider))
                {
                    loaderCollider.enabled = false;
                }
            }

            if (!IsSceneLoaded(k_DuelBossSubscene))
            {
                StartCoroutine(RequestDuelBossRoomLoad());
            }
        }

        IEnumerator RequestDuelBossRoomLoad()
        {
            if (m_DuelBossSceneLoadRequested)
            {
                yield break;
            }

            m_DuelBossSceneLoadRequested = true;
            yield return null;

            if (!NetworkManager.Singleton || !NetworkManager.Singleton.IsServer ||
                NetworkManager.Singleton.SceneManager == null || IsSceneLoaded(k_DuelBossSubscene))
            {
                m_DuelBossSceneLoadRequested = IsSceneLoaded(k_DuelBossSubscene);
                yield break;
            }

            var status = NetworkManager.Singleton.SceneManager.LoadScene(k_DuelBossSubscene, LoadSceneMode.Additive);
            Debug.Log($"Duel arena requested additive scene '{k_DuelBossSubscene}' with status {status}.");
            if (status != SceneEventProgressStatus.Started)
            {
                m_DuelBossSceneLoadRequested = false;
                Debug.LogError($"Duel arena could not start loading '{k_DuelBossSubscene}' because Netcode returned {status}.");
            }
        }

        static bool IsSceneLoaded(string sceneName)
        {
            return SceneManager.GetSceneByName(sceneName).isLoaded;
        }

        IEnumerator ConfigureDuelEncounter()
        {
            if (!EnsureDuelModeFromSessionSettings())
            {
                yield break;
            }

            PrepareDuelSceneLoading();

            for (int i = 0; i < 20; i++)
            {
                yield return null;
                ConfigureDuelArena();
            }
        }

        void ConfigureDuelArena()
        {
            PrepareDuelSceneLoading();

            DuelArenaRuntimeScaffold.EnsureArena(SceneManager.GetActiveScene());

            foreach (var waveSpawner in FindObjectsByType<ServerWaveSpawner>(FindObjectsSortMode.None))
            {
                waveSpawner.SetSpawnerEnabled(false);
                waveSpawner.enabled = false;
            }

            foreach (var serverCharacter in FindObjectsByType<ServerCharacter>(FindObjectsSortMode.None))
            {
                if (!serverCharacter || !serverCharacter.IsNpc)
                {
                    continue;
                }

                bool isBoss = serverCharacter.CharacterType == CharacterTypeEnum.ImpBoss;
                if (m_DuelSessionState.IsPvp || !isBoss)
                {
                    if (serverCharacter.NetworkObject && serverCharacter.NetworkObject.IsSpawned)
                    {
                        serverCharacter.NetworkObject.Despawn(true);
                    }
                    else
                    {
                        Destroy(serverCharacter.gameObject);
                    }

                    continue;
                }

                if (!m_HasDuelCampaignBoss)
                {
                    m_DuelCampaignBossId = serverCharacter.NetworkObjectId;
                    m_HasDuelCampaignBoss = true;
                }
                else if (serverCharacter.NetworkObjectId != m_DuelCampaignBossId)
                {
                    serverCharacter.NetworkObject.Despawn(true);
                    continue;
                }

                ConfigureCampaignBoss(serverCharacter);
            }

            if (m_DuelSessionState.IsCampaign)
            {
                EnsureCampaignBossSpawned();
            }

            PositionDuelPlayers();
            FaceCampaignBossTowardPlayer();
        }

        void ConfigureCampaignBoss(ServerCharacter boss)
        {
            if (!boss.NetworkObject || !boss.NetworkObject.IsSpawned ||
                m_ConfiguredCampaignBossIds.Contains(boss.NetworkObjectId))
            {
                return;
            }

            boss.SetMaxHitPointsMultiplier(m_DuelSessionState.CampaignBossHealthMultiplier, true);
            boss.DamageDealtMultiplier = m_DuelSessionState.CampaignBossDamageMultiplier;

            if (boss.AIBrain != null)
            {
                boss.AIBrain.DetectRange = 80f;
                foreach (var playerCharacter in PlayerServerCharacter.GetPlayerServerCharacters())
                {
                    boss.AIBrain.Hate(playerCharacter);
                }
            }

            m_ConfiguredCampaignBossIds.Add(boss.NetworkObjectId);
        }

        void EnsureCampaignBossSpawned()
        {
            if (!m_DuelSessionState.IsCampaign || TryGetCampaignBoss(out _))
            {
                return;
            }

            if (!m_CampaignBossPrefab)
            {
                Debug.LogError("Duel campaign cannot spawn a boss because BossRoomState is missing m_CampaignBossPrefab.");
                return;
            }

            var placement = GetDuelArenaPlacement();
            var playerPosition = SampleArenaNavMeshPosition(placement.PlayerPosition, "campaign player preview");
            var bossPosition = SampleArenaNavMeshPosition(placement.OpponentPosition, "campaign boss spawn");
            var bossNetworkObject = Instantiate(m_CampaignBossPrefab, bossPosition, LookAtFlat(bossPosition, playerPosition));
            var boss = bossNetworkObject.GetComponent<ServerCharacter>();
            if (!boss)
            {
                Destroy(bossNetworkObject.gameObject);
                Debug.LogError($"Duel campaign boss prefab '{m_CampaignBossPrefab.name}' does not have a ServerCharacter.");
                return;
            }

            bossNetworkObject.Spawn(true);
            m_DuelCampaignBossId = bossNetworkObject.NetworkObjectId;
            m_HasDuelCampaignBoss = true;
            TeleportDuelCharacter(boss, bossPosition, LookAtFlat(bossPosition, playerPosition));
            ConfigureCampaignBoss(boss);
            Debug.Log($"Duel campaign spawned fallback boss '{bossNetworkObject.name}' at {bossPosition} using {placement.Source}.");
        }

        void PositionDuelPlayers()
        {
            if (m_DuelPlayersPositioned)
            {
                return;
            }

            var playerCharacters = PlayerServerCharacter.GetPlayerServerCharacters();
            if (playerCharacters.Count == 0 || (m_DuelSessionState.IsPvp && playerCharacters.Count < 2))
            {
                return;
            }

            if (m_DuelSessionState.IsCampaign)
            {
                if (!TryGetCampaignBoss(out var boss))
                {
                    return;
                }

                var arenaPlacement = GetDuelArenaPlacement();
                var playerPosition = SampleArenaNavMeshPosition(arenaPlacement.PlayerPosition, "campaign player");
                var bossPosition = SampleArenaNavMeshPosition(arenaPlacement.OpponentPosition, "campaign boss");

                TeleportDuelCharacter(
                    boss,
                    bossPosition,
                    LookAtFlat(bossPosition, playerPosition));
                TeleportDuelCharacter(
                    playerCharacters[0],
                    playerPosition,
                    LookAtFlat(playerPosition, bossPosition));
                Debug.Log(
                    $"Duel campaign positioned player at {playerPosition} and boss at {bossPosition} " +
                    $"using {arenaPlacement.Source}.");
            }
            else if (m_DuelSessionState.IsPvp)
            {
                var arenaPlacement = GetDuelArenaPlacement();
                var leftPosition = SampleArenaNavMeshPosition(arenaPlacement.PlayerPosition, "PvP left player");
                var rightPosition = SampleArenaNavMeshPosition(arenaPlacement.OpponentPosition, "PvP right player");
                TeleportDuelCharacter(playerCharacters[0], leftPosition, LookAtFlat(leftPosition, rightPosition));
                TeleportDuelCharacter(playerCharacters[1], rightPosition, LookAtFlat(rightPosition, leftPosition));
                Debug.Log($"Duel PvP positioned players at {leftPosition} and {rightPosition} using {arenaPlacement.Source}.");
            }

            m_DuelPlayersPositioned = true;
        }

        bool TryGetCampaignBoss(out ServerCharacter boss)
        {
            if (m_HasDuelCampaignBoss &&
                NetworkManager.Singleton &&
                NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue(m_DuelCampaignBossId, out var bossNetworkObject) &&
                bossNetworkObject &&
                bossNetworkObject.TryGetComponent(out boss) &&
                boss &&
                boss.IsNpc &&
                boss.CharacterType == CharacterTypeEnum.ImpBoss &&
                boss.LifeState != LifeState.Dead)
            {
                return true;
            }

            m_HasDuelCampaignBoss = false;
            m_DuelCampaignBossId = 0;

            foreach (var serverCharacter in FindObjectsByType<ServerCharacter>(FindObjectsSortMode.None))
            {
                if (serverCharacter && serverCharacter.IsNpc &&
                    serverCharacter.CharacterType == CharacterTypeEnum.ImpBoss &&
                    serverCharacter.NetworkObject && serverCharacter.NetworkObject.IsSpawned)
                {
                    boss = serverCharacter;
                    m_DuelCampaignBossId = serverCharacter.NetworkObjectId;
                    m_HasDuelCampaignBoss = true;
                    return true;
                }
            }

            boss = null;
            return false;
        }

        void FaceCampaignBossTowardPlayer()
        {
            if (!m_DuelSessionState.IsCampaign || !TryGetCampaignBoss(out var boss))
            {
                return;
            }

            var players = PlayerServerCharacter.GetPlayerServerCharacters();
            if (players.Count == 0)
            {
                return;
            }

            var bossPosition = boss.physicsWrapper.Transform.position;
            var lookAtPosition = players[0].physicsWrapper.Transform.position;
            boss.physicsWrapper.Transform.SetPositionAndRotation(bossPosition, LookAtFlat(bossPosition, lookAtPosition));
        }

        static Vector3 SampleArenaNavMeshPosition(Vector3 position, string label)
        {
            if (NavMesh.SamplePosition(position, out var hit, 12f, NavMesh.AllAreas))
            {
                return hit.position;
            }

            Debug.LogWarning($"Duel arena could not sample NavMesh for {label} near {position}; using unsampled position.");
            return position;
        }

        static DuelArenaRuntimeScaffold.DuelArenaPlacement GetDuelArenaPlacement()
        {
            DuelArenaRuntimeScaffold.EnsureArena(SceneManager.GetActiveScene());
            return DuelArenaRuntimeScaffold.Placement;
        }

        static void TeleportDuelCharacter(ServerCharacter character, Vector3 position, Quaternion rotation)
        {
            if (!character)
            {
                return;
            }

            if (character.Movement)
            {
                character.Movement.Teleport(position);
            }

            character.physicsWrapper.Transform.SetPositionAndRotation(position, rotation);
        }

        static Quaternion LookAtFlat(Vector3 from, Vector3 to)
        {
            var direction = to - from;
            direction.y = 0f;
            return direction.sqrMagnitude > 0.001f ? Quaternion.LookRotation(direction.normalized) : Quaternion.identity;
        }
    }
}
