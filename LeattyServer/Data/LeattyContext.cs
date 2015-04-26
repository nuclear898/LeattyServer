using LeattyServer;
using LeattyServer.Constants;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LeattyServer.DB.Models;

namespace LeattyServer.Data
{
    public class LeattyContext : DbContext
    {
        public LeattyContext()
            : base(ServerConstants.DatabaseString)
        {
            this.Configuration.LazyLoadingEnabled = true;
            this.Configuration.ProxyCreationEnabled = false;
        }

        public DbSet<Account> Accounts { get; set; }
        public DbSet<Buddy> Buddies { get; set; }
        public DbSet<CashshopFavorite> CashshopFavorites { get; set; }
        public DbSet<CashshopLike> CashshopLikes { get; set; }
        public DbSet<Character> Characters { get; set; }
        public DbSet<Guild> Guilds { get; set; }
        public DbSet<GuildSkill> GuildSkills { get; set; }
        public DbSet<InventoryItem> InventoryItems { get; set; }
        public DbSet<InventoryEquip> InventoryEquips { get; set; }
        public DbSet<InventorySlot> InventorySlots { get; set; }
        public DbSet<KeyMap> KeyMaps { get; set; }
        public DbSet<QuickSlotKeyMap> QuickSlotKeyMaps { get; set; }
        public DbSet<DbSkillMacro> SkillMacros { get; set; }
        public DbSet<QuestStatus> QuestStatus { get; set; }
        public DbSet<QuestMobStatus> QuestStatusMobs { get; set; }
        public DbSet<QuestCustomData> QuestCustomData { get; set; }
        public DbSet<Skill> Skills { get; set; }
        public DbSet<SkillCooldown> SkillCooldowns { get; set; }
        public DbSet<StolenSkill> StolenSkills { get; set; }
    }
}
