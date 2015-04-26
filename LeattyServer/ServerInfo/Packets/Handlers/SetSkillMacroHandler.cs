using System.Dynamic;
using LeattyServer.ServerInfo.Player;

namespace LeattyServer.ServerInfo.Packets.Handlers
{
    public static class SetSkillMacroHandler
    {
        public static void Handle(MapleClient c, PacketReader pr)
        {
            int macroCount = pr.ReadByte();
            if (macroCount < 0) return;
            if (macroCount > 5) macroCount = 5; 
            for (int i = 0; i < macroCount; i++)
            {
                string name = pr.ReadMapleString();
                if (name.Length > 12)
                    name = name.Substring(0, 12);
                bool shoutName = !pr.ReadBool();

                int skill1 = pr.ReadInt();
                int skill2 = pr.ReadInt();
                int skill3 = pr.ReadInt();
                SkillMacro currentMacro = c.Account.Character.SkillMacros[i];
                bool empty = name.Length == 0 && skill1 == 0 && skill2 == 0 && skill3 == 0;
                if (currentMacro != null)
                {
                    if (empty)
                        c.Account.Character.SkillMacros[i] = null;
                    else
                        currentMacro.SetSkills(name, shoutName, skill1, skill2, skill3);
                }
                else if (!empty)
                {
                    currentMacro = new SkillMacro(name, shoutName, skill1, skill2, skill3);
                    c.Account.Character.SkillMacros[i] = currentMacro;
                }
            }
        }
    }
}



/*
[03] 
[05 00 68 61 79 74 6F] [01] [28 7C 7D 01] [00 00 00 00] [00 00 00 00] 
[00 00]                [00] [00 00 00 00] [00 00 00 00] [00 00 00 00] 
[00 00]                [00] [28 7C 7D 01] [00 00 00 00] [00 00 00 00]
*/
