namespace LeattyServer.Scripting
{
    /// <summary>
    /// Provides data from the server to a script
    /// </summary>
    public abstract class AbstractScriptInterface
    {
        public abstract byte GetEquipRequiredLevel(int equipItemId);
        public abstract long GetEquipRevealCost(int equipItemId);
        public abstract string GetPotentialName(int potentialId, byte equipRequiredLevel);       
    }
}