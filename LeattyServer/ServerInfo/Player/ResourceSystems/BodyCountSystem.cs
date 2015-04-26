using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LeattyServer.ServerInfo.Packets;

namespace LeattyServer.ServerInfo.Player.ResourceSystems
{
    class BodyCountSystem : ResourceSystem
    {
        public int BodyCount { get; private set; }
        public BodyCountSystem()
            : base(ResourceSystemType.Bandit)
        {
            BodyCount = 0;
        }

        public void IncreaseBodyCount(MapleClient c)
        {
            if (BodyCount < 5)
            {
                BodyCount++;
                c.SendPacket(GetBuffPacket());
            }
        }

        public int GetAndResetBodyCount(MapleClient c, int newCount)
        {
            int oldCount = BodyCount;
            BodyCount = 0;
            c.SendPacket(GetBuffPacket());
            return oldCount;
        }

        private PacketWriter GetBuffPacket()
        {
            PacketWriter pw = new PacketWriter();
            pw.WriteHeader(SendHeader.GiveBuff);
            //Buff.WriteSingleBuffMask(pw, MapleBuffStat.BODY_COUNT);

            pw.WriteZeroBytes(5);
            pw.WriteInt(BodyCount);
            pw.WriteZeroBytes(10);

            return pw;
        }
    }
}
