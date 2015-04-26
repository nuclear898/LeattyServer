using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LeattyServer.Constants;
using LeattyServer.ServerInfo.Inventory;
using LeattyServer.ServerInfo.Player;

namespace LeattyServer.ServerInfo.Packets.Handlers
{
    public static class InventorySortHandler
    {
        public static void HandleSlotMerge(MapleClient c, PacketReader pr)
        {
            MapleCharacter chr = c.Account.Character;
            if (!chr.DisableActions()) return;
            int tickCount = pr.ReadInt();
            byte inventory = pr.ReadByte();
            if (inventory < 1 || inventory > 5) return;
            MapleInventoryType type = (MapleInventoryType)inventory;
            chr.Inventory.MergeSlots(type, c);
            chr.EnableActions(false);
            c.SendPacket(Packets.SlotMergeResponse(type));
        }

        public static void HandleItemSort(MapleClient c, PacketReader pr)
        {
            MapleCharacter chr = c.Account.Character;
            if (!chr.DisableActions()) return;
            int tickCount = pr.ReadInt();
            byte inventory = pr.ReadByte();
            if (inventory < 1 || inventory > 5) return;
            MapleInventoryType type = (MapleInventoryType)inventory;
            chr.Inventory.SortItems(type, c);
            chr.EnableActions(false);
            c.SendPacket(Packets.ItemSortResponse(type));
        }

        static class Packets
        {
            public static PacketWriter SlotMergeResponse(MapleInventoryType inventoryType)
            {
                PacketWriter pw = new PacketWriter(SendHeader.SlotMergeComplete);
                pw.WriteBool(true);
                pw.WriteByte((byte)inventoryType);

                return pw;
            }

            public static PacketWriter ItemSortResponse(MapleInventoryType inventoryType)
            {
                PacketWriter pw = new PacketWriter(SendHeader.ItemSortComplete);
                pw.WriteBool(true);
                pw.WriteByte((byte)inventoryType);

                return pw;
            }
        }
    }
}