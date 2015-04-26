using System;
using LeattyServer.Constants;
using LeattyServer.Data;
using LeattyServer.Data.WZ;
using LeattyServer.Helpers;
using LeattyServer.ServerInfo.Inventory;
using LeattyServer.ServerInfo.Map;
using LeattyServer.ServerInfo.Map.Monster;
using LeattyServer.ServerInfo.Player;
using LeattyServer.ServerInfo.Player.ResourceSystems;

namespace LeattyServer.ServerInfo.Packets.Handlers
{
    class DealDamageHandler
    {
        public static void HandleMelee(MapleClient c, PacketReader pr, RecvHeader type)
        {
            pr.Skip(1);
            byte attackByte = pr.ReadByte();
            byte attacks = (byte)(attackByte & 0x0F);
            byte targets = (byte)((attackByte >> 4) & 0x0F);
            int skillId = pr.ReadInt();
            byte skillLevel = pr.ReadByte();
            pr.Skip(6); //melee
            //ranged pr.Skip(7);
            //magic pr.Skip(5);
            int charge = 0;
            SkillEffect effect = null;
            int fixedDamage = -1;
            double skillDamage = 1;
            if (skillId > 0)
            {
                if (skillLevel < 1 || skillLevel != c.Account.Character.GetSkillLevel(skillId))
                {
                    ServerConsole.Warning("Character skill level invalid " + skillLevel);
                    return;
                }
                CheckChargeAndDamage(skillId, skillLevel, pr, out charge, out fixedDamage, out effect, out skillDamage);
            }
            byte unk = pr.ReadByte();
            short display = pr.ReadShort();
            pr.Skip(5);
            byte speed = pr.ReadByte();
            int tickCount = pr.ReadInt();
            pr.Skip(8);
            AttackInfo attackInfo = CreateAttackInfo(pr, attackByte, targets, attacks, skillId, skillLevel, charge, speed, display, fixedDamage, c.Account.Character.Stats, skillDamage);
            if (attackInfo != null)
                HandleAttackInfo(c, attackInfo, SendHeader.MeleeAttack, effect);
        }

        public static void HandleRanged(MapleClient c, PacketReader pr)
        {
            pr.Skip(2);
            byte attackByte = pr.ReadByte();
            byte attacks = (byte)(attackByte & 0x0F);
            byte targets = (byte)((attackByte >> 4) & 0x0F);
            int skillId = pr.ReadInt();
            byte skillLevel = pr.ReadByte();
            pr.Skip(7);
            int charge = 0;
            SkillEffect effect = null;
            int fixedDamage = -1;
            double skillDamage = 1;
            if (skillId > 0)
            {
                if (skillLevel < 1 || skillLevel != c.Account.Character.GetSkillLevel(skillId))
                {
                    ServerConsole.Warning("Character skill level invalid " + skillLevel);
                    return;
                }
                CheckChargeAndDamage(skillId, skillLevel, pr, out charge, out fixedDamage, out effect, out skillDamage);
            }
            else // Normal attack
            {
                if (!HandleRangedAttackAmmoUsage(c.Account.Character, 1))
                    return;
            }
            byte unk = pr.ReadByte();
            short display = pr.ReadShort();
            pr.Skip(5);
            byte speed = pr.ReadByte();
            int tickCount = pr.ReadInt();
            pr.Skip(13);

            AttackInfo attackInfo = CreateAttackInfo(pr, attackByte, targets, attacks, skillId, skillLevel, charge, speed, display, fixedDamage, c.Account.Character.Stats, skillDamage);
            if (attackInfo != null)
                HandleAttackInfo(c, attackInfo, SendHeader.RangedAttack, effect);
        }

