using LeattyServer.ServerInfo.Player;

namespace LeattyServer.ServerInfo.Packets.Handlers
{
    public class ProfessionReactorActionHandler
    {
        public static void Handle(MapleClient c, PacketReader pr)
        {
            //Todo: check if has Proffesionskill
            int ReactorId = pr.ReadInt();
            if (c.Account.Character.ReactorActionState != null)
            {
                c.Account.Character.ReactorActionState = null;
                return;
            }
            c.Account.Character.ReactorActionState = new ReactorActionState()
            {
                Hits = 0,
                ObjectId = ReactorId
            };
            SendActionResponse(c, ReactorId);
        }

        public static void HandleDestroy(MapleClient c, PacketReader pr)
        {
            int ReactorId = pr.ReadInt();
            MapleCharacter Chr = c.Account.Character;
            if (Chr.ReactorActionState == null) return;
            if (Chr.ReactorActionState.Hits > 3)
                Chr.Map.DestroyReactor(ReactorId);
        }

        private static void SendActionResponse(MapleClient c, int ObjectId)
        {
            PacketWriter pw = new PacketWriter();
            pw.WriteHeader(SendHeader.AcceptProffesionAction);
            pw.WriteInt(ObjectId);
            pw.WriteInt(0x0D); //Some kind of state?
            c.SendPacket(pw);
        }
    }
}
