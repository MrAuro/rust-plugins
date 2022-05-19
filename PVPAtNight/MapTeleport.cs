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
    [Info("Map Teleport", "Auro", "1.0.0")]
    [Description("Teleports player to marker on map when placed")]
    public class MapTeleport : RustPlugin
    {

        private const string PermissionUse = "mapteleport.use";
        private List<string> pendingPlayers = new List<string>();

        private void Init()
        {
            permission.RegisterPermission(PermissionUse, this);
        }

        private void OnMapMarkerAdded(BasePlayer player, MapNote note)
        {
            if (player == null || note == null)
            {
                return;
            }

            if (!permission.UserHasPermission(player.UserIDString, PermissionUse))
            {
                return;
            }

            if (player.isMounted)
            {
                player.ChatMessage("You cannot teleport while mounted");
                return;
            }

            if (!player.IsAlive())
            {
                player.ChatMessage("You cannot teleport while dead");
                return;
            }

            if (!pendingPlayers.Contains(player.UserIDString))
            {
                return;
            }

            ulong userID = player.userID;
            player.flyhackPauseTime = 15f;
            var pos = note.worldPosition;
            pos.y = GetGroundPosition(pos);

            player.Teleport(pos);
            player.RemoveFromTriggers();
            player.ForceUpdateTriggers();

            pendingPlayers.Remove(player.UserIDString);
            player.ChatMessage($"Teleported to <color=#FFA500>{pos}</color>");
        }

        static float GetGroundPosition(Vector3 pos)
        {
            float y = TerrainMeta.HeightMap.GetHeight(pos);
            RaycastHit hitInfo;

            if (Physics.Raycast(
                new Vector3(pos.x, pos.y + 200f, pos.z),
                Vector3.down,
                out hitInfo,
                float.MaxValue,
                (Rust.Layers.Mask.Vehicle_Large | Rust.Layers.Solid | Rust.Layers.Mask.Water)))
            {
                var cargoShip = hitInfo.GetEntity() as CargoShip;
                if (cargoShip != null)
                {
                    return hitInfo.point.y;
                }

                return Mathf.Max(hitInfo.point.y, y);
            }

            return y;
        }

        [ChatCommand("mtp")]
        void MapTeleportCommand(BasePlayer player, string command, string[] args)
        {
            if (!permission.UserHasPermission(player.UserIDString, PermissionUse))
            {
                player.ChatMessage("NOIDONTTHINKSO");
                return;
            }

            if (pendingPlayers.Contains(player.UserIDString))
            {
                player.ChatMessage("You already have a pending map teleportation");
            } else {
                pendingPlayers.Add(player.UserIDString);
                player.ChatMessage("Place a marker on the map to teleport to it");
                timer.Once(30.0f, () => { 
                    if (pendingPlayers.Contains(player.UserIDString)) {
                        pendingPlayers.Remove(player.UserIDString);
                        player.ChatMessage("Map teleporting cancelled");
                    }
                });
            }
        }
    }

}