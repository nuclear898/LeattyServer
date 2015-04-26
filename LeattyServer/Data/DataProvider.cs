using LeattyServer.Data.WZ;
using LeattyServer.Helpers;
using LeattyServer.ServerInfo;
using LeattyServer.ServerInfo.Map.Monster;
using Microsoft.CSharp;
using reNX;
using reNX.NXProperties;
using System;
using System.Linq;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Reflection;
using LeattyServer.ServerInfo.Player;
using System.Globalization;
using System.Diagnostics;
using System.Text;
using LeattyServer.ServerInfo.Inventory;
using LeattyServer.Constants;
using System.Threading;
using LeattyServer.Scripting;
using LeattyServer.ServerInfo.Packets.Handlers;

namespace LeattyServer.Data
{
    public static class DataProvider
    {
        #region Wz
        public static int LoadEquips(string path)
        {
            NXFile file = new NXFile(path);
            int ret = 0;
            foreach (NXNode folder in file.BaseNode)
            {
                foreach (NXNode baseNode in folder)
                {
                    if (!(baseNode.Name.StartsWith("0") || baseNode.Name == "Hair" || baseNode.Name == "Face" ||
                        baseNode.Name == "Afterimage"))
                        continue;
                    if (!baseNode.ContainsChild("info")) //If not, its a statless item, why even bother to load it
                        continue;
                    NXNode info = baseNode.GetChild("info");
                    WzEquip item = new WzEquip();
                    int itemId = int.Parse(baseNode.Name.Replace(".img", string.Empty));
                    item.ItemId = itemId;
                    item.ReqJob = (byte)GetIntFromChild(info, "reqJob");
                    item.ReqLevel = (byte)GetIntFromChild(info, "reqLevel");
                    item.ReqFame = GetIntFromChild(info, "reqPOP");
                    item.ReqStr = (short)GetIntFromChild(info, "reqSTR");
                    item.ReqDex = (short)GetIntFromChild(info, "reqDEX");
                    item.ReqInt = (short)GetIntFromChild(info, "reqINT");
                    item.ReqLuk = (short)GetIntFromChild(info, "reqLUK");
                    item.IncStr = (short)GetIntFromChild(info, "incSTR");
                    item.IncDex = (short)GetIntFromChild(info, "incDEX");
                    item.IncInt = (short)GetIntFromChild(info, "incINT");
                    item.IncLuk = (short)GetIntFromChild(info, "incLUK");
                    item.IncPad = (short)GetIntFromChild(info, "incPAD");
                    item.IncMad = (short)GetIntFromChild(info, "incMAD");
                    item.IncPdd = (short)GetIntFromChild(info, "incPDD");
                    item.IncMdd = (short)GetIntFromChild(info, "incMDD");
                    item.IncMhp = (short)GetIntFromChild(info, "incMHP");
                    item.IncMmp = (short)GetIntFromChild(info, "incMMP");
                    item.IncAcc = (short)GetIntFromChild(info, "incACC");
                    item.IncEva = (short)GetIntFromChild(info, "incEVA");
                    item.IncSpeed = (short)GetIntFromChild(info, "incSpeed");
                    item.IncJump = (short)GetIntFromChild(info, "incJump");
                    item.TotalUpgradeCount = (byte)GetIntFromChild(info, "tuc");
                    item.EquipTradeBlock = GetIntFromChild(info, "equipTradeBlock") == 1;
                    //item.AttackSpeed = (byte)GetIntFromChild(info, "attackSpeed");
                    //item.Knockback = GetIntFromChild(info, "knockback");
                    item.SetBonusId = GetIntFromChild(info, "setItemID");
                    item.SpecialId = GetIntFromChild(info, "specialID");
                    LoadDefaultItemAttributes(item, info);
                    if (!DataBuffer.EquipBuffer.ContainsKey(itemId))
                    {
                        DataBuffer.EquipBuffer.Add(itemId, item);
                    }
                    ret++;
                }
            }
            file.Dispose();
            return ret;
        }

        public static int LoadItems(string path)
        {
            NXFile file = new NXFile(path);
            int ret = 0;
            
            foreach (NXNode baseNode in file.BaseNode)
            {
                switch (baseNode.Name)
                {
                    case "ItemOption.img":  //Potentials
                    {
                        Dictionary<int, WzItemOption> potentials = new Dictionary<int, WzItemOption>();                        
                        foreach (NXNode potentialNode in baseNode)
                        {
                            WzItemOption potential = new WzItemOption();
                            potential.Id = int.Parse(potentialNode.Name);
                            NXNode infoNode = potentialNode.GetChild("info");
                            if (infoNode.ContainsChild("optionBlock")) //Some pots that make you do face expressions...
                                continue;
                            potential.ReqLevel = (byte)GetIntFromChild(infoNode, "reqLevel");
                            potential.OptionType = GetIntFromChild(infoNode, "optionType");
                            potential.Text = GetStringFromChild(infoNode, "string");
                            if (potential.Text.Contains("skill") || potential.Text.Contains("invincible") || potential.Text.Contains("chance to reflect")) //Todo: handling for these                       
                                continue;
                            NXNode levelsNode = potentialNode.GetChild("level");
                            Dictionary<byte, Dictionary<string, int>> levels = new Dictionary<byte, Dictionary<string, int>>();
                            foreach (NXNode levelNode in levelsNode)
                            {
                                byte level = byte.Parse(levelNode.Name);
                                if (level < potential.ReqLevel / 10) //Why bother?
                                    continue;                             
                                Dictionary<string, int> attributes = new Dictionary<string, int>();
                                foreach (NXNode attributeNode in levelNode)
                                {
                                    string attributeName = attributeNode.Name;
                                    int value = (int)attributeNode.ValueOrDie<long>();
                                    if (value > 0)
                                        attributes.Add(attributeName, value);
                                }
                                if (attributes.Any())
                                    levels.Add(level, attributes);
                            }
                            potential.LevelStats = levels;
                            potentials.Add(potential.Id, potential);
                        }
                        DataBuffer.PotentialBuffer = potentials;
                        break;
                    }
                    case "Cash":
                    case "Consume":
                    case "Etc":
                    case "Install":
                    {
                        foreach (NXNode imgNode in baseNode)
                        {
                            foreach (NXNode subNode in imgNode)
                            {
                                if (!subNode.ContainsChild("info"))
                                    continue;
                                int itemId = int.Parse(subNode.Name);
                                if (DataBuffer.ItemBuffer.ContainsKey(itemId))
                                    continue;
                                NXNode infoNode = subNode.GetChild("info");
                                WzItem item;
                                if (subNode.ContainsChild("spec"))
                                {
                                    NXNode spec = subNode.GetChild("spec");
                                    WzConsume consume = new WzConsume();
                                    item = consume;
                                    consume.Hp = GetIntFromChild(spec, "hp");
                                    consume.Mp = GetIntFromChild(spec, "mp");
                                    consume.HpR = (byte)GetIntFromChild(spec, "hpR");
                                    consume.MpR = (byte)GetIntFromChild(spec, "mpR");
                                    consume.Speed = GetIntFromChild(spec, "speed");
                                    consume.Time = GetIntFromChild(spec, "time");

                                    consume.CharismaExp = GetIntFromChild(spec, "charismaEXP");
                                    consume.CharmExp = GetIntFromChild(spec, "charmEXP");
                                    consume.CraftExp = GetIntFromChild(spec, "craftEXP");
                                    consume.InsightExp = GetIntFromChild(spec, "insightEXP");
                                    consume.SenseExp = GetIntFromChild(spec, "senseEXP");
                                    consume.WillExp = GetIntFromChild(spec, "willEXP");

                                    consume.MoveTo = GetIntFromChild(spec, "moveTo");
                                }
                                else if (ItemConstants.GetMapleItemType(itemId) == MapleItemType.RegularEquipScroll)
                                {
                                    Dictionary<string, int> statEnhances = new Dictionary<string, int>();
                                    int success = GetIntFromChild(infoNode, "success");
                                    List<int> itemIdsApplicableTo = new List<int>();
                                    foreach (var node in infoNode)
                                    {
                                        /*if (!scrollElements.Contains(node.Name))
                                            scrollElements.Add(node.Name);*/
                                        if (scrollTypes.Contains(node.Name))
                                        {
                                            statEnhances.Add(node.Name, GetIntFromNode(node));
                                        }
                                        if (node.Name == "specialItem")
                                        {
                                            itemIdsApplicableTo.Add((int)node.ValueOrDie<Int64>());
                                        }
                                    }
                                    if (subNode.ContainsChild("req"))
                                    {
                                        foreach (var reqNode in subNode.GetChild("req"))
                                        {
                                            itemIdsApplicableTo.Add((int)reqNode.ValueOrDie<Int64>());
                                        }
                                    }
                                    item = new WzItemEnhancer(success, statEnhances, itemIdsApplicableTo);
                                }
                                else
                                {
                                    item = new WzItem();
                                }
                                item.ItemId = itemId;
                                MapleInventoryType type = ItemConstants.GetInventoryType(itemId);
                                bool stackable = type == MapleInventoryType.Use || type == MapleInventoryType.Etc;
                                LoadDefaultItemAttributes(item, infoNode, stackable ? 200 : 1);
                                DataBuffer.ItemBuffer.Add(itemId, item);
                                ret++;
                            }
                        }
                        break;
                    }
                    case "Pet":
                    {
                        foreach (NXNode imgNode in baseNode)
                        {
                            if (!imgNode.ContainsChild("info"))
                                continue;
                            int itemId = int.Parse(imgNode.Name.Replace(".img", string.Empty));
                            NXNode infoNode = imgNode.GetChild("info");
                            WzPet pet = new WzPet();
                            pet.ItemId = itemId;
                            pet.Hungry = GetIntFromChild(infoNode, "hungry");
                            pet.Life = GetIntFromChild(infoNode, "life");
                            pet.MultiPet = GetIntFromChild(infoNode, "multiPet") == 1;
                            pet.PickUpItem = GetIntFromChild(infoNode, "pickupItem") == 1;
                            pet.AutoBuff = GetIntFromChild(infoNode, "autoBuff") == 1;
                            pet.Permanent = GetIntFromChild(infoNode, "permanent") == 1;
                            LoadDefaultItemAttributes(pet, infoNode);
                            DataBuffer.ItemBuffer.Add(itemId, pet);
                            ret++;
                        }
                        break;
                    }
                }
            }
            return ret;
        }

