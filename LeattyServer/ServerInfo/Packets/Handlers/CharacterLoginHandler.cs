using LeattyServer.ServerInfo.Map;
using LeattyServer.ServerInfo.Player;

//$$UNSAFE$$
namespace LeattyServer.ServerInfo.Packets.Handlers
{
    class CharacterLoginHandler
    {
        public static void Handle(MapleClient c, PacketReader pr)
        {
            //[00 00 00 00] [F2 07 00 00] 00 00 00 00 00 00 64 11 A0 D8 00 00 00 00 B8 54 00 00 38 9A FE 92 98 EB F3 B2
            pr.Skip(4); //v143
            int characterId = pr.ReadInt();
            pr.Skip(18);
            long ConnectionAuth = pr.ReadLong();
            MigrationData migration = Program.TryDequeueMigration(characterId, ConnectionAuth, c.Channel);
            if (migration != null || migration.Character == null)
            {
                MapleClient oldClient = Program.GetClientByCharacterId(migration.CharacterId);
                if (oldClient != null)
                {
                    oldClient.SClient.Dispose();
                }

                MapleAccount account = MapleAccount.GetAccountFromDatabase(migration.AccountName);
                MapleCharacter chr = migration.Character;

                account.Character = chr;
                c.Account = account;
                chr.Bind(c);
                account.MigrationData = migration;
                account.Character = chr;
                if (!migration.ToCashShop)
                {
                    MapleMap map = Program.GetChannelServer(c.Channel).GetMap(chr.MapId);
                    if (map != null)
                    {
                        chr.Map = map;
                        MapleCharacter.EnterChannel(c);
                        c.Account.Character.LoggedIn();                     

                        chr.Stats.Recalculate(chr);
                        chr.Position = map.GetStartpoint(0).Position;
                        map.AddCharacter(chr);
                    }
                }
                else
                {
                    CashShop.StartShowing(c);
                }
            }
            else
            {
                c.Disconnect("No migration data found.");
            }
        }
    }
}
