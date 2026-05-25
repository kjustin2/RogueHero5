using System;
using System.Collections.Generic;
using Unity.BossRoom.Gameplay.Actions;
using Unity.BossRoom.Gameplay.Configuration;
using Unity.BossRoom.Gameplay.GameplayObjects;
using Unity.BossRoom.Infrastructure;
using UnityEngine;
using Action = Unity.BossRoom.Gameplay.Actions.Action;
using Avatar = Unity.BossRoom.Gameplay.Configuration.Avatar;

namespace Unity.BossRoom.Gameplay.GameState
{
    /// <summary>
    /// Session-scoped rules for the 1v1 arena modes. This intentionally persists across scene loads but not app restarts.
    /// </summary>
    public class DuelSessionState
    {
        public const int CampaignMaxPlayers = 1;
        public const int PvpMaxPlayers = 2;
        public const int LoadoutSlotCount = 3;

        const float k_BossHealthGrowthPerWin = 0.35f;
        const float k_BossDamageGrowthPerWin = 0.15f;

        readonly Action[] m_CampaignLoadout = new Action[LoadoutSlotCount];
        readonly List<Action> m_LastDraft = new List<Action>(LoadoutSlotCount);
        Avatar m_CampaignAvatar;
        NetworkGuid m_CampaignAvatarGuid;

        public static DuelSessionState Instance { get; private set; }

        public DuelGameMode Mode { get; private set; } = DuelGameMode.None;

        public int CampaignBossLevel { get; private set; } = 1;

        public bool IsCampaign => Mode == DuelGameMode.Campaign;

        public bool IsPvp => Mode == DuelGameMode.Pvp;

        public float CampaignBossHealthMultiplier => 1f + ((CampaignBossLevel - 1) * k_BossHealthGrowthPerWin);

        public float CampaignBossDamageMultiplier => 1f + ((CampaignBossLevel - 1) * k_BossDamageGrowthPerWin);

        public IReadOnlyList<Action> LastDraft => m_LastDraft;

        public DuelSessionState()
        {
            Instance = this;
        }

        public void StartCampaign(Avatar campaignAvatar = null)
        {
            Mode = DuelGameMode.Campaign;
            CampaignBossLevel = 1;
            ClearCampaignLoadout();
            m_LastDraft.Clear();

            if (campaignAvatar != null)
            {
                SetCampaignAvatar(campaignAvatar);
            }
            else if (m_CampaignAvatar != null)
            {
                InitializeCampaignLoadout(m_CampaignAvatar.CharacterClass);
            }
        }

        public void StartPvp()
        {
            Mode = DuelGameMode.Pvp;
            CampaignBossLevel = 1;
            ClearCampaignLoadout();
            m_LastDraft.Clear();
            ClearCampaignAvatar();
        }

        public void ResetMode()
        {
            Mode = DuelGameMode.None;
            CampaignBossLevel = 1;
            ClearCampaignLoadout();
            m_LastDraft.Clear();
            ClearCampaignAvatar();
        }

        public void RestartCampaign()
        {
            StartCampaign(m_CampaignAvatar);
        }

        public void SetCampaignAvatar(Avatar campaignAvatar)
        {
            if (campaignAvatar == null)
            {
                return;
            }

            m_CampaignAvatar = campaignAvatar;
            m_CampaignAvatarGuid = campaignAvatar.Guid.ToNetworkGuid();
            InitializeCampaignLoadout(campaignAvatar.CharacterClass);
        }

        public bool TryGetCampaignAvatarGuid(out NetworkGuid avatarGuid)
        {
            avatarGuid = m_CampaignAvatarGuid;
            return IsCampaign && m_CampaignAvatarGuid.ToGuid() != Guid.Empty;
        }

        public void InitializeCampaignLoadout(CharacterClass characterClass)
        {
            if (!IsCampaign || characterClass == null || HasCampaignLoadout())
            {
                return;
            }

            m_CampaignLoadout[0] = characterClass.Skill1;
            m_CampaignLoadout[1] = characterClass.Skill2;
            m_CampaignLoadout[2] = characterClass.Skill3;
        }

