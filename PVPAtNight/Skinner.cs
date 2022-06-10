using Network;
using Newtonsoft.Json;
using Oxide.Core;
using Oxide.Core.Configuration;
using Oxide.Core.Libraries.Covalence;
using Oxide.Game.Rust.Cui;
using Oxide.Core.Plugins;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
// https://modhub.to
// This is a user submitted Rust Plugin checked and verified by ModHub
// ModHub is the largest Server Owner Trading Platform Online
// Contains modifications made by Auro
namespace Oxide.Plugins
{
    [Info("Skinner", "ModHub.to", "1.1.7")]
    [Description("Brings automation and ease to skinning items")]
    public class Skinner : CovalencePlugin
    {
        [PluginReference]
        private Plugin Economics;

        static Skinner skinner;
        private DynamicConfigFile _defaultSkins;

        private const string permdefault = "skinner.default";
        private const string permitems = "skinner.items";
        private const string permcraft = "skinner.craft";
        private const string permskininv = "skinner.skininv";
        private const string permskincon = "skinner.skincon";
        private const string permbypassauth = "skinner.bypassauth";
        private const string permimport = "skinner.import";

        private static List<string> _registeredhooks = new List<string> { "OnLootEntityEnd", "CanStackItem" };//, "OnMaxStackable" };
        private Dictionary<ulong, Dictionary<string, CachedSkin>> _playerDefaultSkins = new Dictionary<ulong, Dictionary<string, CachedSkin>>();
        private List<string> permissions = new List<string>() { permdefault, permcraft, permitems, permbypassauth, permskincon, permskininv, permimport };
        private Dictionary<ulong, CoolDowns> _playercooldowns = new Dictionary<ulong, CoolDowns>();
        public class CoolDowns
        {
            public float skin = 30f;
            public float skinitem = 30f;
            public float skincraft = 30f;
            public float skincon = 30f;
            public float skininv = 30f;
        }

        #region Init
        private void Init()
        {
            skinner = this;

            foreach (string perm in permissions)
                permission.RegisterPermission(perm, this);

            foreach (string perm in config.Cooldowns.Keys)
                permission.RegisterPermission($"skinner.{perm}", this);

            AddCovalenceCommand(config.cmdsskin, "SkinCMD");
            AddCovalenceCommand(config.cmdsskincraft, "DefaultSkinsCMD");
            AddCovalenceCommand(config.cmdsskinitems, "SkinItemCMD");
            AddCovalenceCommand(config.cmdsskininv, "SkinInvCMD");
            AddCovalenceCommand(config.cmdsskincon, "SkinConCMD");
            AddCovalenceCommand(config.cmdskinimport, "SkinImportCMD");
            AddCovalenceCommand(new[] { "sbNextPage" }, "SBNextPageCMD");
            AddCovalenceCommand(new[] { "sbBackPage" }, "SBBackPageCMD");
            GetSkins();
        }

        private void Loaded()
        {
            _defaultSkins = Interface.Oxide.DataFileSystem.GetFile("DefaultCraftSkins");
            LoadData();
        }
        private void Unload()
        {
            SaveData();
            foreach (var player in _viewingcon)
                player.EndLooting();

            foreach (var player in BasePlayer.activePlayerList)
            {
                CuiHelper.DestroyUi(player, "SkinPageUI");
            }
        }

        private class CachedSkin
        {
            public string shortname = string.Empty;
            public string displayName = string.Empty;
            public ulong skinid = 0;
            public bool redirect = false;
            public string redirectshortname = string.Empty;
            public ItemCategory category;
        }

        private Dictionary<string, List<CachedSkin>> _skinsCache = new Dictionary<string, List<CachedSkin>>();
        private Dictionary<string, string> _redirectSkins = new Dictionary<string, string>();
        private Dictionary<string, CachedSkin> _defaultskins = new Dictionary<string, CachedSkin>();
        private void GetSkins()
        {
            if ((Steamworks.SteamInventory.Definitions?.Length ?? 0) == 0)
            {
                Puts("Waiting for Steamworks to update skin item definitions");
                Steamworks.SteamInventory.OnDefinitionsUpdated += GetSkins;
                return;
            }
            int sk = 0;
            Puts("Steamworks Updated, Updating Skins");
            Steamworks.SteamInventory.OnDefinitionsUpdated -= GetSkins;

            foreach (ItemDefinition itemDef in ItemManager.GetItemDefinitions())
            {
                List<CachedSkin> skins = new List<CachedSkin>() { new CachedSkin() { skinid = 0uL, shortname = itemDef.shortname } };

                foreach (var skin in ItemSkinDirectory.ForItem(itemDef))
                {
                    if (config.blacklistedskins.Contains((ulong)skin.id)) continue;
                    if (skin.id == 0) continue;
                    CachedSkin cachedSkin = new CachedSkin { skinid = Convert.ToUInt64(skin.id), shortname = itemDef.shortname, displayName = skin.name };
                    ItemSkin itemSkin = skin.invItem as ItemSkin;
                    if (itemSkin != null && itemSkin.Redirect != null)
                    {
                        cachedSkin.redirect = true;
                        cachedSkin.redirectshortname = itemSkin.Redirect.shortname;
                        if (!_redirectSkins.ContainsKey(cachedSkin.redirectshortname))
                            _redirectSkins.Add(cachedSkin.redirectshortname, itemDef.shortname);
                    }
                    skins.Add(cachedSkin);
                }
                if (skins.Count > 1)
                {
                    _skinsCache.Add(itemDef.shortname, skins);
                }
            }

            foreach (Steamworks.InventoryDef item in Steamworks.SteamInventory.Definitions)
            {
                string shortname = item.GetProperty("itemshortname") == "lr300.item" ? "rifle.lr300" : item.GetProperty("itemshortname");
                if (string.IsNullOrEmpty(shortname) || item.Id < 100)
                    continue;

                ulong skinid;

                if (config.blacklistedskins.Contains((ulong)item.Id)) continue;
                if (!ulong.TryParse(item.GetProperty("workshopid"), out skinid))
                {
                    skinid = (ulong)item.Id;
                }
                if (skinid < 100000) continue;
                List<CachedSkin> skins;
                if (_skinsCache.TryGetValue(shortname, out skins))
                {
                    if (skins.Where(x => x.skinid == skinid || x.skinid == (ulong)item.Id).Count() > 0) continue;

                    skins.Add(new CachedSkin { skinid = Convert.ToUInt64(skinid), shortname = shortname, displayName = item.Name });
                }
                else
                {
                    _skinsCache.Add(shortname, new List<CachedSkin>() { new CachedSkin { skinid = 0, shortname = shortname }, new CachedSkin { skinid = Convert.ToUInt64(skinid), shortname = shortname, displayName = item.Name } });
                }
            }
            foreach (var whitelistSkin in config.Importedskins)
            {
                ItemDefinition itemdef = ItemManager.FindItemDefinition(whitelistSkin.Value);
                if (itemdef == null)
                {
                    Puts($"Could not find item definition for {whitelistSkin.Value} {whitelistSkin.Key}");
                    continue;
                }
                List<CachedSkin> skins2;
                if (_skinsCache.TryGetValue(itemdef.shortname, out skins2))
                {
                    skins2.Add(new CachedSkin { skinid = whitelistSkin.Key, shortname = itemdef.shortname });
                }
                else
                {
                    Puts($"Cannot apply skins to non skinable item {whitelistSkin.Value} {whitelistSkin.Key}");
                }
            }

            foreach (var item2 in _skinsCache.ToList())
            {
                if (_redirectSkins.ContainsKey(item2.Key))
                    continue;
                int skinsamt = item2.Value.Count;
                sk += skinsamt;
                if (skinsamt == 1)
                {
                    _skinsCache.Remove(item2.Key);
                    continue;
                }

                ItemDefinition itemdef = ItemManager.FindItemDefinition(item2.Key);
                if (itemdef == null || itemdef?.Blueprint == null)
                {
                    _skinsCache.Remove(item2.Key);
                    continue;
                }

                //if (!itemdef.Blueprint.userCraftable)
                //    continue;

                _defaultskins.Add(item2.Key, new CachedSkin() { shortname = item2.Key, category = itemdef.category });
            }

            //Re-order to look nice
            _defaultskins = _defaultskins.OrderBy(key => key.Value.category).ToDictionary(x => x.Key, x => x.Value);
            Puts($"{sk} skins were indexed, Skin indexing complete");
        }

