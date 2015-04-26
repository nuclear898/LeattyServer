
namespace LeattyServer.ServerInfo.Player
{
    public class MobBuffStat
    {
        public byte ByteIndex;        

        public MobBuffStat(byte byteIndex)
        {
            ByteIndex = byteIndex;
        }
    }

    public class BuffStat
    {
        public int BitIndex { get; private set; }     
        public bool IsStackingBuff { get; private set; }
        public bool UsesStacksAsValue { get; private set; }

        public BuffStat(int bitIndex, bool usesStacksAsValue = false, bool stackingBuff = false)
        {
            BitIndex = bitIndex;
            IsStackingBuff = stackingBuff;
            UsesStacksAsValue = usesStacksAsValue;
        }       
    }

    public class MapleBuffStat
    {
        /*
        public static readonly BuffStat SPAWNMASK1 = new BuffStat(0x28000, 6);
        public static readonly BuffStat SPAWNMASK2 = new BuffStat(0x3200, 10);
        public static readonly BuffStat SPAWNMASK3 = new BuffStat(0xFE000000, 11);
        */

        //Correct v158:         

        public static readonly BuffStat STACKING_WDEF = new BuffStat(2, false, true);
        public static readonly BuffStat STACKING_MDEF = new BuffStat(3, false, true);
        public static readonly BuffStat STACKING_MAXHP = new BuffStat(4, false, true);
        public static readonly BuffStat STACKING_MAXHP_R = new BuffStat(5, false, true);
        public static readonly BuffStat STACKING_MAXMP = new BuffStat(6, false, true);
        public static readonly BuffStat STACKING_MAXMP_R = new BuffStat(6, false, true);
        public static readonly BuffStat STACKING_ACC = new BuffStat(8, false, true);
        public static readonly BuffStat STACKING_AVOID = new BuffStat(9, false, true);
        public static readonly BuffStat STACKING_JUMP = new BuffStat(10, false, true);
        public static readonly BuffStat STACKING_SPEED = new BuffStat(11, false, true);
        public static readonly BuffStat STACKING_STATS = new BuffStat(12, false, true);

        public static readonly BuffStat STACKING_BOOSTER = new BuffStat(15, false, true);

        public static readonly BuffStat STACKING_ATK = new BuffStat(20, false, true);        

        public static readonly BuffStat STACKING_STR = new BuffStat(22, false, true);
        public static readonly BuffStat STACKING_DEX = new BuffStat(23, false, true);
        public static readonly BuffStat STACKING_INT = new BuffStat(24, false, true);
        public static readonly BuffStat STACKING_LUK = new BuffStat(25, false, true);
        public static readonly BuffStat STACKING_DMG_R = new BuffStat(26, false, true);

        public static readonly BuffStat STACKING_ASRB = new BuffStat(30, false, true);
        public static readonly BuffStat STACKING_CRIT = new BuffStat(32, false, true);
        public static readonly BuffStat STACKING_WDEF_R = new BuffStat(33, false, true);
        public static readonly BuffStat STACKING_MAX_CRIT = new BuffStat(34, false, true);
        public static readonly BuffStat STACKING_BOSS = new BuffStat(35, false, true);
        public static readonly BuffStat STACKING_STATS_R = new BuffStat(36, false, true);
        public static readonly BuffStat STACKING_STANCE = new BuffStat(37, false, true);
        public static readonly BuffStat STACKING_IGNORE_DEF = new BuffStat(38, false, true);

        public static readonly BuffStat STACKING_MAX_CRIT_R = new BuffStat(42, false, true); //e.g. 100% will make 25% -> 50%, raises min crit towards max crit if min crit is 50% ???
        public static readonly BuffStat STACKING_AVOID_R = new BuffStat(43, false, true);
        public static readonly BuffStat STACKING_MDEF_R = new BuffStat(44, false, true);

        public static readonly BuffStat STACKING_MIN_CRIT = new BuffStat(47, false, true); //increases min crit to 50% then raises max crit after