        private static HashSet<string> scrollTypes = new HashSet<string>
        {
            "incPDD",
            "incMDD",
            "incACC",
            "incMHP",
            "incINT",
            "incDEX",
            "incMAD",
            "incPAD",
            "incEVA",
            "incLUK",
            "incMMP",
            "incSTR",
            "incSpeed",
            "incJump",
            "incCraft",
            "cursed",
            "reqCUC",
            "reqRUC", //
            "tuc", //uses more upgrade count than normal
            "preventslip", //spike shoes
            "warmsupport", //cold resistance
            "reqEquipLevelMax",
            "reqEquipLevelMin",
            "recover", //clean slate
            "randstat", //chaos scroll randomize
            "incRandVol", //higher chaos scrol; stats
            "noNegative", //chaos scroll only adds stats
            "forceUpgrade", //adds enhancements
            "reset", //Innocence scrolls
            "perfectReset", //Perfect Innocence scrolls
        };
        //todo:
        /*
        enchantCategory //Roora core, Bing machine/alpha and visitor equipment?
        lvOptimum //Doesn't do anything?
        lvRange //Doesn't do anything?        
        incPVPDamage //Not doing pvp for now
        noCursed //Item not destroyed on failure, isn't that default though?
        path //extra UI window for stuff like bonus pot
        additionalSuccess //comes on bonus pot stamps but is always 0
        type //something with Jewel Recipe Synergizer
        level //value = tree, Gives more stats based on the item's growth level
        skill //value = int skillId, Allows the use of a skill

        rebirth flames stuff: not really important
        exGrade
        exGradeWeight    
        createType 
        cuttable 
        exNew    
             
        setItemCategory //adds a zero weapon from this item set Id
        resetRUC //Only one item uses this and it's completely useless, resets upgradecount on Carlton's Mustache...
        */

        static void LoadDefaultItemAttributes(WzItem item, NXNode infoNode, int defaultSlotMax = 1)
        {
            item.Only = GetIntFromChild(infoNode, "only") == 1;
            item.NotSale = GetIntFromChild(infoNode, "notSale") == 1;
            item.SlotMax = GetIntFromChild(infoNode, "slotMax", item.Only ? 1 : defaultSlotMax);
            item.TradeBlock = GetIntFromChild(infoNode, "tradeBlock") == 1;
            item.IsQuestItem = GetIntFromChild(infoNode, "quest") == 1;
            item.Price = GetIntFromChild(infoNode, "price");
            item.AccountShareable = GetIntFromChild(infoNode, "accountSharable") == 1;
            item.IsCashItem = GetIntFromChild(infoNode, "cash") == 1;
        }

        public static int LoadMobSkills(string path)
        {
            NXFile file = new NXFile(path);
            int ret = 0;

            NXNode node = file.BaseNode.GetChild("MobSkill.img");
            foreach (NXNode skill in node)
            {
                NXNode Levels = skill.GetChild("level");
                foreach (NXNode level in Levels)
                {
                    MobSkill mobskill = new MobSkill(int.Parse(skill.Name), int.Parse(level.Name));
                    mobskill.summonOnce = GetIntFromChild(level, "summonOnce", 0) != 0;

                    if (!level.ContainsChild("lt"))
                        mobskill.lt = Point.Empty;
                    else
                        mobskill.lt = level.GetChild("lt").ValueOrDefault<Point>(Point.Empty);

                    if (!level.ContainsChild("rb"))
                        mobskill.rb = Point.Empty;
                    else
                        mobskill.rb = level.GetChild("rb").ValueOrDefault<Point>(Point.Empty);

                    mobskill.limit = (short)GetIntFromChild(level, "limit");
                    mobskill.prop = GetIntFromChild(level, "prop");
                    mobskill.interval = GetIntFromChild(level, "interval", 0);
                    mobskill.time = GetIntFromChild(level, "time", 0);
                    mobskill.x = GetIntFromChild(level, "x", 0);
                    mobskill.y = GetIntFromChild(level, "y", 0);
                    mobskill.summonEffect = GetIntFromChild(level, "summonEffect", 0);
                    mobskill.hp = GetIntFromChild(level, "hp", 0);
                    mobskill.mpCon = GetIntFromChild(level, "mpCon", 0);
                    MobSkill.SetSkill(mobskill);
                    ret++;
                }
            }
            return ret;
        }
        public static int LoadMobs(string path)
        {
            NXFile file = new NXFile(path);
            int ret = 0;
            foreach (NXNode imgNode in file.BaseNode)
            {
                if (!(imgNode.ContainsChild("info") && imgNode.Name.Contains(".img")))
                    continue;
                int start = 0;
                if (imgNode.Name.StartsWith("0")) start = 1;
                int MobId = int.Parse(imgNode.Name.Substring(start, 7 - start));
                if (DataBuffer.MobBuffer.ContainsKey(MobId))
                    continue;
                NXNode Info = imgNode.GetChild("info");
                WzMob Mob = new WzMob();
                Mob.MobId = MobId;
                Mob.Level = GetIntFromChild(Info, "level");
                Mob.HP = GetIntFromChild(Info, "maxHP");
                Mob.MP = GetIntFromChild(Info, "maxMP");
                Mob.Speed = GetIntFromChild(Info, "speed");
                Mob.Kb = GetIntFromChild(Info, "pushed");
                Mob.PAD = GetIntFromChild(Info, "PADamage");
                Mob.PDD = GetIntFromChild(Info, "PDDamage");
                Mob.PDRate = GetIntFromChild(Info, "PDRate");
                Mob.MAD = GetIntFromChild(Info, "MADamage");
                Mob.MDD = GetIntFromChild(Info, "MDDamage");
                Mob.MDRate = GetIntFromChild(Info, "MDRate");
                Mob.Eva = GetIntFromChild(Info, "eva");
                Mob.Acc = GetIntFromChild(Info, "acc");
                Mob.Exp = GetIntFromChild(Info, "exp");
                Mob.SummonType = GetIntFromChild(Info, "summonType");
                Mob.Invincible = GetIntFromChild(Info, "invincible");
                Mob.FixedDamage = GetIntFromChild(Info, "fixedDamage");
                Mob.FFALoot = GetIntFromChild(Info, "publicReward") > 0;
                Mob.ExplosiveReward = GetIntFromChild(Info, "explosiveReward") > 0;
                Mob.IsBoss = GetIntFromChild(Info, "boss") > 0;
                if (Info.ContainsChild("skill"))
                {
                    NXNode skill = Info.GetChild("skill");
                    for (int i = 0; skill.ContainsChild(i.ToString()); i++)
                    {
                        NXNode child = skill.GetChild(i.ToString());
                        Mob.Skills.Add(MobSkill.GetSkill(GetIntFromChild(child, "skill"), GetIntFromChild(child, "level")));
                    }
                }
                DataBuffer.MobBuffer.Add(MobId, Mob);
                ret++;
            }
            file.Dispose();
            return ret;
        }

        #region Maps
        public static int LoadMaps(String path)
        {
            NXFile file = new NXFile(path);

            List<ManualResetEvent> waitHandles = new List<ManualResetEvent>();
            foreach (NXNode mapNode in file.BaseNode.GetChild("Map"))
            {
                if (!mapNode.Name.StartsWith("Map"))
                    continue;
                ManualResetEvent resetEvent = new ManualResetEvent(false);
                waitHandles.Add(resetEvent);
                NXNodeResetEventWrapper wrap = new NXNodeResetEventWrapper(mapNode, resetEvent);
                ThreadPool.QueueUserWorkItem(new WaitCallback(LoadMapImg), wrap);

            }
            foreach (ManualResetEvent r in waitHandles)
                r.WaitOne();
            file.Dispose();
            return DataBuffer.MapBuffer.Count;
        }

