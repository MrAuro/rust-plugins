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
    [Info("CrateNotify", "Auro", "1.0.0")]
    [Description("Notifies players when a crate has begun to hack if it is not their first time")]
    public class CrateNotify : CovalencePlugin
    {
        [PluginReference]
        Plugin MonumentFinder;

        private IDictionary<string, string> monumentNames = new Dictionary<string, string>()
        {
            {"arctic_research_base_a", "Arctic Research Base"},
            {"harbor_1", "Small Harbor"},
            {"harbor_2", "Large Harbor"},
            {"airfield_1", "Airfield"},
            {"excavator_1", "Excavator"},
            {"military_tunnel_1", "Military Tunnel"},
            {"powerplant_1", "Power Plant"},
            {"trainyard_1", "Trainyard"},
            {"water_treatment_plant_1", "Water Treatment Plant"},
            {"lighthouse", "Lighthouse"},
            {"junkyard_1", "Junkyard"},
            {"OilrigAI2", "Large Oil Rig"},
            {"OilrigAI", "Small Oil Rig"},
            {"satellite_dish", "Satellite Dish"},
            {"sphere_tank", "The Dome"},
            {"launch_site_1", "Launch Site"},
        };


        private class StoredData
        {
            public HashSet<PlayerInfo> Players = new HashSet<PlayerInfo>();

            public StoredData()
            {
            }
        }

        private class PlayerInfo
        {
            public string Id;

            public PlayerInfo()
            {
            }

            public PlayerInfo(BasePlayer player)
            {
                Id = player.UserIDString;
            }
        }

        private StoredData storedData;

        private void Init()
        {
            storedData = Interface.Oxide.DataFileSystem.ReadObject<StoredData>("CrateNotifyData");
        }

        // the OnCrateHack event does not have the BasePlayer so we have to use this
        object CanHackCrate(BasePlayer player, HackableLockedCrate crate)
        {
            Puts("CanHackCrate");

            bool firstHack = true;

            for (int i = 0; i < storedData.Players.Count; i++)
            {
                Puts("checking player id: " + storedData.Players.ElementAt(i).Id);

                if (storedData.Players.ElementAt(i).Id == player.UserIDString)
                {
                    firstHack = false;
                    break;
                }
            }

            if (firstHack)
            {
                player.ChatMessage("Since this is the first time you have hacked a crate, nobody has been notified however next time people will be notified but the hack time will be reduced.");
                PlayerInfo info = new PlayerInfo(player);
                storedData.Players.Add(info);
                Interface.Oxide.DataFileSystem.WriteObject("CrateNotifyData", storedData);

                return null;
            }
            else
            {
                bool onCargo = crate.GetComponentInParent<CargoShip>() != null;
                // reduce it by 300 seconds
                crate.hackSeconds = 300;

                if (onCargo)
                {
                    server.Broadcast("A locked crate is being hacked on the Cargo Ship");
                    return null;
                }
                else
                {
                    string prefabName = GetClosestMonument(crate.transform.position).Object.name;
                    string monumentName = monumentNames.Any(x => prefabName.Contains(x.Key)) ? monumentNames.First(x => prefabName.Contains(x.Key)).Value : null;

                    if (monumentName != null)
                    {
                        server.Broadcast(string.Format("A locked crate is being hacked at {0}", monumentName));
                    }
                    else
                    {
                        server.Broadcast(string.Format("A locked crate is being hacked!"));
                    }
                }
            }

            return null;
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
            var dictResult = MonumentFinder?.Call("API_GetClosest", position) as Dictionary<string, object>;
            return dictResult != null ? new MonumentAdapter(dictResult) : null;
        }

    }
}