        public static readonly BuffStat WATK = new BuffStat(50);
        public static readonly BuffStat WDEF = new BuffStat(51);
        public static readonly BuffStat MATK = new BuffStat(52);
        public static readonly BuffStat MDEF = new BuffStat(53);
        public static readonly BuffStat ACC = new BuffStat(54);
        public static readonly BuffStat AVOID = new BuffStat(55);
        public static readonly BuffStat HANDS = new BuffStat(56);
        public static readonly BuffStat SPEED = new BuffStat(57);
        public static readonly BuffStat JUMP = new BuffStat(58);
        public static readonly BuffStat MAGIC_GUARD = new BuffStat(59);
        public static readonly BuffStat DARK_SIGHT = new BuffStat(60);
        public static readonly BuffStat BOOSTER = new BuffStat(61);
        public static readonly BuffStat POWERGUARD = new BuffStat(62);
        public static readonly BuffStat MAXHP_R = new BuffStat(63);
        public static readonly BuffStat MAXMP_R = new BuffStat(64);

        public static readonly BuffStat SPAWNMASK1 = new BuffStat(416);
                
        

        /*
        public static readonly BuffStat WATK =              new BuffStat(0x2000, 12);
        public static readonly BuffStat WDEF =              new BuffStat(0x1000, 12);
        public static readonly BuffStat MATK =              new BuffStat(0x800, 12);
        public static readonly BuffStat MDEF =              new BuffStat(0x400, 12);
        public static readonly BuffStat ACC =               new BuffStat(0x200, 12);
        public static readonly BuffStat AVOID =             new BuffStat(0x100, 12);
        public static readonly BuffStat HANDS =             new BuffStat(0x80, 12);
        public static readonly BuffStat SPEED =             new BuffStat(0x40, 12);
        public static readonly BuffStat JUMP =              new BuffStat(0x20, 12);
        public static readonly BuffStat MAGIC_GUARD =       new BuffStat(0x10, 12);
        public static readonly BuffStat DARK_SIGHT =        new BuffStat(0x8, 12);
        public static readonly BuffStat BOOSTER =           new BuffStat(0x4, 12);
        public static readonly BuffStat POWERGUARD =        new BuffStat(0x2, 12);
        public static readonly BuffStat MAXHP_R =           new BuffStat(0x1, 12);

        public static readonly BuffStat MAXMP_R =           new BuffStat(0x80000000, 11);


        public static readonly BuffStat STACKING_BOOSTER =  new BuffStat(0x10000, 13);
        public static readonly BuffStat STACKING_SPEED =    new BuffStat(0x100000, 13, false, true);
        public static readonly BuffStat STACKING_WDEF =     new BuffStat(0x20000000, 13, false, true);
        */


