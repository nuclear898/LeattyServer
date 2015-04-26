using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using LeattyServer.Constants;
using LeattyServer.Data;
using LeattyServer.Data.Scripts;
using LeattyServer.Data.WZ;
using LeattyServer.Helpers;
using LeattyServer.ServerInfo.Inventory;
using LeattyServer.ServerInfo.Map;
using LeattyServer.ServerInfo.Guild;
using LeattyServer.ServerInfo.Movement;
using LeattyServer.ServerInfo.Quest;
using LeattyServer.ServerInfo.Player.ResourceSystems;
using LeattyServer.DB.Models;
using LeattyServer.ServerInfo.BuddyList;
using LeattyServer.ServerInfo.Packets;
using LeattyServer.ServerInfo.Party;

namespace LeattyServer.ServerInfo.Player
{
    public class MapleCharacter : Character
    {
        #region Init
        public MapleClient Client { get; private set; }
        public int[] SpTable { get; set; }
        public Point Position { get; set; }
        public byte Stance { get; set; }
        public short Foothold { get; set; }
        public BuffedCharacterStats Stats { get; }

        public Dictionary<InviteType, Invite> Invites = new Dictionary<InviteType, Invite>();
        public bool Hidden { get; set; }

        public string FairyBlessingOrigin { get; private set; }
        public string EmpressBlessingOrigin { get; private set; }

        private Dictionary<int, Skill> Skills = new Dictionary<int, Skill>();
        private readonly Dictionary<int, Cooldown> Cooldowns = new Dictionary<int, Cooldown>();
        private readonly Dictionary<int, Buff> Buffs = new Dictionary<int, Buff>();
        private readonly Dictionary<int, MapleSummon> Summons = new Dictionary<int, MapleSummon>();
        private readonly Dictionary<int, MapleQuest> StartedQuests = new Dictionary<int, MapleQuest>();
        private readonly Dictionary<int, uint> CompletedQuests = new Dictionary<int, uint>();
        private readonly Dictionary<string, string> CustomQuestData = new Dictionary<string, string>();

        public SkillMacro[] SkillMacros { get; } = new SkillMacro[5];

        private readonly List<SpecialPortal> Doors = new List<SpecialPortal>();

        public MapleBuddyList BuddyList { get; set; }

        public Dictionary<uint, Pair<byte, int>> Keybinds { get; private set; }
        private bool KeybindsChanged;

        public int[] QuickSlotKeys { get; private set; }
        private bool QuickSlotKeyBindsChanged;

        private static readonly object CharacterDatabaseLock = new object();
        public List<MapleMovementFragment> LastMove { get; set; }
        public ReactorActionState ReactorActionState { get; set; }
        public MapleMap Map { get; set; }
        public MapleInventory Inventory { get; private set; }
        public MapleGuild Guild { get; set; }
        public MapleTrade Trade { get; set; }
        public MapleParty Party { get; set; }
        public MapleMessengerRoom ChatRoom { get; set; }
        public DateTime LastAttackTime { get; set; }
        public ResourceSystem Resource { get; set; }
        public ActionState ActionState { get; private set; } = ActionState.Disabled;

        private readonly object HpLock = new object();

        public MapleCharacter()
        {
            Hidden = false;
            Stats = new BuffedCharacterStats();
            LastAttackTime = DateTime.FromFileTime(0);
        }

        public static MapleCharacter GetDefaultCharacter(MapleClient client)
        {
            MapleCharacter newCharacter = new MapleCharacter
            {
                AccountId = client.Account.Id,
                MapId = 140090000, //Ice Cave
                Level = 1,
                Job = 0,
                SubJob = 0,
                Str = 4,
                Dex = 4,
                Luk = 4,
                Int = 4,
                Hp = 50,
                Mp = 5,
                MaxHp = 50,
                MaxMp = 5,
                Exp = 0,
                AP = 8,
                SpTable = new int[10],
                Position = new Point(0, 0),
                Stance = 0,
                Sp = "0,0,0,0,0,0,0,0,0,0",
                Mesos = 0,
                BuddyCapacity = 50
            };

            newCharacter.Inventory = new MapleInventory(newCharacter);

            newCharacter.Stats.Recalculate(newCharacter);

            return newCharacter;
        }
        #endregion

        #region Exp, Level, Job
        public void GainExp(int exp, bool show = false, bool fromMonster = false)
        {
            if (exp == 0 || Level >= 250 || (Level >= 120 && IsCygnus))
                return;
            Exp += exp;
            while (Exp > GameConstants.GetCharacterExpNeeded(Level) && GameConstants.GetCharacterExpNeeded(Level) != 0)
            {
                Exp -= GameConstants.GetCharacterExpNeeded(Level);
                LevelUp();
            }
            UpdateSingleStat(Client, MapleCharacterStat.Exp, Exp, false);
            if (show)
            {
                if (fromMonster)
                {
                    Client.SendPacket(MapleCharacter.ShowExpFromMonster(exp));
                }
                else
                {
                    //todo: show exp in chat
                }
            }
        }

        public void LevelUp()
        {
            if (Level == 250 || (Level == 120 && IsCygnus))
                return;

            Level++;

            int apIncrease = (IsCygnus && Level <= 70) ? 6 : 5;
            AP += (short)apIncrease;

            #region HpMpIncrease
            int hpInc = 0, mpInc = 0;

            if (IsBeginnerJob)
            {
                hpInc = 13;
                mpInc = 10;
            }
            else if (IsWarrior || IsDawnWarrior || IsMihile)
            {
                hpInc = 66;
                mpInc = 6;
            }
            else if (IsAran)
            {
                hpInc = 46;
                mpInc = 6;
            }
            else if (IsDemonSlayer)
            {
                hpInc = 54;
            }
            else if (IsDemonAvenger)
            {
                hpInc = 30;
            }
            else if (IsHayato)
            {
                hpInc = 46;
                mpInc = 8;
            }
            else if (IsKaiser)
            {
                hpInc = 65;
                mpInc = 5;
            }
            else if (IsMagician || IsBlazeWizard || IsEvan)
            {
                hpInc = IsEvan ? 18 : 12;
                mpInc = 48;
            }
            else if (IsLuminous)
            {
                hpInc = 18;
                mpInc = 200;
            }
            else if (IsBattleMage)
            {
                hpInc = 36;
                mpInc = 28;
            }
            else if (IsKanna)
            {
                hpInc = 14;
            }
            else if (IsArcher || IsWindArcher || IsMercedes || IsWildHunter || IsThief || IsNightWalker || IsPhantom || IsXenon)
            {
                hpInc = 22;
                mpInc = 15;
            }
            else if (IsPirate || IsJett || IsThunderBreaker || IsMechanic)
            {
                hpInc = 24;
                mpInc = 20;
            }
            else if (IsCannonneer)
            {
                hpInc = 41;
                mpInc = 22;
            }
            else if (IsAngelicBuster)
            {
                hpInc = 30;
            }
            else if (Job >= 900 && Job <= 910)
            {
                hpInc = 500;
                mpInc = 500;
            }
            else
                FileLogging.Log("LevelUpStats.txt", "Unhandled Job: " + Job + " when giving hp and mp in MapleCharacter.LevelUp()");

            MaxHp += hpInc;
            MaxMp += mpInc;

            MaxHp = Math.Min(500000, Math.Abs(MaxHp));
            MaxMp = Math.Min(500000, Math.Abs(MaxMp));

            //recalc stats

            Hp = MaxHp;
            Mp = MaxMp;
            #endregion

            Stats.Recalculate(this);
            Hp = Stats.MaxHp;
            Mp = Stats.MaxMp;
            if (Level > 10 || (Level > 8 && Job == JobConstants.MAGICIAN))
                SpTable[CurrentLevelSkillBook] += 3;

            SortedDictionary<MapleCharacterStat, long> updatedStats = new SortedDictionary<MapleCharacterStat, long>();
            updatedStats.Add(MapleCharacterStat.Level, Level);
            updatedStats.Add(MapleCharacterStat.Hp, Hp);
            updatedStats.Add(MapleCharacterStat.MaxHp, MaxHp);
            updatedStats.Add(MapleCharacterStat.Mp, Mp);
            updatedStats.Add(MapleCharacterStat.MaxMp, MaxMp);
            updatedStats.Add(MapleCharacterStat.Ap, AP);
            updatedStats.Add(MapleCharacterStat.Sp, SpTable[0]);

            if ((IsCygnus || IsMihile) && Level < 120)
            {
                Exp += GameConstants.GetCharacterExpNeeded(Level) / 10;
                updatedStats.Add(MapleCharacterStat.Exp, Exp);
            }

            UpdateStats(Client, updatedStats, false);
            CheckAutoAdvance();
        }

        public void ChangeJob(short newJob)
        {
            if (DataBuffer.GetJobNameById(newJob).Length == 0) //check if valid job
                return;
            Job = newJob;
            if (Job != JobConstants.THIEF)
                SubJob = JobConstants.GetSubJobByJob(newJob);

            #region Update resource systems
            UpdateResourceSystem();
            #endregion

            SortedDictionary<MapleCharacterStat, long> updatedStats = new SortedDictionary<MapleCharacterStat, long> { { MapleCharacterStat.Job, Job } };

            if (newJob % 10 >= 1 && Level >= 70) //3rd job or higher
            {
                AP += 5;
                updatedStats.Add(MapleCharacterStat.Ap, AP);
            }
            int oldMp = MaxMp;
            #region HP increase
            switch (Job)
            {
                #region 1st Job
                case JobConstants.SWORDMAN:
                case JobConstants.DAWNWARRIOR1:
                case JobConstants.ARAN1:
                case JobConstants.HAYATO1:
                case JobConstants.MIHILE1:
                    MaxHp += 269;
                    MaxMp += 10;
                    break;
                case JobConstants.DEMONSLAYER1:
                    MaxHp += 659;
                    break;
                case JobConstants.DEMONAVENGER1:
                    MaxHp += 2209;
                    break;
                case JobConstants.KAISER1:
                    MaxHp += 358;
                    MaxMp += 109;
                    break;
                case JobConstants.MAGICIAN:
                case JobConstants.BLAZEWIZARD1:
                case JobConstants.EVAN1:
                case JobConstants.BEASTTAMER1:
                    MaxHp += 8;
                    MaxMp += 176;
                    break;
                case JobConstants.BATTLEMAGE1:
                    MaxHp += 175;
                    MaxMp += 181;
                    break;
                case JobConstants.KANNA1:
                    MaxHp += 100; //untested
                    break;
                case JobConstants.ARCHER:
                case JobConstants.WINDARCHER1:
                case JobConstants.MERCEDES1:
                case JobConstants.WILDHUNTER1:
                case JobConstants.THIEF:
                case JobConstants.NIGHTWALKER1:
                case JobConstants.PHANTOM1:
                case JobConstants.PIRATE:
                case JobConstants.JETT1:
                case JobConstants.THUNDERBREAKER1:
                case JobConstants.MECHANIC1:
                    MaxHp += 159;
                    MaxMp += 59;
                    break;
                case JobConstants.CANNONEER1:
                    MaxHp += 228;
                    MaxMp += 14;
                    break;
                case JobConstants.XENON1:
                    MaxHp += 459;
                    MaxMp += 159;
                    break;
                #endregion
                #region other jobs
                default:
                    switch (Job / 10)
                    {
                        case 11: //Fighter
                        case 12: //Page
                        case 13: //Dragon Knight
                        case 111: //Dawn Warrior
                        case 211: //Aran
                        case 411: //Hayato
                        case 611: //Kaiser
                        case 1011: //Zero
                            MaxHp += 375;
                            break;
                        case 311: //Demon Slayer
                        case 312: //Demon Avenger
                        case 511: //Mihile
                            MaxHp += 500;
                            break;
                        case 21: //Fire-Poison wizard
                        case 22: //Ice-Lightning wizard
                        case 23: //Cleric
                        case 121: //Blaze Wizard
                        case 1121: //Beast Tamer
                            MaxMp += 480;
                            break;
                        case 221: //Evan         
                            MaxMp += 100;
                            break;
                        case 270: //Luminous
                            MaxMp += 300;
                            break;
                        case 321: //Battle Mage
                            MaxHp += 200;
                            MaxMp += 100;
                            break;
                        case 421: //Kanna
                            MaxHp += 460;
                            break;
                        case 31: //Bowman
                        case 32: //Crossbowman
                        case 41: //Assassin
                        case 42: //Bandit
                        case 51: //Gunslinger
                        case 52: //Brawler
                        case 57: //Jett
                        case 131: //Wind Archer
                        case 141: //Night Walker
                        case 241: //Phantom
                            MaxHp += 310;
                            MaxMp += 160;
                            break;
                        case 231: //Mercedes
                            MaxHp += 170;
                            MaxMp += 160;
                            break;
                        case 331: //Wild Hunter
                            MaxHp += 210;
                            MaxMp += 100;
                            break;
                        case 43: //DualBlade
                        case 361: //Xenon, not checked
                            MaxHp += 175;
                            MaxHp += 100;
                            break;
                        case 53: //Cannoneer
                            MaxHp += 325;
                            MaxHp += 85;
                            break;
                        case 351: //Mechanic
                            MaxHp += 150;
                            MaxMp += 100;
                            break;
                        case 651: //Angelic Burster
                            MaxHp += 350;
                            break;
                        default:
                            if (!IsBeginnerJob) //GMs can changejob to beginner jobs
                                FileLogging.Log("ChangeJobStats.txt", "Unhandled Job: " + Job + " when giving hp in MapleCharacter.ChangeJob()");
                            break;
                    }
                    break;
                    #endregion
            }
            Hp = MaxHp;
            updatedStats.Add(MapleCharacterStat.MaxHp, MaxHp);
            if (oldMp != MaxMp)
                updatedStats.Add(MapleCharacterStat.MaxMp, MaxMp);
            #endregion

            if (!IsBeginnerJob)
            {
                short sp = 4;
                if (!IsAran)
                {
                    if ((newJob >= JobConstants.EVAN1 && newJob <= JobConstants.EVAN5) || (IsResistance && newJob % 100 != 0))
                        sp = 3;
                    else if (newJob % 100 == 0)
                        sp = 5;
                    else
                        sp = 4;
                }
                if (newJob != JobConstants.DUALBLADE2) //DB 1+ job doesnt get sp
                {
                    SpTable[CurrentJobSkillBook] += sp;
                    updatedStats.Add(MapleCharacterStat.Sp, SpTable[0]);
                }
            }

            GiveBaseJobSkills(Job);
            Stats.Recalculate(this);
            Hp = Stats.MaxHp;
            Mp = Stats.MaxMp;
            updatedStats.Add(MapleCharacterStat.Hp, Hp);
            updatedStats.Add(MapleCharacterStat.Mp, Mp);
            UpdateStats(Client, updatedStats, false);
        }

        public void CheckAutoAdvance()
        {
            int newJob = -1;
            if (IsExplorer || IsCygnus)
            {
                if (IsCannonneer)
                {
                    if (Job == JobConstants.CANNONEER1 && Level >= 30)
                        newJob = JobConstants.CANNONEER2;
                    else if ((Job == JobConstants.CANNONEER2 && Level >= 60) || (Job == JobConstants.CANNONEER3 && Level >= 100))
                        newJob = Job + 1;
                }
                else if (IsDualBlade)
                {
                    if (Job == JobConstants.THIEF && Level >= 20)
                        newJob = JobConstants.DUALBLADE2;
                    if ((Job == JobConstants.DUALBLADE2 && Level >= 30) ||
                        (Job == JobConstants.DUALBLADE2P && Level >= 45) ||
                        (Job == JobConstants.DUALBLADE3 && Level >= 60) ||
                        (Job == JobConstants.DUALBLADE3P && Level >= 100))
                        newJob = Job + 1;
                }
                else if (IsCygnus && Level >= 30 && Level % 100 == 0)
                {
                    newJob = Job + 10;
                }
                else
                {
                    if (Job % 100 == 0 && Level >= 30)
                    {
                        OpenNpc(1092000);
                        return;
                    }
                    else if ((Job % 10 == 0 && Level >= 60) || (Job % 10 == 1 && Level >= 100))
                        newJob = Job + 1;
                }
            }
            else if (IsEvan)
            {
                if (Job == JobConstants.EVAN1 && Level >= 20)
                    newJob = JobConstants.EVAN2;
                else if ((Job == JobConstants.EVAN2 && Level >= 30) ||
                        (Job == JobConstants.EVAN3 && Level >= 40) ||
                        (Job == JobConstants.EVAN4 && Level >= 50) ||
                        (Job == JobConstants.EVAN5 && Level >= 60) ||
                        (Job == JobConstants.EVAN6 && Level >= 80) ||
                        (Job == JobConstants.EVAN7 && Level >= 100) ||
                        (Job == JobConstants.EVAN8 && Level >= 120) ||
                        (Job == JobConstants.EVAN9 && Level >= 160))
                    newJob = Job + 1;
            }
            else if (IsDemonAvenger)
            {
                if (Job == 3101 && Level >= 30)
                    newJob = 3120;
                else if ((Job == 3120 && Level >= 60) || (Job == 3121 && Level >= 100))
                    newJob = Job + 1;
            }
            else if (IsJett)
            {
                if (Job == 508 && Level >= 30)
                    newJob = 570;
                else if ((Job == 570 && Level >= 60) || (Job == 571 && Level >= 100))
                    newJob = Job + 1;
            }
            else
            {
                if (Job % 100 == 0 && Level >= 30)
                {
                    newJob = Job + 10;
                }
                else if ((Job % 10 == 0 && Level >= 60) || (Job % 10 == 1 && Level >= 100))
                    newJob = Job + 1;
            }

            if (newJob != -1)
                ChangeJob((short)newJob);
        }
        #endregion

