using Oxide.Core;
using System;
using Oxide.Core.Plugins;
using Oxide.Game.Rust.Cui;
using System.Linq;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace Oxide.Plugins
{
    [Info("Welcome", "Auro", "1.0.0")]
    [Description("Sends a welcome message to players")]
    public class Welcome : RustPlugin
    {
        void OnPlayerConnected(BasePlayer player)
        {
            player.ChatMessage(config.welcomeMessage.Replace("{players}", BasePlayer.activePlayerList.Count.ToString()));
        }

        private static ConfigData config;

        private class ConfigData
        {
            [JsonProperty(PropertyName = "Welcome Message")]
            public string welcomeMessage;
        }

        private ConfigData GetDefaultConfig()
        {
            return new ConfigData
            {
                welcomeMessage = "Welcome to the Server",
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
