using LeattyServer.Data;
using LeattyServer.Data.WZ;
using LeattyServer.ServerInfo.Player;

namespace LeattyServer.ServerInfo.Packets.Handlers
{
    class KeybindHandler
    {
        public static void HandleKeyMapChange(MapleClient c, PacketReader pr)
        {
            //ServerConsole.Info("Key packet:" + pr.ToString());
            pr.Skip(4);
            int count = pr.ReadInt();
            //StringBuilder sb = new StringBuilder("Default keys = ");
            for (int i = 0; i < count; i++)
            {
                uint key = (uint)pr.ReadInt();
                byte type = pr.ReadByte();
                int action = pr.ReadInt();
                
                if (type == 1 && (action >= 1000))
                {
                    if (!c.Account.Character.HasSkill(action))
                    {
                        WzCharacterSkill skillInfo = DataBuffer.GetCharacterSkillById(action);
                        if (skillInfo == null || !skillInfo.HasFixedLevel)
                            continue;                                             
                    }                        
                }
                c.Account.Character.ChangeKeybind(key, type, action);
                /*sb.Append("\n{{");
                sb.Append(key);
                sb.Append(", ");
                sb.Append("new Pair<byte, int>(");
                sb.Append(type);
                sb.Append(", ");
                sb.Append(action);
                sb.Append(")}},");*/
            }
            //ServerConsole.Info(sb.ToString());
        }

        public static void HandleQuickSlotKeysChange(MapleClient c, PacketReader pr)
        {
            if (pr.Available != 112)
                return;
            int[] currentMap = c.Account.Character.QuickSlotKeys;
            int[] newMap = new int[28];
            bool different = false;
            for (int i = 0; i < 28; i++)
            {
                int key = pr.ReadInt();
                newMap[i] = key;
                if (currentMap[i] != key)
                    different = true;
            }
            if (different)
                c.Account.Character.SetQuickSlotKeys(newMap);
        }
    }
}