        public static void HandleMagic(MapleClient c, PacketReader pr)
        {
            pr.Skip(1);
            byte attackByte = pr.ReadByte();
            byte attacks = (byte)(attackByte & 0x0F);
            byte targets = (byte)((attackByte >> 4) & 0x0F);
            int skillId = pr.ReadInt();
            byte skillLevel = pr.ReadByte();
            pr.Skip(5);
            int charge = 0;
            SkillEffect effect = null;
            int fixedDamage = -1;
            double skillDamage = 1;            
            if (skillId > 0)
            {
                if (skillLevel < 1 || skillLevel != c.Account.Character.GetSkillLevel(skillId))
                {
                    ServerConsole.Warning("Character skill level invalid " + skillLevel);
                    return;
                }
                if (skillLevel < 1)
                    return;
                CheckChargeAndDamage(skillId, skillLevel, pr, out charge, out fixedDamage, out effect, out skillDamage);
            }
            byte unk = pr.ReadByte();
            short display = pr.ReadShort();
            pr.Skip(5);
            byte speed = pr.ReadByte();
            int tickCount = pr.ReadInt();
            //ServerConsole.Info(tickCount.ToString());
            pr.Skip(4);

            AttackInfo attackInfo = CreateAttackInfo(pr, attackByte, targets, attacks, skillId, skillLevel, charge, speed, display, fixedDamage, c.Account.Character.Stats, skillDamage);
            if (attackInfo != null)
                HandleAttackInfo(c, attackInfo, SendHeader.MagicAttack, effect);
        }

        private static bool CheckChargeAndDamage(int skillId, byte skillLevel, PacketReader pr, out int charge, out int fixedDamage, out SkillEffect effect, out double skillDamage)
        {
            WzCharacterSkill wzSkill = DataBuffer.GetCharacterSkillById(skillId);
            if (wzSkill != null)
            {
                charge = wzSkill.IsKeyDownSkill ? pr.ReadInt() : 0;
                effect = wzSkill.GetEffect(skillLevel);
                if (!effect.Info.TryGetValue(CharacterSkillStat.fixdamage, out fixedDamage))
                {
                    fixedDamage = -1;
                    int temp;
                    if (!effect.Info.TryGetValue(CharacterSkillStat.damage, out temp))
                        temp = effect.Info[CharacterSkillStat.x];
                    skillDamage = temp / 100.0;
                }
                else
                    skillDamage = 1;
                return true;
            }
            effect = null;
            charge = 0;
            skillDamage = 1;
            fixedDamage = -1;
            return false;
        }

        private static AttackInfo CreateAttackInfo(PacketReader pr, byte attackByte, int targets, int attacks, int skillId, byte skillLevel, int charge, byte speed, short display, int fixedDamage, BuffedCharacterStats stats, double skillDamage)
        {
            AttackInfo attackInfo = new AttackInfo();
            attackInfo.AttacksByte = attackByte;
            attackInfo.Targets = targets;
            attackInfo.Attacks = attacks;
            attackInfo.SkillId = skillId;
            attackInfo.SkillLevel = skillLevel;
            attackInfo.Charge = charge;
            attackInfo.Speed = speed;
            attackInfo.Display = display;
            for (int i = 0; i < targets; i++)
            {
                AttackPair ap = new AttackPair();
                ap.TargetObjectId = pr.ReadInt();
                pr.Skip(20);

                for (int j = 0; j < attacks; j++)
                {
                    int damage = pr.ReadInt();
                    if (fixedDamage != -1 && fixedDamage != damage)
                    {
                        ServerConsole.Warning("Incorrect fixed damage, damage hack. Probably due to packet or wz editing.");
                        return null;
                    }
                    ap.Damage.Add(damage);
                    ap.Crits.Add(fixedDamage != -1 ? false : stats.IsCrit(damage, skillDamage));
                }
                pr.Skip(8);
                attackInfo.TargetDamageList.Add(ap);
            }
            attackInfo.Position = pr.ReadPoint();
            return attackInfo;
        }

