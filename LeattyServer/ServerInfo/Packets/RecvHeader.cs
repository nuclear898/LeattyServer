namespace LeattyServer.ServerInfo.Packets
{
    public enum RecvHeader : ushort
    {
        ClientLoaded = 0x1A,
        ClientNetworkInfo = 0x1B,
        WorldSelect = 0x1E,
        ShowServerList = 0x23,
        ReShowServerList = 0x24,
        EnterMap = 0x28,
        CheckCharacterName = 0x29,
        DeleteCharacter = 0x2D,
        CrashReport = 0x31,
        HackShieldHeartBeat = 0x32,
        BlackCipher = 0x33,
        SetAccountPic = 0x34,

        EnteredLoginScreen = 0x3A,
        HandShake = 0x40,
        AccountLogin = 0x41,
        ChooseChannel = 0x44,

        CreateCharacter = 0x47,
        Pong = 0x48,
        ChooseCharacterWithPic = 0x49,

        ErrorCode = 0x4B,

        EnterMapPortal = 0x53,
        ChangeChannel = 0x54,
        EnterCashShop = 0x58,
        MoveCharacter = 0x62,

        MeleeAttack = 0x67,
        RangedAttack = MeleeAttack + 1,
        MagicAttack = MeleeAttack + 2,
        PassiveAttack = MeleeAttack + 3,
        EnvironmentAttack = MeleeAttack + 4,

        CharacterReceiveDamage = 0x6D,

        PlayerChat = 0x6F,

        FacialExpression = 0x71,

        NpcChat = 0x84,
        NpcChatMore = 0x86,
        NpcShopAction = 0x87,

        SlotMerge = 0x97,
        ItemSort = 0x98,
        MoveItem = 0x99,
        UseConsumable = 0x9E,

        UseReturnScroll = 0xA4,
        UseCashItem = 0xAA,

        UseEquipScroll = 0xC7,
        UseSpecialEquipScroll = 0xC8,
        UseEquipEnhancementScroll = 0xC9,
        UsePotentialScroll = 0xCD,
        UseBonusPotentialScroll = 0xCE,

        DistributeAp = 0xDC,
        AutoAssignAp = 0xDD,
        //0xDE new v158
        RegenerateHpMp = 0xDF,
        DistributeSp = 0xE2,
        UseSkill = 0xE3,
        CancelBuff = 0xE4,

        UseCube = 0xD3,
        UseMagnifyGlass = 0xD7,
        DropMeso = 0xD9,
        CharacterInfoRequest = 0xE9,

        FinalPactEnd = 0xF1, //not sure
        EnterMapPortalSpecial = 0xF2,

        QuestAction = 0xFB,

        SetSkillMacro = 0x103,

        RequestHyperskillInfo = 0x11A,
        CraftUnk = 0x10C, //not updated yet

        SpecialChat = 0x137,
        FindPlayer = 0x139,
        MessengerOperation = 0x13B,
        Trade = 0x13C,
        PartyOperation = 0x13D,
        PartyResponse = 0x13E,

        GuildAction = 0x133, //not updated yet

        EnterDoor = 0x140, //not updated yet

        BuddyOperation = 0x14C,

        ChangeKeybind = 0x154,
        SkillMacro = 160,

        GainAranCombo = 0x15E, //not updated yet
        DecayAranCombo = 0x15F, //not updated yet
        BlackBlessing = 0x161, //was 160, not updated yet

        CraftDone = 0x163, //not updated yet
        CraftEffect = 0x164, //not updated yet
        CraftMake = 0x165, //not updated yet

        ChooseStolenSkill = 0x16C, //not updated yet
        StealSkill = 0x16D, //not updated yet
        SkillSwipe = 0x16E, //not updated yet

        MoveSummon = 0x1CE,
        SummonAttack = 0x1CF,
        DamageSummon = 0x1D0,
        SummonUseSkill = 0x1D1,
        RemoveSummon = 0x1D2,
        PvPSummon = 0x1D4,
        MoveDragon = 0x1D5,

        QuickSlotKeyMap = 0x205,
        ClickDialog = 0x207,
        RequestWeeklyMapleStar = 0x214,

        AttackSpam = 0x1E8, //not updated yet

        MoveMob = 0x276,
        AutoAggroMob = 0x277,

        NpcAnimation = 0x28D,

        LootMapItem = 0x292,

        RequestRecommendedPartyMembers = 0x2B0,


        UseGoldenHammer = 0x335,

        ReactorAction = 0x22E,  //not updated yet

        ProffesionReactorAction = 0x251,   //not updated yet
        ProffesionReactorDestroy = 0x253,  //not updated yet

        CashshopSelect = 0x2AD //not updated yet
    }
}
