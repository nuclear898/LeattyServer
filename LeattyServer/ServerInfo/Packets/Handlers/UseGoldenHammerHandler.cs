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
	public static class UseGoldenHammerHandler
	{
		public static void Handle(MapleClient c, PacketReader pr)
		{
		    MapleCharacter chr = c.Account.Character;
			if (!chr.DisableActions()) return;
            int tickCount = pr.ReadInt();
		    int hammerSlot = pr.ReadInt();
		    int hammerItemId = pr.ReadInt();
		    MapleItem hammer = chr.Inventory.GetItemSlotFromInventory(MapleInventoryType.Use, (short)hammerSlot);
		    if (hammer == null || hammer.ItemId != hammerItemId) return;
			pr.Skip(4); //Integer, inventory type?
		    int equipSlot = pr.ReadInt();
		    MapleEquip equip = chr.Inventory.GetItemSlotFromInventory(MapleInventoryType.Equip, (short)equipSlot) as MapleEquip;
		    if (equip == null) return;
			DoHammer(hammer, equip, chr);
			chr.EnableActions(false);
		}

	    public static void DoHammer(MapleItem hammer, MapleEquip equip, MapleCharacter chr)
	    {
            if (!CanHammer(equip))
            {
				chr.SendPopUpMessage("You cannot use that on this item.");
				chr.EnableActions();
                return;
            }
	        switch (hammer.ItemId)
	        {
                case 2470000:
				case 2470003:
				case 2470007:
				case 2470011:
                case 5570000:
	            {
	                equip.RemainingUpgradeCount++;
	                equip.HammersApplied++;
                    chr.Inventory.RemoveItemsFromSlot(hammer.InventoryType, hammer.Position, 1);
					chr.Client.SendPacket(MapleInventory.Packets.AddItem(equip, MapleInventoryType.Equip, equip.Position));
                    chr.Client.SendPacket(Packets.HammerEffect(true));
                    chr.Client.SendPacket(Packets.HammerResult(false, true, equip.HammersApplied));
	                PacketWriter finishPacket = Packets.HammerResult(true, true, 0);
	                Scheduler.ScheduleDelayedAction(() => chr.Client.SendPacket(finishPacket), 1500);
                    break;
	            }
	            default:
	            {
	                chr.SendPopUpMessage("You cannot use this hammer.");
	                chr.EnableActions();
	                return;
	            }
	        }
        }

	    private static bool CanHammer(MapleEquip equip)
	    {
	        WzEquip equipInfo = DataBuffer.GetEquipById(equip.ItemId);
	        if (equipInfo == null || equipInfo.TotalUpgradeCount == 0) return false;
	        switch (equip.ItemId)
	        {
	            case 1122000: //horntail necklaces
                case 1122076:
                case 1122151:
                case 1122278:
	                return false;
	        }
	        return equip.HammersApplied < 2;
	    }

	    public static class Packets
	    {
	        public static PacketWriter HammerResult(bool finish, bool success, int hammersUsed)
	        {
	            PacketWriter pw = new PacketWriter(SendHeader.GoldenHammerResult);
                pw.WriteByte(finish ? (byte) 2 : (byte) 0);
                pw.WriteBool(!success);
                pw.WriteShort(0);
                pw.WriteByte(0);
                pw.WriteInt(hammersUsed);
	            return pw;
	        }
            
	        public static PacketWriter HammerEffect(bool success)
	        {
	            PacketWriter pw = new PacketWriter(SendHeader.ShowInfo);
                pw.WriteByte(0x1B);
	            pw.WriteMapleString(success ? "Effect/BasicEff.img/FindPrize/Success" : "Effect/BasicEff.img/FindPrize/Failure");
	            pw.WriteInt(1);

	            return pw;
	        }
        }
	}
}