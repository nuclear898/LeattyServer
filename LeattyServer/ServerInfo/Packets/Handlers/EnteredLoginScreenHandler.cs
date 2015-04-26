using LeattyServer.Constants;
using LeattyServer.ServerInfo.Player;

namespace LeattyServer.ServerInfo.Packets.Handlers
{
    public class EnteredLoginScreenHandler
    {
        public static void Handle(MapleClient c)
        {       
            if (ServerConstants.LocalHost) //Kek
            {                                
                MapleAccount acc = MapleAccount.GetAccountFromDatabase("Nuclear");
                c.Account = acc;
                c.SendPacket(LoginAccountHandler.LoginAccountSuccess(acc));
            }
        }      
    }
}
