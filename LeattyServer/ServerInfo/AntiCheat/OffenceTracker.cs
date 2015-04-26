using LeattyServer.Constants;
using LeattyServer.Helpers;
using LeattyServer.ServerInfo.Map;
using LeattyServer.ServerInfo.Map.Monster;
using LeattyServer.ServerInfo.Player;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LeattyServer.ServerInfo.AntiCheat
{
    public class OffenceTracker
    {
        public MapleClient Client;

        private ExpiringDictionary<OffenceType, int> OffenceList = new ExpiringDictionary<OffenceType, int>(new TimeSpan(0, 10, 0));
        private Dictionary<OffenceType, OffenceValue> OffenceValues = new Dictionary<OffenceType, OffenceValue>();
        private Dictionary<TriggerType, long> Trackers = new Dictionary<TriggerType, long>();

        private int KillCountTracker;
        private long KillTracker;

        private Point? FirstMobPoint;

        public OffenceTracker()
        {
            String[] Names = Enum.GetNames(typeof(OffenceType));
            foreach (String Name in Names)
            {
                OffenceValue Value = (OffenceValue)Enum.Parse(typeof(OffenceValue), Name, true);
                OffenceType Offence = (OffenceType)Enum.Parse(typeof(OffenceType), Name, true);
                OffenceValues.Add(Offence, Value);
            }
        }

        public void AddOffence(OffenceType offence)
        {
            AddOffence(offence, OffenceValues[offence]);
        }

        private void AddOffence(OffenceType offence, OffenceValue value)
        {
            if (TotalOffenceValue() >= ServerConstants.MaxOffenceValue)
            {
                //Todo add ban/kick or whatever
                ServerConsole.Warning("Client found with high offence values: " + Enum.GetName(typeof(OffenceType), offence));
            }
            
            if (OffenceList.ContainsKey(offence))
                OffenceList[offence] += (int)value;
                else
                    OffenceList.Add(offence, (int)value);
            }

        public int TotalOffenceValue()
        {
            return OffenceList.Values.Sum(x => (Int32)x);
        }

        public void Trigger(TriggerType triggerType)
        {
            long currentMillis = DateTime.UtcNow.Ticks / TimeSpan.TicksPerMillisecond;
            bool triggerExists = Trackers.ContainsKey(triggerType);
            if (!triggerExists) 
                Trackers.Add(triggerType, currentMillis);
            if (triggerExists && (currentMillis - Trackers[triggerType]) < 50)
                AddOffence(OffenceType.NoDelay);
            switch (triggerType)
        {
                case TriggerType.Attack:
                    CheckForVac(Client.Account.Character);
                    break;
                default:
                    break;
            }
            Trackers[triggerType] = currentMillis;
        }

        private void CheckForVac(MapleCharacter c)
        {
            FirstMobPoint = c.Map.CheckMobPositions(this);
            if (FirstMobPoint != null && KillTracker == 0)
            {
                KillCountTracker = 0;
                KillTracker = DateTime.UtcNow.Ticks / TimeSpan.TicksPerMillisecond;
            }
        }

        public void KillTrigger(MapleMonster mob)
        {
            Boolean ForceReturn = false;
            if (FirstMobPoint == null)
            {
                FirstMobPoint = mob.Position;
                ForceReturn = true;
            }
            if (KillTracker == 0)
            {
                KillCountTracker = 0;
                KillTracker = DateTime.UtcNow.Ticks / TimeSpan.TicksPerMillisecond;
                ForceReturn = true;
            }
            if (ForceReturn)
                return;

            TimeSpan Current = TimeSpan.FromMilliseconds(DateTime.UtcNow.Ticks / TimeSpan.TicksPerMillisecond);
            if ((Current - TimeSpan.FromMilliseconds(KillTracker)).TotalMinutes >= 1)
            {
                KillTriggerReset();
                return;
            }
            if (Functions.Distance((Point)FirstMobPoint, mob.Position) < 5)
            {
                KillCountTracker++;
            }
            //50 kills on basicly the same position (~4px)
            if (KillCountTracker > 50)
            {
                AddOffence(OffenceType.MobVac);
                KillTriggerReset();
            }
        }

        private void KillTriggerReset()
        {
            KillCountTracker = 0;
            KillTracker = 0;
            FirstMobPoint = null;
        }
    }


    /// <summary>
    /// Values for tracking offences 
    /// </summary>
    public enum OffenceValue : int
    {
        AbnormalValues = 50,
        PacketEdit = 100,
        NoDelay = 10,
        PosibleVac = 1,
        MobVac = 500,
        NoDelaySummon = 10,
        LootFarAwayItem = 10
    }

    /// <summary>
    /// An Id for tracking the type of offence
    /// </summary>
    public enum OffenceType : byte
    {
        AbnormalValues,
        PacketEdit,
        NoDelay,
        PosibleVac,
        MobVac,
        NoDelaySummon,
        LootFarAwayItem
    }

    /// <summary>
    /// Ids for types of triggers
    /// </summary>
    public enum TriggerType : byte
    {
        RegenerateHP = 0x0,
        RegenerateMP = 0x1,
        Attack = 0x2,
    }
}
