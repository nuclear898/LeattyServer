using LeattyServer.ServerInfo.Player;

namespace LeattyServer.ServerInfo.Packets.Handlers
{
	public static class WeeklyMapleStarHandler
	{
		public static void Handle(MapleClient c, PacketReader pr)
		{
		    byte type = pr.ReadByte();

		    switch (type)
		    {
                case 0x07:
		        {
					c.SendPacket(ResponsePacket(c.Account.Character, c.Account.Character));
		            break;
		        }
		    }
		}

	    private static PacketWriter ResponsePacket(MapleCharacter chr1, MapleCharacter chr2)
	    {
	        PacketWriter pw = new PacketWriter(SendHeader.WeeklyMapleStar);
			pw.WriteByte(0x7);
			//pw.WriteShort(0);
			pw.WriteBool(chr1 != null);
	        if (chr1 != null)
	        {
	            pw.WriteInt(chr1.Id);
	            pw.WriteInt(1); //Heart amount
	            pw.WriteInt(0); //10 0D 4A F5 
	            pw.WriteInt(0); //50 3A D0 01 
	            pw.WriteMapleString(chr1.Name);
	            MapleCharacter.AddCharLook(pw, chr1, false);
	        }
            pw.WriteBool(chr2 != null);
            if (chr2 != null)
            {
                pw.WriteInt(chr2.Id);
                pw.WriteInt(1); //Heart amount
                pw.WriteInt(0); //10 0D 4A F5 
                pw.WriteInt(0); //50 3A D0 01 
                pw.WriteMapleString(chr2.Name);
                MapleCharacter.AddCharLook(pw, chr2, false);
            }
	        return pw;
	    } 
	}
}