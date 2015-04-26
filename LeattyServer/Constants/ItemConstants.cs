using LeattyServer.Helpers;
using LeattyServer.ServerInfo.Inventory;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Remoting.Messaging;
using System.Text;
using System.Threading.Tasks;

namespace LeattyServer.Constants
{
    public static class ItemConstants
    {
        #region Weapon Damage Modifiers
        private static readonly WeaponInfo defaultWeaponModInfo = new WeaponInfo(1.3, 20);
        private static Dictionary<MapleItemType, WeaponInfo> WeaponInfo = new Dictionary<MapleItemType, WeaponInfo>()
        {
            { MapleItemType.ShiningRod, new WeaponInfo(1.2, 25) }, //Luminous       
            { MapleItemType.SoulShooter, new WeaponInfo(1.375, 15) }, //Angelic Buster
            { MapleItemType.Desperado, new WeaponInfo(1.0, 20) }, //Demon Avenger
            { MapleItemType.WhipBlade, new WeaponInfo(1.315, 20) }, //Xenon
            { MapleItemType.Scepter, new WeaponInfo(1.338, 20) }, //BeastTamer
            { MapleItemType.OneHandedSword, new WeaponInfo(1.2, 20) },
            { MapleItemType.OneHandedAxe, new WeaponInfo(1.2, 20) },
            { MapleItemType.OneHandedMace, new WeaponInfo(1.2, 20) },
            { MapleItemType.Dagger, new WeaponInfo(1.56, 20) },
            { MapleItemType.Cane, new WeaponInfo(1.3, 20) },
            { MapleItemType.Wand, new WeaponInfo(1.2, 25) },
            { MapleItemType.Staff, new WeaponInfo(1.2, 25) },
            { MapleItemType.TwoHandedSword, new WeaponInfo(1.34, 20) },
            { MapleItemType.TwoHandedAxe, new WeaponInfo(1.34, 20) },
            { MapleItemType.TwoHandedMace, new WeaponInfo(1.34, 20) },
            { MapleItemType.Spear, new WeaponInfo(1.49, 20) },
            { MapleItemType.Polearm, new WeaponInfo(1.49, 20) },
            { MapleItemType.Bow, new WeaponInfo(1.3, 15) },
            { MapleItemType.Crossbow, new WeaponInfo(1.35, 15) },
            { MapleItemType.Claw, new WeaponInfo(1.75, 15) },
            { MapleItemType.Knuckle, new WeaponInfo(1.7, 20) },
            { MapleItemType.Gun, new WeaponInfo(1.5, 15) },
            { MapleItemType.DualBowGun, new WeaponInfo(1.3, 15) },
            { MapleItemType.Cannon, new WeaponInfo(1.5, 15) },
            { MapleItemType.Katana, new WeaponInfo(1.25, 20) }, //Hayato
            { MapleItemType.Fan, new WeaponInfo(1.35, 25) }, //Kanna
            { MapleItemType.BigSword, new WeaponInfo(1.3, 15) }, //Zero
            { MapleItemType.LongSword, new WeaponInfo(1.3, 15) } //Zero
        };
        #endregion

        public static WeaponInfo GetWeaponModifierInfo(MapleItemType weaponType)
        {
            WeaponInfo ret;
            if (WeaponInfo.TryGetValue(weaponType, out ret))
                return ret;
            ServerConsole.Warning("Unhandled MapleItemType \"" + Enum.GetName(typeof(MapleItemType), weaponType) + "\" for getting Weapon Modifier Info in ItemConstants");
            return defaultWeaponModInfo;
        }

        public static MapleInventoryType GetInventoryType(int itemId)
        {
            byte type = (byte)(itemId / 1000000);
            if (type < 1 || type > 5)
            {
                return MapleInventoryType.Undefined;
            }
            return (MapleInventoryType)type;
        }

        public static bool IsWeapon(int itemId) => IsWeapon(GetMapleItemType(itemId));

        public static bool IsWeapon(MapleItemType itemType) =>
            (itemType >= MapleItemType.ShiningRod && itemType <= MapleItemType.CashShopEffectWeapon) &&
            itemType != MapleItemType.HerbalismTool && itemType != MapleItemType.MiningTool &&
            !IsOffhand(itemType);

