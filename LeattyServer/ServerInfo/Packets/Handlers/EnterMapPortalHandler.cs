using LeattyServer.ServerInfo.Player;

namespace LeattyServer.ServerInfo.Packets.Handlers
{
    class EnterMapPortalHandler
    {
        public static void Handle(MapleClient c, PacketReader pr)
        {
            MapleCharacter chr = c.Account.Character;
            if (chr.Hp <= 0 || chr.ActionState == ActionState.Dead)
            {
                chr.Revive(true);
                return;
            }

            if (!chr.DisableActions())
                return;

            if (pr.Available <= 0)
            {
                chr.EnableActions();
                return;
            }
            pr.Skip(1);
            int targetMap = pr.ReadInt();
            pr.Skip(4);
            string portalName = pr.ReadMapleString();
            if (pr.Available >= 4)
            {
                int tickCount = pr.ReadInt();
            }
            if (c.Account.Character.Map != null)
            {
                if (targetMap == -1)
                {
                    c.Account.Character.Map.EnterPortal(c, portalName);
                }
            }
        }
    }
}
