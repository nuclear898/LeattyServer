using System.Drawing;
using LeattyServer.Helpers;
using LeattyServer.ServerInfo.Player;

namespace LeattyServer.ServerInfo.Packets.Handlers
{
    internal class LootItemHandler
    {
        public static void HandlePlayer(MapleClient c, PacketReader pr)
        {
            MapleCharacter chr = c.Account.Character;
            if (!chr.DisableActions()) return;
            pr.Skip(1);
            try
            {
                int tickCount = pr.ReadInt();
                Point position = pr.ReadPoint();
                int objectId = pr.ReadInt();

                if (position.DistanceTo(chr.Position) >= 50)
                    c.CheatTracker.AddOffence(AntiCheat.OffenceType.LootFarAwayItem);

                bool success = chr.Map.HandlePlayerItemPickup(c, objectId);
                chr.EnableActions(!success); //Client doesn't need to be notified if it was succesful
            }
            catch
            {
                chr.EnableActions();
            }
        }
    }
}
