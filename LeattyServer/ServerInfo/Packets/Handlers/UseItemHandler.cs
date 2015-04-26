using LeattyServer.Data;
using LeattyServer.Data.WZ;
using LeattyServer.Helpers;
using LeattyServer.ServerInfo.Inventory;
using LeattyServer.ServerInfo.Map;
using LeattyServer.ServerInfo.Player;

namespace LeattyServer.ServerInfo.Packets.Handlers
{
    public class UseItemHandler
    {
        public static void Handle(MapleClient c, PacketReader pr)
        {
            int tickCount = pr.ReadInt();
            short slot = pr.ReadShort();
            int id = pr.ReadInt();
            MapleCharacter chr = c.Account.Character;
            MapleItem item = chr.Inventory.GetItemSlotFromInventory(MapleInventoryType.Use, slot);
            if (item != null && item.ItemId == id)
            {
                WzConsume consume = DataBuffer.GetItemById(id) as WzConsume;
                if (consume != null)
                {
                    chr.Inventory.RemoveItemsFromSlot(MapleInventoryType.Use, slot, 1, true);

                    if (!chr.Map.PotionLimit)
                    {
                        if (consume.Hp != 0)
                            chr.AddHP((int)((chr.Stats.PotionEffectR / 100.0) * consume.Hp));

                        if (consume.Mp != 0)
                            chr.AddMP((int)((chr.Stats.PotionEffectR / 100.0) * consume.Mp));

                        if (consume.HpR != 0)
                            chr.AddHP((int)(chr.Stats.MaxHp * (consume.HpR / 100.0)));

                        if (consume.MpR != 0)
                            chr.AddMP((int)(chr.Stats.MaxMp * (consume.MpR / 100.0)));
                    }

                    if (consume.MoveTo != 0 && !chr.Map.PortalScrollLimit) 
                        chr.ChangeMap(consume.MoveTo);

                    if (consume.CharismaExp != 0)
                        chr.AddTraitExp(consume.CharismaExp, MapleCharacterStat.Charisma);

                    if (consume.CharmExp != 0)
                        chr.AddTraitExp(consume.CharmExp, MapleCharacterStat.Charm);

                    if (consume.CraftExp != 0)
                        chr.AddTraitExp(consume.CraftExp, MapleCharacterStat.Craft);

                    if (consume.InsightExp != 0)
                        chr.AddTraitExp(consume.InsightExp, MapleCharacterStat.Insight);

                    if (consume.SenseExp != 0)
                        chr.AddTraitExp(consume.SenseExp, MapleCharacterStat.Sense);

                    if (consume.WillExp != 0)
                        chr.AddTraitExp(consume.WillExp, MapleCharacterStat.Will);

                    return;
                }
            }
            chr.EnableActions();
        }

        public static void HandleReturnScroll(MapleClient c, PacketReader pr)
        {
            MapleCharacter chr = c.Account.Character;
            if (!chr.DisableActions()) return;
            int tickCount = pr.ReadInt();
            pr.Skip(2); //Unk
            int itemId = pr.ReadInt();
            if (!chr.Inventory.HasItem(itemId)) return;
            WzConsume item = DataBuffer.GetItemById(itemId) as WzConsume;
            if (item == null) return;

            if (item.MoveTo > 0)
            {
                int toMap = 0;
                toMap = item.MoveTo == 999999999 ? chr.Map.ReturnMap : item.MoveTo;
                if (toMap != 0)
                {
                    MapleMap map = Program.GetChannelServer(c.Channel).GetMap(toMap);
                    if (map == null || map.PortalScrollLimit) return;
                    if (chr.Inventory.RemoveItemsById(itemId, 1))
                        chr.ChangeMap(toMap);
                }
            }
            else
            {
                string msg = "Unhandled return scroll: " + itemId;
                ServerConsole.Warning(msg);
                FileLogging.Log("Unhandled items", msg);
                chr.EnableActions();
            }
        }
    }
}