using System;
using System.Collections.Generic;
using LeattyServer.Constants;
using LeattyServer.Helpers;
using LeattyServer.Scripting;
using LeattyServer.ServerInfo.Map;
using LeattyServer.ServerInfo.Packets;
using LeattyServer.ServerInfo.Player;

namespace LeattyServer.Data.Scripts
{
    public class NpcEngine
    {
        private MapleClient Client;
        public int NpcId;
        public NpcScript ScriptInstance;
        public bool IsShop = false;

        public NpcEngine(MapleClient c, int id)
        {
            Client = c;
            NpcId = id;
            Type npcT;
            if (DataBuffer.NpcScripts.TryGetValue(NpcId, out npcT) && npcT != null)
            {                
                ScriptInstance = ScriptActivator.CreateScriptInstance(npcT, npcT.ToString(), c.Account.Character) as NpcScript;
                if (ScriptInstance == null)
                {
                    string error = string.Format("Error loading {0} {1}", "NpcScript", npcT.ToString());
                    ServerConsole.Error(error);
                    FileLogging.Log("Npc scripts", error);
                    return;
                }
                if (ScriptInstance.IsShop)
                {
                    IsShop = true;
                }
                else
                {
                    ScriptInstance.SendOk = new Action<string>(SendOk);
                    ScriptInstance.SendNext = new Action<string>(SendNext);
                    ScriptInstance.SendPrev = new Action<string>(SendPrev);
                    ScriptInstance.SendNextPrev = new Action<string>(SendNextPrev);
                    ScriptInstance.SendSimple = new Action<string>(SendSimple);
                    ScriptInstance.EndChat = new Action(Dispose);
                    ScriptInstance.SendYesNo = new Action<string>(SendYesNo);
                    ScriptInstance.SendAskText = new Action<string, int, int, string>(SendAskText);
                }     
            }
            else
            {
                SendOk(string.Format(@"An error has occured in my script. Please report this as a bug\r\nNpcId: {0}", NpcId));
                ServerConsole.Debug(string.Format(@"Missing script for NPC: {0}", NpcId));
                ScriptInstance = null;
                c.Account.Character.EnableActions();
            }
        }

        public void Dispose()
        {
            if (Client.NpcEngine == this)
                Client.NpcEngine = null;
            Client.Account.Character.EnableActions();
            Client = null;
            ScriptInstance = null;
        }

        public bool ExecuteScriptForNpc()
        {
            if (ScriptInstance == null) return false;
            try
            {
                if (IsShop)
                {
                    Client.SendPacket(Packets.ShowShop((ShopScript)ScriptInstance, NpcId));
                }
                ScriptInstance.Execute();
                return true;
            }
            catch (Exception ex)
            {
                string errorString = string.Format("NpcId: {0}\r\nState: {1}\r\n Selection: {2}\r\nException: {3}", NpcId, ScriptInstance.State, ScriptInstance.Selection, ex);
                ServerConsole.Debug("Npc script execution error: " + errorString);
                FileLogging.Log("Npc scripts", errorString);
                SendOk("An error occured, please report this on the forums\r\nNpcId: " + NpcId);
                Dispose();
                return false;
            }
        }

        public static void OpenNpc(int npcId, int objectId, MapleClient c)
        {
            c.NpcEngine = new NpcEngine(c, npcId);
            if (c.NpcEngine.ScriptInstance == null)
                return;
            c.NpcEngine.ScriptInstance.State = 0;
            c.NpcEngine.ScriptInstance.Selection = -1;
            c.NpcEngine.ScriptInstance.ObjectId = objectId;            
            c.NpcEngine.ExecuteScriptForNpc();
        }

        #region Messages
        private void SendOk(string text)
        {
            Client.SendPacket(MapleNpc.GetNpcTalk(NpcId, 0, MapleNpc.ChatType.None, text));
        }

        private void SendNext(string text)
        {
            Client.SendPacket(MapleNpc.GetNpcTalk(NpcId, 0, MapleNpc.ChatType.None, text, next: true));
        }

        private void SendPrev(string text)
        {
            Client.SendPacket(MapleNpc.GetNpcTalk(NpcId, 0, MapleNpc.ChatType.None, text, prev: true));
        }

