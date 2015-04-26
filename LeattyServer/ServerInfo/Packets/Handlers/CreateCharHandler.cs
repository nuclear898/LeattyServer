using System;
using System.Collections.Generic;
using LeattyServer.Constants;
using LeattyServer.Data;
using LeattyServer.Data.WZ;
using LeattyServer.Helpers;
using LeattyServer.ServerInfo.Inventory;
using LeattyServer.ServerInfo.Player;

namespace LeattyServer.ServerInfo.Packets.Handlers
{
    internal class CreateCharHandler
    {
        public static void Handle(MapleClient c, PacketReader pr)
        {
            MapleCharacter newCharacter = MapleCharacter.GetDefaultCharacter(c);
            bool succesful = false;
            string name = pr.ReadMapleString();
            if (!Functions.IsAlphaNumerical(name))
                return;
            bool nameAvailable = !MapleCharacter.CharacterExists(name);
            if (nameAvailable)
            {
                bool basicKeyLayout = pr.ReadInt() == 0;
                pr.Skip(4); //-1            
                int jobId = pr.ReadInt();
                JobType jobType = (JobType)jobId;
                bool jobIsEnabled;
                if (!GameConstants.CreateJobOptions.TryGetValue(jobType, out jobIsEnabled) || !jobIsEnabled)
                {
                    return;
                }

                /*ServerConsole.Debug("Jobtype " + jobId);
                PacketWriter pws = new PacketWriter();
                pws.WriteHeader(SendHeader.AddCharacter);
                pws.WriteBool(!false); //0 = succesful, 1 = failed
                c.SendPacket(pws);
                return;*/

                short subJob = pr.ReadShort();
                byte gender = pr.ReadByte();
                byte skinColor = pr.ReadByte();
                byte availableInts = pr.ReadByte(); // amount of following integers in the packet representing hair, skin and equips
                if (pr.Available / 4 != availableInts) //Must be a packet edit
                    return;
                int face = pr.ReadInt();
                int hair = pr.ReadInt();
                int hairColor = 0;
                int faceMark = 0;
                int tamerTail = 0;
                int tamerEars = 0;

                Dictionary<MapleEquipPosition, int> items = new Dictionary<MapleEquipPosition, int>();
                switch (jobType)
                {
                    case JobType.Explorer:
                    case JobType.DualBlade:
                    case JobType.Cannonneer:
                    case JobType.Cygnus:
                        pr.Skip(4); //skin color again
                        items.Add(MapleEquipPosition.Top, pr.ReadInt());
                        items.Add(MapleEquipPosition.Shoes, pr.ReadInt());
                        items.Add(MapleEquipPosition.Weapon, pr.ReadInt());
                        break;
                    case JobType.Resistance:
                    case JobType.Aran:
                    case JobType.Evan:
                    case JobType.Mihile:
                        hairColor = pr.ReadInt();
                        pr.Skip(4); //skin color again
                        items.Add(MapleEquipPosition.Top, pr.ReadInt());
                        items.Add(MapleEquipPosition.Bottom, pr.ReadInt());
                        items.Add(MapleEquipPosition.Shoes, pr.ReadInt());
                        items.Add(MapleEquipPosition.Weapon, pr.ReadInt());
                        break;
                    case JobType.Jett:
                        items.Add(MapleEquipPosition.Top, pr.ReadInt());
                        items.Add(MapleEquipPosition.Cape, pr.ReadInt());
                        items.Add(MapleEquipPosition.Shoes, pr.ReadInt());
                        items.Add(MapleEquipPosition.Weapon, pr.ReadInt());
                        break;
                    case JobType.Mercedes:
                        items.Add(MapleEquipPosition.Top, pr.ReadInt());
                        items.Add(MapleEquipPosition.Shoes, pr.ReadInt());
                        items.Add(MapleEquipPosition.Weapon, pr.ReadInt());
                        break;
                    case JobType.Phantom:
                    case JobType.Luminous:
                        pr.Skip(4); //skin color again
                        items.Add(MapleEquipPosition.Top, pr.ReadInt());
                        items.Add(MapleEquipPosition.Cape, pr.ReadInt());
                        items.Add(MapleEquipPosition.Shoes, pr.ReadInt());
                        items.Add(MapleEquipPosition.Weapon, pr.ReadInt());
                        break;
                    case JobType.Demon:
                        faceMark = pr.ReadInt();
                        items.Add(MapleEquipPosition.Top, pr.ReadInt());
                        items.Add(MapleEquipPosition.Shoes, pr.ReadInt());
                        items.Add(MapleEquipPosition.Weapon, pr.ReadInt());
                        items.Add(MapleEquipPosition.SecondWeapon, pr.ReadInt());
                        break;
                    case JobType.Xenon:
                        hairColor = pr.ReadInt();
                        pr.Skip(4); //skin color again
                        faceMark = pr.ReadInt();
                        items.Add(MapleEquipPosition.Top, pr.ReadInt());
                        items.Add(MapleEquipPosition.Shoes, pr.ReadInt());
                        items.Add(MapleEquipPosition.Weapon, pr.ReadInt());
                        break;
                    case JobType.Hayato:
                    case JobType.Kanna:
                        hairColor = pr.ReadInt();
                        pr.Skip(4); //skin color again
                        items.Add(MapleEquipPosition.Hat, pr.ReadInt());
                        items.Add(MapleEquipPosition.Top, pr.ReadInt());
                        items.Add(MapleEquipPosition.Shoes, pr.ReadInt());
                        items.Add(MapleEquipPosition.Weapon, pr.ReadInt());
                        break;
                    case JobType.Kaiser:
                    case JobType.AngelicBuster:
                        pr.Skip(4); //skin color again  
                        items.Add(MapleEquipPosition.Top, pr.ReadInt());
                        items.Add(MapleEquipPosition.Shoes, pr.ReadInt());
                        items.Add(MapleEquipPosition.Weapon, pr.ReadInt());
                        break;
                    case JobType.BeastTamer:
                        pr.Skip(4); //skin color again
                        faceMark = pr.ReadInt();
                        tamerEars = pr.ReadInt();
                        tamerTail = pr.ReadInt();
                        items.Add(MapleEquipPosition.Top, pr.ReadInt());
                        items.Add(MapleEquipPosition.Shoes, pr.ReadInt());
                        items.Add(MapleEquipPosition.Weapon, pr.ReadInt());
                        break;
                }

                if (CreateInfo.IsValidCharacter(jobType, gender, hair, face, items, hairColor, skinColor, faceMark, tamerEars, tamerTail))
                {
                    if (jobType == JobType.Xenon)
                        hair = (hair / 10) * 10; //male1 and female1 hair is 36487 and 37467 instead of 36480 and 37460 ...

                    if (jobType != JobType.Mihile) //Mihile hair color is already in the hair ID, but is still sent in the packet
                        hair += hairColor;

                    newCharacter = MapleCharacter.GetDefaultCharacter(c);
                    newCharacter.Name = name;
                    newCharacter.Gender = gender;
                    newCharacter.Skin = skinColor;
                    newCharacter.Face = face;
                    newCharacter.Hair = hair;
                    newCharacter.Job = CreateInfo.GetJobIdByJobType(jobType);
                    newCharacter.SubJob = subJob;
                    newCharacter.FaceMark = faceMark;
                    newCharacter.TamerEars = CreateInfo.GetTamerItemEffectId(tamerEars);
                    newCharacter.TamerTail = CreateInfo.GetTamerItemEffectId(tamerTail);
                    foreach (KeyValuePair<MapleEquipPosition, int> item in items)
                    {
                        WzEquip wzInfo = DataBuffer.GetEquipById(item.Value);
                        if (wzInfo != null)
                        {
                            MapleEquip equip = new MapleEquip(item.Value, "Character creation with jobId: " + jobId, position: (short)item.Key);
                            equip.SetDefaultStats(wzInfo);
                            newCharacter.Inventory.SetItem(equip, MapleInventoryType.Equipped, equip.Position, false);
                        }
                    }
                    if (basicKeyLayout)
                    {
                        newCharacter.SetKeyMap(GameConstants.DefaultBasicKeyBinds);
                        newCharacter.SetQuickSlotKeys(GameConstants.DefaultBasicQuickSlotKeyMap);
                    }
                    else
                    {
                        newCharacter.SetKeyMap(GameConstants.DefaultSecondaryKeyBinds);
                        newCharacter.SetQuickSlotKeys(GameConstants.DefaultSecondaryQuickSlotKeyMap);
                    }

                    CreateInfo.AddBeginnerJobSkills(newCharacter, jobType);
                    newCharacter.InsertCharacter();
                    newCharacter.Inventory.SaveToDatabase(true);
                    succesful = true;
                }
                else
                {
                    ServerConsole.Warning("Invalid item/hair/face ID on char creation, type: " + Enum.GetName(typeof(JobType), jobType));
                }
            }

            //AddCharacter response
            PacketWriter pw = new PacketWriter();
            pw.WriteHeader(SendHeader.AddCharacter);
            pw.WriteBool(!succesful); //0 = succesful, 1 = failed
            if (succesful)
            {
                MapleCharacter.AddCharEntry(pw, newCharacter);
            }
            c.SendPacket(pw);

            //cleanup
            newCharacter?.Release();
        }
    }