        private static void HandleAttackInfo(MapleClient c, AttackInfo attackInfo, SendHeader type, SkillEffect effect)
        {
            //Anti-cheat
            //c.CheatTracker.Trigger(AntiCheat.TriggerType.Attack);
            WzCharacterSkill wzSkill = effect != null ? effect.Parent : null;
            MapleCharacter chr = c.Account.Character;
            if (attackInfo.SkillId > 0)
            {
                if (!SkillEffect.CheckAndApplySkillEffect(c.Account.Character, attackInfo.SkillId, wzSkill, -1, attackInfo.Targets, attackInfo.Attacks))
                    return;
            }                        
            
            chr.Map.BroadcastPacket(GenerateAttackInfo(type, c.Account.Character, attackInfo), c.Account.Character, false);
            long totalDamage = 0;
            #region DoTs and Pickpocket
            int pickPocketProp = 0;
            int dotSkillId = 0;            
            int dotChance = 0;
            int dotDamage = 0;
            int dotTimeMS = 0;
            int dotMaxStacks = 1;
            #region Thief
            if (chr.IsThief)
            {
                byte venomSkillLevel = 0;
                if (chr.IsBandit)
                {
                    Buff pickPocket = chr.GetBuff(ChiefBandit.PICKPOCKET);
                    if (pickPocket != null)
                        pickPocketProp = pickPocket.Effect.Info[CharacterSkillStat.prop];
                    venomSkillLevel = chr.GetSkillLevel(ChiefBandit.VENOM);
                    if (venomSkillLevel > 0)
                    {
                        dotSkillId = ChiefBandit.VENOM;
                        byte toxicVenomSkillLevel = chr.GetSkillLevel(Shadower.TOXIC_VENOM);
                        if (toxicVenomSkillLevel > 0)
                        {
                            venomSkillLevel = toxicVenomSkillLevel;
                            dotSkillId = Shadower.TOXIC_VENOM;
                        }
                    }
                }
                else if (chr.IsAssassin)
                {
                    #region Assassin
                    venomSkillLevel = chr.GetSkillLevel(Hermit.VENOM);
                    if (venomSkillLevel > 0)
                    {
                        dotSkillId = Hermit.VENOM;
                        byte toxicVenomSkillLevel = chr.GetSkillLevel(NightLord.TOXIC_VENOM);
                        if (toxicVenomSkillLevel > 0)
                        {
                            venomSkillLevel = toxicVenomSkillLevel;
                            dotSkillId = NightLord.TOXIC_VENOM;
                        }
                    }
                    #endregion
                }
                else if (chr.IsDualBlade)
                {
                    #region DualBlade
                    venomSkillLevel = chr.GetSkillLevel(DualBlade3.VENOM);
                    if (venomSkillLevel > 0)
                    {
                        dotSkillId = DualBlade3.VENOM;
                        byte toxicVenomSkillLevel = chr.GetSkillLevel(DualBlade4.TOXIC_VENOM);
                        if (toxicVenomSkillLevel > 0)
                        {
                            venomSkillLevel = toxicVenomSkillLevel;
                            dotSkillId = DualBlade4.TOXIC_VENOM;
                        }
                    }
                    #endregion
                }
                if (venomSkillLevel > 0)
                {
                    SkillEffect venomEffect = DataBuffer.GetCharacterSkillById(dotSkillId).GetEffect(venomSkillLevel);
                    dotChance = venomEffect.Info[CharacterSkillStat.prop];                    
                    dotDamage = (int)(chr.Stats.GetDamage() * (venomEffect.Info[CharacterSkillStat.dot] / 100.0));
                    dotTimeMS = venomEffect.Info[CharacterSkillStat.dotTime] * 1000;
                    if (!venomEffect.Info.TryGetValue(CharacterSkillStat.dotSuperpos, out dotMaxStacks))
                        dotMaxStacks = 1;
                }

            }
            #endregion
            if (attackInfo.SkillId > 0 && effect.Info.TryGetValue(CharacterSkillStat.dot, out dotDamage)) //Skill has/is dot
            {
                dotTimeMS = effect.Info[CharacterSkillStat.dotTime] * 1000;
                if (!effect.Info.TryGetValue(CharacterSkillStat.prop, out dotChance))                
                    dotChance = 100;
                dotSkillId = attackInfo.SkillId;
                dotDamage = (int)(chr.Stats.GetDamage() * (dotDamage / 100.0));
                if (!effect.Info.TryGetValue(CharacterSkillStat.dotSuperpos, out dotMaxStacks))
                    dotMaxStacks = 1;
            }
            #endregion

            foreach (AttackPair ap in attackInfo.TargetDamageList)
            {
                MapleMonster mob = chr.Map.GetMob(ap.TargetObjectId);
                if (mob != null && mob.Alive)
                {
                    long totalMobDamage = 0;
                    foreach (int damage in ap.Damage)
                    {
                        totalMobDamage += damage;
                    }
                    if (totalMobDamage > 0)
                    {
                        totalDamage += totalMobDamage;
                        if (totalDamage > int.MaxValue)
                            totalDamage = int.MaxValue;

                        #region Status effects
                        if (effect != null)
                        {
                            foreach (MonsterBuffApplication mba in effect.MonsterBuffs)
                            {
                                if (Functions.MakeChance(mba.Prop))
                                {
                                    foreach(var kvp in mba.Buffs)
                                    {
                                        mob.ApplyStatusEffect(attackInfo.SkillId, kvp.Key, kvp.Value, mba.Duration, chr);
                                    }
                                }
                            }
                        }
                        #endregion

                        #region MP Eater
                        if (chr.Stats.MpEaterProp > 0)
                        {
                            if (Functions.MakeChance(chr.Stats.MpEaterProp))
                            {
                                int mpSteal = (int)((chr.Stats.MpEaterR / 100.0) * mob.WzInfo.MP);
                                chr.AddMP(mpSteal);
                            }
                        }
                        #endregion
                        #region Bandit
                        if (chr.IsBandit)
                        {
                            if (Functions.MakeChance(pickPocketProp))
                            {
                                chr.Map.SpawnMesoMapItem(1, mob.Position, chr.Map.GetDropPositionBelow(mob.Position, mob.Position), false, MapleDropType.Player, chr);
                            }
                            if (attackInfo.SkillId == Bandit.STEAL)
                            {
                                int prop = DataBuffer.GetCharacterSkillById(Bandit.STEAL).GetEffect(chr.GetSkillLevel(Bandit.STEAL)).Info[CharacterSkillStat.prop];
                                if (Functions.MakeChance(prop))
                                {
                                    MapleItem item = mob.TryGetStealableItem(chr.Id, chr.Name);
                                    if (item != null)
                                        chr.Map.SpawnMapItem(item, mob.Position, chr.Map.GetDropPositionBelow(chr.Position, mob.Position), false, Map.MapleDropType.Player, chr);
                                }
                            }
                        }
                        #endregion

                        if (Functions.MakeChance(dotChance))                        
                            mob.ApplyPoison(dotSkillId, dotTimeMS, dotDamage, 1000, chr, dotMaxStacks);                        
                        
                        mob.Damage(chr, (int)totalDamage);
                    }
                }
            }
            #region special skill handling
            if (type == SendHeader.RangedAttack)
            {                
                if (attackInfo.Targets > 0 && chr.IsHunter)
                {
                    #region QuiverCartridge
                    QuiverCartridgeSystem qcs = chr.Resource as QuiverCartridgeSystem;
                    if (qcs != null && qcs.ChosenArrow > -1)
                    {
                        int usedArrow = qcs.HandleUse(c);
                        switch (usedArrow)
                        {
                            case 0: // Blood
                                if (Functions.MakeChance(50)) //50% chance to heal 20% of damage as hp
                                    chr.AddHP((int)(totalDamage * 0.2));
                                break;
                            case 1: // Poison
                                //TODO: poison, 90% damage, 8 seconds, stacks 3 times
                                break;
                            case 2: // Magic, don't need handling I think
                                break;
                        }
                    }
                    #endregion
                }
            }

            if (totalDamage > 0)
            {
                BuffedCharacterStats stats = chr.Stats;
                if (stats.LifeStealProp > 0 && stats.LifeStealR > 0)
                {
                    if (Functions.MakeChance(stats.LifeStealProp))
                    {
                        int lifesteal = (int)((stats.LifeStealR / 100.0) * totalDamage);
                        chr.AddHP(lifesteal);
                    }
                }

                if (chr.IsMagician)
                {
                    #region ArcaneAim
                    int arcaneAimId = 0;
                    if (chr.Job == JobConstants.FIREPOISON4) arcaneAimId = FirePoison4.ARCANE_AIM;
                    else if (chr.Job == JobConstants.ICELIGHTNING4) arcaneAimId = IceLightning4.ARCANE_AIM;
                    else if (chr.Job == JobConstants.BISHOP) arcaneAimId = Bishop.ARCANE_AIM;
                    if (arcaneAimId > 0)
                    {
                        byte skillLevel = chr.GetSkillLevel(arcaneAimId);
                        if (skillLevel > 0)
                        {
                            if ((DateTime.UtcNow.Subtract(chr.LastAttackTime).TotalMilliseconds) < 5000)
                            {
                                Buff oldBuff = chr.GetBuff(arcaneAimId);
                                if (oldBuff != null)
                                {
                                    int prop = oldBuff.Effect.Info[CharacterSkillStat.prop];
                                    if (Functions.MakeChance(prop))
                                    {
                                        Buff newBuff = new Buff(arcaneAimId, oldBuff.Effect, oldBuff.Duration, chr);
                                        int oldStacks = oldBuff.Stacks / 6;
                                        newBuff.Stacks = Math.Min(30, (oldStacks + 1) * 6);
                                        chr.GiveBuff(newBuff);
                                    }
                                }
                                else
                                {
                                    SkillEffect arcaneAimEffect = DataBuffer.GetCharacterSkillById(arcaneAimId).GetEffect(skillLevel);
                                    int prop = arcaneAimEffect.Info[CharacterSkillStat.prop];
                                    if (Functions.MakeChance(prop))
                                    {
                                        Buff newBuff = new Buff(arcaneAimId, arcaneAimEffect, 5000, chr);
                                        newBuff.Stacks = 6;
                                        chr.GiveBuff(newBuff);
                                    }
                                }
                            }
                        }
                    }
                    #endregion
                }
                else if (chr.IsThief)
                {                   
                    if (chr.IsBandit)
                    {                        
                        chr.IncreaseCriticalGrowth(true);
                        byte skillLevel = chr.GetSkillLevel(Shadower.SHADOWER_INSTINCT);
                        if (skillLevel > 0)
                            ((BodyCountSystem)chr.Resource).IncreaseBodyCount(c);
                    }
                } 
            }
            #endregion
            chr.LastAttackTime = DateTime.UtcNow;
        }

