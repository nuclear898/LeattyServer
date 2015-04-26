using System;
using LeattyServer.Helpers;
using LeattyServer.ServerInfo.Player;

namespace LeattyServer.ServerInfo.Packets.Handlers
{
    class ErrorCodeHandler
    {
        public static void Handle(MapleClient c, PacketReader pr)
        {
            if (pr.Available > 8)
            {
                short type = pr.ReadShort();
                string typeString = "Unknown";
                if (type == 0x01)
                    typeString = "SendBackupPacket";          
                else if (type == 0x02)
                    typeString = "Crash Report";            
                else if (type == 0x03)
                    typeString = "Exception";

                int errorType = pr.ReadInt();
                //if (errorType == 0) //Usually some bounceback to login
                    //return;

                short dataLength = pr.ReadShort();
                pr.Skip(4);
                int header = pr.ReadShort();

                string headerName = Enum.GetName(typeof(SendHeader), header) + String.Format(" : {0}", header.ToString("X")) ;
                string accountName = c.Account.Name;
                string playerName = "N/A (not logged in yet)";               
                if (c.Account.Character != null) 
                {
                    playerName = c.Account.Character.Name;
                    //TODO: map id
                }
                string remainingBytes = pr.ToString(true);
                string errorString = String.Format("Error Type: {0}\r\nData Length: {1}\r\nError for player:{2} - account: {3}\r\nHeader: {4}\r\nData: {5}", typeString, dataLength, playerName, accountName, headerName, remainingBytes);

                FileLogging.Log("ErrorCodes.txt", errorString);
                ServerConsole.Warning("Error 38 caused by: " + headerName);
            }       
        }    
    }
}
