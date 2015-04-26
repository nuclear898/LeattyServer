using LeattyServer.Data;
using LeattyServer.Helpers;
using LeattyServer.ServerInfo.Player;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LeattyServer.DB.Models;
using LeattyServer.ServerInfo.Inventory;
using LeattyServer.ServerInfo.Packets;
using LeattyServer.ServerInfo.Packets.Handlers;

namespace LeattyServer.ServerInfo
{
    public class CashShop
    {
        public const short CashShopOperation = 0x67; //just increase this when the game updates

        public static void StartShowing(MapleClient c)
        {
            MapleCharacter.SendCSInfo(c);
            SendUnk1(c);
            SendInit(c);
            SendUnk2(c);

            ShowCategories(c);
            ShowLinkImage(c);
            ShowTop(c);
            ShowSpecial(c);
            ShowFeatured(c);
            //ShowSpecialSale(c);
        }

        public static void Select(MapleClient c, PacketReader pr)
        {
            if (pr.Available == 0) return;
            byte Type = pr.ReadByte();
            switch (Type)
            {
                case 0x65: //Select cat
                    int Cat = pr.ReadInt();
                    Cat %= 1000000;
                    int subCat = Cat;
                    subCat %= 10000;
                    Cat = (int)Math.Floor((decimal)Cat / 10000);
                    subCat = (int)Math.Floor((decimal)subCat / 100);
                    if (Cat == 0) return;
                    if (subCat == 1) return;
                    ShowCatItems(c, Cat, subCat);
                    break;
                case 0x66: //Leave CashShop
                    byte Channel = c.Account.MigrationData.ReturnChannel;
                    ChangeChannelHandler.Handle(c, new PacketReader(new byte[] { (byte)(Channel) }));
                    break;
                case 0x67: //Add Fav
                case 0x68: //Remove Fav
                    pr.ReadByte();
                    AddFav(c, pr.ReadInt());
                    break;
                case 0x69: //Like
                    int SN = pr.ReadInt();
                    AddLike(c, SN);
                    break;
                case 0x6D: //Select fav
                    ShowFav(c);
                    break;
                default:
                    ServerConsole.Warning("Unknow CashshopSelectType : {0}", Type);
                    ServerConsole.Info(Functions.ByteArrayToStr(pr.ToArray()));
                    break;
            }
        }

        public class DbItem
        {
            public byte MinLevel { get; set; }
            public byte Special { get; set; }
            public byte Featured { get; set; }

            public int CsId { get; set; }
            public int CId { get; set; }
            public int Id { get; set; }
            public int CatId { get; set; }
            public int ItemId { get; set; }
            public int Price { get; set; }
            public int NewPrice { get; set; }
            public int Amount { get; set; }
            public int TimesBought { get; set; }
            public int Order { get; set; }
            public int Likes { get; set; }

            public DateTime DateFrom { get; set; }
            public DateTime DateTo { get; set; }

            public String Image { get; set; }

        }

        public class Item
        {
            public byte Base;
            public byte Cat;
            public byte FakeBase;
            public byte FakeCat;
            public byte SubCat;
            public int ItemId;
            public int SN;
            public int Price;
            public int NewPrice;
            public int Likes;
            public int Amount;
            public byte minLevel;
            public int TimesBought;
            public int Order;
            public byte Special;
            public byte Featured;
            public String Url = String.Empty;
        }

        #region Packets
        private static void SendUnk1(MapleClient c)
        {
            PacketWriter pw = new PacketWriter();
            pw.WriteHeader(SendHeader.CSUnk1);
            pw.WriteZeroBytes(5);
            c.SendPacket(pw);
        }