        #region Skills, Cooldowns & Keybinds
        public bool HasSkill(int skillId, int skillLevel = 0)
        {
            int trueSkillLevel = GetSkillLevel(skillId);
            if (skillLevel > 0)
                return trueSkillLevel >= skillLevel;
            return trueSkillLevel > 0;
        }

        public byte GetSkillLevel(int skillId)
        {
            int trueSkillId = SkillConstants.CheckAndGetLinkedSkill(skillId);
            Skill skill;
            if (Skills.TryGetValue(trueSkillId, out skill))
                return skill.Level;
            return 0;
        }

        public byte GetSkillMasterLevel(int skillId)
        {
            Skill skill;
            if (Skills.TryGetValue(skillId, out skill))
            {
                return skill.MasterLevel;
            }
            return 0;
        }

        public Skill GetSkill(int skillId)
        {
            Skill skill;
            if (Skills.TryGetValue(skillId, out skill))
            {
                return skill;
            }
            return null;
        }

        public void IncreaseSkillLevel(int skillId, byte amount = 1, bool updateToClient = true)
        {
            Skill skill;
            if (Skills.TryGetValue(skillId, out skill))
            {
                skill.Level += amount;
                if (updateToClient)
                    Client.SendPacket(Skill.UpdateSingleSkill(skill));
                Stats.Recalculate(this);
            }
            else
            {
                LearnSkill(skillId, amount, null, updateToClient);
            }
        }

        public void SetSkillLevel(int skillId, byte level, byte masterLevel = 0, bool updateToClient = true)
        {
            Skill skill;
            if (Skills.TryGetValue(skillId, out skill))
            {
                skill.Level = level;
                if (masterLevel > 0)
                    skill.MasterLevel = masterLevel;
            }
            else
            {
                skill = new Skill(skillId);
                skill.Level = level;
                skill.MasterLevel = masterLevel;
                Skills.Add(skillId, skill);
            }
            if (updateToClient)
                Client.SendPacket(Skill.UpdateSkills(new List<Skill>() { skill }));
            Stats.Recalculate(this);
        }

        public void SetSkillExp(int SkillId, short Exp)
        {
            Skill skill;
            if (Skills.TryGetValue(SkillId, out skill))
                skill.SkillExp = Exp;
        }

        public void SetSkills(Dictionary<int, Skill> skills)
        {
            this.Skills = skills;
        }

        public void AddSkillSilent(Skill skill)
        {
            if (!Skills.ContainsKey(skill.SkillId))
                Skills.Add(skill.SkillId, skill);
        }

        public void RemoveSkillSilent(int skillId)
        {
            Skills.Remove(skillId);
        }

        public void LearnSkill(int skillId, byte level = 1, WzCharacterSkill skillInfo = null, bool updateToClient = true)
        {
            if (skillInfo == null)
            {
                skillInfo = DataBuffer.GetCharacterSkillById(skillId);
                if (skillInfo == null)
                    return;
            }
            if (!Skills.ContainsKey(skillId))
            {
                Skill skill = new Skill(skillId);
                skill.MasterLevel = skillInfo.HasMastery ? skillInfo.DefaultMastery : skillInfo.MaxLevel;
                skill.Level = level;
                AddSkill(skill);
                if (updateToClient)
                    Client.SendPacket(Skill.UpdateSingleSkill(skill));
                Stats.Recalculate(this);
            }
        }

        public void AddSkills(List<Skill> addSkills, bool updateToClient = true)
        {
            List<Skill> newSkills = new List<Skill>();
            foreach (Skill skill in addSkills)
            {
                if (!Skills.ContainsKey(skill.SkillId))
                {
                    Skills.Add(skill.SkillId, skill);
                    newSkills.Add(skill);
                }
            }
            if (updateToClient) Client.SendPacket(Skill.UpdateSkills(newSkills));
            Stats.Recalculate(this);
        }

        public bool AddSkill(Skill skill)
        {
            if (Skills.ContainsKey(skill.SkillId))
                return false;
            Skills.Add(skill.SkillId, skill);
            return true;
        }

        public void ClearSkills()
        {
            Skills.Clear();
            Stats.Recalculate(this);
        }

        public List<Skill> GetSkillList()
        {
            return Skills.Values.ToList();
        }

        public void GiveBaseJobSkills(int jobId)
        {
            List<WzCharacterSkill> skills = DataBuffer.GetCharacterSkillListByJob(jobId);
            List<Skill> newSkills = new List<Skill>();
            foreach (WzCharacterSkill skillInfo in skills.Where(x => !x.IsInvisible && !x.HasFixedLevel && !x.IsHyperSkill && x.RequiredLevel <= Level && GetSkillLevel(x.SkillId) == 0 && GetSkillMasterLevel(x.SkillId) == 0 && x.DefaultMastery > 0))
            {
                Skill skill = new Skill(skillInfo.SkillId);
                skill.MasterLevel = skillInfo.DefaultMastery;
                newSkills.Add(skill);
            }

            switch (jobId)
            {
                case JobConstants.LUMINOUS1:
                    newSkills.Add(new Skill(Luminous1.FLASH_SHOWER, 10, 0));
                    newSkills.Add(new Skill(Luminous1.ABYSSAL_DROP, 10, 0));
                    newSkills.Add(new Skill(Luminous1.DARK_AFFINITY, 5, 0));
                    break;
                case JobConstants.PHANTOM1:
                    newSkills.Add(new Skill(PhantomBasics.TO_THE_SKIES, 0, 1));
                    newSkills.Add(new Skill(PhantomBasics.SHROUD_WALK, 0, 1));
                    newSkills.Add(new Skill(PhantomBasics.SKILL_SWIPE, 0, 1));
                    newSkills.Add(new Skill(PhantomBasics.LOADOUT, 0, 1));
                    newSkills.Add(new Skill(PhantomBasics.DEXTEROUS_TRAINING, 0, 1));
                    break;
                case JobConstants.DEMONSLAYER1:
                    newSkills.Add(new Skill(DemonBasics.CURSE_OF_FURY, 1, 1));
                    newSkills.Add(new Skill(DemonBasics.FURY_UNLEASHED, 0, 0));
                    break;
                case JobConstants.DEMONAVENGER1:
                    newSkills.Add(new Skill(DemonBasics.WILD_RAGE, 0, 0));
                    newSkills.Add(new Skill(DemonBasics.BLOOD_PACT, 1, 1));
                    newSkills.Add(new Skill(DemonBasics.EXCEED, 1, 1));
                    newSkills.Add(new Skill(DemonBasics.HYPER_POTION_MASTERY, 3, 0));
                    break;

            }

            AddSkills(newSkills);
        }

        public void AddCooldown(int skillId, uint duration, DateTime? nStartTime = null) //duration in MS
        {
            DateTime startTime = nStartTime ?? DateTime.UtcNow;
            if (!Cooldowns.ContainsKey(skillId))
            {
                Cooldown cd = new Cooldown(duration, startTime);
                cd.CancelSchedule = Scheduler.ScheduleRemoveCooldown(this, skillId, duration);
                Cooldowns.Add(skillId, cd);
                Client.SendPacket(Skill.ShowCooldown(skillId, duration / 1000));
            }
        }

        public void AddCooldownSilent(int skillId, uint duration, DateTime? nStartTime = null, bool createCancelSchedule = true) //duration in MS
        {
            DateTime startTime = nStartTime ?? DateTime.UtcNow;
            if (!Cooldowns.ContainsKey(skillId))
            {
                Cooldown cd = new Cooldown(duration, startTime);
                if (createCancelSchedule)
                    cd.CancelSchedule = Scheduler.ScheduleRemoveCooldown(this, skillId, duration);
                Cooldowns.Add(skillId, cd);
            }
        }

        public bool HasSkillOnCooldown(int skillId)
        {
            Cooldown cooldown;
            if (Cooldowns.TryGetValue(skillId, out cooldown))
            {
                if (cooldown.StartTime.AddMilliseconds(cooldown.Duration) > DateTime.UtcNow)
                    return true;
                else
                {
                    Cooldowns.Remove(skillId);
                    return false;
                }
            }
            else
                return false;
        }

        public void RemoveCooldown(int skillId)
        {
            if (Cooldowns.Remove(skillId))
                Client.SendPacket(Skill.ShowCooldown(skillId, 0));
        }

        public void ChangeKeybind(uint key, byte type, int action)
        {
            Keybinds.Remove(key);
            if (type != 0)
                Keybinds.Add(key, new Pair<byte, int>(type, action));
            KeybindsChanged = true;
        }

        public void SetQuickSlotKeys(int[] newMap)
        {
            QuickSlotKeys = newMap;
            QuickSlotKeyBindsChanged = true;
        }

        public void SetKeyMap(Dictionary<uint, Pair<byte, int>> newBinds)
        {
            Keybinds = newBinds;
            KeybindsChanged = true;
        }
        #endregion

        #region Buffs
        public void GiveBuff(Buff buff)
        {
            CancelBuffSilent(buff.SkillId);
            Buffs.Add(buff.SkillId, buff);
            switch (buff.SkillId)
            {
                case Spearman.EVIL_EYE:
                case Berserker.EVIL_EYE_OF_DOMINATION:
                    Client.SendPacket(Buff.GiveEvilEyeBuff(buff));
                    break;
                case Berserker.CROSS_SURGE:
                    Client.SendPacket(Buff.GiveCrossSurgeBuff(buff, this, buff.Effect));
                    break;
                case DarkKnight.FINAL_PACT2:
                    Client.SendPacket(Buff.GiveFinalPactBuff(buff));
                    break;
                case Priest.HOLY_MAGIC_SHELL:
                    buff.Stacks = buff.Effect.Info[CharacterSkillStat.x];
                    AddCooldownSilent(Priest.HOLY_MAGIC_SHELL + 1000, buff.Duration, buff.StartTime, false); //hackish
                    Client.SendPacket(Buff.GiveBuff(buff));
                    break;
                case LuminousBasics.SUNFIRE:
                case LuminousBasics.ECLIPSE:
                case LuminousBasics.EQUILIBRIUM2:
                    LuminousSystem resource = (LuminousSystem)Resource;
                    Client.SendPacket(Buff.GiveLuminousStateBuff(buff, resource.LightGauge, resource.DarkGauge, resource.LightLevel, resource.DarkLevel));
                    //TODO: broadcast to map
                    break;
                default:
                    Client.SendPacket(Buff.GiveBuff(buff));
                    //TODO: broadcast to map
                    break;
            }
            Stats.Recalculate(this);
        }

        public void GiveBuffSilent(Buff buff) //Doesn't send the buff packet to the player
        {
            int skillId = buff.SkillId;
            CancelBuffSilent(skillId);
            Buffs.Add(skillId, buff);
            Stats.Recalculate(this);
        }

        public Buff CancelBuff(int skillId)
        {
            Buff buff;
            if (!Buffs.TryGetValue(skillId, out buff)) return null;
            Buffs.Remove(skillId);
            buff.CancelRemoveBuffSchedule();
            Client.SendPacket(Buff.CancelBuff(buff));
            Stats.Recalculate(this);

            #region Dark Knight Final Pact
            if (buff.SkillId == DarkKnight.FINAL_PACT2)
            {
                if (buff.Stacks > 0) //didn't kill enough mobs
                {
                    AddHP(-Hp);
                }
            }
            #endregion

            return buff;
        }

        public void CancelBuffs(List<int> skillIds)
        {
            bool removed = false;
            foreach (int i in skillIds)
            {
                Buff buff;
                if (!Buffs.TryGetValue(i, out buff)) continue;
                removed = true;
                Buffs.Remove(i);
                buff.CancelRemoveBuffSchedule();
                Client.SendPacket(Buff.CancelBuff(buff));
            }
            if (removed)
                Stats.Recalculate(this);
        }

        public Buff CancelBuffSilent(int skillId) //doesnt update client and doesnt recalculate stats
        {
            Buff buff;
            if (!Buffs.TryGetValue(skillId, out buff)) return null;
            Buffs.Remove(skillId);
            buff.CancelRemoveBuffSchedule();
            return buff;
        }

        public void CancelBuffsSilent(List<int> skillIds) //doesnt update client and doesnt recalculate stats
        {
            foreach (int i in skillIds)
            {
                Buff buff;
                if (!Buffs.TryGetValue(i, out buff)) continue;
                Buffs.Remove(i);
                buff.CancelRemoveBuffSchedule();
            }
        }

        public bool HasBuff(int skillId)
        {
            return Buffs.ContainsKey(skillId);
        }

        public bool HasBuffStat(BuffStat buffStat)
        {
            foreach (Buff buff in Buffs.Values.ToList())
            {
                if (buff.Effect.BuffInfo.ContainsKey(buffStat))
                {
                    return true;
                }
            }
            return false;
        }

        public Buff GetBuff(int skillId)
        {
            Buff ret;
            if (Buffs.TryGetValue(skillId, out ret))
                return ret;
            return null;
        }

        public List<Buff> GetBuffs()
        {
            return Buffs.Values.ToList();
        }
        #endregion

        #region Summons
        public bool HasActiveSummon(int sourceSkillId)
        {
            return Summons.ContainsKey(sourceSkillId);
        }

        public void AddSummon(MapleSummon summon)
        {
            Summons.Add(summon.SourceSkillId, summon);
            Map.AddSummon(summon, true);
        }

        public bool RemoveSummon(int skillId)
        {
            MapleSummon summon;
            if (!Summons.TryGetValue(skillId, out summon)) return false;
            Summons.Remove(summon.SourceSkillId);
            Map.RemoveSummon(summon.ObjectId, true);
            summon.Dispose();
            CancelBuff(skillId);
            return true;
        }

        public MapleSummon GetSummon(int skillId)
        {
            MapleSummon ret;
            return Summons.TryGetValue(skillId, out ret) ? ret : null;
        }

        public List<MapleSummon> GetSummons()
        {
            return Summons.Values.ToList();
        }
        #endregion

        #region Stats
        public void AddHP(int hpInc, bool updateToClient = true, bool healIfDead = false)
        {
            lock (HpLock)
            {
                if (IsDead && !healIfDead) return;

                int oldHp = Hp;
                int newHp = Hp + hpInc;
                if (newHp > Stats.MaxHp)
                    Hp = Stats.MaxHp;
                else if (newHp <= 0)
                {
                    Hp = 0;
                    if (oldHp > Hp)
                        HandleDeath();
                }
                else
                    Hp = newHp;

                if (Job == JobConstants.DARKKNIGHT)
                    FinalPactHook(oldHp, newHp);

                if (updateToClient)
                    UpdateSingleStat(Client, MapleCharacterStat.Hp, Hp);

                if (Party != null)
                {
                    var members = Party.GetCharactersOnMap(Map, Id);
                    if (members.Any())
                    {
                        PacketWriter partyHpUpdatePacket = MapleParty.Packets.UpdatePartyMemberHp(this);
                        foreach (MapleCharacter member in members)
                        {
                            member.Client.SendPacket(partyHpUpdatePacket);
                        }
                    }
                }
            }
        }

        public void HandleDeath()
        {
            ActionState = ActionState.Dead;
            if (Job == JobConstants.DARKKNIGHT && !Cooldowns.ContainsKey(DarkKnight.FINAL_PACT2))
            {
                byte skillLevel = GetSkillLevel(DarkKnight.FINAL_PACT);
                if (skillLevel > 0)
                {
                    SkillEffect effect = DataBuffer.GetCharacterSkillById(DarkKnight.FINAL_PACT2).GetEffect(skillLevel);
                    AddCooldown(DarkKnight.FINAL_PACT2, (uint)effect.Info[CharacterSkillStat.cooltime] * 1000);
                    Buff buff = new Buff(DarkKnight.FINAL_PACT2, effect, (uint)effect.Info[CharacterSkillStat.time] * 1000, this);
                    buff.Stacks = effect.Info[CharacterSkillStat.z];
                    GiveBuff(buff);
                    Client.SendPacket(Skill.ShowBuffEffect(DarkKnight.FINAL_PACT2, Level, null, true));
                    ActionState = ActionState.Enabled;
                    Hp = Stats.MaxHp;
                    AddMP(Stats.MaxMp);
                    return;
                }
            }
            foreach (Buff buff in Buffs.Values.ToList())
            {
                CancelBuffSilent(buff.SkillId);
            }
            foreach (MapleSummon summon in Summons.Values.ToList())
            {
                RemoveSummon(summon.SourceSkillId);
            }
            Stats.Recalculate(this);
        }

        public void AddMP(int mpInc, bool updateToClient = true)
        {
            int buffedMaxMP = Stats.MaxMp;
            if (Mp + mpInc > buffedMaxMP)
                Mp = buffedMaxMP;
            else if (Mp + mpInc < 0)
                Mp = 0;
            else
                Mp += mpInc;
            if (updateToClient)
                UpdateSingleStat(Client, MapleCharacterStat.Mp, Mp, true);
        }

        private void FinalPactHook(int oldHp, int newHp)
        {
            byte skillLevel = GetSkillLevel(DarkKnight.FINAL_PACT);
            if (skillLevel > 0)
            {
                SkillEffect passiveEffect = DataBuffer.GetCharacterSkillById(DarkKnight.FINAL_PACT).GetEffect(skillLevel);
                int boundary = passiveEffect.Info[CharacterSkillStat.x];
                int oldPercent = (int)((oldHp / (double)Stats.MaxHp) * 100);
                int newPercent = (int)((newHp / (double)Stats.MaxHp) * 100);
                if (oldPercent < boundary && newPercent >= boundary) //hp went from under to above the boundary
                    Client.SendPacket(Skill.ShowBuffEffect(DarkKnight.FINAL_PACT, Level, skillLevel, true)); //TODO: show other ppl (dont know packet)
                else if (oldPercent >= boundary && newPercent < boundary) //hp went from above to under the boundary
                    Client.SendPacket(Skill.ShowBuffEffect(DarkKnight.FINAL_PACT, Level, skillLevel, false)); //TODO: show other ppl (dont know packet)               
            }
        }

