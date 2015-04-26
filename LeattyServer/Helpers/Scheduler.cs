using LeattyServer.ServerInfo.Map.Monster;
using LeattyServer.ServerInfo.Player;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Timers;

namespace LeattyServer.Helpers
{
    static class Scheduler
    {
        /// <summary>
        /// Schedules an action to be invoked after a delay
        /// </summary>
        /// <param name="action">The action to be invoked</param>
        /// <param name="delay">The delay in milliseconds</param>
        public static Timer ScheduleDelayedAction(Action action, uint delay) 
        {           
            Timer timer = new Timer(delay);
            timer.Elapsed += (sender, e) => action.Invoke();
            timer.Elapsed += (sender, e) => DisposeTimer(timer);
            timer.Start();
            return timer;
        }

        public static Timer ScheduleRepeatingAction(Action action, uint intervalMS)
        {
            Timer timer = new Timer(intervalMS);
            timer.Elapsed += (sender, e) => action.Invoke();
            timer.Start();

            return timer;
        }

        /// <summary>
        /// Stops a timer and disposes it
        /// </summary>
        /// <param name="t">The timer to be disposed</param>
        public static void DisposeTimer(Timer t)
        {
            if (t == null) return;
            t.Stop();
            t.Dispose();
        }

        /// <summary>
        /// Cooltime in milliseconds
        /// </summary>
        /// <param name="source"></param>
        /// <param name="skillId"></param>
        /// <param name="delay"></param>
        public static Timer ScheduleRemoveCooldown(MapleCharacter source, int skillId, uint delay)
        {
            return Scheduler.ScheduleDelayedAction(new Action(() =>
            {
                if (source != null && source.Client != null)
                    source.RemoveCooldown(skillId);
            }), delay);
        }

        /// <summary>
        /// Cooltime in milliseconds
        /// </summary>
        /// <param name="source"></param>
        /// <param name="skillIds"></param>
        /// <param name="delay"></param>
        public static void ScheduleRemoveCooldowns(MapleCharacter source, List<int> skillIds, uint delay)
        {
            Scheduler.ScheduleDelayedAction(new Action(() =>
            {
                if (source != null && source.Client != null)
                {
                    foreach (int i in skillIds)
                        source.RemoveCooldown(i);
                }
            }), delay);
        }

        /// <summary>
        /// Duration in milliseconds
        /// </summary>
        /// <param name="source"></param>
        /// <param name="skillId"></param>
        /// <param name="delay"></param>
        /// <returns></returns>
        public static Timer ScheduleRemoveBuff(MapleCharacter source, int skillId, uint delay)
        {
            return Scheduler.ScheduleDelayedAction(new Action(() =>
            {
                if (source != null && source.Client != null)
                    source.CancelBuff(skillId);
            }), delay);
        }

        public static Timer ScheduleRemoveSummon(MapleCharacter source, int summonSkillId, uint delay)
        {
            return Scheduler.ScheduleDelayedAction(new Action(() =>
            {
                if (source != null && source.Client != null)
                    source.RemoveSummon(summonSkillId);
            }), delay);
        }

        public static Timer ScheduleRemoveMonsterStatusEffect(MonsterBuff effect, uint delay)
        {
            return Scheduler.ScheduleDelayedAction(new Action(() =>
            {
                effect.Dispose(false);
            }), delay);
        }
    }
}
