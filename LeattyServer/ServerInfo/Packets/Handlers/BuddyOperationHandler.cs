using System;
using System.Linq;
using LeattyServer.Data;
using LeattyServer.DB.Models;
using LeattyServer.ServerInfo.BuddyList;
using LeattyServer.ServerInfo.Player;

namespace LeattyServer.ServerInfo.Packets.Handlers
{
    public static class BuddyOperationHandler
    {
        public static void Handle(MapleClient c, PacketReader pr)
        {
            byte operation = pr.ReadByte();
            switch (operation)
            {
                case 0x1: //Add buddy
                    {
                        MapleCharacter chr = c.Account.Character;
                        MapleBuddyList myList = chr.BuddyList;
                        if (myList.TotalBuddies >= myList.Capacity)
                        {
                            chr.SendPopUpMessage(string.Format("You have reached your limit of {0} buddies", myList.Capacity));
                            c.Account.Character.EnableActions();
                            return;
                        }
                        string buddyName = pr.ReadMapleString();
                        string groupName = pr.ReadMapleString();
                        string memo = pr.ReadMapleString();
                        if (buddyName.Length > 13 || groupName.Length > 16 || memo.Length > 256 || buddyName.Equals(chr.Name, StringComparison.OrdinalIgnoreCase))
                            return;
                        bool accountFriend = pr.ReadBool();

                        if (accountFriend)
                        {
                            #region accountFriend
                            string nickName = pr.ReadMapleString();
                            if (nickName.Length > 13)
                                return;
                            MapleClient buddyClient = Program.GetClientByCharacterName(buddyName);
                            if (buddyClient != null) //if buddy is online
                            {
                                int buddyAccountId = buddyClient.Account.Id;
                                if (buddyAccountId == chr.AccountId)
                                {
                                    c.SendPacket(CannotAddYourselfAsBuddy());
                                    return;
                                }
                                MapleBuddy newBuddy = myList.AddAccountBuddy(buddyAccountId, nickName, groupName, memo);
                                if (newBuddy != null) //newBuddy can be null if the buddy was already on the list
                                {
                                    MapleCharacter buddyCharacter = buddyClient.Account.Character;
                                    if (buddyCharacter.BuddyList.HasAccountBuddy(chr.AccountId)) //Buddy already has player on his list
                                    {
                                        if (!buddyCharacter.BuddyList.Invisible)
                                            newBuddy.Channel = buddyClient.Channel;
                                        if (!myList.Invisible)
                                            buddyCharacter.BuddyList.BuddyChannelChanged(buddyClient, chr.Id, chr.AccountId, chr.Name, true, c.Channel); //Tell buddy we are online
                                    }
                                    else
                                    {
                                        MapleBuddy buddysBuddy = buddyClient.Account.Character.BuddyList.AddAccountBuddyRequest(chr.AccountId, chr.Name);
                                        if (!myList.Invisible)
                                            buddyClient.SendPacket(BuddyRequest(buddysBuddy, chr.Id, chr.AccountId, chr.Level, chr.Job, chr.SubJob));
                                    }
                                    c.SendPacket(AddBuddy(newBuddy));
                                    c.SendPacket(BuddyRequestSuccessfullySent(nickName));
                                }
                                else
                                {
                                    c.SendPacket(AlreadyRegisteredAsBuddy());
                                }
                            }
                            else //Buddy is offline
                            {
                                //check if buddy exists in the database
                                using (LeattyContext dbContext = new LeattyContext())
                                {
                                    var dbCharacter = dbContext.Characters.FirstOrDefault(x => x.Name.Equals(buddyName, StringComparison.OrdinalIgnoreCase));
                                    if (dbCharacter != null)
                                    {
                                        int buddyAccountId = dbCharacter.AccountId;
                                        if (buddyAccountId == chr.AccountId)
                                        {
                                            c.SendPacket(CannotAddYourselfAsBuddy());
                                            return;
                                        }
                                        MapleBuddy newBuddy = myList.AddAccountBuddy(buddyAccountId, nickName, groupName, memo);
                                        if (newBuddy != null)
                                        {
                                            var buddyEntry = dbContext.Buddies.FirstOrDefault(x => x.AccountId == buddyAccountId && x.BuddyAccountId == chr.AccountId);
                                            if (buddyEntry == null) //If target player does not have the requester on his list
                                            {
                                                dbContext.Buddies.Add(new Buddy()
                                                {
                                                    IsRequest = true,
                                                    AccountId = buddyAccountId,
                                                    BuddyAccountId = chr.AccountId,
                                                    Group = MapleBuddyList.DEFAULT_GROUP,
                                                    Name = chr.Name,
                                                    Memo = string.Empty,
                                                });
                                                dbContext.SaveChanges();
                                            }
                                            c.SendPacket(AddBuddy(newBuddy));
                                            c.SendPacket(BuddyRequestSuccessfullySent(nickName));
                                        }
                                        else
                                        {
                                            c.SendPacket(AlreadyRegisteredAsBuddy());
                                        }
                                    }
                                    else
                                    {
                                        c.SendPacket(CharacterNotRegistered());
                                    }
                                }
                            }
                            #endregion
                        }
                        else
                        {
                            MapleClient buddyClient = Program.GetClientByCharacterName(buddyName);
                            if (buddyClient != null) //If buddy is online
                            {
                                MapleCharacter buddyCharacter = buddyClient.Account.Character;
                                int buddyCharacterId = buddyCharacter.Id;
                                if (buddyCharacter.AccountId == chr.AccountId)
                                {
                                    c.SendPacket(CannotAddYourselfAsBuddy());
                                    return;
                                }

                                MapleBuddy newBuddy = myList.AddCharacterBuddy(buddyCharacterId, buddyCharacter.Name, groupName, memo);
                                if (newBuddy != null) //newBuddy is null if the buddy is already on the list
                                {
                                    if (buddyCharacter.BuddyList.HasCharacterBuddy(chr.Id))
                                    {
                                        if (!buddyCharacter.BuddyList.Invisible)
                                            newBuddy.Channel = buddyClient.Channel;
                                        if (!myList.Invisible)
                                            buddyCharacter.BuddyList.BuddyChannelChanged(buddyClient, chr.Id, 0, chr.Name, false, c.Channel);
                                    }
                                    else
                                    {
                                        MapleBuddy buddysBuddy = buddyCharacter.BuddyList.AddCharacterBuddyRequest(chr.Id, chr.Name);
                                        buddyClient.SendPacket(BuddyRequest(buddysBuddy, chr.Id, chr.AccountId, chr.Level, chr.Job, chr.SubJob));
                                    }
                                    c.SendPacket(AddBuddy(newBuddy));
                                    c.SendPacket(BuddyRequestSuccessfullySent(buddyCharacter.Name));
                                }
                                else
                                {
                                    c.SendPacket(AlreadyRegisteredAsBuddy());
                                }
                            }
                            else //Buddy is offline
                            {
                                //check if buddy exists in the database
                                using (LeattyContext dbContext = new LeattyContext())
                                {
                                    var dbCharacter = dbContext.Characters.FirstOrDefault(x => x.Name.Equals(buddyName, StringComparison.OrdinalIgnoreCase));
                                    if (dbCharacter != null)
                                    {
                                        if (dbCharacter.AccountId == chr.AccountId)
                                        {
                                            c.SendPacket(CannotAddYourselfAsBuddy());
                                            return;
                                        }
                                        int buddyCharacterId = dbCharacter.Id;
                                        MapleBuddy newBuddy = myList.AddCharacterBuddy(buddyCharacterId, dbCharacter.Name, groupName, memo);
                                        if (newBuddy != null)
                                        {
                                            // Remove current buddy entries if existing
                                            var buddyEntry = dbContext.Buddies.FirstOrDefault(x => x.CharacterId == buddyCharacterId && x.BuddyCharacterId == chr.Id);
                                            if (buddyEntry == null)
                                            {
                                                dbContext.Buddies.Add(new Buddy()
                                                {
                                                    IsRequest = true,
                                                    CharacterId = buddyCharacterId,
                                                    BuddyCharacterId = chr.Id,
                                                    Group = MapleBuddyList.DEFAULT_GROUP,
                                                    Name = chr.Name,
                                                    Memo = string.Empty,
                                                });
                                                dbContext.SaveChanges();
                                            }
                                            c.SendPacket(AddBuddy(newBuddy));
                                            c.SendPacket(BuddyRequestSuccessfullySent(dbCharacter.Name));
                                        }
                                        else
                                        {
                                            c.SendPacket(AlreadyRegisteredAsBuddy());
                                        }
                                    }
                                    else
                                    {
                                        c.SendPacket(CharacterNotRegistered());
                                    }
                                }
                            }
                        }
                        break;

                    }
                case 0x2: //Accept character buddy request
                    {
                        int characterId = pr.ReadInt();
                        MapleBuddyList buddyList = c.Account.Character.BuddyList;
                        MapleBuddy buddy = buddyList.CharacterRequestAccepted(characterId);
                        if (buddy != null)
                        {
                            //Check if buddy is online
                            MapleClient requesterClient = Program.GetClientByAccountId(characterId);
                            if (requesterClient != null)
                            { 
                                MapleBuddyList requesterBuddyList = requesterClient.Account.Character.BuddyList;
                                //Update buddy's status
                                if (!requesterBuddyList.Invisible)
                                {
                                    buddy.Name = requesterClient.Account.Character.Name;
                                    buddy.Channel = requesterClient.Channel;
                                }
                                //Update my status to the new buddy
                                if (!buddyList.Invisible) 
                                {
                                    requesterBuddyList.BuddyChannelChanged(requesterClient, c.Account.Character.Id, c.Account.Id, c.Account.Character.Name, false, c.Channel);
                                }
                            }
                            //Update buddy to the client
                            buddyList.UpdateBuddyList(c);
                        }
                        break;
                    }
                case 0x3: //Accept Account Buddy request
                    {
                        int accountId = pr.ReadInt();
                        MapleBuddyList buddyList = c.Account.Character.BuddyList;
                        MapleBuddy buddy = buddyList.AccountRequestAccepted(accountId);
                        if (buddy != null)
                        {
                            //Check if buddy is online
                            MapleClient requesterClient = Program.GetClientByAccountId(accountId);
                            if (requesterClient != null) 
                            {
                                //Update buddy's status
                                MapleBuddyList requesterBuddyList = requesterClient.Account.Character.BuddyList;
                                if (!requesterBuddyList.Invisible)
                                {
                                    buddy.Name = requesterClient.Account.Character.Name;
                                    buddy.Channel = requesterClient.Channel;
                                }
                                //Update my status to the new buddy
                                if (!buddyList.Invisible)
                                {
                                    requesterClient?.Account.Character.BuddyList.BuddyChannelChanged(requesterClient, c.Account.Character.Id, c.Account.Id, c.Account.Character.Name, true, c.Channel);
                                }
                            }
                            //Update buddy to the client
                            buddyList.UpdateBuddyList(c);
                        }
                        break;
                    }
                case 0x4: //Delete characterbuddy
                    {
                        int characterId = pr.ReadInt();
                        MapleBuddyList buddyList = c.Account.Character.BuddyList;
                        if (buddyList.RemoveCharacterBuddy(characterId, c))
                        {
                            MapleClient requesterClient = Program.GetClientByAccountId(characterId);
                            requesterClient?.Account.Character.BuddyList.BuddyChannelChanged(requesterClient, c.Account.Character.Id, c.Account.Id, c.Account.Character.Name, false, c.Channel);
                        }
                        break;
                    }
                case 0x5: //Delete account buddy
                    {
                        int accountId = pr.ReadInt();
                        MapleBuddyList buddyList = c.Account.Character.BuddyList;
                        if (buddyList.RemoveAccountBuddy(accountId, c))
                        {
                            MapleClient requesterClient = Program.GetClientByAccountId(accountId);
                            requesterClient?.Account.Character.BuddyList.BuddyChannelChanged(requesterClient, c.Account.Character.Id, c.Account.Id, c.Account.Character.Name, true, c.Channel);
                        }
                        break;
                    }
                case 0x6: //Decline character buddy
                    {
                        int characterId = pr.ReadInt();
                        MapleClient requesterClient = Program.GetClientByCharacterId(characterId);
                        if (requesterClient != null)
                        {
                            requesterClient.Account.Character.BuddyList.RemoveCharacterBuddy(c.Account.Character.Id, requesterClient);
                            requesterClient.SendPacket(BuddyRequestDeclined(c.Account.Character.Name));
                        }
                        break;
                    }
                case 0x7: //Decline account buddy 
                    {
                        int accountId = pr.ReadInt();
                        MapleClient requesterClient = Program.GetClientByAccountId(accountId);
                        if (requesterClient != null)
                        {
                            requesterClient.Account.Character.BuddyList.RemoveAccountBuddy(c.Account.Id, requesterClient);
                            requesterClient.SendPacket(BuddyRequestDeclined(c.Account.Character.Name));
                        }
                        break;
                    }
                case 0xC: //Change alias and memo
                    {
                        bool accountBuddy = pr.ReadBool();
                        int characterId = pr.ReadInt();
                        int accountId = pr.ReadInt();
                        string newNickName = pr.ReadMapleString(); //not used for character buddy because you cannot change name
                        string newMemo = pr.ReadMapleString();
                        if (newNickName.Length > 13 || newMemo.Length > 256) return;
                        if (accountBuddy)
                        {
                            c.Account.Character.BuddyList.UpdateAccountBuddyMemo(c, accountId, newNickName, newMemo);
                        }
                        else
                        {
                            c.Account.Character.BuddyList.UpdateCharacterBuddyMemo(c, characterId, newMemo);
                        }
                        break;
                    }
                case 0xF: //offline mode
                    {
                        MapleCharacter chr = c.Account.Character;
                        chr.BuddyList.SetInvisible(true, chr.Id, chr.AccountId, chr.Name, -1, c);
                        break;
                    }
                case 0x10: //online mode
                    {
                        MapleCharacter chr = c.Account.Character;
                        chr.BuddyList.SetInvisible(false, chr.Id, chr.AccountId, chr.Name, c.Channel, c);
                        break;
                    }
            }
        }

