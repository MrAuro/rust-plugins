using Oxide.Core;
using System;
using Oxide.Core.Plugins;
using Oxide.Game.Rust.Cui;
using System.Linq;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace Oxide.Plugins
{
    [Info("ChinookCrates", "Auro", "1.0.0")]
    [Description("Makes Chinook Crates automatically start to hack")]
    public class ChinookCrates : RustPlugin
    {
        void OnCrateDropped(HackableLockedCrate crate)
        {
            Server.Broadcast("OnCrateDropped has been fired!");

            // if (BasePlayer.allPlayerList.Count() <= 4) return;
            // Server.Broadcast("The Chinook Crate will unlock in 5 minutes!");
            // crate.StartHacking();
            // crate.hackSeconds = 300;

            // timer.Once(240f, () =>
            // {
            //     Server.Broadcast("The Chinook Crate will unlock in 60 seconds!");
            //     crate.hackSeconds = 60;

            //     timer.Once(59f, () =>
            //     {
            //         Server.Broadcast("The Chinook Crate has been unlocked!");
            //     });
            // });
        }
    }
}
