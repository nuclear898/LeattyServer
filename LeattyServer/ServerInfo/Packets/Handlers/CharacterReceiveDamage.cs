using System;
using System.Drawing;
using LeattyServer.Constants;
using LeattyServer.Data;
using LeattyServer.Data.WZ;
using LeattyServer.Helpers;
using LeattyServer.ServerInfo.Map;
using LeattyServer.ServerInfo.Map.Monster;
using LeattyServer.ServerInfo.Player;

namespace LeattyServer.ServerInfo.Packets.Handlers
{
    class CharacterReceiveDamage
    {
        public static void Handle(MapleClient c, PacketReader pr)
        {
            //6800 29939E7B 81BFD40F FD 00 60000000 00 00 037B 00 
            //6800 418AE151 6BE4D40F FF 00 01000000 00 00 47357C00 FE77AD00 00 00000000 00000000 00 00 00 

            //C101 CA566800 FD 60000000 00 60000000 
            //C101 68EB7A00 FF 01000000 00 F6761200 00 00000000 00000000 00 00 01000000 

            //effect
            //CD01 CA566800 [000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000200] 0100 7B000300 00 00 00 00000000 00000000 00000000 00000000 00000000 0000 
            MapleCharacter chr = c.Account.Character;
            if (chr.Hidden)
                return;
            pr.Skip(4);//rnd num
            pr.Skip(4);//tickcount
            byte type = pr.ReadByte();
            int damage = 0;
            pr.ReadByte();
            damage = pr.ReadInt();
            pr.Skip(2);
            int fake = 0;
            if (damage == -1)
            {
                fake = 4020002 + (chr.Job / 10 - 40) * 100000;
                if ((fake != NightLord.SHADOW_SHIFTER) && (fake != Shadower.SHADOW_SHIFTER))
                {
                    fake = NightLord.SHADOW_SHIFTER;
                }
            }
            switch (type)
            {
                case 0xFF://mob damage
                    {
                        int mobid = pr.ReadInt();
                        int oid = pr.ReadInt();
                        MapleMonster mob = chr.Map.GetMob(oid);
                        if (mob == null) 
                            return;
                        byte direction = pr.ReadByte();
                        int skillid = pr.ReadInt();
                        int pDmg = pr.ReadInt();
                        byte defType = pr.ReadByte();
                        bool pPhysical = false;
                        int pId = 0;
                        byte pType = 0;
                        Point pPos = Point.Empty;
                        pr.ReadByte();
                        if (skillid != 0)
                        {
                            pPhysical = pr.ReadBool();
                            pId = pr.ReadInt();
                            pType = pr.ReadByte();
                            pr.Skip(4);
                            pPos = pr.ReadPoint();
                        }
                        byte offset = 0;
                        int offsetD = 0;
                        if (pr.Available == 1L)
                        {
                            offset = pr.ReadByte();
                            if ((offset == 1) && (pr.Available >= 4L))
                            {
                                offsetD = pr.ReadInt();
                            }
                            if ((offset < 0) || (offset > 2))
                            {
                                offset = 0;
                            }
                        }
                        BroadcastDamagePlayerPacket(chr, type, damage, mobid, direction, skillid, pDmg, pPhysical, pId, pType, pPos, offset, offsetD, fake);
                        damage = DoMonsterDamageModifiers(damage, chr, mob, oid);
                        if (damage > 0)
                            chr.AddHP(-damage);
                    }
                    break;
                case 0xFD://no mob, such as fall damage
                    {
                        short skill = pr.ReadShort();//skill
                        pr.ReadByte();//?

                        byte offset = 0;
                        int offsetD = 0;
                        if (pr.Available >= 1)
                        {
                            offset = pr.ReadByte();
                            if ((offset == 1) && (pr.Available >= 4))
                            {
                                offsetD = pr.ReadInt();
                            }
                            if ((sbyte)offset < 0 || offset > 2)
                            {
                                offset = 0;
                            }
                        }
                        BroadcastDamagePlayerPacket(chr, type, damage, 0, 0, 0, 0, false, 0, 0, System.Drawing.Point.Empty, offset, offsetD, fake);
                        if (damage > 0)
                            chr.AddHP(-damage);
                    }
                    break;
                default:
                    ServerConsole.Error("Invalid CharacterReceiveDamage type {0} ", type);
                    break;
            }
        }

