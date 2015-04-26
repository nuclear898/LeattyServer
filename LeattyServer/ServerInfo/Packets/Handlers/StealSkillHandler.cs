using System.Linq;
using LeattyServer.Constants;
using LeattyServer.ServerInfo.Player;
using LeattyServer.ServerInfo.Player.ResourceSystems;

namespace LeattyServer.ServerInfo.Packets.Handlers
{
    public class StealSkillHandler
    {
        public static void HandleSkillSwipe(MapleClient c, PacketReader pr)
        {
            MapleCharacter chr = c.Account.Character;           
            if (!chr.IsPhantom || chr.Map == null)
                return;
            int targetId = pr.ReadInt();
            MapleCharacter target = chr.Map.GetCharacter(targetId);
            if (target != null && target.IsExplorer)
            {
                PacketWriter pw = new PacketWriter();
                pw.WriteHeader(SendHeader.ShowStealSkills);

                pw.WriteByte(1);
                var stealableSkills = target.GetSkillList().Where(x => x.IsStealable);
                pw.WriteInt(stealableSkills.Any() ? 4 : 2);
                pw.WriteInt(targetId);
                pw.WriteInt(target.Job);
                pw.WriteInt(stealableSkills.Count());
                foreach (Skill skill in stealableSkills)
                {
                    pw.WriteInt(skill.SkillId);
                }
                c.SendPacket(pw);
            }
        }

        public static void HandleStealSkill(MapleClient c, PacketReader pr)
        {
            //[6A 88 1E 00] [F0 B2 30 00] [00] //choose new
            //[6A 88 1E 00] [00 00 00 00] [01] //remove
            MapleCharacter chr = c.Account.Character;
            if (!chr.IsPhantom)
                return;
            PhantomSystem resource = chr.Resource as PhantomSystem;
            if (resource == null)
                return;          
            int skillId = pr.ReadInt();
            int stolenFrom = pr.ReadInt();
            //last byte: 0 = learn, 1 = remove, but we dont need this since chrId == 0 when removing            
            if (stolenFrom == 0) //remove
            {
                int arrayIndex = resource.GetSkillIndex(skillId);
                if (arrayIndex >= 0)
                {                    
                    resource.StolenSkills[arrayIndex] = 0;
                    resource.RemoveChosenSkill(skillId);
                    int jobNum = PhantomSystem.GetJobNum(arrayIndex);
                    int index = PhantomSystem.GetJobPositionIndex(arrayIndex);
                    chr.RemoveSkillSilent(skillId);
                    c.SendPacket(RemoveStolenSkill(jobNum, index));
                }
            }
            else
            {
                if (resource.GetSkillIndex(skillId) > -1) //Already have the skill
                    return;
                MapleCharacter target = Program.GetCharacterById(stolenFrom);
                if (target != null)
                {
                    Skill targetsSkill = target.GetSkill(skillId);                    
                    if (targetsSkill != null && targetsSkill.IsStealable)
                    {
                        
                        int jobNum = JobConstants.GetJobNumber(skillId / 10000);
                        int arrayIndex = resource.AddStolenSkill(targetsSkill.SkillId, jobNum);
                        int index = PhantomSystem.GetJobPositionIndex(arrayIndex);
                        if (arrayIndex >= 0 && index >= 0)
                        {
                            Skill skill = new Skill(skillId, 0, targetsSkill.Level);
                            chr.AddSkillSilent(skill);
                            c.SendPacket(AddStolenSkill(skillId, targetsSkill.Level, jobNum, index));
                        }
                    }
                }
            }
        }

        public static void HandleChooseSkill(MapleClient c, PacketReader pr)
        {
            //Equip  : [E9 39 6E 01] [EB 0C 3D 00]
            //Unequip: [E9 39 6E 01] [00 00 00 00]            
            MapleCharacter chr = c.Account.Character;
            PhantomSystem resource = chr.Resource as PhantomSystem;
            if (!chr.IsPhantom || resource == null)
                return;           
            int skillBase = pr.ReadInt();
            int skillId = pr.ReadInt();
            if (skillId > 0) //choose new
            {
                if (!resource.HasSkill(skillId))
                    return;
                int jobNum = JobConstants.GetJobNumber(skillId / 10000);
                if (jobNum != PhantomSystem.GetJobIndexByImpeccableMemory(skillBase))
                    return;
                if (jobNum > 0 && jobNum < 5)
                {
                    resource.ChosenSkills[jobNum-1] = skillId;
                    c.SendPacket(UpdateChosenSkill(skillBase, skillId));                    
                }
            }
            else //Unequip chosen skill
            {
                int jobNum = JobConstants.GetJobNumber(skillBase);
                if (jobNum > 0 && jobNum < 5) {
                    resource.ChosenSkills[jobNum-1] = 0;
                    c.SendPacket(UpdateChosenSkill(skillBase, 0));
                }
            }
        }

        private static PacketWriter AddStolenSkill(int skillId, int skillLevel, int jobNum, int index)
        {
            //[01] [00] [01 00 00 00] [01 00 00 00] [70 88 1E 00] [14 00 00 00] [00 00 00 00]
            PacketWriter pw = new PacketWriter();
            pw.WriteHeader(SendHeader.UpdateStolenSkill);

            pw.WriteByte(1);
            pw.WriteByte(0);
            pw.WriteInt(jobNum);
            pw.WriteInt(index);
            pw.WriteInt(skillId);
            pw.WriteInt(skillLevel);
            pw.WriteInt(0);

            return pw;
        }

        private static PacketWriter RemoveStolenSkill(int jobNum, int index)
        {
            //[01] [03] [01 00 00 00] [02 00 00 00]
            PacketWriter pw = new PacketWriter();
            pw.WriteHeader(SendHeader.UpdateStolenSkill);

            pw.WriteByte(1);
            pw.WriteByte(3);
            pw.WriteInt(jobNum);
            pw.WriteInt(index);            

            return pw;
        }

        private static PacketWriter UpdateChosenSkill(int baseSkill, int skillId)
        {
            //[01] [01] [E9 39 6E 01] [EB 0C 3D 00] choose new
            //[01] [00] [E9 39 6E 01]               clear
            PacketWriter pw = new PacketWriter();
            pw.WriteHeader(SendHeader.UpdateChosenStolenSkill);

            bool update = skillId > 0;
            pw.WriteByte(1);
            pw.WriteBool(update); //true = new chosen skill, false = clear chosen skill
            pw.WriteInt(baseSkill);
            if (update)
                pw.WriteInt(skillId);
            return pw;
        }      
    }
}
