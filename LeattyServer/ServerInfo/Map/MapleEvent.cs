using LeattyServer.Data.WZ;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LeattyServer.ServerInfo.Map
{
    public class MapleEvent : MapleMap
    {
        public MapleEvent(int mapId, WzMap wzMap, bool skipSpawn) : base(mapId, wzMap, skipSpawn) { }
    }
}
