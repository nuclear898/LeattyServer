using LeattyServer.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LeattyServer.ServerInfo.Player.ResourceSystems
{
    public class ResourceSystem
    {
        public ResourceSystemType Type { get; set; }

        public ResourceSystem(ResourceSystemType type)
        {
            Type = type;
        }

        public virtual void SaveToDatabase(int chrId, bool detach = false)
        {
        }

        public static ResourceSystem CheckAndSaveResourceSystem(ResourceSystem rs, ResourceSystemType newType, int chrId)
        {
            if (rs != null && rs.Type != newType)
            {
                rs.SaveToDatabase(chrId); //This actually doesn't always do something
            }
            if (rs == null || rs.Type != newType)
            {
                switch (newType)
                {
                    case ResourceSystemType.Hunter:
                        return new QuiverCartridgeSystem();
                    case ResourceSystemType.Bandit:
                        return new BodyCountSystem();
                    case ResourceSystemType.Aran:
                        return new AranSystem();
                    case ResourceSystemType.Phantom:
                        PhantomSystem phantomResource = new PhantomSystem();
                        using (LeattyContext DBContext = new LeattyContext())
                        {
                            phantomResource.PopulateSkills(DBContext.StolenSkills.Where(x => x.CharacterId == chrId).ToList());
                        }
                        return phantomResource;
                    case ResourceSystemType.Luminous:
                        return new LuminousSystem();
                    default:
                        return null;
                }                
            }
            return rs;
        }
    }

    public enum ResourceSystemType
    {
        Hunter,
        Bandit,
        Aran,
        Phantom,
        Luminous
    }
}