        public void DoDeathExpLosePenalty()
        {
            long totalNeededExp = GameConstants.GetCharacterExpNeeded(Level);
            long loss = (long)(totalNeededExp * 0.1);
            if (Stats.ExpLossReductionR > 0)
                loss -= (long)(loss * (Stats.ExpLossReductionR / 100.0));
            Exp -= loss;
            Exp = Math.Max(0, Exp);
            UpdateSingleStat(Client, MapleCharacterStat.Exp, Exp);
        }

        public void AddTraitExp(int amount, MapleCharacterStat type)
        {
            Client.SendPacket(ShowGainMapleCharacterStat(amount, type));
            switch (type)
            {
                case MapleCharacterStat.Charisma:
                    Charisma += amount;
                    amount = Charisma;
                    break;
                case MapleCharacterStat.Charm:
                    Charm += amount;
                    amount = Charm;
                    break;
                case MapleCharacterStat.Craft:
                    Craft += amount;
                    amount = Craft;
                    break;
                case MapleCharacterStat.Sense:
                    Sense += amount;
                    amount = Sense;
                    break;
                case MapleCharacterStat.Insight:
                    Insight += amount;
                    amount = Insight;
                    break;
                case MapleCharacterStat.Will:
                    Will += amount;
                    amount = Will;
                    break;
            }
            UpdateSingleStat(Client, type, amount, false);
        }

        public void AddFame(int amount)
        {
            Fame += amount;
            UpdateSingleStat(Client, MapleCharacterStat.Fame, amount);
        }
        #endregion

        #region NPC
        public void OpenNpc(int npcId)
        {
            this.Client.NpcEngine?.Dispose();
            NpcEngine.OpenNpc(npcId, -1, Client);
        }
        #endregion

        public void UpdateResourceSystem()
        {
            if (IsArcher) Resource = ResourceSystem.CheckAndSaveResourceSystem(Resource, ResourceSystemType.Hunter, Id);
            else if (IsBandit) Resource = ResourceSystem.CheckAndSaveResourceSystem(Resource, ResourceSystemType.Bandit, Id);
            else if (IsAran) Resource = ResourceSystem.CheckAndSaveResourceSystem(Resource, ResourceSystemType.Aran, Id);
            else if (IsPhantom) Resource = ResourceSystem.CheckAndSaveResourceSystem(Resource, ResourceSystemType.Phantom, Id);
            else if (IsLuminous) Resource = ResourceSystem.CheckAndSaveResourceSystem(Resource, ResourceSystemType.Luminous, Id);
        }

        public void Update()
        {
            if (IsBandit)
            {
                IncreaseCriticalGrowth(false);
            }
        }

        public void IncreaseCriticalGrowth(bool fromAttack)
        {
            Buff criticalGrowth = GetBuff(Bandit.CRITICAL_GROWTH);
            if (criticalGrowth != null)
            {
                if (Stats.CritRate + criticalGrowth.Stacks < 100)
                {
                    byte primeCriticalLevel = GetSkillLevel(Shadower.PRIME_CRITICAL);
                    int critInc = 2;
                    if (primeCriticalLevel > 0)
                        critInc = DataBuffer.GetCharacterSkillById(Shadower.PRIME_CRITICAL).GetEffect(primeCriticalLevel).Info[CharacterSkillStat.x];
                    criticalGrowth.Stacks += critInc;
                    if (criticalGrowth.Stacks > 100)
                        criticalGrowth.Stacks = 100;
                    Client.SendPacket(Buff.GiveBuff(criticalGrowth));
                }
                else if (fromAttack)
                {
                    criticalGrowth.Stacks = 1;
                    Client.SendPacket(Buff.GiveBuff(criticalGrowth));
                }
            }
            else
            {
                SkillEffect effect = DataBuffer.GetCharacterSkillById(Bandit.CRITICAL_GROWTH).GetEffect(1);
                criticalGrowth = new Buff(Bandit.CRITICAL_GROWTH, effect, SkillEffect.MAX_BUFF_TIME_MS, this);
                criticalGrowth.Stacks = 2;
                GiveBuff(criticalGrowth);
            }
        }

        public void LoggedIn()
        {
            Client.SendPacket(ShowTitles());
            Client.SendPacket(ShowKeybindLayout(Keybinds));
            Client.SendPacket(ShowQuickSlotKeys(QuickSlotKeys));
            Client.SendPacket(SkillMacro.Packets.ShowSkillMacros(SkillMacros));

            if (IsLuminous)
                Client.SendPacket(((LuminousSystem)Resource).Update());

            Guild = MapleGuild.FindGuild(GuildId);
            Guild?.UpdateGuildData();
            Party = MapleParty.FindParty(Id);
            Party?.UpdateParty();
            BuddyList.NotifyChannelChangeToBuddies(Id, AccountId, Name, Client.Channel, Client, true);
        }

        public void LoggedOut()
        {
            Guild?.UpdateGuildData();
            if (Party != null)
            {
                Party.CacheCharacterInfo(this);
                Party.UpdateParty();
            }
            BuddyList.NotifyChannelChangeToBuddies(Id, AccountId, Name, -1);
        }

        #region Map functions
        public int? GetSavedLocation(string script)
        {
            string data = GetCustomQuestData(CustomQuestKeys.SAVED_LOCATION + script);
            if (string.IsNullOrEmpty(data))
                return null;
            return int.Parse(data);
        }

        public void SaveLocation(string script, int value)
        {
            string data = value.ToString();
            if (value == -1)
                data = null;
            SetCustomQuestData(CustomQuestKeys.SAVED_LOCATION + script, data);
        }

        public void ChangeMap(int mapId, string toPortal = "")
        {
            MapleMap map = Program.GetChannelServer(Client.Channel).GetMap(mapId);
            if (map != null)
            {
                ChangeMap(map, toPortal);
            }
        }

        public void ChangeMap(MapleMap toMap, string toPortal = "", bool fromSpecialPortal = false)
        {
            if (toMap == null) return;
            var portal = string.IsNullOrEmpty(toPortal) ? toMap.GetDefaultSpawnPortal() : toMap.GetPortal(toPortal);
            if (portal == null) return;
            MapleMap oldMap = Map;

            Map = toMap;
            MapId = Map.MapId;

            oldMap.RemoveCharacter(Id);
            Position = portal.Position;
            EnterMap(Client, toMap.MapId, portal.Id, fromSpecialPortal);
            toMap.AddCharacter(this);
            ActionState = ActionState.Enabled;
        }

        public void Revive(bool returnToCity = false, bool loseExp = true, bool restoreHpToFull = false)
        {
            if (loseExp)
                DoDeathExpLosePenalty();
            Hp = 49;
            AddHP(restoreHpToFull ? Stats.MaxHp : 1);
            if (returnToCity)
                ChangeMap(Map.ReturnMap);
            else
                EnableActions();
        }

        public void FakeRelog()
        {
            MapleMap currentMap = Map;
            currentMap.RemoveCharacter(this.Id);
            EnterChannel(Client);
            currentMap.AddCharacter(this);
        }
        #endregion

        #region Quests
        public MapleQuest ForfeitQuest(int questId)
        {
            MapleQuest quest = null;
            if (StartedQuests.TryGetValue(questId, out quest))
            {
                StartedQuests.Remove(questId);
                quest.Forfeit();
                Client.SendPacket(quest.Update());
            }
            return quest;
        }

        public bool CompleteQuest(int questId, int npcId, int choice = 0)
        {
            MapleQuest quest = null;
            if (StartedQuests.TryGetValue(questId, out quest))
            {
                if (Map == null || !Map.HasNpc(npcId)) return false;
                foreach (WzQuestRequirement wqr in quest.QuestInfo.FinishRequirements)
                {
                    if (!wqr.Check(this, npcId, quest))
                        return false;
                }
                ForceCompleteQuest(quest, npcId);
                foreach (WzQuestAction wqa in quest.QuestInfo.FinishActions)
                {
                    wqa.Act(this, questId);
                }
                return true;
            }
            return false;
        }

        public bool HasCompletedQuest(int questId)
        {
            return CompletedQuests.ContainsKey(questId);
        }

        public int CompletedQuestCount
        {
            get
            {
                return CompletedQuests.Count;
            }
        }

        public bool HasQuestInProgress(int questId)
        {
            return StartedQuests.ContainsKey(questId);
        }

        public MapleQuest GetQuest(ushort questId)
        {
            MapleQuest ret;
            if (StartedQuests.TryGetValue(questId, out ret))
                return ret;
            return null;
        }

        public bool StartQuest(int questId, int npcId)
        {
            if (!StartedQuests.ContainsKey(questId) && !CompletedQuests.ContainsKey(questId))
            {
                if (Map == null || !Map.HasNpc(npcId)) return false;
                WzQuest info = DataBuffer.GetQuestById((ushort)questId);
                if (info == null) return false;
                MapleQuest quest = new MapleQuest(info);
                foreach (WzQuestRequirement wqr in info.StartRequirements)
                {
                    if (!wqr.Check(this, npcId, quest))
                        return false;
                }
                foreach (WzQuestAction wqa in quest.QuestInfo.StartActions)
                {
                    wqa.Act(this, questId);
                }
                StartedQuests.Add(questId, quest);
                Client.SendPacket(quest.Update());
            }
            return false;
        }

        public void AddQuest(MapleQuest quest, ushort questId)
        {
            if (!StartedQuests.ContainsKey(questId))
            {
                StartedQuests.Add(questId, quest);
                Client.SendPacket(quest.Update());
            }
        }

        public bool ForceCompleteQuest(MapleQuest quest, int npcId, int nextQuest = 0)
        {
            int questId = quest.QuestInfo.Id;
            if (StartedQuests.ContainsKey(questId))
            {
                StartedQuests.Remove(questId);
                quest.State = MapleQuestStatus.Completed;
                if (!CompletedQuests.ContainsKey(questId))
                    CompletedQuests.Add(questId, 0x4E35FF7B); //TODO: real date, some korean thing in minutes I think
                Client.SendPacket(quest.Update());
                Client.SendPacket(quest.UpdateFinish(npcId, nextQuest));
                return true;
            }
            else if (!CompletedQuests.ContainsKey(questId))
            {
                CompletedQuests.Add(questId, 0x4E35FF7B); //TODO: real date, some korean thing in minutes I think
                Client.SendPacket(quest.Update());
                Client.SendPacket(quest.UpdateFinish(npcId, nextQuest));
            }
            return false;
        }

        public void UpdateQuestKills(int mobId)
        {
            foreach (var quest in StartedQuests)
                quest.Value.KilledMob(Client, mobId);
        }

        public void SetQuestData(ushort questId, string data)
        {
            MapleQuest quest;
            if (StartedQuests.TryGetValue(questId, out quest))
            {
                quest.Data = data;
            }
            else
            {
                WzQuest info = DataBuffer.GetQuestById(questId);
                if (info == null) return;
                quest = new MapleQuest(info, MapleQuestStatus.InProgress, data);
                StartedQuests.Add(questId, quest);
            }
        }

        public string GetQuestData(ushort questId)
        {
            MapleQuest quest;
            return StartedQuests.TryGetValue(questId, out quest) ? quest.Data : null;
        }

        public string GetCustomQuestData(string customQuestKey)
        {
            string data;
            return CustomQuestData.TryGetValue(customQuestKey, out data) ? data : string.Empty;
        }

        //Removes the custom quest from the collection if data is null or empty
        public void SetCustomQuestData(string customQuestKey, string data)
        {
            if (string.IsNullOrEmpty(data))
            {
                CustomQuestData.Remove(customQuestKey);
                return;
            }
            if (CustomQuestData.ContainsKey(customQuestKey))
                CustomQuestData[customQuestKey] = data;
            else
                CustomQuestData.Add(customQuestKey, data);
        }
        #endregion

        public void Release(bool hasMigration = true)
        {
            Inventory?.Release();
            if (Map != null)
            {
                Map.RemoveCharacter(Id);
                Map = null;
            }
            if (ChatRoom != null)
            {
                ChatRoom.RemovePlayer(Id);
                ChatRoom = null;
            }
            if (!hasMigration)
            {
                if (Buffs.ContainsKey(DarkKnight.FINAL_PACT2)) //people logging out while having final pact :3
                {
                    Hp = 0;
                    Mp = 0;
                }
                foreach (Buff buff in Buffs.Values.ToList())
                    buff.CancelRemoveBuffSchedule();
                foreach (MapleSummon summon in Summons.Values.ToList())
                    summon.Dispose();
            }
            Client = null;
        }

        public void Bind(MapleClient c)
        {
            Client = c;
            Inventory.Bind(this);
            ActionState = ActionState.Enabled;
        }

        #region Messages
        public void SendMessage(string message, byte type = 0)
        {
            Client.SendPacket(ServerNotice(message, type));
        }

        public void SendBlueMessage(string message)
        {
            Client.SendPacket(ServerNotice(message, 6));
        }

        public void SendPopUpMessage(string message)
        {
            Client.SendPacket(ServerNotice(message, 1));
        }

        public void SendWhiteMessage(string message)
        {
            Client.SendPacket(SystemMessage(message, 0x0B));
        }

        #endregion

        #region Database
        public static bool CharacterExists(string name)
        {
            using (LeattyContext DBContext = new LeattyContext())
            {
                Character DbChar = DBContext.Characters.FirstOrDefault(x => x.Name.ToLower().Equals(name.ToLower()));
                return DbChar != null;
            }
        }

        public static MapleCharacter ConvertCharacterToMapleCharacter(Character dbChar)
        {
            return new MapleCharacter()
            {
                Id = dbChar.Id,
                Exp = dbChar.Exp,
                Mesos = dbChar.Mesos,
                AccountId = dbChar.AccountId,
                MapId = dbChar.MapId,
                GuildId = dbChar.GuildId,
                Hp = dbChar.Hp,
                Mp = dbChar.Mp,
                MaxHp = dbChar.MaxHp,
                MaxMp = dbChar.MaxMp,
                Fame = dbChar.Fame,
                Hair = dbChar.Hair,
                Face = dbChar.Face,
                FaceMark = dbChar.FaceMark,
                TamerEars = dbChar.TamerEars,
                TamerTail = dbChar.TamerTail,
                GuildContribution = dbChar.GuildContribution,
                Job = dbChar.Job,
                SubJob = dbChar.SubJob,
                Str = dbChar.Str,
                Dex = dbChar.Dex,
                Int = dbChar.Int,
                Luk = dbChar.Luk,
                AP = dbChar.AP,
                BuddyCapacity = dbChar.BuddyCapacity,
                Level = dbChar.Level,
                Gender = dbChar.Gender,
                Skin = dbChar.Skin,
                SpawnPoint = dbChar.SpawnPoint,
                GuildRank = dbChar.GuildRank,
                AllianceRank = dbChar.AllianceRank,
                Name = dbChar.Name,
                Sp = dbChar.Sp,

                Charisma = dbChar.Charisma,
                Charm = dbChar.Charm,
                Craft = dbChar.Craft,
                Insight = dbChar.Insight,
                Sense = dbChar.Sense,
                Will = dbChar.Will,
                Fatigue = dbChar.Fatigue,
            };
        }

        public bool CreateGuild(string guildName)
        {
            if (this.Guild != null)
                return false;
            MapleGuild guild = MapleGuild.CreateGuild(guildName, this);
            if (guild == null)
                return false;

            this.Guild = guild;
            this.GuildRank = 1;
            this.GuildContribution = 500;
            this.AllianceRank = 5;
            SaveToDatabase(this);
            Client.SendPacket(guild.GenerateGuildDataPacket());
            MapleGuild.UpdateCharacterGuild(this, guildName);
            return true;
        }

        public int InsertCharacter()
        {
            Character InsertChar = new Character();
            InsertChar.Name = Name;
            InsertChar.AccountId = AccountId;
            InsertChar.Job = Job;
            InsertChar.SubJob = SubJob;
            InsertChar.Level = Level;
            InsertChar.Str = Str;
            InsertChar.Dex = Dex;
            InsertChar.Int = Int;
            InsertChar.Luk = Luk;
            InsertChar.AP = AP;
            InsertChar.Hair = Hair;
            InsertChar.Face = Face;
            InsertChar.Hp = Hp;
            InsertChar.MaxHp = MaxHp;
            InsertChar.Mp = Mp;
            InsertChar.MaxMp = MaxMp;
            InsertChar.Exp = Exp;
            InsertChar.Gender = Gender;
            InsertChar.Skin = Skin;
            InsertChar.FaceMark = FaceMark;
            InsertChar.TamerEars = TamerEars;
            InsertChar.TamerTail = TamerTail;
            InsertChar.MapId = 140090000; //Ice Cave
            InsertChar.BuddyCapacity = 50;
            string spString = "";
            for (int i = 0; i < 10; i++)
            {
                spString += SpTable[i].ToString();
                if (i != 9)
                    spString += ",";
            }
            InsertChar.Sp = spString;
            using (LeattyContext DBContext = new LeattyContext())
            {
                DBContext.Characters.Add(InsertChar);
                DBContext.SaveChanges();
                this.Id = InsertChar.Id;

                #region Keybinds
                foreach (var kvp in Keybinds)
                {
                    KeyMap insertKeyMap = new KeyMap();

                    insertKeyMap.CharacterId = Id;
                    insertKeyMap.Key = (byte)kvp.Key; //Posible overflow?
                    insertKeyMap.Type = kvp.Value.Left;
                    insertKeyMap.Action = kvp.Value.Right;

                    DBContext.KeyMaps.Add(insertKeyMap);
                }
                for (int i = 0; i < QuickSlotKeys.Length; i++)
                {
                    QuickSlotKeyMap dbQuickSlotKeyMap = new QuickSlotKeyMap();
                    dbQuickSlotKeyMap.CharacterId = InsertChar.Id;
                    dbQuickSlotKeyMap.Key = QuickSlotKeys[i];
                    dbQuickSlotKeyMap.Index = (byte)i;
                    DBContext.QuickSlotKeyMaps.Add(dbQuickSlotKeyMap);
                }
                #endregion

                #region skills
                foreach (Skill skill in Skills.Values)
                {
                    DB.Models.Skill InsertSkill = new DB.Models.Skill();
                    InsertSkill.SkillId = skill.SkillId;
                    InsertSkill.CharacterId = Id;
                    InsertSkill.Level = skill.Level;
                    InsertSkill.MasterLevel = skill.MasterLevel;
                    InsertSkill.Expiration = skill.Expiration;

                    DBContext.Skills.Add(InsertSkill);
                }
                #endregion
                DBContext.SaveChanges();
            }

            return Id;
        }

