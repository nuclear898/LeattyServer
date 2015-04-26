using LeattyServer.ServerInfo.Inventory;
using LeattyServer.ServerInfo.Player;

namespace LeattyServer.ServerInfo.Packets.Handlers
{
    class TradeHandler
    {
        //todo
        //response to join trade room if the owner disconnected or closed it
        //response packet if character is busy
        //"enabled actions" needs worked on, such as after adding an item to the trade the client expects its actions enabled.
        private static void HandleAddItem(PacketReader pr, MapleCharacter chr)
        {
            MapleInventoryType inventoryType = (MapleInventoryType)pr.ReadByte();

            short fromSlot = pr.ReadShort();
            short quantity = pr.ReadShort();
            byte tradeSlot = pr.ReadByte();

           
            
            MapleItem item = chr.Inventory.GetItemSlotFromInventory(inventoryType, fromSlot);
            if (item != null)
            {
                if (chr.Trade.AddItem(item, tradeSlot,quantity, chr))
                {
                    if (item.Quantity == quantity)
                    {
                        chr.Client.SendPacket(MapleInventory.Packets.RemoveItem(inventoryType, fromSlot));
                    }
                    else
                    {
                        short newVal = (short)(item.Quantity - quantity);
                        chr.Client.SendPacket(MapleInventory.Packets.UpdateItemQuantity(inventoryType, fromSlot, newVal));
                    }
                }
            }
        }

        public static void Handle(MapleClient c, PacketReader pr)
        {
            byte func = pr.ReadByte();
            MapleCharacter chr = c.Account.Character;
            switch (func)
            {
                case 0x00://okay so i have no idea why these 4 functions exist
                case 0x01://add equip item?
                case 0x02://add stackable item to trade?
                case 0x03://partner add equip item?
                    {
                        if (chr.Trade != null)
                        {
                            HandleAddItem(pr, chr);
                        }
                        break;
                    }
                case 0x04:
                case 0x05://add meso
                case 0x06:
                case 0x07:
                    long mesos = pr.ReadLong();
                    if (mesos > 0)
                    {
                        if (chr.Trade != null && chr.Trade.Type == MapleTrade.TradeType.Trade)
                        {
                            chr.Trade.AddMesos(chr, mesos);
                        }
                    }
                    break;
                case 0x10://Create
                    if (chr.Trade == null)
                    {
                        if (!chr.DisableActions(ActionState.Trading)) return;
                        byte creationType = pr.ReadByte();
                        switch (creationType)
                        {
                            case 4:
                                MapleTrade t = MapleTrade.CreateTrade(MapleTrade.TradeType.Trade, chr);
                                chr.Trade = t;
                                break;
                            case 5:
                            case 6://create shop
                                break;
                            case 1:
                            case 2://minigame
                                break;
                        }
                    }
                    break;
                case 0x15://invite
                    {
                        int ID = pr.ReadInt();
                        MapleCharacter inviteChr = Program.GetClientByCharacterId(ID).Account.Character;
                        if (inviteChr.Trade == null && chr.Trade != null && chr.Trade.Type == MapleTrade.TradeType.Trade && chr.Trade.IsOwner(chr))
                        {
                            if (chr.Trade.Partners.Count == 0)
                            {
                                chr.Trade.Invite(inviteChr, chr);//i should check to make sure that i can invite them. i'll figure it out later.
                                chr.Trade.Partners.Add(inviteChr);//this way the trade can't invite two people.
                            }
                        }
                    }
                    break;
                case 0x13://invite accept
                    {
                        if (chr.Trade == null)
                        {
                            uint tradeID = pr.ReadUInt();
                            Invite invite = null;
                            if (chr.Invites.TryGetValue(InviteType.Trade, out invite))
                            {
                                if (invite.SenderId == tradeID)
                                {
                                    MapleTrade t = null;
                                    if (MapleTrade.TradeIDs.TryGetValue(tradeID, out t))
                                    {
                                        chr.Trade = t;
                                        c.SendPacket(t.GenerateTradeStart(chr, true));
                                        t.Owner.Client.SendPacket(t.GenerateTradePartnerAdd(chr, 1));
                                    }
                                    else
                                    {
                                        chr.Client.SendPacket(t.GenerateRoomClosedMessage());
                                    }
                                }
                            }
                        }
                    }
                    break;
                case 0x16://invite deny
                    if (chr.Trade == null)
                    {
                        uint tradeID = pr.ReadUInt();//there's one byte after this. in testing it was 03
                        Invite invite = null;
                        if (chr.Invites.TryGetValue(InviteType.Trade, out invite))
                        {
                            if (invite.SenderId == tradeID)
                            {
                                MapleTrade t = null;
                                if (MapleTrade.TradeIDs.TryGetValue(tradeID, out t))
                                {
                                    t.Owner.Client.SendPacket(t.GenerateTradeDeny(chr));
                                    t.Partners.Clear();
                                }
                                else
                                {

                                }
                                chr.Invites.Remove(InviteType.Trade);
                            }
                        }
                    }
                    break;
                case 0x1C://cancel trade.
                    if (chr.Trade != null && chr.Trade.Type == MapleTrade.TradeType.Trade)
                    {
                        chr.Trade.Close(false, false);
                    }
                    break;
                case 0x08:
                case 0x09:
                case 0x0A://Trade accept
                case 0x0B:
                    if (chr.Trade != null && chr.Trade.Type == MapleTrade.TradeType.Trade && chr.Trade.Partners.Count == 1)
                    {
                        chr.Trade.AcceptTrade(chr);
                    }
                    break;
                case 0x18:
                    pr.ReadInt();//timestamp
                    string text = pr.ReadMapleString();
                    if (chr.Trade != null && chr.Trade.Type == MapleTrade.TradeType.Trade && chr.Trade.Partners.Count == 1)
                    {
                        chr.Trade.Chat(chr, text);
                    }
                    break;
            }
        }
    }
}
