using LeattyServer.ServerInfo.AntiCheat;
using LeattyServer.ServerInfo.Player;

namespace LeattyServer.ServerInfo.Packets.Handlers
{
    class RegenerateHPMPHandler
    {
        public static void Handle(MapleClient c, PacketReader pr)
        {
            int tickCount = pr.ReadInt();
            pr.Skip(8);
            short hp = pr.ReadShort();
            short mp = pr.ReadShort();

            if (hp >= 10000 || mp >= 10000)
                c.CheatTracker.AddOffence(OffenceType.AbnormalValues);
            if (hp > 0)
            {
                //c.CheatTracker.Trigger(TriggerType.RegenerateHP);
                c.Account.Character.AddHP(hp, true);
            }
            if (mp > 0)
            {
                //c.CheatTracker.Trigger(TriggerType.RegenerateMP);
                c.Account.Character.AddMP(mp, true);
            }
        }
    }
}