        public static MapleCharacter LoadFromDatabase(int characterId, bool characterScreen, MapleClient c = null)
        {
            lock (CharacterDatabaseLock)
            {
                using (LeattyContext dbContext = new LeattyContext())
                {
                    Character dbChar = dbContext.Characters.SingleOrDefault(x => x.Id == characterId);
                    if (dbChar == null)
                        return null;
                    MapleCharacter chr = ConvertCharacterToMapleCharacter(dbChar);
                    if (c != null)
                    {
                        chr.Client = c;
                    }

                    string[] splitSp = dbChar.Sp.Split(',');
                    int[] spTable = new int[10];
                    for (int i = 0; i < splitSp.Length; i++)
                    {
                        spTable[i] = int.Parse(splitSp[i]);
                    }
                    chr.SpTable = spTable;

                    chr.Inventory = MapleInventory.LoadFromDatabase(chr);

                    foreach (QuestCustomData customQuest in dbContext.QuestCustomData.Where(x => x.CharacterId == characterId))
                    {
                        if (!chr.CustomQuestData.ContainsKey(customQuest.Key))
                            chr.CustomQuestData.Add(customQuest.Key, customQuest.Value);
                    }

                    if (characterScreen) return chr; //No need to load more

                    #region Blessing of fairy and empress
                    chr.EmpressBlessingOrigin = "";
                    chr.FairyBlessingOrigin = "";
                    var myOtherCharacters = dbContext.Characters.Where(x => x.AccountId == chr.AccountId && x.Id != characterId);
                    Character fairyChr = myOtherCharacters.OrderByDescending(x => x.Level).FirstOrDefault();
                    Character empressChr = myOtherCharacters.Where(x => (x.Job / 1000 == 1) || x.Job / 1000 == 5).OrderByDescending(x => x.Level).FirstOrDefault();
                    if (fairyChr != null)
                        chr.FairyBlessingOrigin = fairyChr.Name;
                    if (empressChr != null)
                        chr.EmpressBlessingOrigin = empressChr.Name;
                    #endregion

                    #region Buddies
                    chr.BuddyList = MapleBuddyList.LoadFromDatabase(dbContext.Buddies.Where(x => x.AccountId == chr.AccountId || x.CharacterId == characterId).ToList(), chr.BuddyCapacity);
                    #endregion

                    #region Skills
                    var dbSkills = dbContext.Skills.Where(x => x.CharacterId == characterId);
                    Dictionary<int, Skill> skills = new Dictionary<int, Skill>();
                    foreach (DB.Models.Skill DbSkill in dbSkills)
                    {
                        Skill skill = new Skill(DbSkill.SkillId)
                        {
                            Level = DbSkill.Level,
                            MasterLevel = DbSkill.MasterLevel,
                            Expiration = DbSkill.Expiration,
                            SkillExp = DbSkill.SkillExp
                        };
                        if (!skills.ContainsKey(skill.SkillId))
                            skills.Add(skill.SkillId, skill);
                    }
                    chr.SetSkills(skills);
                    #endregion

                    chr.UpdateResourceSystem();

                    #region Keybinds
                    List<KeyMap> dbKeyMaps = dbContext.KeyMaps.Where(x => x.CharacterId == characterId).ToList();
                    Dictionary<uint, Pair<byte, int>> keyMap = new Dictionary<uint, Pair<byte, int>>();
                    foreach (KeyMap dbKeyMap in dbKeyMaps)
                    {
                        if (!keyMap.ContainsKey(dbKeyMap.Key))
                        {
                            keyMap.Add(dbKeyMap.Key, new Pair<byte, int>(dbKeyMap.Type, dbKeyMap.Action));
                        }
                    }
                    chr.Keybinds = keyMap;

                    List<QuickSlotKeyMap> dbQuickSlotKeyMaps = dbContext.QuickSlotKeyMaps.Where(x => x.CharacterId == characterId).ToList();
                    int[] quickSlots = new int[28];
                    foreach (QuickSlotKeyMap dbQuickSlotKeyMap in dbQuickSlotKeyMaps)
                    {
                        quickSlots[dbQuickSlotKeyMap.Index] = dbQuickSlotKeyMap.Key;
                    }
                    chr.QuickSlotKeys = quickSlots;

                    List<DbSkillMacro> dbSkillMacros = dbContext.SkillMacros.Where(x => x.CharacterId == characterId).ToList();
                    foreach (DbSkillMacro dbSkillMacro in dbSkillMacros)
                    {
                        chr.SkillMacros[dbSkillMacro.Index] = new SkillMacro(dbSkillMacro.Name, dbSkillMacro.ShoutName, dbSkillMacro.Skill1, dbSkillMacro.Skill2, dbSkillMacro.Skill3);
                    }

                    #endregion

                    #region Cooldowns
                    List<SkillCooldown> DbSkillCooldowns = dbContext.SkillCooldowns.Where(x => x.CharacterId == characterId).ToList();
                    foreach (SkillCooldown DbSkillCooldown in DbSkillCooldowns)
                    {
                        if (!chr.Cooldowns.ContainsKey(DbSkillCooldown.SkillId))
                        {
                            DateTime startTime = new DateTime(DbSkillCooldown.StartTime);
                            uint duration = (uint)DbSkillCooldown.Length;
                            uint remaining = (uint)(startTime.AddMilliseconds(duration) - DateTime.UtcNow).TotalMilliseconds;
                            if (remaining <= 2000) // less than 2 seconds is not worth the effort
                                continue;
                            chr.AddCooldownSilent(DbSkillCooldown.SkillId, duration, startTime);
                        }
                    }
                    #endregion

                    #region Quests
                    List<QuestStatus> DbQuestStatuses = dbContext.QuestStatus.Where(x => x.CharacterId == characterId).ToList();
                    foreach (QuestStatus DbQuestStatus in DbQuestStatuses)
                    {
                        MapleQuestStatus status = (MapleQuestStatus)DbQuestStatus.Status;
                        int questId = DbQuestStatus.Quest;
                        if (status == MapleQuestStatus.InProgress)
                        {
                            WzQuest info = DataBuffer.GetQuestById((ushort)questId);
                            if (info != null)
                            {
                                string data = DbQuestStatus.CustomData ?? "";
                                MapleQuest quest = null;
                                if (info.FinishRequirements.Where(x => x.Type == QuestRequirementType.mob).Any())
                                {
                                    List<QuestMobStatus> DbQuestStatusesMobs = dbContext.QuestStatusMobs.Where(x => x.QuestStatusId == DbQuestStatus.Id).ToList();
                                    Dictionary<int, int> mobs = new Dictionary<int, int>();
                                    foreach (QuestMobStatus DbQuestStatusMobs in DbQuestStatusesMobs)
                                    {
                                        int mobId = DbQuestStatusMobs.Mob;
                                        if (mobId > 0)
                                            mobs.Add(mobId, DbQuestStatusMobs.Count);
                                    }
                                    quest = new MapleQuest(info, status, data, mobs);
                                }
                                else
                                {
                                    quest = new MapleQuest(info, status, data);
                                }
                                chr.StartedQuests.Add(questId, quest);
                            }
                        }
                        else if (status == MapleQuestStatus.Completed)
                        {
                            if (!chr.CompletedQuests.ContainsKey(questId))
                                chr.CompletedQuests.Add(questId, 0x4E35FF7B); //TODO: real date
                        }
                    }
                    #endregion

                    return chr;
                }
            }
        }

        /// <summary>
        /// Saves a character to the database
        /// </summary>
        /// <param name="chr">The character to save</param>
        public static void SaveToDatabase(MapleCharacter chr)
        {
            lock (CharacterDatabaseLock)
            {
                using (LeattyContext dbContext = new LeattyContext())
                {
                    #region Stats
                    chr.GuildId = chr.Guild == null ? 0 : chr.Guild.GuildId;

                    if (chr.Map != null)
                    {
                        int forcedReturn = DataBuffer.GetMapById(chr.Map.MapId).ForcedReturn;
                        if (forcedReturn != 999999999)
                        {
                            chr.MapId = forcedReturn;
                            chr.SpawnPoint = 0;
                        }
                        else
                        {
                            chr.MapId = chr.Map.MapId;
                            chr.SpawnPoint = chr.Map.GetClosestSpawnPointId(chr.Position);
                        }
                    }
                    else
                    {
                        chr.SpawnPoint = 0;
                    }
                    string sp = "";
                    for (int i = 0; i < chr.SpTable.Length; i++)
                    {
                        sp += chr.SpTable[i].ToString();
                        if (i != chr.SpTable.Length - 1)
                            sp += ",";
                    }
                    chr.Sp = sp;
                    Character updateChar = dbContext.Characters.FirstOrDefault(x => x.Id == chr.Id);
                    updateChar.Id = chr.Id;
                    updateChar.Name = chr.Name;
                    updateChar.AccountId = chr.AccountId;
                    updateChar.Job = chr.Job;
                    updateChar.SubJob = chr.SubJob;
                    updateChar.Level = chr.Level;
                    updateChar.Str = chr.Str;
                    updateChar.Dex = chr.Dex;
                    updateChar.Int = chr.Int;
                    updateChar.Luk = chr.Luk;
                    updateChar.AP = chr.AP;
                    updateChar.Hair = chr.Hair;
                    updateChar.Face = chr.Face;
                    updateChar.Hp = chr.Hp;
                    updateChar.MaxHp = chr.MaxHp;
                    updateChar.Mp = chr.Mp;
                    updateChar.MaxMp = chr.MaxMp;
                    updateChar.Exp = chr.Exp;
                    updateChar.Mesos = chr.Mesos;
                    updateChar.Gender = chr.Gender;
                    updateChar.Skin = chr.Skin;
                    updateChar.FaceMark = chr.FaceMark;
                    updateChar.MapId = chr.MapId;
                    updateChar.Sp = chr.Sp;

                    updateChar.Charisma = chr.Charisma;
                    updateChar.Charm = chr.Charm;
                    updateChar.Craft = chr.Craft;
                    updateChar.Insight = chr.Insight;
                    updateChar.Sense = chr.Sense;
                    updateChar.Will = chr.Will;
                    updateChar.Fatigue = chr.Fatigue;

                    updateChar.BuddyCapacity = chr.BuddyCapacity;

                    dbContext.Entry<Character>(updateChar).State = System.Data.Entity.EntityState.Modified;
                    #endregion

                    chr.Inventory.SaveToDatabase();

                    #region Skills
                    List<DB.Models.Skill> DbSkills = dbContext.Skills.Where(x => x.CharacterId == chr.Id).ToList();
                    foreach (DB.Models.Skill DbSkill in DbSkills)
                    {
                        if (!chr.Skills.ContainsKey(DbSkill.SkillId)) //skill was removed                                 
                            dbContext.Skills.Remove(DbSkill);
                    }
                    foreach (Skill skill in chr.Skills.Values)
                    {
                        DB.Models.Skill dbSkill = DbSkills.Where(x => x.SkillId == skill.SkillId).FirstOrDefault();
                        if (dbSkill != null) //Update                   
                        {
                            dbSkill.Level = skill.Level;
                            dbSkill.MasterLevel = skill.MasterLevel;
                            dbSkill.Expiration = skill.Expiration;
                            dbSkill.SkillExp = skill.SkillExp;
                        }
                        else //Insert
                        {
                            DB.Models.Skill InsertSkill = new DB.Models.Skill();
                            InsertSkill.CharacterId = chr.Id;
                            InsertSkill.SkillId = skill.SkillId;
                            InsertSkill.Level = skill.Level;
                            InsertSkill.MasterLevel = skill.MasterLevel;
                            InsertSkill.Expiration = skill.Expiration;
                            InsertSkill.SkillExp = skill.SkillExp;
                            dbContext.Skills.Add(InsertSkill);
                        }
                    }
                    #endregion

                    #region Resource System
                    if (chr.Resource != null)
                        chr.Resource.SaveToDatabase(chr.Id);
                    #endregion

                    #region Keybinds
                    if (chr.KeybindsChanged)
                    {
                        dbContext.KeyMaps.RemoveRange(dbContext.KeyMaps.Where(x => x.CharacterId == chr.Id));
                        foreach (var kvp in chr.Keybinds)
                        {
                            KeyMap insertKeyMap = new KeyMap();
                            insertKeyMap.CharacterId = chr.Id;
                            insertKeyMap.Key = (byte)kvp.Key; //Posible overflow?
                            insertKeyMap.Type = kvp.Value.Left;
                            insertKeyMap.Action = kvp.Value.Right;
                            dbContext.KeyMaps.Add(insertKeyMap);
                        }
                    }
                    if (chr.QuickSlotKeyBindsChanged)
                    {
                        dbContext.QuickSlotKeyMaps.RemoveRange(dbContext.QuickSlotKeyMaps.Where(x => x.CharacterId == chr.Id));
                        for (int i = 0; i < chr.QuickSlotKeys.Length; i++)
                        {
                            int key = chr.QuickSlotKeys[i];
                            if (key > 0)
                            {
                                QuickSlotKeyMap dbQuickSlotKeyMap = new QuickSlotKeyMap();
                                dbQuickSlotKeyMap.CharacterId = chr.Id;
                                dbQuickSlotKeyMap.Key = key;
                                dbQuickSlotKeyMap.Index = (byte)i;
                                dbContext.QuickSlotKeyMaps.Add(dbQuickSlotKeyMap);
                            }
                        }
                    }
                    List<DbSkillMacro> dbSkillMacros = dbContext.SkillMacros.Where(x => x.CharacterId == chr.Id).ToList();
                    foreach (DbSkillMacro dbSkillMacro in dbSkillMacros)
                    {
                        if (chr.SkillMacros[dbSkillMacro.Index] == null)
                            dbContext.SkillMacros.Remove(dbSkillMacro);
                    }
                    for (int i = 0; i < 5; i++)
                    {
                        if (chr.SkillMacros[i] != null && chr.SkillMacros[i].Changed)
                        {
                            SkillMacro macro = chr.SkillMacros[i];
                            DbSkillMacro dbSkillMacro = dbSkillMacros.FirstOrDefault(x => x.Index == i);
                            if (dbSkillMacro != null)
                            {
                                dbSkillMacro.Name = macro.Name;
                                dbSkillMacro.ShoutName = macro.ShoutName;
                                dbSkillMacro.Skill1 = macro.Skills[0];
                                dbSkillMacro.Skill2 = macro.Skills[1];
                                dbSkillMacro.Skill3 = macro.Skills[2];
                            }
                            else
                            {
                                dbSkillMacro = new DbSkillMacro { Index = (byte)i, CharacterId = chr.Id, Name = macro.Name, ShoutName = macro.ShoutName, Skill1 = macro.Skills[0], Skill2 = macro.Skills[1], Skill3 = macro.Skills[2] };
                                dbContext.SkillMacros.Add(dbSkillMacro);
                            }
                            macro.Changed = false;
                        }
                    }
                    #endregion

                    #region Buddies
                    List<MapleBuddy> buddies = chr.BuddyList.GetAllBuddies();
                    var currentDbBuddies = dbContext.Buddies.Where(x => x.AccountId == chr.AccountId || x.CharacterId == chr.Id);
                    //Removed buddies:
                    foreach (Buddy b in currentDbBuddies)
                    {
                        bool accbuddy = b.BuddyAccountId > 0;
                        if (accbuddy)
                        {
                            if (!buddies.Exists(x => x.AccountId == b.BuddyAccountId)) //check if the character's buddlist contains the buddy that is in the database
                                dbContext.Buddies.Remove(b);
                        }
                        else
                        {
                            if (!buddies.Exists(x => x.CharacterId == b.BuddyCharacterId)) //ditto for non-account buddy
                                dbContext.Buddies.Remove(b);
                        }
                    }

                    foreach (MapleBuddy buddy in buddies)
                    {
                        Buddy dbBuddy;
                        if ((dbBuddy = currentDbBuddies.FirstOrDefault(x => x.BuddyAccountId == buddy.AccountId || x.BuddyCharacterId == buddy.CharacterId)) != null)
                        {
                            dbBuddy.Name = buddy.NickName;
                            dbBuddy.Group = buddy.Group;
                            dbBuddy.Memo = buddy.Memo;
                            dbBuddy.IsRequest = buddy.IsRequest;
                        }
                        else
                        {
                            Buddy newBuddy;
                            if (buddy.AccountBuddy)
                            {
                                newBuddy = new Buddy()
                                {
                                    AccountId = chr.AccountId,
                                    BuddyAccountId = buddy.AccountId,
                                    Name = buddy.NickName,
                                    Group = buddy.Group,
                                    Memo = buddy.Memo,
                                    IsRequest = buddy.IsRequest
                                };
                            }
                            else
                            {
                                newBuddy = new Buddy()
                                {
                                    CharacterId = chr.Id,
                                    BuddyCharacterId = buddy.CharacterId,
                                    Name = buddy.NickName,
                                    Group = buddy.Group,
                                    Memo = buddy.Memo,
                                    IsRequest = buddy.IsRequest
                                };
                            }
                            dbContext.Buddies.Add(newBuddy);
                        }
                    }
                    #endregion

                    #region Cooldowns
                    dbContext.SkillCooldowns.RemoveRange(dbContext.SkillCooldowns.Where(x => x.CharacterId == chr.Id));
                    foreach (var kvp in chr.Cooldowns)
                    {
                        if (DateTime.UtcNow <= (kvp.Value.StartTime.AddMilliseconds(kvp.Value.Duration)))
                        {
                            SkillCooldown InserSkillCooldown = new SkillCooldown();
                            InserSkillCooldown.CharacterId = chr.Id;
                            InserSkillCooldown.SkillId = kvp.Key;
                            if (kvp.Value.Duration > int.MaxValue)
                                InserSkillCooldown.Length = int.MaxValue;
                            else
                                InserSkillCooldown.Length = (int)kvp.Value.Duration;
                            InserSkillCooldown.StartTime = kvp.Value.StartTime.Ticks;
                            dbContext.SkillCooldowns.Add(InserSkillCooldown);
                        }
                    }

                    #endregion

                    #region Quests
                    var dbCustomQuestData = dbContext.QuestCustomData.Where(x => x.CharacterId == chr.Id).ToList();
                    foreach (var dbCustomQuest in dbCustomQuestData)
                    {
                        if (chr.CustomQuestData.All(x => x.Key != dbCustomQuest.Key)) //doesn't exist in current chr.CustomQuestData but it does in the DB
                        {
                            dbContext.QuestCustomData.Remove(dbCustomQuest); //Delete it from the DB
                        }
                    }
                    foreach (var kvp in chr.CustomQuestData)
                    {
                        QuestCustomData dbCustomQuest = dbCustomQuestData.FirstOrDefault(x => x.Key == kvp.Key);
                        if (dbCustomQuest != null)
                        {
                            dbCustomQuest.Value = kvp.Value;
                        }
                        else
                        {
                            QuestCustomData newCustomQuest = new QuestCustomData { CharacterId = chr.Id, Key = kvp.Key, Value = kvp.Value };
                            dbContext.QuestCustomData.Add(newCustomQuest);
                        }
                    }
                    List<QuestStatus> databaseQuests = dbContext.QuestStatus.Where(x => x.CharacterId == chr.Id).ToList();
                    List<QuestStatus> startedDatabaseQuests = databaseQuests.Where(x => x.Status == 1).ToList();
                    List<QuestStatus> completedDatabaseQuests = databaseQuests.Where(x => x.Status == 2).ToList();
                    foreach (QuestStatus qs in startedDatabaseQuests)
                    {
                        if (!chr.StartedQuests.ContainsKey(qs.Quest)) //quest in progress was removed or forfeited
                        {
                            dbContext.QuestStatusMobs.RemoveRange(dbContext.QuestStatusMobs.Where(x => x.QuestStatusId == qs.Id));
                            dbContext.QuestStatus.Remove(qs);
                        }
                    }
                    foreach (var questPair in chr.StartedQuests)
                    {
                        MapleQuest quest = questPair.Value;
                        QuestStatus dbQuestStatus = startedDatabaseQuests.FirstOrDefault(x => x.Quest == questPair.Key);
                        if (dbQuestStatus != null) //record exists
                        {
                            dbQuestStatus.CustomData = quest.Data;
                            dbQuestStatus.Status = (byte)quest.State;
                            if (quest.HasMonsterKillObjectives)
                            {
                                List<QuestMobStatus> qmsList = dbContext.QuestStatusMobs.Where(x => x.QuestStatusId == dbQuestStatus.Id).ToList();
                                foreach (var mobPair in quest.MonsterKills)
                                {
                                    QuestMobStatus qms = qmsList.FirstOrDefault(x => x.Mob == mobPair.Key);
                                    if (qms != null) //record exists                                
                                        qms.Count = mobPair.Value;
                                    else //doesnt exist yet, need to insert
                                    {
                                        qms = new QuestMobStatus();
                                        qms.Mob = mobPair.Key;
                                        qms.Count = mobPair.Value;
                                        qms.QuestStatusId = dbQuestStatus.Id;
                                        dbContext.QuestStatusMobs.Add(qms);
                                    }
                                }
                            }
                        }
                        else //doesnt exist yet, need to insert
                        {
                            dbQuestStatus = new QuestStatus();
                            dbQuestStatus.CharacterId = chr.Id;
                            dbQuestStatus.Quest = questPair.Key;
                            dbQuestStatus.Status = (byte)quest.State;
                            dbQuestStatus.CustomData = quest.Data;
                            dbContext.QuestStatus.Add(dbQuestStatus);
                            if (quest.HasMonsterKillObjectives)
                            {
                                dbContext.SaveChanges();
                                foreach (var kvp in quest.MonsterKills)
                                {
                                    QuestMobStatus qms = new QuestMobStatus();
                                    qms.QuestStatusId = dbQuestStatus.Id;
                                    qms.Mob = kvp.Key;
                                    qms.Count = kvp.Value;
                                    dbContext.QuestStatusMobs.Add(qms);
                                }
                            }
                        }
                    }
                    foreach (var questPair in chr.CompletedQuests)
                    {
                        if (!completedDatabaseQuests.Where(x => x.Quest == questPair.Key).Any()) //completed quest isn't in the completed Database yet
                        {
                            QuestStatus qs = databaseQuests.Where(x => x.Quest == questPair.Key).FirstOrDefault();
                            if (qs != null) //quest is in StartedQuests database
                            {
                                dbContext.QuestStatusMobs.RemoveRange(dbContext.QuestStatusMobs.Where(x => x.QuestStatusId == qs.Id));
                                qs.Status = (byte)MapleQuestStatus.Completed;
                                qs.CompleteTime = questPair.Value;
                            }
                            else //not in database yet
                            {
                                qs = new QuestStatus();
                                qs.CharacterId = chr.Id;
                                qs.Quest = questPair.Key;
                                qs.CompleteTime = questPair.Value;
                                qs.Status = (byte)MapleQuestStatus.Completed;
                                dbContext.QuestStatus.Add(qs);
                            }
                        }
                    }
                    #endregion

                    dbContext.SaveChanges();

                    ServerConsole.Info("Character " + chr.Name + " saved to Database!");
                }
            }
        }
        #endregion