        private static void SendUnk2(MapleClient c)
        {
            PacketWriter pw = new PacketWriter();
            pw.WriteHeader(SendHeader.CSPlayerHighlight);
            pw.WriteByte(7); //Type?
            pw.WriteBool(false); //Show char?
            //pw.WriteInt(1); //Char id
            //pw.WriteStaticString("Test", 13); //Char name
            //pw.WriteByte(0); //Unk
            //pw.WriteInt(-1); //Maple calls it groupid?
            //pw.WriteStaticString("Group Unknown", 20); //Group name?
            //pw.WriteByte(0);

            c.SendPacket(pw);
        }

        private static void SendInit(MapleClient c)
        {
            SendInventory(c);

            SendTokens(c);

            PacketWriter pw = new PacketWriter();
            pw.WriteHeader(SendHeader.CashShopInit);
            pw.WriteShort(CashShopOperation + 3);
            pw.WriteZeroBytes(3);
            c.SendPacket(pw);

            pw = new PacketWriter();
            pw.WriteHeader(SendHeader.CashShopInit);
            pw.WriteShort(CashShopOperation + 5);
            pw.WriteByte(0);
            c.SendPacket(pw);
        }

        private static void SendTokens(MapleClient c)
        {
            PacketWriter pw = new PacketWriter();
            pw.WriteHeader(SendHeader.CashShopTokens);
            pw.WriteInt(c.Account.NXCredit); 
            pw.WriteInt(c.Account.MaplePoints); 
            pw.WriteInt(0); //Unk?
            pw.WriteInt(c.Account.NXPrepaid);
            c.SendPacket(pw);
        }

        private static void SendInventory(MapleClient c)
        {
            PacketWriter pw = new PacketWriter();
            pw.WriteHeader(SendHeader.CashShopInit);
            pw.WriteShort(CashShopOperation + 2);
            List<InventoryItem> CashInventory =new List<InventoryItem>();
            int characterId = c.Account.Character.Id;
            using (LeattyContext DBContext = new LeattyContext())
            {
                //CashInventory = DBContext.InventoryItems.Where(x => x.CharacterId == characterId && x.Inventory == (sbyte)MapleInventoryType.CashShop).ToList();
            }
            pw.WriteShort((short)CashInventory.Count()); //Item count
            foreach (InventoryItem DbItem in CashInventory)
            {
                pw.WriteLong(DbItem.Id);
                pw.WriteLong(c.Account.Id);
                pw.WriteInt(DbItem.ItemId);
                pw.WriteInt(0); //IsFirst? o-o"
                pw.WriteShort(DbItem.Quantity);
                pw.WriteStaticString(c.Account.Character.Name, 13);
                pw.WriteLong(DateTime.UtcNow.ToFileTimeUtc()); //Exp time
                pw.WriteLong(0); //IsFirst? o-o"
                pw.WriteZeroBytes(19);
                pw.WriteHexString("40 E0 FD 3B 37 4F");
                pw.WriteBool(true);
                pw.WriteZeroBytes(16);

                pw.WriteInt(0); //Todo: pet stuff
            }
            pw.WriteShort(4); //Storage slots?
            pw.WriteShort(0xB); //Character slots?
            pw.WriteShort(0);
            pw.WriteShort(0xB); //Character count?
            c.SendPacket(pw);
        }

