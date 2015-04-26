using System;
using System.Linq;
using LeattyServer.ServerInfo.Player;

namespace LeattyServer.ServerInfo.Packets.Handlers
{
	public static class RecommendedPartyMembersHandler
	{
	    private const int MAX_LEVEL_DIFFERENCE = 10;
		public static void Handle(MapleClient c, PacketReader pr)
		{
		    MapleCharacter chr = c.Account.Character;
		    if (chr.Party?.LeaderId != chr.Id) return;
		    var availableCharacters = c.Account.Character.Map.GetCharacters().Where(x => !x.Hidden && x.Party == null && Math.Abs(chr.Level - x.Level) <= MAX_LEVEL_DIFFERENCE);
		    int count = availableCharacters.Count();
            if (count > 50) return;

            //[01] [46 7B 3C 00] [06 00 4B 61 7A 72 6F 6C] [90 01] [00 00] [15]
            PacketWriter pw = new PacketWriter(SendHeader.RecommendedPartyMembers);
			pw.WriteByte((byte)count);
		    foreach (MapleCharacter recommendedChr in availableCharacters)
		    {
		        pw.WriteInt(recommendedChr.Id);
				pw.WriteMapleString(recommendedChr.Name);
				pw.WriteShort(recommendedChr.Job);
				pw.WriteShort(recommendedChr.SubJob);
				pw.WriteByte(recommendedChr.Level);
		    }
			c.SendPacket(pw);
		}
    }
}