        #region Doors
        public bool HasDoor(int skillId)
        {
            lock (Doors)
            {
                List<SpecialPortal> sameSkillPortals = Doors.Where(x => x.SkillId == skillId).ToList();
                int count = sameSkillPortals.Count;
                foreach (SpecialPortal portal in sameSkillPortals)
                {
                    if (DateTime.UtcNow >= portal.Expiration)
                    {
                        portal.FromMap.RemoveStaticObject(portal.ObjectId, false);
                        Doors.Remove(portal);
                        count--;
                    }
                }
                return count > 0;
            }
        }

        public void CancelDoor(int skillId = 0)
        {
            List<SpecialPortal> sameSkillDoors;
            lock (Doors)
            {
                sameSkillDoors = Doors.Where(x => skillId == 0 ? true : x.SkillId == skillId).ToList();
            }
            foreach (SpecialPortal door in sameSkillDoors)
            {
                door.FromMap.RemoveStaticObject(door.ObjectId, false);
            }
        }

        public void RemoveDoor(int skillId)
        {
            lock (Doors)
            {
                Doors.RemoveAll(x => x.SkillId == skillId);
            }
        }

        public void AddDoor(SpecialPortal door)
        {
            lock (Doors)
            {
                Doors.Add(door);
            }
        }
        #endregion

        #region Packets
        public static PacketWriter ShowExpFromMonster(int exp)
        {
            PacketWriter pw = new PacketWriter();
            pw.WriteHeader(SendHeader.ShowStatusInfo);
            pw.WriteByte(3);
            pw.WriteByte(1);
            pw.WriteInt(exp);
            pw.WriteZeroBytes(9);
            return pw;
        }

        public static PacketWriter ShowGainMapleCharacterStat(int amount, MapleCharacterStat stat)
        {
            //11 00 00 00 01 00 00 00 00 05 00 00 00
            PacketWriter pw = new PacketWriter();
            pw.WriteHeader(SendHeader.ShowStatusInfo);
            pw.WriteByte(0x11);
            pw.WriteLong((long)stat);
            pw.WriteInt(amount);
            return pw;
        }

        public static PacketWriter ShowTitles()
        {
            PacketWriter pw = new PacketWriter(SendHeader.ShowTitles);
            pw.WriteHexString("00 00 FF 00 00 FF 00 00 FF 00 00 FF 00 00 FF"); //no titles
            return pw;
        }

        public static void AddCharEntry(PacketWriter pw, MapleCharacter chr)
        {
            AddCharStats(pw, chr);
            AddCharLook(pw, chr, true);
            pw.WriteBool(false);
            pw.WriteBool(false); //Rankings info. We don't show it.
            /* if (ranking) {
                mplew.writeInt(chr.getRank());
                mplew.writeInt(chr.getRankMove());
                mplew.writeInt(chr.getJobRank());
                mplew.writeInt(chr.getJobRankMove());
            }
            if (chr.IsGameMasterJob)
            {
                pw.WriteLong(0);
            }*/
        }

        public static void AddCharStats(PacketWriter pw, MapleCharacter chr, bool CashShop = false)
        {

            pw.WriteInt(chr.Id);
            string name = chr.Name;
            while (name.Length < 13)
            {
                name = name + Convert.ToChar(0x0).ToString();
            }
            pw.WriteStaticString(name);
            pw.WriteByte(chr.Gender);
            pw.WriteByte(chr.Skin);
            pw.WriteInt(chr.Face);
            pw.WriteInt(chr.Hair);

            pw.WriteByte(0xFF); //pets I think 0xFF
            pw.WriteByte(0);
            pw.WriteByte(0);

            pw.WriteByte(chr.Level);
            pw.WriteShort(chr.Job);

            pw.WriteShort(chr.Str);
            pw.WriteShort(chr.Dex);
            pw.WriteShort(chr.Int);
            pw.WriteShort(chr.Luk);

            pw.WriteInt(chr.Hp);
            pw.WriteInt(chr.MaxHp);
            pw.WriteInt(chr.Mp);
            pw.WriteInt(chr.MaxMp);

            pw.WriteShort(chr.AP);
            if (!chr.IsSeparatedSpJob)
                pw.WriteShort((short)chr.SpTable[0]);
            else
                AddSeparatedSP(chr, pw);

            pw.WriteLong(chr.Exp);
            pw.WriteInt(chr.Fame);
            pw.WriteInt(0); //gachexp
            pw.WriteInt(0); //new v142
            int mapId = 0;
            if (chr.Map != null)
                mapId = chr.Map.MapId;
            pw.WriteInt(mapId);
            pw.WriteByte(chr.SpawnPoint);

            pw.WriteInt(0); //?
            pw.WriteShort(chr.SubJob);
            if (chr.IsDemon || chr.IsXenon || chr.IsBeastTamer)
                pw.WriteInt(chr.FaceMark);
            pw.WriteByte(chr.Fatigue); //fatigue
            pw.WriteInt(MapleFormatHelper.GetCurrentDate());

            pw.WriteInt(chr.Charisma);
            pw.WriteInt(chr.Insight);
            pw.WriteInt(chr.Will);
            pw.WriteInt(chr.Craft);
            pw.WriteInt(chr.Sense);
            pw.WriteInt(chr.Charm);

            pw.WriteZeroBytes(13);
            pw.WriteUInt(0xFDE04000);
            pw.WriteInt(0x014F373B);

            pw.WriteInt(0); //pvp exp
            pw.WriteByte(0xA); //pvp rank
            pw.WriteInt(0); //battle points

            pw.WriteByte(5);
            pw.WriteByte(6);

            pw.WriteInt(0); //Knights of virtue blesssing on screen
            pw.WriteByte(0); //part time job action of resting = 1, herbalism= 2, Mining = 3, general store = 4, Weapon and armor store = 5

            pw.WriteInt(0x014F373B);
            pw.WriteUInt(0xFDE04000);
            pw.WriteInt(0);
            pw.WriteByte(0);

            for (int i = 0; i < 9; i++) //getCharacterCard
            {
                pw.WriteInt(0);
                pw.WriteByte(0);
                pw.WriteInt(0);
            }

            long ticksNow = MapleFormatHelper.GetMapleTimeStamp(DateTime.UtcNow); //last login time, reversed long: last 4 bytes come first O-o"
            pw.WriteInt((int)((ticksNow >> 32) & 0xFFFFFFFF));
            pw.WriteInt((int)(ticksNow & 0xFFFFFFFF));
        }

        public static void AddCharLook(PacketWriter pw, MapleCharacter chr, bool mega = false)
        {
            pw.WriteByte(chr.Gender);
            pw.WriteByte(chr.Skin);
            pw.WriteInt(chr.Face);
            pw.WriteShort(chr.Job);
            pw.WriteShort(chr.SubJob); //not sure
            pw.WriteBool(mega);
            pw.WriteInt(chr.Hair);

            IEnumerable<MapleItem> allEquips = chr.Inventory.GetItemsFromInventory(MapleInventoryType.Equipped);
            Dictionary<byte, MapleItem> equips = new Dictionary<byte, MapleItem>();
            Dictionary<byte, MapleItem> maskedEquips = new Dictionary<byte, MapleItem>();
            foreach (MapleItem equip in allEquips) //I really dont understand nexon
            {
                byte pos = (byte)Math.Abs(equip.Position);
                if (!equips.ContainsKey(pos) && pos < 100)
                    equips.Add(pos, equip);
                else if (pos > 100 && pos != 111)
                {
                    pos %= 100;
                    if (equips.ContainsKey(pos))
                        maskedEquips.Add(pos, equip);
                    else
                        equips.Add(pos, equip);
                }
                else if (equips.ContainsKey(pos))
                    maskedEquips.Add(pos, equip);
            }

            foreach (var equip in equips)
            {
                pw.WriteByte(equip.Key);
                pw.WriteInt(equip.Value.ItemId);
            }
            pw.WriteByte(0xFF);

            foreach (var equip in maskedEquips)
            {
                pw.WriteByte(equip.Key);
                pw.WriteInt(equip.Value.ItemId);
            }
            pw.WriteByte(0xFF);

            pw.WriteByte(0xFF);

            pw.WriteInt(0); //v118

            MapleItem weapon = chr.Inventory.GetEquippedItem((short)MapleEquipPosition.Weapon);
            int weaponId = 0;
            if (weapon != null) weaponId = weapon.ItemId;
            MapleItem shield = chr.Inventory.GetEquippedItem((short)MapleEquipPosition.SecondWeapon); //Secondary weapon  or shield
            int shieldId = 0;
            if (shield != null) shieldId = shield.ItemId;
            pw.WriteInt(weaponId);
            pw.WriteInt(shieldId);
            pw.WriteBool(chr.IsMercedes); //Mercedes ears
            pw.WriteZeroBytes(14);
            if (chr.IsDemon || chr.IsXenon)
                pw.WriteInt(chr.FaceMark);
            if (chr.IsBeastTamer)
            {
                pw.WriteInt(chr.FaceMark);
                pw.WriteBool(true); //show ears
                pw.WriteInt(chr.TamerEars);
                pw.WriteBool(true); //show tail
                pw.WriteInt(chr.TamerTail);
            }
        }

