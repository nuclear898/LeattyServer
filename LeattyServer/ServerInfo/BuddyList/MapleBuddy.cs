using System;
using System.Collections.Generic;
using System.Data.Entity.Migrations.Infrastructure;
using System.Linq;
using System.Security.Authentication.ExtendedProtection;
using System.Text;
using System.Threading.Tasks;

namespace LeattyServer.ServerInfo.BuddyList
{
    public class MapleBuddy
    {
        public int CharacterId { get; set; }
        public int AccountId { get; set; }
        public string NickName { get; set; }
        public string Group { get; set; }
        public string Memo { get; set; }
        public bool IsRequest { get; set; }
        public int Channel { get; set; }
        public string Name { get; set; }
        public bool AccountBuddy { get; set; }

        public MapleBuddy(int characterId, int accountId, string name, string group, bool isRequest, string memo = "")
        {
            CharacterId = characterId;
            AccountId = accountId;
            NickName = name;
            Group = group;
            Memo = memo;
            IsRequest = isRequest;
            Channel = -1;
            AccountBuddy = accountId > 0;
            Name = string.Empty;
        }
    }
}