        public static PacketWriter GenerateAttackInfo(SendHeader type, MapleCharacter chr, AttackInfo info)
        {
            PacketWriter pw = new PacketWriter(type);
            pw.WriteInt(chr.Id);
            pw.WriteByte(0); //v158
            pw.WriteByte(info.AttacksByte);
            pw.WriteByte(chr.Level);
            if (info.SkillId > 0)
            {
                pw.WriteByte(info.SkillLevel);
                pw.WriteInt(info.SkillId);
            }
            else
            {
                pw.WriteByte(0);
            }
            
            pw.WriteByte(0);//ultlevel
            //if (ultlevel >0)
            //writeint(3220010);

            pw.WriteInt(0);
            pw.WriteByte(info.Unk);
            pw.WriteShort(info.Display);
            pw.WriteByte(info.Speed);
            pw.WriteByte(0); //pw.WriteByte((byte)chr.Stats.MasteryR);
            if (type == SendHeader.RangedAttack)
                pw.WritePoint(info.Position);
            else 
                pw.WriteInt(0);
            foreach (AttackPair ap in info.TargetDamageList)
            {
                pw.WriteInt(ap.TargetObjectId);
                pw.WriteByte(7);
                pw.WriteByte(0);
                pw.WriteBool(false);
                if (info.SkillId == 42111002)
                {
                    pw.WriteByte((byte)ap.Damage.Count);
                    foreach (int i in ap.Damage)
                    {
                        pw.WriteInt(i);
                    }
                }
                else
                {
                    for (int i = 0; i < ap.Damage.Count; i++)
                    {
                        pw.WriteBool(ap.Crits[i]);
                        pw.WriteInt(ap.Damage[i]);
                    }
                }
            }
            if (info.Charge > 0) //and if type == charge attack
            {
                pw.WriteInt(info.Charge);
            }
            return pw;
        }

