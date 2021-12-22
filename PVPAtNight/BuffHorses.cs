using Oxide.Core;
using System;
using Oxide.Core.Plugins;
using Oxide.Game.Rust.Cui;
using System.Linq;

namespace Oxide.Plugins
{
    [Info("BuffHorses", "Auro", "1.0.0")]
    [Description("Buffs horses' speed and stamina")]
    public class BuffHorses : RustPlugin
    {
        private const string PREFAB_RIDABLE_HORSE = "assets/rust.ai/nextai/testridablehorse.prefab";

        private void OnEntitySpawned(RidableHorse ridableHorse)
        {
            if (ridableHorse != null)
            {
                ApplyHorseSettings(ridableHorse);
            }
        }

        private void OnServerInitialized()
        {
            foreach (var ridableHorse in BaseNetworkable.serverEntities.OfType<RidableHorse>())
            {
                if (ridableHorse != null)
                {
                    ApplyHorseSettings(ridableHorse);
                }
            }
        }

        private void ApplyHorseSettings(RidableHorse ridableHorse)
        {
            ridableHorse.maxSpeed = 25;
            ridableHorse.walkSpeed = 3f;
            ridableHorse.trotSpeed = 7;
            ridableHorse.runSpeed = 18;
            ridableHorse.turnSpeed = 100;
            ridableHorse.roadSpeedBonus = 1;

            Puts(ridableHorse.staminaCoreLossRatio.ToString());
            Puts(ridableHorse.staminaCoreSpeedBonus.ToString());
            Puts(ridableHorse.staminaReplenishRatioMoving.ToString());
            Puts(ridableHorse.staminaReplenishRatioStanding.ToString());
            Puts(ridableHorse.staminaSeconds.ToString());

            ridableHorse.staminaSeconds = 9999999999;
            ridableHorse.maxStaminaSeconds = 9999999999;
            ridableHorse.staminaReplenishRatioMoving = 2f;
            ridableHorse.staminaReplenishRatioStanding = 2f;
            ridableHorse.staminaCoreLossRatio = 0.01f;

            ridableHorse.ReplenishStamina(1000000);
            ridableHorse.ReplenishStaminaCore(1000000, 1000000);

            ridableHorse.SendNetworkUpdate();
        }
    }
}