        #endregion Init

        #region Configuration

        private Configuration config;

        public class Configuration
        {
            [JsonProperty("Skin Commands (skin items in you inventory")]
            public string[] cmdsskin = new[] { "skin", "s" };

            [JsonProperty("Skin Items Commands (skin items you have already placed")]
            public string[] cmdsskinitems = new[] { "skinitem", "si" };

            [JsonProperty("Set default items to be skinned")]
            public string[] cmdsskincraft = new[] { "skincraft", "sc" };

            [JsonProperty("Automatically set all items in you inventory to your default skins")]
            public string[] cmdsskininv = new[] { "skininv", "sinv" };

            [JsonProperty("Automatically set all items a container to your default skins")]
            public string[] cmdsskincon = new[] { "skincon", "scon" };

            [JsonProperty("Import Custom Skins")]
            public string[] cmdskinimport = new[] { "skinimport", "sip" };

            [JsonProperty("Custom UI Positon 'min x, min y', 'max x', max y'")]
            public string[] uiposition = new[] { "0.66 0.05", "0.82 0.1" };

            [JsonProperty("Blacklisted Skins (skinID)", ObjectCreationHandling = ObjectCreationHandling.Replace)]
            public List<ulong> blacklistedskins = new List<ulong>();

            [JsonProperty("Command based cooldowns ('permission' : 'command' seconds")]
            public Dictionary<string, CoolDowns> Cooldowns = new Dictionary<string, CoolDowns>() { { "Default30CD", new CoolDowns() } };

            [JsonProperty("Imported Skins (skinid : 'shortnamestring', skinid2 : 'shortnamestring2'", ObjectCreationHandling = ObjectCreationHandling.Replace)]
            public Dictionary<ulong, string> Importedskins = new Dictionary<ulong, string>() { { 861142659, "vending.machine" } };

            public string ToJson() => JsonConvert.SerializeObject(this);

            public Dictionary<string, object> ToDictionary() => JsonConvert.DeserializeObject<Dictionary<string, object>>(ToJson());
        }
        protected override void LoadDefaultConfig() => config = new Configuration();

        protected override void LoadConfig()
        {
            base.LoadConfig();
            try
            {
                config = Config.ReadObject<Configuration>();
                if (config == null)
                {
                    throw new JsonException();
                }

                if (!config.ToDictionary().Keys.SequenceEqual(Config.ToDictionary(x => x.Key, x => x.Value).Keys))
                {
                    Puts("Configuration appears to be outdated; updating and saving");
                    SaveConfig();
                }
            }
            catch
            {
                Puts($"Configuration file {Name}.json is invalid; using defaults");
                LoadDefaultConfig();
            }
        }

        protected override void SaveConfig()
        {
            Puts($"Configuration changes saved to {Name}.json");
            Config.WriteObject(config, true);
        }

        #endregion Configuration

        #region Data
        private void LoadData()
        {
            try
            {
                _playerDefaultSkins = _defaultSkins.ReadObject<Dictionary<ulong, Dictionary<string, CachedSkin>>>();
            }
            catch
            {
                _playerDefaultSkins = new Dictionary<ulong, Dictionary<string, CachedSkin>>();
            }
        }

        private void SaveData()
        {
            _defaultSkins.WriteObject(_playerDefaultSkins);
        }
        #endregion Data

        #region Localization
        protected override void LoadDefaultMessages()
        {
            lang.RegisterMessages(new Dictionary<string, string>
            {
                ["NoPerms"] = "You don't have permissions to use this command",
                ["Poor"] = "You don't have enough points to use this command",
                ["NoBuildingAuth"] = "You must have building auth to use this",
                ["NoObjectsFound"] = "No object found",
                ["NoSkins"] = "No skins available",
                ["ImportSkinArgs"] = "Bad args, Required input skinid itemshortname",
                ["SkinIDError"] = "Cannot parse skinid {0}, required input skinid itemshortname",
                ["NoShortname"] = "No item found for shortname : {0}",
                ["DuplicateSkin"] = "Duplicate Skin ID for : {0} {1}",
                ["SkinImported"] = "Skin {0} for {1} has been imported and saved",
                ["CommandCooldown"] = "You can not use this command for another {0}",
                ["CompletedInvSkin"] = "All items in your inventory have been set to your default skins",
                ["CompletedConSkin"] = "All items in {0} have been set to your default skins"
            }, this);
        }

        #endregion Localization

        #region Commands
        private void SkinImportCMD(IPlayer iplayer, string command, string[] args)
        {
            BasePlayer player = iplayer.Object as BasePlayer;
            if (player == null) return;
            if (!HasPerm(player.UserIDString, permimport))
            {
                ChatMessage(iplayer, "NoPerms");
                return;
            }
            if (args.Length < 2)
            {
                ChatMessage(iplayer, "ImportSkinArgs");
                return;
            }
            string shortname = args[1];
            ulong skinid = 0ul;
            if (!ulong.TryParse(args[0], out skinid))
            {
                ChatMessage(iplayer, "SkinIDError", args[0]);
                return;
            }
            ItemDefinition itemdef = ItemManager.FindItemDefinition(shortname);
            if (itemdef == null)
            {
                ChatMessage(iplayer, "NoShortname", args[1]);
                return;
            }

            List<CachedSkin> skins2;
            if (_skinsCache.TryGetValue(itemdef.shortname, out skins2))
            {
                if (!skins2.Any(x => x.skinid == skinid))
                {
                    skins2.Add(new CachedSkin { skinid = skinid, shortname = itemdef.shortname });
                    config.Importedskins.Add(skinid, itemdef.shortname);
                    ChatMessage(iplayer, "SkinImported", args[0], itemdef.shortname);
                    SaveConfig();
                }
                else
                {
                    ChatMessage(iplayer, "DuplicateSkin", args[0], itemdef.shortname);
                    return;
                }
            }
            else
            {
                ChatMessage(iplayer, "NoShortname", args[1]);
            }
        }