        private static void LoadMapImg(object r)
        {
            NXNodeResetEventWrapper wrap = (NXNodeResetEventWrapper)r;
            NXNode mapNode = wrap.NxNode;
            foreach (NXNode mapImg in mapNode)
            {
                int mapId = 0;
                if (!int.TryParse(mapImg.Name.Replace(".img", String.Empty), out mapId))
                    continue;
                if (DataBuffer.MapBuffer.ContainsKey(mapId))
                    continue;
                WzMap map = new WzMap();
                map.MapId = mapId;
                NXNode info;
                if (mapImg.ContainsChild("info"))
                {
                    info = mapImg.GetChild("info");
                    if (info.ContainsChild("link")) //Linked map aka timed mini dungeon, we dont need that to load
                        continue;
                    map.Town = GetIntFromChild(info, "town") == 1;

                    map.FieldType = GetIntFromChild(info, "fieldType");
                    map.FieldScript = GetStringFromChild(info, "fieldScript");
                    map.FirstUserEnter = GetStringFromChild(info, "onFirstUserEnter");
                    map.UserEnter = GetStringFromChild(info, "onUserEnter");
                    map.Fly = GetIntFromChild(info, "fly");
                    map.Swim = GetIntFromChild(info, "swim");
                    map.ForcedReturn = GetIntFromChild(info, "forcedReturn");
                    map.ReturnMap = GetIntFromChild(info, "returnMap");
                    map.TimeLimit = GetIntFromChild(info, "timeLimit");
                    map.MobRate = GetDoubleFromChild(info, "mobRate");
                    map.Limit = (WzMap.FieldLimit)GetIntFromChild(info, "fieldLimit");
                }
                if (mapImg.ContainsChild("ladderRope"))
                {
                    info = mapImg.GetChild("ladderRope");
                    foreach (NXNode ChildNode in info)
                    {
                        WzMap.LadderRope lr = new WzMap.LadderRope();
                        lr.StartPoint = new Point(GetIntFromChild(ChildNode, "x"), GetIntFromChild(ChildNode, "y1"));
                        lr.EndPoint = new Point(GetIntFromChild(ChildNode, "x"), GetIntFromChild(ChildNode, "y2"));
                        map.LaderRopes.Add(lr);
                    }
                }
                if (mapImg.ContainsChild("life"))
                {
                    info = mapImg.GetChild("life");
                    foreach (NXNode ChildNode in info)
                    {
                        string Type = GetStringFromChild(ChildNode, "type");
                        if (Type == "m")
                        {
                            WzMap.MobSpawn mobSpawn = new WzMap.MobSpawn();
                            mobSpawn.MobId = GetIntFromChild(ChildNode, "id");
                            mobSpawn.wzMob = DataBuffer.GetMobById(mobSpawn.MobId);
                            if (mobSpawn.wzMob == null)
                            {
                                ServerConsole.Error("WzMob not found for mob " + mobSpawn.MobId + " on map " + map.MapId);
                            }
                            mobSpawn.Position = new Point(GetIntFromChild(ChildNode, "x"), GetIntFromChild(ChildNode, "y"));
                            int mobTime = GetIntFromChild(ChildNode, "mobTime");
                            mobSpawn.MobTime = mobTime < 0 ? -1 : mobTime * 1000; //mobTime is in seconds in the .WZ
                            mobSpawn.Rx0 = (short)GetIntFromChild(ChildNode, "rx0");
                            mobSpawn.Rx1 = (short)GetIntFromChild(ChildNode, "rx1");
                            mobSpawn.Cy = (short)GetIntFromChild(ChildNode, "cy");
                            mobSpawn.Fh = (short)GetIntFromChild(ChildNode, "fh");
                            int F = GetIntFromChild(ChildNode, "f");
                            mobSpawn.F = (F == 1 ? false : true);
                            int Hide = GetIntFromChild(ChildNode, "hide");
                            mobSpawn.Hide = (Hide == 1);
                            map.MobSpawnPoints.Add(mobSpawn);
                        }
                        else
                        {
                            WzMap.Npc Npc = new WzMap.Npc();
                            Npc.Id = GetIntFromChild(ChildNode, "id");
                            Npc.x = (short)GetIntFromChild(ChildNode, "x");
                            Npc.y = (short)GetIntFromChild(ChildNode, "y");
                            Npc.Rx0 = (short)GetIntFromChild(ChildNode, "rx0");
                            Npc.Rx1 = (short)GetIntFromChild(ChildNode, "rx1");
                            Npc.Cy = (short)GetIntFromChild(ChildNode, "cy");
                            Npc.Fh = (short)GetIntFromChild(ChildNode, "fh");
                            int F = GetIntFromChild(ChildNode, "f");
                            Npc.F = (F == 1 ? false : true);
                            int Hide = GetIntFromChild(ChildNode, "hide");
                            if (ChildNode.ContainsChild("limitedname")) //No special event npcs for now
                                Hide = 1;
                            Npc.Hide = (Hide == 1);
                            map.Npcs.Add(Npc);
                        }
                    }
                }

                if (mapImg.ContainsChild("portal"))
                {
                    info = mapImg.GetChild("portal");

                    byte townPortal = 0x80;

                    foreach (NXNode childNode in info)
                    {
                        byte portalId = byte.Parse(childNode.Name);
                        WzMap.Portal portal = new WzMap.Portal();
                        portal.Id = portalId;
                        portal.Type = (WzMap.PortalType)GetIntFromChild(childNode, "pt");
                        if (portal.Type == WzMap.PortalType.TownportalPoint)
                        {
                            portal.Id = townPortal;
                            townPortal++;
                        }
                        portal.Position = new Point(GetIntFromChild(childNode, "x"), GetIntFromChild(childNode, "y"));
                        portal.ToMap = GetIntFromChild(childNode, "tm");
                        portal.Name = GetStringFromChild(childNode, "pn");

                        portal.ToName = GetStringFromChild(childNode, "tn");
                        portal.Script = GetStringFromChild(childNode, "script");
                        if (!map.Portals.ContainsKey(portal.Name))
                        {
                            map.Portals.Add(portal.Name, portal);
                        }
                    }
                }
                short topBound = 0;
                short bottomBound = 0;
                short leftBound = 0;
                short rightBound = 0;

                List<WzMap.FootHold> footHolds = new List<WzMap.FootHold>();

                if (mapImg.ContainsChild("foothold"))
                {
                    foreach (NXNode fhRoot in mapImg.GetChild("foothold"))
                    {
                        foreach (NXNode fhCat in fhRoot)
                        {
                            foreach (NXNode fh in fhCat)
                            {
                                WzMap.FootHold footHold = new WzMap.FootHold();

                                footHold.Id = short.Parse(fh.Name);
                                footHold.Next = (short)GetIntFromChild(fh, "next");
                                footHold.Prev = (short)GetIntFromChild(fh, "prev");
                                Point p1 = new Point(GetIntFromChild(fh, "x1"), GetIntFromChild(fh, "y1"));
                                Point p2 = new Point(GetIntFromChild(fh, "x2"), GetIntFromChild(fh, "y2"));
                                footHold.Point1 = p1;
                                footHold.Point2 = p2;
                                footHolds.Add(footHold);

                                if (p1.X < leftBound)
                                    leftBound = (short)p1.X;
                                if (p2.X < leftBound)
                                    leftBound = (short)p2.X;

                                if (p1.X > rightBound)
                                    rightBound = (short)p1.X;
                                if (p2.X > rightBound)
                                    rightBound = (short)p2.X;

                                if (p1.Y > bottomBound)
                                    bottomBound = (short)p1.Y;
                                if (p2.Y > bottomBound)
                                    bottomBound = (short)p2.Y;

                                if (p1.Y < topBound)
                                    topBound = (short)p1.Y;
                                if (p2.Y < topBound)
                                    topBound = (short)p2.Y;

                            }
                        }
                    }
                }
                map.FootHolds = footHolds;

                map.TopBorder = topBound;
                map.BottomBorder = bottomBound;
                map.LeftBorder = leftBound;
                map.RightBorder = rightBound;

                if (mapImg.ContainsChild("reactor"))
                {
                    info = mapImg.GetChild("reactor");
                    foreach (NXNode childNode in info)
                    {
                        WzMap.Reactor Reactor = new WzMap.Reactor();
                        Reactor.Position = new Point(GetIntFromChild(childNode, "x"), GetIntFromChild(childNode, "y"));
                        Reactor.Id = GetIntFromChild(childNode, "id");
                        Reactor.ReactorTime = GetIntFromChild(childNode, "reactorTime");
                        map.Reactors.Add(Reactor);
                    }
                }

                map.Reactors.AddRange(GenerateRandomVeins(map));
                lock (DataBuffer.MapBuffer)
                {
                    DataBuffer.MapBuffer.Add(mapId, map);
                }
            }
            wrap.ResetEvent.Set();
        }
        #endregion

        #region Veins
        private static List<WzMap.Reactor> GenerateRandomVeins(WzMap map)
        {
            List<WzMap.Reactor> Veins = new List<WzMap.Reactor>();
            if (map.Town || map.MobSpawnPoints.Count == 0) return Veins;
            //Herbs and ores calculations
            int avgMobLvl = (int)Math.Floor(map.MobSpawnPoints.Average(x => x.wzMob.Level));
            Dictionary<int, int> Ores = new Dictionary<int, int>();
            Dictionary<int, int> Herbs = new Dictionary<int, int>();
            if (avgMobLvl > 0 && avgMobLvl <= 60)
            {
                Ores.Add(200000, 400);
                Ores.Add(200001, 400);
                Ores.Add(200002, 120);
                Ores.Add(200003, 80);

                Herbs.Add(100000, 400);
                Herbs.Add(100001, 400);
                Herbs.Add(100002, 120);
                Herbs.Add(100003, 80);
            }
            else if (avgMobLvl > 60 && avgMobLvl <= 120)
            {
                Ores.Add(200002, 400);
                Ores.Add(200003, 300);
                Ores.Add(200004, 200);
                Ores.Add(200005, 100);

                Herbs.Add(100002, 400);
                Herbs.Add(100003, 300);
                Herbs.Add(100004, 200);
                Herbs.Add(100005, 100);
            }
            else if (avgMobLvl > 120 && avgMobLvl <= 150)
            {
                Ores.Add(200005, 200);
                Ores.Add(200006, 200);
                Ores.Add(200007, 200);
                Ores.Add(200008, 150);
                Ores.Add(200009, 150);
                Ores.Add(200011, 100);

                Herbs.Add(100005, 200);
                Herbs.Add(100006, 200);
                Herbs.Add(100007, 200);
                Herbs.Add(100008, 150);
                Herbs.Add(100009, 150);
                Herbs.Add(100011, 100);
            }
            else if (avgMobLvl > 150)
            {
                Ores.Add(200008, 200);
                Ores.Add(200009, 200);
                Ores.Add(200011, 200);
                Ores.Add(200012, 200);
                Ores.Add(200013, 200);

                Herbs.Add(100008, 200);
                Herbs.Add(100009, 200);
                Herbs.Add(100011, 200);
                Herbs.Add(100012, 200);
                Herbs.Add(100013, 200);
            }

            //MapId as seed to make the positions and spawn seem static per map
            List<int> PassedFhs = new List<int>();
            Random RandomCalc = new Random(map.MapId);
            for (int i = 0; i < 4; i++)
            {
                WzMap.Reactor RandomOre = new WzMap.Reactor();
                WzMap.Reactor RandomHerb = new WzMap.Reactor();

                //Todo: fix near edge positions
                if (i + 1 == map.MobSpawnPoints.Count || PassedFhs.Count == map.MobSpawnPoints.Count) break;
                int RandomFH = RandomCalc.Next(0, map.MobSpawnPoints.Count);
                for (int x = 0; PassedFhs.Contains(RandomFH); x++)
                {
                    RandomFH = RandomCalc.Next(0, map.MobSpawnPoints.Count);
                    if (x == 10) RandomFH = -1;
                }
                if (RandomFH == -1) break;
                WzMap.FootHold OreFh = GetFootHoldBelow(map.MobSpawnPoints[RandomFH].Position, map);

                RandomFH = RandomCalc.Next(0, map.MobSpawnPoints.Count);
                for (int x = 0; PassedFhs.Contains(RandomFH); x++)
                {
                    RandomFH = RandomCalc.Next(0, map.MobSpawnPoints.Count);
                    if (x == 10) RandomFH = -1;
                }
                if (RandomFH == -1) break;
                WzMap.FootHold HerbFh = GetFootHoldBelow(map.MobSpawnPoints[RandomFH].Position, map);

                Dictionary<int, int> Temp = new Dictionary<int, int>();
                if (OreFh == null && HerbFh == null) continue;
                if (OreFh != null)
                {
                    Point RandomPos = OreFh.Point1;
                    RandomOre.Position = RandomPos;
                    PassedFhs.Add(RandomFH);

                    int OreTotal = Ores.Sum(x => x.Value);
                    int prev = 0;
                    foreach (KeyValuePair<int, int> Ore in Ores)
                    {
                        Temp.Add(Ore.Key, Ore.Value + prev);
                        prev += Ore.Value;
                    }
                    Ores = Temp;

                    int RandomOreId = RandomCalc.Next(0, OreTotal);
                    RandomOre.Id = Ores.FirstOrDefault(x => x.Value >= RandomOreId).Key;

                    int RandomOreReactorTime = 5;
                    switch (RandomOre.Id % 100000)
                    {
                        case 6:
                        case 7:
                        case 8:
                        case 9:
                        RandomOreReactorTime = 120;
                        break;
                        case 11:
                        case 12:
                        RandomOreReactorTime = 3600;
                        break;
                        case 13:
                        RandomOreReactorTime = 18000;
                        break;
                        default:
                        RandomOreReactorTime = 60;
                        break;
                    }
                    RandomOre.ReactorTime = RandomOreReactorTime;
                    Veins.Add(RandomOre);
                }
                if (HerbFh != null)
                {
                    Point RandomPos = HerbFh.Point1;
                    RandomHerb.Position = RandomPos;
                    PassedFhs.Add(RandomFH);

                    int HerbTotal = Herbs.Sum(x => x.Value);
                    Temp = new Dictionary<int, int>();
                    int prev = 0;
                    foreach (KeyValuePair<int, int> Herb in Herbs)
                    {
                        Temp.Add(Herb.Key, Herb.Value + prev);
                        prev += Herb.Value;
                    }
                    Herbs = Temp;

                    int RandomHerbId = RandomCalc.Next(0, HerbTotal);
                    RandomHerb.Id = Herbs.FirstOrDefault(x => x.Value >= RandomHerbId).Key;

                    int RandomHerbReactorTime = 5;
                    switch (RandomHerb.Id % 100000)
                    {
                        case 6:
                        case 7:
                        case 8:
                        case 9:
                        RandomHerbReactorTime = 120;
                        break;
                        case 11:
                        case 12:
                        RandomHerbReactorTime = 3600;
                        break;
                        case 13:
                        RandomHerbReactorTime = 18000;
                        break;
                        default:
                        RandomHerbReactorTime = 60;
                        break;
                    }
                    RandomHerb.ReactorTime = RandomHerbReactorTime;
                    Veins.Add(RandomHerb);
                }
            }

            return Veins;
        }

