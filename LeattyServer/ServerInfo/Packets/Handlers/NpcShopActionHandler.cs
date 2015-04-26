using System;
using LeattyServer.Constants;
using LeattyServer.Data;
using LeattyServer.Data.WZ;
using LeattyServer.Helpers;
using LeattyServer.ServerInfo.Inventory;
using LeattyServer.ServerInfo.Player;

namespace LeattyServer.ServerInfo.Packets.Handlers
{
    class NpcShopActionHandler
    {
        public static void Handle(MapleClient c, PacketReader pr)
        {
            try
            {
                if (c.NpcEngine != null && c.NpcEngine.IsShop)
                {
                    byte mode = pr.ReadByte();
                    int NpcId = c.NpcEngine.NpcId;
                    switch (mode)
                    {
                        case 0:
                        {
                            short shopIndex = pr.ReadShort();
                            int itemId = pr.ReadInt();
                            short amount = pr.ReadShort();
                            c.NpcEngine.BuyItem(itemId, shopIndex, amount);
                            break;
                        }
                        case 1: //sell 
                        {
                            short inventoryIndex = pr.ReadShort();
                            int itemId = pr.ReadInt();
                            short qty = pr.ReadShort();

                            MapleInventoryType invType = ItemConstants.GetInventoryType(itemId);
                            switch (invType)
                            {
                                case MapleInventoryType.Equip:
                                case MapleInventoryType.Etc:
                                case MapleInventoryType.Setup:
                                case MapleInventoryType.Use:
                                    break;
                                default:
                                    return; // Not a valid item
                            }
                            WzItem wzitem = DataBuffer.GetItemById(itemId);
                            if (wzitem == null)
                                wzitem = DataBuffer.GetEquipById(itemId);
                            if (wzitem == null) // Item doesnt exist (anymore?)
                                return;
                            if (wzitem.NotSale || wzitem.IsCashItem || wzitem.IsQuestItem)
                                return;
                            byte response = 0;
                            if (!wzitem.IsQuestItem)
                            {
                                MapleInventory inventory = c.Account.Character.Inventory;
                                MapleItem item = inventory.GetItemSlotFromInventory(invType, inventoryIndex);
                                if (item?.ItemId == itemId && item.Quantity >= qty)
                                {
                                    if (inventory.Mesos + wzitem.Price > GameConstants.MAX_MESOS)
                                    {
                                        response = 2; // You do not have enough mesos
                                    }
                                    else
                                    {
                                        inventory.RemoveItemsFromSlot(item.InventoryType, item.Position, qty, true);
                                        inventory.GainMesos(wzitem.Price*qty, false, false);
                                    }
                                    // TODO: buyback
                                }
                            }
                            PacketWriter pw = new PacketWriter();
                            pw.WriteHeader(SendHeader.NpcTransaction);
                            pw.WriteByte(response);
                            pw.WriteByte(0);
                            pw.WriteByte(0);
                            c.SendPacket(pw);
                            break;
                        }
                        case 3:
                        {
                            c.NpcEngine.Dispose();
                            break;
                        }
                        default:
                        {
                            c.NpcEngine.ScriptInstance = null;
                            ServerConsole.Warning("Unkown NpcShopActionHandler mode:" + mode);
                            ServerConsole.Info(Functions.ByteArrayToStr(pr.ToArray()));
                            break;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                ServerConsole.Error("NpcShopActionHandler Failure");
                ServerConsole.Error(ex.Message);
                if (c.NpcEngine != null)
                {
                    c.NpcEngine.Dispose();
                }
            }
        }
    }
}