        //Returns false if character doensn't have ammo
        public static bool HandleRangedAttackAmmoUsage(MapleCharacter chr, int bulletCon)
        {
            if (!chr.IsMechanic && !chr.IsMercedes) // Don't use ammo
            {
                MapleEquip weapon = chr.Inventory.GetEquippedItem((short)MapleEquipPosition.Weapon) as MapleEquip;
                MapleItemType weaponType = weapon.ItemType;
                int ammoItemId = 0;
                switch (weaponType)
                {
                    case MapleItemType.Bow:
                        if (!chr.HasBuff(Hunter.SOUL_ARROW_BOW) && !chr.HasBuff(WindArcher2.SOUL_ARROW))
                        {
                            MapleItem ammoItem = chr.Inventory.GetFirstItemFromInventory(MapleInventoryType.Use, item => item.IsBowArrow && item.Quantity > 0);
                            if (ammoItem == null) return false; //player has no bow arrows                                        
                            ammoItemId = ammoItem.ItemId;
                        }
                        break;
                    case MapleItemType.Crossbow:
                        if (!chr.HasBuff(Crossbowman.SOUL_ARROW_CROSSBOW) && !chr.HasBuff(WildHunter2.SOUL_ARROW_CROSSBOW))
                        {
                            MapleItem ammoItem = chr.Inventory.GetFirstItemFromInventory(MapleInventoryType.Use, item => item.IsCrossbowArrow && item.Quantity > 0);
                            if (ammoItem == null) return false; //player has no xbow arrows                                        
                            ammoItemId = ammoItem.ItemId;
                        }
                        break;
                    case MapleItemType.Claw:
                        if (!chr.HasBuff(Hermit.SHADOW_STARS) && !chr.HasBuff(NightWalker3.SHADOW_STARS))
                        {
                            MapleItem ammoItem = chr.Inventory.GetFirstItemFromInventory(MapleInventoryType.Use, item => item.IsThrowingStar && item.Quantity > 0);
                            if (ammoItem == null) return false; //player has no bullets                                        
                            ammoItemId = ammoItem.ItemId;
                        }
                        break;
                    case MapleItemType.Gun:
                        if (!chr.HasBuff(Gunslinger.INFINITY_BLAST))
                        {
                            MapleItem ammoItem = chr.Inventory.GetFirstItemFromInventory(MapleInventoryType.Use, item => item.IsBullet && item.Quantity > 0);
                            if (ammoItem == null) return false; //player has no bullets                                        
                            ammoItemId = ammoItem.ItemId;
                        }
                        break;
                }
                if (ammoItemId > 0)
                {
                    chr.Inventory.RemoveItemsById(ammoItemId, bulletCon, false); //Even if player only has 1 bullet left and bulletCon is > 1, we'll allow it since it removes the item or stack anyway
                }
            }
            return true;
        }
    }
}
