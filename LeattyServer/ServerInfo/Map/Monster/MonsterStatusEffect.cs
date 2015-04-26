using LeattyServer.Helpers;
using LeattyServer.ServerInfo.Player;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Timers;
using LeattyServer.ServerInfo.Packets;

namespace LeattyServer.ServerInfo.Map.Monster
{
    public class MonsterBuff
    {       
        public int OwnerId { get; private set; }
        public int SkillId { get; private set; }
        public int Duration { get; private set; }
        public Timer RemoveSchedule { get; private set; }
        public BuffStat BuffStat { get; private set; }
        public int BuffValue { get; private set; }
        public byte Stacks { get; private set; }
        public MapleMonster Victim { get; private set; }

        public MonsterBuff(int ownerId, int skillId, int durationMS, BuffStat buffStat, int buffValue, MapleMonster victim, byte stacks = 0)
        {
            OwnerId = ownerId;
            SkillId = skillId;
            Duration = durationMS;
            BuffStat = buffStat;
            BuffValue = buffValue;
            Victim = victim;
            Stacks = stacks;
            if (buffStat != MonsterBuffStat.POISON) //Poison will remove itself after all ticks
                RemoveSchedule = Scheduler.ScheduleRemoveMonsterStatusEffect(this, (uint)durationMS);
        }

        public virtual PacketWriter GetApplicationPacket()
        {
            PacketWriter pw = new PacketWriter();
            pw.WriteHeader(SendHeader.GiveMonsterBuff);
            pw.WriteInt(Victim.ObjectId);
            WriteSingleBuffMask(pw, BuffStat);
            pw.WriteInt(BuffValue);
            pw.WriteInt(SkillId);
            pw.WriteShort(0);
            if (BuffStat.IsStackingBuff)
            {
                pw.WriteByte(Stacks);
            }
            pw.WriteShort(0);
            pw.WriteByte(0); 
            pw.WriteByte(1);
            pw.WriteByte(1);
            return pw;
        }

        public virtual PacketWriter GetRemovePacket()
        {
            PacketWriter pw = new PacketWriter();
            pw.WriteHeader(SendHeader.RemoveMonsterBuff);
            pw.WriteInt(Victim.ObjectId);
            WriteSingleBuffMask(pw, BuffStat);
            pw.WriteInt(0);
            return pw;
        }

        public virtual void Dispose(bool silent)
        {
            if (Victim != null && Victim.Map != null)
            {
                if (!silent)
                    Victim.Map.BroadcastPacket(GetRemovePacket());
                Victim.RemoveStatusEffect(this);
            }
            Victim = null;
            Scheduler.DisposeTimer(RemoveSchedule);
        }

        public static void WriteSingleBuffMask(PacketWriter pw, BuffStat buffStat)
        {
            WriteBuffMask(pw, new List<BuffStat>() { buffStat });
        }

        public static void WriteBuffMask(PacketWriter pw, List<BuffStat> buffStats)
        {
            int[] mask = new int[3];
            foreach (BuffStat buffStat in buffStats)
            {
                int pos = buffStat.BitIndex;                
                int maskIndex = pos / 32;
                int relativeBitPos = pos % 32;
                int bit = 1 << 31 - relativeBitPos;
                mask[maskIndex] |= bit;
            }
            for (int i = 0; i < mask.Length; i++)
            {
                pw.WriteInt(mask[i]);
            }
        }        
    }

    public class Poison : MonsterBuff
    {
        int Damage { get; set; }
        Timer PoisonSchedule { get; set; }
        MapleCharacter Applicant { get; set; }
        int ElapsedTicks { get; set; }
        int TotalTicks { get; set; }
        int Interval { get; set; }
        public Poison(int skillId, int durationMS, MapleMonster victim, int damage, int intervalMS, MapleCharacter applicant) 
            : base(applicant.Id, skillId, durationMS, MonsterBuffStat.POISON, 0, victim)
        {
            Interval = intervalMS;
            ElapsedTicks = 0;
            TotalTicks = durationMS / intervalMS;
            Applicant = applicant;
            Damage = damage;
            if (TotalTicks > 0)
            {
                PoisonSchedule = Scheduler.ScheduleRepeatingAction(() =>
                {
                    if (ElapsedTicks < TotalTicks && victim != null && applicant != null)
                    {                        
                        victim.Damage(applicant, Damage, true, true);
                        ElapsedTicks++;                        
                    }
                    else
                    {
                        Dispose(false);
                    }
                }, 1000);
            }
        }

        public override void Dispose(bool death)
        {
            Scheduler.DisposeTimer(PoisonSchedule);            
            Applicant = null;
            base.Dispose(death);            
        }

        public override PacketWriter GetApplicationPacket()
        {
            PacketWriter pw = new PacketWriter();
            pw.WriteHeader(SendHeader.GiveMonsterBuff);
            pw.WriteInt(Victim.ObjectId);
            WriteSingleBuffMask(pw, BuffStat);
            pw.WriteByte(1);
            pw.WriteInt(OwnerId);
            pw.WriteInt(SkillId);
            pw.WriteInt(Damage);
            pw.WriteInt(Interval);
            pw.WriteInt(0);
            pw.WriteInt(Duration);
            pw.WriteInt(Duration / 1000);
            pw.WriteInt(0);
            pw.WriteInt(0xF); //?
            pw.WriteInt(Damage);

            pw.WriteShort(0);
            pw.WriteByte(1);
            return pw;
        }

        public override PacketWriter GetRemovePacket()
        {
            PacketWriter pw = new PacketWriter();
            pw.WriteHeader(SendHeader.RemoveMonsterBuff);
            pw.WriteInt(Victim.ObjectId);
            WriteSingleBuffMask(pw, BuffStat);
            pw.WriteInt(0);
            pw.WriteInt(1);
            pw.WriteInt(OwnerId);
            pw.WriteInt(SkillId);
            pw.WriteByte(3);
            
            return pw;
        }
    }

    

    public static class MonsterBuffStat
    {
        public static readonly BuffStat WATK = new BuffStat(0);
        public static readonly BuffStat WDEF = new BuffStat(1);
        public static readonly BuffStat MATK = new BuffStat(2);
        public static readonly BuffStat MDEF = new BuffStat(3);
        public static readonly BuffStat ACC = new BuffStat(4);
        public static readonly BuffStat AVOID = new BuffStat(5);
        public static readonly BuffStat FREEZE = new BuffStat(6, false, true);
        public static readonly BuffStat STUN =          new BuffStat(7);
        public static readonly BuffStat IMMOBILIZE =    new BuffStat(8);




        public static readonly BuffStat DAM_R_TAKEN = new BuffStat(45);
        public static readonly BuffStat POISON      =   new BuffStat(58);
        

    }
}