        public static WzMap.FootHold GetFootHoldBelow(Point position, WzMap WzInfo)
        {
            List<WzMap.FootHold> validXFootHolds = WzInfo.FootHolds.Where(fh => !fh.IsWall && position.X >= fh.Point1.X && position.X <= fh.Point2.X && (fh.Point1.Y > position.Y || fh.Point2.Y > position.Y)).ToList();
            if (validXFootHolds.Any())
            {
                foreach (WzMap.FootHold fh in validXFootHolds.OrderBy(fh => (fh.Point1.Y < fh.Point2.Y ? fh.Point1.Y : fh.Point2.Y)))
                {
                    if (fh.Point1.Y != fh.Point2.Y) //diagonal foothold
                    {
                        int width = Math.Abs(fh.Point2.X - fh.Point1.X);
                        int height = Math.Abs(fh.Point2.Y - fh.Point1.Y);
                        double xy = (double)height / width;

                        int distFromPoint1 = position.X - fh.Point1.X; //doesnt matter if abs or not as long as you use point1's Y too

                        int addedY = (int)(distFromPoint1 * xy);

                        int y = fh.Point1.Y + addedY; //Foothold's Y value on position.X

                        if (y >= position.Y)
                            return fh;
                    }
                    else
                    {
                        return fh;
                    }
                }
            }
            return null;
        }
        #endregion       

        public static int LoadSkills(String path)
        {
            NXFile File = new NXFile(path);
            int ret = 0;
            List<ManualResetEvent> waitHandles = new List<ManualResetEvent>();

            #region Player skills
            foreach (NXNode skillImg in File.BaseNode)
            {
                ManualResetEvent resetEvent = new ManualResetEvent(false);
                waitHandles.Add(resetEvent);
                NXNodeResetEventWrapper wrap = new NXNodeResetEventWrapper(skillImg, resetEvent);
                ThreadPool.QueueUserWorkItem(new WaitCallback(LoadCharacterSkill), wrap);
            }
            #endregion

            #region Familiar skills
            NXNode familiarImg = File.BaseNode.GetChild("FamiliarSkill.img");
            foreach (NXNode familiarSkill in familiarImg)
            {
                int skillId;
                if (!int.TryParse(familiarSkill.Name, out skillId) || DataBuffer.FamiliarSkillBuffer.ContainsKey(skillId))
                    continue;
                WzFamiliarSkill newFamiliarSkill = new WzFamiliarSkill();
                newFamiliarSkill.Prop = GetIntFromChild(familiarSkill, "prop", 0);
                newFamiliarSkill.AttackCount = GetIntFromChild(familiarSkill, "attackCount", 1);
                newFamiliarSkill.TargetCount = GetIntFromChild(familiarSkill, "targetCount", 1);
                newFamiliarSkill.Time = GetIntFromChild(familiarSkill, "time", 0);
                newFamiliarSkill.Speed = GetIntFromChild(familiarSkill, "speed", 1);
                newFamiliarSkill.Knockback = (GetIntFromChild(familiarSkill, "knockback", 0) > 0 || GetIntFromChild(familiarSkill, "attract", 0) > 0);
                //TODO: status effects

                DataBuffer.FamiliarSkillBuffer.Add(skillId, newFamiliarSkill);
                ret++;

            }
            #endregion

            #region Crafting Recipies  
            NXNode[] RecipeNodes = new NXNode[] {
                File.BaseNode.GetChild("Recipe_9200.img"),
                File.BaseNode.GetChild("Recipe_9201.img"),
                File.BaseNode.GetChild("Recipe_9202.img"),
                File.BaseNode.GetChild("Recipe_9203.img"),
                File.BaseNode.GetChild("Recipe_9204.img")
            };
            foreach (NXNode RecipeNode in RecipeNodes)
            {
                foreach (NXNode Recipe in RecipeNode)
                {
                    WzRecipe AddRecipe = new WzRecipe();
                    int RecipeId;
                    if (!int.TryParse(Recipe.Name, out RecipeId) || DataBuffer.CraftRecipeBuffer.ContainsKey(RecipeId)) continue;

                    int SkillId = (int)Math.Floor((double)RecipeId / 10000);
                    SkillId *= 10000;
                    AddRecipe.ReqSkill = SkillId;

                    AddRecipe.IncFatigue = (byte)GetIntFromChild(Recipe, "incFatigability", 1);
                    AddRecipe.ReqSkillLevel = (byte)GetIntFromChild(Recipe, "reqSkillLevel", 1);
                    AddRecipe.IncProficiency = (byte)GetIntFromChild(Recipe, "incSkillProficiency", 1);

                    if (Recipe.ContainsChild("recipe"))
                    {
                        NXNode ReqItemNode = Recipe["recipe"];
                        foreach (NXNode ReqItem in ReqItemNode)
                        {
                            WzRecipe.Item AddReqItem = new WzRecipe.Item();
                            AddReqItem.ItemId = GetIntFromChild(ReqItem, "item", 0);
                            if (AddReqItem.ItemId == 0) continue;
                            AddReqItem.Count = (short)GetIntFromChild(ReqItem, "count", 0);
                            AddReqItem.Chance = (byte)GetIntFromChild(ReqItem, "probWeight", 0);
                            AddRecipe.ReqItems.Add(AddReqItem);
                        }
                    }

                    if (Recipe.ContainsChild("target"))
                    {
                        NXNode CreateItemNode = Recipe["target"];
                        foreach (NXNode CeateItem in CreateItemNode)
                        {
                            WzRecipe.Item AddCreateItem = new WzRecipe.Item();
                            AddCreateItem.ItemId = GetIntFromChild(CeateItem, "item", 0);
                            if (AddCreateItem.ItemId == 0) continue;
                            AddCreateItem.Count = (short)GetIntFromChild(CeateItem, "count", 0);
                            AddCreateItem.Chance = (byte)GetIntFromChild(CeateItem, "probWeight", 100);
                            AddRecipe.CreateItems.Add(AddCreateItem);
                        }
                    }

                    DataBuffer.CraftRecipeBuffer.Add(RecipeId, AddRecipe);
                    ret++;
                }
            }
            #endregion

            foreach (ManualResetEvent e in waitHandles)
                e.WaitOne();

            ret += DataBuffer.CharacterSkillBuffer.Count;

            File.Dispose();
            return ret;
        }

        private class NXNodeResetEventWrapper
        {
            public NXNode NxNode;
            public ManualResetEvent ResetEvent;
            public NXNodeResetEventWrapper(NXNode node, ManualResetEvent rEvent)
            {
                NxNode = node;
                ResetEvent = rEvent;
            }
        }