        private void SendNextPrev(string text)
        {            
            Client.SendPacket(MapleNpc.GetNpcTalk(NpcId, 0, MapleNpc.ChatType.None, text, next: true, prev: true));            
        }

        private void SendYesNo(string text)
        {          
            Client.SendPacket(MapleNpc.GetNpcTalk(NpcId, 2, MapleNpc.ChatType.None, text));           
        }

        private void SendSimple(string text)
        {
            Client.SendPacket(MapleNpc.GetNpcTalk(NpcId, 5, MapleNpc.ChatType.None, text));
        }

        private void SendGetNumber(string text, int defaultValue, int minValue, int maxValue)
        {           
            Client.SendPacket(MapleNpc.GetNpcTalkNum(NpcId, text, defaultValue, minValue, maxValue));
        }

        private void SendAskText(string text, int min, int max, string textboxText = "")
        {
            Client.SendPacket(MapleNpc.GetNpcTalkAskText(NpcId, text, min, max, textboxText));
        }

        public void BuyItem(int purchaseId, short index, short quantity)
        {
            ScriptInstance.State = 1;
            ScriptInstance.Selection = index;
            ShopScript shopScript = (ShopScript)ScriptInstance;
            var shopItems = shopScript.ShopItems;
            if (shopItems != null)
            {
                if (index >= 0 && index < shopItems.Count)
                {
                    ShopItem item = shopItems[index];
                    if (item.Id == purchaseId)
                    {
                        byte response = shopScript.Character.BuyItem(item, quantity);
                        Client.SendPacket(Packets.ShopTransactionResponse(response));
                    }
                }
            }           
        }
        #endregion

        public static class Packets
        {
            public static PacketWriter ShopTransactionResponse(byte response)
            {
                PacketWriter pw = new PacketWriter();
                pw.WriteHeader(SendHeader.NpcTransaction);
                pw.WriteByte(response);
                pw.WriteByte(0);
                pw.WriteByte(0);
                return pw;
            }

            public static PacketWriter ShowShop(ShopScript shop, int npcId)
            {
                List<ShopItem> items = shop.ShopItems;
                PacketWriter pw = new PacketWriter(SendHeader.NpcShop);
                pw.WriteByte(0);
                pw.WriteInt(0);
                pw.WriteInt(npcId);
                pw.WriteByte(0);
                pw.WriteShort((short)items.Count);

                foreach (ShopItem item in items)
                {
                    pw.WriteInt(item.Id);
                    pw.WriteInt(item.Price);
                    pw.WriteByte(0);
                    pw.WriteInt(item.ReqItemId);
                    pw.WriteInt(item.ReqItemQuantity);
                    pw.WriteInt(0);//unk
                    pw.WriteInt(0);//unk
                    pw.WriteInt(0);//unk
                    pw.WriteZeroBytes(22); //v158
                    pw.WriteLong(MapleFormatHelper.GetMapleTimeStamp(-2));
                    pw.WriteLong(MapleFormatHelper.GetMapleTimeStamp(-1));
                    pw.WriteInt(item.Tab);
                    pw.WriteByte(0);
                    pw.WriteByte(0);
                    pw.WriteInt(0);
                    pw.WriteZeroBytes(3); //v158
                    MapleItemType itemType = ItemConstants.GetMapleItemType(item.Id);
                    if (itemType == MapleItemType.Bullet || itemType == MapleItemType.ThrowingStar)
                    {
                        pw.WriteZeroBytes(6);
                        pw.WriteShort((short)(BitConverter.DoubleToInt64Bits((double)item.Price) >> 48));
                        pw.WriteShort(item.BulletCount); //Todo, correct max amount
                    }
                    else
                    {
                        pw.WriteShort((short)item.DefaultQuantity);
                        pw.WriteShort((short)item.MaximumPurchase);
                    }
                    pw.WriteByte(0);
                    pw.WriteInt(0);
                    pw.WriteInt(0);
                    pw.WriteInt(0);
                    pw.WriteInt(0);

                    pw.WriteLong(9410165);
                    pw.WriteLong(9410166);
                    pw.WriteLong(9410167);
                    pw.WriteLong(9410168);
                    //pw.WriteBytes(Functions.HexToBytes("75968F000000000076968F000000000077968F000000000078968F0000000000"));//no idea
                }
                return pw;
            }
        }
    }   
}
