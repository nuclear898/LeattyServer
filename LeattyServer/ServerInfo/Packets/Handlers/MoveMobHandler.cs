using System;
using System.Collections.Generic;
using System.Drawing;
using LeattyServer.Data.WZ;
using LeattyServer.Helpers;
using LeattyServer.ServerInfo.Map.Monster;
using LeattyServer.ServerInfo.Movement;
using LeattyServer.ServerInfo.Player;

namespace LeattyServer.ServerInfo.Packets.Handlers
{
    class MoveMobHandler
    {
        public static void Handle(MapleClient c, PacketReader pr)
        {
            if (c.Account.Character.Map != null)
            {
                int objectId = pr.ReadInt();
                MapleMonster Mob = c.Account.Character.Map.GetMob(objectId);
                if (Mob == null) return;
                lock (Mob.MobLock)
                {
                    if (!Mob.Alive) 
                        return;
                    if (Mob.GetController() != c.Account.Character)
                    {
                        c.SendPacket(MapleMonster.RemoveMobControl(objectId));
                        return;  
                    }
                    pr.Skip(1);
                    short moveID = pr.ReadShort();
                    bool useSkill = pr.ReadBool();
                    byte skill = pr.ReadByte();
                    int unk = pr.ReadInt();
                    Point startPos = Mob.Position;
                    int skillid = 0;
                    int skilllevel = 0;
                    if (useSkill)
                    {
                        if (Mob.WzInfo.Skills.Count > 0)
                        {
                            if (skill >= 0 && skill < Mob.WzInfo.Skills.Count)
                            {
                                MobSkill mobSkill = Mob.WzInfo.Skills[skill];
                                if ((DateTime.UtcNow - Mob.SkillTimes[mobSkill]).TotalMilliseconds > mobSkill.interval)// && mobSkill.summonOnce
                                {
                                    Mob.SkillTimes[mobSkill] = DateTime.UtcNow;
                                    if (mobSkill.hp <= Mob.HpPercent)
                                    {
                                        //supposed to apply efffect here :/
                                        //todo $$
                                        //mobSkill.applyEffect(chr, monster, true);
                                        skillid = mobSkill.Skill;
                                        skilllevel = mobSkill.Level;
                                    }
                                }
                            }
                            else
                            {
                                return;//hacking?
                            }

                        }
                    }
                    List<int> unkList = new List<int>();
                    List<short> unkList2 = new List<short>();
                    byte count = pr.ReadByte();
                    for (int i = 0; i < count; i++)
                    {
                        unkList.Add(pr.ReadInt());
                    }
                    count = pr.ReadByte();
                    for (int i = 0; i < count; i++)
                    {
                        unkList.Add(pr.ReadShort());
                    }

                    pr.Skip(30);
                    List<MapleMovementFragment> Res = ParseMovement.Parse(pr);
                    if (Res != null && Res.Count > 0)
                    {
                        updatePosition(Res, Mob, -1);
                        if (Mob.Alive)
                        {
                            MoveResponse(c, objectId, moveID, Mob.ControllerHasAggro, (short)Mob.WzInfo.MP, 0, 0);
                            MoveMob(c, objectId, useSkill, skill, unk, startPos, Res, unkList, unkList2);
                        }
                    }
                    else
                    {
                        ServerConsole.Warning("Monster Res == null or empty!");
                    }
                }
            }
        }
        public static void MoveMob(MapleClient c, int objectId, bool useSkill, byte skillIndex,int unk, Point startPosition, List<MapleMovementFragment> movement, List<int> unkList, List<short> unkList2)
        {
            PacketWriter pw = new PacketWriter();
            pw.WriteHeader(SendHeader.MoveMonster);
            pw.WriteInt(objectId);
            pw.WriteBool(useSkill);
            pw.WriteByte(skillIndex);
            pw.WriteInt(unk);
            pw.WriteByte((byte)unkList.Count);
            foreach (int i in unkList)
                pw.WriteInt(i);
            pw.WriteByte((byte)unkList2.Count);
            foreach (short i in unkList2)
                pw.WriteShort(i);

            pw.WriteInt(0);
            pw.WritePoint(startPosition);
            pw.WriteInt(0);

            MapleMovementFragment.WriteMovementList(pw, movement);
            pw.WriteByte(0);

            c.Account.Character.Map.BroadcastPacket(pw, c.Account.Character, false);
        }
        public static void MoveResponse(MapleClient c, int objectId, short moveId, bool Aggro, int Mp, byte skillId, byte skillLevel)
        {
            PacketWriter pw = new PacketWriter();
            pw.WriteHeader(SendHeader.MoveMonsterResponse);
            pw.WriteInt(objectId);
            pw.WriteShort(moveId);
            pw.WriteBool(Aggro);
            pw.WriteInt(Mp);
            pw.WriteByte(skillId);
            pw.WriteByte(skillLevel);
            pw.WriteInt(0);

            c.Account.Character.Map.BroadcastPacket(pw, c.Account.Character, true);
        }

        public static void updatePosition(List<MapleMovementFragment> movements, MapleMonster mob, int yOffset)
        {
            if (movements == null)            
                return;
            
            for (int i = movements.Count - 1; i >= 0; i--) //loop from the back
            {
                if (movements[i] is AbsoluteLifeMovement)
                {
                    //ServerConsole.Info("Monster position updated :" + Mob.Position + " => " + Move.Position);
                    Point position = movements[i].Position;
                    position.Y += yOffset;
                    mob.Position = position;
                    mob.Stance = movements[i].State;
                    break;
                }
            }            
        }
    }
}
