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
    [Info("PvpAction", "Auro", "2.0.0")]
    [Description("PvpAtNight Renewed")]
    public class PvpAction : CovalencePlugin
    {
        [PluginReference]
        Plugin TimeOfDay, MonumentFinder;

        public bool PvpAllowed = false;

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

        private string[] allowedMonuments = new string[]
        {
            "airfield_1",
            "desert_military_base_a",
            "desert_military_base_b",
            "desert_military_base_c",
            "desert_military_base_d",
            "excavator_1",
            "harbor_1",
            "harbor_2",
            "junkyard_1",
            "launch_site_1",
            "military_tunnel_1",
            "oilrig_1",
            "oilrig_2",
            "powerplant_1",
            "satellite_dish",
            "sphere_tank",
            "trainyard_1",
            "underwater_lab_a",
            "underwater_lab_b",
            "underwater_lab_c",
            "underwater_lab_d",
            "warehouse",
            "water_treatment_plant_1",
        };

        void EnablePVP(string broadcastMsg)
        {
            foreach (BasePlayer player in BasePlayer.activePlayerList)
            {
                CuiHelper.AddUi(player, cachedPVPUI);
            }

            server.Command("gather.rate dispenser Stones 3");
            server.Command("gather.rate dispenser Wood 3");
            server.Command("gather.rate dispenser \"Sulfur Ore\" 3");
            server.Command("gather.rate dispenser \"Metal Ore\" 3");

            server.Broadcast(broadcastMsg);
            PvpAllowed = true;
            Interface.CallHook("API_EnablePVP");
            Puts("PvP is enabled");
        }

        void DisablePVP(string broadcastMsg)
        {
            foreach (BasePlayer player in BasePlayer.activePlayerList)
            {
                CuiHelper.DestroyUi(player, "PVPUI");
            }

            server.Command("gather.rate dispenser Stones 2");
            server.Command("gather.rate dispenser Wood 2");
            server.Command("gather.rate dispenser \"Sulfur Ore\" 2");
            server.Command("gather.rate dispenser \"Metal Ore\" 2");

            server.Broadcast(broadcastMsg);
            PvpAllowed = false;
            Interface.CallHook("API_DisablePVP");
            Puts("PvP is disabled");
        }


        void Init()
        {
            cachedPVPUI = CreatePVPUI();

            if (TimeOfDay == null)
            {
                Puts("TimeOfDay is not loaded");
                return;
            }

            if (MonumentFinder == null)
            {
                Puts("MonumentFinder is not loaded");
                return;
            }
        }

        void OnTimeSunset()
        {
            EnablePVP("As the sun sets, PVP is now enabled and farming is now 3x");
        }

        void OnTimeSunrise()
        {
            var random = UnityEngine.Random.Range(0, 100);
            if (random > 33)
            {
                Puts("The Purge IS happening today");
                StartPurgeTimer();
            }
            else
            {
                Puts("The Purge is NOT happening today");
            }
            DisablePVP("As the sun rises, PVP is now disabled and farming is now 2x");
        }

        void StartPurgeTimer()
        {
            // generate a time between 10 and 40 minutes
            var timeUntilPurge = UnityEngine.Random.Range(10, 20);

            Puts($"The Purge will happen in {timeUntilPurge} minutes");

            // start the timer
            timer.Once(timeUntilPurge * 60, () =>
            {
                EnablePVP("The Purge has begun! PVP is now enabled and farming is now 3x for the next 5 minutes");
                timer.Once(5 * 60, () =>
                {
                    DisablePVP("The Purge has ended. PVP is now disabled and farming is now 2x");
                });
            });
        }

        private void OnEntityDeath(BaseCombatEntity victimEntity, HitInfo hitInfo)
        {
            MonumentAdapter prefab = GetClosestMonument(victimEntity.transform.position);
            if (prefab != null)
            {
                if (prefab.IsInBounds(victimEntity.transform.position))
                {
                    Puts($"{victimEntity.GetType()} was killed in {prefab.ShortName}");
                }
            }

            if (victimEntity is BasePlayer && !(victimEntity is NPCPlayer))
            {
                if (victimEntity.lastDamage == Rust.DamageType.Bleeding) ;

                if (PvpAllowed)
                {
                    BasePlayer attackerPlayer = victimEntity.lastAttacker.ToPlayer();
                    BasePlayer victimPlayer = victimEntity.ToPlayer();

                    if (attackerPlayer == null) return;


                    if (IsTeamKill(attackerPlayer, victimPlayer)) return;


                    var item = itemList.ElementAt(UnityEngine.Random.Range(0, itemList.Count));
                    attackerPlayer.inventory.GiveItem(ItemManager.CreateByItemID(ItemManager.FindItemDefinition(item.Key).itemid, item.Value));
                    server.Broadcast($"{attackerPlayer.displayName} has received {item.Value}x {ItemManager.FindItemDefinition(item.Key).displayName.english} for killing {victimPlayer.displayName}");
                }
                else
                {
                    if (prefab != null)
                    {
                        if (prefab.IsInBounds(victimEntity.transform.position))
                        {
                            Puts($"{victimEntity.GetType()} was killed in {prefab.ShortName}");
                        }
                    }
                }
            }
        }

        bool IsTeamKill(BasePlayer attacker, BasePlayer victim)
        {
            return victim.ToPlayer().Team != null && victim.ToPlayer().Team.members.Contains(attacker.userID);
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

        class MonumentAdapter
        {
            public MonoBehaviour Object => (MonoBehaviour)_monumentInfo["Object"];
            public string PrefabName => (string)_monumentInfo["PrefabName"];
            public string ShortName => (string)_monumentInfo["ShortName"];
            public string Alias => (string)_monumentInfo["Alias"];
            public Vector3 Position => (Vector3)_monumentInfo["Position"];
            public Quaternion Rotation => (Quaternion)_monumentInfo["Rotation"];

            private Dictionary<string, object> _monumentInfo;

            public MonumentAdapter(Dictionary<string, object> monumentInfo)
            {
                _monumentInfo = monumentInfo;
            }

            public Vector3 TransformPoint(Vector3 localPosition) =>
                ((Func<Vector3, Vector3>)_monumentInfo["TransformPoint"]).Invoke(localPosition);

            public Vector3 InverseTransformPoint(Vector3 worldPosition) =>
                ((Func<Vector3, Vector3>)_monumentInfo["InverseTransformPoint"]).Invoke(worldPosition);

            public Vector3 ClosestPointOnBounds(Vector3 position) =>
                ((Func<Vector3, Vector3>)_monumentInfo["ClosestPointOnBounds"]).Invoke(position);

            public bool IsInBounds(Vector3 position) =>
                ((Func<Vector3, bool>)_monumentInfo["IsInBounds"]).Invoke(position);
        }

        // Call this method within your plugin to get the closest monument, train tunnel, or underwater lab.
        MonumentAdapter GetClosestMonument(Vector3 position)
        {
            var dictResult = MonumentFinder?.Call("API_GetClosestMonument", position) as Dictionary<string, object>;
            return dictResult != null ? new MonumentAdapter(dictResult) : null;
        }
    }


}