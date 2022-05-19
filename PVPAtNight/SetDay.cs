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
    [Info("SetDay", "Auro", "1.0.0")]
    [Description("Does the same thing as the SkipNightVote plugin")]
    public class SetDay : CovalencePlugin
    {
        private const string PermissionUse = "setday.use";

        private void Init()
        {
            permission.RegisterPermission(PermissionUse, this);
        }

        [Command("setday")]
        void SetDayCmd(IPlayer player, string command, string[] args)
        {
            if (!permission.UserHasPermission(player.Id, PermissionUse))
            {
                player.Reply("NOIDONTTHINKSO");
                return;
            };

            server.Time = server.Time.Date + TimeSpan.Parse("10:00:00");
            server.Time.Date.AddDays(1);
        }
    }
}