        private void SkinCMD(IPlayer iplayer, string command, string[] args)
        {
            BasePlayer player = iplayer.Object as BasePlayer;
            if (player == null) return;
            player.EndLooting();

            Puts("asdfasdfasdf");

            if (!HasPerm(player.UserIDString, permdefault))
            {
                ChatMessage(iplayer, "NoPerms");
                return;
            }

            double userBalance = (double)Economics.Call("Balance", player.userID);

            if (userBalance >= 30)
            {
                bool result = (bool)Economics.Call("Withdraw", player.userID, (double)30);
            }
            else
            {
                ChatMessage(iplayer, "Poor");
                return;
            }

            //Check for cooldown
            foreach (var cdperm in config.Cooldowns)
            {
                if (!HasPerm(player.UserIDString, $"skinner.{cdperm.Key}")) continue;
                CoolDowns coolDowns;
                if (!_playercooldowns.TryGetValue(player.userID, out coolDowns))
                    _playercooldowns.Add(player.userID, new CoolDowns() { skin = Time.time });
                else
                {
                    if (coolDowns.skin + cdperm.Value.skin > Time.time)
                    {
                        ChatMessage(iplayer, "CommandCooldown", TimeSpan.FromSeconds(coolDowns.skin + cdperm.Value.skin - Time.time).ToString("hh' hrs 'mm' mins 'ss' secs'"));
                        return;
                    }
                    coolDowns.skin = Time.time;
                }
            }

            StorageContainer storageContainer = CreateStorageCon(player);

            BoxController boxController;
            if (!storageContainer.TryGetComponent<BoxController>(out boxController))
            {
                storageContainer.Kill();
                return;

            }
            boxController.StartItemSkin();

            if (!_viewingcon.Contains(player))
                _viewingcon.Add(player);
            if (_viewingcon.Count == 1)
                SubscribeToHooks();

            timer.Once(0.3f, () =>
            {
                StartLooting(player, storageContainer);
            });
        }

        private void DefaultSkinsCMD(IPlayer iplayer, string command, string[] args)
        {
            BasePlayer player = iplayer.Object as BasePlayer;
            if (player == null) return;

            if (!HasPerm(player.UserIDString, permcraft))
            {
                ChatMessage(iplayer, "NoPerms");
                return;
            }

            //Check for cooldown
            foreach (var cdperm in config.Cooldowns)
            {
                if (!HasPerm(player.UserIDString, $"skinner.{cdperm.Key}")) continue;
                CoolDowns coolDowns;
                if (!_playercooldowns.TryGetValue(player.userID, out coolDowns))
                    _playercooldowns.Add(player.userID, new CoolDowns() { skincraft = Time.time });
                else
                {
                    if (coolDowns.skincraft + cdperm.Value.skincraft > Time.time)
                    {
                        ChatMessage(iplayer, "CommandCooldown", TimeSpan.FromSeconds(coolDowns.skincraft + cdperm.Value.skincraft - Time.time).ToString("hh' hrs 'mm' mins 'ss' secs'"));
                        return;
                    }
                    coolDowns.skincraft = Time.time;
                }
            }

            if (!_playerDefaultSkins.ContainsKey(player.userID))
                _playerDefaultSkins.Add(player.userID, new Dictionary<string, CachedSkin>());

            StorageContainer storageContainer = CreateStorageCon(player);

            BoxController boxController;
            if (!storageContainer.TryGetComponent<BoxController>(out boxController))
            {
                storageContainer.Kill();
                return;
            }
            boxController.GetDefaultSkins();

            if (!_viewingcon.Contains(player))
                _viewingcon.Add(player);

            if (_viewingcon.Count == 1)
                SubscribeToHooks();

            timer.Once(0.3f, () =>
            {
                StartLooting(player, storageContainer);
            });
        }

        private static int Layermask = LayerMask.GetMask("Deployed", "Construction");
        private void SkinItemCMD(IPlayer iplayer, string command, string[] args)
        {
            BasePlayer player = iplayer.Object as BasePlayer;
            if (player == null) return;

            if (!HasPerm(player.UserIDString, permitems))
            {
                ChatMessage(iplayer, "NoPerms");
                return;
            }

            if (!player.IsBuildingAuthed() && !HasPerm(player.UserIDString, permbypassauth))
            {
                ChatMessage(iplayer, "NoBuildingAuth");
                return;
            }

            RaycastHit raycastHit;
            if (!Physics.Raycast(player.eyes.HeadRay(), out raycastHit, 5f, Layermask))
            {
                ChatMessage(iplayer, "NoObjectsFound");
                return;
            }
            BaseCombatEntity entity = raycastHit.GetEntity() as BaseCombatEntity;
            if (entity == null || entity?.pickup.itemTarget == null)
            {
                ChatMessage(iplayer, "NoObjectsFound");
                return;
            }
            if (!_skinsCache.ContainsKey(entity.pickup.itemTarget.shortname))
            {
                ChatMessage(iplayer, "NoSkins");
                return;
            }

            //Check for cooldown
            foreach (var cdperm in config.Cooldowns)
            {
                if (!HasPerm(player.UserIDString, $"skinner.{cdperm.Key}")) continue;
                CoolDowns coolDowns;
                if (!_playercooldowns.TryGetValue(player.userID, out coolDowns))
                    _playercooldowns.Add(player.userID, new CoolDowns() { skinitem = Time.time });
                else
                {
                    if (coolDowns.skinitem + cdperm.Value.skinitem > Time.time)
                    {
                        ChatMessage(iplayer, "CommandCooldown", TimeSpan.FromSeconds(coolDowns.skinitem + cdperm.Value.skinitem - Time.time).ToString("hh' hrs 'mm' mins 'ss' secs'"));
                        return;
                    }
                    coolDowns.skinitem = Time.time;
                }
            }

            StorageContainer storageContainer = CreateStorageCon(player);
            BoxController boxController;
            if (!storageContainer.TryGetComponent<BoxController>(out boxController))
            {
                storageContainer.Kill();
                return;
            }
            boxController.SkinDeplyoables(entity);

            if (!_viewingcon.Contains(player))
                _viewingcon.Add(player);

            if (_viewingcon.Count == 1)
                SubscribeToHooks();

            timer.Once(0.3f, () =>
            {
                StartLooting(player, storageContainer);
            });
        }
        private void SkinInvCMD(IPlayer iplayer, string command, string[] args)
        {
            BasePlayer player = iplayer.Object as BasePlayer;
            if (player == null) return;
            player.EndLooting();

            if (!HasPerm(player.UserIDString, permskininv))
            {
                ChatMessage(iplayer, "NoPerms");
                return;
            }
            Dictionary<string, CachedSkin> cachedskins;
            if (!_playerDefaultSkins.TryGetValue(player.userID, out cachedskins))
            {
                ChatMessage(iplayer, "NoDefaultSkins");
                return;
            }
            if (player.inventory == null)
                return;

            //Check for cooldown
            foreach (var cdperm in config.Cooldowns)
            {
                if (!HasPerm(player.UserIDString, $"skinner.{cdperm.Key}")) continue;
                CoolDowns coolDowns;
                if (!_playercooldowns.TryGetValue(player.userID, out coolDowns))
                    _playercooldowns.Add(player.userID, new CoolDowns() { skininv = Time.time });
                else
                {
                    if (coolDowns.skininv + cdperm.Value.skininv > Time.time)
                    {
                        ChatMessage(iplayer, "CommandCooldown", TimeSpan.FromSeconds(coolDowns.skininv + cdperm.Value.skininv - Time.time).ToString("hh' hrs 'mm' mins 'ss' secs'"));
                        return;
                    }
                    coolDowns.skininv = Time.time;
                }
            }

            foreach (Item item in player.inventory.AllItems())
            {
                if (item == null) continue;
                CachedSkin cachedSkin;
                if (cachedskins.TryGetValue(item.info.shortname, out cachedSkin))
                {
                    if (cachedSkin.redirect) continue;
                    item.skin = cachedSkin.skinid;
                    BaseEntity held = item.GetHeldEntity();

                    if (held != null)
                    {
                        held.skinID = cachedSkin.skinid;
                        held.SendNetworkUpdate();
                    }
                }
            }
            player.inventory.containerWear.MarkDirty();
            player.inventory.containerBelt.MarkDirty();
            player.SendNetworkUpdateImmediate();
            ChatMessage(iplayer, "CompletedInvSkin");
        }

