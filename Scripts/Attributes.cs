namespace LeattyServer.Scripting
{
    [System.AttributeUsage(System.AttributeTargets.Class)]
    public class Author : System.Attribute
    {
        public string AuthorName { get; }
       
        public Author(string name)
        {
            AuthorName = name;
        }
    }

    [System.AttributeUsage(System.AttributeTargets.Class)]
    public class NPC : System.Attribute
    {
        public string Name { get; }
        private string Description { get; }
        public NPC(string name, string description)
        {
            Name = name;
            Description = description;
        }
    }

    [System.AttributeUsage(System.AttributeTargets.Class)]
    public class Portal : System.Attribute
    {
        public string Name { get; }
        private string Description { get; }
        public Portal(string name, string description)
        {
            Name = name;
            Description = description;
        }
    }

    [System.AttributeUsage(System.AttributeTargets.Class)]
    public class Event : System.Attribute
    {
        public string Name { get; }
        private string Description { get; }
        public Event(string name, string description)
        {
            Name = name;
            Description = description;
        }
    }
}