    internal static class CreateInfo
    {
        public static JobType GetJobTypeById(short id)
        {
            switch (id)
            {
                case 0:
                    return JobType.Explorer;
                case 1:
                    return JobType.DualBlade;
                case 508:
                    return JobType.Jett;
                case 1000:
                    return JobType.Cygnus;
                case 2000:
                    return JobType.Aran;
                case 2001:
                    return JobType.Evan;
                case 2002:
                    return JobType.Mercedes;
                case 2003:
                    return JobType.Phantom;
                case 2004:
                    return JobType.Luminous;
                case 2005:
                    return JobType.Shade;
                case 3000:
                    return JobType.Resistance;
                case 3001:
                    return JobType.Demon;
                case 3002:
                    return JobType.Xenon;
                case 4001:
                    return JobType.Hayato;
                case 4002:
                    return JobType.Kanna;
                case 5000:
                    return JobType.Mihile;
                case 6000:
                    return JobType.Kaiser;
                case 6001:
                    return JobType.AngelicBuster;
                case 10112:
                    return JobType.Zero;
                case 11000:
                    return JobType.BeastTamer;
                default:
                    ServerConsole.Debug("Unhandled job id found in makecharinfo: " + id);
                    return 0;
            }
        }

        public static short GetJobIdByJobType(JobType jobType)
        {
            switch (jobType)
            {
                case JobType.Ultimate:
                case JobType.Explorer:
                case JobType.Cannonneer:
                case JobType.Jett:
                case JobType.DualBlade:
                    return 0;
                case JobType.Cygnus:
                    return 1000;
                case JobType.Aran:
                    return 2000;
                case JobType.Evan:
                    return 2001;
                case JobType.Mercedes:
                    return 2002;
                case JobType.Phantom:
                    return 2003;
                case JobType.Luminous:
                    return 2004;
                case JobType.Resistance:
                    return 3000;
                case JobType.Demon:
                    return 3001;
                case JobType.Xenon:
                    return 3002;
                case JobType.Hayato:
                    return 4001;
                case JobType.Kanna:
                    return 4002;
                case JobType.Mihile:
                    return 5000;
                case JobType.Kaiser:
                    return 6000;
                case JobType.AngelicBuster:
                    return 6001;
                case JobType.Zero:
                    return 10000;
                case JobType.BeastTamer:
                    return 11000;
                default:
                    ServerConsole.Error(String.Format("Unhandled JobType: {0} in CreateCharHandler", jobType));
                    return 0;
            }
        }