        public static PacketWriter BuddyRequest(MapleBuddy buddy, int characterIdFrom, int accountIdFrom, int level, int job, int subJob)
        {
            PacketWriter pw = new PacketWriter(SendHeader.BuddyList);
            pw.WriteByte(0x16);
            pw.WriteBool(buddy.AccountBuddy);
            pw.WriteInt(characterIdFrom);
            pw.WriteInt(accountIdFrom);
            pw.WriteMapleString(buddy.NickName);
            pw.WriteInt(level);
            pw.WriteInt(job);
            pw.WriteInt(subJob);
            MapleBuddyList.Packets.AddBuddyInfo(pw, buddy);
            pw.WriteByte(0);
            return pw;
        }

        public static PacketWriter BuddyRequestSuccessfullySent(string name)
        {
            PacketWriter pw = new PacketWriter(SendHeader.BuddyList);
            pw.WriteByte(0x17);
            pw.WriteMapleString(name);
            return pw;
        }

        public static PacketWriter AlreadyRegisteredAsBuddy()
        {
            PacketWriter pw = new PacketWriter(SendHeader.BuddyList);
            pw.WriteByte(0x1A);
            return pw;
        }

        public static PacketWriter CannotAddYourselfAsBuddy()
        {
            PacketWriter pw = new PacketWriter(SendHeader.BuddyList);
            pw.WriteByte(0x1D);
            return pw;
        }



        public static PacketWriter CharacterNotRegistered()
        {
            PacketWriter pw = new PacketWriter(SendHeader.BuddyList);
            pw.WriteByte(0x1F);
            return pw;
        }

        public static PacketWriter AddBuddy(MapleBuddy buddy)
        {
            PacketWriter pw = new PacketWriter(SendHeader.BuddyList);
            pw.WriteByte(0x23);
            MapleBuddyList.Packets.AddBuddyInfo(pw, buddy);
            return pw;
        }

        public static PacketWriter BuddyRequestDeclined(string name)
        {
            PacketWriter pw = new PacketWriter(SendHeader.BuddyList);
            pw.WriteByte(0x30);
            pw.WriteMapleString(name);
            return pw;
        }
    }
}