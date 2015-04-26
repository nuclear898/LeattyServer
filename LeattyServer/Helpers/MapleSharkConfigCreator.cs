using LeattyServer.Constants;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LeattyServer.ServerInfo.Packets;

namespace LeattyServer.Helpers
{
    //Uses the server's opcode names to create a config file for naming all the opcodes in maple shark
    class MapleSharkConfigCreator
    {
        public static void GenerateConfigFile()
        {
            if (!Directory.Exists("./MapleSharkConfig"))
            {
                Directory.CreateDirectory("./MapleSharkConfig/");
            }
            File.WriteAllText("./MapleSharkConfig/PacketDefinitions.xml", "<ArrayOfDefinition xmlns:xsi=\"http://www.w3.org/2001/XMLSchema-instance\" xmlns:xsd=\"http://www.w3.org/2001/XMLSchema\">\n");
            using (StreamWriter file = File.AppendText("./MapleSharkConfig/PacketDefinitions.xml"))
            {
                var recvOps = Enum.GetValues(typeof(RecvHeader)).Cast<short>();
                foreach (short op in recvOps)
                {
                    file.WriteLine("        <Definition>");
                    file.WriteLine("            <Build>" + ServerConstants.Version + "</Build>\n            <Locale>8</Locale>");
                    file.WriteLine("            <Outbound>true</Outbound>");
                    file.WriteLine("            <Opcode>" + op + "</Opcode>");
                    file.WriteLine("            <Name>" + Enum.GetName(typeof(RecvHeader), op) + "</Name>");
                    file.WriteLine("            <Ignore>false</Ignore>");
                    file.WriteLine("        </Definition>");
                }
                var sendOps = Enum.GetValues(typeof(SendHeader)).Cast<short>();
                foreach (short op in sendOps)
                {                    
                    file.WriteLine("        <Definition>");
                    file.WriteLine("            <Build>" + ServerConstants.Version + "</Build>\n            <Locale>8</Locale>");
                    file.WriteLine("            <Outbound>false</Outbound>");
                    file.WriteLine("            <Opcode>" + op + "</Opcode>");
                    file.WriteLine("            <Name>" + Enum.GetName(typeof(SendHeader), op) + "</Name>");
                    file.WriteLine("            <Ignore>false</Ignore>");
                    file.WriteLine("        </Definition>");
                }

                file.Write("</ArrayOfDefinition>");
                ServerConsole.Info("Finished creating MapleShark config file.");
            }
        }
    }
}