        public static bool IsValidCharacter(JobType type, byte gender, int hair, int face, Dictionary<MapleEquipPosition, int> equips, int hairColor, int skinColor, int demonMark, int tamerEars, int tamerTail)
        {
            if (!IsValidId(type, gender, face, 0) || !IsValidId(type, gender, hair, 1))
                return false;
            try
            {
                switch (type)
                {
                    case JobType.Explorer:
                    case JobType.DualBlade:
                    case JobType.Cannonneer:
                    case JobType.Cygnus:
                        if (!IsValidId(type, gender, skinColor, 2) || !IsValidId(type, gender, equips[MapleEquipPosition.Top], 3) ||
                            !IsValidId(type, gender, equips[MapleEquipPosition.Shoes], 4) || !IsValidId(type, gender, equips[MapleEquipPosition.Weapon], 5))
                            return false;
                        break;
                    case JobType.Resistance:
                    case JobType.Aran:
                    case JobType.Evan:
                        if (!IsValidId(type, gender, hairColor, 2) || !IsValidId(type, gender, skinColor, 3) ||
                            !IsValidId(type, gender, equips[MapleEquipPosition.Top], 4) || !IsValidId(type, gender, equips[MapleEquipPosition.Bottom], 5) ||
                            !IsValidId(type, gender, equips[MapleEquipPosition.Shoes], 6) || !IsValidId(type, gender, equips[MapleEquipPosition.Weapon], 7))
                            return false;
                        break;
                    case JobType.Jett:
                        if (!IsValidId(type, gender, equips[MapleEquipPosition.Top], 2) || !IsValidId(type, gender, equips[MapleEquipPosition.Cape], 3) ||
                            !IsValidId(type, gender, equips[MapleEquipPosition.Shoes], 4) || !IsValidId(type, gender, equips[MapleEquipPosition.Weapon], 5))
                            return false;
                        break;
                    case JobType.Mercedes:
                        if (!IsValidId(type, gender, equips[MapleEquipPosition.Top], 2) || !IsValidId(type, gender, equips[MapleEquipPosition.Shoes], 3) ||
                            !IsValidId(type, gender, equips[MapleEquipPosition.Weapon], 4))
                            return false;
                        break;
                    case JobType.Phantom:
                    case JobType.Luminous:
                        if (!IsValidId(type, gender, skinColor, 2) ||
                            !IsValidId(type, gender, equips[MapleEquipPosition.Top], 3) || !IsValidId(type, gender, equips[MapleEquipPosition.Cape], 4) ||
                            !IsValidId(type, gender, equips[MapleEquipPosition.Shoes], 5) || !IsValidId(type, gender, equips[MapleEquipPosition.Weapon], 6))
                            return false;
                        break;
                    case JobType.Demon:
                        if (!IsValidId(type, gender, demonMark, 2) ||
                            !IsValidId(type, gender, equips[MapleEquipPosition.Top], 3) || !IsValidId(type, gender, equips[MapleEquipPosition.Shoes], 4) ||
                            !IsValidId(type, gender, equips[MapleEquipPosition.Weapon], 5) || !IsValidId(type, gender, equips[MapleEquipPosition.SecondWeapon], 6))
                            return false;
                        break;
                    case JobType.Xenon:
                        if (!IsValidId(type, gender, hairColor, 2) || !IsValidId(type, gender, skinColor, 3) ||
                            !IsValidId(type, gender, demonMark, 4) || !IsValidId(type, gender, equips[MapleEquipPosition.Top], 5) ||
                            !IsValidId(type, gender, equips[MapleEquipPosition.Shoes], 6) || !IsValidId(type, gender, equips[MapleEquipPosition.Weapon], 7))
                            return false;
                        break;
                    case JobType.Hayato:
                    case JobType.Kanna:
                        if (!IsValidId(type, gender, hairColor, 2) || !IsValidId(type, gender, skinColor, 3) ||
                            !IsValidId(type, gender, equips[MapleEquipPosition.Hat], 4) || !IsValidId(type, gender, equips[MapleEquipPosition.Top], 5) ||
                            !IsValidId(type, gender, equips[MapleEquipPosition.Shoes], 6) || !IsValidId(type, gender, equips[MapleEquipPosition.Weapon], 7))
                            return false;
                        break;
                    case JobType.Mihile:
                        if (!IsValidId(type, gender, skinColor, 3) ||
                            !IsValidId(type, gender, equips[MapleEquipPosition.Top], 4) || !IsValidId(type, gender, equips[MapleEquipPosition.Bottom], 5) ||
                            !IsValidId(type, gender, equips[MapleEquipPosition.Shoes], 6) || !IsValidId(type, gender, equips[MapleEquipPosition.Weapon], 7))
                            return false;
                        break;
                    case JobType.Kaiser:
                    case JobType.AngelicBuster:
                        if (!IsValidId(type, gender, skinColor, 2) || !IsValidId(type, gender, equips[MapleEquipPosition.Top], 3) ||
                            !IsValidId(type, gender, equips[MapleEquipPosition.Shoes], 4) || !IsValidId(type, gender, equips[MapleEquipPosition.Weapon], 5))
                            return false;
                        break;
                    case JobType.BeastTamer:
                        if (!IsValidId(type, gender, skinColor, 2) || !IsValidId(type, gender, demonMark, 3) ||
                            !IsValidId(type, gender, tamerEars, 4) || !IsValidId(type, gender, tamerTail, 5) ||
                            !IsValidId(type, gender, equips[MapleEquipPosition.Top], 6) || !IsValidId(type, gender, equips[MapleEquipPosition.Shoes], 7) ||
                            !IsValidId(type, gender, equips[MapleEquipPosition.Weapon], 8))
                            return false;
                        break;
                    default:
                        ServerConsole.Error("Could not validate newly created character because job " + type + " is not handled");
                        return false;
                }
            }
            catch
            {
                return false;
            }
            return true;
        }

