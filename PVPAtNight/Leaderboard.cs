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


namespace Oxide.Plugins
{
    [Info("Leaderboard", "Auro", "1.0.0")]
    [Description("Connects with rust.mrauro.dev")]
    public class Leaderboard : CovalencePlugin
    {
        enum LeaderboardEvents
        {
            KillBradley,
            AttackHeli
        }

        private string _latestAttackerHelicopter;


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


        private void OnEntityDeath(BaseCombatEntity victimEntity, HitInfo hitInfo)
        {
            if (victimEntity == null)
                return;

            if (victimEntity.gameObject == null)
                return;

            BasePlayer attackerPlayer = victimEntity.lastAttacker?.ToPlayer();

            if (victimEntity is BradleyAPC && attackerPlayer != null)
            {
                Puts($"BradleyAPC was killed by {attackerPlayer.userID}");
                PostEvent(LeaderboardEvents.KillBradley, attackerPlayer.UserIDString);
            }

            if (victimEntity is BaseHelicopter && _latestAttackerHelicopter != null)
            {
                Puts($"Attack Heli was killed by {_latestAttackerHelicopter}");
                PostEvent(LeaderboardEvents.AttackHeli, _latestAttackerHelicopter);
                _latestAttackerHelicopter = null;
            }
        }

        private void PostEvent(LeaderboardEvents eventType, string userId)
        {
            Dictionary<string, string> headers = new Dictionary<string, string> { { "Authorization", "asdf" } };

            Puts($"Posting event {eventType} to rust.mrauro.dev");
            webrequest.Enqueue(
                $"https://rust.mrauro.dev/api/leaderboard?eventType={eventType}&userId={userId}",
                null,
                (code, response) =>
                {
                    if (code != 200)
                    {
                        Puts($"Failed to post event {eventType} to rust.mrauro.dev");
                        return;
                    }

                    Puts($"Successfully posted event {eventType} to rust.mrauro.dev");
                }, this, Core.Libraries.RequestMethod.POST, headers);
        }
    }
}
