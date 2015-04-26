using System;
using System.Collections.Generic;
using System.Drawing;
using LeattyServer.Constants;
using LeattyServer.Data;
using LeattyServer.Data.WZ;
using LeattyServer.Helpers;
using LeattyServer.ServerInfo.Map;
using LeattyServer.ServerInfo.Map.Monster;
using LeattyServer.ServerInfo.Movement;
using LeattyServer.ServerInfo.Player;

namespace LeattyServer.ServerInfo.Packets.Handlers
{
    public class SummonHandler
    {
        public static void HandleMove(MapleClient c, PacketReader pr)
        {
            int objectId = pr.ReadInt();
            MapleSummon summon = c.Account.Character.Map.GetSummon(objectId);
            if (summon != null && summon.Owner.Id == c.Account.Character.Id && summon.MovementType != SummonMovementType.Stationary)
            {
                pr.Skip(4);
                Point startPosition = pr.ReadPoint();
                pr.Skip(4);
                List<MapleMovementFragment> movements = ParseMovement.Parse(pr);
                if (movements != null && movements.Count > 0)
                {
                    c.Account.Character.Map.BroadcastPacket(summon.MovePacket(startPosition, movements), c.Account.Character);
                    for (int i = movements.Count - 1; i >= 0; i--)
                    {
                        if (movements[i] is AbsoluteLifeMovement)
                        {
                            summon.Position = movements[i].Position;
                            break;
                        }
                    }
                }
            }
        }

        public static void HandleRemove(MapleClient c, PacketReader pr)
        {
            int objectId = pr.ReadInt();
            MapleSummon summon = c.Account.Character.Map.GetSummon(objectId);
            if (summon != null)
            {
                if (summon.Owner.Id == c.Account.Character.Id)
                {
                    c.Account.Character.RemoveSummon(summon.SourceSkillId);
                }
            }
        }

        public static void HandleSkill(MapleClient c, PacketReader pr)
        {
            int objectId = pr.ReadInt();
            MapleSummon summon = c.Account.Character.Map.GetSummon(objectId);
            if (summon != null && summon.Owner.Id == c.Account.Character.Id && summon.MovementType != SummonMovementType.Stationary)
            {
                int skillId = pr.ReadInt();                
                WzCharacterSkill skillInfo = DataBuffer.GetCharacterSkillById(skillId);
                if (skillInfo == null)
                    return;
                SkillEffect effect = skillInfo.GetEffect(summon.SkillLevel);
                switch (skillId) 
                {
                    case Spearman.EVIL_EYE:
                        if (summon.SourceSkillId != Spearman.EVIL_EYE)
                            return;
                        if (DateTime.UtcNow.Subtract(summon.LastAbilityTime).TotalMilliseconds < effect.Info[CharacterSkillStat.x] * 1000)
                        {
                            c.CheatTracker.AddOffence(AntiCheat.OffenceType.NoDelaySummon);
                            return;
                        }
                        c.Account.Character.AddHP(effect.Info[CharacterSkillStat.hp]);
                        summon.LastAbilityTime = DateTime.UtcNow;
                        break;
                    case Berserker.HEX_OF_THE_EVIL_EYE:
                        if (summon.SourceSkillId != Spearman.EVIL_EYE)
                            return;
                        effect.ApplyBuffEffect(summon.Owner);
                        break;
                    default:
                        string txt = "Unhandled summon skill: " + skillId + " from summon skill: " + summon.SourceSkillId;
                        ServerConsole.Warning(txt);
                        Helpers.FileLogging.Log("Unhandled Summon Skills", txt);
                        break;
                }
                //c.SendPacket(Skill.ShowOwnSkillEffect(skillId, summon.SkillLevel));
                c.Account.Character.Map.BroadcastPacket(summon.GetUseSkillPacket(skillId, (byte)7), c.Account.Character); //stance ?                
            }
        }

        public static void HandleAttack(MapleClient c, PacketReader pr)
        {
            int objectId = pr.ReadInt();
            MapleCharacter chr = c.Account.Character;
            MapleSummon summon = chr.Map.GetSummon(objectId);
            if (summon != null)
            {
                if (summon.Owner.Id != chr.Id)
                    return;
                int tickCount = pr.ReadInt();
                WzCharacterSkill skillInfo = DataBuffer.GetCharacterSkillById(summon.SourceSkillId);
                if (skillInfo == null || skillInfo.SummonInfo == null)
                    return;
                WzCharacterSkill.SummonAttackInfo summonAttackInfo = skillInfo.SummonInfo;
                byte animation = pr.ReadByte();
                byte attackByte = pr.ReadByte();
                int attacks = (attackByte & 0x0F);
                int targets = ((attackByte >> 4) & 0x0F);
                if (summonAttackInfo.MobCount < targets || summonAttackInfo.AttackCount < attacks)
                {
                    ServerConsole.Warning("Player " + chr.Name + "'s summon: " + summon.SourceSkillId + "has mismatching targets- or attackcount: " + attacks + "/" + summonAttackInfo.AttackCount + " attacks, " + targets + "/" + summonAttackInfo.MobCount + " mobs");
                    return;
                }
                pr.Skip(12);
                List<AttackPair> attackList = new List<AttackPair>();
                for (int i = 0; i < targets; i++)
                {
                    int targetObjectId = pr.ReadInt();
                    MapleMonster target = chr.Map.GetMob(targetObjectId);
                    if (target == null)
                    {
                        ServerConsole.Debug("Error parsing summon attack, summon skillId: " + summon.SourceSkillId + " attack byte: " + attackByte);
                        return;
                    }
                    AttackPair ap = new AttackPair();
                    ap.TargetObjectId = targetObjectId;
                    pr.Skip(24);
                    int damage = pr.ReadInt(); //only supports 1 damage count, not sure if there are summons with attackcount > 1
                    ap.Damage.Add(damage);
                    attackList.Add(ap);
                    pr.Skip(4);
                }
                AttackInfo attackInfo = new AttackInfo();
                attackInfo.Attacks = attacks;
                attackInfo.AttacksByte = attackByte;
                attackInfo.Speed = animation;
                attackInfo.Targets = targets;
                attackInfo.TargetDamageList = attackList;
                foreach (AttackPair ap in attackList)
                {
                    MapleMonster mob = chr.Map.GetMob(ap.TargetObjectId);
                    if (mob != null)
                    {
                        long totalDamage = 0;
                        foreach (int i in ap.Damage)
                            totalDamage += i;
                        if (totalDamage > int.MaxValue)
                            totalDamage = int.MaxValue;
                        mob.Damage(chr, (int)totalDamage); 
                    }
                }
                bool darkFlare = summon.SourceSkillId == ChiefBandit.DARK_FLARE || summon.SourceSkillId == Hermit.DARK_FLARE || summon.SourceSkillId == NightWalker3.DARK_FLARE;
                chr.Map.BroadcastPacket(summon.GetAttackPacket(attackInfo, darkFlare));
            }
        }
    }
}
