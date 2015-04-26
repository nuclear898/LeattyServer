using LeattyServer.Helpers;
using LeattyServer.ServerInfo.Player;

namespace LeattyServer.ServerInfo.Packets.Handlers
{
    class LoginAccountHandler
    {
        public static void Handle(MapleClient c, PacketReader pr)
        {
            string accountPassword = pr.ReadMapleString();
            string accountName = pr.ReadMapleString();
            if (accountName.StartsWith("NP12:auth"))
            {
                c.SendPacket(MapleCharacter.ServerNotice("Please start MapleStory using the leatty launcher.", 1));
                return;
            }
            ServerConsole.Debug("Login account: " + accountName);
            MapleAccount account = MapleAccount.GetAccountFromDatabase(accountName);

            if (account != null)
            {
                if (Program.IsAccountOnline(account.Id))
                {
                    c.SendPacket(LoginAccountFailed(7));
                }
                else
                {
                    if (account.CheckPassword(accountPassword))
                    {
                        c.Account = account;
                        c.SendPacket(LoginAccountSuccess(account));
                    }
                    else
                    {
                        c.SendPacket(LoginAccountFailed(4));
                    }
                }
            }
            else
            {
                c.SendPacket(LoginAccountFailed(5));
            }
        }

        public static PacketWriter LoginAccountSuccess(MapleAccount acc)
        {
            PacketWriter pw = new PacketWriter();
            pw.WriteHeader(SendHeader.AccountLoginResponse);

            pw.WriteByte(0);
            pw.WriteByte(0);
            pw.WriteInt(0);
            pw.WriteInt(acc.Id);
            pw.WriteLong(0);
            pw.WriteShort(0);
            pw.WriteByte(0x95);
            pw.WriteMapleString(acc.Name);
            pw.WriteLong(0);
            pw.WriteShort(0);
            pw.WriteHexString("A0 2C D1 09 92 7C C5 01 0A 00 00 00 01 11 01 01 00 01 01 00 01 01 00 01 01 00 01 01 00 01 01 00 01 01 00 01 01 00 01 01 00 01 01 00 01 01 00 01 01 00 01 01 00 01 01 00 01 01 00 00 02 00 01 01 00 01 01 00 01 01 00 01 01 00 00 01 00 00 FF FF FF FF 01 01");
            /*pw.WriteHexString("60 AC CC 1F 57 C7 01");
            pw.WriteInt(0x38);
            pw.WriteBool(true); //?
            pw.WriteByte(0xB); //featured job, shows up at the front, 0xA = Zero, 0xB = BeastTamer
            pw.WriteBool(true); //Resistance
            pw.WriteBool(true); //Explorer
            pw.WriteBool(true); //Cygnus
            pw.WriteBool(true); //Aran
            pw.WriteBool(true); //Evan
            pw.WriteBool(true); //Mercedes
            pw.WriteBool(true); //Demon
            pw.WriteBool(true); //Phantom
            pw.WriteBool(true); //DualBlade
            pw.WriteBool(true); //Mihile
            pw.WriteBool(true); //Luminous
            pw.WriteBool(true); //Kaiser
            pw.WriteBool(true); //Angelic Buster
            pw.WriteBool(true); //Cannonneer
            pw.WriteBool(true); //Xenon
            pw.WriteBool(false); //Zero
            pw.WriteBool(true); //Jett
            pw.WriteBool(true); //Hayato
            pw.WriteBool(true); //Kanna
            pw.WriteBool(true); //BeastTamer
            */


            MigrationData migration = new MigrationData()
            {                
                CharacterId = 0,
                ConnectionAuth = Functions.RandomLong(),
                AccountName = acc.Name
            };
            acc.MigrationData = migration;
            pw.WriteLong(migration.ConnectionAuth);           
            return pw;
        }

        /* 
         * To ban make the structure:
         * [0000] header
         * [02] reason
         * [00]
         * [00000000]
         * [01] ban reason, can be anything from the list below
         * [long, expiring date. could also be permanent]
        01:Blocked for hacking or illegal use of third-party programs
        02:Your account has been blocked for using macro / auto-keyboard.
        03:Your account has been blocked for illicit promotion and advertising.
        04:Your account has been blocked for harrassment.
        05:Your account has been blocked for using profane language.
        06:Your account has been blocked for scamming.
        07:Your account has been blocked for misconduct
        08:Your account has been blocked for Illegal cash transaction
        09:Your account has been blocked for illegal charging/funding. Please contact customer support for further details
        10:Your account has been blocked for temporary request.
        11:Your account has been blocked for for impersonating GM
        12:Your account has been blocked for using illegal programs or violating the game policy
        13:Your account has been blocked for one of cursing, scamming, or illegal trading via Megaphones.
        14:Your account has been blocked by the MapleStory GM's for hacking.
        
         Other login failed reasons:
         03:Account is deleted or blocked
         04:Wrong password
         05:Account not registered
         07:Account is already logged in
         */
        static PacketWriter LoginAccountFailed(byte reason)
        {
            PacketWriter pw = new PacketWriter();
            pw.WriteHeader(SendHeader.AccountLoginResponse);

            pw.WriteByte(reason);
            pw.WriteByte(0);
            pw.WriteInt(0);

            return pw;
        }
    }
}