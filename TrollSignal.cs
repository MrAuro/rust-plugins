using Oxide.Core;
using System;
using Oxide.Core.Plugins;
using Oxide.Game.Rust.Cui;
using System.Linq;
using UnityEngine;
using System.Collections.Generic;

namespace Oxide.Plugins
{
    [Info("TrollSignal", "Auro", "1.0.0")]
    [Description("Spawns a smoke grenade with a supply signal skin")]
    public class TrollSignal : RustPlugin
    {
        string craftPerm = "trollsignal.craft";

        void Init()
        {
            permission.RegisterPermission(craftPerm, this);
        }

        [ChatCommand("trollsignal.craft")]
        void CraftCommand(BasePlayer player, string command, string[] args)
        {
            if (!permission.UserHasPermission(player.UserIDString, craftPerm))
            {
                SendReply(player, "You don't have permission to use this command");
                return;
            }


            var grenade = ItemManager.CreateByName("grenade.smoke", 1, 1935663865);

            grenade.name = "Supply Signal";
            player.inventory.GiveItem(grenade);
        }
    }
}
