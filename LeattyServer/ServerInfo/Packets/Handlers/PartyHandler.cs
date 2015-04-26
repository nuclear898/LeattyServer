using LeattyServer.Helpers;
using LeattyServer.ServerInfo.Party;
using LeattyServer.ServerInfo.Player;

namespace LeattyServer.ServerInfo.Packets.Handlers
{
    class PartyHandler
    {
        public static void Handle(MapleClient c, PacketReader pr)
        {
            byte operation = pr.ReadByte();
            MapleCharacter chr = c.Account.Character;
            switch (operation)
            {
                case 0x01: //Create party
                    if (chr.Party == null)
                    {
                        bool privateParty = !pr.ReadBool();
                        string partyName = pr.ReadMapleString();
                        chr.Party = MapleParty.CreateParty(chr, partyName, privateParty);
                    }
                    break;
                case 0x02: //Leave party
                    if (chr.Party != null)
                    {
                        chr.Party.RemovePlayer(chr.Id, false);
                    }
                    break;
                case 0x04: //Invite to party
                    string targetName = pr.ReadMapleString();
                    MapleCharacter target = Program.GetCharacterByName(targetName);
                    if (target != null && !target.Hidden)
                    {
                        if (target.Party != null)
                        {
                            chr.SendWhiteMessage("'" + targetName + "' is already in a party.");
                            return;
                        }
                        Invite inv;
                        if (target.Invites.TryGetValue(InviteType.Party, out inv))
                        {
                            if (inv.SenderId == chr.Id)
                                chr.SendWhiteMessage("You have already invited '" + targetName + "' to your party.");
                            else
                                chr.SendWhiteMessage("'" + targetName + "' currently has a party invite pending.");
                        }
                        else
                        {
                            if (chr.Party != null && chr.Party.LeaderId != chr.Id)
                            {
                                chr.SendWhiteMessage("You are not the leader of your party.");
                            }
                            else
                            {
                                target.Invites.Add(InviteType.Party, new Invite(chr.Id, InviteType.Party));
                                target.Client.SendPacket(MapleParty.Packets.GenerateInvite(chr));
                            }
                        }
                    }
                    else
                    {
                        chr.SendWhiteMessage("'" + targetName + "' could not be found.");
                    }
                    break;
                case 0x07: //Set leader
                    {
                        if (!chr.Map.PartyLeaderChangeLimit)
                        {
                            MapleParty party = chr.Party;
                            if (party != null && party.LeaderId == chr.Id)
                            {                                
                                party.SetLeader(pr.ReadInt());
                            }
                        }
                    }
                    break;
                case 0x0D: //Rename 
                    {
                        MapleParty party = chr.Party;
                        if (party != null && party.LeaderId == chr.Id)
                        {
                            party.Private = !pr.ReadBool();
                            party.Name = pr.ReadMapleString();
                            party.BroadcastPacket(MapleParty.Packets.UpdatePartyName(party));
                        }                        
                        break;
                    }
                /*
            case 0x22://deny invite
                {
                    int id = pr.ReadInt();
                    Invite inv;
                    if (chr.Invites.TryGetValue(InviteType.Party, out inv))
                    {
                        if (inv.SenderId == id)
                        {
                            chr.Invites.Remove(InviteType.Party);
                            MapleCharacter from = Program.GetCharacterById(id);
                            if (from != null)
                                from.SendWhiteMessage(chr.Name + " has denied the party request.");//should be a white message.
                        }
                    }
                }
                break;
            case 0x23://accept invite
                {
                    int id = pr.ReadInt();
                    Invite inv;
                    if (chr.Invites.TryGetValue(InviteType.Party, out inv))
                    {
                        if (inv.SenderId == id)
                        {
                            chr.Invites.Remove(InviteType.Party);
                            if (chr.Party == null)
                            {
                                MapleClient victim = Program.GetClientByCharacterId(id);
                                MapleParty p = victim.Account.Character.Party;
                                if (p != null)
                                {
                                    chr.Party = p;
                                    p.AddPlayer(chr);
                                }
                            }
                            else
                            {
                            }
                        }
                    }
                }
                break;*/
                default:
                    ServerConsole.Warning("Unknown PartyHandler operation: 0x" + operation.ToString("X"));
                    break;
            }
        }
    }
}
