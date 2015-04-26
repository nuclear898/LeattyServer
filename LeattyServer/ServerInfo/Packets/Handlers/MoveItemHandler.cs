using LeattyServer.ServerInfo.Inventory;
using LeattyServer.ServerInfo.Player;

namespace LeattyServer.ServerInfo.Packets.Handlers
{
    class MoveItemHandler
    {
        public static void Handle(MapleClient c, PacketReader pw)
        {
            MapleCharacter chr = c.Account.Character;
            if (!chr.DisableActions()) return;
            int tickCount = pw.ReadInt();
            MapleInventoryType type = (MapleInventoryType)pw.ReadByte();
            short oldPosition = pw.ReadShort();
            short newPosition = pw.ReadShort();
            short quantity = pw.ReadShort();
            bool success = false;
            if (oldPosition == newPosition)
                return;
            try
            {
                MapleInventory inventory = chr.Inventory;

                if (newPosition < 0 && oldPosition > 0) //equip item
                    success = inventory.EquipItem(oldPosition, newPosition);
                else if (oldPosition < 0 && newPosition > 0) //unequip item
                   success = inventory.UnEquip(oldPosition, newPosition);
                else if (newPosition == 0) //drop item    
                {
                    if (!chr.Map.DropItemLimit)
                        success = inventory.DropItem(type, oldPosition, quantity);
                }
                else //item moved within inventory
                    success = inventory.MoveItem(type, oldPosition, newPosition);
            }
            finally
            {
                chr.EnableActions(!success);
            }
        }
    }
}
