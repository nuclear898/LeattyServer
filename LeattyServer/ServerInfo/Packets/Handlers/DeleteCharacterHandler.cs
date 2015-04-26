using System.Collections.Generic;
using System.Linq;
using LeattyServer.Data;
using LeattyServer.DB.Models;
using LeattyServer.ServerInfo.Player;

namespace LeattyServer.ServerInfo.Packets.Handlers
{
    class DeleteCharacterHandler
    {
        public static void Handle(MapleClient c, PacketReader pr)
        {
            string enteredPic = pr.ReadMapleString();
            int characterId = pr.ReadInt();

            byte state = 20;
            if (c.Account.CheckPic(enteredPic) && c.Account.HasCharacter(characterId))
            {
                using (LeattyContext DBContext = new LeattyContext())
                {
                    //Do delete stuff
                    List<InventoryItem> ItemsToDelete = DBContext.InventoryItems.Where(x => x.CharacterId == characterId).ToList();
                    List<InventoryEquip> EquipsToDelete = new List<InventoryEquip>();
                    foreach (InventoryItem ItemToDelete in ItemsToDelete)
                    {
                        InventoryEquip EquipToDelete = DBContext.InventoryEquips.SingleOrDefault(x => x.InventoryItemId == ItemToDelete.Id);
                        if (EquipToDelete != null)
                            EquipsToDelete.Add(EquipToDelete);
                    }
                    DBContext.InventoryItems.RemoveRange(ItemsToDelete);
                    DBContext.InventoryEquips.RemoveRange(EquipsToDelete);
                    DBContext.InventorySlots.RemoveRange(DBContext.InventorySlots.Where(x => x.CharacterId == characterId));
                    DBContext.KeyMaps.RemoveRange(DBContext.KeyMaps.Where(x => x.CharacterId == characterId));
                    DBContext.QuickSlotKeyMaps.RemoveRange(DBContext.QuickSlotKeyMaps.Where(x => x.CharacterId == characterId));
                    DBContext.StolenSkills.RemoveRange(DBContext.StolenSkills.Where(x => x.CharacterId == characterId));

                    DBContext.Characters.Remove(DBContext.Characters.SingleOrDefault(x => x.Id == characterId));


                    DBContext.SaveChanges();
                }

                state = 0;
            } 

            //Response
            PacketWriter pw = new PacketWriter();
            pw.WriteHeader(SendHeader.DeleteCharacter);
            pw.WriteInt(characterId);
            pw.WriteByte(state);
            c.SendPacket(pw);
        }
    }
}
