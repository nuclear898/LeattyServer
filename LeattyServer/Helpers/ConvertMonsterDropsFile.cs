using LeattyServer.Data;
using LeattyServer.Data.WZ;
using LeattyServer.ServerInfo.Inventory;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LeattyServer.Helpers
{
    public static class ConvertMonsterDropsFile
    {
        public static void Convert()
        {
            string[] lines = File.ReadAllLines(@".\CustomData\drop_data_v144_3.txt");
            Dictionary<int, List<TempMobDrop>> drops = new Dictionary<int, List<TempMobDrop>>();
            for (int i = 0; i < lines.Length; i++)
            {
                string[] split = lines[i].Split(' ');
                TempMobDrop drop = new TempMobDrop();                
                int mobId = int.Parse(split[0]);
                drop.itemId = int.Parse(split[1]);
                drop.min = int.Parse(split[2]);
                drop.max = int.Parse(split[3]);
                drop.questId = int.Parse(split[4]);
                drop.dropChance = int.Parse(split[5]);
                WzItem item = DataBuffer.GetItemById(drop.itemId);
                if (item != null)
                {
                    if (!item.IsQuestItem)
                    {
                        if (!drops.ContainsKey(mobId))
                        {
                            drops.Add(mobId, new List<TempMobDrop>() { drop });
                        }
                        else
                            drops[mobId].Add(drop);
                    }
                }                
            }
            StringBuilder sb = new StringBuilder();
            drops = drops.OrderBy(x => x.Key).ToDictionary(x => x.Key, x => x.Value);
            foreach (var kvp in drops)
            {
                //4031161 1000000 1 1 1008
                sb.Append('#');
                sb.AppendLine(kvp.Key.ToString());
                foreach(TempMobDrop drop in kvp.Value) 
                {
                    sb.Append(drop.itemId + " ");
                    sb.Append(drop.dropChance + " ");
                    sb.Append(drop.min + " ");
                    sb.Append(drop.max + " ");
                    sb.AppendLine(drop.questId.ToString());
                }
                sb.AppendLine();

            }
            File.WriteAllText(@".\CustomData\MonsterDrops.txt", sb.ToString());
                
        }
    }

    class TempMobDrop
    {
        public int itemId;
        public int dropChance;
        public int min;
        public int max;
        public int questId;
    }
}