        private static void LoadCharacterSkill(object r)
        {
            NXNodeResetEventWrapper wrap = (NXNodeResetEventWrapper)r;
            NXNode skillImg = wrap.NxNode;
            int num;
            bool isNumber = Int32.TryParse(skillImg.Name.Substring(0, 3), out num);
            if (isNumber) //Job skills
            {
                if (skillImg.ContainsChild("skill"))
                {
                    NXNode skill = skillImg.GetChild("skill");
                    foreach (NXNode SkillNode in skill)
                    {
                        int skillId;
                        if (!int.TryParse(SkillNode.Name, out skillId) || DataBuffer.CharacterSkillBuffer.ContainsKey(skillId))
                            continue;
                        NXNode commonNode = null;
                        NXNode levelNode = null;

                        WzCharacterSkill wzSkill = new WzCharacterSkill();
                        wzSkill.SkillId = skillId;
                        int maxLevel = 0;
                        if (SkillNode.ContainsChild("common"))
                        {
                            commonNode = SkillNode.GetChild("common");
                            maxLevel = GetIntFromChild(commonNode, "maxLevel");
                            if (commonNode.ContainsChild("lt"))
                            {
                                wzSkill.TopLeft = GetPointFromChild(commonNode, "lt");
                                wzSkill.BottomRight = GetPointFromChild(commonNode, "rb");
                            }
                        }
                        else if (SkillNode.ContainsChild("level"))
                        {
                            levelNode = SkillNode.GetChild("level");
                            maxLevel = 1;
                            while (levelNode.ContainsChild((maxLevel + 1).ToString()))
                                maxLevel++;
                        }
                        wzSkill.MaxLevel = (byte)maxLevel;
                        wzSkill.HasMastery = SkillNode.ContainsChild("masterLevel");
                        if (wzSkill.HasMastery)
                            wzSkill.DefaultMastery = (byte)GetIntFromChild(SkillNode, "masterLevel");
                        #region MasterLevel Hack
                        switch (skillId) //There are exceptions... They have a MasterLevel attribute but dont use it and cause error 38 when sent
                        {
                            case Hero.COMBAT_MASTERY:
                            case DarkKnight.REVENGE_OF_THE_EVIL_EYE:
                            case DualBlade3.VENOM:
                            case DualBlade4.TOXIC_VENOM:
                            case DualBlade4.SHARPNESS:
                            case Buccaneer.DOUBLE_DOWN:
                            case Buccaneer.PIRATES_REVENGE:
                            case Corsair.DOUBLE_DOWN:
                            case Corsair.PIRATES_REVENGE:
                            case Cannoneer4.HEROS_WILL:
                            case Aran4.BOSS_REVERSE_COMBO:
                            case Aran4.SUDDEN_STRIKE:
                            case Mercedes4.HEROS_WILL:
                            case BattleMage3.STANCE:
                            case WildHunter4.WILD_INSTINCT:
                            wzSkill.DefaultMastery = 0;
                            wzSkill.HasMastery = false;
                            break;
                        }
                        #endregion
                        wzSkill.IsInvisible = GetIntFromChild(SkillNode, "invisible") == 1;
                        wzSkill.IsHyperSkill = GetIntFromChild(SkillNode, "hyper") == 1;
                        wzSkill.RequiredLevel = (byte)GetIntFromChild(SkillNode, "reqLev", 0);
                        wzSkill.IsKeyDownSkill = SkillNode.ContainsChild("keydown");
                        wzSkill.HasFixedLevel = GetIntFromChild(SkillNode, "fixLevel") > 0;
                        wzSkill.IsPassiveSkill = skillId % 10000 < 1000;
                        if (SkillNode.ContainsChild("req"))
                        {
                            NXNode reqNode = SkillNode.GetChild("req");
                            var requiredSkills = new Dictionary<int, byte>();
                            foreach (NXNode req in reqNode)
                            {
                                int reqSkillId;
                                if (int.TryParse(req.Name, out reqSkillId))
                                    requiredSkills.Add(reqSkillId, (byte)GetIntFromNode(req));
                                else
                                {
                                    if (req.Name == "level")
                                        wzSkill.RequiredLevel = (byte)GetIntFromNode(req);
                                }
                            }
                            if (requiredSkills.Count > 0)
                                wzSkill.RequiredSkills = requiredSkills;
                        }
                        bool combatOrders = GetIntFromChild(SkillNode, "combatOrders") == 1;
                        if (combatOrders)
                        {
                            maxLevel += 2;
                            wzSkill.CombatOrdersMaxLevel = (byte)maxLevel;
                        }
                        if (maxLevel > 0)
                        {
                            if (SkillNode.ContainsChild("summon"))
                            {
                                WzCharacterSkill.SummonAttackInfo summonInfo = new WzCharacterSkill.SummonAttackInfo();
                                NXNode summonNode = SkillNode.GetChild("summon");
                                if (summonNode.ContainsChild("attack1") && summonNode.GetChild("attack1").ContainsChild("info"))
                                {
                                    NXNode summonAttackInfoNode = summonNode.GetChild("attack1").GetChild("info");
                                    summonInfo.Delay = GetIntFromChild(summonAttackInfoNode, "effectAfter");
                                    summonInfo.Delay += GetIntFromChild(summonAttackInfoNode, "delay");
                                    summonInfo.MobCount = (byte)GetIntFromChild(summonAttackInfoNode, "mobCount", 1);
                                    summonInfo.AttackCount = (byte)GetIntFromChild(summonAttackInfoNode, "attackCount", 1);
                                }
                                summonInfo.MovementType = SkillConstants.GetSummonMovementType(skillId);
                                summonInfo.Type = SkillConstants.GetSummonType(skillId);
                                /*if (summonInfoNode.ContainsChild("range"))
                                {
                                    Point p1 = GetPointFromChild(summonInfoNode.GetChild("range"), "lt");
                                    Point p2 = GetPointFromChild(summonInfoNode.GetChild("range"), "rb");                                                                         
                                }*/
                                wzSkill.SummonInfo = summonInfo;

                                wzSkill.HasSummon = true;
                            }
                            List<NXNode> childNodes = new List<NXNode>();
                            if (commonNode != null)
                            {
                                for (int x = 1; x <= maxLevel; x++)
                                {
                                    SkillEffect levelInfo = new SkillEffect(wzSkill, (byte)x);
                                    Dictionary<CharacterSkillStat, int> stats = new Dictionary<CharacterSkillStat, int>();

                                    foreach (NXNode childNode in commonNode) //Loop through stats
                                    {
                                        string name = childNode.Name;
                                        if (name == "maxLevel")
                                            continue;
                                        CharacterSkillStat stat;
                                        if (!Enum.TryParse<CharacterSkillStat>(name, out stat))
                                            continue;

                                        int value = 0;
                                        NXValuedNode<string> snode = childNode as NXValuedNode<string>;
                                        if (snode != null)
                                        {
                                            string formula = snode.Value;
                                            if (skillId == 65000003 && formula.Contains("y")) //only this skill has a 'y' var in it, doesnt even stand for anything afaik
                                                continue;
                                            value = ParseWzFormula.ParseInt(formula, x, skillId);
                                        }
                                        else
                                        {
                                            bool success = true;
                                            value = GetIntFromNode(childNode, out success);
                                            if (!success)
                                            {
                                                ServerConsole.Error("Could not parse data from child " + name + " from skill " + skillId);
                                            }
                                        }
                                        stats.Add(stat, value);
                                    }
                                    levelInfo.Info = stats;
                                    int temp;
                                    if (stats.TryGetValue(CharacterSkillStat.attackCount, out temp))
                                        levelInfo.AttackCount = (byte)temp;
                                    else
                                    {
                                        if (stats.TryGetValue(CharacterSkillStat.bulletCount, out temp))
                                        {
                                            levelInfo.AttackCount = (byte)temp;
                                        }
                                        else
                                            levelInfo.AttackCount = 1;
                                    }
                                    if (stats.TryGetValue(CharacterSkillStat.mobCount, out temp))
                                        levelInfo.MobCount = (byte)temp;
                                    else
                                        levelInfo.MobCount = 1;
                                    temp = 0;
                                    if (stats.TryGetValue(CharacterSkillStat.mpCon, out temp))
                                        levelInfo.MpCon = temp;

                                    #region Buff Time Hack
                                    switch (skillId)
                                    {
                                        case FirePoison4.ARCANE_AIM:
                                        case IceLightning4.ARCANE_AIM:
                                        case Bishop.ARCANE_AIM:
                                        stats.Add(CharacterSkillStat.time, 5);
                                        break;
                                        case Priest.DIVINE_PROTECTION:
                                        case Bandit.CRITICAL_GROWTH:
                                        case ChiefBandit.PICKPOCKET:
                                        case Aran1.COMBO_ABILITY:
                                        stats.Add(CharacterSkillStat.time, SkillEffect.MAX_BUFF_TIME_S);
                                        break;
                                    }
                                    #endregion

                                    if (wzSkill.IsBuff || levelInfo.Info.ContainsKey(CharacterSkillStat.time))
                                    {
                                        levelInfo.LoadBuffStats();
                                        if (!wzSkill.IsBuff && levelInfo.BuffInfo.Count > 0)
                                            wzSkill.IsBuff = true;
                                    }
                                    wzSkill.SkillEffects.Add((byte)x, levelInfo);
                                }
                            }
                            else if (levelNode != null)
                            {
                                for (int x = 1; x <= maxLevel; x++)
                                {
                                    SkillEffect levelInfo = new SkillEffect(wzSkill, (byte)x);
                                    Dictionary<CharacterSkillStat, int> stats = new Dictionary<CharacterSkillStat, int>();
                                    NXNode thisLevelNode = levelNode.GetChild(x.ToString());
                                    foreach (NXNode childNode in thisLevelNode)
                                    {
                                        string name = childNode.Name;
                                        CharacterSkillStat stat;
                                        if (!Enum.TryParse<CharacterSkillStat>(name, out stat))
                                        {
                                            continue;
                                        }
                                        if (stats.ContainsKey(stat))
                                            continue;
                                        int value = 0;
                                        bool success = true;
                                        value = GetIntFromNode(childNode, out success);
                                        if (!success)
                                        {
                                            ServerConsole.Error("Could not parse data from child " + name + " from skill " + skillId + " level " + x);
                                            System.Console.ReadKey();
                                        }
                                        stats.Add(stat, value);
                                    }
                                    levelInfo.Info = stats;
                                    int temp;
                                    if (stats.TryGetValue(CharacterSkillStat.attackCount, out temp))
                                        levelInfo.AttackCount = (byte)temp;
                                    else
                                    {
                                        if (stats.TryGetValue(CharacterSkillStat.bulletCount, out temp))
                                        {
                                            levelInfo.AttackCount = (byte)temp;
                                        }
                                        else
                                            levelInfo.AttackCount = 1;
                                    }
                                    if (stats.TryGetValue(CharacterSkillStat.mobCount, out temp))
                                        levelInfo.MobCount = (byte)temp;
                                    else
                                        levelInfo.MobCount = 1;
                                    temp = 0;
                                    if (stats.TryGetValue(CharacterSkillStat.mpCon, out temp))
                                        levelInfo.MpCon = temp;

                                    if (wzSkill.IsBuff || levelInfo.Info.ContainsKey(CharacterSkillStat.time))
                                    {
                                        levelInfo.LoadBuffStats();
                                        if (!wzSkill.IsBuff && levelInfo.BuffInfo.Count > 0)
                                            wzSkill.IsBuff = true;
                                    }

                                    wzSkill.SkillEffects.Add((byte)x, levelInfo);
                                }
                            }
                            lock (DataBuffer.CharacterSkillBuffer)
                            {
                                if (!DataBuffer.CharacterSkillBuffer.ContainsKey(skillId))
                                {
                                    DataBuffer.CharacterSkillBuffer.Add(skillId, wzSkill);
                                }
                            }
                        }
                        if (wzSkill.IsBuff)
                        {
                            if (SkillNode.ContainsChild("info"))
                            {
                                wzSkill.IsPartySkill = GetIntFromChild(SkillNode.GetChild("info"), "massSpell") > 0;
                            }
                        }
                    }
                }
            }
            wrap.ResetEvent.Set();
        }

