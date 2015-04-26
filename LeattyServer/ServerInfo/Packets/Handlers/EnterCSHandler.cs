using LeattyServer.ServerInfo.Player;

namespace LeattyServer.ServerInfo.Packets.Handlers
{
    class EnterCSHandler
    {        
        public static void Handle(MapleClient c, PacketReader pr)
        {
            MapleCharacter chr = c.Account.Character;
            if (chr.ActionState != ActionState.Enabled) return;
            //chr.ActionState = ActionState.Disabled;
            int tickCount = pr.ReadInt();
            chr.SendBlueMessage("The cash shop is disabled at this time, please go to the Free Market Entrance to buy Cash Items.");
            chr.EnableActions();

            /*
            PacketWriter pw = new PacketWriter();
            pw.WriteHeader(SendHeaders.ChangeChannel);
            pw.WriteBool(true);
            pw.WriteBytes(ServerConstants.NexonIp[3]);
            pw.WriteShort(ServerConstants.CashShopPort);
            pw.WriteByte(0);

            c.Account.MigrationData.ToChannel = 0xFF;
            c.Account.MigrationData.ToCashShop = true;
            c.Account.MigrationData.ReturnChannel = c.Channel;            
            c.Account.MigrationData.CheatTracker = c.CheatTracker;
            c.Account.MigrationData.Character = c.Account.Character;
            Program.EnqueueMigration(c.Account.MigrationData.CharacterId, c.Account.MigrationData);
            c.Account.Character.ActionState = ActionState.Disabled;

            c.SendPacket(pw);*/
        }
    }
}
