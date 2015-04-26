using LeattyServer.Constants;
using LeattyServer.Data;
using LeattyServer.Data.WZ;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LeattyServer.ServerInfo.Packets;

namespace LeattyServer.ServerInfo.Player.ResourceSystems
{
    class LuminousSystem : ResourceSystem
    {
        public int DarkLevel { get; set; }
        public int LightLevel { get; set; }
        public int DarkGauge { get; set; }
        public int LightGauge { get; set; }
        public LuminousState State { get; set; } //0 = nothing, 1 = light, 2 = dark

        public LuminousSystem()
            : base(ResourceSystemType.Luminous)
        {
            DarkLevel = 1;
            LightLevel = 1;
            DarkGauge = 0;
            LightGauge = 0;
        }

        public PacketWriter Update()
        {
            PacketWriter pw = new PacketWriter();
            pw.WriteHeader(SendHeader.LuminousGauge);

            pw.WriteInt(DarkGauge);
            pw.WriteInt(LightGauge);
            pw.WriteInt(DarkLevel);
            pw.WriteInt(LightLevel);
            pw.WriteInt(0);
            return pw;
        }

        public void IncreaseLightGauge(int amount, MapleCharacter chr)
        {
            if (State == LuminousState.None)
            {
                WzCharacterSkill skill = DataBuffer.GetCharacterSkillById(LuminousBasics.SUNFIRE);
                skill.GetEffect(1).ApplyBuffEffect(chr);
                chr.AddCooldown(LuminousBasics.SUNFIRE, 180000);
                State = LuminousState.Light;
            }
            if (LightLevel < 5)
            {
                LightGauge += amount;
                if (LightGauge > 10000)
                {
                    LightGauge = 0;
                    LightLevel++;
                }
            }
        }

        public void IncreaseDarkGauge(int amount, MapleCharacter chr)
        {
            if (State == LuminousState.None)
            {
                DataBuffer.GetCharacterSkillById(LuminousBasics.ECLIPSE).GetEffect(1).ApplyBuffEffect(chr);
                chr.AddCooldown(LuminousBasics.ECLIPSE, 180000);
                State = LuminousState.Dark;
            }
            if (DarkLevel < 5)
            {
                DarkGauge += amount;
                if (DarkGauge > 10000)
                {
                    DarkGauge = 0;
                    DarkLevel++;
                }
            }
        }

        public static void HandleGaugeGain(MapleCharacter chr, int skillId, int gaugeInc)
        {
            LuminousSystem resource = (LuminousSystem)chr.Resource;
            LuminousState state = SkillConstants.GetLuminousSkillState(skillId);
            if (state == LuminousState.Light)
                resource.IncreaseLightGauge(gaugeInc, chr);
            else
                resource.IncreaseDarkGauge(gaugeInc, chr);
            chr.Client.SendPacket(resource.Update());
        }

        public static void HandleChangeDarkLight(MapleCharacter chr, int sourceSkillId)
        {
            LuminousSystem resource = (LuminousSystem)chr.Resource;
            if (sourceSkillId == LuminousBasics.SUNFIRE)
            {                
                if (resource.LightLevel > 0)
                {
                    chr.CancelBuffSilent(LuminousBasics.ECLIPSE);
                    DataBuffer.CharacterSkillBuffer[LuminousBasics.SUNFIRE].GetEffect(1).ApplyBuffEffect(chr);
                    resource.LightLevel--;
                    resource.State = LuminousState.Light;
                }
            }
            else if (sourceSkillId == LuminousBasics.ECLIPSE)
            {
                if (resource.DarkLevel > 0)
                {
                    chr.CancelBuffSilent(LuminousBasics.SUNFIRE);
                    DataBuffer.CharacterSkillBuffer[LuminousBasics.ECLIPSE].GetEffect(1).ApplyBuffEffect(chr);
                    resource.DarkLevel--;
                    resource.State = LuminousState.Dark;
                }
            }           
            chr.AddCooldown(LuminousBasics.SUNFIRE, 180000);
            chr.AddCooldown(LuminousBasics.ECLIPSE, 180000);
            chr.AddCooldown(LuminousBasics.CHANGE_LIGHT_DARK_MODE, 180000);           
        }
    }

    public enum LuminousState
    {
        None = 0,
        Light = 1,
        Dark = 2,
        Equilibrium = 3
    }
}
