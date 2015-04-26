using System;
using System.Linq;
using System.Collections.Generic;
using LeattyServer.Data;
using LeattyServer.Helpers;
using LeattyServer;
using System.Data.Entity;
using LeattyServer.DB.Models;

namespace LeattyServer.ServerInfo.Player
{
    public class MapleAccount : Account
    {
        public MigrationData MigrationData;
        
        private Object ReleaseLock = new Object();

        public MapleCharacter Character { get; set; }

        MapleAccount(int id)
        {
            Id = id;            
        }

        public static MapleAccount GetAccountFromDatabase(String name)
        {
            try
            {
                Account DbAccount;
                using (LeattyContext DBContext = new LeattyContext())
                {
                    DbAccount = DBContext.Accounts.SingleOrDefault(x => x.Name.ToLower().Equals(name.ToLower()));
                }
                if (DbAccount == null) return null;
                MapleAccount ret = new MapleAccount(DbAccount.Id);
                ret.Name = DbAccount.Name;
                ret.Password = DbAccount.Password;
                ret.Key = DbAccount.Key;
                ret.AccountType = DbAccount.AccountType;
                ret.MaplePoints = DbAccount.MaplePoints;
                ret.NXCredit = DbAccount.NXCredit;
                ret.NXPrepaid = DbAccount.NXPrepaid;
                return ret;
            }
            catch (Exception)
            {
                return null;
            }
        }

        public bool IsDonor
        {
            get
            {
                return AccountType == 1;
            }
        }

        public bool IsGM
        {
            get
            {
                return AccountType >= 2;
            }
        }

        public bool IsAdmin
        {
            get
            {
                return AccountType >= 3;
            }
        }

        public void Release()
        {            
            lock (ReleaseLock)
            {                
                if (Character != null)
                {
                    bool hasMigration = Program.MigrationExists(Character.Id);
                    MapleCharacter.SaveToDatabase(Character);
                    Character.Release(hasMigration);
                    Character = null;
                }
                MigrationData = null;                
            }
        }

        public List<MapleCharacter> GetCharsFromDatabase()
        {
            List<Character> DbChars;
            using (LeattyContext DBContext = new LeattyContext())
            {
                DbChars = DBContext.Characters.Where(x => x.AccountId == Id).ToList();
            }
            List<MapleCharacter> ret = new List<MapleCharacter>();
            foreach (Character DbChar in DbChars)
            {
                MapleCharacter chr = MapleCharacter.LoadFromDatabase(DbChar.Id, true);
                if (chr != null)
                    ret.Add(chr);
            }
            return ret;
        }

        public bool HasCharacter(int characterId)
        {
            using (LeattyContext DBContext = new LeattyContext())
            {
                Character DbChar = DBContext.Characters.SingleOrDefault(x => x.Id == characterId);
                return DbChar != null;
            }
        }
        public static byte[] saltshaker(int chars)
        {
            byte[] b = new byte[chars / 2];
            Functions.RandomBytes(b);
            return b;
        }
        public void SetPic(string pic)
        {
            using (LeattyContext DBContext = new LeattyContext())
            {
                byte[] key = saltshaker(32);
                string hashedPic = Functions.GetHMACSha512(pic, key);
                Account DbAccount = DBContext.Accounts.SingleOrDefault(x => x.Id == Id);
                DbAccount.Pic = hashedPic;
                DbAccount.PicKey = key.ByteArrayToString(nospace: true);
                DBContext.Entry<Account>(DbAccount).State = EntityState.Modified;
                DBContext.SaveChanges();
            }
        }
        public bool CheckPic(string enteredPic)
        {
            Account DbAccount;
            using (LeattyContext DBContext = new LeattyContext())
            {
                DbAccount = DBContext.Accounts.SingleOrDefault(x => x.Id == Id);
            }
            if (DbAccount == null) return false;
            if (DbAccount.Pic == null || DbAccount.PicKey == null)
                return false;
            string hashedPic = DbAccount.Pic;
            byte[] picKey = Functions.HexToBytes(DbAccount.PicKey);

            string saltedSha512Pic = Functions.GetHMACSha512(enteredPic, picKey);

            if (saltedSha512Pic.Equals(hashedPic, StringComparison.OrdinalIgnoreCase))
                return true;
            return false;
        }
        public bool HasPic()
        {
            Account DbAccount;
            using (LeattyContext DBContext = new LeattyContext())
            {
                DbAccount = DBContext.Accounts.SingleOrDefault(x => x.Id == Id);
            }
            try
            {
                if (DbAccount.Pic.Length != 0)
                    return true;
            }
            catch (Exception) { }
            return false;

        }

        public static bool AccountExists(string name)
        {
            using (LeattyContext DBContext = new LeattyContext())
            {
                return DBContext.Accounts.SingleOrDefault(x => x.Name.ToLower().Equals(name.ToLower())) != null;
            }
        }

        public bool CheckPassword(string enteredPassword)
        {
            Account DbAccount;
            using (LeattyContext DBContext = new LeattyContext())
            {
                DbAccount = DBContext.Accounts.SingleOrDefault(x => x.Id == Id);
            }
            if (DbAccount == null) return false;
            string passwordHash = DbAccount.Password;
            byte[] key = Functions.HexToBytes(DbAccount.Key);

            string saltedSha512Password = Functions.GetHMACSha512(enteredPassword, key);

            if (passwordHash == enteredPassword) //temporary
                return true;
            else if (saltedSha512Password.Equals(passwordHash, StringComparison.OrdinalIgnoreCase))
                return true;
            else
                return false;
        }
    }
}
