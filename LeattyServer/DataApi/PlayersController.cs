using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using System.Web.Http;

namespace LeattyServer.DataApi
{
    /// <summary>
    /// This should only be running on localhost, still I felt like caching it
    /// </summary>
    [System.Web.Mvc.OutputCache(Duration = 60)]
    public class PlayersController : ApiController
    {
        [HttpGet]
        public PlayersOnline Online()
        {
            return new PlayersOnline() {
                Players = Program.Clients.Where(client => client.Value.Account != null && client.Value.Account.Character != null).Count()
            };
        }

        [HttpGet]
        public IsPlayerOnline IsOnline(string param) 
        {
            param = Uri.UnescapeDataString(param);
            return new IsPlayerOnline()
            {
                Online = Program.GetCharacterByName(param) != null
            };
        }
    }
}