        public bool TryGetCampaignLoadout(CharacterClass fallbackClass, out Action[] loadout)
        {
            if (!IsCampaign)
            {
                loadout = null;
                return false;
            }

            InitializeCampaignLoadout(fallbackClass);
            loadout = new Action[LoadoutSlotCount];
            Array.Copy(m_CampaignLoadout, loadout, LoadoutSlotCount);
            return HasCampaignLoadout();
        }

        public Action GetCampaignLoadoutAction(int slotIndex)
        {
            if (slotIndex < 0 || slotIndex >= m_CampaignLoadout.Length)
            {
                return null;
            }

            return m_CampaignLoadout[slotIndex];
        }

        public IReadOnlyList<Action> CreateCampaignRewardDraft(GameDataSource gameDataSource, CharacterClass fallbackClass)
        {
            m_LastDraft.Clear();

            if (!IsCampaign || gameDataSource == null)
            {
                return m_LastDraft;
            }

            InitializeCampaignLoadout(fallbackClass);

            var candidates = new List<Action>(gameDataSource.GetCampaignRewardCandidates());
            for (int i = candidates.Count - 1; i >= 0; i--)
            {
                if (candidates[i] == null || IsInCampaignLoadout(candidates[i]))
                {
                    candidates.RemoveAt(i);
                }
            }

            Shuffle(candidates);

            for (int i = 0; i < candidates.Count && m_LastDraft.Count < LoadoutSlotCount; i++)
            {
                m_LastDraft.Add(candidates[i]);
            }

            return m_LastDraft;
        }

        public void ApplyCampaignReward(Action rewardAction, int slotIndex)
        {
            if (!IsCampaign || rewardAction == null)
            {
                return;
            }

            if (slotIndex < 0 || slotIndex >= m_CampaignLoadout.Length)
            {
                throw new ArgumentOutOfRangeException(nameof(slotIndex), "Campaign loadout slot is out of range.");
            }

            m_CampaignLoadout[slotIndex] = rewardAction;
        }

        public void AdvanceCampaignRound()
        {
            if (!IsCampaign)
            {
                return;
            }

            CampaignBossLevel++;
            m_LastDraft.Clear();
        }

        public string GetCampaignLoadoutSlotName(int slotIndex)
        {
            return GetActionDisplayName(GetCampaignLoadoutAction(slotIndex), $"Empty Slot {slotIndex + 1}");
        }

        public static string GetActionDisplayName(Action action, string fallback = "Unknown Ability")
        {
            if (action == null || action.Config == null || string.IsNullOrWhiteSpace(action.Config.DisplayedName))
            {
                return fallback;
            }

            return action.Config.DisplayedName;
        }

        bool HasCampaignLoadout()
        {
            for (int i = 0; i < m_CampaignLoadout.Length; i++)
            {
                if (m_CampaignLoadout[i] != null)
                {
                    return true;
                }
            }

            return false;
        }

        bool IsInCampaignLoadout(Action action)
        {
            for (int i = 0; i < m_CampaignLoadout.Length; i++)
            {
                if (m_CampaignLoadout[i] == action)
                {
                    return true;
                }
            }

            return false;
        }

        void ClearCampaignLoadout()
        {
            for (int i = 0; i < m_CampaignLoadout.Length; i++)
            {
                m_CampaignLoadout[i] = null;
            }
        }

        void ClearCampaignAvatar()
        {
            m_CampaignAvatar = null;
            m_CampaignAvatarGuid = default;
        }

        static void Shuffle<T>(IList<T> items)
        {
            for (int i = items.Count - 1; i > 0; i--)
            {
                int swapIndex = UnityEngine.Random.Range(0, i + 1);
                var item = items[i];
                items[i] = items[swapIndex];
                items[swapIndex] = item;
            }
        }
    }
}
