using System;
using System.Collections.Generic;
using System.Linq;
using LeattyServer.Data;
using LeattyServer.Data.WZ;
using LeattyServer.ServerInfo.Inventory;
using LeattyServer.ServerInfo.Player;

namespace LeattyServer.ServerInfo.Packets.Handlers
{
    public class CraftHandler
    {
        private static readonly Dictionary<string, int> EffectList = new Dictionary<string, int>(){
            {"Effect/BasicEff.img/professions/herbalism", 92000000},
            {"Effect/BasicEff.img/professions/herbalismExtract", 92000000},
            {"Effect/BasicEff.img/professions/mining", 92010000},
            {"Effect/BasicEff.img/professions/miningExtract", 92010000},

            {"Effect/BasicEff.img/professions/equip_product", 92020000},
            {"Effect/BasicEff.img/professions/acc_product", 92030000},
            {"Effect/BasicEff.img/professions/alchemy", 92040000}
        };

        public static void HandleCraftDone(MapleClient c, PacketReader pr)
        {
            int craftId = pr.ReadInt();
            MapleCharacter Chr = c.Account.Character;
            int skillId = (int)(10000 * Math.Floor((decimal)craftId / 10000));
            WzRecipe recipeInfo = DataBuffer.GetCraftRecipeById(craftId);

            if (recipeInfo == null) return; //Recipe not found, wierd, can happen due to packet edit
            if (!EffectList.ContainsValue(skillId)) return; //Should not happen unless if theres a packet edit or an update to the info compared to the code
            if (!Chr.HasSkill(skillId) && Chr.HasSkill(recipeInfo.ReqSkill)) return; //Obviously packet edit
            if (Chr.Map.MapId != 910001000 && (craftId % 92049000 > 7)) return; //Not in ardentmill, nor placing an extractor/fusing
            if (Chr.GetSkillLevel(skillId) < recipeInfo.ReqSkillLevel) return; //Not the correct level
            if ((200 - Chr.Fatigue - recipeInfo.IncFatigue) < 0) return; //Todo: show a message?
            skillId = recipeInfo.ReqSkill; //Just to be sure

            Chr.Fatigue += recipeInfo.IncFatigue;
            if (craftId % 92049000 > 0 && craftId % 92049000 < 7)
            {
                //Todo: Add code for disassembling/fusing 
                //Todo: Figure out what CraftId % 92049000 >= 2 do
                switch (craftId % 92049000)
                {
                    case 0:
                        int ExtractorId = pr.ReadInt();
                        int ItemId = pr.ReadInt();
                        long InventoryId = pr.ReadLong();
                        break; //disassembling
                    case 1:
                        ItemId = pr.ReadInt();
                        InventoryId = pr.ReadLong();
                        long InventoryId2 = pr.ReadLong();
                        break; //fusing
                }
                return;
            }
            if (recipeInfo.CreateItems.Count == 0) return;

            //Remove items
            bool HasItems = true;
            foreach (WzRecipe.Item Item in recipeInfo.ReqItems)
                HasItems &= Chr.Inventory.HasItem(Item.ItemId, Item.Count);
            if (!HasItems) return; //Todo: show message? "not enough items", though this smells like PE
            foreach (WzRecipe.Item Item in recipeInfo.ReqItems)
                Chr.Inventory.RemoveItemsById(Item.ItemId, Item.Count);

            //Calculate what items to get
            //Todo: check with older sources to check if proccess aint missing any orignal functionality
            int TotalChance = recipeInfo.CreateItems.Where(x => x.Chance != 100).Sum(x => x.Chance);

            List<WzRecipe.Item> SuccesedItems = new List<WzRecipe.Item>();
            SuccesedItems.AddRange(recipeInfo.CreateItems.Where(x => x.Chance == 100));
            if (TotalChance == 0)
                SuccesedItems.AddRange(recipeInfo.CreateItems.Where(x => x.Chance != 100));
            else
            {
                Dictionary<int, int> ChanceCalc = new Dictionary<int, int>();
                int Last = 0;
                foreach (WzRecipe.Item Item in recipeInfo.CreateItems.Where(x => x.Chance != 100))
                {
                    if (ChanceCalc.ContainsKey(Item.ItemId)) continue;
                    ChanceCalc.Add(Item.ItemId, Item.Chance + Last);
                    Last += Item.Chance;
                }

                Random RandomCalc = new Random();
                int PickItem = RandomCalc.Next(TotalChance);
                SuccesedItems.Add(recipeInfo.CreateItems.FirstOrDefault(x => x.ItemId == ChanceCalc.FirstOrDefault(cc => cc.Value >= PickItem).Key));
            }

            if (SuccesedItems.Count == 0) return; //Something went wrong

            //Give character the new item(s)
            foreach (WzRecipe.Item Item in SuccesedItems)
            {
                MapleItem CreateItem = MapleItemCreator.CreateItem(Item.ItemId, "Crafting with skill " + skillId, Item.Count, true);
                Chr.Inventory.AddItem(CreateItem, CreateItem.InventoryType, true);
            }


            //Give character his Exp
            //Todo: check if character is given a junk item and lower the exp gained
            Skill CraftSkill = Chr.GetSkill(skillId);
            int ReqLvlExp = 50 * CraftSkill.Level ^ 2 + 200 * CraftSkill.Level;
            if (ReqLvlExp < CraftSkill.SkillExp + recipeInfo.IncProficiency)
            {
                int ExpLeft = (CraftSkill.SkillExp + recipeInfo.IncProficiency) % ReqLvlExp;
                Chr.SetSkillLevel(skillId, (byte)(CraftSkill.Level + 1));
                Chr.SetSkillExp(skillId, (short)ExpLeft);
                //Todo: broadcast levelup message
            }
            else
            {
                Chr.SetSkillExp(skillId, (short)(CraftSkill.SkillExp + recipeInfo.IncProficiency));
            }

            //Todo: figure out craftrankings
            MapleCharacter.UpdateSingleStat(c, MapleCharacterStat.Fatigue, recipeInfo.IncFatigue);
            Chr.Map.BroadcastPacket(ShowCraftComplete(Chr.Id, craftId, 23, SuccesedItems[0].ItemId, SuccesedItems[0].Count, recipeInfo.IncProficiency));
        }

