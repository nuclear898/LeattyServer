using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LeattyServer.ServerInfo.Player
{
    public class Invite
    {
        public int SenderId;
        public InviteType Type;
        public Invite(int fromID, InviteType type)
        {
            Type = type;
            SenderId = fromID;
        }
    }
    public enum InviteType
    {
        Guild,
        Party,
        Buddy,
        Trade,
    }
}
