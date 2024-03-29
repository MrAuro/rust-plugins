using Newtonsoft.Json;
using Oxide.Core.Libraries.Covalence;
using Oxide.Core;
using ProtoBuf;
using System.Collections.Generic;
using UnityEngine;
using System;
using Oxide.Core.Plugins;
using Oxide.Game.Rust.Cui;
using System.Linq;
using Rust;

namespace Oxide.Plugins
{
    [Info("Leaderboard", "Auro", "1.0.0")]
    [Description("Connects with rust.mrauro.dev")]
    public class Leaderboard : CovalencePlugin
    {
        protected string URL = "https://rust.mrauro.dev";

        [PluginReference]
        Plugin PlaytimeTracker;

        public enum LeaderboardEvents
        {
            KillBradley,
            KillAttackHeli,
            HackCrate,
            PermittedKill,
            FirstLoginOfDay,
            CatchFish,
            RaidableEasy,
            RaidableMedium,
            RaidableHard,
            CallSupplySignal,
            ResearchItem,
        }

        public enum AchievementEvents
        {
            LongestKill,
            MostBearsKilled,
            MostCratesHacked,
            MostPermittedKills,
            MostBasesRaided,
            MostFishCaught,
            MostBradleysKilled,
            MostHelisKilled,
            MostPlayTime,
            MostDeaths,
        }

        private string _latestAttackerHelicopter;


        private void Init()
        {
            Subscribe("API_EnablePVP");
            Subscribe("API_DisablePVP");
        }

        // HANDLES:
        // - KillAttackHeli
        private void OnEntityTakeDamage(BaseCombatEntity victimEntity, HitInfo hitInfo)
        {
            if (victimEntity == null)
                return;

            if (victimEntity.gameObject == null)
                return;

            if (victimEntity.lastAttacker == null)
                return;

            if (victimEntity is BaseHelicopter)
            {
                BasePlayer attackerPlayer = victimEntity.lastAttacker.ToPlayer();
                if (attackerPlayer == null)
                    return;

                _latestAttackerHelicopter = attackerPlayer.UserIDString;
            }
        }

        private void OnUserDisconnected(IPlayer player)
        {
            // update the playertime in the achievement
            float playtime = PlaytimeTracker.Call<float>("GetPlayTime", player.Id);

            PostAction(AchievementEvents.MostPlayTime, player.Id, playtime.ToString("0.00"));
        }


        // HANDLES:
        // - KillBradley
        // - KillAttackHeli
        // - MostBradleysKilled
        // - MostHelisKilled
        // - MostBearsKilled
        private void OnEntityDeath(BaseCombatEntity victimEntity, HitInfo hitInfo)
        {
            // Puts($"{victimEntity.GetType()} died");

            if (victimEntity == null)
                return;

            if (victimEntity.gameObject == null)
                return;

            BasePlayer attackerPlayer = victimEntity.lastAttacker?.ToPlayer();

            // if (victimEntity is BasePlayer && attackerPlayer != null && !(victimEntity is NPCPlayer))
            // {
            //     if (PvpAllowed)
            //     {
            //         if (IsTeamKill(attackerPlayer, victimEntity.ToPlayer()))
            //             return;

            //         PostAction(AchievementEvents.MostPermittedKills, attackerPlayer.UserIDString);
            //         PostEvent(LeaderboardEvents.PermittedKill, attackerPlayer.UserIDString);

            //         float distance = attackerPlayer.Distance(victimEntity);
            //         Puts($"{attackerPlayer.displayName} killed {victimEntity.GetType()} at {distance}m");
            //         PostAction(AchievementEvents.LongestKill, attackerPlayer.UserIDString, distance.ToString("0.00"));
            //     }
            // }

            if (attackerPlayer is NPCPlayer || victimEntity is NPCPlayer) return;

            if ((victimEntity is Bear || victimEntity is Polarbear) && attackerPlayer != null)
            {
                PostAction(AchievementEvents.MostBearsKilled, attackerPlayer.UserIDString);
            }

            if (victimEntity.lastDamage == DamageType.Suicide)
                return;

            if (victimEntity is BasePlayer)
            {
                PostAction(AchievementEvents.MostDeaths, victimEntity.ToPlayer().UserIDString);
            }

            if (victimEntity is BradleyAPC && attackerPlayer != null)
            {
                Puts($"BradleyAPC was killed by {attackerPlayer.userID}");
                PostEvent(LeaderboardEvents.KillBradley, attackerPlayer.UserIDString);
                PostAction(AchievementEvents.MostBradleysKilled, attackerPlayer.UserIDString);
            }

            if (victimEntity is BaseHelicopter && _latestAttackerHelicopter != null)
            {
                Puts($"Attack Heli was killed by {_latestAttackerHelicopter}");
                PostEvent(LeaderboardEvents.KillAttackHeli, _latestAttackerHelicopter);
                PostAction(AchievementEvents.MostHelisKilled, _latestAttackerHelicopter);
                _latestAttackerHelicopter = null;
            }
        }

