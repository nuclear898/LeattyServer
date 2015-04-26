using System.Collections.Generic;
using LeattyServer.Constants;
using LeattyServer.ServerInfo.Inventory;
using LeattyServer.ServerInfo.Player;

namespace LeattyServer.ServerInfo.Packets.Handlers
{
    public class CharacterInfoRequest
    {
        public static void Handle(MapleClient c, PacketReader pr)
        {
            MapleCharacter chr = c.Account.Character;
            pr.ReadInt();//timestamp
            int characterId = pr.ReadInt();
            MapleCharacter target = chr.Map.GetCharacter(characterId);
            if (target != null && !target.Hidden)
            {
                PacketWriter pw = new PacketWriter(SendHeader.CharacterInfo);
                pw.WriteInt(target.Id);
                pw.WriteByte(0); //v158
                pw.WriteByte(target.Level);
                pw.WriteShort(target.Job);
                pw.WriteShort(target.SubJob);
                pw.WriteByte(10);//pvpRank
                pw.WriteInt(target.Fame);
                pw.WriteByte(0);
                pw.WriteByte(0);//count of professions stats
                for (int i = 0; i < 0; i++)//count of professions stats
                {
                    pw.WriteShort(0); //profession value
                }
                if (target.Guild != null)
                    pw.WriteMapleString(target.Guild.Name);
                else
                    pw.WriteMapleString("-");
                pw.WriteShort(0); //alliance name

                pw.WriteByte(0xFF); //pet stuff
                pw.WriteByte(0);
                pw.WriteByte(0);
                /*
                for (MaplePet pet : chr.getPets()) {
                    if (pet.getSummoned()) {
                        mplew.write(index);
                        mplew.writeInt(pet.getPetItemId());
                        mplew.writeMapleAsciiString(pet.getName());
                        mplew.write(30);
                        mplew.writeShort(30000);
                        mplew.write(100);
                        //      mplew.write(pet.getLevel());
                        //    mplew.writeShort(pet.getCloseness());
                        //  mplew.write(pet.getFullness());
                        mplew.writeShort(0);
                        Item inv = chr.getInventory(MapleInventoryType.EQUIPPED).getItem((short) (byte) (index == 1 ? -114 : index == 2 ? -130 : -138));
                        mplew.writeInt(inv == null ? 0 : inv.getItemId());
                        mplew.writeInt(-1);
                        index = (byte) (index + 1);
                    }
                }*/

                pw.WriteByte(0);

                /*
                if ((chr.getInventory(MapleInventoryType.EQUIPPED).getItem((byte) -18) != null) && (chr.getInventory(MapleInventoryType.EQUIPPED).getItem((byte) -19) != null)) {
                    MapleMount mount = chr.getMount();
                    mplew.write(1);
                    mplew.writeInt(mount.getLevel());
                    mplew.writeInt(mount.getExp());
                    mplew.writeInt(mount.getFatigue());
                } else {
                    mplew.write(0);//no mount
                }*/
                pw.WriteByte(0); //no mount
                /*int wishlistSize = 0;
                pw.WriteByte((byte)wishlistSize);
                for (int i = 0; i < wishlistSize; i++)
                {
                   // mplew.writeInt(wishlist[x]);
                }*/
                MapleItem item = chr.Inventory.GetEquippedItem(0xD2); //medal
                if (item == null)                
                    pw.WriteInt(0);                
                else                
                    pw.WriteInt(item.ItemId);
                
                pw.WriteShort(0);//medals size
                /*
                mplew.writeShort(medalQuests.size());
                for (Pair<Integer, Long> x : medalQuests) {
                    mplew.writeShort(x.left);
                    mplew.writeLong(x.right); // Gain Filetime 
                }
                */
                pw.WriteZeroBytes(6);//each byte is a level of a trait
                pw.WriteInt(target.AccountId);

                pw.WriteMapleString("Creating..."); //name of farm, creating... = not made yet
                pw.WriteInt(0); //coins
                pw.WriteInt(0); //level
                pw.WriteInt(0); //exp
                pw.WriteInt(0); //clovers
                pw.WriteInt(0); //diamonds nx currency 
                pw.WriteByte(0); //kitty power

                pw.WriteZeroBytes(20);

                List<MapleItem> chairs = chr.Inventory.GetItemsFromInventory(MapleInventoryType.Setup, x => x.ItemType == MapleItemType.Chair);
                List<int> chairIds = new List<int>();
                foreach (MapleItem chair in chairs)
                {
                    if (!chairIds.Contains(chair.ItemId))
                    {
                        chairIds.Add(chair.ItemId);
                    }
                }
                pw.WriteInt(chairIds.Count);
                foreach (var id in chairIds)
                {
                    pw.WriteInt(id);
                }
                c.SendPacket(pw);

                pw = new PacketWriter(SendHeader.CharacterInfoFarmImage);
                pw.WriteInt(chr.AccountId);
                pw.WriteInt(0); //image data here
                c.SendPacket(pw);
            }
        }
    }
}
