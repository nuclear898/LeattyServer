using LeattyServer.Data.WZ;
using LeattyServer.Helpers;
using LeattyServer.ServerInfo.Player;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Timers;
using LeattyServer.ServerInfo.Packets;

namespace LeattyServer.ServerInfo.Map
{
    public class SpecialPortal : StaticMapObject
    {
        public int SkillId { get; private set; }      
        public MapleMap FromMap { get; set; }
        public MapleMap ToMap { get; set; }        
        public WzMap.Portal ToMapPortal { get; set; }
        
        public SpecialPortal(int skillId, MapleCharacter owner, Point position, MapleMap fromMap, MapleMap toMap, WzMap.Portal toMapSpawnPortal, int durationMS, bool partyObject)
            : base(0, owner, position, durationMS, partyObject)
        {
            SkillId = skillId;
            FromMap = fromMap;
            ToMap = toMap;
            ToMapPortal = toMapSpawnPortal;            
        }

        public override void Dispose()
        {
            Owner.RemoveDoor(SkillId);
            FromMap = null;
            ToMapPortal = null;
            base.Dispose();
        }

        public void Warp(MapleCharacter chr)
        {
            chr.ChangeMap(ToMap, ToMapPortal.Name, true);
        }

        public override PacketWriter GetSpawnPacket(bool animatedSpawn)
        {
            //[00 84 D7 17] [84 D2 D7 17] [5A 43 23 00] [61 FF] [E1 FF]
            PacketWriter pw = new PacketWriter();
            pw.WriteHeader(SendHeader.SpawnPortal);
            pw.WriteInt(ToMap.MapId);
            pw.WriteInt(FromMap.MapId);
            pw.WriteInt(SkillId);
            pw.WritePoint(Position);

            return pw;
        }

        public override PacketWriter GetDestroyPacket(bool animatedDestroy)
        {
            PacketWriter pw = new PacketWriter();
            pw.WriteHeader(SendHeader.SpawnPortal);
            pw.WriteInt(999999999);
            pw.WriteInt(999999999);
            return pw;
        }
    }

    public class MysticDoor : SpecialPortal
    {
        public MysticDoor(int skillId, MapleCharacter owner, Point position, MapleMap fromMap, MapleMap toMap, WzMap.Portal toMapSpawnPortal, int durationMS, bool partyObject)
            : base(skillId, owner, position, fromMap, toMap, toMapSpawnPortal, durationMS, partyObject)
        {
            
        }

        public override PacketWriter GetSpawnPacket(bool animatedSpawn)
        {
            //[00] [2F 44 86 00] [5A 43 23 00] [32 F5] [E1 FF]
            PacketWriter pw = new PacketWriter();
            pw.WriteHeader(SendHeader.SpawnMysticDoor);
            pw.WriteBool(!animatedSpawn);
            pw.WriteInt(Owner.Id);
            pw.WriteInt(SkillId);
            pw.WritePoint(Position);

            return pw;
        }

        public override PacketWriter GetDestroyPacket(bool animatedDestroy)
        {
            PacketWriter pw = new PacketWriter();
            pw.WriteHeader(SendHeader.RemoveMysticDoor);
            pw.WriteBool(true);
            pw.WriteInt(Owner.Id);
            return pw;
        }
    }
}
