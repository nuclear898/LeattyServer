using LeattyServer.Constants;
using LeattyServer.Helpers;
using LeattyServer.ServerInfo.Inventory;
using LeattyServer.ServerInfo.Player;

namespace LeattyServer.ServerInfo.Packets.Handlers
{
    internal class UseSpecialItemHandler
    {
        public static void Handle(MapleClient c, PacketReader pr)
        {
            MapleCharacter chr = c.Account.Character;
            if (!chr.DisableActions()) return;
            int tickCount = pr.ReadInt();
            short slot = pr.ReadShort();
            int itemId = pr.ReadInt();
            MapleItem item = chr.Inventory.GetItemSlotFromInventory(ItemConstants.GetInventoryType(itemId), slot);
            bool removeItem = true;
            if (item == null || item.ItemId != itemId)
            {
                return;
            }


            switch (itemId)
            {
                case 5062006: //Platinum Miracle Cube
                {
                    int equipSlot = pr.ReadInt();
                    if (equipSlot < 0) return;
                    MapleEquip equip = chr.Inventory.GetItemSlotFromInventory(MapleInventoryType.Equip, (short) equipSlot) as MapleEquip;
                    if (equip == null) return;
                    if (!MapleEquipEnhancer.CubeItem(equip, CubeType.PlatinumMiracle, chr))
                        removeItem = false;
                    break;
                }
                case 5072000: //Super Megaphone
                case 5072001: //Super Megaphone
                {
                    if (!CanMegaPhone(c.Account.Character))
                    {
                        chr.EnableActions();
                        break;
                    }
                    string message = pr.ReadMapleString();
                    if (message.Length > 60) return;
                    bool whisperIcon = pr.ReadBool();
                    message = string.Format("{0} : {1}", c.Account.Character.Name, message);
                    Program.BroadCastWorldPacket(MapleCharacter.ServerNotice(message, 3, c.Channel, whisperIcon));
                    break;
                }
                case 5570000: //Vicious hammer
                {
                    removeItem = false; //Handled in UseGoldenHammerHandler
                    pr.Skip(4);
                    short equipSlot = (short)pr.ReadInt();
                    MapleEquip equip = chr.Inventory.GetItemSlotFromInventory(MapleInventoryType.Equip, equipSlot) as MapleEquip;
                    if (equip != null)
                    {
                        UseGoldenHammerHandler.DoHammer(item, equip, chr);
                    }
                    break;
                }
                default:
                {
                    ServerConsole.Warning("Unhandled UseSpecialItem: {0}", itemId);
                    removeItem = false;
                    chr.SendPopUpMessage("You cannot use this item");
                    chr.EnableActions();
                    break;
                }
            }
            if (removeItem)
            {
                chr.Inventory.RemoveItemsFromSlot(item.InventoryType, item.Position, 1);
            }
            chr.EnableActions(false);
        }

        private static bool CanMegaPhone(MapleCharacter chr)
        {
            if (chr.Level < 10) return false;
            //todo: check mute, jail, etc
            return true;
        }
    }
}