        public static int LoadStrings(String path)
        {
            NXFile File = new NXFile(path);
            int count = 0;

            #region Equips
            NXNode EqpFolder = File.BaseNode.GetChild("Eqp.img").GetChild("Eqp");
            foreach (NXNode equipTypeNode in EqpFolder)
            {
                foreach (NXNode equipNode in equipTypeNode)
                {
                    int id;
                    if (int.TryParse(equipNode.Name, out id))
                    {
                        WzEquip wzEq = DataBuffer.GetEquipById(id);
                        if (wzEq == null)
                            continue;
                        wzEq.Name = GetStringFromChild(equipNode, "name");
                        count++;
                    }
                }
            }
            #endregion
            #region Items
            NXNode ConsumeImg = File.BaseNode.GetChild("Consume.img");
            foreach (NXNode consumeNode in ConsumeImg)
            {
                int id;
                if (int.TryParse(consumeNode.Name, out id))
                {
                    WzItem wzItem = DataBuffer.GetItemById(id);
                    if (wzItem == null)
                        continue;
                    wzItem.Name = GetStringFromChild(consumeNode, "name");
                    count++;
                }
            }
            NXNode SetupImg = File.BaseNode.GetChild("Ins.img"); //chairs
            foreach (NXNode setupNode in SetupImg)
            {
                int id;
                if (int.TryParse(setupNode.Name, out id))
                {
                    WzItem wzItem = DataBuffer.GetItemById(id);
                    if (wzItem == null)
                        continue;
                    wzItem.Name = GetStringFromChild(setupNode, "name");
                    count++;
                }
            }
            NXNode EtcFolder = File.BaseNode.GetChild("Etc.img").GetChild("Etc");
            foreach (NXNode etcNode in EtcFolder)
            {
                int id;
                if (int.TryParse(etcNode.Name, out id))
                {
                    WzItem wzItem = DataBuffer.GetItemById(id);
                    if (wzItem == null)
                        continue;
                    wzItem.Name = GetStringFromChild(etcNode, "name");
                    count++;
                }
            }
            NXNode CashImg = File.BaseNode.GetChild("Cash.img");
            foreach (NXNode cashNode in CashImg)
            {
                int id;
                if (int.TryParse(cashNode.Name, out id))
                {
                    WzItem wzItem = DataBuffer.GetItemById(id);
                    if (wzItem == null)
                        continue;
                    wzItem.Name = GetStringFromChild(cashNode, "name");
                    count++;
                }
            }
            NXNode PetImg = File.BaseNode.GetChild("Pet.img");
            foreach (NXNode petNode in PetImg)
            {
                int id;
                if (int.TryParse(petNode.Name, out id))
                {
                    WzItem wzItem = DataBuffer.GetItemById(id);
                    if (wzItem == null)
                        continue;
                    wzItem.Name = GetStringFromChild(petNode, "name");
                    count++;
                }
            }
            #endregion
            #region Skills
            NXNode SkillImg = File.BaseNode.GetChild("Skill.img");
            foreach (NXNode skillNode in SkillImg)
            {
                bool skillBook = skillNode.Name.Length < 7;
                int id;
                if (int.TryParse(skillNode.Name, out id))
                {
                    if (skillBook)
                    {
                        DataBuffer.JobNames.Add(id, GetStringFromChild(skillNode, "bookName"));
                        count++;
                    }
                    else
                    {
                        WzCharacterSkill wzSkill = DataBuffer.GetCharacterSkillById(id);
                        if (wzSkill == null)
                            continue;
                        wzSkill.Name = GetStringFromChild(skillNode, "name");
                        count++;
                    }
                }
            }
            #endregion
            #region Maps
            NXNode MapImg = File.BaseNode.GetChild("Map.img");
            foreach (NXNode mapZone in MapImg)
            {
                foreach (NXNode mapNode in mapZone)
                {
                    int id;
                    if (int.TryParse(mapNode.Name, out id))
                    {
                        WzMap wzMap = DataBuffer.GetMapById(id);
                        if (wzMap == null)
                            continue;
                        wzMap.Name = GetStringFromChild(mapNode, "mapName");
                        count++;
                    }
                }
            }
            #endregion
            #region Mobs
            NXNode mobImg = File.BaseNode.GetChild("Mob.img");
            foreach (NXNode mobNode in mobImg)
            {
                int id;
                if (int.TryParse(mobNode.Name, out id))
                {
                    WzMob wzMob = DataBuffer.GetMobById(id);
                    if (wzMob == null)
                        continue;
                    wzMob.Name = GetStringFromChild(mobNode, "name");
                    count++;
                }
            }
            #endregion
            #region Npcs
            NXNode npcImg = File.BaseNode.GetChild("Npc.img");
            foreach (NXNode npcNode in npcImg)
            {
                int id;
                if (int.TryParse(npcNode.Name, out id))
                {
                    DataBuffer.NpcNames.Add(id, GetStringFromChild(npcNode, "name"));
                    count++;
                }
            }
            #endregion

            return count;
        }

        public static int LoadEtc(string path)
        {
            NXFile file = new NXFile(path);
            int ret = 0;

            #region Character Creation Info
            NXNode makeCharInfoNode = file.BaseNode.GetChild("MakeCharInfo.img");
            foreach (NXNode jobNode in makeCharInfoNode)
            {
                short jobId;
                Dictionary<int, List<int>> maleValues = null;
                Dictionary<int, List<int>> femaleValues = null;
                if (short.TryParse(jobNode.Name, out jobId) || jobNode.Name == "000_1")
                {
                    if (jobNode.Name == "000_1") //dual blade                
                        jobId = 1;
                    NXNode infoNode = jobNode.GetChild("info");
                    byte choosableGender = (byte)GetIntFromChild(infoNode, "choosableGender", 1);
                    if (choosableGender != 3) //not female only
                    {
                        maleValues = new Dictionary<int, List<int>>();
                        List<NXNode> children;
                        if (!jobNode.ContainsChild("male"))
                        {
                            if (jobNode.ContainsChild("male0") && jobNode.ContainsChild("male1")) //Xenon are special and can choose from 2 types
                            {

                                children = jobNode.GetChild("male0").ToList();
                                children.AddRange(jobNode.GetChild("male1"));
                            }
                            else
                                continue;
                        }
                        else
                            children = new List<NXNode>(jobNode.GetChild("male").ToList());

                        foreach (NXNode numNode in children)
                        {
                            int num = int.Parse(numNode.Name);
                            List<int> values = new List<int>();

                            if (numNode.ContainsChild("color"))
                            {
                                foreach (NXNode hairNode in numNode.GetChild("color"))
                                {
                                    foreach (NXNode colorNode in hairNode)
                                        values.Add(GetIntFromNode(colorNode));
                                }
                            }
                            else
                            {
                                foreach (NXNode valueNode in numNode)
                                    values.Add(GetIntFromNode(valueNode));
                            }
                            if (maleValues.ContainsKey(num))
                            {
                                maleValues[num].AddRange(values);
                            }
                            else
                                maleValues.Add(num, values);
                        }
                    }
                    if (choosableGender != 2) //not male only
                    {
                        femaleValues = new Dictionary<int, List<int>>();
                        List<NXNode> children;
                        if (!jobNode.ContainsChild("female"))
                        {
                            if (jobNode.ContainsChild("female0") && jobNode.ContainsChild("female1")) //Xenon are special and can choose from 2 types
                            {
                                children = jobNode.GetChild("female0").ToList();
                                children.AddRange(jobNode.GetChild("female1"));
                            }
                            else
                                continue;
                        }
                        else
                            children = new List<NXNode>(jobNode.GetChild("female").ToList());

                        foreach (NXNode numNode in children)
                        {
                            int num = int.Parse(numNode.Name);
                            List<int> values = new List<int>();

                            if (numNode.ContainsChild("color"))
                            {
                                foreach (NXNode hairNode in numNode.GetChild("color"))
                                {
                                    foreach (NXNode colorNode in hairNode)
                                        values.Add(GetIntFromNode(colorNode));
                                }
                            }
                            else
                            {
                                foreach (NXNode valueNode in numNode)
                                    values.Add(GetIntFromNode(valueNode));
                            }
                            if (femaleValues.ContainsKey(num))
                            {
                                femaleValues[num].AddRange(values);
                            }
                            else
                                femaleValues.Add(num, values);
                        }
                    }
                    WzMakeCharInfo createInfo = new WzMakeCharInfo(choosableGender, maleValues, femaleValues);
                    JobType jobType = CreateInfo.GetJobTypeById(jobId);
                    if (!DataBuffer.CharCreationInfo.ContainsKey(jobType))
                        DataBuffer.CharCreationInfo.Add(jobType, createInfo);
                    ret++;
                }
            }
            #endregion

            return ret;
        }

        public static int LoadQuests(string path)
        {
            NXFile file = new NXFile(path);
            int ret = 0;
            #region Quest Requirements
            NXNode questReq = file.BaseNode.GetChild("Check.img");
            foreach (NXNode questNode in questReq)
            {
                ushort questId = ushort.Parse(questNode.Name);
                WzQuest wzQuest = new WzQuest(questId);
                foreach (NXNode questSubNode in questNode)
                {
                    int i = int.Parse(questSubNode.Name);
                    foreach (NXNode req in questSubNode)
                    {
                        string reqName = req.Name;
                        QuestRequirementType type = GetMapleQuestRequirementTypeByName(reqName);
                        if (type != QuestRequirementType.undefined)
                        {
                            if (i == 0)
                                wzQuest.StartRequirements.Add(CreateQuestRequirement(wzQuest, type, req));
                            else if (i == 1)
                                wzQuest.FinishRequirements.Add(CreateQuestRequirement(wzQuest, type, req));
                        }
                    }
                }
                DataBuffer.QuestBuffer.Add(wzQuest.Id, wzQuest);
                ret++;
            }
            #endregion
            #region Quest Actions
            NXNode questAct = file.BaseNode.GetChild("Act.img");
            foreach (NXNode questNode in questAct)
            {
                ushort questId = ushort.Parse(questNode.Name);
                WzQuest wzQuest;
                if (DataBuffer.QuestBuffer.TryGetValue(questId, out wzQuest))
                {
                    foreach (NXNode questSubNode in questNode)
                    {
                        int i = int.Parse(questSubNode.Name);
                        foreach (NXNode act in questSubNode)
                        {
                            string actName = act.Name;

                            QuestActionType type = GetMapleQuestActionTypeByName(actName);
                            if (type != QuestActionType.undefined)
                            {
                                if (i == 0)
                                    wzQuest.StartActions.Add(CreateQuestAction(type, act, questId));
                                else if (i == 1)
                                    wzQuest.FinishActions.Add(CreateQuestAction(type, act, questId));
                            }
                        }
                    }
                }
            }
            #endregion
            return ret;
        }

