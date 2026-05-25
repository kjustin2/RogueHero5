using System;
using Unity.BossRoom.ConnectionManagement;
using Unity.BossRoom.Gameplay.Actions;
using Unity.Multiplayer.Samples.BossRoom;
using Unity.Multiplayer.Samples.Utilities;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.Serialization;
using VContainer;

namespace Unity.BossRoom.Gameplay.GameState
{
    [RequireComponent(typeof(NetcodeHooks))]
    public class ServerPostGameState : GameStateBehaviour
    {
        [SerializeField]
        NetcodeHooks m_NetcodeHooks;

        [FormerlySerializedAs("synchronizedStateData")]
        [SerializeField]
        NetworkPostGame networkPostGame;
        public NetworkPostGame NetworkPostGame => networkPostGame;

        public override GameState ActiveState { get { return GameState.PostGame; } }

        [Inject]
        ConnectionManager m_ConnectionManager;

        [Inject]
        PersistentGameState m_PersistentGameState;
        [Inject]
        DuelSessionState m_DuelSessionState;

        protected override void Awake()
        {
            base.Awake();

            m_NetcodeHooks.OnNetworkSpawnHook += OnNetworkSpawn;
        }

        void OnNetworkSpawn()
        {
            if (!NetworkManager.Singleton.IsServer)
            {
                enabled = false;
            }
            else
            {
                SessionManager<SessionPlayerData>.Instance.OnSessionEnded();
                networkPostGame.WinState.Value = m_PersistentGameState.WinState;
            }
        }

        protected override void OnDestroy()
        {
            //clear actions pool
            ActionFactory.PurgePooledActions();
            m_PersistentGameState.Reset();

            base.OnDestroy();

            m_NetcodeHooks.OnNetworkSpawnHook -= OnNetworkSpawn;
        }

        public void PlayAgain()
        {
            if (m_DuelSessionState.IsCampaign)
            {
                m_DuelSessionState.StartCampaign();
                ResetConnectedPlayerFightData();
            }

            SceneLoaderWrapper.Instance.LoadScene("CharSelect", useNetworkSceneManager: true);
        }

        public void ContinueCampaignAfterReward()
        {
            if (!m_DuelSessionState.IsCampaign)
            {
                PlayAgain();
                return;
            }

            m_DuelSessionState.AdvanceCampaignRound();
            ResetConnectedPlayerFightData();
            SceneLoaderWrapper.Instance.LoadScene("BossRoom", useNetworkSceneManager: true);
        }

        public void GoToMainMenu()
        {
            m_DuelSessionState.ResetMode();
            m_ConnectionManager.RequestShutdown();
        }

        void ResetConnectedPlayerFightData()
        {
            foreach (var clientId in NetworkManager.Singleton.ConnectedClientsIds)
            {
                var sessionPlayerData = SessionManager<SessionPlayerData>.Instance.GetPlayerData(clientId);
                if (!sessionPlayerData.HasValue)
                {
                    continue;
                }

                var playerData = sessionPlayerData.Value;
                playerData.Reinitialize();
                playerData.CurrentHitPoints = 0;
                playerData.PlayerPosition = Vector3.zero;
                playerData.PlayerRotation = Quaternion.identity;
                SessionManager<SessionPlayerData>.Instance.SetPlayerData(clientId, playerData);
            }
        }
    }
}
