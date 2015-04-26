using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LeattyServer.ServerInfo.Player;
using LeattyServer.ServerInfo.AntiCheat;
namespace LeattyServer.ServerInfo
{
    public class MigrationData
    {
        public bool ToCashShop { get; set; }
        public byte ToChannel { get; set; }
        public byte ReturnChannel { get; set; } // set by cashshop among other things.
        public string AccountName { get; set; }
        public long ConnectionAuth { get; set; }        
        public int CharacterId { get; set; }
        public OffenceTracker CheatTracker { get; set; }
        public MapleCharacter Character { get; set; }        
    }
}
