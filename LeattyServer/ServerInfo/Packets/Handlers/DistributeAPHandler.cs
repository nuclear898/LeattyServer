using System.Collections.Generic;
using LeattyServer.Helpers;
using LeattyServer.ServerInfo.Player;

namespace LeattyServer.ServerInfo.Packets.Handlers
{
    class DistributeAPHandler
    {
        public static void HandleSingle(MapleClient c, PacketReader pr)
        {
            MapleCharacter chr = c.Account.Character;
            if (chr.AP <= 0)
                return;
            int tickCount = pr.ReadInt();
            MapleCharacterStat stat = (MapleCharacterStat)pr.ReadLong();

            SortedDictionary<MapleCharacterStat, long> statUpdates = new SortedDictionary<MapleCharacterStat, long>();

            switch (stat)
            {
                case MapleCharacterStat.Str:
                    if (chr.Str < 9999)
                    {
                        chr.Str++;
                        chr.AP--;
                        statUpdates.Add(MapleCharacterStat.Str, chr.Str);
                    }
                    break;
                case MapleCharacterStat.Dex:
                    if (chr.Dex < 9999)
                    {
                        chr.Dex++;
                        chr.AP--;
                        statUpdates.Add(MapleCharacterStat.Dex, chr.Dex);
                    }
                    break;
                case MapleCharacterStat.Int:
                    if (chr.Int < 9999)
                    {
                        chr.Int++;
                        chr.AP--;
                        statUpdates.Add(MapleCharacterStat.Int, chr.Int);
                    }
                    break;
                case MapleCharacterStat.Luk:
                    if (chr.Luk < 9999)
                    {
                        chr.Luk++;
                        chr.AP--;
                        statUpdates.Add(MapleCharacterStat.Luk, chr.Luk);
                    }
                    break;
                case MapleCharacterStat.MaxHp:
                    //TODO
                    break;
                case MapleCharacterStat.MaxMp:
                    //TODO
                    break;
                default:
                    ServerConsole.Warning("Unhandled stat in DistributeAPHandler: " + stat.ToString("X"));
                    break;

            }
            statUpdates.Add(MapleCharacterStat.Ap, chr.AP);
            MapleCharacter.UpdateStats(c, statUpdates, true);
        }

        public static void HandleDistribute(MapleClient c, PacketReader pr)
        {
            MapleCharacter chr = c.Account.Character;
            if (chr.AP == 0)
                return;
            int tickCount = pr.ReadInt();
            int statsCount = pr.ReadInt();

            Dictionary<MapleCharacterStat, long> statsAssign = new Dictionary<MapleCharacterStat, long>();

            for (int i = 0; i < statsCount; i++)
            {
                MapleCharacterStat stat = (MapleCharacterStat)pr.ReadLong();
                int addValue = pr.ReadInt();
                if (addValue > 0)
                    statsAssign.Add(stat, addValue);
            }
            SortedDictionary<MapleCharacterStat, long> statsUpdate = new SortedDictionary<MapleCharacterStat, long>();
            foreach (KeyValuePair<MapleCharacterStat, long> kvp in statsAssign)
            {
                if (chr.AP < kvp.Value)
                    continue;
                short statInc = (short)kvp.Value;
                switch (kvp.Key)
                {
                    case MapleCharacterStat.Str:
                        if (chr.Str + statInc <= 9999)
                        {
                            chr.Str += statInc;
                            chr.AP -= statInc;
                            statsUpdate.Add(MapleCharacterStat.Str, chr.Str);
                        }    
                        break;
                    case MapleCharacterStat.Dex:
                        if (chr.Dex + statInc <= 9999)
                        {
                            chr.Dex += statInc;
                            chr.AP -= statInc;
                            statsUpdate.Add(MapleCharacterStat.Dex, chr.Dex);
                        }
                        break;
                    case MapleCharacterStat.Int:
                        if (chr.Int + statInc <= 9999)
                        {
                            chr.Int += statInc;
                            chr.AP -= statInc;
                            statsUpdate.Add(MapleCharacterStat.Int, chr.Int);
                        }
                        break;
                    case MapleCharacterStat.Luk:
                        if (chr.Luk + statInc <= 9999)
                        {
                            chr.Luk += statInc;
                            chr.AP -= statInc;
                            statsUpdate.Add(MapleCharacterStat.Luk, chr.Luk);
                        }
                        break;
                    default:
                        ServerConsole.Error("Unhandled stat in AutoAssignAp handling: " + kvp.Key);
                        break;
                }
            }
            statsUpdate.Add(MapleCharacterStat.Ap, chr.AP);
            MapleCharacter.UpdateStats(c, statsUpdate, true);
        }
    }
}
