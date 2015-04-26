using LeattyServer.Constants;
using LeattyServer.Data;
using LeattyServer.Data.WZ;
using LeattyServer.ServerInfo.Player;

namespace LeattyServer.ServerInfo.Packets.Handlers
{
    class DistributeSPHandler
    {
        public static void Handle(MapleClient c, PacketReader packet)
        {
            MapleCharacter chr = c.Account.Character;
            if (!chr.DisableActions()) return;
            try
            {
                int tickCount = packet.ReadInt();
                int skillId = packet.ReadInt();
                byte amount = packet.ReadByte(); //new v137, multiple sp
                if (amount <= 0)
                    amount = 1;

                short skillJobId = (short)(skillId / 10000);

                WzCharacterSkill wzSkill = DataBuffer.GetCharacterSkillById(skillId);
                if (wzSkill == null)
                    return;
                if (wzSkill.IsGmSkill && !chr.IsStaff)                    
                    return;                    
                if (!JobConstants.JobCanLearnSkill(skillId, chr.Job))
                    return;
                if (wzSkill.HasFixedLevel)
                    return;
                if (chr.Level < wzSkill.RequiredLevel)
                    return;
                if (wzSkill.RequiredSkills != null)
                {
                    foreach (var kvp in wzSkill.RequiredSkills)
                    {
                        if (!chr.HasSkill(kvp.Key, kvp.Value))
                            return;
                    }
                }
                if (wzSkill.IsHyperSkill)
                {
                    chr.SendPopUpMessage("Hyper skills aren't functional yet.");
                    chr.EnableActions();
                    return;
                }
                if (JobConstants.IsBeginnerJob(skillJobId))
                {
                    switch (skillId)
                    {
                        //Three snails:
                        case Explorer.THREE_SNAILS:
                        case Cygnus.THREE_SNAILS:
                        case AranBasics.THREE_SNAILS:
                        case EvanBasics.THREE_SNAILS:
                        case MihileBasics.THREE_SNAILS:
                        //Recovery:
                        case Explorer.RECOVERY:
                        case Cygnus.RECOVERY:
                        case AranBasics.RECOVERY:
                        case EvanBasics.RECOVER:
                        case MihileBasics.RECOVERY:
                        //Nimble Feet:
                        case Explorer.NIMBLE_FEET:
                        case Cygnus.NIMBLE_FEET:
                        case AranBasics.AGILE_BODY:
                        case EvanBasics.NIMBLE_FEET:
                        case MihileBasics.NIMBLE_FEET:
                        //Resistance:                          
                        case Resistance.POTION_MASTERY:
                        case Resistance.INFILTRATE:
                        case Resistance.CRYSTAL_THROW:
                            if (chr.GetSkillLevel(skillId) + amount > 3) //already maxed                            
                                return;
                            int baseNum = 0;
                            switch (skillId / 100000)
                            {
                                case 300: //resistance
                                {
                                    if (!chr.IsResistance)
                                        return;
                                    int usedBeginnerSP = chr.GetSkillLevel(Resistance.CRYSTAL_THROW) + chr.GetSkillLevel(Resistance.INFILTRATE) + chr.GetSkillLevel(Resistance.POTION_MASTERY);
                                    if (usedBeginnerSP + amount <= 9 && chr.GetSkillLevel(skillId) + amount <= 3)
                                    {                                              
                                        chr.IncreaseSkillLevel(skillId, amount);                                                
                                    }
                                    break;
                                }
                                case 0:
                                    if (!chr.IsExplorer)
                                        return;
                                    goto common;
                                case 100: //cygnus
                                    if (!chr.IsCygnus)
                                        return;
                                    baseNum = 10000000;
                                    goto common;
                                case 200: //hero
                                    if (!chr.IsAran && !chr.IsEvan)
                                        return;
                                    baseNum = 20000000;
                                    goto common;
                                case 500: //mihile
                                    if (!chr.IsMihile)
                                        return;
                                    baseNum = 50000000;
                                    common:
                                {
                                    int usedBeginnerSP = chr.GetSkillLevel(baseNum + 1000) + chr.GetSkillLevel(baseNum + 1001) + chr.GetSkillLevel(baseNum + 1002);
                                    if (usedBeginnerSP + amount <= 6 && chr.GetSkillLevel(skillId) + amount <= 3)
                                    {
                                        chr.IncreaseSkillLevel(skillId, amount);                                                
                                    }
                                    break;
                                }
                                default:
                                    return;
                            }
                            break;
                        default:
                            return;
                    }
                }
                else
                {
                    int spTableIndex = JobConstants.GetSkillBookForJob(skillJobId);
                    if (chr.SpTable[spTableIndex] >= amount)
                    {   
                        Skill skill = chr.GetSkill(skillId);
                        if (skill == null) //Player doesnt have the skill yet
                        {
                            int maxLevel = wzSkill.HasMastery ? wzSkill.DefaultMastery : wzSkill.MaxLevel;
                            if (amount <= maxLevel)
                            {
                                chr.LearnSkill(skillId, amount, wzSkill);
                                chr.SpTable[spTableIndex] -= amount;
                                MapleCharacter.UpdateSingleStat(c, MapleCharacterStat.Sp, chr.SpTable[0], true);
                            }
                        }
                        else
                        {                                
                            if (skill.Level + amount <= skill.MasterLevel)
                            {
                                chr.IncreaseSkillLevel(skillId, amount);
                                chr.SpTable[spTableIndex] -= amount;
                                MapleCharacter.UpdateSingleStat(c, MapleCharacterStat.Sp, chr.SpTable[0], true);
                            }
                        }
                    }
                }
            }
            finally
            {
                chr.EnableActions(false);
            }
        }
    }
}