        private static void ShowCategories(MapleClient c)
        {
            PacketWriter HeaderPw = new PacketWriter();
            PacketWriter pw = new PacketWriter();
            HeaderPw.WriteHeader(SendHeader.CashShopInfo);
            HeaderPw.WriteByte(3); //Type
            HeaderPw.WriteBool(true); //Unk

            byte count = 0;

            pw.WriteInt(GetCatInt(Base: 2));
            pw.WriteMapleString("Favorite");
            pw.WriteInt(1);
            pw.WriteInt(0); //Type, 1 = New, 2 = Hot
            pw.WriteInt(0);
            count++;

            List<CashshopCat> Categories;
            using (LeattyContext DBContext = new LeattyContext())
            {
                //Categories = DBContext.CashshopCats.Where(x => x.ParentId == 0).ToList();
                Categories = new List<CashshopCat>();
            }
            foreach (CashshopCat Category in Categories)
            {
                int Id = Category.CsId;
                pw.WriteInt(GetCatInt(Id));
                pw.WriteMapleString(Category.Name);
                pw.WriteInt(1);
                pw.WriteInt(Category.Type);
                pw.WriteInt(0);

                List<CashshopCat> SubCategories;
                using (LeattyContext DBContext = new LeattyContext())
                {
                    //SubCategories = DBContext.CashshopCats.Where(x => x.ParentId == Category.Id).ToList();
                    SubCategories = new List<CashshopCat>();
                }
                foreach (CashshopCat SubCategory in SubCategories)
                {
                    int subId = SubCategory.Id;
                    pw.WriteInt(GetCatInt(Id, subId));
                    pw.WriteMapleString(SubCategory.Name);
                    pw.WriteInt(1);
                    pw.WriteInt(SubCategory.Type);
                    pw.WriteInt(0);
                    count++;
                }
                count++;
            }

            //Somehow setByte stopped working </care>
            HeaderPw.WriteByte(count);
            HeaderPw.WriteBytes(pw.ToArray());

            c.SendPacket(HeaderPw);
        }

        private static void ShowLinkImage(MapleClient c)
        {
            PacketWriter pw = new PacketWriter();
            pw.WriteHeader(SendHeader.CashShopInfo);
            pw.WriteByte(4); //Type
            pw.WriteBool(true); //Unk

            List<Item> Items = GetItemsWithImage();
            pw.WriteByte((byte)Items.Count); //Count

            foreach (Item Item in Items)
            {
                ShowCashItem(pw, Item, c.Account.Id);
            }

            c.SendPacket(pw);
        }

        private static void ShowTop(MapleClient c)
        {
            PacketWriter pw = new PacketWriter();
            pw.WriteHeader(SendHeader.CashShopInfo);
            pw.WriteByte(5); //Type
            pw.WriteBool(true); //Unk

            List<Item> Items = GetTop();
            pw.WriteByte((byte)Items.Count); //Count

            foreach (Item Item in Items)
            {
                ShowCashItem(pw, Item, c.Account.Id);
            }

            c.SendPacket(pw);
        }

        private static void ShowSpecial(MapleClient c)
        {
            PacketWriter pw = new PacketWriter();
            pw.WriteHeader(SendHeader.CashShopInfo);
            pw.WriteByte(6); //Type
            pw.WriteBool(true); //Unk

            List<Item> Items = GetSpecial();
            pw.WriteByte((byte)Items.Count); //Count
            foreach (Item Item in Items)
            {
                ShowCashItem(pw, Item, c.Account.Id);
            }

            c.SendPacket(pw);
        }

        private static void ShowFeatured(MapleClient c)
        {
            PacketWriter pw = new PacketWriter();
            pw.WriteHeader(SendHeader.CashShopInfo);
            pw.WriteByte(8); //Type
            pw.WriteBool(true); //Unk

            List<Item> Items = GetFeatured();
            pw.WriteByte((byte)Items.Count); //Count
            foreach (Item Item in Items)
            {
                ShowCashItem(pw, Item, c.Account.Id);
            }

            c.SendPacket(pw);
        }

        private static void ShowSpecialSale(MapleClient c)
        {
            PacketWriter pw = new PacketWriter();
            pw.WriteHeader(SendHeader.CashShopInfo);

            pw.WriteByte(9); //Type
            pw.WriteBool(true); //Unk
            pw.WriteByte(0); //Count

            c.SendPacket(pw); //Todo make this function work
        }