        const string CUSTOM_NPC_FILE_LOCATION = @".\CustomData\CustomNPCs.txt";
        public static List<Pair<int, WzMap.Npc>> LoadCustomNpcs()
        {
            List<Pair<int, WzMap.Npc>> ret = new List<Pair<int, WzMap.Npc>>();
            if (File.Exists(CUSTOM_NPC_FILE_LOCATION))
            {
                string[] lines = File.ReadAllLines(CUSTOM_NPC_FILE_LOCATION);
                foreach (string line in lines)
                {
                    string[] split = line.Split(',');
                    if (split.Length == 5)
                    {
                        int mapId = int.Parse(split[0]);
                        WzMap.Npc npc = new WzMap.Npc();
                        npc.Id = int.Parse(split[1]);
                        npc.x = short.Parse(split[2]);
                        npc.y = short.Parse(split[3]);
                        npc.Rx0 = 0;
                        npc.Rx1 = 0;
                        npc.Cy = npc.y;
                        npc.Fh = short.Parse(split[4]);
                        npc.F = true;
                        npc.Hide = false;
                        ret.Add(new Pair<int, WzMap.Npc>(mapId, npc));
                    }
                }
            }
            return ret;
        }

        public static void SaveCustomNpcs(List<Pair<int, WzMap.Npc>> npcs)
        {
            StringBuilder sb = new StringBuilder();
            foreach (var pair in npcs)
            {
                sb.Append(pair.Left);
                sb.Append(",");
                sb.Append(pair.Right.Id);
                sb.Append(",");
                sb.Append(pair.Right.x);
                sb.Append(",");
                sb.Append(pair.Right.y);
                sb.Append(",");
                sb.Append(pair.Right.Fh);
                sb.Append(Environment.NewLine);
            }
            File.WriteAllText(CUSTOM_NPC_FILE_LOCATION, sb.ToString());
        }

        private const string MONSTER_DROPS_FILE_LOCATION = @".\CustomData\MonsterDrops.txt";
        public static int LoadMobDrops()
        {
            int ret = 0;
            int currentMobId = 0;
            if (!File.Exists(MONSTER_DROPS_FILE_LOCATION))
            {
                ServerConsole.Warning("No MonsterDrops.txt file found at {0}", MONSTER_DROPS_FILE_LOCATION);
                return 0;
            }
            string[] lines = File.ReadAllLines(MONSTER_DROPS_FILE_LOCATION);
            for (int i = 0; i < lines.Length; i++)
            {
                if (lines[i].Length == 0)
                    continue;
                if (lines[i][0] == '#')
                    currentMobId = int.Parse(lines[i].Substring(1));
                else
                {
                    string[] split = lines[i].Split(' ');
                    MobDrop newDrop = new MobDrop();
                    newDrop.ItemId = int.Parse(split[0]);
                    newDrop.DropChance = int.Parse(split[1]);
                    newDrop.MinQuantity = int.Parse(split[2]);
                    newDrop.MaxQuantity = int.Parse(split[3]);
                    newDrop.QuestId = int.Parse(split[4]);

                    List<MobDrop> outList;
                    if (!DataBuffer.MobDropBuffer.TryGetValue(currentMobId, out outList))
                    {
                        List<MobDrop> newList = new List<MobDrop>() { newDrop };
                        DataBuffer.MobDropBuffer.Add(currentMobId, newList);
                    }
                    else
                        outList.Add(newDrop);
                    ret++;
                }
            }
            //add mesos
            foreach (var kvp in DataBuffer.MobDropBuffer)
            {
                List<MobDrop> drops = kvp.Value;
                if (drops.Where(x => x.ItemId == 0).Count() == 0) //mob has no meso yet
                {
                    WzMob mob = DataBuffer.GetMobById(kvp.Key);
                    if (mob != null)
                    {
                        double divided = (mob.Level < 100 ? (mob.Level < 10 ? (double)mob.Level : 10.0) : (mob.Level / 10.0));
                        int max = /*mob.Boss&& !mons.isPartyBonus() ? (mob.Level * mob.Level) : */(mob.Level * (int)Math.Ceiling(mob.Level / divided));
                        for (int i = 0; i < mob.MesoDrops; i++)
                        {
                            MobDrop drop = new MobDrop();
                            drop.ItemId = 0;
                            drop.DropChance = mob.IsBoss ? 1000000 : 200000;
                            drop.MinQuantity = (int)(0.66 * max);
                            drop.MaxQuantity = max;
                            drop.QuestId = 0;
                            drops.Add(drop);
                            ret++;
                        }
                    }
                }
            }
            return ret;
        }

        public static int LoadGlobalDrops()
        {
            int ret = 0;
            if (!File.Exists(@".\CustomData\GlobalDrops.txt"))
            {
                ServerConsole.Warning("No GlobalDrops.txt file found in /CustomData/");
                return 0;
            }
            string[] lines = File.ReadAllLines(@".\CustomData\GlobalDrops.txt");
            for (int i = 0; i < lines.Length; i++)
            {
                string[] split = lines[i].Split(' ');
                MobDrop newDrop = new MobDrop();
                newDrop.ItemId = int.Parse(split[0]);
                newDrop.DropChance = int.Parse(split[1]);
                newDrop.MinQuantity = int.Parse(split[2]);
                newDrop.MaxQuantity = int.Parse(split[3]);
                newDrop.QuestId = int.Parse(split[4]);

                DataBuffer.GlobalDropBuffer.Add(newDrop);
                ret++;
            }
            return ret;
        }

        #region Gets
        private static string GetStringFromNode(NXNode node)
        {
            return node.ValueOrDie<string>();
        }
        private static int GetIntFromNode(NXNode node, out bool success, int defaultVal = 0)
        {
            int ret = GetIntFromNode(node, defaultVal);
            if (ret == defaultVal)
            {
                defaultVal = defaultVal == 0 ? -1 : 0;
                if (GetIntFromNode(node, defaultVal) == defaultVal)
                {
                    success = false;
                }
                else
                {
                    success = true;
                }
            }
            else
            {
                success = true;
            }
            return ret;
        }
        private static int GetIntFromNode(NXNode node, int defaultVal = 0)
        {
            if (node is NXValuedNode<long>)
            {
                return (int)node.ValueOrDie<long>();
            }
            if (node is NXValuedNode<string>)
            {
                int ret = 0;
                return (int.TryParse(node.ValueOrDie<string>(), out ret)) ? ret : defaultVal;
            }
            if (node is NXValuedNode<int>)
            {
                return node.ValueOrDie<int>();
            }
            return defaultVal;
        }

        private static string GetStringFromChild(NXNode node, string get, string defaultVal = "")
        {
            if (!node.ContainsChild(get)) return defaultVal;
            var child = node.GetChild(get) as NXStringNode;
            if (child == null)
            {
                ServerConsole.Debug("Child '" + get + "' of NxNode '" + node.Name + "' is not a string");
                return defaultVal;
            }
            return child.Value;
        }

        private static int GetIntFromChild(NXNode node, string get, int defaultVal = 0)
        {
            if (!node.ContainsChild(get))
                return defaultVal;
            NXNode child = node.GetChild(get);
            return GetIntFromNode(child, defaultVal);
        }
        private static double GetDoubleFromChild(NXNode node, string get, double defaultVal = 0)
        {
            if (!node.ContainsChild(get))
                return defaultVal;
            return node.GetChild(get).ValueOrDie<double>();
        }
        private static Point GetPointFromChild(NXNode node, string get, Point defaultVal = new Point())
        {
            if (!node.ContainsChild(get))
                return defaultVal;
            return node.GetChild(get).ValueOrDie<Point>();
        }
        #endregion

        #endregion

        #region Scripts
        public static int LoadScripts()
        {
            DataBuffer.NpcScripts = new Dictionary<int, Type>();
            DataBuffer.PortalScripts = new Dictionary<string, Type>();
            DataBuffer.EventScripts = new Dictionary<string, Type>();        
            int i = 0;

            /*CompilerParameters cParams = new CompilerParameters();
            string[] assemblies = Assembly.GetAssembly(typeof(Script)).GetReferencedAssemblies().Select(x => x.Name + ".dll").ToArray();
            cParams.ReferencedAssemblies.AddRange(assemblies);
            cParams.ReferencedAssemblies.Add("LeattServer.Scripting.dll");
            //cParams.(".\\CompiledScripts\\");
            //cParams.CompilerOptions = "/optimize";
            cParams.IncludeDebugInformation = false;
            cParams.GenerateExecutable = false;
            cParams.GenerateInMemory = true;            
            // Npc scripts:
            const string scriptDirectory = @".\Scripts\";*/
            const string scriptFile = ".\\Scripts\\LeattyScripts.dll";
            if (!File.Exists(scriptFile)) return 0;
            var bytes = File.ReadAllBytes(scriptFile);
            //Assembly scriptAssembly = Assembly.LoadFrom(scriptFile);
            Assembly scriptAssembly = Assembly.Load(bytes);
            Type[] types = scriptAssembly.GetExportedTypes();
            foreach (Type t in types)
            {
                if (t.IsSubclassOf(typeof(NpcScript)))
                {
                    int id;
                    if (int.TryParse(t.Name.Substring(t.Name.IndexOf("_") + 1), out id))
                    {
                        DataBuffer.NpcScripts.Add(id, t);
                        i++;
                    }
                }
                else if (t.IsSubclassOf(typeof(ShopScript)))
                {
                    int id;
                    if (int.TryParse(t.Name.Substring(t.Name.IndexOf("_") + 1), out id))
                    {
                        DataBuffer.NpcScripts.Add(id, t);
                        i++;
                    }
                }
                else if (t.IsSubclassOf(typeof(PortalScript)))
                {
                    DataBuffer.PortalScripts.Add(t.Name, t);
                    i++;
                }
                else if (t.IsSubclassOf(typeof(EventScript)))
                {
                    DataBuffer.EventScripts.Add(t.Name, t);
                    i++;
                }
            }
            /*foreach (string scriptPath in Directory.GetFiles(scriptDirectory + "NPCs"))
            {
                Type[] types = CompileScriptAndGetTypes(scriptPath, cParams);
                foreach (Type t in types)
                {
                    if (t.IsSubclassOf(typeof(NpcScript)))
                    {
                        int id;
                        if (int.TryParse(t.Name.Substring(t.Name.IndexOf("_") + 1), out id))
                        {
                            DataBuffer.NpcScripts.Add(id, t);
                            i++;
                        }
                    }
                }
            }
            //Shop scripts:
            foreach (string scriptPath in Directory.GetFiles(scriptDirectory + "Shops"))
            {
                Type[] types = CompileScriptAndGetTypes(scriptPath, cParams);
                foreach (Type t in types)
                {
                    if (t.IsSubclassOf(typeof(ShopScript)))
                    {
                        int id;
                        if (int.TryParse(t.Name.Substring(t.Name.IndexOf("_") + 1), out id))
                        {
                            DataBuffer.NpcScripts.Add(id, t);
                            i++;
                        }
                    }
                }
            }
            // Portal scripts:
            foreach (string scriptPath in Directory.GetFiles(scriptDirectory + "Portals"))
            {
                Type[] types = CompileScriptAndGetTypes(scriptPath, cParams);
                foreach (Type t in types)
                {
                    if (t.IsSubclassOf(typeof(PortalScript)))
                    {
                        DataBuffer.PortalScripts.Add(t.Name, t);
                        i++;
                    }
                }
            }
            // Event scripts:
            foreach (string scriptPath in Directory.GetFiles(scriptDirectory + "Events"))
            {
                Type[] types = CompileScriptAndGetTypes(scriptPath, cParams);
                foreach (Type t in types)
                {
                    if (t.IsSubclassOf(typeof(EventScript)))
                    {
                        DataBuffer.EventScripts.Add(t.Name, t);
                        i++;
                    }
                }
            }*/           
            return i;
                /*
                Type npcScriptT = typeof(NpcScript);
                foreach (Type t in types)
                {
                    if (t.IsSubclassOf(typeof(NpcScript)))
                    {
                       if (t != typeof(ShopScript)) //the type ShopScript is a subclass of NpcScript but we don't want to load it. Actual shopscripts aren't filtered by this
                        {
                            int id;
                            if (int.TryParse(t.Name.Substring(t.Name.IndexOf("_") + 1), out id))
                            {
                                DataBuffer.NpcScripts.Add(id, t);
                                i++;
                            }                             
                        }
                    }                    
                    else if (t.IsSubclassOf(typeof(PortalScript)))
                    {                       
                        DataBuffer.PortalScripts.Add(t.Name, t);
                        i++;
                    }
                    else if (t.IsSubclassOf(typeof(EventScript)))
                    {
                        DataBuffer.EventScripts.Add(t.Name, t);
                        i++;
                    }
                }
            }
            catch (Exception e)
            {
                ServerConsole.Error("LoadNpcScript " + e.ToString());
            }
            return i;*/
        }

