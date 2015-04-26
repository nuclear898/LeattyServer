using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LeattyServer.ServerInfo.Party
{
    public class PartyCharacterInfo
    {
        public int Id { get; private set; }
        public string Name { get; private set; }
        public int Level { get; private set; }
        public int Job { get; private set; }
        public int SubJob { get; private set; }
        public int Channel { get; set; }
        public int MapId { get; set; }

        public PartyCharacterInfo(int id, string name, int level, int job, int subJob, int channel = -2, int mapId = 999999999)
        {
            Id = id;
            Name = name;
            Level = level;
            Job = job;
            SubJob = SubJob;
            Channel = channel;
            MapId = mapId;
        }
    }
}