        private static int DoMonsterDamageModifiers(int damage, MapleCharacter chr, MapleMonster mobFrom, int mobFromOID)
        {
            if (damage == 0) //guard/miss etc
            {
                Buff buff = chr.GetBuff(Priest.HOLY_MAGIC_SHELL);
                if (buff != null)
                {
                    buff.Stacks -= 1;
                    if (buff.Stacks == 0)
                        chr.CancelBuff(Priest.HOLY_MAGIC_SHELL);
                }
            }
            #region Spearman
            if (chr.IsSpearman)
            {
                if (chr.Job >= JobConstants.BERSERKER)
                {
                    if (chr.Job == JobConstants.DARKKNIGHT)
                    {
                        byte evilEyeRevengeLevel = chr.GetSkillLevel(DarkKnight.REVENGE_OF_THE_EVIL_EYE);
                        if (evilEyeRevengeLevel > 0 && !chr.HasSkillOnCooldown(DarkKnight.REVENGE_OF_THE_EVIL_EYE))
                        {
                            MapleSummon evilEye = chr.GetSummon(Spearman.EVIL_EYE);
                            Buff evilEyebuff = chr.GetBuff(Spearman.EVIL_EYE);
                            if (evilEye != null && evilEyebuff != null && evilEyebuff.Stacks != Berserker.EVIL_EYE_OF_DOMINATION) 
                            {
                                SkillEffect effect = DataBuffer.GetCharacterSkillById(DarkKnight.REVENGE_OF_THE_EVIL_EYE).GetEffect(evilEyeRevengeLevel);
                                int summonDamage = (int)((effect.Info[CharacterSkillStat.damage] / 100.0) * chr.Stats.GetDamage());
                                int healHp = (int)((effect.Info[CharacterSkillStat.x] / 100.0) * summonDamage);
                                chr.AddHP(healHp);
                                //instant KO:
                                if (!mobFrom.IsBoss && summonDamage < mobFrom.HP)
                                {
                                    if (Functions.MakeChance(effect.Info[CharacterSkillStat.z]))
                                        summonDamage = mobFrom.HP;
                                }
                                evilEye.AttackMonster(summonDamage, 0x84, mobFrom);
                                chr.AddCooldownSilent(DarkKnight.REVENGE_OF_THE_EVIL_EYE, (uint)effect.Info[CharacterSkillStat.cooltime] * 1000, DateTime.UtcNow, false);
                            }
                        }
                        if (chr.HasBuff(DarkKnight.FINAL_PACT2))
                        {
                            return 0; //Invincible
                        }
                    }
                    Buff crossSurgeBuff = chr.GetBuff(Berserker.CROSS_SURGE);
                    if (crossSurgeBuff != null) 
                    {
                        int absorbPercent = crossSurgeBuff.Effect.Info[CharacterSkillStat.y];
                        int absorb = (int)((chr.Stats.MaxHp - chr.Hp) * (absorbPercent / 100.0));
                        absorb = Math.Min(absorb, crossSurgeBuff.Effect.Info[CharacterSkillStat.z]); //normally z = 4000
                        damage -= absorb;
                    }
                }
            }
            #endregion
            #region Magician
            else if (chr.IsMagician)
            {
                Buff buff = chr.GetBuff(Magician.MAGIC_GUARD);
                if (buff != null)
                {                    
                    if (chr.Mp > 0)
                    {
                        int absorb = (int)((buff.Effect.Info[CharacterSkillStat.x] / 100.0) * damage);
                        if (chr.Mp < absorb) 
                            absorb = chr.Mp;
                        chr.AddMP(-absorb);
                        damage -= absorb;
                    }                    
                }
            }
            #endregion
            #region Bandit
            else if (chr.IsBandit)
            {
                Buff mesoGuard = chr.GetBuff(Bandit.MESOGUARD);
                if (mesoGuard != null)
                {
                    double absorb = 0.5;
                    double mesoLoss = mesoGuard.Effect.Info[CharacterSkillStat.x] / 100.0;
                    double mesoLossReduction = 0.0;
                    byte MesoMasteryLevel = chr.GetSkillLevel(ChiefBandit.MESO_MASTERY);
                    if (MesoMasteryLevel > 0)
                    {
                        SkillEffect effect = DataBuffer.GetCharacterSkillById(ChiefBandit.MESO_MASTERY).GetEffect(MesoMasteryLevel);
                        absorb += effect.Info[CharacterSkillStat.v] / 100.0;
                        mesoLossReduction = effect.Info[CharacterSkillStat.v] / 100.0;
                    }
                    int damageAbsorbed = (int)(damage * absorb);
                    if (damageAbsorbed > 0)
                    {
                        int mesoUse = (int)(damageAbsorbed * mesoLoss);
                        mesoUse -= (int)(mesoUse * mesoLossReduction);
                        if (chr.Mesos >= mesoUse)
                        {
                            chr.Inventory.RemoveMesos(mesoUse, false);
                            damage -= damageAbsorbed;
                            int mesoDrops = Functions.Random(1, 4);
                            for (int i = 0; i < mesoDrops; i++)
                            {
                                chr.Map.SpawnMesoMapItem(1, chr.Position, chr.Map.GetDropPositionBelow(chr.Position, chr.Position), false, MapleDropType.Player, chr);
                            }
                        }
                    }
                }
            }
            #endregion
            #region Luminous
            else if (chr.IsLuminous)
            {
                Buff oldBuff = chr.GetBuff(Luminous2.BLACK_BLESSING);
                if (oldBuff != null)
                {
                    int remove = (int)(damage * 0.7);
                    damage -= remove;                 
                    if (oldBuff.Stacks < 2)
                        chr.CancelBuff(Luminous2.BLACK_BLESSING);
                    else
                    {
                        chr.CancelBuffSilent(Luminous2.BLACK_BLESSING);                        
                        Buff newBuff = new Buff(oldBuff.SkillId, oldBuff.Effect, oldBuff.Duration, chr);
                        newBuff.Stacks = oldBuff.Stacks - 1;
                        chr.GiveBuff(newBuff);
                    }
                }
                byte skillLevel = 0;
                if ((skillLevel = chr.GetSkillLevel(Luminous1.STANDARD_MAGIC_GUARD)) > 0)
                {
                    SkillEffect effect = DataBuffer.GetCharacterSkillById(Luminous1.STANDARD_MAGIC_GUARD).GetEffect(skillLevel);
                    double percent = effect.Info[CharacterSkillStat.x] / 100.0;
                    int absorb = (int)(percent * damage);
                    if (chr.Mp >= absorb)
                    {
                        chr.AddMP(absorb);
                        damage -= absorb;
                    }
                }
            }
            #endregion

            return damage;
        }