        public static bool IsOffhand(int itemId) => IsOffhand(GetMapleItemType(itemId));

        public static bool IsOffhand(MapleItemType itemType) => itemType == MapleItemType.SecondaryWeapon || itemType == MapleItemType.Shield;

        public static bool IsAccessory(int itemId) => IsAccessory(GetMapleItemType(itemId));

        public static bool IsAccessory(MapleItemType itemType) =>
                (itemType >= MapleItemType.FaceAccessory && itemType <= MapleItemType.Top) ||
                (itemType >= MapleItemType.Ring && itemType <= MapleItemType.MonsterBook) ||
                (itemType >= MapleItemType.Badge && itemType <= MapleItemType.Emblem);

        public static MapleItemType GetMapleItemType(int itemId)
        {
            int itemBase = itemId / 10000;
            if (Enum.IsDefined(typeof(MapleItemType), itemBase))
                return (MapleItemType)itemBase;
            return MapleItemType.Undefined;
        }

        public static bool GetItemPotentialType()
        {
            return true;
            /*
            public static boolean optionTypeFits(int optionType, int itemId) {
                switch (optionType) {
                case 10:
                    return isWeapon(itemId);
                case 11:
                    return !isWeapon(itemId);
                case 20:
                    return (!isAccessory(itemId)) && (!isWeapon(itemId));
                case 40:
                    return isAccessory(itemId);
                case 51:
                    return itemId / 10000 == 100;
                case 52:
                    return (itemId / 10000 == 104) || (itemId / 10000 == 105);
                case 53:
                    return (itemId / 10000 == 106) || (itemId / 10000 == 105);
                case 54:
                    return itemId / 10000 == 108;
                case 55:
                    return itemId / 10000 == 107;
                }
                return true;
            }
            */
        }

    }

    public enum MapleItemType
    {
        Undefined = 0,
        Cap = 100,
        FaceAccessory = 101,
        EyeAccessory = 102,
        Earring = 103,
        Top = 104,
        Overall = 105,
        Legs = 106,
        Shoes = 107,
        Glove = 108,
        Shield = 109,
        Cape = 110,
        Ring = 111,
        Pendant = 112,
        Belt = 113,
        Medal = 114,
        Shoulder = 115,
        Pocket = 116,
        MonsterBook = 117,
        Badge = 118,
        Emblem = 119,
        Totem = 120,

        ShiningRod = 121, //Luminous
        SoulShooter = 122, //Angelic Buster
        Desperado = 123, //Demon Avenger
        WhipBlade = 124, //Xenon
        Scepter = 125, //BeastTamer
        OneHandedSword = 130,
        OneHandedAxe = 131,
        OneHandedMace = 132,
        Dagger = 133,
        Katara = 134,
        SecondaryWeapon = 135, //magic arrow, luminous orb etc
        Cane = 136,
        Wand = 137,
        Staff = 138,
        Unk = 139,
        TwoHandedSword = 140,
        TwoHandedAxe = 141,
        TwoHandedMace = 142,
        Spear = 143,
        Polearm = 144,
        Bow = 145,
        Crossbow = 146,
        Claw = 147,
        Knuckle = 148,
        Gun = 149,
        HerbalismTool = 150,
        MiningTool = 151,
        DualBowGun = 152,
        Cannon = 153,
        Katana = 154, //Hayato
        Fan = 155, //Kanna
        BigSword = 156, //Zero
        LongSword = 157, //Zero
        CashShopEffectWeapon = 160,

        Engine = 161, //Mechanic
        Android = 166,
        AndroidHeart = 167,
        Bit = 168,
        PetEquip = 180,
        MonsterBattle = 184,
        TamingMob = 190,
        Dragon = 194, //Evan

        RegularEquipScroll = 204,
        SpecialEquipScroll = 253,

        ThrowingStar = 207,
        Bullet = 233,

        Chair = 301
    }

    public class WeaponInfo
    {
        public double DamageModifier;
        public int BaseMastery;

        public WeaponInfo(double mod, int baseMastery)
        {
            DamageModifier = mod;
            BaseMastery = baseMastery;
        }
    }
}
