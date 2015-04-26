using LeattyServer.ServerInfo.Map;
using LeattyServer.ServerInfo.Player;

namespace LeattyServer.ServerInfo.Packets.Handlers
{
    public class EnterDoorHandler
    {
        public static void Handle(MapleClient c, PacketReader pr)
        {            
            MapleCharacter chr = c.Account.Character;
            if (chr.ActionState == ActionState.Enabled) 
            {
                int doorOwnerId = pr.ReadInt();
                bool back = pr.ReadBool(); //return to source door if used in a town
                SpecialPortal door = chr.Map.GetDoor(doorOwnerId);
                if (door != null)
                {
                    if (!door.IsPartyObject || (doorOwnerId == chr.Id || (chr.Party != null && chr.Party.Id == door.PartyId)))
                    {
                        door.Warp(chr);
                        return;
                    }
                }
                chr.EnableActions();
            }
        }
    }
}
