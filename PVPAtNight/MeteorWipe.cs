using Oxide.Core;
using System;
using UnityEngine;
using Oxide.Core.Plugins;
using Oxide.Game.Rust.Cui;
using System.Linq;

namespace Oxide.Plugins
{
    [Info("MeteorWipe", "Auro", "1.0.0")]
    [Description("Destorys the map with a bunch of meteors")]
    public class MeteorWipe : RustPlugin
    {
        private const string PermissionName = "meteorwipe.use";

        private void Init()
        {
            permission.RegisterPermission(PermissionName, this);
        }

        [ChatCommand("wipe")]
        void wipe(BasePlayer player, string command, string[] args)
        {
            if (!permission.UserHasPermission(player.UserIDString, PermissionName))
            {
                SendReply(player, "You don't have permission to use this command");
                return;
            }


        }

    }
}