        public static bool IsValidId(JobType type, byte gender, int id, int num)
        {
            WzMakeCharInfo info = DataBuffer.GetCharCreationInfo(type);
            if (gender == 0)
            {
                if (info.ChoosableGender == 3) //3 == female only
                    return false;
                List<int> validIds = info.MaleValues[num];
                if (!validIds.Contains(id))
                    return false;
            }
            else if (gender == 1)
            {
                if (info.ChoosableGender == 2) // 2 == male only
                    return false;
                List<int> validIds = info.FemaleValues[num];
                if (!validIds.Contains(id))
                    return false;
            }
            else
                return false;

            return true;
        }

        public static void AddBeginnerJobSkills(MapleCharacter chr, JobType type)
        {
            List<Skill> newSkills = new List<Skill>();
            switch (type)
            {
                case JobType.Cannonneer:
                    newSkills.Add(new Skill(Explorer.PIRATE_BLESSING, 0, 0));
                    newSkills.Add(new Skill(Explorer.MASTER_OF_SWIMMING, 0, 1));
                    newSkills.Add(new Skill(Explorer.MASTER_OF_ORGANIZATION, 0, 1));
                    chr.Inventory.EquipSlots += 12;
                    chr.Inventory.UseSlots += 12;
                    chr.Inventory.SetupSlots += 12;
                    chr.Inventory.EtcSlots += 12;
                    break;
                case JobType.Jett:
                    newSkills.Add(new Skill(Explorer.RETRO_ROCKETS, 0, 0));
                    break;
                case JobType.Mercedes:
                    newSkills.Add(new Skill(MercedesBasics.ELVEN_HEALING, 0, 1));
                    newSkills.Add(new Skill(MercedesBasics.ELVEN_BLESSING, 0, 0));
                    newSkills.Add(new Skill(MercedesBasics.UPDRAFT, 0, 1));
                    newSkills.Add(new Skill(MercedesBasics.ELVEN_GRACE, 0, 1));
                    break;
                case JobType.Phantom:
                    newSkills.Add(new Skill(PhantomBasics.PHANTOM_INSTINCT, 0, 0));
                    newSkills.Add(new Skill(PhantomBasics.DEXTEROUS_TRAINING, 0, 1));
                    break;
                case JobType.Luminous:
                    newSkills.Add(new Skill(LuminousBasics.INNER_LIGHT, 0, 1));
                    newSkills.Add(new Skill(LuminousBasics.LIGHT_WASH, 0, 0));
                    newSkills.Add(new Skill(LuminousBasics.SUNFIRE, 0, 1));
                    newSkills.Add(new Skill(LuminousBasics.ECLIPSE, 0, 1));
                    newSkills.Add(new Skill(LuminousBasics.CHANGE_LIGHT_DARK_MODE, 0, 1));
                    newSkills.Add(new Skill(LuminousBasics.FLASH_BLINK, 0, 1));
                    break;
                case JobType.Demon:
                    newSkills.Add(new Skill(DemonBasics.DARK_WINDS, 0, 1));
                    newSkills.Add(new Skill(DemonBasics.DEMONIC_BLOOD, 0, 1));
                    break;
                case JobType.Hayato:
                    newSkills.Add(new Skill(HayatoBasics.MASTER_OF_BLADES, 0, 1));
                    newSkills.Add(new Skill(HayatoBasics.KEEN_EDGE, 0, 0));
                    newSkills.Add(new Skill(HayatoBasics.SHIMADA_HEART, 0, 1));
                    newSkills.Add(new Skill(HayatoBasics.SUMMER_RAIN, 0, 1));
                    break;
                case JobType.Kanna:
                    newSkills.Add(new Skill(KannaBasics.ELEMENTAL_BLESSING, 1, 1));
                    newSkills.Add(new Skill(KannaBasics.MANA_FONT, 1, 1));
                    newSkills.Add(new Skill(KannaBasics.HAKU, 1, 1));
                    newSkills.Add(new Skill(KannaBasics.NINE_TAILED_FURY, 1, 1));
                    newSkills.Add(new Skill(KannaBasics.ELEMENTALISM_LINK_SKILL, 3, 0));
                    break;
                case JobType.Mihile:
                    newSkills.Add(new Skill(MihileBasics.KNIGHTS_WATCH, 1, 1));
                    break;
                case JobType.Xenon:
                    newSkills.Add(new Skill(XenonBasics.SUPPLY_SURPLUS, 1, 1));
                    newSkills.Add(new Skill(XenonBasics.HYBRID_LOGIC, 1, 1));
                    newSkills.Add(new Skill(XenonBasics.MULTILATERAL_I, 1, 1));
                    newSkills.Add(new Skill(XenonBasics.MIMIC_PROTOCOL, 1, 1));
                    break;
                case JobType.Kaiser:
                    newSkills.Add(new Skill(KaiserBasics.REALIGN_DEFENDER_MODE, 1, 1));
                    newSkills.Add(new Skill(KaiserBasics.REALIGN_ATTACKER_MODE, 1, 1));
                    newSkills.Add(new Skill(KaiserBasics.VERTICAL_GRAPPLE, 1, 1));
                    newSkills.Add(new Skill(KaiserBasics.TRANSFIGURATION, 1, 1));
                    newSkills.Add(new Skill(KaiserBasics.IRON_WILL, 3, 1));
                    break;
                case JobType.AngelicBuster:
                    newSkills.Add(new Skill(AngelicBusterBasics.SOUL_BUSTER, 1, 1));
                    newSkills.Add(new Skill(AngelicBusterBasics.GRAPPLING_HEART, 1, 1));
                    newSkills.Add(new Skill(AngelicBusterBasics.TERMS_AND_CONDITIONS, 3, 1));
                    newSkills.Add(new Skill(AngelicBusterBasics.HYPER_COORDINATE, 1, 1));
                    newSkills.Add(new Skill(AngelicBusterBasics.DRESS_UP, 1, 1));
                    break;
                case JobType.Ultimate:
                    //TODO: +10 level equip skill, id = 80 ?
                    break;
            }
            newSkills.Add(new Skill(UniversalBeginner.NEBULITE_FUSION, 1, 1)); //nebulite fusion
            if (newSkills.Count > 0)
                chr.AddSkills(newSkills, false);
        }

        public static int GetTamerItemEffectId(int itemId)
        {
            switch (itemId)
            {
                case 1004062: //brown ears
                    return 5010116;
                case 1004063: //white ears
                    return 5010117;
                case 1004064: //black ears
                    return 5010118;
                case 1102661: //brown tail
                    return 5010119;
                case 1102662: //white tail
                    return 5010120;
                case 1102663: //black tail
                    return 5010121;
            }
            return 0;
        }
    }

    public enum JobType
    {
        Ultimate = -1,
        Resistance,
        Explorer,
        Cygnus,
        Aran,
        Evan,
        Mercedes,
        Demon,
        Phantom,
        DualBlade,
        Mihile,
        Luminous,
        Kaiser,
        AngelicBuster,
        Cannonneer,
        Xenon,
        Zero,
        Shade,
        Jett,
        Hayato,
        Kanna,
        BeastTamer
    }
}


