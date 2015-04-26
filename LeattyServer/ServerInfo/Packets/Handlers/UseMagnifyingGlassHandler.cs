using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LeattyServer.Data;
using LeattyServer.Data.WZ;
using LeattyServer.Helpers;
using LeattyServer.ServerInfo.Inventory;
using LeattyServer.ServerInfo.Player;

namespace LeattyServer.ServerInfo.Packets.Handlers
{
    public static class UseMagnifyingGlassHandler
    {
        public static void Handle(MapleClient c, PacketReader pr)
        {
            MapleCharacter chr = c.Account.Character;
            if (!chr.DisableActions()) return;
            int tickCount = pr.ReadInt();
            short magnifySlot = pr.ReadShort();
            short equipSlot = pr.ReadShort();


            MapleItem magnifyer = null;
            MapleEquip equip = c.Account.Character.Inventory.GetItemSlotFromInventory(MapleInventoryType.Equip, equipSlot) as MapleEquip;
            if (magnifySlot != 0x7F) // Using magnify button in inventory sends 0x007F as the slot
            {
                magnifyer = chr.Inventory.GetItemSlotFromInventory(MapleInventoryType.Use, magnifySlot);
                if (magnifyer == null) return; //todo: check if it's a magnifying glass
            }
            if (equip == null) return;
            WzEquip equipInfo = DataBuffer.GetEquipById(equip.ItemId);
            if (equipInfo == null) return;
            if (equip.PotentialState >= MaplePotentialState.HiddenRare && equip.PotentialState <= MaplePotentialState.HiddenLegendary)
            {
                long price = equipInfo.RevealPotentialCost;
                ServerConsole.Debug("pot cost: " + price);
                if (chr.Mesos < price)
                {
                    chr.SendPopUpMessage(string.Format("You do not have {0} mesos", price));
                    chr.EnableActions();
                }
                else
                {
                    chr.Inventory.RemoveMesos(price);
                    equip.PotentialState += 16;
                    if (magnifyer != null)
                        chr.Inventory.RemoveItemsFromSlot(magnifyer.InventoryType, magnifyer.Position, 1);
                    c.SendPacket(MapleInventory.Packets.AddItem(equip, equip.InventoryType, equip.Position));
                    chr.Map.BroadcastPacket(MagnifyEffectPacket(chr.Id, equip.Position, true), chr, true);
                    chr.EnableActions(false);
                }
            }
            else
            {
                chr.SendPopUpMessage("You cannot use that on this item");
                chr.EnableActions();
            }
        }

        private static PacketWriter MagnifyEffectPacket(int characterId, short itemPosition, bool success)
        {
            PacketWriter pw = new PacketWriter(SendHeader.ShowMagnifyEffect);
            pw.WriteInt(characterId);
            pw.WriteShort(itemPosition);
            pw.WriteBool(success);
            return pw;
        }
    }
}