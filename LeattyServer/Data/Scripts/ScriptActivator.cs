using LeattyServer.Helpers;
using LeattyServer.ServerInfo.Player;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LeattyServer.Scripting;

namespace LeattyServer.Data.Scripts
{
    public class ScriptActivator
    {
        private static ScriptInterface ScriptDataProvider = new ScriptInterface();

        /// <summary>
        /// Creates a new instance of parameter scriptType as a Script object
        /// </summary>
        /// <param name="scriptType">The type of the script to be instantialized</param>
        /// <param name="scriptName">The name of the script</param>
        /// <param name="chr">The character that triggered the creation of the script</param>
        /// <returns>The created Script instance</returns>
        public static Script CreateScriptInstance(Type scriptType, string scriptName, MapleCharacter chr)
        {
            var instance = Activator.CreateInstance(scriptType) as Script;

            if (instance == null)
            {
                ServerConsole.Error(string.Format("Type {0} cannot be cast to 'Script'", scriptType));
                return null;
            }
            if (instance is CharacterScript)
            {
                CharacterScript cInstance = (CharacterScript)instance;
                cInstance.Character = new ScriptCharacter(chr, scriptName);
            }
            instance.DataProvider = ScriptDataProvider;
            return instance;            
        }
    }
}
