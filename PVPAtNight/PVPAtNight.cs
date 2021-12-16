using Oxide.Core;
using System;
using System.Collections.Generic;
using Oxide.Core.Plugins;
using Oxide.Game.Rust.Cui;
using Rust;
using System.Linq;

namespace Oxide.Plugins
{
    [Info("PVPAtNight", "Auro", "1.0.0")]
    [Description("Shows a status when PVP at night is enabled")]
    public class PVPAtNight : RustPlugin
    {
        private bool canPvp = false;
        CuiElementContainer cachedPVPUI = null;

        private Dictionary<string, int> itemList = new Dictionary<string, int>
        {
            {"wood", 2000},
            {"stones", 2000},
            {"metal.refined", 20},
            {"metal.fragments", 1000},
            {"lowgradefuel", 250},
            {"ammo.rifle", 64},
            {"ammo.pistol", 64},
            {"explosive.satchel", 4},
            {"ammo.rifle.explosive", 12},
            {"smg.2", 1},
            {"smg.thompson", 1},
            {"grenade.f1", 4},
        };


        [PluginReference]
        Plugin TimeOfDay;

        void Init()
        {
            cachedPVPUI = CreatePVPUI();

            if (TimeOfDay == null)
            {
                Puts("TimeOfDay is not loaded");
                return;
            }
        }

        private void OnEntityDeath(BaseCombatEntity victimEntity, HitInfo hitInfo)
        {
            if (victimEntity is BasePlayer)
            {
                if (victimEntity is NPCPlayer)
                {
                    //
                }
                else
                {
                    if (victimEntity.lastDamage == DamageType.Bleeding)
                        return;

                    if (canPvp == true)
                    {
                        BaseEntity attacker = victimEntity.lastAttacker;
                        if (attacker == null) return;

                        if (attacker == victimEntity.ToPlayer()) return;


                        if (attacker is BasePlayer)
                        {
                            if (victimEntity.ToPlayer().Team != null && victimEntity.ToPlayer().Team.members.Contains(attacker.ToPlayer().userID))
                                return;

                            var item = itemList.ElementAt(UnityEngine.Random.Range(0, itemList.Count));
                            (attacker as BasePlayer).inventory.GiveItem(ItemManager.CreateByItemID(ItemManager.FindItemDefinition(item.Key).itemid, item.Value));
                            Server.Broadcast($"{attacker.ToPlayer().displayName} has received {item.Value}x {ItemManager.FindItemDefinition(item.Key).displayName.english} for killing {victimEntity.ToPlayer().displayName}");
                        }
                    }
                }
            }
            else
            {
                //
            }


        }

        [ChatCommand("test")]
        void Test(BasePlayer player, string command, string[] args)
        {
        }


        void OnTimeSunset()
        {
            foreach (BasePlayer player in BasePlayer.activePlayerList)
            {
                CuiHelper.AddUi(player, cachedPVPUI);
                Server.Command("gather.rate dispenser Stones 3");
                Server.Command("gather.rate dispenser Wood 3");
                Server.Command("gather.rate dispenser \"Sulfur Ore\" 3");
                Server.Command("gather.rate dispenser \"Metal Ore\" 3");
            }

            Server.Broadcast("As the sun sets, PVP is now enabled and farming is now 3x");
            canPvp = true;
            Puts("PVP is enabled");
        }

        void OnTimeSunrise()
        {
            foreach (BasePlayer player in BasePlayer.activePlayerList)
            {
                CuiHelper.DestroyUi(player, "PVPUI");
            }

            Server.Broadcast("As the sun rises, PVP is now disabled and farming is now 2x");
            Server.Command("gather.rate dispenser Stones 2");
            Server.Command("gather.rate dispenser Wood 2");
            Server.Command("gather.rate dispenser \"Sulfur Ore\" 2");
            Server.Command("gather.rate dispenser \"Metal Ore\" 2");
            canPvp = false;
            Puts("PVP is disabled");
        }

        private CuiElementContainer CreatePVPUI()
        {
            CuiElementContainer elements = new CuiElementContainer();
            string panel = elements.Add(new CuiPanel
            {
                Image = { Color = "0.5 0.5 0.5 0.0" },
                RectTransform = { AnchorMin = "0.175 0.017", AnchorMax = "0.22 0.08" }
            }, "Hud.Menu", "PVPUI");
            elements.Add(new CuiElement
            {
                Parent = panel,
                Components =
                {
                    new CuiRawImageComponent {Color = "1 1 1 0.3", Url = "https://i.mrauro.dev/pvpicon.png"},
                    new CuiRectTransformComponent {AnchorMin = "0 0", AnchorMax = "1 1"}
                }
            });
            return elements;
        }
    }
}
