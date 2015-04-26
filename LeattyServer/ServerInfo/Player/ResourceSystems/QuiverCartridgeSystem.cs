using LeattyServer.Constants;
using LeattyServer.Data.WZ;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LeattyServer.ServerInfo.Packets;

namespace LeattyServer.ServerInfo.Player.ResourceSystems
{
    public class QuiverCartridgeSystem : ResourceSystem
    {
        public int ChosenArrow { get; private set; } // 0 = blood, 1 = poison, 2 = magic
        
        int[] Arrows { get; set; }

        public QuiverCartridgeSystem() 
            : base (ResourceSystemType.Hunter)
        {
            ChosenArrow = -1;
            Arrows = new int[3];
            for (int i = 0; i < 3; i++)
                Arrows[i] = 10;
        }

        private void IncreaseChosenArrow()
        {
            if (ChosenArrow >= 2)
                ChosenArrow = 0;
            else
                ChosenArrow++;
        }

        public int HandleUse(MapleClient c)
        {
            int usedArrow = ChosenArrow;
            Arrows[ChosenArrow]--;
            if (Arrows[ChosenArrow] < 1) // Switch to other quiver
            {
                for (int i = 0; i < 3; i++)
                {
                    IncreaseChosenArrow();
                    if (Arrows[ChosenArrow] > 0)
                        break;
                }
                if (Arrows[ChosenArrow] <= 0) // All quivers are empty
                {
                    ChosenArrow = 0;
                    for (int i = 0; i < 3; i++)
                        Arrows[i] = 10;
                }               
            }
            c.SendPacket(GetBuffPacket());
            if (usedArrow != ChosenArrow)
                c.SendPacket(GetEffectPacket());

            return usedArrow;
        }

        public void SwitchCurrentArrow(MapleClient c)
        {
            int oldArrow = ChosenArrow;
            for (int i = 0; i < 3; i++)
            {
                IncreaseChosenArrow();
                if (Arrows[ChosenArrow] > 0)
                    break;
                if (i == 2) // All quivers empty, should not be possible
                {
                    ChosenArrow = oldArrow; // Monkey proof...
                    Arrows[oldArrow] = 1;
                }
            }
            c.SendPacket(GetBuffPacket());
            if (ChosenArrow != oldArrow)
                c.SendPacket(GetEffectPacket());
        }

        public PacketWriter GetEffectPacket()
        {
            PacketWriter pw = new PacketWriter();
            pw.WriteHeader(SendHeader.ShowSkillEffect);

            pw.WriteByte(0x35);
            pw.WriteInt(Hunter.QUIVER_CARTRIDGE);
            pw.WriteInt(ChosenArrow);
            pw.WriteInt(Arrows[ChosenArrow]);

            return pw;
        }

        public PacketWriter GetBuffPacket()
        {
            PacketWriter pw = new PacketWriter();
            pw.WriteHeader(SendHeader.GiveBuff);

            //Buff.WriteSingleBuffMask(pw, MapleBuffStat.QUIVER_CARTRIDGE);            
            int quiverCounts = Arrows[0] * 10000 + Arrows[1] * 100 + Arrows[2];
            pw.WriteInt(quiverCounts);
            pw.WriteInt(Hunter.QUIVER_CARTRIDGE);
            pw.WriteInt(SkillEffect.MAX_BUFF_TIME_MS);
            pw.WriteZeroBytes(5);
            pw.WriteInt(ChosenArrow + 1);

            pw.WriteInt(0);
            pw.WriteInt(0);
            pw.WriteInt(0);
            pw.WriteByte(0);

            return pw;
        }        
    }
}
