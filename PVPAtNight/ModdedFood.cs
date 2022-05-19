using Oxide.Core;
using System;
using Oxide.Core.Plugins;
using Oxide.Game.Rust.Cui;
using System.Linq;
using UnityEngine;
using System.Collections.Generic;

namespace Oxide.Plugins
{
    [Info("ModdedFood", "Auro", "1.0.0")]
    [Description("Replaces burnt food variants with special ones")]
    public class ModdedFood : RustPlugin
    {
        string craftPerm = "moddedfood.craft";

        void Init()
        {
            permission.RegisterPermission(craftPerm, this);
        }

        enum Foods
        {
            Pizza
        }

        [ChatCommand("craft")]
        void CraftCommand(BasePlayer player, string command, string[] args)
        {
            if (!permission.UserHasPermission(player.UserIDString, craftPerm))
            {
                SendReply(player, "You don't have permission to use this command");
                return;
            }

            RaycastHit hit;
            var raycast = Physics.Raycast(player.eyes.HeadRay(), out hit, 5f);
            BaseEntity entity = raycast ? hit.GetEntity() : null;

            if (entity == null)
            {
                SendReply(player, "You need to be looking at a barbeque");
                return;
            }

            if (entity is StorageContainer)
            {
                var container = entity as StorageContainer;
                if (container.name == "assets/prefabs/deployable/bbq/bbq.deployed.prefab")
                {
                    if (!args.Any())
                    {
                        SendReply(player, "You need to specify a food to craft");
                        return;
                    }


                    switch (args[0])
                    {
                        case "pizza":
                            var pizza = ItemManager.CreateByName("bearmeat.burned", 1, 1902068186);
                            pizza.name = "Pizza";
                            ItemModConsumable component = (ItemModConsumable)((Component)pizza.info).GetComponent<ItemModConsumable>();
                            foreach (var effect in component.effects)
                            {
                                Puts($"{effect.type} - {effect.amount}");
                                if (effect.type == MetabolismAttribute.Type.HealthOverTime)
                                {
                                    effect.amount = 100f;
                                }
                            }

                            pizza.info.GetComponent<ItemModConsumable>().effects = component.effects;

                            player.inventory.GiveItem(pizza);
                            break;

                        case "burger":
                            var burger = ItemManager.CreateByName("deermeat.burned", 1, 2668022576);
                            burger.name = "Burger";
                            ItemModConsumable component2 = (ItemModConsumable)((Component)burger.info).GetComponent<ItemModConsumable>();
                            foreach (var effect in component2.effects)
                            {
                                Puts($"{effect.type} - {effect.amount}");
                                if (effect.type == MetabolismAttribute.Type.HealthOverTime)
                                {
                                    effect.amount = 100f;
                                }
                            }

                            burger.info.GetComponent<ItemModConsumable>().effects = component2.effects;

                            player.inventory.GiveItem(burger);
                            break;
                    }

                }
                else
                {
                    SendReply(player, "You need to be looking at a barbeque");
                    return;
                }
            }
        }
    }
}
