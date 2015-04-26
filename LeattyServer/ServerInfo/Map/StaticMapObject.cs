using LeattyServer.Helpers;
using LeattyServer.ServerInfo.Player;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LeattyServer.ServerInfo.Packets;

namespace LeattyServer.ServerInfo.Map
{
    public abstract class StaticMapObject
    {
        public int ObjectId { get; set; }
        public MapleCharacter Owner { get; private set; }
        public Point Position { get; private set; }
        public DateTime Expiration { get; set; }
        public bool IsPartyObject { get; set; }
        public int PartyId { get; set; }

        public StaticMapObject(int objectId, MapleCharacter owner, Point position, int durationMS, bool isPartyObject)
        {
            ObjectId = objectId;
            Owner = owner;
            Position = position;
            Expiration = DateTime.UtcNow.AddMilliseconds(durationMS);
            IsPartyObject = isPartyObject;
            PartyId = owner.Party != null ? owner.Party.Id : -1;
        }

        public virtual void Dispose()
        {
            Owner = null;
        }

        public abstract PacketWriter GetSpawnPacket(bool animatedSpawn);

        public abstract PacketWriter GetDestroyPacket(bool animatedDestroy);
    }

    public class MapleMist : StaticMapObject
    {        
        public int SourceSkillId { get; private set; }
        public byte SkillLevel { get; set; }
        public BoundingBox BoundingBox { get; set; }
       
        public MapleMist(int skillId, byte skillLevel, int objectId, MapleCharacter owner, BoundingBox boundingBox, Point position, int durationMS, bool partyObject)
            : base(objectId, owner, position, durationMS, partyObject)
        {
            SourceSkillId = skillId;            
            BoundingBox = boundingBox;          
            SkillLevel = skillLevel;
        }
        
        #region Packets
        public override PacketWriter GetSpawnPacket(bool animatedSpawn)
        {
            PacketWriter pw = new PacketWriter();
            pw.WriteHeader(SendHeader.SpawnMist);

            pw.WriteInt(ObjectId);
            pw.WriteBool(!animatedSpawn); //not sure
            pw.WriteInt(Owner.Id);
            pw.WriteInt(SourceSkillId);
            pw.WriteByte(SkillLevel);
            pw.WriteShort(0xA); //Skill delay
            pw.WriteBox(BoundingBox);
            pw.WriteInt(0); //WriteInt(1) when spawned by a mob
            pw.WriteInt(0);
            pw.WritePoint(Position);
            pw.WriteInt(0);
            pw.WriteInt(0);

            return pw;
        }

        public override PacketWriter GetDestroyPacket(bool animatedDestroy)
        {
            PacketWriter pw = new PacketWriter();
            pw.WriteHeader(SendHeader.RemoveMist);
            pw.WriteInt(ObjectId);
            pw.WriteBool(animatedDestroy); //eruption?

            return pw;
        }
        #endregion
    }
}
