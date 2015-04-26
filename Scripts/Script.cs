namespace LeattyServer.Scripting
{
    public abstract class Script
    {
        public AbstractScriptInterface DataProvider { get; set; }
        
        public abstract void Execute();
    }
}