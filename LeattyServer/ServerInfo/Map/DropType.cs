using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LeattyServer.ServerInfo.Map
{
    public enum MapleDropType
    {
        Player = 0,
        Party = 1,
        FreeForAll = 2,
        Boss = 3, //Explosive, drops fly higher and is FFA
        Unk = 4
    }
}