        private static CodeDomProvider p = new CSharpCodeProvider();
        private static Type[] CompileScriptAndGetTypes(string sourceFilePath, CompilerParameters cParams)
        {
            CompilerResults r = p.CompileAssemblyFromFile(cParams, sourceFilePath);
            if (r.Errors.HasErrors)
            {
                ServerConsole.Error("Error compiling script {0} Errors: ", sourceFilePath);
                foreach (CompilerError error in r.Errors)
                {
                    Console.WriteLine(error.ToString());
                }
                return new Type[0];
            }
            return r.CompiledAssembly.GetExportedTypes();
        }
        #endregion

        #region CashShop
        public static int LoadCashShopItems()
        {
            /*using (LeattyContext DBContext = new LeattyContext())
            {
                DataBuffer.CSItems = DBContext.Database.SqlQuery<CashShop.DbItem>(@"SELECT Inner3.CsId, Inner3.Id AS cId, Inner4.* 
                                                                FROM (SELECT Inner1.Id, Inner1.CsId 
                                                                FROM dbo.CashshopCats AS Inner1 INNER JOIN 
                                                                dbo.CashshopCats AS Inner2 ON Inner1.ParentId = Inner2.Id) AS Inner3 INNER JOIN 
                                                                dbo.CashshopItems AS Inner4 ON Inner4.CatId = Inner3.Id").OrderByDescending(x => x.Order).ToList();
                return DataBuffer.CSItems.Count();
            }*/
            return 0;
        }
        #endregion

        #region Functions
        public static QuestRequirementType GetMapleQuestRequirementTypeByName(string name)
        {
            if (Enum.IsDefined(typeof(QuestRequirementType), name))
                return (QuestRequirementType)Enum.Parse(typeof(QuestRequirementType), name);
            return QuestRequirementType.undefined;
        }

        public static QuestActionType GetMapleQuestActionTypeByName(string name)
        {
            if (Enum.IsDefined(typeof(QuestActionType), name))
                return (QuestActionType)Enum.Parse(typeof(QuestActionType), name);
            return QuestActionType.undefined;
        }

        public static MaplePotentialState GetPotentialGradeByName(string name)
        {
            MaplePotentialState state;
            if (Enum.TryParse(name, true, out state))
                return state;
            return MaplePotentialState.None;
        }

        public static WzQuestRequirement CreateQuestRequirement(WzQuest quest, QuestRequirementType type, NXNode data)
        {
            switch (type)
            {
                //Single string:
                case QuestRequirementType.startscript:
                case QuestRequirementType.endscript:
                return new WzQuestStringRequirement(type, GetStringFromNode(data));
                //Integer list:
                case QuestRequirementType.job:
                case QuestRequirementType.fieldEnter:
                {
                    List<int> jobs = new List<int>();
                    foreach (NXNode subNode in data)
                    {
                        jobs.Add(GetIntFromNode(subNode));
                    }
                    return new WzQuestIntegerListRequirement(type, jobs);
                }
                //Integer pair:
                case QuestRequirementType.item:
                case QuestRequirementType.mob:
                {
                    Dictionary<int, int> items = new Dictionary<int, int>();
                    foreach (NXNode subNode in data)
                    {
                        items.Add(GetIntFromChild(subNode, "id"), GetIntFromChild(subNode, "count"));
                    }
                    return new WzQuestIntegerPairRequirement(type, items);
                }
                case QuestRequirementType.quest:
                {
                    Dictionary<int, int> quests = new Dictionary<int, int>();
                    foreach (NXNode subNode in data)
                    {
                        quests.Add(GetIntFromChild(subNode, "id"), GetIntFromChild(subNode, "state"));
                    }
                    return new WzQuestIntegerPairRequirement(type, quests);
                }
                //Date:
                case QuestRequirementType.end:
                string strDate = GetStringFromNode(data);
                string year = strDate.Substring(0, 4);
                string month = strDate.Substring(4, 2);
                string day = strDate.Substring(6, 2);
                string hour = strDate.Substring(8, 2);
                return new WzQuestDateRequirement(type, new DateTime(int.Parse(year), int.Parse(month), int.Parse(day), int.Parse(hour), 0, 0));
                case QuestRequirementType.pet:
                List<int> petIds = new List<int>();
                foreach (NXNode subNode in data)
                {
                    petIds.Add(GetIntFromChild(subNode, "id"));
                }
                return new WzQuestIntegerListRequirement(type, petIds);
                //Single integer:
                default:
                return new WzQuestIntegerRequirement(type, GetIntFromNode(data));
            }
        }

        public static WzQuestAction CreateQuestAction(QuestActionType type, NXNode data, ushort questIdz)
        {
            switch (type)
            {
                case QuestActionType.item:
                {
                    List<WzQuestItemReward> rewards = new List<WzQuestItemReward>();
                    foreach (NXNode item in data)
                    {
                        int itemId = GetIntFromChild(item, "id");
                        if (itemId > 0)
                        {
                            WzQuestItemReward reward = new WzQuestItemReward();
                            reward.Count = GetIntFromChild(item, "count", 1);
                            reward.Gender = (byte)GetIntFromChild(item, "gender", 2);
                            reward.Potential = GetPotentialGradeByName(GetStringFromChild(item, "potentialGrade", "None"));                           
                            int jobMask = GetIntFromChild(item, "job", 0);
                            List<int> jobs = jobMask == 0 ? new List<int>() : WzQuestItemAction.GetJobsFromMask(jobMask, itemId, questIdz);
                            reward.Jobs = jobs;
                            rewards.Add(reward);
                        }
                    }
                    return new WzQuestItemAction(rewards);
                }
                case QuestActionType.quest:
                {
                    Dictionary<int, int> questUpdates = new Dictionary<int, int>();
                    foreach (NXNode questNode in data)
                    {
                        int questId = GetIntFromChild(questNode, "id");
                        int state = GetIntFromChild(questNode, "state", -1337);
                        if (questId > 0 && state != 1337)
                        {
                            questUpdates.Add(questId, state);
                        }
                    }
                    return new WzQuestIntegerPairAction(type, questUpdates);
                }
                case QuestActionType.skill:
                {
                    List<WzQuestSkillreward> rewards = new List<WzQuestSkillreward>();
                    foreach (NXNode skill in data)
                    {
                        int id = GetIntFromChild(skill, "id");
                        if (id > 0)
                        {
                            WzQuestSkillreward reward = new WzQuestSkillreward();
                            reward.SkillId = id;
                            reward.Level = (byte)GetIntFromChild(skill, "skillLevel", 1);
                            reward.MasterLevel = (byte)GetIntFromChild(skill, "masterLevel", 1);
                            if (!skill.ContainsChild("job"))
                                reward.Jobs = new List<int>();
                            else
                            {
                                List<int> jobs = new List<int>();
                                foreach (NXNode job in skill.GetChild("job"))
                                {
                                    jobs.Add(GetIntFromNode(job));
                                }
                                reward.Jobs = jobs;
                            }
                            rewards.Add(reward);
                        }
                    }
                    return new WzQuestSkillAction(rewards);
                }
                case QuestActionType.sp:
                {
                    List<WzQuestSPReward> rewards = new List<WzQuestSPReward>();
                    foreach (NXNode sp in data)
                    {
                        WzQuestSPReward reward = new WzQuestSPReward();
                        reward.Sp = GetIntFromChild(sp, "sp_value");
                        List<int> jobs = new List<int>();
                        foreach (NXNode job in sp.GetChild("job"))
                        {
                            jobs.Add(GetIntFromNode(job));
                        }
                        reward.Jobs = jobs;
                        rewards.Add(reward);
                    }
                    return new WzQuestSPAction(rewards);
                }
                case QuestActionType.willEXP:
                case QuestActionType.senseEXP:
                case QuestActionType.insightEXP:
                case QuestActionType.craftEXP:
                case QuestActionType.charismaEXP:
                case QuestActionType.charmEXP:
                case QuestActionType.exp:
                case QuestActionType.money:
                case QuestActionType.buffItemID:
                case QuestActionType.pop:
                case QuestActionType.nextQuest:
                return new WzQuestIntegerAction(type, GetIntFromNode(data));
                default:
                ServerConsole.Error("Unhandled quest action: " + type);
                return null;
            }
        }
        #endregion
    }
}