            /*
        old:
        //public static readonly BuffStat WATK =              new BuffStat(0x1, 0);
        //public static readonly BuffStat WDEF =              new BuffStat(0x2, 0);
        //public static readonly BuffStat MATK =              new BuffStat(0x4, 0); 
        //public static readonly BuffStat MDEF =              new BuffStat(0x8, 0);
        //public static readonly BuffStat ACC =               new BuffStat(0x10, 0);
        //public static readonly BuffStat AVOID =             new BuffStat(0x20, 0);
        //public static readonly BuffStat HANDS =             new BuffStat(0x40, 0); 
        //public static readonly BuffStat SPEED =             new BuffStat(0x80, 0);
        //public static readonly BuffStat JUMP =              new BuffStat(0x100, 0);
        //public static readonly BuffStat MAGIC_GUARD =       new BuffStat(0x200, 0); 
        //public static readonly BuffStat DARK_SIGHT =        new BuffStat(0x400, 0);
        //public static readonly BuffStat BOOSTER =           new BuffStat(0x800, 0);        
        //public static readonly BuffStat MAXMP_R =           new BuffStat(0x4000, 0);
        public static readonly BuffStat INVINCIBLE =        new BuffStat(0x8000, 0);
        public static readonly BuffStat SOULARROW =         new BuffStat(0x10000, 0);        
        public static readonly BuffStat HOLY_SYMBOL =       new BuffStat(0x1000000, 0);
        public static readonly BuffStat SHADOW_PARTNER =    new BuffStat(0x4000000, 0);
        public static readonly BuffStat PICKPOCKET =        new BuffStat(0x8000000, 0);
        public static readonly BuffStat MESOGUARD =         new BuffStat(0x10000000, 0);
        

        public static readonly BuffStat LOW =               new BuffStat(0x1, 1);
        public static readonly BuffStat MORPH =             new BuffStat(0x2, 1);
        public static readonly BuffStat RECOVERY =          new BuffStat(0x4, 1);
        public static readonly BuffStat MAPLE_WARRIOR =     new BuffStat(0x8, 1);
        public static readonly BuffStat POWER_STANCE =      new BuffStat(0x10, 1);
        public static readonly BuffStat SHARP_EYES =        new BuffStat(0x20, 1);
        public static readonly BuffStat MANA_REFLECTION =   new BuffStat(0x40, 1);
        public static readonly BuffStat SEDUCE =            new BuffStat(0x80, 1);
        public static readonly BuffStat SPIRIT_CLAW =       new BuffStat(0x100, 1);
        public static readonly BuffStat INFINITY =          new BuffStat(0x200, 1);
        public static readonly BuffStat ADVANCED_BLESSING = new BuffStat(0x400, 1);
        public static readonly BuffStat HAMSTRING =         new BuffStat(0x800, 1); 
        public static readonly BuffStat BLIND =             new BuffStat(0x1000, 1); 
        public static readonly BuffStat CONCENTRATE =       new BuffStat(0x2000, 1); 
        public static readonly BuffStat ZOMBIFY =           new BuffStat(0x4000, 1); 
        public static readonly BuffStat ECHO_OF_HERO =      new BuffStat(0x8000, 1); 
        public static readonly BuffStat MESO_RATE =         new BuffStat(0x10000, 1); 
        public static readonly BuffStat GHOST_MORPH =       new BuffStat(0x20000, 1); 
        public static readonly BuffStat ARIANT_COSS_IMU =   new BuffStat(0x40000, 1); 
        public static readonly BuffStat REVERSE_DIRECTION = new BuffStat(0x80000, 1); 
        public static readonly BuffStat DROP_RATE =         new BuffStat(0x100000, 1); 
        public static readonly BuffStat EXPRATE =           new BuffStat(0x400000, 1); 
        public static readonly BuffStat ACASH_RATE =        new BuffStat(0x800000, 1); 
        public static readonly BuffStat ILLUSION =          new BuffStat(0x1000000, 1); 
        public static readonly BuffStat BERSERK_FURY =      new BuffStat(0x8000000, 1); 
        public static readonly BuffStat DIVINE_BODY =       new BuffStat(0x10000000, 1); 
        public static readonly BuffStat SPARK =             new BuffStat(0x20000000, 1); 
        public static readonly BuffStat ARIANT_COSS_IMU2 =  new BuffStat(0x40000000, 1); 
        public static readonly BuffStat FINALATTACK =       new BuffStat(0x80000000, 1);

        public static readonly BuffStat COMBO_ABILITY =     new BuffStat(0x4, 2, true); 
        
        public static readonly BuffStat ARAN_COMBO =        new BuffStat(0x10, 2);
        public static readonly BuffStat COMBO_DRAIN =       new BuffStat(0x20, 2);
        public static readonly BuffStat COMBO_BARRIER =     new BuffStat(0x40, 2);
        public static readonly BuffStat BODY_PRESSURE =     new BuffStat(0x80, 2);
        public static readonly BuffStat ANGELIC_BLESSING =  new BuffStat(0x80, 2);
        public static readonly BuffStat SMART_KNOCKBACK =   new BuffStat(0x100, 2);
     
        public static readonly BuffStat SLOW =              new BuffStat(0x4000, 2);
        public static readonly BuffStat MAGIC_SHIELD =      new BuffStat(0x8000, 2);
        public static readonly BuffStat MAGIC_RESISTANCE =  new BuffStat(0x10000, 2);
        public static readonly BuffStat SOUL_STONE =        new BuffStat(0x20000, 2);
        public static readonly BuffStat SOARING =           new BuffStat(0x40000, 2);
    
        public static readonly BuffStat LIGHTNING_CHARGE =  new BuffStat(0x100000, 2);
        public static readonly BuffStat FINAL_PACT1 =       new BuffStat(0x200000, 2);
        public static readonly BuffStat OWL_SPIRIT =        new BuffStat(0x400000, 2);
        public static readonly BuffStat ATTACK_PERCENT =    new BuffStat(0x800000, 2);
      
        public static readonly BuffStat FINAL_CUT =         new BuffStat(0x1000000, 2);
        public static readonly BuffStat DAMAGE_BUFF =       new BuffStat(0x2000000, 2);        
        public static readonly BuffStat RAINING_MINES =     new BuffStat(0x8000000, 2);
        
        public static readonly BuffStat ENHANCED_MAXHP =    new BuffStat(0x4000000, 2);
        public static readonly BuffStat ENHANCED_MAXMP =    new BuffStat(0x4000000, 2);
        public static readonly BuffStat ENHANCED_WATK =     new BuffStat(0x10000000, 2);
        public static readonly BuffStat ENHANCED_MATK =     new BuffStat(0x20000000, 2);
        public static readonly BuffStat ENHANCED_WDEF =     new BuffStat(0x40000000, 2);
        public static readonly BuffStat ENHANCED_MDEF =     new BuffStat(0x80000000, 2);      

        public static readonly BuffStat CRIT =            new BuffStat(0x10, 3);
        public static readonly BuffStat DMG_REDUCTION_R =   new BuffStat(0x20, 3);
        public static readonly BuffStat AVOID_PERCENT =     new BuffStat(0x40, 3);
        public static readonly BuffStat MAX_MP_PERCENT =    new BuffStat(0x80, 3);
        public static readonly BuffStat DAMAGE_TAKEN_BUFF = new BuffStat(0x100, 3);
        public static readonly BuffStat DODGE_CHANCE_BUFF = new BuffStat(0x200, 3);
        public static readonly BuffStat CONVERSION =        new BuffStat(0x400, 3);
        public static readonly BuffStat REAPER =            new BuffStat(0x800, 3);
        public static readonly BuffStat INFILTRATE =        new BuffStat(0x1000, 3);
        public static readonly BuffStat MECH_CHANGE =       new BuffStat(0x2000, 3);
        public static readonly BuffStat AURA =              new BuffStat(0x8000, 3);
        public static readonly BuffStat DARK_AURA =         new BuffStat(0x8000, 3);
        public static readonly BuffStat BLUE_AURA =         new BuffStat(0x10000, 3);
        public static readonly BuffStat YELLOW_AURA =       new BuffStat(0x20000, 3);
        public static readonly BuffStat BODY_BOOST =        new BuffStat(0x40000, 3);
        public static readonly BuffStat FELINE_BERSERK =    new BuffStat(0x80000, 3);
        public static readonly BuffStat DICE_ROLL =         new BuffStat(0x100000, 3);
        public static readonly BuffStat TELEPORT_MASTERY =  new BuffStat(0x200000, 3);
        public static readonly BuffStat PIRATES_REVENGE =   new BuffStat(0x400000, 3);
        public static readonly BuffStat EVIL_EYE =          new BuffStat(0x800000, 3); 
        public static readonly BuffStat COMBAT_ORDERS =     new BuffStat(0x1000000, 3);
        public static readonly BuffStat BEHOLDER =          new BuffStat(0x2000000, 3);      
        public static readonly BuffStat GIANT_POTION =      new BuffStat(0x8000000, 3);
        public static readonly BuffStat ONYX_SHROUD =       new BuffStat(0x10000000, 3);
        public static readonly BuffStat ONYX_WILL =         new BuffStat(0x20000000, 3);       
        public static readonly BuffStat BLESS =             new BuffStat(0x20000000, 3);


        public static readonly BuffStat THREATEN_PVP =      new BuffStat(0x4, 4);
        public static readonly BuffStat ICE_KNIGHT =        new BuffStat(0x8, 4);
        public static readonly BuffStat STR =               new BuffStat(0x10, 4);
        public static readonly BuffStat INT =               new BuffStat(0x20, 4);
        public static readonly BuffStat DEX =               new BuffStat(0x40, 4);
        public static readonly BuffStat LUK =               new BuffStat(0x80, 4);
        public static readonly BuffStat ATTACK =            new BuffStat(0x100, 4);
        public static readonly BuffStat STACKING_WATK =     new BuffStat(0x400, 4, false, true);
        public static readonly BuffStat STACKING_MATK =     new BuffStat(0x800, 4, false, true);
        public static readonly BuffStat STACKING_MAXHP =    new BuffStat(0x1000, 4, false, true);
        public static readonly BuffStat STACKING_MAXMP =    new BuffStat(0x2000, 4, false, true);
        public static readonly BuffStat STACKING_ACC =      new BuffStat(0x4000, 4, false, true);
        public static readonly BuffStat STACKING_AVOID =    new BuffStat(0x8000, 4, false, true);
        public static readonly BuffStat STACKING_JUMP =     new BuffStat(0x10000, 4, false, true);
        //public static readonly BuffStat STACKING_SPEED =    new BuffStat(0x20000, 4, false, true);
        public static readonly BuffStat STACKING_STATS =    new BuffStat(0x40000, 4, false, true);
        public static readonly BuffStat PVP_DAMAGE =        new BuffStat(0x200000, 4);
        public static readonly BuffStat PVP_ATTACK =        new BuffStat(0x400000, 4);
        public static readonly BuffStat INVINCIBILITY =     new BuffStat(0x800000, 4);
        public static readonly BuffStat HIDDEN_POTENTIAL =  new BuffStat(0x1000000, 4);
        public static readonly BuffStat ELEMENT_WEAKEN =    new BuffStat(0x2000000, 4);
        public static readonly BuffStat SNATCH =            new BuffStat(0x4000000, 4);
        public static readonly BuffStat FROZEN =            new BuffStat(0x8000000, 4);
        public static readonly BuffStat ICE_SKILL =         new BuffStat(0x20000000, 4);
        public static readonly BuffStat BOUNDLESS_RAGE =    new BuffStat(0x20000000, 4);

        public static readonly BuffStat HOLY_MAGIC_SHELL =  new BuffStat(0x1, 5);
        public static readonly BuffStat ARCANE_AIM =        new BuffStat(0x4, 5, true);
        //public static readonly BuffStat STACKING_WDEF =     new BuffStat(0x2000, 5, false, true);
        public static readonly BuffStat STACKING_MDEF =     new BuffStat(0x4000, 5, false, true);

        public static readonly BuffStat BODY_COUNT =        new BuffStat(0x20000, 6, false, true);
        public static readonly BuffStat STACKING_MAXHP_R =  new BuffStat(0x40000, 6, false, true);
        public static readonly BuffStat STACKING_MAXMP_R =  new BuffStat(0x80000, 6, false, true);        
        public static readonly BuffStat IGNORE_DEFENSE_R =  new BuffStat(0x2000000, 6);
        public static readonly BuffStat FINAL_PACT2 =       new BuffStat(0x80000000, 6);

        public static readonly BuffStat ECLIPSE_SUNFIRE =   new BuffStat(0x200, 7); 
        public static readonly BuffStat BLACK_BLESSING =    new BuffStat(0x800, 7, true);
        public static readonly BuffStat DIVINE_PROTECTION = new BuffStat(0x1000, 7);

        public static readonly BuffStat STACKING_CRIT =   new BuffStat(0x40000, 8, false, true);

        public static readonly BuffStat CROSS_SURGE =       new BuffStat(0x8000000, 9);
        public static readonly BuffStat FINAL_PACT3 =       new BuffStat(0x20000000, 9);

        public static readonly BuffStat BLESSED_ENSEMBLE =  new BuffStat(0x4, 10, true);
        public static readonly BuffStat QUIVER_CARTRIDGE =  new BuffStat(0x40, 10);
        public static readonly BuffStat CRITICAL_GROWTH =   new BuffStat(0x4000, 10, true);
        public static readonly BuffStat STACKING_BOSS_R =   new BuffStat(0x10000, 10, false, true);
        
    */
    }
}