        public static void AddCharInfo(PacketWriter pw, MapleCharacter chr)
        {
            pw.WriteHexString("FF FF FF FF FF FF FF FF");

            pw.WriteByte(0);

            pw.WriteUInt(0xFFFFFFF8); //new v135 FA FF FF FF in v158?
            pw.WriteUInt(0xFFFFFFF8); //new v135
            pw.WriteUInt(0xFFFFFFF8); //new v135

            pw.WriteByte(0);
            pw.WriteByte(0);
            pw.WriteInt(0);
            pw.WriteByte(0);

            AddCharStats(pw, chr);

            pw.WriteByte(50); //buddy list capacity

            if (chr.FairyBlessingOrigin.Length > 0)
            {
                pw.WriteByte(1);
                pw.WriteMapleString(chr.FairyBlessingOrigin);
            }
            else pw.WriteByte(0);

            if (chr.EmpressBlessingOrigin.Length > 0)
            {
                pw.WriteByte(1);
                pw.WriteMapleString(chr.EmpressBlessingOrigin);
            }
            else pw.WriteByte(0);

            pw.WriteByte(0); //TODO: Ultimate explorer's "parent" name

            MapleInventory.Packets.AddInventoryInfo(pw, chr.Inventory);

            //SkillInfo
            pw.WriteByte(1); //use old, max 500 skills            
            pw.WriteShort((short)chr.Skills.Count);
            foreach (KeyValuePair<int, Skill> kvp in chr.Skills)
            {
                if (kvp.Value.SkillExp != 0)
                {
                    pw.WriteInt(kvp.Key);
                    pw.WriteShort(kvp.Value.SkillExp);
                    pw.WriteByte(0);
                    pw.WriteByte(kvp.Value.Level);
                    pw.WriteLong(MapleFormatHelper.GetMapleTimeStamp(kvp.Value.Expiration));
                }
                else
                {
                    pw.WriteInt(kvp.Key);
                    pw.WriteInt(kvp.Value.Level);
                    pw.WriteLong(MapleFormatHelper.GetMapleTimeStamp(kvp.Value.Expiration));
                    if (kvp.Value.HasMastery) //hyper skills as well
                        pw.WriteInt(kvp.Value.MasterLevel);
                }
            }

            pw.WriteShort(0);

            //Cooldowninfo
            pw.WriteShort((short)chr.Cooldowns.Count); //cooldown size
            foreach (var kvp in chr.Cooldowns)
            {
                pw.WriteInt(kvp.Key); //skill Id
                int remaining = (int)(kvp.Value.StartTime.AddMilliseconds(kvp.Value.Duration) - DateTime.UtcNow).TotalMilliseconds;
                pw.WriteInt(remaining / 1000); //cooldown time is in seconds
            }

            //Quests
            pw.WriteByte(1);
            pw.WriteUShort((ushort)chr.StartedQuests.Count); //started quests size
            foreach (var questPair in chr.StartedQuests)
            {
                pw.WriteUShort((ushort)questPair.Key);
                pw.WriteMapleString(questPair.Value.Data);
            }
            pw.WriteShort(0); //some custom string quests or something, each one has 2 maplestrings, 2 examples: "1NX5211068" "1", "SE20130116" "1"
            pw.WriteByte(1);
            pw.WriteUShort((ushort)chr.CompletedQuests.Count); //completed quests size
            foreach (var questPair in chr.CompletedQuests)
            {
                pw.WriteUShort((ushort)questPair.Key);
                pw.WriteUInt(questPair.Value);
            }

            {   //TODO: Ringinfo
                pw.WriteShort(0);
                pw.WriteShort(0);
                pw.WriteShort(0);
                pw.WriteShort(0);
            }

            {   //TODO: Telerock info
                for (int i = 0; i < 41; i++)
                    pw.WriteInt(999999999);
            }

            {   //TODO: monster cards
                pw.WriteInt(0);
                pw.WriteByte(0); //unfinished
                pw.WriteShort(0); //size of card list
                pw.WriteInt(-1); //current set selected for bonus
            }

            pw.WriteShort(0);
            pw.WriteShort(0);

            pw.WriteInt(0); //new v148        

            { //custom questinfo
                if (chr.IsBeastTamer)
                {
                    pw.WriteShort(1); //size
                    pw.WriteUShort(59300); //questid
                    pw.WriteMapleString(String.Format("bTail=1;bEar=1;TailID={0};EarID={1}", chr.TamerTail, chr.TamerEars)); //Why the fuck is this here
                }
                else
                    pw.WriteShort(0); //size
                                      //foreach:
                                      //pw.WriteShort(key);
                                      //pw.WriteMapleString(questinfo); e.g.: "RG=0;SM=0;ALP=0;DB=0;CD=0;MH=0"
            }

            if (chr.IsWildHunter)
            {
                pw.WriteInt(10); //TODO: quest record 111112 custom data jaguar ID
                for (int i = 0; i < 5; i++)
                    pw.WriteInt(0);
            }



            pw.WriteShort(0); //dunno, probably amount for something
            pw.WriteShort(0); //dunno, probably amount for something
            pw.WriteShort(0); //new v143

            pw.WriteInt(0); //new v148

            // Stolen skills:
            if (chr.IsPhantom && chr.Resource != null && chr.Resource.Type == ResourceSystemType.Phantom)
            {
                PhantomSystem resource = (PhantomSystem)chr.Resource;
                for (int i = 0; i < 13; i++)
                    pw.WriteInt(resource.StolenSkills[i]);
                for (int i = 0; i < 4; i++)
                    pw.WriteInt(resource.ChosenSkills[i]);
            }
            else
            {
                for (int i = 0; i < 17; i++)
                    pw.WriteInt(0);
            }


            pw.WriteShort(0); //added v135s
            pw.WriteInt(0); //added v135
            pw.WriteShort(0); //added v148

            {   //TODO: inner stats
                pw.WriteByte(0); //amount                
                pw.WriteInt(1); //Honor level
                pw.WriteInt(0); //Honor Exp
            }

            pw.WriteByte(1); //added v135
            pw.WriteShort(0); //added v135
            pw.WriteByte(0); //added v135

            pw.WriteInt(0);
            pw.WriteInt(0);
            pw.WriteInt(0);
            pw.WriteByte(0);
            pw.WriteInt(-1);
            pw.WriteInt(0);

            pw.WriteInt(0); // new between v149 and 158
            pw.WriteInt(0); // new between v149 and 158
            pw.WriteInt(0); // new between v149 and 158

            pw.WriteLong(MapleFormatHelper.GetMapleTimeStamp(-2));

            pw.WriteShort(0); //dunno
            pw.WriteShort(0); //dunno
            pw.WriteInt(0); //dunno

            pw.WriteInt(0); //new between v149 and 158
            pw.WriteShort(0); //new betweenv 149 and 158

            pw.WriteMapleString("Creating..."); //name of farm, creating... = not made yet
            pw.WriteInt(0); //coins
            pw.WriteInt(0); //level
            pw.WriteInt(0); //exp
            pw.WriteInt(0); //clovers
            pw.WriteInt(0); //diamonds nx currency 
            pw.WriteByte(0); //kitty power

            {   //Some stuff added in v135...    
                pw.WriteInt(0);
                pw.WriteInt(0);
                pw.WriteInt(1);
                pw.WriteInt(-1);
                pw.WriteInt(0);
                pw.WriteByte(0);
                pw.WriteInt(0);
                pw.WriteLong(MapleFormatHelper.GetMapleTimeStamp(-2L));
                pw.WriteInt(0);
                pw.WriteInt(0); //new between v149 and 158
                pw.WriteByte(0);
                pw.WriteByte(1);
                pw.WriteByte(0);
                pw.WriteInt(1);
                pw.WriteInt(0);
                pw.WriteInt(0);
                pw.WriteLong(MapleFormatHelper.GetMapleTimeStamp(DateTime.UtcNow)); //Timestamp
                pw.WriteShort(0);
                pw.WriteShort(0);
                pw.WriteByte(0);
                pw.WriteInt(0);
                pw.WriteInt(0);

                //pw.WriteInt(0x143EEA58);
                pw.WriteZeroBytes(60); //68?

                pw.WriteLong(MapleFormatHelper.GetMapleTimeStamp(DateTime.UtcNow)); //Timestamp
                pw.WriteByte(0);
                pw.WriteByte(1);
                pw.WriteShort(0);

            }

            pw.WriteInt(chr.AccountId);
            pw.WriteInt(chr.Id);

            int size = 4;
            pw.WriteInt(size);
            pw.WriteInt(0); //new v142
            for (int i = 0; i < size; i++)
                pw.WriteLong(9410165 + i);

        }

        public static void EnterChannel(MapleClient c)
        {
            MapleCharacter chr = c.Account.Character;
            PacketWriter pw = new PacketWriter(SendHeader.EnterMap);
            //pw.WriteHexString("02 00 01 00 00 00 00 00 00 00 02 00 00 00 00 00 00 00 07 00 00 00 00 00 00 00 00 01 00 00 00 00 01 00 00 0A 5D 73 0A C6 00 1C DE 49 CF 2E 62 FF FF FF FF FF FF FF FF 00 FC FF FF FF FC FF FF FF FC FF FF FF 00 00 00 00 00 00 00 36 2F 37 00 41 6E 67 65 72 73 68 72 6F 6F 6D 00 00 00 00 20 4E 00 00 44 75 00 00 FF 00 00 07 00 00 28 00 07 00 04 00 04 00 5F 00 00 00 88 00 00 00 47 00 00 00 47 00 00 00 00 00 00 3E 01 00 00 00 00 00 00 00 00 00 00 9F 86 01 00 00 00 00 00 00 E1 F5 05 00 9E 03 00 00 00 00 00 A4 A4 1A 78 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 40 E0 FD 3B 37 4F 01 00 00 00 00 0A 00 00 00 00 05 06 00 00 00 00 00 3B 37 4F 01 00 40 E0 FD 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 6D 34 D0 01 F0 7E D8 08 32 01 06 00 4E 75 6B 61 6B 6F 00 00 A8 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 36 2F 37 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 18 18 18 18 60 00 40 E0 FD 3B 37 4F 01 00 05 00 01 16 DF 0F 00 00 00 80 05 BB 46 E6 17 02 FF FF FF FF 01 04 00 00 07 02 00 14 00 00 00 FF 00 11 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 FF FF FF FF FF FF FF FF 00 40 E0 FD 3B 37 4F 01 FF FF FF FF 00 00 00 00 00 00 00 00 00 40 E0 FD 3B 37 4F 01 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 06 00 01 2E 2D 10 00 00 00 80 05 BB 46 E6 17 02 FF FF FF FF 01 04 00 00 07 03 00 14 00 00 00 FF 00 11 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 FF FF FF FF FF FF FF FF 00 40 E0 FD 3B 37 4F 01 FF FF FF FF 00 00 00 00 00 00 00 00 00 40 E0 FD 3B 37 4F 01 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 07 00 01 A5 5B 10 00 00 00 80 05 BB 46 E6 17 02 FF FF FF FF 01 04 00 00 05 02 00 14 00 00 00 FF 00 11 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 FF FF FF FF FF FF FF FF 00 40 E0 FD 3B 37 4F 01 FF FF FF FF 00 00 00 00 00 00 00 00 00 40 E0 FD 3B 37 4F 01 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 0B 00 01 8C DE 13 00 00 00 80 05 BB 46 E6 17 02 FF FF FF FF 01 01 00 00 07 0F 00 14 00 00 00 FF 00 11 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 FF FF FF FF FF FF FF FF 00 40 E0 FD 3B 37 4F 01 FF FF FF FF 00 00 00 00 00 00 00 00 00 40 E0 FD 3B 37 4F 01 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 37 00 01 20 E2 11 00 00 00 80 05 BB 46 E6 17 02 FF FF FF FF 3C 00 00 00 01 00 01 00 01 00 01 00 14 00 00 00 FF 00 01 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 FF FF FF FF FF FF FF FF 00 40 E0 FD 3B 37 4F 01 FF FF FF FF 00 00 00 00 00 00 00 00 00 40 E0 FD 3B 37 4F 01 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 01 00 01 1E 4A 0F 00 00 00 80 05 BB 46 E6 17 02 FF FF FF FF 01 14 00 00 07 05 00 01 00 14 00 00 00 FF 00 11 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 A3 13 00 00 9B 01 00 30 00 40 E0 FD 3B 37 4F 01 FF FF FF FF 00 00 00 00 00 00 00 00 00 40 E0 FD 3B 37 4F 01 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 02 00 01 95 4A 0F 00 00 00 80 05 BB 46 E6 17 02 FF FF FF FF 01 04 00 00 07 06 00 14 00 00 00 FF 00 11 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 6D 69 00 00 E1 01 00 00 00 40 E0 FD 3B 37 4F 01 FF FF FF FF 00 00 00 00 00 00 00 00 00 40 E0 FD 3B 37 4F 01 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 03 00 01 9F 5B 10 00 00 00 80 05 BB 46 E6 17 02 FF FF FF FF 01 04 00 00 05 07 00 14 00 00 00 FF 00 11 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 D5 0F 00 00 A4 01 00 40 00 40 E0 FD 3B 37 4F 01 FF FF FF FF 00 00 00 00 00 00 00 00 00 40 E0 FD 3B 37 4F 01 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 01 02 80 84 1E 00 00 00 80 05 BB 46 E6 17 02 FF FF FF FF 1C 00 00 00 00 00 02 02 83 84 1E 00 00 00 80 05 BB 46 E6 17 02 FF FF FF FF 17 00 00 00 00 00 03 02 81 84 1E 00 00 00 80 05 BB 46 E6 17 02 FF FF FF FF 09 00 00 00 00 00 04 02 C8 72 1F 00 00 00 80 05 BB 46 E6 17 02 FF FF FF FF 11 00 00 00 00 00 00 00 01 02 E9 7D 3F 00 00 00 80 05 BB 46 E6 17 02 FF FF FF FF 01 00 00 00 00 00 02 02 13 09 3D 00 00 00 80 05 BB 46 E6 17 02 FF FF FF FF 07 00 00 00 00 00 03 02 0B 09 3D 00 00 00 80 05 BB 46 E6 17 02 FF FF FF FF 0C 00 00 00 00 00 04 02 00 09 3D 00 00 00 80 05 BB 46 E6 17 02 FF FF FF FF 0E 00 00 00 00 00 05 02 01 09 3D 00 00 00 80 05 BB 46 E6 17 02 FF FF FF FF 13 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 01 04 00 49 00 00 00 00 00 00 00 00 80 05 BB 46 E6 17 02 EA 03 00 00 01 00 00 00 00 80 05 BB 46 E6 17 02 80 B8 C4 04 01 00 00 00 00 80 05 BB 46 E6 17 02 0C 00 00 00 01 00 00 00 00 80 05 BB 46 E6 17 02 00 00 00 00 01 17 00 00 00 06 00 30 30 36 30 30 39 18 51 01 00 30 63 25 01 00 30 AA CC 02 00 32 30 A3 CC 01 00 31 7B CC 01 00 32 94 0A 01 00 31 C5 1D 00 00 A4 CC 01 00 31 DE 28 01 00 30 A5 CC 01 00 30 6B 1B 01 00 30 5F 38 00 00 E0 28 01 00 30 A6 CC 01 00 31 7A 1C 01 00 30 0E 51 09 00 53 65 61 72 63 68 69 6E 67 54 2F 01 00 31 6D 65 01 00 30 0C 2C 08 00 43 6F 6D 70 6C 65 74 65 2D 1C 01 00 30 7C 1C 01 00 30 BE 70 01 00 31 01 00 08 00 73 65 6E 67 6F 6B 75 52 01 00 31 01 07 00 63 6F 44 80 7A 59 1B 1E CB 7F 7A 59 0B 04 F6 F0 CE 41 0C 04 F7 F0 CE 41 CE AA CB 7F 7A 59 07 04 F5 F0 CE 41 0F 04 23 F2 CE 41 00 00 00 00 00 00 00 00 FF C9 9A 3B FF C9 9A 3B FF C9 9A 3B FF C9 9A 3B FF C9 9A 3B FF C9 9A 3B FF C9 9A 3B FF C9 9A 3B FF C9 9A 3B FF C9 9A 3B FF C9 9A 3B FF C9 9A 3B FF C9 9A 3B FF C9 9A 3B FF C9 9A 3B FF C9 9A 3B FF C9 9A 3B FF C9 9A 3B FF C9 9A 3B FF C9 9A 3B FF C9 9A 3B FF C9 9A 3B FF C9 9A 3B FF C9 9A 3B FF C9 9A 3B FF C9 9A 3B FF C9 9A 3B FF C9 9A 3B FF C9 9A 3B FF C9 9A 3B FF C9 9A 3B FF C9 9A 3B FF C9 9A 3B FF C9 9A 3B FF C9 9A 3B FF C9 9A 3B FF C9 9A 3B FF C9 9A 3B FF C9 9A 3B FF C9 9A 3B FF C9 9A 3B 00 00 00 00 00 00 00 FF FF FF FF 00 00 00 00 00 00 00 00 4B 00 1C 1E 13 00 64 72 61 77 3D 30 3B 6C 6F 73 65 3D 30 3B 77 69 6E 3D 30 9B 65 00 00 61 47 27 00 63 6F 75 6E 74 3D 30 3B 6C 61 73 74 3D 31 35 2F 30 31 2F 32 30 3B 73 74 61 74 65 31 3D 30 3B 73 74 61 74 65 32 3D 30 9F 65 00 00 61 E6 16 00 6C 61 73 74 44 61 79 3D 31 35 2F 30 31 2F 32 30 2F 30 33 2F 33 31 0D 47 14 00 65 54 69 6D 65 3D 31 32 2F 31 32 2F 33 31 2F 30 30 2F 30 30 B5 46 07 00 63 6F 75 6E 74 3D 30 5B 46 0C 00 52 65 74 75 72 6E 55 73 65 72 3D 31 A7 47 07 00 73 74 61 74 65 3D 30 A9 47 07 00 73 74 61 74 65 3D 30 71 37 1D 00 6C 61 73 74 47 61 6D 65 3D 31 35 2F 30 31 2F 32 30 3B 54 52 57 41 74 74 65 6E 64 3D 30 9F 46 1C 00 69 6E 64 65 78 3D 31 3B 6C 61 73 74 52 3D 31 34 2F 31 32 2F 33 30 3B 73 6E 31 3D 30 83 38 14 00 63 6F 75 6E 74 3D 30 3B 72 65 73 65 74 3D 30 3B 70 63 3D 30 AF 47 07 00 73 74 61 74 65 3D 30 CB 67 06 00 77 61 6E 67 3D 30 0B 48 2F 00 41 3D 30 3B 42 3D 30 3B 43 3D 30 3B 44 3D 30 3B 45 3D 30 3B 46 3D 30 3B 47 3D 30 3B 48 3D 30 3B 49 3D 30 3B 4A 3D 30 3B 4B 3D 30 3B 4C 3D 30 55 67 05 00 76 61 6C 3D 30 79 37 1A 00 6C 61 73 74 47 61 6D 65 3D 31 35 2F 30 31 2F 32 30 3B 41 74 74 65 6E 64 3D 30 B5 47 07 00 73 74 61 74 65 3D 30 5F 47 26 00 42 42 3D 30 3B 43 42 3D 30 3B 50 3D 30 3B 42 4C 3D 30 3B 43 4C 3D 30 3B 42 51 3D 30 3B 43 50 3D 30 3B 43 51 3D 30 9B 47 07 00 73 74 61 74 65 3D 30 9D 47 18 00 70 61 72 74 79 3D 31 30 3B 73 6F 6C 6F 3D 31 30 3B 72 3D 30 31 2F 32 30 B9 67 07 00 63 6F 75 6E 74 3D 30 75 38 07 00 63 68 65 63 6B 3D 30 17 69 00 00 19 69 00 00 42 34 0C 00 52 6F 6C 6C 50 65 72 44 61 79 3D 30 09 AB 07 00 63 6C 65 61 72 3D 30 53 0C 07 00 72 65 73 65 74 3D 31 A0 46 05 00 6E 75 6D 3D 30 6A 36 1D 00 6C 61 73 74 47 61 6D 65 3D 31 35 2F 30 31 2F 32 30 3B 53 6E 57 41 74 74 65 6E 64 3D 30 40 47 2F 00 63 6F 75 6E 74 3D 30 3B 64 6F 31 3D 30 3B 64 6F 32 3D 30 3B 64 61 69 6C 79 46 50 3D 30 3B 6C 61 73 74 44 61 74 65 3D 32 30 31 35 30 31 32 30 60 47 33 00 41 3D 30 3B 42 3D 30 3B 43 3D 30 3B 44 3D 30 3B 45 3D 30 3B 46 3D 30 3B 47 3D 30 3B 48 3D 30 3B 49 3D 30 3B 4A 3D 30 3B 4B 3D 30 3B 4C 3D 30 3B 4D 3D 30 64 47 04 00 41 51 3D 30 23 7F 1F 00 6C 61 73 74 44 65 63 54 69 6D 65 3D 32 30 31 35 2F 30 31 2F 32 30 20 30 33 3A 33 31 3A 32 33 0C 47 0E 00 63 6F 6D 65 62 61 63 6B 55 73 65 72 3D 31 B4 46 07 00 63 6F 75 6E 74 3D 30 68 47 11 00 50 3D 30 3B 51 3D 30 3B 43 50 3D 30 3B 43 51 3D 30 B6 46 07 00 63 6F 75 6E 74 3D 30 6A 47 15 00 63 6F 75 6E 74 3D 30 3B 6C 61 73 74 3D 31 35 2F 30 31 2F 32 30 10 47 06 00 76 61 6C 32 3D 30 C2 67 0D 00 61 64 64 50 3D 30 3B 61 64 64 53 3D 30 12 47 40 00 4D 4C 3D 30 3B 4D 4D 3D 30 3B 4D 41 3D 30 3B 4D 42 3D 30 3B 4D 43 3D 30 3B 4D 44 3D 30 3B 4D 45 3D 30 3B 4D 46 3D 30 3B 4D 47 3D 30 3B 4D 48 3D 30 3B 4D 49 3D 30 3B 4D 4A 3D 30 3B 4D 4B 3D 30 9A 46 1A 00 63 6F 75 6E 74 30 3D 31 3B 63 6F 75 6E 74 31 3D 31 3B 63 6F 75 6E 74 32 3D 31 A8 47 07 00 73 74 61 74 65 3D 30 16 47 31 00 52 48 3D 30 3B 47 54 3D 30 3B 57 4D 3D 30 3B 46 41 3D 30 3B 45 43 3D 30 3B 43 48 3D 30 3B 4B 44 3D 30 3B 49 4B 3D 30 3B 50 44 3D 30 3B 50 46 3D 30 CA 47 07 00 73 74 61 74 65 3D 30 FA 46 26 00 63 6F 75 6E 74 3D 37 36 30 32 32 35 39 3B 74 69 6D 65 3D 32 30 31 34 2F 31 32 2F 33 30 20 30 39 3A 30 37 3A 33 37 0A 48 1D 00 48 52 3D 30 3B 4E 4F 3D 30 3B 52 4D 3D 30 3B 47 58 3D 30 3B 4B 58 3D 30 3B 45 4D 3D 30 86 38 07 00 73 74 61 74 65 3D 30 0C 48 23 00 41 3D 30 3B 42 3D 30 3B 43 3D 30 3B 44 3D 30 3B 45 3D 30 3B 46 3D 30 3B 47 3D 30 3B 48 3D 30 3B 49 3D 30 24 C8 0A 00 53 74 61 67 65 4B 65 79 3D 30 9E 47 07 00 51 75 65 73 74 3D 30 BC 47 06 00 76 61 6C 32 3D 30 66 A8 07 00 63 6C 65 61 72 3D 30 CA A8 07 00 63 6C 65 61 72 3D 30 14 69 13 00 73 66 3D 30 3B 6D 74 3D 30 3B 61 6C 3D 31 3B 69 64 3D 30 16 69 00 00 2E A9 07 00 63 6C 65 61 72 3D 30 18 69 00 00 92 A9 07 00 63 6C 65 61 72 3D 30 C3 33 07 00 63 6F 75 6E 74 3D 30 F6 A9 07 00 63 6C 65 61 72 3D 30 A7 E2 07 00 63 68 65 63 6B 3D 30 CD 33 0B 00 62 6F 72 6E 3D 31 31 30 34 30 38 0C AB 2E 00 41 6C 6C 43 6F 6D 70 6C 65 74 65 3D 30 3B 73 74 61 74 65 3D 30 3B 64 6F 6E 65 3D 30 3B 6C 61 73 74 64 61 74 65 3D 31 35 2F 30 31 2F 32 30 D7 33 05 00 73 6E 32 3D 30 F7 33 04 00 30 33 3D 31 FA AA 15 00 65 6E 74 65 72 32 3D 31 3B 6C 74 32 3D 31 35 2F 30 31 2F 32 30 87 35 08 00 6B 43 6F 75 6E 74 3D 30 31 15 04 00 64 63 3D 30 8B 45 00 00 85 46 17 00 31 3D 30 3B 32 3D 30 3B 33 3D 30 3B 34 3D 30 3B 35 3D 30 3B 36 3D 30 87 46 1E 00 52 47 3D 30 3B 53 4D 3D 30 3B 41 4C 50 3D 30 3B 44 42 3D 30 3B 43 44 3D 30 3B 4D 48 3D 30 23 47 09 00 62 41 74 74 65 6E 64 3D 30 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 01 00 00 00 00 00 00 00 01 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 FF FF FF FF 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 40 E0 FD 3B 37 4F 01 00 00 00 00 00 00 00 00 00 00 00 00 00 00 0B 00 43 72 65 61 74 69 6E 67 2E 2E 2E 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 01 00 00 00 FF FF FF FF 00 00 00 00 00 00 00 00 00 00 40 E0 FD 3B 37 4F 01 00 00 00 00 00 00 00 00 00 01 00 01 00 00 00 00 00 00 00 64 00 00 00 40 AF B3 B8 61 34 D0 01 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 60 1E A7 01 36 35 D0 01 00 01 00 00 F3 F4 03 00 36 2F 37 00 04 00 00 00 00 00 00 00 75 96 8F 00 00 00 00 00 76 96 8F 00 00 00 00 00 77 96 8F 00 00 00 00 00 78 96 8F 00 00 00 00 00 00 00 00 00 00 00 F0 73 0C 83 6F 34 D0 01 64 00 00 00 00 00 01 00 00 00 00 00 00");

            pw.WriteShort(2);
            pw.WriteInt(1);
            pw.WriteInt(0);
            pw.WriteInt(2);
            pw.WriteInt(0);
            pw.WriteInt(c.Channel);
            pw.WriteByte(0);
            pw.WriteInt(0);
            pw.WriteByte(1);
            pw.WriteInt(0);
            pw.WriteByte(1);
            pw.WriteShort(0); //1 = 10 sec login message (2x maplestring credz -> Clarity)

            //conect random data            
            pw.WriteHexString("FB 90 4C 0A 8E A3 EC 83 C7 D3 15 15");

            MapleCharacter.AddCharInfo(pw, chr);

            pw.WriteInt(0);
            pw.WriteByte(0); //v143
            pw.WriteByte(0); //new between v149 and v158
            pw.WriteLong(MapleFormatHelper.GetMapleTimeStamp(DateTime.UtcNow)); //current time

            pw.WriteInt(100);
            pw.WriteByte(0);
            pw.WriteByte(0);
            pw.WriteByte(1);
            pw.WriteZeroBytes(6);
            c.SendPacket(pw);
        }

