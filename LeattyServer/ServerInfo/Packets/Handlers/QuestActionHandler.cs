using LeattyServer.ServerInfo.Player;

namespace LeattyServer.ServerInfo.Packets.Handlers
{
    class QuestActionHandler
    {
        public static void Handle(MapleClient c, PacketReader pr)
        {
            byte action = pr.ReadByte();
            ushort questId = pr.ReadUShort();
            switch (action)
            {
                case 0: //Restore lost item
                {
                    //TODO
                    break;
                }
                case 1: //Start quest
                {   //[01] [D5 7D] [42 28 00 00] [00 00 00 00] [6F 05 0F 02] [00 00 00 00]                    
                    int npcId = pr.ReadInt();
                    c.Account.Character.StartQuest(questId, npcId);
                    break;
                }
                case 2: //Complete Quest
                {   //[02] [D5 7D] [42 28 00 00] [00 00 00 00] [F3 04 0F 02] [FF FF FF FF]
                    //[02] [D2 7D] [40 28 00 00] [00 00 00 00] [F7 05 9A 00] [FF FF FF FF]                    
                    int npcId = pr.ReadInt();
                    pr.Skip(8);
                    int choice = pr.ReadInt();
                    c.Account.Character.CompleteQuest(questId, npcId, choice);
                    break;
                }
                case 3: //Forfeit quest
                {   //[03] [27 08]                    
                    c.Account.Character.ForfeitQuest(questId);                  
                    break;
                }
                case 4: //Scripted start quest
                {   //[04] [7B 05] [84 71 0F 00] [57 0A 97 01]                    
                    int npcId = pr.ReadInt();
                    //TODO
                    break;
                }
                case 5: //Scripted end quest
                    //TODO
                    break;
            }
        }
    }
}
