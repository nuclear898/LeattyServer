using LeattyServer.ServerInfo.Party;
using LeattyServer.ServerInfo.Player;

namespace LeattyServer.ServerInfo.Packets.Handlers
{
    class PartyResponseHandler
    {
        public static void Handle(MapleClient c, PacketReader pr)
        {
            byte response = pr.ReadByte();
            int inviterId = pr.ReadInt();
            MapleCharacter chr = c.Account.Character;
            switch (response)
            {
                case 0x21: //Invite success
                    {
                        Invite inv;
                        if (chr.Invites.TryGetValue(InviteType.Party, out inv))
                        {
                            if (inv.SenderId == inviterId)
                            {
                                //response is same as here, changes sometimes
                                Program.GetClientByCharacterId(inviterId)?.SendPacket(MapleParty.Packets.InviteResponse(response, chr.Name));
                            }
                        }
                    }
                    break;
                case 0x22: //Blocking invitations
                    {
                        Invite inv;
                        if (chr.Invites.TryGetValue(InviteType.Party, out inv))
                        {
                            if (inv.SenderId == inviterId)
                            {
                                Program.GetClientByCharacterId(inviterId)?.Account.Character.SendWhiteMessage(chr.Name + " is currently blocking any party invitations.");
                            }
                        }
                    }
                    break;
                case 0x25://Deny invite
                    {
                        Invite inv;
                        if (chr.Invites.TryGetValue(InviteType.Party, out inv))
                        {
                            if (inv.SenderId == inviterId)
                            {
                                chr.Invites.Remove(InviteType.Party);
                                Program.GetCharacterById(inviterId)?.SendWhiteMessage(chr.Name + "has denied the party request.");
                            }
                        }
                    }
                    break;
                case 0x26: //Accept invite
                    {
                        Invite inv;
                        if (chr.Invites.TryGetValue(InviteType.Party, out inv))
                        {
                            if (inv.SenderId == inviterId)
                            {
                                chr.Invites.Remove(InviteType.Party);
                                if (chr.Party == null)
                                {
                                    MapleCharacter inviter = Program.GetCharacterById(inviterId);
                                    if (inviter != null)
                                    {
                                        if (inviter.Party == null) //If the inviter doesn't have a party yet one is created
                                        {
                                            inviter.Party = MapleParty.CreateParty(inviter, inviter.Name + "'s Party", false);
                                            inviter.Party.AddPlayer(chr);
                                        }
                                        else if (inviter.Party.LeaderId == inviter.Id) //Check if the inviter is the leader, in case of packet edit
                                        {
                                            inviter.Party.AddPlayer(chr);
                                        }
                                        else
                                        {
                                            inviter.SendWhiteMessage("You are not the leader of your party.");
                                        }
                                    }
                                }
                            }
                        }
                    }
                    break;
            }
        }
    }
}