        private void SkinConCMD(IPlayer iplayer, string command, string[] args)
        {
            BasePlayer player = iplayer.Object as BasePlayer;
            if (player == null) return;
            player.EndLooting();

            if (!HasPerm(player.UserIDString, permskincon))
            {
                ChatMessage(iplayer, "NoPerms");
                return;
            }
            if (!player.IsBuildingAuthed() && !HasPerm(player.UserIDString, permbypassauth))
            {
                ChatMessage(iplayer, "NoBuildingAuth");
                return;
            }

            Dictionary<string, CachedSkin> cachedskins;
            if (!_playerDefaultSkins.TryGetValue(player.userID, out cachedskins))
            {
                ChatMessage(iplayer, "NoDefaultSkins");
                return;
            }

            RaycastHit raycastHit;
            if (!Physics.Raycast(player.eyes.HeadRay(), out raycastHit, 5f, Layermask))
            {
                ChatMessage(iplayer, "NoObjectsFound");
                return;
            }

            StorageContainer storage = raycastHit.GetEntity() as StorageContainer;
            if (storage == null || storage?.inventory == null)
            {
                ChatMessage(iplayer, "NoObjectsFound");
                return;
            }

            //Check for cooldown
            foreach (var cdperm in config.Cooldowns)
            {
                if (!HasPerm(player.UserIDString, $"skinner.{cdperm.Key}")) continue;
                CoolDowns coolDowns;
                if (!_playercooldowns.TryGetValue(player.userID, out coolDowns))
                    _playercooldowns.Add(player.userID, new CoolDowns() { skincon = Time.time });
                else
                {
                    if (coolDowns.skincon + cdperm.Value.skincon > Time.time)
                    {
                        ChatMessage(iplayer, "CommandCooldown", TimeSpan.FromSeconds(coolDowns.skincon + cdperm.Value.skincon - Time.time).ToString("hh' hrs 'mm' mins 'ss' secs'"));
                        return;
                    }
                    coolDowns.skincon = Time.time;
                }
            }

            foreach (Item item in storage.inventory.itemList)
            {
                if (item == null) continue;
                CachedSkin cachedSkin;
                if (cachedskins.TryGetValue(item.info.shortname, out cachedSkin))
                {
                    if (cachedSkin.redirect) continue;
                    item.skin = cachedSkin.skinid;
                    BaseEntity held = item.GetHeldEntity();

                    if (held != null)
                    {
                        held.skinID = cachedSkin.skinid;
                        held.SendNetworkUpdate();
                    }
                }
            }
            storage.SendNetworkUpdateImmediate();
            ChatMessage(iplayer, "CompletedConSkin", storage.ShortPrefabName);
        }

        private void SBNextPageCMD(IPlayer iplayer, string command, string[] args)
        {
            BasePlayer player = iplayer.Object as BasePlayer;
            if (player == null) return;
            if (!_viewingcon.Contains(player)) return;
            StorageContainer storagecontainer = player.inventory.loot.entitySource as StorageContainer;
            if (storagecontainer == null) return;
            BoxController boxController;
            if (!storagecontainer.TryGetComponent<BoxController>(out boxController))
                return;
            if (boxController._fillingbox || boxController._clearingbox)
                return;
            boxController.NextPage();
        }

        private void SBBackPageCMD(IPlayer iplayer, string command, string[] args)
        {
            BasePlayer player = iplayer.Object as BasePlayer;
            if (player == null) return;
            if (!_viewingcon.Contains(player)) return;
            StorageContainer storagecontainer = player.inventory.loot.entitySource as StorageContainer;
            if (storagecontainer == null) return;
            BoxController boxController;
            if (!storagecontainer.TryGetComponent<BoxController>(out boxController))
                return;
            if (boxController._fillingbox || boxController._clearingbox)
                return;
            boxController.BackPage();
        }

        #endregion Commands

        #region Hooks

        private object CanStackItem(Item item, Item targetItem) => (targetItem.parent?.entityOwner?._limitedNetworking ?? false) ? false : (object)null;

        private List<BasePlayer> _viewingcon = new List<BasePlayer>();
        private void OnLootEntityEnd(BasePlayer player, StorageContainer storageContainer)
        {
            if (!_viewingcon.Contains(player)) return;

            _viewingcon.Remove(player);

            CuiHelper.DestroyUi(player, "SkinPageUI");

            if (_viewingcon.Count < 1)
                UnSubscribeFromHooks();

            BoxController boxController;
            if (storageContainer.TryGetComponent<BoxController>(out boxController))
            {
                UnityEngine.Object.Destroy(boxController);
            }
        }

        private void OnItemCraftFinished(ItemCraftTask task, Item item)
        {
            if (task.skinID != 0)
                return;

            BasePlayer player = task.owner;

            if (!HasPerm(player.UserIDString, permdefault))
                return;

            Dictionary<string, CachedSkin> cached;
            if (!_playerDefaultSkins.TryGetValue(player.userID, out cached))
                return;

            CachedSkin cachedSkin;
            if (!cached.TryGetValue(item.info.shortname, out cachedSkin))
                return;

            if (cachedSkin.redirect)
            {
                int amt = item.amount;
                NextTick(() =>
                {
                    DoRemove(item);
                    player.GiveItem(ItemManager.CreateByName(cachedSkin.redirectshortname, amt, 0), BaseEntity.GiveItemReason.Crafted);
                });
                return;
            }

            item.skin = cachedSkin.skinid;
            var held = item.GetHeldEntity();

            if (held != null)
            {
                held.skinID = cachedSkin.skinid;
                held.SendNetworkUpdate();
            }
        }

