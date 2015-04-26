using System;
using LeattyServer.Data;
using LeattyServer.Data.Scripts;
using LeattyServer.Data.WZ;
using LeattyServer.Helpers;
using LeattyServer.ServerInfo.Player;

namespace LeattyServer.ServerInfo.Packets.Handlers
{
    public class NpcChatMoreHandler
    {
        public static void Handle(MapleClient c, PacketReader pr)
        {
            MapleCharacter chr = c.Account.Character;
            try
            {
                if (c.NpcEngine == null || c.NpcEngine.ScriptInstance == null)
                {
                    if (chr.ActionState == ActionState.NpcTalking)
                    {
                        chr.EnableActions();
                    }
                    return;
                }
                int objectId = c.NpcEngine.ScriptInstance.ObjectId;
                if (objectId != -1) //objectId == -1 when npc conversation was started by the server, so players can talk to NPCs that are not on their map (with commands etc)
                {
                    WzMap.Npc npc = chr.Map.GetNpc(objectId);
                    if (npc == null) 
                    {
                        c.NpcEngine.ScriptInstance = null;
                        return;
                    }
                }                
                byte type = pr.ReadByte();
                byte next = pr.ReadByte();
                if (next == 0xFF) //End Chat
                {
                    c.NpcEngine.Dispose();                    
                    return;
                }
                if (next == 0 && (type != 2))
                {
                    c.NpcEngine.ScriptInstance.State -= 1;
                }
                else
                {
                    c.NpcEngine.ScriptInstance.State += 1;
                }
                bool execute = false;                
                switch (type)
                {
                    case 0: //SendOk, SendPrev, SendNext
                        execute = true;
                        break;
                    case 2: //SendYesNo
                        execute = true;
                        c.NpcEngine.ScriptInstance.Selection = next == 0 ? 0 : 1;
                        break;
                    case 3://SendAskText
                        if (next == 0)
                        {
                            execute = false;
                        }
                        else
                        {
                            execute = true;
                            string text = pr.ReadMapleString();
                            c.NpcEngine.ScriptInstance.InText = text;
                        }
                        break;
                    case 4: //SendGetNumber
                        //Player.GetNpcStatus().Num = pr.ReadInt();
                        execute = true;
                        break;
                    case 5: //SendSimple
                        if (next == 0)
                        {
                            execute = false;
                        }
                        else
                        {
                            if (pr.Available >= 4)
                                c.NpcEngine.ScriptInstance.Selection = pr.ReadInt(); //This can be int as well, decided by the client
                            else if (pr.Available >= 1)
                                c.NpcEngine.ScriptInstance.Selection = pr.ReadSByte();
                            execute = true;
                        }
                        break;
                    case 23: //Choose Job
                        int choice = pr.ReadInt();

                        break;
                    default:
                        string msg = "Unknown Npc chat type: " + pr.ToString();
                        ServerConsole.Error(msg);
                        FileLogging.Log("NPC chat type", msg);
                        c.NpcEngine.ScriptInstance = null;
                        break;
                }
                if (execute)
                {
                    NpcEngine engine = c.NpcEngine;
                    if (engine == null)
                    {
                        if (c.Account.Character.ActionState == ActionState.NpcTalking)
                        {
                            c.Account.Character.EnableActions();
                        }
                        return;
                    }
                    engine.ExecuteScriptForNpc();
                }
                else
                {
                    c.NpcEngine.ScriptInstance = null;
                    if (chr.ActionState == ActionState.NpcTalking)
                    {
                        chr.EnableActions();
                    }
                }
            }
            catch (Exception ex)
            {
                ServerConsole.Error("NpcChatMoreHandler.Handle : " + ex.ToString());
                if (c.NpcEngine != null)
                {
                    c.NpcEngine.ScriptInstance = null;
                }
                if (chr.ActionState == ActionState.NpcTalking)
                {
                    chr.EnableActions();
                }
            }
        }
    }
}
