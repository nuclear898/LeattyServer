using System;
using System.Timers;

namespace LeattyServer.ServerInfo.Player
{
    class Cooldown
    {        
        public uint Duration { get; private set; }
        public DateTime StartTime { get; private set; }
        public Timer CancelSchedule { get; set; }

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="skillId">The id of the skill that is on cooldown</param>
        /// <param name="duration">The duration of the cooldown in milliseconds</param>
        /// <param name="startTime">The timestamp of when the cooldown was triggered</param>
        public Cooldown(uint duration, DateTime startTime)
        {           
            Duration = duration;
            StartTime = startTime;
        }
    }
}