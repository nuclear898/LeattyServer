namespace LeattyServer.Migrations
{
    using System;
    using System.Data.Entity.Migrations;
    
    public partial class Initial : DbMigration
    {
        public override void Up()
        {
            CreateTable(
                "dbo.Accounts",
                c => new
                    {
                        Id = c.Int(nullable: false, identity: true),
                        MaplePoints = c.Int(nullable: false),
                        NXPrepaid = c.Int(nullable: false),
                        NXCredit = c.Int(nullable: false),
                        AccountType = c.Byte(nullable: false),
                        Name = c.String(maxLength: 13),
                        Password = c.String(maxLength: 128),
                        Key = c.String(maxLength: 32),
                        Pic = c.String(maxLength: 134),
                        PicKey = c.String(maxLength: 32),
                    })
                .PrimaryKey(t => t.Id);
            
            CreateTable(
                "dbo.CashshopCats",
                c => new
                    {
                        Id = c.Int(nullable: false, identity: true),
                        ParentId = c.Int(nullable: false),
                        CsId = c.Int(nullable: false),
                        Type = c.Byte(nullable: false),
                        Name = c.String(maxLength: 100),
                    })
                .PrimaryKey(t => t.Id);
            
            CreateTable(
                "dbo.CashshopFavorites",
                c => new
                    {
                        Id = c.Long(nullable: false, identity: true),
                        AccountId = c.Int(nullable: false),
                        ItemId = c.Int(nullable: false),
                    })
                .PrimaryKey(t => t.Id);
            
            CreateTable(
                "dbo.CashshopInventory",
                c => new
                    {
                        Id = c.Long(nullable: false, identity: true),
                        AccountId = c.Long(nullable: false),
                    })
                .PrimaryKey(t => t.Id);
            
            CreateTable(
                "dbo.CashshopItems",
                c => new
                    {
                        Id = c.Int(nullable: false, identity: true),
                        CatId = c.Int(nullable: false),
                        ItemId = c.Int(nullable: false),
                        Price = c.Int(nullable: false),
                        NewPrice = c.Int(nullable: false),
                        Amount = c.Int(nullable: false),
                        TimesBought = c.Int(nullable: false),
                        Order = c.Int(nullable: false),
                        Likes = c.Int(nullable: false),
                        MinLevel = c.Byte(nullable: false),
                        Special = c.Byte(nullable: false),
                        Featured = c.Byte(nullable: false),
                        DateFrom = c.DateTime(nullable: false),
                        DateTo = c.DateTime(nullable: false),
                        Image = c.String(maxLength: 200),
                    })
                .PrimaryKey(t => t.Id);
            
            CreateTable(
                "dbo.CashshopLikes",
                c => new
                    {
                        Id = c.Int(nullable: false, identity: true),
                        CharacterId = c.Int(nullable: false),
                        CsId = c.Int(nullable: false),
                    })
                .PrimaryKey(t => t.Id);
            
            CreateTable(
                "dbo.Characters",
                c => new
                    {
                        Id = c.Int(nullable: false, identity: true),
                        Exp = c.Long(nullable: false),
                        Mesos = c.Long(nullable: false),
                        AccountId = c.Int(nullable: false),
                        MapId = c.Int(nullable: false),
                        GuildId = c.Int(nullable: false),
                        HP = c.Int(nullable: false),
                        MP = c.Int(nullable: false),
                        MaxHP = c.Int(nullable: false),
                        MaxMP = c.Int(nullable: false),
                        Fame = c.Int(nullable: false),
                        Hair = c.Int(nullable: false),
                        Face = c.Int(nullable: false),
                        FaceMark = c.Int(nullable: false),
                        TamerEars = c.Int(nullable: false),
                        TamerTail = c.Int(nullable: false),
                        GuildContribution = c.Int(nullable: false),
                        Charisma = c.Int(nullable: false),
                        Insight = c.Int(nullable: false),
                        Will = c.Int(nullable: false),
                        Craft = c.Int(nullable: false),
                        Sense = c.Int(nullable: false),
                        Charm = c.Int(nullable: false),
                        Job = c.Short(nullable: false),
                        SubJob = c.Short(nullable: false),
                        Str = c.Short(nullable: false),
                        Dex = c.Short(nullable: false),
                        Luk = c.Short(nullable: false),
                        Int = c.Short(nullable: false),
                        AP = c.Short(nullable: false),
                        BuddyCapacity = c.Short(nullable: false),
                        Level = c.Byte(nullable: false),
                        Gender = c.Byte(nullable: false),
                        Skin = c.Byte(nullable: false),
                        SpawnPoint = c.Byte(nullable: false),
                        GuildRank = c.Byte(nullable: false),
                        AllianceRank = c.Byte(nullable: false),
                        Fatigue = c.Byte(nullable: false),
                        Name = c.String(maxLength: 13),
                        SP = c.String(maxLength: 50),
                        BannedTill = c.DateTime(),
                    })
                .PrimaryKey(t => t.Id);
            
            CreateTable(
                "dbo.Guilds",
                c => new
                    {
                        Id = c.Int(nullable: false, identity: true),
                        Leader = c.Int(nullable: false),
                        GP = c.Int(nullable: false),
                        AllianceId = c.Int(nullable: false),
                        Signature = c.Int(nullable: false),
                        Name = c.String(maxLength: 45),
                        Notice = c.String(maxLength: 101),
                        Rank1Title = c.String(maxLength: 45),
                        Rank2Title = c.String(maxLength: 45),
                        Rank3Title = c.String(maxLength: 45),
                        Rank4Title = c.String(maxLength: 45),
                        Rank5Title = c.String(maxLength: 45),
                    })
                .PrimaryKey(t => t.Id);
            
            CreateTable(
                "dbo.GuildSkills",
                c => new
                    {
                        Id = c.Int(nullable: false, identity: true),
                        Timestamp = c.Long(nullable: false),
                        GuildId = c.Int(nullable: false),
                        SkillId = c.Int(nullable: false),
                        Level = c.Short(nullable: false),
                        Purchaser = c.String(maxLength: 13),
                    })
                .PrimaryKey(t => t.Id);
            
            CreateTable(
                "dbo.InventoryEquips",
                c => new
                    {
                        Id = c.Long(nullable: false, identity: true),
                        InventoryItemId = c.Long(nullable: false),
                        Durability = c.Int(nullable: false),
                        Str = c.Short(nullable: false),
                        Dex = c.Short(nullable: false),
                        Luk = c.Short(nullable: false),
                        Int = c.Short(nullable: false),
                        IncHP = c.Short(nullable: false),
                        IncMP = c.Short(nullable: false),
                        WAtk = c.Short(nullable: false),
                        MAtk = c.Short(nullable: false),
                        WDef = c.Short(nullable: false),
                        MDef = c.Short(nullable: false),
                        Accuracy = c.Short(nullable: false),
                        Avoid = c.Short(nullable: false),
                        Speed = c.Short(nullable: false),
                        Jump = c.Short(nullable: false),
                        Potential1 = c.Short(nullable: false),
                        Potential2 = c.Short(nullable: false),
                        Potential3 = c.Short(nullable: false),
                        Potential4 = c.Short(nullable: false),
                        Potential5 = c.Short(nullable: false),
                        BonusPotential = c.Short(nullable: false),
                        Socket1 = c.Short(nullable: false),
                        Socket2 = c.Short(nullable: false),
                        Socket3 = c.Short(nullable: false),
                        UpgradeSlots = c.Byte(nullable: false),
                        ScrolledLevel = c.Byte(nullable: false),
                        Enhancements = c.Byte(nullable: false),
                        CustomLevel = c.Byte(nullable: false),
                    })
                .PrimaryKey(t => t.Id);
            
            CreateTable(
                "dbo.InventoryItems",
                c => new
                    {
                        Id = c.Long(nullable: false, identity: true),
                        ItemId = c.Int(nullable: false),
                        CharacterId = c.Int(nullable: false),
                        InventoryType = c.Int(nullable: false),
                        Position = c.Short(nullable: false),
                        Quantity = c.Short(nullable: false),
                        CashshopInventory_Id = c.Long(),
                    })
                .PrimaryKey(t => t.Id)
                .ForeignKey("dbo.CashshopInventory", t => t.CashshopInventory_Id)
                .Index(t => t.CashshopInventory_Id);
            
            CreateTable(
                "dbo.InventorySlots",
                c => new
                    {
                        Id = c.Int(nullable: false, identity: true),
                        CharacterId = c.Int(nullable: false),
                        EquipSlots = c.Byte(nullable: false),
                        UseSlots = c.Byte(nullable: false),
                        SetupSlots = c.Byte(nullable: false),
                        EtcSlots = c.Byte(nullable: false),
                        CashSlots = c.Byte(nullable: false),
                    })
                .PrimaryKey(t => t.Id);
            
            CreateTable(
                "dbo.KeyMaps",
                c => new
                    {
                        Id = c.Int(nullable: false, identity: true),
                        CharacterId = c.Int(nullable: false),
                        Action = c.Int(nullable: false),
                        Key = c.Byte(nullable: false),
                        Type = c.Byte(nullable: false),
                    })
                .PrimaryKey(t => t.Id);
            
            CreateTable(
                "dbo.QuestCustomDatas",
                c => new
                    {
                        Id = c.Int(nullable: false, identity: true),
                        CharacterId = c.Int(nullable: false),
                        Data = c.String(),
                    })
                .PrimaryKey(t => t.Id);
            
            CreateTable(
                "dbo.QuestStatus",
                c => new
                    {
                        Id = c.Int(nullable: false, identity: true),
                        CharacterId = c.Int(nullable: false),
                        Quest = c.Int(nullable: false),
                        Status = c.Byte(nullable: false),
                        CustomData = c.String(maxLength: 255),
                    })
                .PrimaryKey(t => t.Id);
            
            CreateTable(
                "dbo.QuestStatusMobs",
                c => new
                    {
                        Id = c.Int(nullable: false, identity: true),
                        QuestStatusId = c.Int(nullable: false),
                        Mob = c.Int(nullable: false),
                        Count = c.Int(nullable: false),
                    })
                .PrimaryKey(t => t.Id);
            
            CreateTable(
                "dbo.SkillCooldowns",
                c => new
                    {
                        Id = c.Int(nullable: false, identity: true),
                        StartTime = c.Long(nullable: false),
                        Length = c.Int(nullable: false),
                        CharacterId = c.Int(nullable: false),
                        SkillId = c.Int(nullable: false),
                    })
                .PrimaryKey(t => t.Id);
            
            CreateTable(
                "dbo.Skills",
                c => new
                    {
                        Id = c.Int(nullable: false, identity: true),
                        Expiration = c.Long(nullable: false),
                        CharacterId = c.Int(nullable: false),
                        SkillId = c.Int(nullable: false),
                        Level = c.Byte(nullable: false),
                        MasterLevel = c.Byte(nullable: false),
                        SkillExp = c.Short(nullable: false),
                    })
                .PrimaryKey(t => t.Id);
            
            CreateTable(
                "dbo.StolenSkills",
                c => new
                    {
                        Id = c.Int(nullable: false, identity: true),
                        CharacterId = c.Int(nullable: false),
                        SkillId = c.Int(nullable: false),
                        Chosen = c.Boolean(nullable: false),
                        Index = c.Byte(nullable: false),
                    })
                .PrimaryKey(t => t.Id);
            
        }
        
        public override void Down()
        {
            DropForeignKey("dbo.InventoryItems", "CashshopInventory_Id", "dbo.CashshopInventory");
            DropIndex("dbo.InventoryItems", new[] { "CashshopInventory_Id" });
            DropTable("dbo.StolenSkills");
            DropTable("dbo.Skills");
            DropTable("dbo.SkillCooldowns");
            DropTable("dbo.QuestStatusMobs");
            DropTable("dbo.QuestStatus");
            DropTable("dbo.QuestCustomDatas");
            DropTable("dbo.KeyMaps");
            DropTable("dbo.InventorySlots");
            DropTable("dbo.InventoryItems");
            DropTable("dbo.InventoryEquips");
            DropTable("dbo.GuildSkills");
            DropTable("dbo.Guilds");
            DropTable("dbo.Characters");
            DropTable("dbo.CashshopLikes");
            DropTable("dbo.CashshopItems");
            DropTable("dbo.CashshopInventory");
            DropTable("dbo.CashshopFavorites");
            DropTable("dbo.CashshopCats");
            DropTable("dbo.Accounts");
        }
    }
}
