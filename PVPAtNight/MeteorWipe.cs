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
        /*
        [ChatCommand("meteorwipe")]
        private void MeteorWipeCommand(BasePlayer player, string command, string[] args)
        {
            if (!permission.UserHasPermission(player.UserIDString, PermissionName))
            {
                SendReply(player, "You don't have permission to use this command");
                return;
            }
            else
            {
                var mapsize = Terrain.activeTerrain.terrainData.size / 2;

                // Starting at the top left corner work to the bottom right corner
                // Vertical doesnt matter so ignore it
                for (var x = -mapsize.x; x <= mapsize.x; x += 10)
                {
                    for (var z = -mapsize.z; z <= mapsize.z; z += 10)
                    {
                        Puts($"X: {x},  Z: {z}");
                        var position = new Vector3(x, TerrainMeta.HeightMap.GetHeight(x, z), z);

                        SpawnMeteor(position);
                    }
                }

            }

            // Credit to BuzZ from the Ragnorok plugin for the code below
            void SpawnMeteor(Vector3 origin)
            {
                var launchAngle = UnityEngine.Random.Range(0.25f, 0.5f);
                var launchHeight = UnityEngine.Random.Range(100.0f, 250.0f);
                var launchDirection = (Vector3.up * -launchAngle + Vector3.right).normalized;
                var launchPosition = origin - launchDirection * launchHeight;
                var r = UnityEngine.Random.Range(0, 3);
                ItemDefinition projectileItem;
                // Fetch rocket of type <x>:
                switch (r)
                {
                    case 0:
                        projectileItem = GetBasicRocket();
                        break;

                    case 1:
                        projectileItem = GetHighVelocityRocket();
                        break;

                    case 2:
                        projectileItem = GetSmokeRocket();
                        break;

                    default:
                        projectileItem = GetFireRocket();
                        break;
                }
                // Create the in-game "Meteor" entity:
                var component = projectileItem.GetComponent<ItemModProjectile>();
                var entity = GameManager.server.CreateEntity(component.projectileObject.resourcePath, launchPosition, new Quaternion(), true);
                if (entity == null)
                {
                    return;
                }
                // Set Meteor speed:
                var serverProjectile = entity.GetComponent<ServerProjectile>();
                serverProjectile.speed = UnityEngine.Random.Range(25.0f, 75.0f);
                entity.SendMessage("InitializeVelocity", (object)(launchDirection * 1.0f));
                entity.OwnerID = 666999666999666;
                entity.Spawn();
            }

            ItemDefinition GetBasicRocket()
            {
                return ItemManager.FindItemDefinition("ammo.rocket.basic");
            }
            ItemDefinition GetFireRocket()
            {
                return ItemManager.FindItemDefinition("ammo.rocket.fire");
            }
            ItemDefinition GetHighVelocityRocket()
            {
                return ItemManager.FindItemDefinition("ammo.rocket.hv");
            }
            ItemDefinition GetSmokeRocket()
            {
                return ItemManager.FindItemDefinition("ammo.rocket.smoke");
            }
			}
        }
        */
        [ChatCommand("meteorwipe")]
        void MeteorWipeCommand(BasePlayer player, string command, string[] args)
        {
            if (!permission.UserHasPermission(player.UserIDString, PermissionName))
            {
                SendReply(player, "You don't have permission to use this command");
                return;
            }

            var mapsize = Terrain.activeTerrain.terrainData.size / 2;

            // Start from the middle of the map and work outwards in a spiral
            // Vertical doesnt matter so ignore it
            for (var x = -mapsize.x; x <= mapsize.x; x += 10)
            {
                for (var z = -mapsize.z; z <= mapsize.z; z += 10)
                {
                    Puts("X: " + x + ",  Z: " + z);
                    SpawnMeteor(new Vector3(x, 300.0f, z));
                }
            }

            SendReply(player, "daaaank");
        }

        [ChatCommand("spawnmeteor")]
        void SpawnMeteorCommand(BasePlayer player, string command, string[] args)
        {
            if (!permission.UserHasPermission(player.UserIDString, PermissionName))
            {
                SendReply(player, "You don't have permission to use this command");
                return;
            }

            Puts(player.transform.position.ToString());

            SpawnMeteor(player.transform.position);
        }

        void SpawnMeteor(Vector3 origin)
        {
            var launchAngle = 0.0f;
            var launchHeight = 250.0f;
            var launchDirection = (Vector3.up * -launchAngle + Vector3.right).normalized;
            var launchPosition = origin - launchDirection * launchHeight;
            var r = UnityEngine.Random.Range(0, 3);
            ItemDefinition projectileItem;
            // Fetch rocket of type <x>:
            switch (r)
            {
                case 0:
                    projectileItem = GetBasicRocket();
                    break;

                case 1:
                    projectileItem = GetHighVelocityRocket();
                    break;

                case 2:
                    projectileItem = GetSmokeRocket();
                    break;

                default:
                    projectileItem = GetFireRocket();
                    break;
            }
            // Create the in-game "Meteor" entity:
            var component = projectileItem.GetComponent<ItemModProjectile>();
            var entity = GameManager.server.CreateEntity(component.projectileObject.resourcePath, launchPosition, new Quaternion(), true);
            if (entity == null)
            {
                return;
            }
            // Set Meteor speed:
            var serverProjectile = entity.GetComponent<ServerProjectile>();
            serverProjectile.speed = 75.0f;
            entity.SendMessage("InitializeVelocity", (object)(launchDirection * 1.0f));
            entity.OwnerID = 666999666999666;
            entity.Spawn();
        }

        ItemDefinition GetBasicRocket()
        {
            return ItemManager.FindItemDefinition("ammo.rocket.basic");
        }
        ItemDefinition GetFireRocket()
        {
            return ItemManager.FindItemDefinition("ammo.rocket.fire");
        }
        ItemDefinition GetHighVelocityRocket()
        {
            return ItemManager.FindItemDefinition("ammo.rocket.hv");
        }
        ItemDefinition GetSmokeRocket()
        {
            return ItemManager.FindItemDefinition("ammo.rocket.smoke");
        }
    }
}