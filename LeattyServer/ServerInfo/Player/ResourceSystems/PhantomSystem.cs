using LeattyServer.Constants;
using LeattyServer;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LeattyServer.Data;
using LeattyServer.DB.Models;

namespace LeattyServer.ServerInfo.Player.ResourceSystems
{
    public class PhantomSystem : ResourceSystem
    {
        public int[] StolenSkills { get; set; }
        public int[] ChosenSkills { get; set; }
        public int CardDeck { get; set; }

        public PhantomSystem()
            : base(ResourceSystemType.Phantom)
        {
            StolenSkills = new int[13];
            ChosenSkills = new int[4];
            CardDeck = 0;
        }

        public void PopulateSkills(List<StolenSkill> skills)
        {
            foreach (StolenSkill skill in skills)
            {
                int index = skill.Index;
                if (index >= 0 && index < 13)
                {
                    StolenSkills[index] = skill.SkillId;
                    if (skill.Chosen)
                        ChosenSkills[GetChosenIndex(index)] = skill.SkillId;
                }
                else
                {
                    using (LeattyContext DBContext = new LeattyContext())
                    {
                        DBContext.StolenSkills.Remove(skill); //invalid job index
                        DBContext.SaveChanges();
                    }
                }
            }
        }

        public override void SaveToDatabase(int chrId, bool detach = false)
        {
            using (LeattyContext DBContext = new LeattyContext())
            {
                List<StolenSkill> stolenSkillsDatabase = DBContext.StolenSkills.Where(x => x.CharacterId == chrId).ToList();
                for (int i = 0; i < StolenSkills.Length; i++)
                {
                    StolenSkill stolenDatabaseSkill = stolenSkillsDatabase.Where(x => x.Index == i).FirstOrDefault();
                    if (stolenDatabaseSkill != null) //update record
                    {
                        stolenDatabaseSkill.SkillId = StolenSkills[i];
                        stolenDatabaseSkill.Chosen = IsChosenSkill(StolenSkills[i]);
                    }
                    else //insert new record
                    {
                        if (StolenSkills[i] > 0) //Only insert if skill id > 0, otherwise it isnt nesecary
                        {
                            StolenSkill insertStolenSkill = new StolenSkill();
                            insertStolenSkill.CharacterId = chrId;
                            insertStolenSkill.SkillId = StolenSkills[i];
                            insertStolenSkill.Index = (byte)i;
                            insertStolenSkill.Chosen = IsChosenSkill(StolenSkills[i]);
                            DBContext.StolenSkills.Add(insertStolenSkill);
                            if (detach)
                                DBContext.SaveChanges();
                        }
                    }
                    if (detach)
                        DBContext.Entry<StolenSkill>(stolenDatabaseSkill).State = System.Data.Entity.EntityState.Detached;
                }
            }
        }

        private bool IsChosenSkill(int skillId)
        {
            foreach (int i in ChosenSkills)
            {
                if (skillId == i)
                    return true;
            }
            return false;
        }

        /// <summary>
        /// Finds the first index that has skillId == 0 and returns the index number for the StolenSkills array
        /// </summary>
        /// <param name="skillId"></param>
        /// <param name="jobNum"></param>
        /// <returns>Returns -1 if no free spots</returns>
        public int AddStolenSkill(int skillId, int jobNum)
        {
            int startIndex = 0;
            int indexes = 4;
            switch (jobNum)
            {
                case 1:
                    startIndex = 0;
                    break;
                case 2:
                    startIndex = 4;
                    break;
                case 3:
                    startIndex = 8;
                    indexes = 3;
                    break;
                case 4:
                    startIndex = 11;
                    indexes = 2;
                    break;
                default:
                    return -1;
            }
            for (int i = startIndex; i < startIndex + indexes; i++)
            {
                if (StolenSkills[i] == 0)
                    return i;
            }
            return -1;
        }

        public static int GetJobNum(int stolenSkillIndex)
        {
            return GetChosenIndex(stolenSkillIndex) + 1;
        }

        public static int GetChosenIndex(int stolenSkillIndex)
        {
            if (stolenSkillIndex >= 0 && stolenSkillIndex < 4)
                return 0;
            if (stolenSkillIndex >= 4 && stolenSkillIndex < 8)
                return 1;
            if (stolenSkillIndex >= 8 && stolenSkillIndex < 11)
                return 2;
            if (stolenSkillIndex >= 11 && stolenSkillIndex < 13)
                return 3;
            return -1;
        }

        public static int GetJobPositionIndex(int stolenSkillIndex)
        {
            if (stolenSkillIndex >= 0 && stolenSkillIndex < 4)
                return stolenSkillIndex;
            if (stolenSkillIndex >= 4 && stolenSkillIndex < 8)
                return stolenSkillIndex - 4;
            if (stolenSkillIndex >= 8 && stolenSkillIndex < 11)
                return stolenSkillIndex - 8;
            if (stolenSkillIndex >= 11 && stolenSkillIndex < 13)
                return stolenSkillIndex - 11;
            return -1;
        }

        public int GetSkillIndex(int skillId)
        {
            for (int i = 0; i < StolenSkills.Length; i++)
            {
                if (StolenSkills[i] == skillId)
                    return i;
            }
            return -1;
        }

        public bool HasSkill(int skillId)
        {
            return GetSkillIndex(skillId) > -1;
        }

        public int GetChosenSkillIndex(int skillId)
        {
            for (int i = 0; i < ChosenSkills.Length; i++)
            {
                if (ChosenSkills[i] == skillId)
                    return i;
            }
            return -1;
        }

        public bool HasChosenSkill(int skillId)
        {
            return GetChosenSkillIndex(skillId) > -1;
        }

        public void RemoveChosenSkill(int skillId)
        {
            for (int i = 0; i < ChosenSkills.Length; i++)
            {
                if (ChosenSkills[i] == skillId)
                    ChosenSkills[i] = 0;
            }
        }

        public static int GetStealSkill(int jobNum)
        {
            switch (jobNum)
            {
                case 1:
                    return Phantom1.IMPECCABLE_MEMORY_I;
                case 2:
                    return Phantom2.IMPECCABLE_MEMORY_II;
                case 3:
                    return Phantom3.IMPECCABLE_MEMORY_III;
                case 4:
                    return Phantom4.IMPECCABLE_MEMORY_IV;
            }
            return 0;
        }

        public static int GetJobIndexByImpeccableMemory(int skillId)
        {
            switch (skillId)
            {
                case Phantom1.IMPECCABLE_MEMORY_I:
                    return 1;
                case Phantom2.IMPECCABLE_MEMORY_II:
                    return 2;
                case Phantom3.IMPECCABLE_MEMORY_III:
                    return 3;
                case Phantom4.IMPECCABLE_MEMORY_IV:
                    return 4;
                default:
                    return 0;
            }
        }
    }
}

 
