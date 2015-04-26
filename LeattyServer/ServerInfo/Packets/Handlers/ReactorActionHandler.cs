using LeattyServer.ServerInfo.Player;

namespace LeattyServer.ServerInfo.Packets.Handlers
{
    public class ReactorActionHandler
    {
        public static void Handle(MapleClient c, PacketReader pr)
        {
            int ReactorId = pr.ReadInt();
            MapleCharacter Chr = c.Account.Character;
            if (Chr.ReactorActionState == null)
                Chr.ReactorActionState = new ReactorActionState()
                {
                    Hits = 1,
                    ObjectId = ReactorId
                };

            if (Chr.ReactorActionState.ObjectId != ReactorId) return;

            if (Chr.ReactorActionState.Hits > 3)
            {
                Chr.Map.DestroyReactor(ReactorId);
                return;
            }

            Chr.ReactorActionState.Hits += 1;
            SendActionResponse(c, Chr.ReactorActionState);
        }

        private static void SendActionResponse(MapleClient c, ReactorActionState State)
        {
            PacketWriter pw = new PacketWriter();
            pw.WriteHeader(SendHeader.DamageReactor);
            pw.WriteInt(State.ObjectId);
            pw.WriteByte(State.Hits);
            pw.WritePoint(c.Account.Character.Map.GetReactorPos(State.ObjectId));
            pw.WriteHexString("00 00 00 04"); //Unk
            c.SendPacket(pw);
        }
    }
}