        public static void EnterMap(MapleClient c, int mapId, byte spawnPoint, bool fromSpecialPortal = false)
        {
            PacketWriter pw = new PacketWriter(SendHeader.EnterMap);
            pw.WriteShort(2);
            pw.WriteLong(1);
            pw.WriteLong(2);
            pw.WriteInt(c.Channel);
            pw.WriteInt(0);
            pw.WriteByte(0);
            pw.WriteByte(fromSpecialPortal ? (byte)2 : (byte)3);
            pw.WriteLong(0);
            pw.WriteInt(mapId);
            pw.WriteByte(spawnPoint);
            pw.WriteInt(c.Account.Character.Hp);
            pw.WriteByte(0);
            pw.WriteByte(0); //Unknown added in v143
            pw.WriteByte(0); //Unknown added in v158
            pw.WriteLong(MapleFormatHelper.GetMapleTimeStamp(DateTime.UtcNow));
            pw.WriteInt(100);
            pw.WriteByte(0);
            pw.WriteByte(0);
            pw.WriteByte(1);
            pw.WriteZeroBytes(6);

            c.SendPacket(pw);
        }

        public static void SendCSInfo(MapleClient c)
        {
            MapleCharacter chr = c.Account.Character;
            PacketWriter pw = new PacketWriter();
            pw.WriteHeader(SendHeader.EnterCashShop);

            MapleCharacter.AddCharInfo(pw, chr);
            pw.WriteBool(true); //IsNotBeta? lol
            pw.WriteInt(0);
            pw.WriteShort(0); //ModCashItemInfo?
            pw.WriteInt(0); //Packages? dont know, neither want to
                            //Unk
            pw.WriteHexString("6C 00 6F 00 6F 00 6E 00 2E 00 20 00 23 00 63 00 44 00 6F 00 75 00 62 00 6C 00 65 00 2D 00 63 00 6C 00 69 00 63 00 6B 00 20 00 74 00 68 00 65 00 20 00 64 00 65 00 73 00 74 00 69 00 6E 00 61 00 74 00 69 00 6F 00 6E 00 20 00 6F 00 6E 00 20 00 74 00 68 00 65 00 20 00 57 00 6F 00 72 00 6C 00 64 00 20 00 4D 00 61 00 70 00 23 00 20 00 74 00 6F 00 20 00 66 00 6C 00 79 00 20 00 74 00 68 00 65 00 72 00 65 00 20 00 61 00 75 00 74 00 6F 00 6D 00 61 00 74 00 69 00 63 00 61 00 6C 00 6C 00 79 00 2E 00 20 00 28 00 41 00 75 00 74 00 6F 00 20 00 50 00 69 00 6C 00 6F 00 74 00 20 00 69 00 73 00 20 00 6F 00 6E 00 6C 00 79 00 20 00 61 00 76 00 61 00 69 00 6C 00 61 00 62 00 6C 00 65 00 20 00 6F 00 6E 00 20 00 73 00 6F 00 6D 00 65 00 20 00 63 00 6F 00 6E 00 74 00 69 00 6E 00 65 00 6E 00 74 00 73 00 2C 00 20 00 69 00 6E 00 63 00 6C 00 75 00 64 00 69 00 6E 00 67 00 20 00 56 00 69 00 63 00 74 00 6F 00 72 00 69 00 61 00 20 00 49 00 73 00 6C 00 61 00 6E 00 64 00 2E 00 20 00 23 00 63 00 42 00 75 00 79 00 20 00 61 00 20 00 46 00 6C 00 69 00 67 00 68 00 74 00 20 00 50 00 65 00 72 00 6D 00 69 00 74 00 20 00 66 00 72 00 6F 00 6D 00 20 00 49 00 6E 00 73 00 74 00 72 00 75 00 63 00 74 00 6F 00 72 00 20 00 49 00 72 00 76 00 69 00 6E 00 23 00 20 00 74 00 6F 00 20 00 75 00 73 00 65 00 20 00 74 00 68 00 65 00 20 00 53 00 6D 00 61 00 72 00 74 00 20 00 4D 00 6F 00 75 00 6E 00 74 00 20 00 6F 00 6E 00 20 00 6D 00 6F 00 72 00 65 00 20 00 63 00 6F 00 6E 00 74 00 69 00 6E 00 65 00 6E 00 74 00 73 00 2E 00 29 00 00 00 00 00 00 00 00 00 5B 00 01 5A 67 66 B9 0A C8 02 00 00 5B 00 4D 00 61 00 73 00 74 00 65 00 72 00 20 00 4C 00 65 00 76 00 65 00 6C 00 20 00 3A 00 20 00 31 00 5D 00 5C 00 6E 00 42 00 65 00 63 00 6F 00 6D 00 65 00 20 00 61 00 20 00 70 00 69 00 72 00 61 00 74 00 65 00 20 00 63 00 61 00 70 00 74 00 69 00 76 00 65 00 20 00 6F 00 6E 00 20 00 61 00 20 00 73 00 68 00 69 00 70 00 20 00 74 00 68 00 61 00 74 00 20 00 63 00 61 00 6E 00 20 00 75 00 73 00 65 00 20 00 74 00 68 00 65 00 20 00 44 00 6F 00 75 00 62 00 6C 00 65 00 20 00 4A 00 75 00 6D 00 70 00 20 00 73 00 6B 00 69 00 6C 00 6C 00 20 00 74 00 6F 00 20 00 6D 00 6F 00 76 00 65 00 20 00 23 00 63 00 76 00 65 00 72 00 79 00 20 00 71 00 75 00 69 00 63 00 6B 00 6C 00 79 00 23 00 2E 00 20 00 23 00 63 00 44 00 6F 00 75 00 62 00 6C 00 65 00 2D 00 63 00 6C 00 69 00 63 00 6B 00 20 00 74 00 68 00 65 00 20 00 64 00 65 00 73 00 74 00 69 00 6E 00 61 00 74 00 69 00 6F 00 6E 00 20 00 6F 00 6E 00 20 00 74 00 68 00 65 00 20 00 57 00 6F 00 72 00 6C 00 64 00 20 00 4D 00 61 00 70 00 23 00 20 00 74 00 6F 00 20 00 66 00 6C 00 79 00 20 00 74 00 68 00 65 00 72 00 65 00 20 00 61 00 75 00 74 00 6F 00 6D 00 61 00 74 00 69 00 63 00 61 00 6C 00 6C 00 79 00 2E 00 20 00 28 00 41 00 75 00 74 00 6F 00 20 00 50 00 69 00 6C 00 6F 00 74 00 20 00 69 00 73 00 20 00 6F 00 6E 00 6C 00 79 00 20 00 61 00 76 00 61 00 69 00 6C 00 61 00 62 00 6C 00 65 00 20 00 6F 00 6E 00 20 00 73 00 6F 00 6D 00 65 00 20 00 63 00 6F 00 6E 00 74 00 69 00 6E 00 65 00 6E 00 74 00 73 00 2C 00 20 00 69 00 6E 00 63 00 6C 00 75 00 64 00 69 00 6E 00 67 00 20 00 56 00 69 00 63 00 74 00 6F 00 72 00 69 00 61 00 20 00 49 00 73 00 6C 00 61 00 6E 00 64 00 2E 00 20 00 23 00 63 00 42 00 75 00 79 00 20 00 61 00 20 00 46 00 6C 00 69 00 67 00 68 00 74 00");
            pw.WriteZeroBytes(7);
            pw.WriteByte(0xAC);
            pw.WriteZeroBytes(6);
            pw.WriteLong(MapleFormatHelper.GetMapleTimeStamp(DateTime.UtcNow));
            pw.WriteZeroBytes(7);

            c.SendPacket(pw);
        }

        public static void AddSeparatedSP(MapleCharacter chr, PacketWriter pw)
        {
            Dictionary<byte, int> spTable = new Dictionary<byte, int>();
            for (int i = 0; i < 10; i++)
            {
                if (chr.SpTable[i] > 0)
                {
                    spTable.Add((byte)(i + 1), chr.SpTable[i]);
                }
            }
            pw.WriteByte((byte)spTable.Count);
            foreach (KeyValuePair<byte, int> pair in spTable)
            {
                pw.WriteByte(pair.Key);
                pw.WriteInt(pair.Value);
            }
        }

        public void EnableActions(bool updateClient = true)
        {
            ActionState = ActionState.Enabled;
            if (!updateClient) return;
            SortedDictionary<MapleCharacterStat, long> empty = new SortedDictionary<MapleCharacterStat, long>();
            UpdateStats(Client, empty, true);
        }

        public void SetActionState(ActionState state)
        {
            ActionState = state;
        }

        public bool DisableActions(ActionState newState = ActionState.Disabled)
        {
            if (ActionState != ActionState.Enabled)
            {
#if DEBUG
                ServerConsole.Debug("Player actions are disabled! Current state: " + Enum.GetName(typeof(ActionState), ActionState));
#endif
                return false;
            }
            ActionState = newState;
            return true;
        }

        public static void UpdateSingleStat(MapleClient c, MapleCharacterStat stat, long value, bool enableActions = false)
        {
            SortedDictionary<MapleCharacterStat, long> stats = new SortedDictionary<MapleCharacterStat, long>() { { stat, value } };
            UpdateStats(c, stats, enableActions);
        }

