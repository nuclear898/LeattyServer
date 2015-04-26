using System;
using System.Collections.Generic;
using System.Linq;
using System.Timers;
using LeattyServer.Constants;
using LeattyServer.Data;
using LeattyServer.Data.WZ;
using LeattyServer.Helpers;
using LeattyServer.ServerInfo.Map;
using LeattyServer.ServerInfo.Packets.Handlers;
using LeattyServer.ServerInfo.Player;

namespace LeattyServer.ServerInfo.Packets
{
    class ChannelServer : Server
    {
        private Dictionary<int, MapleMap> Maps { get; set; }         
        private Timer MapRespawnTimer = new Timer(ServerConstants.MonsterSpawnInterval);
        private Timer MapUpdateTimer = new Timer(4500);
      
        public byte ChannelId { get; }

        public ChannelServer(byte channelId)
            : base()
        {
            ChannelId = channelId;

            Maps = new Dictionary<int, MapleMap>();
            foreach (KeyValuePair<int, WzMap> kvp in DataBuffer.MapBuffer)
            {               
                MapleMap map = new MapleMap(kvp.Key, kvp.Value);
                Maps.Add(kvp.Key, map);
            }
           
            ICollection<MapleMap> mapsList = Maps.Values;
            MapRespawnTimer.Elapsed += (sender, e) => CheckMapRespawns(mapsList);
            MapUpdateTimer.Elapsed += (sender, e) => UpdateMaps(mapsList);            
            MapUpdateTimer.Enabled = true;
            MapRespawnTimer.Enabled = true;           
        }

        public MapleMap GetMap(int mapId)
        {
            MapleMap ret;
            if (Maps.TryGetValue(mapId, out ret))
            {
                return ret;
            }
            return null;
        }

        private static void CheckMapRespawns(IEnumerable<MapleMap> maps)
        {            
            foreach (MapleMap map in maps)
            {
                if (map.CharacterCount > 0)
                {
                    map.CheckAndRespawnMobs();
                    map.CheckAndRespawnReactors();
                }
            }
        }

        private static void UpdateMaps(IEnumerable<MapleMap> maps)
        {          
            DateTime now = DateTime.UtcNow;
            foreach (MapleMap map in maps)
            {
                map.UpdateMap(now);
            }            
        }

        public void ShutDownChannel()
        {
            Dispose();            
        }
    }
}
