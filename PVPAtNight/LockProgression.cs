using Oxide.Core;
using System;
using Oxide.Core.Plugins;
using Oxide.Game.Rust.Cui;
using System.Linq;
using System.Collections.Generic;

namespace Oxide.Plugins
{
    [Info("LockProgression", "Auro", "1.0.0")]
    [Description("Locks certain monuments and events until a certain time")]
    public class LockProgression : RustPlugin
    {
        [PluginReference]
        Plugin MonumentFinder;
        private const string usePerm = "lockprogression.use";
        bool lockProgression = false;

        float time = 3600f;
        DateTime startTime;

        void Init()
        {
            permission.RegisterPermission(usePerm, this);
        }

        [ChatCommand("starttimer")]
        void StartTimer(BasePlayer player, string command, string[] args)
        {
            if (!permission.UserHasPermission(player.UserIDString, usePerm))
            {
                SendReply(player, "You don't have permission to use this command");
                return;
            }

            startTime = DateTime.UtcNow;
            lockProgression = true;
            timer.Once(time, () => { lockProgression = false; Server.Broadcast("The Oilrigs crates are now hackable!"); });
        }


        void OnEntitySpawned(BaseNetworkable entity)
        {
            if (entity.ShortPrefabName.Contains("cargoship") && lockProgression)
            {
                Puts(entity.ShortPrefabName);
                entity.Kill();
            }

            if (entity.ShortPrefabName == "patrolhelicopter" && lockProgression)
            {
                Puts(entity.ShortPrefabName);
                entity.Kill();
            }
        }

        private object CanHackCrate(BasePlayer player, HackableLockedCrate crate)
        {
            if (crate.PrefabName == "assets/prefabs/deployable/chinooklockedcrate/codelockedhackablecrate_oilrig.prefab" && lockProgression)
            {
                int secondsRemaining = (int)(time - (DateTime.UtcNow - startTime).TotalSeconds);
                string humanizedTime = TimeSpan.FromSeconds(secondsRemaining).Minutes + " minute" + (TimeSpan.FromSeconds(secondsRemaining).Minutes == 1 ? "" : "s") + " and " + TimeSpan.FromSeconds(secondsRemaining).Seconds + " second" + (TimeSpan.FromSeconds(secondsRemaining).Seconds == 1 ? "" : "s");
                player.ChatMessage($"You can't hack this crate for {humanizedTime}");
                return true;
            }
            return null;
        }

    }
}