        public static void HandleCraftEffect(MapleClient c, PacketReader pr)
        {
            if (c.Account.Character.Map.MapId != 910001000) return; //Not in ardentmill

            String Effect = pr.ReadMapleString();
            if (!EffectList.ContainsKey(Effect)) return;
            int SkillId = EffectList[Effect];
            if (!c.Account.Character.HasSkill(SkillId)) return;
            if (pr.Available < 8) return;
            int Time = pr.ReadInt();
            if (Time > 6000 || Time < 3000)
            {
                Time = 4000;
            }
            int Type = pr.ReadInt();
            c.SendPacket(ShowOwnCraftingEffect(Effect, Time, Type));
        }

        public static void HandleCraftMake(MapleClient c, PacketReader pr)
        {
            if (c.Account.Character.Map.MapId != 910001000) return; //Not in ardentmill

            int Unk = pr.ReadInt();
            int Time = pr.ReadInt();
            if (Time > 6000 || Time < 3000)
            {
                Time = 4000;
            }
            c.Account.Character.Map.BroadcastPacket(ShowCraftMake(c.Account.Character.Id, Unk, Time));
        }

        public static void HandleUnk(MapleClient c, PacketReader pr)
        {
            String CraftId = pr.ReadMapleString();
            int Unk1 = pr.ReadInt();
            int Unk2 = pr.ReadInt();
            //c.SendPacket(CraftUnkResponse(CraftId, Unk1, Unk2));
        }

        private static PacketWriter ShowOwnCraftingEffect(String Effect, int Time, int Type)
        {
            PacketWriter pw = new PacketWriter();
            pw.WriteHeader(SendHeader.ShowSkillEffect);
            pw.WriteByte(0x21);
            pw.WriteMapleString(Effect);
            pw.WriteByte(1);
            pw.WriteInt(Time);
            pw.WriteInt(Type);
            if (Type == 2) pw.WriteInt(0);
            return pw;
        }

        private static PacketWriter ShowCraftMake(int cId, int Unk, int Time)
        {
            PacketWriter pw = new PacketWriter();
            pw.WriteHeader(SendHeader.CraftMake);
            pw.WriteInt(cId);
            pw.WriteInt(Unk);
            pw.WriteInt(Time);
            return pw;
        }

        private static PacketWriter ShowCraftComplete(int cId, int CraftId, int Ranking, int ItemId, int Count, int ExpGain)
        {
            PacketWriter pw = new PacketWriter();
            pw.WriteHeader(SendHeader.CraftComplete);
            pw.WriteInt(cId);
            pw.WriteInt(CraftId);
            pw.WriteInt(Ranking);
            pw.WriteInt(ItemId);
            pw.WriteInt(Count);
            pw.WriteInt(ExpGain);
            return pw;
        }

        private static PacketWriter CraftUnkResponse(String CraftId, int Unk1, int Unk2)
        {
            PacketWriter pw = new PacketWriter();
            pw.WriteHeader(SendHeader.CraftUnk);
            pw.WriteMapleString(CraftId);
            pw.WriteInt(Unk1);
            pw.WriteInt(Unk2);
            return pw;
        }
    }
}