        // HANDLES:
        // - HackCrate
        // - MostCratesHacked
        object CanHackCrate(BasePlayer player, HackableLockedCrate crate)
        {
            PostEvent(LeaderboardEvents.HackCrate, player.UserIDString);
            PostAction(AchievementEvents.MostCratesHacked, player.UserIDString);

            return null;
        }

        // HANDLES:
        // - FirstLoginOfDay
        void OnPlayerConnected(BasePlayer player)
        {
            if (player == null)
                return;

            PostEvent(LeaderboardEvents.FirstLoginOfDay, player.UserIDString);
        }

        // HANDLES:
        // - ResearchItem
        void OnItemResearch(ResearchTable table, Item targetItem, BasePlayer player)
        {
            if (targetItem == null)
                return;

            if (player == null)
                return;

            PostEvent(LeaderboardEvents.ResearchItem, player.UserIDString);
        }

        // HANDLES:
        // - CatchFish
        // - MostFishCaught
        void OnFishCaught(ItemDefinition definition, BaseFishingRod rod, BasePlayer player)
        {
            if (definition == null)
                return;

            if (player == null)
                return;

            PostEvent(LeaderboardEvents.CatchFish, player.UserIDString);
            PostAction(AchievementEvents.MostFishCaught, player.UserIDString);
        }

        private void PostEvent(LeaderboardEvents eventType, string userId)
        {
            Dictionary<string, string> headers = new Dictionary<string, string> { { "Authorization", "asdf" } };

            Puts($"Posting event {eventType} to {URL}");
            webrequest.Enqueue(
                $"{URL}/api/leaderboard?eventType={eventType}&userId={userId}",
                null,
                (code, response) =>
                {
                    if (code != 200)
                    {
                        Puts($"Failed to post event {eventType} to {URL}");
                        BasePlayer player = BasePlayer.FindByID(Convert.ToUInt64(userId));
                        player.SendMessage($"{eventType} failed to send to the server ({code}). Please send a screenshot of this to Auro");
                        return;
                    }

                    Puts($"Successfully posted event {eventType} to {URL}");
                }, this, Core.Libraries.RequestMethod.POST, headers);
        }

        private void PostAction(AchievementEvents eventType, string userId, string data = null)
        {
            Dictionary<string, string> headers = new Dictionary<string, string> { { "Authorization", "asdf" } };

            string dataParam = data == null ? "" : $"&data={data}";

            Puts($"Posting event {eventType} to {URL}");
            webrequest.Enqueue(
                $"{URL}/api/achievement?eventType={eventType}&userId={userId}{dataParam}",
                null,
                (code, response) =>
                {
                    if (code != 200)
                    {
                        Puts($"Failed to post action event {eventType} to {URL}");
                        BasePlayer player = BasePlayer.FindByID(Convert.ToUInt64(userId));
                        player.SendMessage($"{eventType} failed to send to the server ({code}). Please send a screenshot of this to Auro");
                        return;
                    }

                    Puts($"Successfully posted action event {eventType} to {URL}");
                }, this, Core.Libraries.RequestMethod.POST, headers);
        }


    }
}
