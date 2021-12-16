using Oxide.Core;
using System;
using Oxide.Core.Plugins;
using Oxide.Game.Rust.Cui;
using System.Linq;
using Newtonsoft.Json;
using UnityEngine;
using Newtonsoft.Json.Linq;
using System.Collections.Generic;

namespace Oxide.Plugins
{
    [Info("CheckForHeli", "Auro", "1.0.0")]
    [Description("Checks if you have a minicopter on the helipad when calling heavies")]
    public class CheckForHeli : RustPlugin
    {
        [PluginReference]
        Plugin MonumentFinder;

        void OnCardSwipe(CardReader cardReader, Keycard card, BasePlayer player)
        {
            var monumentResult = MonumentFinder.Call("API_GetClosest", player.transform.position) as Dictionary<string, object>;
            var monument = monumentResult["ShortName"] as string;

            Puts(monument);

            if (monument == "OilrigAI" || monument == "OilrigAI2")
            {
                Puts(cardReader.accessLevel.ToString());
                if (cardReader.accessLevel == 3)
                {
                    if (monument == "OilrigAI")
                    {
                        var minicopter = UnityEngine.Object.FindObjectsOfType<MiniCopter>().FirstOrDefault();
                        if (!minicopter) return;

                        Puts($"{Convert.ToInt32(config.smoil_x) + 1} - {Convert.ToInt32(config.smoil_z) + 1}");

                        var distance = Math.Sqrt(Math.Pow(minicopter.transform.position.x - Convert.ToInt32(config.smoil_x), 2) + Math.Pow(minicopter.transform.position.z - Convert.ToInt32(config.smoil_z), 2));
                        Puts($"{distance}");
                        if (distance <= 11)
                        {
                            player.ChatMessage("You have a minicopter on the helipad, you should get it off before calling heavies or else it will explode!");
                        }
                    }
                    else if (monument == "OilrigAI2")
                    {
                        var minicopter = UnityEngine.Object.FindObjectsOfType<MiniCopter>().FirstOrDefault();
                        if (!minicopter) return;

                        var distance = Math.Sqrt(Math.Pow(minicopter.transform.position.x - Convert.ToInt32(config.large_x), 2) + Math.Pow(minicopter.transform.position.z - Convert.ToInt32(config.large_z), 2));
                        if (distance <= 11)
                        {
                            player.ChatMessage("You have a minicopter on the helipad, you should get it off before calling heavies or else it will explode!");
                        }
                    }
                    else
                    {
                        throw new Exception("Monument not OilrigAI or OilrigAI2");
                    }

                }
            }
            return;
        }

        private static ConfigData config;

        private class ConfigData
        {
            [JsonProperty(PropertyName = "Small Oil Rig Helipad X Position")]
            public string smoil_x;

            [JsonProperty(PropertyName = "Small Oil Rig Helipad Z Position")]
            public string smoil_z;

            [JsonProperty(PropertyName = "Large Oil Rig Helipad X Position")]
            public string large_x;

            [JsonProperty(PropertyName = "Large Oil Rig Helipad Z Position")]
            public string large_z;
        }

        private ConfigData GetDefaultConfig()
        {
            return new ConfigData
            {
                smoil_x = "Use printpos to get this value",
                smoil_z = "Use printpos to get this value",
                large_x = "Use printpos to get this value",
                large_z = "Use printpos to get this value"
            };
        }

        protected override void LoadConfig()
        {
            base.LoadConfig();

            try
            {
                config = Config.ReadObject<ConfigData>();

                if (config == null)
                {
                    LoadDefaultConfig();
                }
            }
            catch
            {
                PrintError("Configuration file is corrupt! Check your config file at https://jsonlint.com/");

                timer.Every(10f, () =>
                {
                    PrintError("Configuration file is corrupt! Check your config file at https://jsonlint.com/");
                });
                LoadDefaultConfig();
                return;
            }

            SaveConfig();
        }

        protected override void LoadDefaultConfig()
        {
            config = GetDefaultConfig();
        }

        protected override void SaveConfig()
        {
            Config.WriteObject(config);
        }
    }
}
