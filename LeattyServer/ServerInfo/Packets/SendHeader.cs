namespace LeattyServer.ServerInfo.Packets
{
    public enum SendHeader : ushort
    {
        AccountLoginResponse = 0x00,
        LoginSuccess = 0x02,

        ServerStatus = 0x04, 
        ShowWorlds = 0x09, 
        ShowCharacters = 0x0A,
        ChannelIp = 0x0B,
        CharacterNameResponse = 0x0C,
        AddCharacter = 0x0D, 
        DeleteCharacter = 0x0E,

        ChangeChannelResponse = 0x12, 
        Ping = 0x13, 
        CSUnk1 = 0x14, 
        LoginScreenUnk = 0x15, 
        HackShieldHeartBeat = 0x16, 
        BlackCipher = 0x17, 

        HighlightedServer = 0x1C,
        RecommendedServer = 0x1D,
        SelectedServer = 0x1E, 

        WorldId = 0x20, 
        CreateCharacterOptions = 0x25,
        PICResponse = 0x26,
        InventoryOperation = 0x27, 

        UpdateStats = 0x29, 
        GiveBuff = 0x2A,
        RemoveBuff = 0x2B,
        UpdateSkills = 0x2E, 

        UpdateStolenSkill = 0x2E, //not updated yet
        ShowStealSkills = 0x2E, //not updated yet

        ShowStatusInfo = 0x37,
        QuestCompleteNotice = 0x3E, //not updated yet

        SlotMergeComplete = 0x4F,
        ItemSortComplete = 0x50,

        CharacterInfo = 0x59, 
        PartyInfo = 0x5A,
        RecommendedPartyMembers = 0x5B,

        CSPlayerHighlight = 0x5D, //not updated yet

        GuildData = 0x59, //not updated yet
        AllianceOperation = 0x60, //not updated yet
        SpawnPortal = 0x61, //not updated yet
        //MechPortal = 0x62, //not updated yet
        BuddyList = 0x62,

        ServerNotice = 0x6A,

        UpdateChosenStolenSkill = 0xD2, //not updated yet

        CraftUnk = 0xC3, //not updated yet

        ShowOverHeadNotice = 0xB2,
        HyperskillInfo = 0xC6,

        WeeklyMapleStar = 0x114,
        ShowTitles = 0x117, 

        SkillMacro = 0x160,
        EnterMap = 0x161, 
        EnterCashShop = 0x164, 
        ClientLoaded = 0x166, 
        SpecialChat = 0x16E,

        FindPlayerResponse = 0x170, 

        MesoExplosionTargets = 0x15A,  //not updated yet

        ShowQuickSlotKeys = 0x188, 

        SpawnPlayer = 0x1A9,
        RemovePlayer = 0x1AA,
        PlayerChat = 0x1AB, 

        ShowScrollEffect = 0x1B0,
        ShowMagnifyEffect = 0x1B4,

        CraftMake = 0x195, //not updated yet
        CraftComplete = 0x196, //not updated yet
        UpdateGuildName = 0x1E3, //not updated yet
        ShowSkillEffect = 0x1FA, //not updated yet
        UpdateQuestInfo = 0x203, //not updated yet

        MovePlayer = 0x203,
        MeleeAttack = 0x205,
        RangedAttack = MeleeAttack + 1,
        MagicAttack = RangedAttack + 1,

        PlayerDamaged = 0x20C,
        FacialExpression = 0x20D,
        UpdateCharLook = 0x217,
        ShowForeignSkillEffect = 0x218,
        GiveForeignBuff = 0x219,
        RemoveForeignBuff = 0x21A,
        UpdatePartyMemberHp = 0x21B,
       
        ShowInfo = 0x234,

        ShowAranCombo = 0x214, //not updated yet

        SendText = 0x21B, //not updated yet

        AcceptProffesionAction = 0x230, //not updated yet

        LuminousGauge = 0x23D, //not updated yet

        SystemMessage = 0x256, 

        CharacterInfoFarmImage = 0x28F,

        GiveCooldown = 0x2D0, 

        SpawnSummon = 0x2D2,
        RemoveSummon = 0x2D3,
        MoveSummon = 0x2D4,
        SummonAttack = 0x2D5,
        PvPSummon = 0x2D6,
        SummonSkill = 0x2D7,

        SpawnMonster = 0x2DE,
        RemoveMonster = 0x2DF,
        ControlMonster = 0x2E0,
        MoveMonster = 0x2E2,
        MoveMonsterResponse = 0x2E3,

        GiveMonsterBuff = 0x2E5, 
        RemoveMonsterBuff = 0x2E6,

        MonsterHpBar = 0x2ED, 

        SpawnNpc = 0x312,
        RemoveNpc = 0x313,
        ControlNpc = 0x315, 
        NpcAnimation = 0x316, 

        SpawnMapItem = 0x32A, 
        RemoveMapItem = 0x32C, 

        SpawnMist = 0x2D2, //not updated yet
        RemoveMist = 0x2D3, //not updated yet
        SpawnMysticDoor = 0x2D4, //not updated yet 
        RemoveMysticDoor = 0x2D5, //not updated yet
        SpawnMechDoor = 0x2D6, //not updated yet
        RemoveMechDoor = 0x2D7, //not updated yet

        DamageReactor = 0x2D8, //not updated yet
        MoveReactor = 0x2D9, //not updated yet
        SpawnReactor = 0x2DA, //not updated yet
        DestroyReactor = 0x2DC, //not updated yet
        SpawnExtractor = 0x2DD, //not updated yet
        RemoveExtractor = 0x2DE, //not updated yet

        NpcChat = 0x3EF, 
        NpcShop = 0x3F0, 
        NpcTransaction = 0x3F1, 

        CashShopTokens = 0x38B, //not updated yet
        CashShopInit = 0x38C, //not updated yet
        CashShopInfo = 0x3A2, //not updated yet
        CashShopItemAction = 0x3A3, //not updated yet

        Messenger = 0x410,
        Trade = 0x411, 

        KeybindLayout = 0x463,

        GoldenHammerResult = 0x477
    }
}
