using LeattyServer.Helpers;
using LeattyServer.ServerInfo.Player;

namespace LeattyServer.ServerInfo.Packets.Handlers
{
    class GuildActionHandler
    {
        public static void Handle(MapleClient c, PacketReader pr)
        {
            byte type = pr.ReadByte();
            MapleCharacter chr = c.Account.Character;
            if (chr.Guild != null)
            {
                switch (type)
                {
                    case 0x0E://setGuildRanks
                        string rank1 = pr.ReadMapleString();
                        string rank2 = pr.ReadMapleString();
                        string rank3 = pr.ReadMapleString();
                        string rank4 = pr.ReadMapleString();
                        string rank5 = pr.ReadMapleString();
                        if (rank1.Length >= 4 && rank2.Length >= 4 && rank3.Length >= 4)
                        {
                            chr.Guild.RankTitles[0] = rank1;
                            chr.Guild.RankTitles[1] = rank2;
                            chr.Guild.RankTitles[2] = rank3;
                            if (rank4.Length >= 4)
                            {
                                chr.Guild.RankTitles[3] = rank4;
                                if (rank5.Length >= 4)
                                {
                                    chr.Guild.RankTitles[4] = rank5;
                                }
                            }
                            chr.Guild.UpdateGuildData();
                        }
                        break;
                    case 0x11://setnotice
                        string notice = pr.ReadMapleString();
                        if (chr.GuildRank == 1 || chr.GuildRank == 2)
                        {
                            chr.Guild.ChangeNotice(notice);
                        }
                        break;
                    case 0x1D://Increase skill
                        break;
                    case 0x07://leave guild
                        int charid = pr.ReadInt();
                        chr.Guild.RemoveCharacter(chr);

                        break;
                    case 0x05://invite character
                        if (chr.GuildRank == 1 || chr.GuildRank == 2)
                        {
                            string character = pr.ReadMapleString();
                            MapleClient invitee = Program.GetClientByCharacterName(character);
                            if (invitee != null)
                            {
                                if (invitee.Account.Character.Guild == null)
                                {
                                    Invite iv = null;
                                    if (invitee.Account.Character.Invites.TryGetValue(InviteType.Guild,out iv))
                                    {
                                        invitee.Account.Character.Invites.Add(InviteType.Guild,new Invite((int)chr.Guild.GuildId, InviteType.Guild));
                                        invitee.SendPacket(chr.Guild.GenerateGuildInvite(chr));
                                    }
                                    else//already invited
                                    {
                                    }
                                }
                                else//already in guild
                                {
                                    PacketWriter pw = new PacketWriter();
                                    pw.WriteHeader(SendHeader.GuildData);
                                    pw.WriteByte(0x2E);
                                    c.SendPacket(pw);
                                }
                            }
                            else//couldnt find character in channel
                            {
                                PacketWriter pw = new PacketWriter();
                                pw.WriteHeader(SendHeader.GuildData);
                                pw.WriteByte(0x30);
                                c.SendPacket(pw);
                            }
                        }
                        break;
                    case 0x1F://set master
                        if (chr.Guild.LeaderId == (uint)chr.Id)
                        {
                            int id = pr.ReadInt();
                            if (id != chr.Id)
                            {
                                MapleClient newLeader = Program.GetClientByCharacterId(id);
                                if (newLeader.Account.Character.Guild != null && newLeader.Account.Character.Guild == chr.Guild)
                                {
                                    chr.Guild.SetMaster(newLeader.Account.Character,chr);
                                }
                            }
                        }
                        break;
                    case 0x0F:
                        if (chr.GuildRank == 1 || chr.GuildRank == 2)
                        {
                            int id = pr.ReadInt();
                            if (id != chr.Id)
                            {
                                MapleClient rankClient = Program.GetClientByCharacterId(id);
                                MapleCharacter rankchr = rankClient.Account.Character;
                                if (rankchr.Guild == chr.Guild)
                                {
                                    byte rank = pr.ReadByte();
                                    if (rank > 1 && rank <= 5)
                                    {
                                        chr.Guild.ChangeRank(rankchr, rank);
                                    }
                                }
                            }
                        }
                        break;
                    case 0x08:
                        if (chr.GuildRank == 1 || chr.GuildRank == 2)
                        {
                            int id = pr.ReadInt();
                            MapleClient kickVictim = Program.GetClientByCharacterId(id);
                            MapleCharacter kickChr = kickVictim.Account.Character;
                            if (kickChr.Guild == chr.Guild)
                            {
                                chr.Guild.KickCharacter(kickChr);
                            }
                        }
                        break;
                }
            }
            else
            {
                switch (type)
                {
                    case 0x06:
                        uint guildID = pr.ReadUInt();
                        int charID = pr.ReadInt();
                        
                        Invite invite = null;
                        if (chr.Invites.TryGetValue(InviteType.Guild,out invite) && invite.SenderId == guildID)
                        {
                            chr.Invites.Remove(InviteType.Guild);
                            Guild.MapleGuild guild = ServerInfo.Guild.MapleGuild.FindGuild((int)guildID);
                            guild.AddCharacter(chr);
                            Guild.MapleGuild.UpdateCharacterGuild(chr,guild.Name);
                            guild.BroadcastCharacterJoinedMessage(chr);
                        }
                        else
                        {
                            ServerConsole.Warning("[GuildActionHandler] The character {0} was probably packet editing", chr.Name);
                        }
                        break;
                }
            }
        }
    }
}