        #endregion Hooks

        #region Methods
        private StorageContainer CreateStorageCon(BasePlayer player)
        {
            StorageContainer storage = GameManager.server.CreateEntity(StringPool.Get(4080262419), Vector3.zero) as StorageContainer;

            storage.syncPosition = false;
            storage.limitNetworking = true;
            storage.name = player.displayName;
            storage.enableSaving = false;
            storage.Spawn();
            storage.inventory.playerOwner = player;

            DestroyOnGroundMissing bouyancy;
            if (storage.TryGetComponent<DestroyOnGroundMissing>(out bouyancy))
            {
                UnityEngine.Object.Destroy(bouyancy);
            }
            GroundWatch ridgidbody;
            if (storage.TryGetComponent<GroundWatch>(out ridgidbody))
            {
                UnityEngine.Object.Destroy(ridgidbody);
            }

            storage.gameObject.AddComponent<BoxController>();

            return storage;
        }

        private void StartLooting(BasePlayer player, StorageContainer storage)
        {
            _viewingcon.Add(player);

            if (_viewingcon.Count == 1)
                SubscribeToHooks();

            storage.SendAsSnapshot(player.Connection);

            player.inventory.loot.AddContainer(storage.inventory);
            player.inventory.loot.entitySource = storage;
            player.inventory.loot.PositionChecks = false;
            player.inventory.loot.MarkDirty();
            player.inventory.loot.SendImmediate();
            player.ClientRPCPlayer<string>(null, player, "RPC_OpenLootPanel", "generic_resizable");
        }

        #endregion Methods

        #region Controller
        private class BoxController : FacepunchBehaviour
        {
            private StorageContainer storageContainer;
            private BasePlayer player;
            private Item mainitem = null;
            private int mainitemamt = 1;
            private Item returnitem = null;
            private Item returnitemplayer = null;
            public bool _fillingbox = false;
            public bool _clearingbox = false;
            private bool _redirectskin = false;
            private List<Item> _redirectitems = new List<Item>();
            private ItemDefinition itemselected = null;
            public BaseCombatEntity maindeployable = null;
            private int page = 0;

            private void Awake()
            {
                storageContainer = GetComponent<StorageContainer>();
                player = storageContainer.inventory.playerOwner;
            }

            #region Skin Deployables

            public void SkinDeplyoables(BaseCombatEntity entity)
            {
                storageContainer.inventory.onItemAddedRemoved += new Action<Item, bool>(CheckforItemDply);
                storageContainer.inventory.SetFlag(ItemContainer.Flag.IsLocked, true);
                maindeployable = entity;
                GetDeployableSkins();
            }

            private void CheckforItemDply(Item item, bool b)
            {
                //if item added
                if (b)
                {
                    if (_fillingbox)
                        return;

                    if (item == returnitem)
                    {
                        returnitem = null;
                        return;
                    }

                    returnitemplayer = item;
                    GiveItem(returnitemplayer);
                    return;
                }

                if (_clearingbox)
                    return;

                if (maindeployable == null)
                {
                    Remove(item, true);
                    player.EndLooting();
                    return;
                }

                if (item == returnitemplayer)
                {
                    returnitemplayer = null;
                    return;
                }

                if (maindeployable.skinID != item.skin)
                {
                    //force refresh client skins
                    maindeployable.skinID = item.skin;
                    if (maindeployable.skinID == 0uL || maindeployable.skinID < 100000)
                    {
                        SendNetworkUpdate(maindeployable);
                    }
                    else
                        maindeployable.SendNetworkUpdateImmediate();
                }

                storageContainer.inventory.SetFlag(ItemContainer.Flag.IsLocked, true);
                returnitem = item;
                skinner.NextTick(() =>
                {
                    item.MoveToContainer(storageContainer.inventory);
                    storageContainer.inventory.SetFlag(ItemContainer.Flag.IsLocked, false);
                });

            }

            private void GetDeployableSkins()
            {
                ItemDefinition itemdef = maindeployable.pickup.itemTarget;

                //Get Skins List
                List<CachedSkin> cachedskins = skinner._skinsCache[itemdef.shortname];
                if (page > (cachedskins.Count - 1) / storageContainer.inventorySlots)
                    page = (cachedskins.Count - 1) / storageContainer.inventorySlots;

                //Check for UI
                if (cachedskins.Count > storageContainer.inventorySlots)
                {
                    CuiHelper.DestroyUi(player, "SkinPageUI");
                    CuiHelper.AddUi(player, skinner.CreatePageUI(page + 1, (cachedskins.Count - 1) / storageContainer.inventorySlots + 1));
                }

                //Fill container
                _fillingbox = true;

                int i;
                for (i = 0; i < storageContainer.inventorySlots && i < cachedskins.Count - (storageContainer.inventorySlots) * page; i++)
                {
                    CachedSkin cachedSkin = cachedskins[i + ((storageContainer.inventorySlots) * page)];
                    if (cachedSkin.redirect)
                    {
                        cachedskins.Remove(cachedSkin);
                        i -= 1;
                        continue;
                    }
                    InsertItem(cachedSkin, itemdef);
                }
                _fillingbox = false;
                storageContainer.inventory.SetFlag(ItemContainer.Flag.IsLocked, false);
                storageContainer.inventory.SetFlag(ItemContainer.Flag.NoItemInput, true);
                storageContainer.inventory.MarkDirty();
                storageContainer.SendNetworkUpdateImmediate();
            }

            #endregion Skin Deployables

            #region Skin Items

            public void StartItemSkin()
            {
                storageContainer.inventory.onItemAddedRemoved += new Action<Item, bool>(CheckforItem);
            }

            private void CheckforItem(Item item, bool b)
            {
                //if item removed
                if (!b)
                {
                    if (_clearingbox || mainitem == null)
                        return;
                    if (item == mainitem)
                    {
                        item.amount = mainitemamt;
                        item.MarkDirty();
                        mainitem = null;
                        mainitemamt = 1;
                        ClearCon();
                        return;
                    }
                    ItemRemoveCheck(item);
                    return;
                }

                //if item added
                storageContainer.inventory.SetFlag(ItemContainer.Flag.NoItemInput, true);
                storageContainer.inventory.SetFlag(ItemContainer.Flag.IsLocked, true);

                if (_fillingbox)
                    return;

                if (item.amount > 1)
                {
                    mainitemamt = item.amount;
                    item.amount = 1;
                }
                else
                    mainitemamt = 1;
                mainitem = item;
                GetSkins();
            }