        private static void ShowCashItem(PacketWriter pw, Item Item, int AccountId)
        {
            pw.WriteInt(GetCatInt(Base: Item.FakeBase));
            pw.WriteInt(GetCatInt(Base: Item.FakeBase, Cat: Item.FakeCat, Sub: Item.SubCat));
            pw.WriteInt(GetCatInt(Base: Item.Base, Cat: Item.Cat, Sub: Item.SubCat));
            pw.WriteMapleString(Item.Url);
            pw.WriteInt(Item.SN);
            pw.WriteInt(Item.ItemId);

            pw.WriteInt(1);
            pw.WriteInt(4);
            pw.WriteInt(1);
            pw.WriteInt(0);

            pw.WriteInt(Item.Price);

            long Date1 = DateTime.UtcNow.ToFileTimeUtc();
            long Date2 = DateTime.UtcNow.AddDays(1).ToFileTimeUtc();
            pw.WriteLong(Date1);
            pw.WriteLong(Date2);
            pw.WriteLong(Date1);
            pw.WriteLong(Date2);
            pw.WriteLong(Item.NewPrice);
            pw.WriteInt(Item.Amount);
            pw.WriteInt(30);

            pw.WriteZeroBytes(10); //Bunch of booleans

            pw.WriteInt(2); //Has something todo with gift enable etc
            pw.WriteInt(Item.Likes);
            pw.WriteInt(Item.minLevel);
            pw.WriteMapleString(""); //special offer (etc.wz)

            pw.WriteZeroBytes(8); //more booleans?
            pw.WriteBool(IsItemFav(AccountId, Item.SN));
            pw.WriteBool(false);

            pw.WriteInt(0); //Todo package items
            //[02 00 00 00] 
            //[1E A4 98 00] [31 4C 4C 00] [00 00 00 00] [01 00 00 00] [01 00 00 00] [00 00 00 00] [01 00 00 00] [00 00 00 00] [02 00 00 00] 
            //[1F A4 98 00] [7E 80 1B 00] [00 00 00 00] [00 00 00 00] [00 00 00 00] [00 00 00 00] [01 00 00 00] [07 00 00 00] [02 00 00 00]
            //pw.WriteInt(PackageItems.Count);
            //foreach (int pItemId in PackageItems)
            //{
            //    pw.WriteInt(GetUniqueID());
            //    pw.WriteInt(pItemId); //item id
            //    pw.WriteInt(0);
            //    pw.WriteInt(0); //Normal price
            //    pw.WriteInt(0); //Package price

            //    pw.WriteZeroBytes(4);
            //    pw.WriteInt(10); //amount sold?
            //    pw.WriteInt(0x5A);
            //    pw.WriteInt(2);
            //}
        }

        private static void ShowFav(MapleClient c)
        {
            List<Item> Items = GetItemsForFav(c.Account.Id);
            PacketWriter pw = new PacketWriter();
            pw.WriteHeader(SendHeader.CashShopItemAction);
            pw.WriteByte(0x12); //Type
            pw.WriteByte(1);
            pw.WriteByte((byte)Items.Count);
            foreach (Item Item in Items)
            {
                ShowCashItem(pw, Item, c.Account.Id);
            }
            c.SendPacket(pw);
        }

        private static void ShowCatItems(MapleClient c, int Cat, int subCat)
        {
            List<Item> Items = GetItemsForCat(Cat, subCat);
            int totalCount = Items.Count;

            byte MaxCount = 50;
            int times = (int)Math.Ceiling((float)totalCount / MaxCount);
            byte left = (byte)(totalCount % MaxCount);
            PacketWriter pw = new PacketWriter();
            for (int i = 0; i < times; i++)
            {
                pw.Position = 0;
                pw.WriteHeader(SendHeader.CashShopItemAction);
                pw.WriteByte(0x0B); //Type
                if (i + 1 == times)
                {
                    pw.WriteByte(1);
                    pw.WriteByte(left);
                    foreach (Item Item in Items.GetRange(i * MaxCount, left))
                    {
                        ShowCashItem(pw, Item, c.Account.Id);
                    }
                }
                else
                {
                    pw.WriteByte(2);
                    pw.WriteByte(MaxCount);
                    foreach (Item Item in Items.GetRange(i * MaxCount, MaxCount))
                    {
                        ShowCashItem(pw, Item, c.Account.Id);
                    }
                }
                c.SendPacket(pw);
            }
        }

