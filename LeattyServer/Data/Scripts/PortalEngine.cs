using System;
using LeattyServer.Data.WZ;
using LeattyServer.Helpers;
using LeattyServer.Scripting;
using LeattyServer.ServerInfo.Player;

namespace LeattyServer.Data.Scripts
{
    static class PortalEngine
    {
        public static void EnterScriptedPortal(WzMap.Portal portal, MapleCharacter character)
        {
            if (!string.IsNullOrEmpty(portal.Script))
            {
                Type portalScriptType;
                if (DataBuffer.PortalScripts.TryGetValue(portal.Script, out portalScriptType) && portalScriptType != null)
                {
                    PortalScript scriptInstance = Activator.CreateInstance(portalScriptType) as PortalScript;
                    if (scriptInstance == null)
                    {
                        string error = string.Format("Error loading {0} {1}", "PortalScript", portal.Script);
                        ServerConsole.Error(error);
                        FileLogging.Log("Portal scripts", error);
                        return;
                    }
                    scriptInstance.Character = new ScriptCharacter(character, portal.Script);
                    try
                    {
                        scriptInstance.Execute();
                    }
                    catch (Exception e)
                    {
                        string error = string.Format("Script: {0} error: {1}", portal.Script, e);
                        FileLogging.Log("Portal scripts", error);
                        ServerConsole.Debug("Portal script execution error: " + error);
                        character.EnableActions();
                    }
                }
                else
                {
                    character.SendBlueMessage(string.Format("This portal is not coded yet (mapId: {0}, portalId: {1}, script name: {2})", character.MapId, portal.Id, portal.Script));
                    character.EnableActions();
                }
            }
            else
                character.EnableActions();
        }
    }
}