        public static void UpdateStats(MapleClient c, SortedDictionary<MapleCharacterStat, long> stats, bool enableActions)
        {
            PacketWriter pw = new PacketWriter();
            pw.WriteHeader(SendHeader.UpdateStats);

            pw.WriteBool(enableActions);
            if (enableActions)
                c.Account.Character.ActionState = ActionState.Enabled;
            long mask = 0;
            foreach (KeyValuePair<MapleCharacterStat, long> kvp in stats)
            {
                mask |= (long)kvp.Key;
            }
            pw.WriteLong(mask);
            foreach (KeyValuePair<MapleCharacterStat, long> kvp in stats)
            {
                switch (kvp.Key)
                {
                    case MapleCharacterStat.Skin:
                    case MapleCharacterStat.Level:
                    case MapleCharacterStat.Fatigue:
                    case MapleCharacterStat.BattleRank:
                    case MapleCharacterStat.IceGage: // not sure..
                        pw.WriteByte((byte)kvp.Value);
                        break;
                    case MapleCharacterStat.Str:
                    case MapleCharacterStat.Dex:
                    case MapleCharacterStat.Int:
                    case MapleCharacterStat.Luk:
                    case MapleCharacterStat.Ap:
                        pw.WriteShort((short)kvp.Value);
                        break;
                    case MapleCharacterStat.TraitLimit:
                        pw.WriteInt((int)kvp.Value);
                        pw.WriteInt((int)kvp.Value);
                        pw.WriteInt((int)kvp.Value);
                        break;
                    case MapleCharacterStat.Exp:
                    case MapleCharacterStat.Meso:
                        pw.WriteLong(kvp.Value);
                        break;
                    case MapleCharacterStat.Pet:
                        pw.WriteLong(kvp.Value);
                        pw.WriteLong(kvp.Value);
                        pw.WriteLong(kvp.Value);
                        break;
                    case MapleCharacterStat.Sp:
                        if (c.Account.Character.IsSeparatedSpJob)
                            AddSeparatedSP(c.Account.Character, pw);
                        else
                            pw.WriteShort((short)kvp.Value);
                        break;
                    case MapleCharacterStat.Job:
                        pw.WriteShort((short)kvp.Value);
                        pw.WriteShort(c.Account.Character.SubJob); //new v144
                        break;
                    default:
                        pw.WriteInt((int)kvp.Value);
                        break;
                }
            }
            pw.WriteByte(0xFF);
            if (mask == 0 && !enableActions)
            {
                pw.WriteByte(0);
            }
            pw.WriteInt(0);

            c.SendPacket(pw);
        }

        public static PacketWriter RemovePlayerFromMap(int Id)
        {
            PacketWriter pw = new PacketWriter();
            pw.WriteHeader(SendHeader.RemovePlayer);
            pw.WriteInt(Id);
            return pw;
        }

        public static PacketWriter SystemMessage(string message, short type)
        {
            PacketWriter pw = new PacketWriter(SendHeader.SystemMessage);
            pw.WriteShort(type);
            pw.WriteMapleString(message);
            return pw;
        }

        public static PacketWriter ServerNotice(string message, byte type, int channel = 0, bool whisperIcon = false)
        {
            PacketWriter pw = new PacketWriter();
            pw.WriteHeader(SendHeader.ServerNotice);

            pw.WriteByte(type);
            if (type == 4)
            {
                pw.WriteByte(1);
            }
            if ((type != 23) && (type != 24))
            {
                pw.WriteMapleString(message);
            }
            switch (type)
            {
                case 3:
                case 22:
                case 25:
                case 26:
                    pw.WriteByte((byte)(channel));
                    pw.WriteBool(whisperIcon);
                    break;
                case 9:
                    pw.WriteByte((byte)(channel));
                    break;
                case 12:
                    pw.WriteInt(channel);
                    break;
                case 6:
                case 11:
                case 20:
                    pw.WriteInt(0);
                    break;
                case 24:
                    pw.WriteShort(0);
                    break;
            }

            return pw;
        }

        private static void SetBuffMask(byte[] b, int bit)
        {
            int bitnum = (bit % 8);
            int bytenum = (bit - (bit % 8)) / 8;
            b[bytenum] |= (byte)(1 << bitnum);
        }

        private static void EncodeTime(PacketWriter packet, int time)
        {
            packet.WriteByte(1);
            packet.WriteInt(time);//This isn't the proper variable but it's better than Random(). It should be how long the session has been since it entered login screen
        }

        public static PacketWriter SpawnPlayer(MapleCharacter chr)
        {
            PacketWriter pw = new PacketWriter();
            pw.WriteHeader(SendHeader.SpawnPlayer);
            pw.WriteInt(chr.Id);
            pw.WriteByte(chr.Level);
            pw.WriteMapleString(chr.Name);
            pw.WriteMapleString(""); //Ultimate adventurer parent name
            if (chr.Guild != null)
            {
                pw.WriteMapleString(chr.Guild.Name);
                pw.WriteShort((short)chr.Guild.LogoBG);
                pw.WriteByte((byte)chr.Guild.LogoBGColor);
                pw.WriteShort((short)chr.Guild.Logo);
                pw.WriteByte((byte)chr.Guild.LogoColor);
            }
            else
            {
                pw.WriteLong(0);
            }
            pw.WriteByte(0); //new v135            

            pw.WriteZeroBytes(12); //v158

            /*List<BuffStat> buffs = new List<BuffStat> {MapleBuffStat.SPAWNMASK1};
            List<Pair<int, int>> valuesToWrite = new List<Pair<int, int>>(); //left = value, right = type to write
            if (chr.HasBuffStat(MapleBuffStat.DARK_SIGHT))
            {
                buffs.Add(MapleBuffStat.DARK_SIGHT);
            }
            if (chr.HasBuffStat(MapleBuffStat.ANGELIC_BLESSING))
            {
                buffs.Add(MapleBuffStat.ANGELIC_BLESSING);
                valuesToWrite.Add(new Pair<int, int>(1, 2));
                valuesToWrite.Add(new Pair<int, int>(1, -2022746));
            }
            buffs.Add(MapleBuffStat.SPAWNMASK1);
            buffs.Add(MapleBuffStat.SPAWNMASK2);

            Buff.WriteBuffMask(pw, buffs);*/

            for (int i = 0; i < 7; i++)
                pw.WriteInt(0);
            pw.WriteInt(0x00C00000);
            pw.WriteInt(0);
            pw.WriteInt(0);
            pw.WriteInt(0x00000030);
            pw.WriteInt(0);
            pw.WriteInt(0x0000003F);
            pw.WriteUInt(0x80000000);

            /*foreach (Pair<int, int> pair in valuesToWrite)
            {
                switch (pair.Right)
                {
                    case 1: //write byte
                        pw.WriteByte((byte)pair.Left);
                        break;
                    case 2: //write short
                        pw.WriteShort((short)pair.Left);
                        break;
                    case 4: //write int
                        pw.WriteInt((int)pair.Left);
                        break;
                }
            }*/

            pw.WriteInt(-1);

            pw.WriteByte(0);
            pw.WriteByte(0);
            pw.WriteByte(0);

            pw.WriteByte(0);
            pw.WriteByte(0);
            pw.WriteByte(0);

            //pw.WriteShort(0);

            for (int i = 0; i < 10; i++)
                pw.WriteInt(0);

            int encodetime = Environment.TickCount;
            EncodeTime(pw, encodetime);
            pw.WriteInt(0);
            pw.WriteInt(0);
            for (int i = 0; i < 2; i++)
            {
                EncodeTime(pw, encodetime);
                pw.WriteShort(0);
                pw.WriteInt(0);
                pw.WriteInt(0);
            }

            EncodeTime(pw, encodetime);
            pw.WriteInt(0);
            pw.WriteInt(0);

            EncodeTime(pw, encodetime);
            pw.WriteByte(0);

            pw.WriteUInt(0xDA77ACDE); //some constant that happens to be read by the same function as "encodetime" in ms
            pw.WriteShort(0);
            pw.WriteInt(0);
            pw.WriteInt(0);

            EncodeTime(pw, encodetime);
            pw.WriteInt(0);
            pw.WriteInt(0);
            pw.WriteInt(0);
            pw.WriteInt(0);

            EncodeTime(pw, encodetime);
            pw.WriteShort(0);
            pw.WriteShort(chr.Job);
            pw.WriteShort(chr.SubJob);

            pw.WriteInt(0); //new v158

            AddCharLook(pw, chr, true);

            for (int i = 0; i < 14; i++)
                pw.WriteInt(0);

            pw.WriteShort(-1);  //v158?
            pw.WriteZeroBytes(12); //v158?

            pw.WritePoint(chr.Position);
            pw.WriteByte(chr.Stance);
            pw.WriteShort(chr.Foothold);//$$UNSAFE$$ this should be foothold
            pw.WriteByte(0);
            pw.WriteByte(0);
            pw.WriteByte(0);
            pw.WriteByte(1);
            pw.WriteByte(0);

            //v143: Why are these mount things still here? I thought mount levels were removed in RED
            pw.WriteInt(1); //mount level 
            pw.WriteInt(0); //mount exp
            pw.WriteInt(0); //mount fatigue
            pw.WriteByte(0);
            pw.WriteByte(0);
            pw.WriteByte(0);
            pw.WriteByte(0);
            pw.WriteByte(0);
            pw.WriteByte(0);
            pw.WriteInt(0);
            pw.WriteByte(0);//v143
            pw.WriteInt(0);//v143
            //farm stuff, hence the indent
            {
                pw.WriteMapleString("Creating..."); //name of farm, creating... = not made yet
                pw.WriteInt(0); //coins
                pw.WriteInt(0); //level
                pw.WriteInt(0); //exp
                pw.WriteInt(0); //clovers
                pw.WriteInt(0); //diamonds nx currency 
                pw.WriteByte(0); //kitty power
                pw.WriteInt(0);//unk
                pw.WriteInt(0);//unk
                pw.WriteInt(1);//unk
            }
            for (int i = 0; i < 5; i++)
            {
                pw.WriteByte(0xFF);
            }

            pw.WriteInt(0); //v158
            pw.WriteByte(1); //v158

            pw.WriteInt(0);
            pw.WriteByte(0);
            pw.WriteInt(0);
            pw.WriteInt(0);

            pw.WriteZeroBytes(20); //v158
            return pw;
        }

        public static PacketWriter ShowKeybindLayout(Dictionary<uint, Pair<byte, int>> keybinds)
        {
            PacketWriter pw = new PacketWriter(SendHeader.KeybindLayout);

            bool empty = !keybinds.Any();
            pw.WriteBool(empty);
            for (byte i = 0; i < 89; i++)
            {
                Pair<byte, int> keybind;
                if (keybinds.TryGetValue(i, out keybind))
                {
                    pw.WriteByte(keybind.Left);
                    pw.WriteInt(keybind.Right);
                }
                else
                {
                    pw.WriteByte(0);
                    pw.WriteInt(0);
                }
            }
            return pw;
        }

        public static PacketWriter ShowQuickSlotKeys(int[] binds)
        {
            PacketWriter pw = new PacketWriter(SendHeader.ShowQuickSlotKeys);
            pw.WriteByte(1);
            if (binds.Length == 28)
            {
                for (int i = 0; i < 28; i++)
                {
                    pw.WriteInt(binds[i]);
                }
            }
            else
            {
                pw.WriteZeroBytes(112);
            }
            return pw;
        }
        #endregion

        #region Properties
        public bool IsDead
        {
            get { lock (HpLock) { return Hp <= 0; } }
        }

        public bool IsFacingLeft => Stance % 2 != 0;

        public bool IsDonor => Client.Account.AccountType >= 1;

        public bool IsStaff => Client.Account.AccountType >= 2;

        public bool IsAdmin => Client.Account.AccountType == 3;

        public bool IsSeparatedSpJob => !IsAran && !IsZero && !IsBeastTamer && !IsGameMasterJob;

        public int CurrentLevelSkillBook
        {
            get
            {
                if (!IsSeparatedSpJob)
                    return 0;
                if (Job >= 2210 && Job <= 2218)
                    return Job - 2209;

                if (Level <= 30)
                    return 0;
                if (Level <= 60)
                    return 1;
                if (Level <= 100)
                    return 2;
                if (Level > 100)
                    return 3;
                return 0;
            }
        }
        public int CurrentJobSkillBook
        {
            get
            {
                if (!IsSeparatedSpJob)
                    return 0;
                return JobConstants.GetSkillBookForJob(Job);
            }
        }
        public bool IsBeginnerJob => JobConstants.IsBeginnerJob(Job);
        public bool IsExplorer => Job < 600;
        public bool IsWarrior { get { return Job / 100 == 1; } }
        public bool IsFighter { get { return Job / 10 == 11; } }
        public bool IsPage { get { return Job / 10 == 12; } }
        public bool IsSpearman { get { return Job / 10 == 13; } }
        public bool IsMagician { get { return Job / 100 == 2; } }
        public bool IsFirePoisonMage { get { return Job / 10 == 21; } }
        public bool IsIceLightningMage { get { return Job / 10 == 22; } }
        public bool IsCleric { get { return Job / 10 == 23; } }
        public bool IsArcher { get { return Job / 100 == 3; } }
        public bool IsHunter { get { return Job / 10 == 31; } }
        public bool IsCrossbowman { get { return Job / 10 == 32; } }
        public bool IsThief { get { return Job / 100 == 4 && SubJob == 0; } }
        public bool IsAssassin { get { return Job / 10 == 41; } }
        public bool IsBandit { get { return Job / 10 == 42; } }
        public bool IsDualBlade { get { return SubJob == 1; } }
        public bool IsPirate { get { return Job / 100 == 5 && SubJob == 0; } }
        public bool IsBrawler { get { return Job / 10 == 51; } }
        public bool IsGunslinger { get { return Job / 10 == 52; } }
        public bool IsCannonneer { get { return SubJob == 2; } }
        public bool IsJett { get { return Job == 508 || Job / 10 == 57; } }

        public bool IsGameMasterJob { get { return Job / 100 == 9; } }
        public bool IsSuperGameMasterJob { get { return Job == 910; } }

        public bool IsCygnus { get { return Job / 1000 == 1; } }
        public bool IsDawnWarrior { get { return Job / 10 == 11; } }
        public bool IsBlazeWizard { get { return Job / 10 == 12; } }
        public bool IsWindArcher { get { return Job / 10 == 13; } }
        public bool IsNightWalker { get { return Job / 10 == 14; } }
        public bool IsThunderBreaker { get { return Job / 10 == 15; } }

        public bool IsHero { get { return Job / 1000 == 2; } }
        public bool IsAran { get { return Job == 2000 || Job / 100 == 21; } }
        public bool IsEvan { get { return Job == 2001 || Job / 100 == 22; } }
        public bool IsMercedes { get { return Job == 2002 || Job / 100 == 23; } }
        public bool IsPhantom { get { return Job == 2003 || Job / 100 == 24; } }
        public bool IsLuminous { get { return Job == 2004 || Job / 100 == 27; } }

        public bool IsResistance { get { return Job / 1000 == 3; } }
        public bool IsDemon { get { return Job == 3001 || Job / 100 == 31; } }
        public bool IsDemonSlayer { get { return Job == 3100 || Job / 10 == 311; } }
        public bool IsDemonAvenger { get { return Job == 3101 || Job / 10 == 312; } }
        public bool IsBattleMage { get { return Job / 100 == 32; } }
        public bool IsWildHunter { get { return Job / 100 == 33; } }
        public bool IsMechanic { get { return Job / 100 == 35; } }
        public bool IsXenon { get { return Job == 3002 || Job / 100 == 36; } }

        public bool IsSengoku { get { return Job / 1000 == 4; } }
        public bool IsHayato { get { return Job == 4001 || Job / 100 == 41; } }
        public bool IsKanna { get { return Job == 4002 || Job / 100 == 42; } }

        public bool IsMihile { get { return Job == 5000 || Job / 100 == 51; } }

        public bool IsNova { get { return Job / 1000 == 6; } }
        public bool IsKaiser { get { return Job == 6000 || Job / 100 == 61; } }
        public bool IsAngelicBuster { get { return Job == 6001 || Job / 100 == 65; } }

        public bool IsZero { get { return Job == 10000 || Job / 100 == 101; } }
        public bool IsBeastTamer { get { return Job == 11000 || Job / 100 == 112; } }
        #endregion
    }

    [Flags]
    public enum MapleCharacterStat : long
    {
        Skin = 0x1, // byte
        Face = 0x2, // int
        Hair = 0x4, // int
        Level = 0x10, // byte
        Job = 0x20, // short
        Str = 0x40, // short
        Dex = 0x80, // short
        Int = 0x100, // short
        Luk = 0x200, // short
        Hp = 0x400, // int
        MaxHp = 0x800, // int
        Mp = 0x1000, // int
        MaxMp = 0x2000, // int
        Ap = 0x4000, // short
        Sp = 0x8000, // short (depends)
        Exp = 0x10000, // long
        Fame = 0x20000, // int
        Meso = 0x40000, // long
        Pet = 0x180008, // Pets: 0x8 + 0x80000 + 0x100000  [3 longs]
        GachaponExp = 0x200000, // int
        Fatigue = 0x400000, // byte
        Charisma = 0x800000, // ambition int
        Insight = 0x1000000,
        Will = 0x2000000, // int
        Craft = 0x4000000, // dilligence, int
        Sense = 0x8000000, // empathy, int
        Charm = 0x10000000, // int
        TraitLimit = 0x20000000, // 12 bytes
        BattleExp = 0x40000000, // byte, int, int
        BattleRank = 0x80000000, // byte
        BattlePoints = 0x100000000,
        IceGage = 0x200000000,
        Virtue = 0x400000000
    }
}