            private void GetSkins()
            {
                ItemDefinition itemdef = mainitem.info;
                _redirectskin = false;
                //No Skins Found
                if (!skinner._skinsCache.ContainsKey(mainitem.info.shortname))
                {
                    //No Skins available
                    if (!skinner._redirectSkins.ContainsKey(mainitem.info.shortname))
                    {
                        skinner.NextTick(() =>
                        {
                            GiveItem(mainitem);
                        });
                        storageContainer.inventory.SetFlag(ItemContainer.Flag.NoItemInput, false);
                        storageContainer.inventory.SetFlag(ItemContainer.Flag.IsLocked, false);
                        return;
                    }
                    else
                    {
                        //Get Redirect Skin
                        itemdef = ItemManager.FindItemDefinition(skinner._redirectSkins[mainitem.info.shortname]);
                        _redirectskin = true;
                    }
                }

                //Get Skins List
                List<CachedSkin> cachedskins = skinner._skinsCache[itemdef.shortname];
                if (page > (cachedskins.Count - 1) / (storageContainer.inventorySlots - 1))
                    page = (cachedskins.Count - 1) / (storageContainer.inventorySlots - 1);

                //Check for UI
                if (cachedskins.Count + 1 > storageContainer.inventorySlots)
                {
                    CuiHelper.DestroyUi(player, "SkinPageUI");
                    CuiHelper.AddUi(player, skinner.CreatePageUI(page + 1, (cachedskins.Count - 1) / (storageContainer.inventorySlots - 1) + 1));
                }

                //Fill container
                _fillingbox = true;
                int amount = 1;
                if (itemdef.stackable > 1)
                    amount = mainitem.amount;
                int i;
                for (i = 0; i < storageContainer.inventorySlots - 1 && i < cachedskins.Count - (storageContainer.inventorySlots - 1) * page; i++)
                {
                    CachedSkin cachedskin = cachedskins[i + ((storageContainer.inventorySlots - 1) * page)];
                    InsertItem(cachedskin, itemdef, amount);
                }

                _fillingbox = false;
                storageContainer.inventory.SetFlag(ItemContainer.Flag.IsLocked, false);

                storageContainer.inventory.MarkDirty();
                storageContainer.SendNetworkUpdateImmediate();
            }

            private void ItemRemoveCheck(Item item)
            {
                CuiHelper.DestroyUi(player, "SkinPageUI");

                if (_redirectitems.Contains(item))
                {
                    item.maxCondition = mainitem.maxCondition;
                    item.condition = mainitem.condition;
                    item.amount = mainitemamt;
                    if (item.contents?.itemList != null)
                    {
                        foreach (var con in mainitem.contents.itemList)
                        {
                            var newCon = ItemManager.Create(con.info, con.amount, con.skin);
                            newCon.condition = con.condition;
                            newCon.maxCondition = con.maxCondition;
                            newCon.MoveToContainer(item.contents);
                            newCon.MarkDirty();
                        }
                        item.contents.MarkDirty();
                    }
                    item.contents?.SetFlag(ItemContainer.Flag.NoItemInput, false);
                    item.contents?.SetFlag(ItemContainer.Flag.IsLocked, false);

                    BaseEntity held = item.GetHeldEntity();
                    if (held != null)
                    {
                        BaseEntity mainheld = mainitem.GetHeldEntity();
                        if (mainheld != null)
                        {
                            BaseProjectile mainbaseProjectile = mainheld as BaseProjectile;
                            BaseProjectile baseProjectile = held as BaseProjectile;
                            if (baseProjectile != null && mainbaseProjectile != null)
                            {
                                baseProjectile.canUnloadAmmo = true;
                                baseProjectile.primaryMagazine.contents = mainbaseProjectile.primaryMagazine.contents;
                            }
                        }
                        //held.SendNetworkUpdate();
                    }
                    item.MarkDirty();
                    Remove(mainitem);
                    return;
                }
                else
                {
                    BaseEntity held = mainitem.GetHeldEntity();
                    mainitem.skin = item.skin;
                    if (held != null)
                    {
                        held.skinID = item.skin;
                        held.SendNetworkUpdateImmediate();
                    }
                    if (item != mainitem)
                    {
                        Remove(item, true);
                        mainitem.amount = mainitemamt;
                        GiveItem(mainitem);
                    }
                }

                mainitemamt = 1;
                mainitem = null;
            }

            #endregion Skin Items

            #region Set Default Skins

            private void CheckforItemSelect(Item item, bool b)
            {
                //if item added
                if (b)
                {
                    if (_fillingbox)
                        return;

                    if (item == returnitem)
                    {
                        returnitem = null;
                        return;
                    }

                    returnitemplayer = item;
                    GiveItem(returnitemplayer);
                    return;
                }

                if (_clearingbox)
                    return;

                if (item == returnitemplayer)
                {
                    returnitemplayer = null;
                    return;
                }
                item.amount = 0;

                storageContainer.inventory.SetFlag(ItemContainer.Flag.IsLocked, true);

                skinner.NextTick(() =>
                {
                    if (_clearingbox || _fillingbox || item == null)
                        return;

                    ItemDefinition itemdef = item.info;
                    if (itemselected == null)
                    {
                        page = 0;
                        string origionalskin;
                        if (skinner._redirectSkins.TryGetValue(itemdef.shortname, out origionalskin))
                        {
                            itemdef = ItemManager.FindItemDefinition(origionalskin);
                        }
                        itemselected = itemdef;
                    }

                    Remove(item, true);
                    ClearCon();

                    CuiHelper.DestroyUi(player, "SkinPageUI");

                    //Show Skins
                    List<CachedSkin> cachedskins = skinner._skinsCache[itemdef.shortname];
                    if (page > (cachedskins.Count - 1) / storageContainer.inventorySlots)
                        page = (cachedskins.Count - 1) / storageContainer.inventorySlots;

                    //Check for UI
                    if (cachedskins.Count + 1 > storageContainer.inventorySlots)
                    {
                        CuiHelper.AddUi(player, skinner.CreatePageUI(page + 1, (cachedskins.Count - 1) / storageContainer.inventorySlots + 1));
                    }
                    _fillingbox = true;
                    int i;
                    for (i = 0; i < storageContainer.inventorySlots && i < cachedskins.Count - (storageContainer.inventorySlots) * page; i++)
                    {
                        CachedSkin cachedskin = cachedskins[i + ((storageContainer.inventorySlots) * page)];
                        InsertItem(cachedskin, itemdef);
                    }
                    _fillingbox = false;

                    storageContainer.inventory.onItemAddedRemoved -= new Action<Item, bool>(CheckforItemSelect);
                    storageContainer.inventory.onItemAddedRemoved += new Action<Item, bool>(CheckforSkinSelect);
                    storageContainer.inventory.SetFlag(ItemContainer.Flag.IsLocked, false);
                    storageContainer.inventory.SetFlag(ItemContainer.Flag.NoItemInput, true);
                    storageContainer.inventory.MarkDirty();
                    storageContainer.SendNetworkUpdateImmediate();
                });
            }

