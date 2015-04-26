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
    public static class UseScrollHandler
    {
        public static void HandleRegularEquipScroll(MapleClient c, PacketReader pr)
        {
            MapleCharacter chr = c.Account.Character;
            if (!chr.DisableActions()) return;

            //[BC E0 27 09] [09 00] [05 00] [01 00] [00]
            int tickCount = pr.ReadInt();
            short useSlot = pr.ReadShort();
            short equipSlot = pr.ReadShort();
            short whiteScrollMask = pr.ReadShort();
            bool whiteScroll = (whiteScrollMask & 0x2) > 0;
            //1 byte left, don't know what it is
            MapleEquip equip;
            MapleItem scroll;
            if (GetAndCheckItemsFromInventory(chr.Inventory, equipSlot, useSlot, out equip, out scroll))
            {
                if (scroll.ItemType == MapleItemType.RegularEquipScroll)
                    MapleEquipEnhancer.UseRegularEquipScroll(chr, equip, scroll, whiteScroll);
            }
            chr.EnableActions(false);
        }

        public static void HandleSpecialEquipScroll(MapleClient c, PacketReader pr)
        {
            MapleCharacter chr = c.Account.Character;
            if (!chr.DisableActions()) return;

            int tickCount = pr.ReadInt();
            short useSlot = pr.ReadShort();
            short equipSlot = pr.ReadShort();
            //1 byte left, don't know what it is
            MapleEquip equip;
            MapleItem scroll;
            if (GetAndCheckItemsFromInventory(chr.Inventory, equipSlot, useSlot, out equip, out scroll))
                MapleEquipEnhancer.UseSpecialEquipScroll(equip, scroll, chr);
            chr.EnableActions(false);

        }

        public static void HandleEquipEnhancementScroll(MapleClient c, PacketReader pr)
        {
            MapleCharacter chr = c.Account.Character;
            if (!chr.DisableActions()) return;

            int tickCount = pr.ReadInt();
            short useSlot = pr.ReadShort();
            short equipSlot = pr.ReadShort();
            MapleEquip equip;
            MapleItem scroll;
            if (GetAndCheckItemsFromInventory(chr.Inventory, equipSlot, useSlot, out equip, out scroll))
                MapleEquipEnhancer.UseEquipEnhancementScroll(equip, scroll, chr);
            chr.EnableActions(false);
        }

        public static void HandlePotentialScroll(MapleClient c, PacketReader pr)
        {
            MapleCharacter chr = c.Account.Character;
            if (!chr.DisableActions()) return;

            int tickCount = pr.ReadInt();
            short useSlot = pr.ReadShort();
            short equipSlot = pr.ReadShort();
            MapleEquip equip;
            MapleItem scroll;
            if (GetAndCheckItemsFromInventory(chr.Inventory, equipSlot, useSlot, out equip, out scroll))
               MapleEquipEnhancer.UsePotentialScroll(equip, scroll, chr);            
            chr.EnableActions(false);            
        }

        public static void HandleBonusPotentialScroll(MapleClient c, PacketReader pr)
        {
            MapleCharacter chr = c.Account.Character;
            if (!chr.DisableActions()) return;

            int tickCount = pr.ReadInt();
            short useSlot = pr.ReadShort();
            short equipSlot = pr.ReadShort();
            MapleEquip equip;
            MapleItem scroll;
            if (GetAndCheckItemsFromInventory(chr.Inventory, equipSlot, useSlot, out equip, out scroll))
                MapleEquipEnhancer.UseBonusPotentialScroll(equip, scroll, chr);
            chr.EnableActions(false);
        }

        public static void HandleCube(MapleClient c, PacketReader pr)
        {
            MapleCharacter chr = c.Account.Character;
            if (!chr.DisableActions()) return;

            int tickCount = pr.ReadInt();
            short useSlot = pr.ReadShort();
            short equipSlot = pr.ReadShort();
            MapleEquip equip;
            MapleItem cube;
            if (GetAndCheckItemsFromInventory(chr.Inventory, equipSlot, useSlot, out equip, out cube))
            {
                CubeType cubeType;
                switch (cube.ItemId)
                {
                    //Occult:
                    case 2710000:
                    case 2711000:
                    case 2710001:
                    {
                        cubeType = CubeType.Occult;
                        break;
                    }
                    //Master craftsman:
                    case 2710002:
                    case 2710007:
                    case 2711003:
                    case 2711005:
                    {
                        cubeType = CubeType.MasterCraftsman;
                        break;
                    }
                    //Meister:
                    case 2710003:
                    case 2711004:
                    case 2711006:
                    {
                        cubeType = CubeType.Meister;
                        break;
                    }
                    default:
                    {
                        chr.SendPopUpMessage("You cannot use this item");
                        chr.EnableActions();
                        return;
                    }
                }
                if (MapleEquipEnhancer.CubeItem(equip, cubeType, chr))
                {
                    chr.Inventory.RemoveItemsFromSlot(MapleInventoryType.Use, cube.Position, 1);
                    chr.EnableActions(false);
                }
            }
        }

        private static bool GetAndCheckItemsFromInventory(MapleInventory inventory, short equipSlot, short scrollSlot, out MapleEquip equip, out MapleItem scroll) 
        {
            MapleInventoryType equipInventory = equipSlot < 0 ? MapleInventoryType.Equipped : MapleInventoryType.Equip;
            equip = inventory.GetItemSlotFromInventory(equipInventory, equipSlot) as MapleEquip;
            scroll = inventory.GetItemSlotFromInventory(MapleInventoryType.Use, scrollSlot);
            return equip != null && scroll != null;
        }
    }
}