        public static void BroadcastDamagePlayerPacket(MapleCharacter chr, byte type, int damage, int monsteridfrom, byte direction, int skillid, int pDMG, bool pPhysical, int pID, byte pType, System.Drawing.Point pPos, byte offset, int offset_d, int fake)
        {
            PacketWriter pw = new PacketWriter();

            pw.WriteHeader(SendHeader.PlayerDamaged);
            pw.WriteInt(chr.Id);
            pw.WriteByte(type);
            pw.WriteInt(damage);
            pw.WriteByte(0);
            if (type == 0xFF)
            {
                pw.WriteInt(monsteridfrom);
                pw.WriteByte(direction);
                pw.WriteInt(skillid);
                pw.WriteInt(pDMG);
                pw.WriteByte(0);
                if (pDMG > 0)
                {
                    pw.WriteBool(pPhysical);
                    pw.WriteInt(pID);
                    pw.WriteByte(pType);
                    pw.WritePoint(pPos);
                }
                pw.WriteByte(offset);
                if (offset == 1)
                {
                    pw.WriteInt(offset_d);
                }
            }
            pw.WriteInt(0); //? v158
            pw.WriteInt(damage);
            if ((damage <= 0) || (fake > 0))
            {
                pw.WriteInt(fake);
            }
            chr.Map.BroadcastPacket(pw, chr, false);
        }
    }
}