            private void CheckforSkinSelect(Item item, bool b)
            {
                if (item == null)
                    return;
                //if item added
                if (b)
                {
                    if (_fillingbox)
                        return;

                    if (item == returnitem)
                    {
                        returnitem = null;
                        return;
                    }

                    returnitemplayer = item;
                    GiveItem(returnitemplayer);
                    return;
                }

                if (_clearingbox)
                    return;

                if (item == returnitemplayer)
                {
                    returnitemplayer = null;
                    return;
                }
                item.amount = 0;

                storageContainer.inventory.SetFlag(ItemContainer.Flag.IsLocked, true);

                skinner.NextTick(() =>
                {
                    if (_clearingbox || _fillingbox || item == null)
                        return;

                    bool flag1 = skinner._redirectSkins.ContainsKey(item.info.shortname);
                    if (item.skin == 0 && !flag1)
                    {
                        if (skinner._playerDefaultSkins[player.userID].ContainsKey(itemselected.shortname))
                        {
                            skinner._playerDefaultSkins[player.userID].Remove(itemselected.shortname);
                        }
                    }
                    else
                    {
                        List<CachedSkin> cachelist = new List<CachedSkin>();
                        CachedSkin cachedskin = new CachedSkin();
                        if (skinner._skinsCache.TryGetValue(itemselected.shortname, out cachelist))
                        {
                            if (!flag1)
                                cachedskin = cachelist.Find(x => x.skinid == item.skin);
                            else
                            {
                                cachedskin = cachelist.Find(x => x.redirectshortname == item.info.shortname);
                            }
                        }
                        //Use real short name here to avoid redirect skins
                        skinner._playerDefaultSkins[player.userID][itemselected.shortname] = cachedskin;
                    }

                    itemselected = null;

                    Remove(item, true);

                    ClearCon();
                    page = 0;
                    GetDefaultSkins();
                });
            }

            public void GetDefaultSkins()
            {
                //BlockItemInputPlayer(true);
                storageContainer.inventory.SetFlag(ItemContainer.Flag.IsLocked, true);

                Dictionary<string, CachedSkin> defaultskins = new Dictionary<string, CachedSkin>();
                CachedSkin defaults;

                foreach (KeyValuePair<string, CachedSkin> item in skinner._defaultskins)
                {
                    if (skinner._playerDefaultSkins[player.userID].TryGetValue(item.Key, out defaults))
                    {
                        if (defaults != null)
                            defaultskins[item.Key] = defaults;
                        continue;
                    }
                    defaultskins[item.Key] = item.Value;
                }

                if (page > (defaultskins.Count - 1) / (storageContainer.inventorySlots))
                    page = (defaultskins.Count - 1) / (storageContainer.inventorySlots);

                //Check for UI
                if (defaultskins.Count > storageContainer.inventorySlots)
                {
                    CuiHelper.DestroyUi(player, "SkinPageUI");
                    CuiHelper.AddUi(player, skinner.CreatePageUI(page + 1, (skinner._skinsCache.Keys.Count - 1) / storageContainer.inventorySlots + 1));
                }

                int i;
                _fillingbox = true;
                for (i = 0; i < storageContainer.inventorySlots && i < defaultskins.Count - (storageContainer.inventorySlots) * page; i++)
                {
                    CachedSkin cachedskin = defaultskins.Values.ElementAt(i + ((storageContainer.inventorySlots) * page));
                    ItemDefinition itemdef;

                    if (cachedskin.redirect)
                    {
                        itemdef = ItemManager.FindItemDefinition(cachedskin.redirectshortname);
                    }
                    else
                    {
                        itemdef = ItemManager.FindItemDefinition(cachedskin.shortname);
                    }
                    InsertItem(cachedskin, itemdef);
                }

                _fillingbox = false;
                storageContainer.inventory.onItemAddedRemoved -= new Action<Item, bool>(CheckforSkinSelect);
                storageContainer.inventory.onItemAddedRemoved += new Action<Item, bool>(CheckforItemSelect);

                storageContainer.inventory.SetFlag(ItemContainer.Flag.IsLocked, false);
                storageContainer.inventory.SetFlag(ItemContainer.Flag.NoItemInput, true);
                storageContainer.inventory.MarkDirty();
                storageContainer.SendNetworkUpdateImmediate();
            }

            #endregion  Set Default Skins

            #region UI
            public void NextPage()
            {
                page += 1;
                storageContainer.inventory.SetFlag(ItemContainer.Flag.IsLocked, true);
                if (maindeployable != null)
                {
                    ClearCon();
                    skinner.NextTick(() =>
                    {
                        GetDeployableSkins();
                    });
                    return;
                }

                if (mainitem == null)
                {
                    if (itemselected == null)
                    {
                        ClearCon();
                        storageContainer.inventory.onItemAddedRemoved -= new Action<Item, bool>(CheckforItemSelect);
                        GetDefaultSkins();
                        return;
                    }
                    Item dummy = ItemManager.Create(itemselected, 1);
                    storageContainer.inventory.SetFlag(ItemContainer.Flag.IsLocked, true);
                    storageContainer.inventory.onItemAddedRemoved -= new Action<Item, bool>(CheckforSkinSelect);
                    CheckforItemSelect(dummy, false);
                    return;
                }

                ClearCon(false, true);
                skinner.NextTick(() =>
                {
                    GetSkins();
                });
            }

            public void BackPage()
            {
                page -= 1;
                storageContainer.inventory.SetFlag(ItemContainer.Flag.IsLocked, true);
                if (page < 0)
                    page = 0;

                if (maindeployable != null)
                {
                    ClearCon();
                    GetDeployableSkins();
                    return;
                }

                if (mainitem == null)
                {
                    if (itemselected == null)
                    {
                        ClearCon();
                        storageContainer.inventory.onItemAddedRemoved -= new Action<Item, bool>(CheckforItemSelect);
                        GetDefaultSkins();
                        return;
                    }
                    Item dummy = ItemManager.Create(itemselected, 1);
                    storageContainer.inventory.SetFlag(ItemContainer.Flag.IsLocked, true);
                    storageContainer.inventory.onItemAddedRemoved -= new Action<Item, bool>(CheckforSkinSelect);
                    CheckforItemSelect(dummy, false);
                    return;
                }

                ClearCon(false, true);
                skinner.NextTick(() =>
                {
                    GetSkins();
                });
            }

            #endregion UI

            #region Helpers
            private void GiveItem(Item item)
            {
                if (!player.inventory.GiveItem(item, null))
                {
                    item.Drop(player.transform.position, player.inventory.containerMain.dropVelocity, new Quaternion());
                }
            }

            private void Remove(Item item, bool nextTick = false)
            {
                if (item == null)
                {
                    player.ChatMessage("mainitem null");
                    return;
                }
                if (nextTick)
                {
                    skinner.NextTick(() =>
                    {
                        Remove(item);
                    });
                    return;
                }
                DoRemove(item);
            }

