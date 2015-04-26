using System.Linq;
using LeattyServer.ServerInfo.Player;

namespace LeattyServer.ServerInfo.Packets.Handlers
{
    public class SetAccountPicHandler
    {
        public static void Handle(MapleClient c, PacketReader pr)
        {
            if (c.Account.MigrationData == null)
            {
                c.Disconnect("Account was found to have no migration data");
                return;
            }
            pr.Skip(1);
            pr.Skip(1);
            int characterId = pr.ReadInt();
            string mac = pr.ReadMapleString();
            string mac_hdd = pr.ReadMapleString();
            string newpic = pr.ReadMapleString();
            if (!c.Account.HasPic() && newpic.Length >= 6 && newpic.Length <= 16 && c.Account.HasCharacter(characterId) && Program.ChannelServers.Keys.Contains(c.Channel))
            {
                ushort port = Program.ChannelServers[c.Channel].Port;
                c.Account.MigrationData.CharacterId = characterId;
                c.Account.MigrationData.Character = MapleCharacter.LoadFromDatabase(characterId, false);
                c.Account.MigrationData.Character.Hidden = c.Account.IsGM;
                c.Account.SetPic(newpic);

                Program.EnqueueMigration(characterId, c.Account.MigrationData);

                c.SendPacket(ChooseCharWithPicHandler.ChannelIpPacket(port, characterId));
            }
        }
    }
}