        private static void SetItemFav(MapleClient c, int SN, bool enable)
        {
            PacketWriter pw = new PacketWriter();
            pw.WriteHeader(SendHeader.CashShopItemAction);
            if (enable)
            {
                pw.WriteByte(0x0E);
                pw.WriteByte(1);
                pw.WriteInt(SN);
            }
            else
            {
                pw.WriteByte(0x10);
                pw.WriteByte(1);
                pw.WriteInt(SN);
                pw.WriteByte(1);
            }
            c.SendPacket(pw);
        }

        #endregion

        #region Data

        private static List<Item> GetItemsForFav(int AccountId, byte Cat = 1, byte SubCat = 1, byte Base = 2)
        {
            List<Item> Ret = new List<Item>();
            using (LeattyContext DBContext = new LeattyContext())
            {
                foreach (CashshopFavorite FavItem in DBContext.CashshopFavorites.Where(x => x.AccountId == AccountId))
                {
                    DbItem DbItem = DataBuffer.CSItems.SingleOrDefault(x => x.Id == FavItem.ItemId);
                    if (DbItem == null) continue;
                    Item Item = DbToNormalItem(DbItem);
                    Item.Base = Base;
                    Item.Cat = Cat;
                    Item.FakeBase = Base;
                    Item.FakeCat = Cat;
                    Ret.Add(Item);
                }
            }
            return Ret;
        }

        private static List<Item> GetTop()
        {
            List<Item> Ret = new List<Item>();
            byte FakeCat = 2, FakeSub = 0;
            foreach (DbItem DbItem in DataBuffer.CSItems.OrderByDescending(x => x.TimesBought).Take(6))
            {
                Item Item = DbToNormalItem(DbItem);
                Item.Base = 1;
                Item.Cat = 1;
                Item.FakeBase = 3;
                Item.FakeCat = FakeCat;
                Item.SubCat = FakeSub;

                FakeSub++;
                Ret.Add(Item);
            }
            return Ret;
        }

        private static List<Item> GetSpecial()
        {
            List<Item> Ret = new List<Item>();
            byte FakeCat = 3, FakeSub = 0;
            foreach (DbItem DbItem in DataBuffer.CSItems.Where(x => x.Special >= 1).Take(6))
            {
                Item Item = DbToNormalItem(DbItem);
                Item.Base = 1;
                Item.Cat = 2;
                Item.FakeBase = 3;
                Item.FakeCat = FakeCat;
                Item.SubCat = FakeSub;

                FakeSub++;
                Ret.Add(Item);
            }
            return Ret;
        }

        private static List<Item> GetFeatured()
        {
            List<Item> Ret = new List<Item>();
            byte FakeCat = 4, FakeSub = 0;
            foreach (DbItem DbItem in DataBuffer.CSItems.Where(x => x.Featured >= 1).Take(6))
            {
                Item Item = DbToNormalItem(DbItem);
                Item.Base = 1;
                Item.Cat = 3;
                Item.FakeBase = 3;
                Item.FakeCat = FakeCat;
                Item.SubCat = FakeSub;

                FakeSub++;
                Ret.Add(Item);
            }
            return Ret;
        }

        private static List<Item> GetItemsWithImage()
        {
            List<Item> Ret = new List<Item>();
            byte FakeCat = 1, FakeSub = 0;
            foreach (DbItem DbItem in DataBuffer.CSItems.Where(x => !String.IsNullOrEmpty(x.Image)))
            {
                Item Item = DbToNormalItem(DbItem);
                Item.Base = 1;
                Item.Cat = 0;
                Item.FakeBase = 3;
                Item.FakeCat = FakeCat;
                Item.SubCat = FakeSub;

                FakeSub++;
                Ret.Add(Item);
            }
            return Ret;
        }

