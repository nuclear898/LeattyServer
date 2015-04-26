using LeattyServer.Constants;
using LeattyServer.Helpers;
using LeattyServer.ServerInfo.Packets.Handlers;
using LeattyServer.ServerInfo.Player;

namespace LeattyServer.ServerInfo.Packets
{
    class RecvPacketHandler
    {
        public static void Handle(PacketReader packet, MapleClient c)
        {
            if (packet.Length >= 2)
            {                
                if (ServerConstants.PrintPackets)
                    ServerConsole.Info("Receiving: {0}", Functions.ByteArrayToStr(packet.ToArray()));
                RecvHeader header = (RecvHeader)packet.ReadHeader();

                if (header <= RecvHeader.ErrorCode) {
                    switch (header)
                    {           
                        #region Miscellaneous
                        case RecvHeader.ErrorCode:
                            ErrorCodeHandler.Handle(c, packet);
                            break;
                        case RecvHeader.BlackCipher:
                            BlackCipherHandler.Handle(packet.ReadInt(), c);
                            break;
                        case RecvHeader.HandShake:
                            ReceiveHandShakeHandler.Handle(c, packet);
                            break;
                        case RecvHeader.CrashReport:
                            break;
                        case RecvHeader.Pong:
                            PongHandler.Handle(c);
                            break;
                        #endregion
                        #region Login Server
                        case RecvHeader.EnteredLoginScreen:
                            EnteredLoginScreenHandler.Handle(c);
                            break;                            
                        case RecvHeader.ClientLoaded:
                            ClientLoadedHandler.Handle(c);
                            break;
                        case RecvHeader.ShowServerList:
                        case RecvHeader.ReShowServerList:
                            ServerlistRequestHandler.Handle(c);
                            break;
                        case RecvHeader.WorldSelect:
                            WorldSelectHandler.Handle(packet.ReadShort(), c);
                            break;
                        case RecvHeader.CheckCharacterName:
                            CheckCharnameHandler.Handle(c, packet.ReadMapleString());
                            break;
                        case RecvHeader.CreateCharacter:
                            CreateCharHandler.Handle(c, packet);
                            break;
                        case RecvHeader.DeleteCharacter:
                            DeleteCharacterHandler.Handle(c, packet);
                            break;
                        case RecvHeader.SetAccountPic:
                            SetAccountPicHandler.Handle(c, packet);
                            break;
                        case RecvHeader.ChooseCharacterWithPic:
                            ChooseCharWithPicHandler.Handle(c, packet);
                            break;
                        case RecvHeader.EnterMap:
                            CharacterLoginHandler.Handle(c, packet);
                            break;
                        case RecvHeader.AccountLogin:
                            LoginAccountHandler.Handle(c, packet);
                            break;
                        case RecvHeader.ChooseChannel:
                            ChooseChannelHandler.Handle(c, packet);
                            break;
                        #endregion
                        default:
#if DEBUG
                            ServerConsole.Debug("Unhandled recv packet: {0}", Functions.ByteArrayToStr(packet.ToArray()));
#endif
                            break;
                    }
                }
                else 
                {
                    if (c.Account?.Character?.Map == null) return;
                    switch (header)
                    {
                        #region GameServer
                        // Spam packets:
                        case RecvHeader.ClickDialog:
                        case RecvHeader.AttackSpam:
                        case RecvHeader.FinalPactEnd:
                            break;
                        case RecvHeader.PartyResponse:
                            PartyResponseHandler.Handle(c, packet);
                            break;
                        case RecvHeader.PartyOperation:
                            PartyHandler.Handle(c, packet);
                            break;
                        case RecvHeader.RequestRecommendedPartyMembers:
                            RecommendedPartyMembersHandler.Handle(c, packet);
                            break;
                        case RecvHeader.CharacterReceiveDamage:
                            CharacterReceiveDamage.Handle(c, packet);
                            break;
                        case RecvHeader.PlayerChat:
                            PlayerChatHandler.Handle(c, packet);
                            break;
                        case RecvHeader.SpecialChat:
                            SpecialChatHandler.Handle(c, packet);
                            break;
                        case RecvHeader.RequestWeeklyMapleStar:
                            WeeklyMapleStarHandler.Handle(c, packet);
                            return;
                        case RecvHeader.MoveCharacter:
                            MoveCharacterHandler.Handle(c, packet);
                            break;
                        case RecvHeader.EnterMapPortal:
                            EnterMapPortalHandler.Handle(c, packet);
                            break;
                        case RecvHeader.EnterDoor:
                            EnterDoorHandler.Handle(c, packet);
                            break;
                        case RecvHeader.MoveMob:
                            MoveMobHandler.Handle(c, packet);
                            break;
                        case RecvHeader.NpcChat:
                            NpcChatHandler.Handle(c, packet);
                            break;
                        case RecvHeader.NpcShopAction:
                            NpcShopActionHandler.Handle(c, packet);
                            break;
                        case RecvHeader.NpcAnimation:
                            NpcAnimationHandler.Handle(c, packet);
                            break;
                        case RecvHeader.NpcChatMore:
                            NpcChatMoreHandler.Handle(c, packet);
                            break;
                        case RecvHeader.FacialExpression:
                            FacialExpressionHandler.Handle(c, packet);
                            break;
                        case RecvHeader.MeleeAttack:                        
                            DealDamageHandler.HandleMelee(c, packet, header);
                            break;
                        case RecvHeader.RangedAttack:
                            DealDamageHandler.HandleRanged(c, packet);
                            break;
                        case RecvHeader.PassiveAttack:
                        case RecvHeader.MagicAttack:
                            DealDamageHandler.HandleMagic(c, packet);
                            break;
                        case RecvHeader.DistributeAp:
                            DistributeAPHandler.HandleSingle(c, packet);
                            break;
                        case RecvHeader.AutoAssignAp:
                            DistributeAPHandler.HandleDistribute(c, packet);
                            break;
                        case RecvHeader.DistributeSp:
                            DistributeSPHandler.Handle(c, packet);
                            break;
                        case RecvHeader.UseSkill:
                            UseSkillHandler.Handle(c, packet);
                            break;
                        case RecvHeader.MoveItem:
                            MoveItemHandler.Handle(c, packet);
                            break;
                        case RecvHeader.SlotMerge:
                            InventorySortHandler.HandleSlotMerge(c, packet);
                            break;
                        case RecvHeader.ItemSort:
                            InventorySortHandler.HandleItemSort(c, packet);
                            break;
                        case RecvHeader.ChangeChannel:
                            ChangeChannelHandler.Handle(c, packet);
                            break;
                        case RecvHeader.EnterCashShop:
                            EnterCSHandler.Handle(c, packet);
                            break;
                        case RecvHeader.AutoAggroMob:
                            AutoAggroHandler.Handle(c, packet);
                            break;
                        case RecvHeader.LootMapItem:
                            LootItemHandler.HandlePlayer(c, packet);
                            break;
                        case RecvHeader.RegenerateHpMp:
                            RegenerateHPMPHandler.Handle(c, packet);
                            break;
                        case RecvHeader.ChangeKeybind:
                            KeybindHandler.HandleKeyMapChange(c, packet);
                            break;
                        case RecvHeader.QuickSlotKeyMap:
                            KeybindHandler.HandleQuickSlotKeysChange(c, packet);
                            break;
                        case RecvHeader.CancelBuff:
                            CancelBuffHandler.Handle(c, packet);
                            break;
                        case RecvHeader.CharacterInfoRequest:
                            CharacterInfoRequest.Handle(c, packet);
                            break;
                        case RecvHeader.QuestAction:
                            QuestActionHandler.Handle(c, packet);
                            break;
                        case RecvHeader.EnterMapPortalSpecial:
                            EnterMapPortalSpecialHandler.Handle(c, packet);
                            break;
                        case RecvHeader.GuildAction:
                            GuildActionHandler.Handle(c, packet);
                            break;
                        case RecvHeader.DropMeso:
                            DropMesoHandler.Handle(c, packet);
                            break;
                        case RecvHeader.Trade:
                            TradeHandler.Handle(c, packet);
                            break;
                        case RecvHeader.UseConsumable:
                            UseItemHandler.Handle(c, packet);
                            break;
                        case RecvHeader.UseReturnScroll:
                            UseItemHandler.HandleReturnScroll(c, packet);
                            break;
                        case RecvHeader.UseEquipScroll:
                            UseScrollHandler.HandleRegularEquipScroll(c, packet);
                            break;
                        case RecvHeader.UseSpecialEquipScroll:
                            UseScrollHandler.HandleSpecialEquipScroll(c, packet);
                            break;
                        case RecvHeader.UseEquipEnhancementScroll:
                            UseScrollHandler.HandleEquipEnhancementScroll(c, packet);
                            break;
                        case RecvHeader.UseCashItem:
                            UseSpecialItemHandler.Handle(c, packet);
                            break;
                        case RecvHeader.UsePotentialScroll:
                            UseScrollHandler.HandlePotentialScroll(c, packet);
                            break;
                        case RecvHeader.UseBonusPotentialScroll:
                            UseScrollHandler.HandleBonusPotentialScroll(c, packet);
                            break;
                        case RecvHeader.UseCube:
                            UseScrollHandler.HandleCube(c, packet);
                            break;
                        case RecvHeader.UseMagnifyGlass:
                            UseMagnifyingGlassHandler.Handle(c, packet);
                            break;
                        case RecvHeader.SetSkillMacro:
                            SetSkillMacroHandler.Handle(c, packet);
                            break;
                        case RecvHeader.ProffesionReactorAction:
                            ProfessionReactorActionHandler.Handle(c, packet);
                            break;
                        case RecvHeader.ProffesionReactorDestroy:
                            ProfessionReactorActionHandler.HandleDestroy(c, packet);
                            break;
                        case RecvHeader.ReactorAction:
                            ReactorActionHandler.Handle(c, packet);
                            break;
                        case RecvHeader.CraftDone:
                            CraftHandler.HandleCraftDone(c, packet);
                            break;
                        case RecvHeader.CraftEffect:
                            CraftHandler.HandleCraftEffect(c, packet);
                            break;
                        case RecvHeader.CraftMake:
                            CraftHandler.HandleCraftMake(c, packet);
                            break;
                        case RecvHeader.CraftUnk:
                            CraftHandler.HandleUnk(c, packet);
                            break;
                        case RecvHeader.GainAranCombo:
                            AranComboHandler.HandleGain(c);
                            break;
                        case RecvHeader.DecayAranCombo:
                            AranComboHandler.HandleDecay(c);
                            break;
                        case RecvHeader.BlackBlessing:
                            BlackBlessingHandler.Handle(c);
                            break;
                        case RecvHeader.RequestHyperskillInfo:
                            HyperskillInfoRequestHandler.Handle(c, packet);
                            return;
                        case RecvHeader.SkillSwipe:
                            StealSkillHandler.HandleSkillSwipe(c, packet);
                            break;
                        case RecvHeader.ChooseStolenSkill:
                            StealSkillHandler.HandleChooseSkill(c, packet);
                            break;
                        case RecvHeader.StealSkill:
                            StealSkillHandler.HandleStealSkill(c, packet);
                            break;
                        case RecvHeader.MessengerOperation:
                            MapleMessengerHandler.Handle(c, packet);
                            break;
                        case RecvHeader.MoveSummon:
                            SummonHandler.HandleMove(c, packet);
                            break;
                        case RecvHeader.SummonAttack:
                            SummonHandler.HandleAttack(c, packet);
                            break;
                        case RecvHeader.SummonUseSkill:
                            SummonHandler.HandleSkill(c, packet);
                            break;
                        case RecvHeader.RemoveSummon:
                            SummonHandler.HandleRemove(c, packet);
                            break;
                        case RecvHeader.FindPlayer:
                            FindPlayerHandler.Handle(c, packet);
                            break;
                        case RecvHeader.BuddyOperation:
                            BuddyOperationHandler.Handle(c, packet);
                            break;
                        case RecvHeader.UseGoldenHammer:
                            UseGoldenHammerHandler.Handle(c, packet);
                            break;
                        #endregion
                        #region CashShop
                        case RecvHeader.CashshopSelect:
                            CashShop.Select(c, packet);
                            break;
                        #endregion
                        default:
#if DEBUG
                            ServerConsole.Debug("Unhandled recv packet: {0}", Functions.ByteArrayToStr(packet.ToArray()));
#endif
                            c.Account.Character.EnableActions();
                            break;
                    }
                }
            }           
        }
    }
}