            private void DoRemove(Item item)
            {
                if (item.isServer && item.uid > 0 && Net.sv != null)
                {
                    Net.sv.ReturnUID(item.uid);
                    item.uid = 0;
                }
                if (item.contents != null)
                {
                    item.contents.Kill();
                    item.contents = null;
                }
                if (item.isServer)
                {
                    item.RemoveFromWorld();
                    item.RemoveFromContainer();
                }
                BaseEntity heldEntity = item.GetHeldEntity();
                if (heldEntity.IsValid())
                {
                    heldEntity.Kill();
                }
            }
            private void ClearCon(bool nexttick = false, bool skipmainitem = false)
            {
                if (nexttick)
                {
                    skinner.NextTick(() => ClearCon());
                    return;
                }
                storageContainer.inventory.SetFlag(ItemContainer.Flag.NoItemInput, true);
                storageContainer.inventory.SetFlag(ItemContainer.Flag.IsLocked, true);
                _clearingbox = true;
                foreach (var item in storageContainer.inventory.itemList)
                {
                    if (item == null) continue;
                    if (!skipmainitem || item != mainitem) item.Remove();
                }
                ItemManager.DoRemoves();
                storageContainer.inventory.MarkDirty();
                storageContainer.SendNetworkUpdateImmediate();
                _redirectitems.Clear();
                _clearingbox = false;
                storageContainer.inventory.SetFlag(ItemContainer.Flag.NoItemInput, false);
                storageContainer.inventory.SetFlag(ItemContainer.Flag.IsLocked, false);
            }

            private void InsertItem(CachedSkin cachedSkin, ItemDefinition itemDef, int amount = 1)
            {
                ulong skinid = cachedSkin.skinid;
                ItemDefinition itemdef2 = itemDef;

                //Get redirect item def
                if (cachedSkin.redirect)
                    itemdef2 = ItemManager.FindItemDefinition(cachedSkin.redirectshortname);

                Item item = ItemManager.Create(itemdef2, amount, skinid);

                if (!item.MoveToContainer(storageContainer.inventory, -1, false))
                    Remove(item, true);

                if (cachedSkin.redirect || _redirectskin)
                    _redirectitems.Add(item);
                else
                    item.name = cachedSkin.displayName;

                //Lock mod slots
                item.contents?.SetFlag(ItemContainer.Flag.NoItemInput, true);
                item.contents?.SetFlag(ItemContainer.Flag.IsLocked, true);

                //Update held skins
                BaseEntity held = item.GetHeldEntity();
                if (held != null)
                {
                    //held.skinID = skinid;
                    //Remove Bullets
                    BaseProjectile baseProjectile = held as BaseProjectile;
                    if (baseProjectile != null)
                    {
                        baseProjectile.canUnloadAmmo = false;
                    }
                    //held.SendNetworkUpdate();
                }
                item.MarkDirty();
            }

            //Refresh skins so they dont show as missing textures on client side for deployables
            private void SendNetworkUpdate(BaseEntity ent)
            {
                if (Net.sv.write.Start())
                {
                    Net.sv.write.PacketID(Message.Type.EntityDestroy);
                    Net.sv.write.EntityID(ent.net.ID);
                    Net.sv.write.UInt8((byte)BaseNetworkable.DestroyMode.None);
                    Net.sv.write.Send(new SendInfo(player.net.group.subscribers.ToList()));
                }

                ent.OnNetworkGroupLeave(ent.net.group);
                ent.InvalidateNetworkCache();

                List<Connection> subscribers = ent.GetSubscribers();
                if (subscribers != null && subscribers.Count > 0)
                {
                    for (int i = 0; i < subscribers.Count; i++)
                    {
                        BasePlayer item = subscribers[i].player as BasePlayer;
                        if (!(item == null) && ent.ShouldNetworkTo(item))
                        {
                            item.QueueUpdate(0, ent);
                            item.SendEntityUpdate();
                        }
                    }
                }
                foreach (var child in ent.children)
                {
                    SendNetworkUpdate(child);
                }
                ent.gameObject.SendOnSendNetworkUpdate(ent as BaseEntity);
            }

            public void OnDestroy()
            {
                _clearingbox = true;
                if (mainitem != null)
                {
                    mainitem.amount = mainitemamt;
                    GiveItem(mainitem);
                }
                ClearCon();
                storageContainer.Kill();
                CuiHelper.DestroyUi(player, "SkinPageUI");
            }
            #endregion Helpers
        }

        #endregion Controller

        #region GUI Panel
        // Cached UI
        public CuiElementContainer CreatePageUI(int pagecurr, int pagemax)
        {
            CuiElementContainer elements = new CuiElementContainer();
            string panel = elements.Add(new CuiPanel
            {
                Image = { Color = "0.5 0.5 0.5 0.3" },
                RectTransform = { AnchorMin = config.uiposition[0], AnchorMax = config.uiposition[1] }
            }, "Hud.Menu", "SkinPageUI");

            elements.Add(new CuiLabel
            {
                Text = { Text = $"{pagecurr} of {pagemax}", FontSize = 14, Align = TextAnchor.MiddleCenter, Color = "0.8 0.8 0.8 0.75" },
                RectTransform = { AnchorMin = "0.30 0.00", AnchorMax = "0.70 1.0" }
            }, panel);

            string cmdback = "sbBackPage";
            if (pagecurr == 1)
                cmdback = "";

            elements.Add(new CuiButton
            {
                Button = { Command = cmdback, Color = "0.5 0.5 0.5 0.0" },
                Text = { Text = "←", FontSize = 25, Align = TextAnchor.MiddleCenter, Color = "0.8 0.8 0.8 0.75" },
                RectTransform = { AnchorMin = "0.00 0.00", AnchorMax = "0.30 1.0" }
            }, panel);

            string cmdfwd = "sbNextPage";
            if (pagecurr == pagemax)
                cmdfwd = "";

            elements.Add(new CuiButton
            {
                Button = { Command = cmdfwd, Color = "0.5 0.5 0.5 0.0" },
                Text = { Text = "→", FontSize = 25, Align = TextAnchor.MiddleCenter, Color = "0.8 0.8 0.8 0.75" },
                RectTransform = { AnchorMin = "0.70 0.00", AnchorMax = "1.0 1.0" }
            }, panel);
            return elements;
        }
        #endregion GUI Panel

        #region Helpers
        private void DoRemove(Item item)
        {
            if (item.isServer && item.uid > 0 && Net.sv != null)
            {
                Net.sv.ReturnUID(item.uid);
                item.uid = 0;
            }
            if (item.contents != null)
            {
                item.contents.Kill();
                item.contents = null;
            }
            if (item.isServer)
            {
                item.RemoveFromWorld();
                item.RemoveFromContainer();
            }
            BaseEntity heldEntity = item.GetHeldEntity();
            if (heldEntity.IsValid())
            {
                heldEntity.Kill();
            }
        }

        private void UnSubscribeFromHooks()
        {
            foreach (var hook in _registeredhooks)
                Unsubscribe(hook);
        }
        private void SubscribeToHooks()
        {
            foreach (var hook in _registeredhooks)
                Subscribe(hook);
        }
        private bool HasPerm(string id, string perm) => permission.UserHasPermission(id, perm);

        private string GetLang(string langKey, string playerId = null, params object[] args)
        {
            return string.Format(lang.GetMessage(langKey, this, playerId), args);
        }
        private void ChatMessage(IPlayer player, string langKey, params object[] args)
        {
            if (player.IsConnected) player.Message(GetLang(langKey, player.Id, args));
        }
        #endregion Helpers
    }
}