        private static List<Item> GetItemsForCat(int Cat, int SubCat)
        {
            return GetItemsForCat((byte)Cat, (byte)SubCat);
        }

        private static List<Item> GetItemsForCat(byte Cat, byte SubCat, byte Base = 1, byte FakeBase = 1)
        {
            List<Item> Ret = new List<Item>();
            foreach (DbItem DbItem in DataBuffer.CSItems.Where(x => x.CId == SubCat))
            {
                Item Item = DbToNormalItem(DbItem);
                Item.Base = Base;
                Item.Cat = Cat;
                Item.FakeBase = FakeBase;
                Item.FakeCat = Cat;
                Ret.Add(Item);
            }
            return Ret;
        }

        private static void AddLike(MapleClient c, int SN)
        {
            /*using (LeattyContext DBContext = new LeattyContext())
            {
                int cId = c.Account.Character.Id;
                CashshopItem ItemToLike = DBContext.CashshopItems.SingleOrDefault(x => x.Id == SN);
                int count = DBContext.CashshopLikes.Count(x => x.CharacterId == cId && x.CsId == SN);
                if (count == 0 && ItemToLike != null)
                {
                    CashshopLike InsertLike = new CashshopLike();
                    InsertLike.CharacterId = cId;
                    InsertLike.CsId = SN;
                    DBContext.CashshopLikes.Add(InsertLike);
                    Item Liked = DbToNormalItem(DataBuffer.CSItems.Single(x => x.Id == SN));
                    Liked.Likes += 1;
                    ItemToLike.Likes = Liked.Likes;
                    DBContext.SaveChanges();
                }
            }*/
        }

        private static void AddFav(MapleClient c, int SN)
        {
            /*using (LeattyContext DBContext = new LeattyContext())
            {
                bool bIsItemFav = IsItemFav(c.Account.Id, SN);
                CashshopItem ItemToFav = DBContext.CashshopItems.SingleOrDefault(x => x.Id == SN);
                if (!bIsItemFav)
                {
                    CashshopFavorite InsertFav = new CashshopFavorite();
                    InsertFav.ItemId = SN;
                    InsertFav.AccountId = c.Account.Id;
                    DBContext.CashshopFavorites.Add(InsertFav);
                }
                else
                    DBContext.CashshopFavorites.RemoveRange(DBContext.CashshopFavorites.Where(x => x.ItemId == SN && x.AccountId == c.Account.Id));

                DBContext.SaveChanges();
                SetItemFav(c, SN, !bIsItemFav);
            }*/
        }

        private static bool IsItemFav(int AccountId, int SN)
        {
            using (LeattyContext DBContext = new LeattyContext())
            {
                return DBContext.CashshopFavorites.Count(x => x.ItemId == SN && x.AccountId == AccountId) > 0;
            }
        }
        #endregion

        #region Etc
        private static Item DbToNormalItem(DbItem Item)
        {
            return new Item()
            {
                SubCat = (byte)Item.CId,
                SN = Item.Id,
                ItemId = Item.ItemId,
                Price = Item.Price,
                NewPrice = Item.NewPrice,
                Amount = Item.Amount,
                minLevel = Item.MinLevel,
                Likes = Item.Likes,
                Order = Item.Order,
                TimesBought = Item.TimesBought,
                Special = Item.Special,
                Featured = Item.Featured
            };
        }

        private static int GetCatInt(int Cat = 0, int Sub = 0, int Base = 1)
        {
            return Base * 1000000 + Cat * 10000 + Sub * 100;
        }

        private static AutoIncrement UniqueID = new AutoIncrement();
        private static int GetUniqueID()
        {
            return UniqueID.Get + 10000000;
        }
        #endregion
    }
}
