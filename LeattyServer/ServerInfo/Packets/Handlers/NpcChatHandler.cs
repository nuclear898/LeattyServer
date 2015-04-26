using System;
using LeattyServer.Data;
using LeattyServer.Data.Scripts;
using LeattyServer.Data.WZ;
using LeattyServer.Helpers;
using LeattyServer.ServerInfo.Player;

namespace LeattyServer.ServerInfo.Packets.Handlers
{
    public class NpcChatHandler
    {
        public static void Handle(MapleClient c, PacketReader pr)
        {
            MapleCharacter chr = c.Account.Character;
            if (!chr.DisableActions(ActionState.NpcTalking)) return;
            try
            {
                int objectId = pr.ReadInt();
                WzMap.Npc npc = chr.Map.GetNpc(objectId);
                if (npc == null)
                {
                    ServerConsole.Warning("NPC is not on player {0}'s map: {1}", chr.Name, chr.Map.MapId);
                    chr.EnableActions();
                    return;
                }
                NpcEngine.OpenNpc(npc.Id, objectId, c);
            }
            catch (Exception ex)
            {
                ServerConsole.Error("NpcChatHandler.Handle : " + ex);
                chr.EnableActions();
            }
        }
    }
}
