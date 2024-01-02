using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Threading.Tasks;
using Comet.Core;
using Comet.Core.World.Maps.Enums;
using Comet.Database.Entities;
using Comet.Game.Database;
using Comet.Game.Database.Repositories;
using Comet.Game.Internal.AI;
using Comet.Game.Packets;
using Comet.Game.Packets.Ai;
using Comet.Game.States.Events;
using Comet.Game.States.Items;
using Comet.Game.States.Npcs;
using Comet.Game.States.Syndicates;
using Comet.Game.World;
using Comet.Game.World.Managers;
using Comet.Game.World.Maps;
using Comet.Network.Packets.Ai;
using Comet.Network.Packets.Game;
using Comet.Shared;
using Newtonsoft.Json;
using MsgAction = Comet.Game.Packets.MsgAction;
using MsgTalk = Comet.Game.Packets.MsgTalk;

namespace Comet.Game.States
{
    public static class GameAction
    {
        public static async Task<bool> ExecuteActionAsync(uint idAction, Character user, Role role, Item item,
                                                          string input)
        {
            const int _MAX_ACTION_I = 128;
            const int _DEADLOCK_CHECK_I = 5;

            var actionCount = 0;
            var deadLoopCount = 0;
            uint idNext = idAction, idOld = idAction;
            while (idNext > 0)
            {
                if (actionCount++ > _MAX_ACTION_I)
                {
                    await Log.WriteLogAsync(LogLevel.Error,
                                            $"Error: too many game action, from: {idAction}, last action: {idNext}");
                    return false;
                }

                if (idAction == idOld && deadLoopCount++ >= _DEADLOCK_CHECK_I)
                {
                    await Log.WriteLogAsync(LogLevel.DeadLoop,
                                            $"Error: dead loop detected, from: {idAction}, last action: {idNext}");
                    return false;
                }

                if (idNext != idOld) deadLoopCount = 0;

                DbAction action = EventManager.GetAction(idNext);
                if (action == null)
                {
                    await Log.WriteLogAsync(LogLevel.Error, $"Error: invalid game action: {idNext}");
                    return false;
                }

                string param = await FormatParamAsync(action, user, role, item, input);

#if !DEBUG_FELIPE
                if (user?.IsPm() == true && user?.ShowAction == true)
                {
#else
                if (user != null && user.ShowAction)
                {
#endif
                    await user.SendAsync(
                        $"{action.Identity}: [{action.IdNext},{action.IdNextfail}]. type[{action.Type}], data[{action.Data}], param:[{param}].",
                        TalkChannel.System,
                        Color.White);
#if !DEBUG_FELIPE
                }
#else
                }
#endif

                var result = false;
                switch ((TaskActionType) action.Type)
                {
                    case TaskActionType.ActionMenutext:
                        result = await ExecuteActionMenuTextAsync(action, param, user, role, item, input);
                        break;
                    case TaskActionType.ActionMenulink:
                        result = await ExecuteActionMenuLinkAsync(action, param, user, role, item, input);
                        break;
                    case TaskActionType.ActionMenuedit:
                        result = await ExecuteActionMenuEditAsync(action, param, user, role, item, input);
                        break;
                    case TaskActionType.ActionMenupic:
                        result = await ExecuteActionMenuPicAsync(action, param, user, role, item, input);
                        break;
                    case TaskActionType.ActionMenuMessage:
                        result = await ExecuteActionMenuMessageAsync(action, param, user, role, item, input);
                        break;
                    case TaskActionType.ActionMenucreate:
                        result = await ExecuteActionMenuCreateAsync(action, param, user, role, item, input);
                        break;
                    case TaskActionType.ActionRand:
                        result = await ExecuteActionMenuRandAsync(action, param, user, role, item, input);
                        break;
                    case TaskActionType.ActionRandaction:
                        result = await ExecuteActionMenuRandActionAsync(action, param, user, role, item, input);
                        break;
                    case TaskActionType.ActionChktime:
                        result = await ExecuteActionMenuChkTimeAsync(action, param, user, role, item, input);
                        break;
                    case TaskActionType.ActionPostcmd:
                        result = await ExecuteActionPostcmdAsync(action, param, user, role, item, input);
                        break;
                    case TaskActionType.ActionBrocastmsg:
                        result = await ExecuteActionBrocastmsgAsync(action, param, user, role, item, input);
                        break;
                    case TaskActionType.ActionMessagebox:
                        result = await ExecuteActionMessageboxAsync(action, param, user, role, item, input);
                        break;
                    case TaskActionType.ActionExecutequery:
                        result = await ExecuteActionExecutequeryAsync(action, param, user, role, item, input);
                        break;
                    case TaskActionType.ActionInviteTrans:
                        result = await ExecuteActionInviteTransAsync(action, param, user, role, item, input);
                        break;
                    case TaskActionType.ActionSysPathFinding:
                        result = await ExecuteActionSysPathFindingAsync(action, param, user, role, item, input);
                        break;

                    case TaskActionType.ActionNpcAttr:
                        result = await ExecuteActionNpcAttrAsync(action, param, user, role, item, input);
                        break;
                    case TaskActionType.ActionNpcErase:
                        result = await ExecuteActionNpcEraseAsync(action, param, user, role, item, input);
                        break;
                    case TaskActionType.ActionNpcResetsynowner:
                        result = await ExecuteActionNpcResetsynownerAsync(action, param, user, role, item, input);
                        break;
                    case TaskActionType.ActionNpcFindNextTable:
                        result = await ExecuteActionNpcFindNextTableAsync(action, param, user, role, item, input);
                        break;
                    case TaskActionType.ActionNpcFamilyCreate:
                        result = await ExecuteActionNpcFamilyCreateAsync(action, param, user, role, item, input);
                        break;
                    case TaskActionType.ActionNpcFamilyDestroy:
                        result = await ExecuteActionNpcFamilyDestroyAsync(action, param, user, role, item, input);
                        break;

                    case TaskActionType.ActionMapMovenpc:
                        result = await ExecuteActionMapMovenpcAsync(action, param, user, role, item, input);
                        break;
                    case TaskActionType.ActionMapMapuser:
                        result = await ExecuteActionMapMapuserAsync(action, param, user, role, item, input);
                        break;
                    case TaskActionType.ActionMapBrocastmsg:
                        result = await ExecuteActionMapBrocastmsgAsync(action, param, user, role, item, input);
                        break;
                    case TaskActionType.ActionMapDropitem:
                        result = await ExecuteActionMapDropitemAsync(action, param, user, role, item, input);
                        break;
                    case TaskActionType.ActionMapSetstatus:
                        result = await ExecuteActionMapSetstatusAsync(action, param, user, role, item, input);
                        break;
                    case TaskActionType.ActionMapAttrib:
                        result = await ExecuteActionMapAttribAsync(action, param, user, role, item, input);
                        break;
                    case TaskActionType.ActionMapRegionMonster:
                        result = await ExecuteActionMapRegionMonsterAsync(action, param, user, role, item, input);
                        break;
                    case TaskActionType.ActionMapRandDropItem:
                        result = await ExecuteActionMapRandDropItemAsync(action, param, user, role, item, input);
                        break;
                    case TaskActionType.ActionMapChangeweather:
                        result = await ExecuteActionMapChangeweatherAsync(action, param, user, role, item, input);
                        break;
                    case TaskActionType.ActionMapChangelight:
                        result = await ExecuteActionMapChangelightAsync(action, param, user, role, item, input);
                        break;
                    case TaskActionType.ActionMapMapeffect:
                        result = await ExecuteActionMapMapeffectAsync(action, param, user, role, item, input);
                        break;
                    case TaskActionType.ActionMapFireworks:
                        result = await ExecuteActionMapFireworksAsync(action, param, user, role, item, input);
                        break;

                    case TaskActionType.ActionItemRequestlaynpc:
                        result = await ExecuteActionItemRequestlaynpcAsync(action, param, user, role, item, input);
                        break;
                    case TaskActionType.ActionItemLaynpc:
                        result = await ExecuteActionItemLaynpcAsync(action, param, user, role, item, input);
                        break;
                    case TaskActionType.ActionItemDelthis:
                        result = await ExecuteActionItemDelthisAsync(action, param, user, role, item, input);
                        break;

                    case TaskActionType.ActionItemAdd:
                        result = await ExecuteActionItemAddAsync(action, param, user, role, item, input);
                        break;
                    case TaskActionType.ActionItemDel:
                        result = await ExecuteActionItemDelAsync(action, param, user, role, item, input);
                        break;
                    case TaskActionType.ActionItemCheck:
                        result = await ExecuteActionItemCheckAsync(action, param, user, role, item, input);
                        break;
                    case TaskActionType.ActionItemHole:
                        result = await ExecuteActionItemHoleAsync(action, param, user, role, item, input);
                        break;
                    case TaskActionType.ActionItemMultidel:
                        result = await ExecuteActionItemMultidelAsync(action, param, user, role, item, input);
                        break;
                    case TaskActionType.ActionItemMultichk:
                        result = await ExecuteActionItemMultichkAsync(action, param, user, role, item, input);
                        break;
                    case TaskActionType.ActionItemLeavespace:
                        result = await ExecuteActionItemLeavespaceAsync(action, param, user, role, item, input);
                        break;
                    case TaskActionType.ActionItemUpequipment:
                        result = await ExecuteActionItemUpequipmentAsync(action, param, user, role, item, input);
                        break;
                    case TaskActionType.ActionItemEquiptest:
                        result = await ExecuteActionItemEquiptestAsync(action, param, user, role, item, input);
                        break;
                    case TaskActionType.ActionItemEquipexist:
                        result = await ExecuteActionItemEquipexistAsync(action, param, user, role, item, input);
                        break;
                    case TaskActionType.ActionItemEquipcolor:
                        result = await ExecuteActionItemEquipcolorAsync(action, param, user, role, item, input);
                        break;
                    case TaskActionType.ActionItemCheckrand:
                        result = await ExecuteActionItemCheckrandAsync(action, param, user, role, item, input);
                        break;
                    case TaskActionType.ActionItemModify:
                        result = await ExecuteActionItemModifyAsync(action, param, user, role, item, input);
                        break;
                    case TaskActionType.ActionItemJarCreate:
                        result = await ExecuteActionItemJarCreateAsync(action, param, user, role, item, input);
                        break;
                    case TaskActionType.ActionItemJarVerify:
                        result = await ExecuteActionItemJarVerifyAsync(action, param, user, role, item, input);
                        break;

                    case TaskActionType.ActionSynCreate:
                        result = await ExecuteActionSynCreateAsync(action, param, user, role, item, input);
                        break;
                    case TaskActionType.ActionSynDestroy:
                        result = await ExecuteActionSynDestroyAsync(action, param, user, role, item, input);
                        break;
                    case TaskActionType.ActionSynSetAssistant:
                        result = await ExecuteActionSynSetAssistantAsync(action, param, user, role, item, input);
                        break;
                    case TaskActionType.ActionSynClearRank:
                        result = await ExecuteActionSynClearRankAsync(action, param, user, role, item, input);
                        break;
                    case TaskActionType.ActionSynChangeLeader:
                        result = await ExecuteActionSynChangeLeaderAsync(action, param, user, role, item, input);
                        break;
                    case TaskActionType.ActionSynAntagonize:
                        result = await ExecuteActionSynAntagonizeAsync(action, param, user, role, item, input);
                        break;
                    case TaskActionType.ActionSynClearAntagonize:
                        result = await ExecuteActionSynClearAntagonizeAsync(action, param, user, role, item, input);
                        break;
                    case TaskActionType.ActionSynAlly:
                        result = await ExecuteActionSynAllyAsync(action, param, user, role, item, input);
                        break;
                    case TaskActionType.ActionSynClearAlly:
                        result = await ExecuteActionSynClearAllyAsync(action, param, user, role, item, input);
                        break;
                    case TaskActionType.ActionSynAttr:
                        result = await ExecuteActionSynAttrAsync(action, param, user, role, item, input);
                        break;

                    case TaskActionType.ActionMstDropitem:
                        result = await ExecuteActionMstDropitemAsync(action, param, user, role, item, input);
                        break;

                    case TaskActionType.ActionUserAttr:
                        result = await ExecuteUserAttrAsync(action, param, user, role, item, input);
                        break;
                    case TaskActionType.ActionUserFull:
                        result = await ExecuteUserFullAsync(action, param, user, role, item, input);
                        break;
                    case TaskActionType.ActionUserChgmap:
                        result = await ExecuteUserChgMapAsync(action, param, user, role, item, input);
                        break;
                    case TaskActionType.ActionUserRecordpoint:
                        result = await ExecuteUserRecordpointAsync(action, param, user, role, item, input);
                        break;
                    case TaskActionType.ActionUserHair:
                        result = await ExecuteUserHairAsync(action, param, user, role, item, input);
                        break;
                    case TaskActionType.ActionUserChgmaprecord:
                        result = await ExecuteUserChgmaprecordAsync(action, param, user, role, item, input);
                        break;
                    case TaskActionType.ActionUserChglinkmap:
                        result = await ExecuteActionUserChglinkmapAsync(action, param, user, role, item, input);
                        break;
                    case TaskActionType.ActionUserTransform:
                        result = await ExecuteUserTransformAsync(action, param, user, role, item, input);
                        break;
                    case TaskActionType.ActionUserIspure:
                        result = await ExecuteActionUserIspureAsync(action, param, user, role, item, input);
                        break;
                    case TaskActionType.ActionUserTalk:
                        result = await ExecuteActionUserTalkAsync(action, param, user, role, item, input);
                        break;
                    case TaskActionType.ActionUserMagic:
                        result = await ExecuteActionUserMagicAsync(action, param, user, role, item, input);
                        break;
                    case TaskActionType.ActionUserWeaponskill:
                        result = await ExecuteActionUserWeaponSkillAsync(action, param, user, role, item, input);
                        break;
                    case TaskActionType.ActionUserLog:
                        result = await ExecuteActionUserLogAsync(action, param, user, role, item, input);
                        break;
                    case TaskActionType.ActionUserBonus:
                        result = await ExecuteActionUserBonusAsync(action, param, user, role, item, input);
                        break;
                    case TaskActionType.ActionUserDivorce:
                        result = await ExecuteActionUserDivorceAsync(action, param, user, role, item, input);
                        break;
                    case TaskActionType.ActionUserMarriage:
                        result = await ExecuteActionUserMarriageAsync(action, param, user, role, item, input);
                        break;
                    case TaskActionType.ActionUserSex:
                        result = await ExecuteActionUserSexAsync(action, param, user, role, item, input);
                        break;
                    case TaskActionType.ActionUserEffect:
                        result = await ExecuteActionUserEffectAsync(action, param, user, role, item, input);
                        break;
                    case TaskActionType.ActionUserTaskmask:
                        result = await ExecuteActionUserTaskmaskAsync(action, param, user, role, item, input);
                        break;
                    case TaskActionType.ActionUserMediaplay:
                        result = await ExecuteActionUserMediaplayAsync(action, param, user, role, item, input);
                        break;
                    case TaskActionType.ActionUserCreatemap:
                        result = await ExecuteActionUserCreatemapAsync(action, param, user, role, item, input);
                        break;
                    case TaskActionType.ActionUserEnterHome:
                        result = await ExecuteActionUserEnterHomeAsync(action, param, user, role, item, input);
                        break;
                    case TaskActionType.ActionUserEnterMateHome:
                        result = await ExecuteActionUserEnterMateHomeAsync(action, param, user, role, item, input);
                        break;
                    case TaskActionType.ActionUserUnlearnMagic:
                        result = await ExecuteActionUserUnlearnMagicAsync(action, param, user, role, item, input);
                        break;
                    case TaskActionType.ActionUserRebirth:
                        result = await ExecuteActionUserRebirthAsync(action, param, user, role, item, input);
                        break;
                    case TaskActionType.ActionUserWebpage:
                        result = await ExecuteActionUserWebpageAsync(action, param, user, role, item, input);
                        break;
                    case TaskActionType.ActionUserBbs:
                        result = await ExecuteActionUserBbsAsync(action, param, user, role, item, input);
                        break;
                    case TaskActionType.ActionUserUnlearnSkill:
                        result = await ExecuteActionUserUnlearnSkillAsync(action, param, user, role, item, input);
                        break;
                    case TaskActionType.ActionUserDropMagic:
                        result = await ExecuteActionUserDropMagicAsync(action, param, user, role, item, input);
                        break;
                    case TaskActionType.ActionUserOpenDialog:
                        result = await ExecuteActionUserOpenDialogAsync(action, param, user, role, item, input);
                        break;
                    case TaskActionType.ActionUserPointAllot:
                        result = await ExecuteActionUserFixAttrAsync(action, param, user, role, item, input);
                        break;
                    case TaskActionType.ActionUserExpMultiply:
                        result = await ExecuteActionUserExpMultiplyAsync(action, param, user, role, item, input);
                        break;
                    case TaskActionType.ActionUserWhPassword:
                        result = await ExecuteActionUserWhPasswordAsync(action, param, user, role, item, input);
                        break;
                    case TaskActionType.ActionUserSetWhPassword:
                        result = await ExecuteActionUserSetWhPasswordAsync(action, param, user, role, item, input);
                        break;
                    case TaskActionType.ActionUserOpeninterface:
                        result = await ExecuteActionUserOpeninterfaceAsync(action, param, user, role, item, input);
                        break;
                    case TaskActionType.ActionUserTaskManager:
                        result = await ExecuteActionUserTaskManagerAsync(action, param, user, role, item, input);
                        break;
                    case TaskActionType.ActionUserTaskOpe:
                        result = await ExecuteActionUserTaskOpeAsync(action, param, user, role, item, input);
                        break;
                    case TaskActionType.ActionUserTaskLocaltime:
                        result = await ExecuteActionUserTaskLocaltimeAsync(action, param, user, role, item, input);
                        break;
                    case TaskActionType.ActionUserTaskFind:
                        result = await ExecuteActionUserTaskFindAsync(action, param, user, role, item, input);
                        break;
                    case TaskActionType.ActionUserVarCompare:
                        result = await ExecuteActionUserVarCompareAsync(action, param, user, role, item, input);
                        break;
                    case TaskActionType.ActionUserVarDefine:
                        result = await ExecuteActionUserVarDefineAsync(action, param, user, role, item, input);
                        break;
                    case TaskActionType.ActionUserVarCalc:
                        result = await ExecuteActionUserVarCalcAsync(action, param, user, role, item, input);
                        break;
                    case TaskActionType.ActionUserTestEquipment:
                        result = await ExecuteActionUserTestEquipmentAsync(action, param, user, role, item, input);
                        break;
                    case TaskActionType.ActionUserExecAction:
                        result = await ExecuteActionUserExecActionAsync(action, param, user, role, item, input);
                        break;
                    case TaskActionType.ActionUserTestPos:
                        result = true;
                        break; // gotta investigate
                    case TaskActionType.ActionUserStcCompare:
                        result = await ExecuteActionUserStcCompareAsync(action, param, user, role, item, input);
                        break;
                    case TaskActionType.ActionUserStcOpe:
                        result = await ExecuteActionUserStcOpeAsync(action, param, user, role, item, input);
                        break;
                    case TaskActionType.ActionUserDataSync:
                        result = await ExecuteActionUserDataSyncAsync(action, param, user, role, item, input);
                        break;
                    case TaskActionType.ActionUserSelectToData:
                        result = await ExecuteActionUserSelectToDataAsync(action, param, user, role, item, input);
                        break;
                    case TaskActionType.ActionUserStcTimeCheck:
                        result = await ExecuteActionUserStcTimeCheckAsync(action, param, user, role, item, input);
                        break;
                    case TaskActionType.ActionUserStcTimeOperation:
                        result = await ExecuteActionUserStcTimeOpeAsync(action, param, user, role, item, input);
                        break;
                    case TaskActionType.ActionUserAttachStatus:
                        result = await ExecuteActionUserAttachStatusAsync(action, param, user, role, item, input);
                        break;
                    case TaskActionType.ActionUserGodTime:
                        result = await ExecuteActionUserGodTimeAsync(action, param, user, role, item, input);
                        break;
                    case TaskActionType.ActionUserLogEvent:
                        result = await ExecuteActionUserLogAsync(action, param, user, role, item, input);
                        break;
                    case TaskActionType.ActionUserExpballExp:
                        result = await ExecuteActionUserExpballExpAsync(action, param, user, role, item, input);
                        break;
                    case TaskActionType.ActionSomethingRelatedToRebirth:
                        result = true;
                        break;
                    case TaskActionType.ActionUserStatusCreate:
                        result = await ExecuteActionUserStatusCreateAsync(action, param, user, role, item, input);
                        break;
                    case TaskActionType.ActionUserStatusCheck:
                        result = await ExecuteActionUserStatusCheckAsync(action, param, user, role, item, input);
                        break;

                    case TaskActionType.ActionTeamBroadcast:
                        result = await ExecuteActionTeamBroadcastAsync(action, param, user, role, item, input);
                        break;
                    case TaskActionType.ActionTeamAttr:
                        result = await ExecuteActionTeamAttrAsync(action, param, user, role, item, input);
                        break;
                    case TaskActionType.ActionTeamLeavespace:
                        result = await ExecuteActionTeamLeavespaceAsync(action, param, user, role, item, input);
                        break;
                    case TaskActionType.ActionTeamItemAdd:
                        result = await ExecuteActionTeamItemAddAsync(action, param, user, role, item, input);
                        break;
                    case TaskActionType.ActionTeamItemDel:
                        result = await ExecuteActionTeamItemDelAsync(action, param, user, role, item, input);
                        break;
                    case TaskActionType.ActionTeamItemCheck:
                        result = await ExecuteActionTeamItemCheckAsync(action, param, user, role, item, input);
                        break;
                    case TaskActionType.ActionTeamChgmap:
                        result = await ExecuteActionTeamChgmapAsync(action, param, user, role, item, input);
                        break;
                    case TaskActionType.ActionTeamChkIsleader:
                        result = await ExecuteActionTeamChkIsleaderAsync(action, param, user, role, item, input);
                        break;

                    case TaskActionType.ActionGeneralLottery:
                        result = await ExecuteGeneralLotteryAsync(action, param, user, role, item, input);
                        break;
                    case TaskActionType.ActionChgMapSquare:
                        result = await ExecuteActionChgMapSquareAsync(action, param, user, role, item, input);
                        break;
                    case TaskActionType.ActionAchievements:
                        result = true;
                        break;

                    case TaskActionType.ActionEventSetstatus:
                        result = await ExecuteActionEventSetstatusAsync(action, param, user, role, item, input);
                        break;
                    case TaskActionType.ActionEventDelnpcGenid:
                        result = await ExecuteActionEventDelnpcGenidAsync(action, param, user, role, item, input);
                        break;
                    case TaskActionType.ActionEventCompare:
                        result = await ExecuteActionEventCompareAsync(action, param, user, role, item, input);
                        break;
                    case TaskActionType.ActionEventCompareUnsigned:
                        result = await ExecuteActionEventCompareUnsignedAsync(action, param, user, role, item, input);
                        break;
                    case TaskActionType.ActionEventChangeweather:
                        result = await ExecuteActionEventChangeweatherAsync(action, param, user, role, item, input);
                        break;
                    case TaskActionType.ActionEventCreatepet:
                        result = await ExecuteActionEventCreatepetAsync(action, param, user, role, item, input);
                        break;
                    case TaskActionType.ActionEventCreatenewNpc:
                        result = await ExecuteActionEventCreatenewNpcAsync(action, param, user, role, item, input);
                        break;
                    case TaskActionType.ActionEventCountmonster:
                        result = await ExecuteActionEventCountmonsterAsync(action, param, user, role, item, input);
                        break;
                    case TaskActionType.ActionEventDeletemonster:
                        result = await ExecuteActionEventDeletemonsterAsync(action, param, user, role, item, input);
                        break;
                    case TaskActionType.ActionEventBbs:
                        result = await ExecuteActionEventBbsAsync(action, param, user, role, item, input);
                        break;
                    case TaskActionType.ActionEventErase:
                        result = await ExecuteActionEventEraseAsync(action, param, user, role, item, input);
                        break;
                    case TaskActionType.ActionEventTeleport:
                        result = await ExecuteActionEventTeleportAsync(action, param, user, role, item, input);
                        break;
                    case TaskActionType.ActionEventMassaction:
                        result = await ExecuteActionEventMassactionAsync(action, param, user, role, item, input);
                        break;

                    case TaskActionType.ActionEventRegister:
                        result = await ExecuteActionEventRegisterAsync(action, param, user, role, item, input);
                        break;
                    case TaskActionType.ActionEventExit:
                        result = await ExecuteActionEventExitAsync(action, param, user, role, item, input);
                        break;
                    case TaskActionType.ActionEventCmd:
                        result = await ExecuteActionEventCmdAsync(action, param, user, role, item, input);
                        break;

                    case TaskActionType.ActionFamilyAttr:
                        result = await ExecuteActionFamilyAttrAsync(action, param, user, role, item, input);
                        break;
                    case TaskActionType.ActionFamilyMemberAttr:
                        result = await ExecuteActionFamilyMemberAttrAsync(action, param, user, role, item, input);
                        break;
                    case TaskActionType.ActionFamilyWarActivityCheck:
                        result = await ExecuteActionFamilyWarActivityCheckAsync(action, param, user, role, item, input);
                        break;
                    case TaskActionType.ActionFamilyWarAuthorityCheck:
                        result = await ExecuteActionFamilyWarAuthorityCheckAsync(
                                     action, param, user, role, item, input);
                        break;
                    case TaskActionType.ActionFamilyWarRegisterCheck:
                        result = await ActionFamilyWarRegisterCheckAsync(action, param, user, role, item, input);
                        break;

                    case TaskActionType.ActionTrapCreate:
                        result = await ExecuteActionTrapCreateAsync(action, param, user, role, item, input);
                        break;
                    case TaskActionType.ActionTrapErase:
                        result = await ExecuteActionTrapEraseAsync(action, param, user, role, item, input);
                        break;
                    case TaskActionType.ActionTrapCount:
                        result = await ExecuteActionTrapCountAsync(action, param, user, role, item, input);
                        break;

                    case TaskActionType.ActionDetainDialog:
                        result = await ExecuteActionDetainDialogAsync(action, param, user, role, item, input);
                        break;

                    default:
                        await Log.WriteLogAsync(LogLevel.Warning,
                                                $"GameAction::ExecuteActionAsync unhandled action type {action.Type} for action: {action.Identity}");
                        break;
                }

                idOld = idAction;
                idNext = result ? action.IdNext : action.IdNextfail;
            }

            return true;
        }

        #region Action

        private static async Task<bool> ExecuteActionMenuTextAsync(DbAction action, string param, Character user,
                                                                   Role role,
                                                                   Item item, string input)
        {
            if (user == null)
            {
                await Log.WriteLogAsync(LogLevel.Warning, $"Action[{action.Identity}] type 101 non character");
                return false;
            }

            await user.SendAsync(new MsgTaskDialog
            {
                InteractionType = MsgTaskDialog<Client>.TaskInteraction.Dialog,
                Text = param,
                Data = (ushort) action.Data
            });
            return true;
        }

        private static async Task<bool> ExecuteActionMenuLinkAsync(DbAction action, string param, Character user,
                                                                   Role role,
                                                                   Item item, string input)
        {
            if (user == null)
            {
                await Log.WriteLogAsync(LogLevel.Warning, $"Action[{action.Identity}] type 101 non character");
                return false;
            }

            uint task = 0;
            var align = 0;
            string[] parsed = param.Split(' ');
            if (parsed.Length > 1)
                uint.TryParse(parsed[1], out task);
            if (parsed.Length > 2)
                int.TryParse(parsed[2], out align);

            await user.SendAsync(new MsgTaskDialog
            {
                InteractionType = MsgTaskDialog<Client>.TaskInteraction.Option,
                Text = parsed[0],
                OptionIndex = user.PushTaskId(task),
                Data = (ushort) align
            });
            return true;
        }

        private static async Task<bool> ExecuteActionMenuEditAsync(DbAction action, string param, Character user,
                                                                   Role role,
                                                                   Item item, string input)
        {
            if (user == null)
                return false;

            string[] paramStrings = SplitParam(param, 3);
            if (paramStrings.Length < 3)
            {
                await Log.WriteLogAsync(LogLevel.Error,
                                        $"Invalid input param length for {action.Identity}, param: {param}");
                return false;
            }

            await user.SendAsync(new MsgTaskDialog
            {
                InteractionType = MsgTaskDialog<Client>.TaskInteraction.Input,
                OptionIndex = user.PushTaskId(uint.Parse(paramStrings[1])),
                Data = ushort.Parse(paramStrings[0]),
                Text = paramStrings[2]
            });

            return true;
        }

        private static async Task<bool> ExecuteActionMenuPicAsync(DbAction action, string param, Character user,
                                                                  Role role,
                                                                  Item item, string input)
        {
            if (user == null)
                return false;

            string[] splitParam = SplitParam(param);

            ushort x = ushort.Parse(splitParam[0]);
            ushort y = ushort.Parse(splitParam[1]);

            await user.SendAsync(new MsgTaskDialog
            {
                TaskIdentity = (uint) ((x << 16) | y),
                InteractionType = MsgTaskDialog<Client>.TaskInteraction.Avatar,
                Data = ushort.Parse(splitParam[2])
            });
            return true;
        }

        private static async Task<bool> ExecuteActionMenuMessageAsync(DbAction action, string param, Character user,
                                                                      Role role, Item item, string input)
        {
            if (user == null)
                return false;

            await user.SendAsync(new MsgTaskDialog
            {
                InteractionType = MsgTaskDialog<Client>.TaskInteraction.MessageBox,
                Text = param,
                OptionIndex = user.PushTaskId(action.Data)
            });

            return true;
        }

        private static async Task<bool> ExecuteActionMenuCreateAsync(DbAction action, string param, Character user,
                                                                     Role role, Item item, string input)
        {
            if (user == null)
                return false;

            await user.SendAsync(new MsgTaskDialog
            {
                InteractionType = MsgTaskDialog<Client>.TaskInteraction.Finish
            });
            return true;
        }

        private static async Task<bool> ExecuteActionMenuRandAsync(DbAction action, string param, Character user,
                                                                   Role role,
                                                                   Item item, string input)
        {
            string[] paramSplit = SplitParam(param, 2);

            int x = int.Parse(paramSplit[0]);
            int y = int.Parse(paramSplit[1]);
            var chance = 0.01;
            if (x > y)
            {
                chance = 99;
            }
            else
            {
                chance = x / (double) y;
                chance *= 100;
            }

            return await Kernel.ChanceCalcAsync(chance);
        }

        private static async Task<bool> ExecuteActionMenuRandActionAsync(DbAction action, string param, Character user,
                                                                         Role role, Item item, string input)
        {
            string[] paramSplit = SplitParam(param);
            if (paramSplit.Length == 0)
                return false;
            uint taskId = uint.Parse(paramSplit[await Kernel.NextAsync(0, paramSplit.Length) % paramSplit.Length]);
            return await ExecuteActionAsync(taskId, user, role, item, input);
        }

        private static async Task<bool> ExecuteActionMenuChkTimeAsync(DbAction action, string param, Character user,
                                                                      Role role, Item item, string input)
        {
            string[] paramSplit = SplitParam(param);

            DateTime actual = DateTime.Now;
            var nCurWeekDay = (int) actual.DayOfWeek;
            int nCurHour = actual.Hour;
            int nCurMinute = actual.Minute;

            switch (action.Data)
            {
                #region Complete date (yyyy-mm-dd hh:mm yyyy-mm-dd hh:mm)

                case 0:
                {
                    if (paramSplit.Length < 4)
                        return false;

                    string[] time0 = paramSplit[1].Split(':');
                    string[] date0 = paramSplit[0].Split('-');
                    string[] time1 = paramSplit[3].Split(':');
                    string[] date1 = paramSplit[2].Split('-');

                    var dTime0 = new DateTime(int.Parse(date0[0]), int.Parse(date0[1]), int.Parse(date0[2]),
                                              int.Parse(time0[0]), int.Parse(time0[1]), 0);
                    var dTime1 = new DateTime(int.Parse(date1[0]), int.Parse(date1[1]), int.Parse(date1[2]),
                                              int.Parse(time1[0]), int.Parse(time1[1]), 59);

                    return dTime0 <= actual && dTime1 >= actual;
                }

                #endregion

                #region On Year date (mm-dd hh:mm mm-dd hh:mm)

                case 1:
                {
                    if (paramSplit.Length < 4)
                        return false;

                    string[] time0 = paramSplit[1].Split(':');
                    string[] date0 = paramSplit[0].Split('-');
                    string[] time1 = paramSplit[3].Split(':');
                    string[] date1 = paramSplit[2].Split('-');

                    var dTime0 = new DateTime(DateTime.Now.Year, int.Parse(date0[1]), int.Parse(date0[2]),
                                              int.Parse(time0[0]), int.Parse(time0[1]), 0);
                    var dTime1 = new DateTime(DateTime.Now.Year, int.Parse(date1[1]), int.Parse(date1[2]),
                                              int.Parse(time1[0]), int.Parse(time1[1]), 59);

                    return dTime0 <= actual && dTime1 >= actual;
                }

                #endregion

                #region Day of the month (dd hh:mm dd hh:mm)

                case 2:
                {
                    if (paramSplit.Length < 4)
                        return false;

                    string[] time0 = paramSplit[1].Split(':');
                    string date0 = paramSplit[0];
                    string[] time1 = paramSplit[3].Split(':');
                    string date1 = paramSplit[2];

                    var dTime0 = new DateTime(DateTime.Now.Year, DateTime.Now.Month, int.Parse(date0),
                                              int.Parse(time0[0]), int.Parse(time0[1]), 0);
                    var dTime1 = new DateTime(DateTime.Now.Year, DateTime.Now.Month, int.Parse(date1),
                                              int.Parse(time1[0]), int.Parse(time1[1]), 59);

                    return dTime0 <= actual && dTime1 >= actual;
                }

                #endregion

                #region Day of the week (dw hh:mm dw hh:mm)

                case 3:
                {
                    if (paramSplit.Length < 4)
                        return false;

                    string[] time0 = paramSplit[1].Split(':');
                    string[] time1 = paramSplit[3].Split(':');

                    int nDay0 = int.Parse(paramSplit[0]);
                    int nDay1 = int.Parse(paramSplit[2]);
                    int nHour0 = int.Parse(time0[0]);
                    int nHour1 = int.Parse(time1[0]);
                    int nMinute0 = int.Parse(time0[1]);
                    int nMinute1 = int.Parse(time1[1]);

                    int timeNow = nCurWeekDay * 24 * 60 + nCurHour * 60 + nCurMinute;
                    int from = nDay0 * 24 * 60 + nHour0 * 60 + nMinute0;
                    int to = nDay1 * 24 * 60 + nHour1 * 60 + nMinute1;

                    return timeNow >= from && timeNow <= to;
                }

                #endregion

                #region Hour check (hh:mm hh:mm)

                case 4:
                {
                    if (paramSplit.Length < 2)
                        return false;

                    string[] time0 = paramSplit[0].Split(':');
                    string[] time1 = paramSplit[1].Split(':');

                    int nHour0 = int.Parse(time0[0]);
                    int nHour1 = int.Parse(time1[0]);
                    int nMinute0 = int.Parse(time0[1]);
                    int nMinute1 = int.Parse(time1[1]);

                    int timeNow = nCurHour * 60 + nCurMinute;
                    int from = nHour0 * 60 + nMinute0;
                    int to = nHour1 * 60 + nMinute1;

                    return timeNow >= from && timeNow <= to;
                }

                #endregion

                #region Minute check (mm mm)

                case 5:
                {
                    if (paramSplit.Length < 2)
                        return false;

                    return nCurMinute >= int.Parse(paramSplit[0]) && nCurMinute <= int.Parse(paramSplit[1]);
                }

                #endregion
            }

            return false;
        }

        private static async Task<bool> ExecuteActionPostcmdAsync(DbAction action, string param, Character user,
                                                                  Role role,
                                                                  Item item, string input)
        {
            if (user == null)
                return false;

            await user.SendAsync(new MsgAction
            {
                Identity = user.Identity,
                Command = action.Data,
                Action = MsgAction<Client>.ActionType.ClientCommand,
                ArgumentX = user.MapX,
                ArgumentY = user.MapY
            });

            return true;
        }

        private static async Task<bool> ExecuteActionBrocastmsgAsync(DbAction action, string param, Character user,
                                                                     Role role, Item item, string input)
        {
            await RoleManager.BroadcastMsgAsync(param, (TalkChannel) action.Data, Color.White);
            return true;
        }

        private static async Task<bool> ExecuteActionMessageboxAsync(DbAction action, string param, Character user,
                                                                     Role role, Item item, string input)
        {
            if (user != null)
            {
                var channel = TalkChannel.MessageBox;
                if (action.Data != 0 && Enum.IsDefined(typeof(TalkChannel), (ushort) action.Data))
                    channel = (TalkChannel) action.Data;

                await user.SendAsync(new MsgTalk(0, channel, param));
            }

            return true;
        }

        private static async Task<bool> ExecuteActionExecutequeryAsync(DbAction action, string param, Character user,
                                                                       Role role, Item item, string input)
        {
            try
            {
                if (param.Trim().StartsWith("SELECT", StringComparison.InvariantCultureIgnoreCase) ||
                    param.Trim().StartsWith("UPDATE", StringComparison.InvariantCultureIgnoreCase))
                    if (!param.Contains("WHERE") || !param.Contains("LIMIT"))
                    {
                        await Log.WriteLogAsync("database", LogLevel.Warning,
                                                $"ExecuteActionExecutequery {action.Identity} doesn't have WHERE or LIMIT clause [{param}]");
                        return false;
                    }

                await ServerDbContext.ScalarAsync(param);
            }
            catch (Exception ex)
            {
                await Log.WriteLogAsync(LogLevel.Exception, ex.ToString());
                return false;
            }

            return true;
        }

        private static async Task<bool> ExecuteActionInviteTransAsync(DbAction action, string param, Character user,
                                                                      Role role, Item item, string input)
        {
            if (user == null)
                return false;

            if (user.MessageBox is CaptchaBox)
                return false;

            string[] split = param.Trim().Split(' ');
            if (split.Length != 21)
            {
                await Log.WriteLogAsync(LogLevel.Action,
                                        "ActionInviteTrans must have 21 parameters. mapid 8pos msg acceptmsg type seconds");
                return false;
            }

            uint idMap = uint.Parse(split[0]);
            var px = new ushort[8];
            var py = new ushort[8];
            for (var i = 0; i < 8; i++)
            {
                px[i] = ushort.Parse(split[i * 2 + 1]);
                py[i] = ushort.Parse(split[i * 2 + 2]);
            }

            int sendMsg = int.Parse(split[17]);
            int acceptMsg = int.Parse(split[18]);
            int priority = int.Parse(split[19]);
            int seconds = int.Parse(split[20]);

            if (user.MessageBox is EventInvitationBox box)
                if (box.Priority > priority && !box.HasExpired)
                    return true;

            var timedMessageBox = new EventInvitationBox(user, seconds)
            {
                MessageId = sendMsg,
                AcceptMsgId = acceptMsg,
                Priority = priority,
                TargetMapIdentity = idMap,
                TargetMapX = px,
                TargetMapY = py
            };

            user.MessageBox = timedMessageBox;

            await timedMessageBox.SendAsync();

            return true;
        }

        private static async Task<bool> ExecuteActionSysPathFindingAsync(DbAction action, string param, Character user,
                                                                         Role role, Item item, string input)
        {
            if (user == null)
                return false;

            string[] @params = param.Split(' ');
            ushort x = ushort.Parse(@params[0]);
            ushort y = ushort.Parse(@params[1]);
            uint npc = uint.Parse(@params[2]);
            uint idMap = user.MapIdentity;

            if (@params.Length > 3)
                idMap = uint.Parse(@params[3]);

            var msg = new MsgAction();
            msg.Action = MsgAction<Client>.ActionType.PathFinding;
            msg.Identity = user.Identity;
            msg.Data = npc;
            msg.Command = npc;
            msg.Argument = npc;
            msg.Map = idMap;
            msg.X = x;
            msg.Y = y;
            await user.SendAsync(msg);
            return true;
        }

        #endregion

        #region Npc

        private static async Task<bool> ExecuteActionNpcAttrAsync(DbAction action, string param, Character user,
                                                                  Role role,
                                                                  Item item, string input)
        {
            string[] splitParams = SplitParam(param);
            if (splitParams.Length < 3)
            {
                await Log.WriteLogAsync(LogLevel.Warning,
                                        $"ExecuteActionNpcAttr invalid param num {param}, {action.Identity}");
                return false;
            }

            string ope = splitParams[0].ToLower();
            string opt = splitParams[1].ToLower();
            bool isInt = int.TryParse(splitParams[2], out int data);
            string strData = splitParams[2];

            if (splitParams.Length < 4 || !uint.TryParse(splitParams[3], out uint idNpc))
                idNpc = role?.Identity ?? user?.InteractingNpc ?? 0;

            var npc = RoleManager.GetRole<BaseNpc>(idNpc);
            if (npc == null)
            {
                await Log.WriteLogAsync(LogLevel.Warning,
                                        $"ExecuteActionNpcAttr invalid NPC id {idNpc} for action {action.Identity}");
                return false;
            }

            var cmp = 0;
            var strCmp = "";
            if (ope.Equals("life", StringComparison.InvariantCultureIgnoreCase))
            {
                if (opt == "=") return await npc.SetAttributesAsync(ClientUpdateType.Hitpoints, (ulong) data);

                if (opt == "+=") return await npc.AddAttributesAsync(ClientUpdateType.Hitpoints, data);

                cmp = (int) npc.Life;
            }
            else if (ope.Equals("lookface", StringComparison.InvariantCultureIgnoreCase))
            {
                if (opt == "=") return await npc.SetAttributesAsync(ClientUpdateType.Mesh, (ulong) data);

                cmp = (int) npc.Mesh;
            }
            else if (ope.Equals("ownerid", StringComparison.InvariantCultureIgnoreCase))
            {
                if (opt == "=")
                {
                    if (!(npc is DynamicNpc dyna))
                        return false;
                    return await dyna.SetOwnerAsync((uint) data);
                }

                cmp = (int) npc.OwnerIdentity;
            }
            else if (ope.Equals("ownertype", StringComparison.InvariantCultureIgnoreCase))
            {
                cmp = (int) npc.OwnerType;
            }
            else if (ope.Equals("maxlife", StringComparison.InvariantCultureIgnoreCase))
            {
                if (opt == "=") return await npc.SetAttributesAsync(ClientUpdateType.MaxHitpoints, (ulong) data);

                cmp = (int) npc.MaxLife;
            }
            else if (ope.StartsWith("data", StringComparison.InvariantCultureIgnoreCase))
            {
                if (opt == "=")
                {
                    npc.SetData(ope, data);
                    return await npc.SaveAsync();
                }

                if (opt == "+=")
                {
                    npc.SetData(ope, npc.GetData(ope) + data);
                    return await npc.SaveAsync();
                }

                cmp = npc.GetData(ope);
                isInt = true;
            }
            else if (ope.Equals("datastr", StringComparison.InvariantCultureIgnoreCase))
            {
                if (opt == "=")
                {
                    npc.DataStr = strData;
                    return await npc.SaveAsync();
                }

                if (opt == "+=")
                {
                    npc.DataStr += strData;
                    return await npc.SaveAsync();
                }

                strCmp = npc.DataStr;
            }

            switch (opt)
            {
                case "==": return isInt && cmp == data || strCmp == strData;
                case ">=": return isInt && cmp >= data;
                case "<=": return isInt && cmp <= data;
                case ">":  return isInt && cmp > data;
                case "<":  return isInt && cmp < data;
            }

            return false;
        }

        private static async Task<bool> ExecuteActionNpcEraseAsync(DbAction action, string param, Character user,
                                                                   Role role,
                                                                   Item item, string input)
        {
            if (user == null)
                return false;

            BaseNpc npc = RoleManager.GetRole<BaseNpc>(user.InteractingNpc) ?? role as BaseNpc;
            if (npc == null)
                return false;

            if (action.Data == 0)
            {
                await npc.DelNpcAsync();
                user.InteractingNpc = 0;
                return true;
            }

            foreach (DynamicNpc del in RoleManager.QueryRoleByType<DynamicNpc>().Where(x => x.Type == action.Data))
                await del.DelNpcAsync();

            return true;
        }

        private static async Task<bool> ExecuteActionNpcResetsynownerAsync(
            DbAction action, string param, Character user,
            Role role, Item item, string input)
        {
            if (!(role is DynamicNpc npc))
                return false;

            if (npc.IsSynNpc() && !npc.Map.IsSynMap()
                || npc.IsCtfFlag() && !npc.Map.IsSynMap())
                return false;

            DynamicNpc.Score score = npc.GetTopScore();
            if (score != null)
            {
                Syndicate syn = SyndicateManager.GetSyndicate((int) score.Identity);
                if (npc.IsSynFlag() && syn != null)
                {
                    await RoleManager.BroadcastMsgAsync(string.Format(Language.StrWarWon, syn.Name),
                                                        TalkChannel.Center);
                    npc.Map.OwnerIdentity = syn.Identity;
                }
                else if (npc.IsCtfFlag())
                {
                    if (user?.IsPm() == true)
                        await user.SendAsync("CTF Flag is not handled");
                    return true;
                }

                if (syn != null) await npc.SetOwnerAsync(syn.Identity, true);

                npc.ClearScores();

                if (npc.Map.IsDynamicMap())
                    await npc.Map.SaveAsync();

                await npc.SaveAsync();
            }

            foreach (Character player in RoleManager.QueryRoleByMap<Character>(npc.MapIdentity))
            {
                //player.SetAttackTarget(null);
            }

            if (npc.IsSynFlag())
                foreach (BaseNpc resetNpc in RoleManager.QueryRoleByMap<BaseNpc>(npc.MapIdentity))
                {
                    if (resetNpc.IsSynFlag())
                        continue;

                    resetNpc.OwnerIdentity = npc.OwnerIdentity;
                    /*if (resetNpc.IsLinkNpc()) TODO
                    {
                        
                    }*/
                    await resetNpc.SaveAsync();
                }

            return true;
        }

        private static async Task<bool> ExecuteActionNpcFindNextTableAsync(
            DbAction action, string param, Character user,
            Role role, Item item, string input)
        {
            string[] splitParam = SplitParam(param);
            if (splitParam.Length < 4)
                return false;

            uint idNpc = uint.Parse(splitParam[0]);
            uint idMap = uint.Parse(splitParam[1]);
            ushort usMapX = ushort.Parse(splitParam[2]);
            ushort usMapY = ushort.Parse(splitParam[3]);

            var npc = RoleManager.GetRole<BaseNpc>(idNpc);
            if (npc == null)
                return false;

            npc.SetData("data0", (int) idMap);
            npc.SetData("data1", usMapX);
            npc.SetData("data2", usMapY);
            await npc.SaveAsync();
            return true;
        }

        private static async Task<bool> ExecuteActionNpcFamilyCreateAsync(DbAction action, string param, Character user,
                                                                          Role role, Item item, string input)
        {
            if (user == null || user.Family != null)
                return false;

            if (user.Level < 50 || user.Silvers < 500000)
                return false;

            return await user.CreateFamilyAsync(input, 500000);
        }

        private static async Task<bool> ExecuteActionNpcFamilyDestroyAsync(
            DbAction action, string param, Character user,
            Role role, Item item, string input)
        {
            if (user?.Family == null)
                return false;
            return await user.DisbandFamilyAsync();
        }

        #endregion

        #region Map

        private static async Task<bool> ExecuteActionMapMovenpcAsync(DbAction action, string param, Character user,
                                                                     Role role, Item item, string input)
        {
            string[] splitParam = SplitParam(param);
            if (splitParam.Length < 3)
                return false;

            uint idMap = uint.Parse(splitParam[0]);
            ushort nPosX = ushort.Parse(splitParam[1]), nPosY = ushort.Parse(splitParam[2]);

            if (idMap <= 0 || nPosX <= 0 || nPosY <= 0)
                return false;

            var npc = RoleManager.GetRole<BaseNpc>(action.Data);
            if (npc == null)
                return false;

            return await npc.ChangePosAsync(idMap, nPosX, nPosY);
        }

        private static async Task<bool> ExecuteActionMapMapuserAsync(DbAction action, string param, Character user,
                                                                     Role role, Item item, string input)
        {
            string[] splitParam = SplitParam(param);
            if (splitParam.Length < 3) return false;

            var amount = 0;

            if (splitParam[0].Equals("map_user", StringComparison.InvariantCultureIgnoreCase))
            {
                amount += MapManager.GetMap(action.Data)?.PlayerCount ?? 0;
            }
            else if (splitParam[0].Equals("alive_user", StringComparison.InvariantCultureIgnoreCase))
            {
                amount += RoleManager.QueryRoleByMap<Character>(action.Data).Count(x => x.IsAlive);
            }
            else
            {
                await Log.WriteLogAsync(LogLevel.Warning,
                                        $"ExecuteActionMapMapuser invalid cmd {splitParam[0]} for action {action.Identity}, {param}");
                return false;
            }

            switch (splitParam[1])
            {
                case "==":
                    return amount == int.Parse(splitParam[2]);
                case "<=":
                    return amount <= int.Parse(splitParam[2]);
                case ">=":
                    return amount >= int.Parse(splitParam[2]);
            }

            return false;
        }

        private static async Task<bool> ExecuteActionMapBrocastmsgAsync(DbAction action, string param, Character user,
                                                                        Role role, Item item, string input)
        {
            GameMap map = MapManager.GetMap(action.Data);
            if (map == null)
                return false;

            await map.BroadcastMsgAsync(param);
            return true;
        }

        private static async Task<bool> ExecuteActionMapDropitemAsync(DbAction action, string param, Character user,
                                                                      Role role, Item item, string input)
        {
            string[] splitParam = SplitParam(param);
            if (splitParam.Length < 4)
                return false;

            uint idMap = uint.Parse(splitParam[1]);
            uint idItemtype = uint.Parse(splitParam[0]);
            ushort x = ushort.Parse(splitParam[2]);
            ushort y = ushort.Parse(splitParam[3]);

            GameMap map = MapManager.GetMap(idMap);
            if (map == null)
                return false;

            var mapItem = new MapItem((uint) IdentityGenerator.MapItem.GetNextIdentity);
            if (await mapItem.CreateAsync(map, new Point(x, y), idItemtype, 0, 0, 0, 0, MapItem.DropMode.Common))
                await mapItem.EnterMapAsync();
            else
                return false;

            return true;
        }

        private static async Task<bool> ExecuteActionMapSetstatusAsync(DbAction action, string param, Character user,
                                                                       Role role, Item item, string input)
        {
            string[] splitParam = SplitParam(param);
            if (splitParam.Length < 3)
                return false;

            uint idMap = uint.Parse(splitParam[0]);
            byte dwStatus = byte.Parse(splitParam[1]);
            bool flag = splitParam[2] != "0";

            GameMap map = MapManager.GetMap(idMap);
            if (map == null)
                return false;

            await map.SetStatusAsync(dwStatus, flag);
            return true;
        }

        private static async Task<bool> ExecuteActionMapAttribAsync(DbAction action, string param, Character user,
                                                                    Role role,
                                                                    Item item, string input)
        {
            string[] splitParam = SplitParam(param);
            if (splitParam.Length < 3) return false;

            string szField = splitParam[0];
            string szOpt = splitParam[1];
            var x = 0;
            int data = int.Parse(splitParam[2]);
            uint idMap = 0;

            if (splitParam.Length >= 4)
                idMap = uint.Parse(splitParam[3]);

            GameMap map;
            if (idMap == 0)
            {
                if (user == null)
                    return false;
                map = MapManager.GetMap(user.MapIdentity);
            }
            else
            {
                map = MapManager.GetMap(idMap);
            }

            if (map == null)
                return false;

            if (szField.Equals("status", StringComparison.InvariantCultureIgnoreCase))
            {
                switch (szOpt.ToLowerInvariant())
                {
                    case "test":
                        return map.IsWarTime();
                    case "set":
                        await map.SetStatusAsync((ulong) data, true);
                        return true;
                    case "reset":
                        await map.SetStatusAsync((ulong) data, false);
                        return true;
                }
            }
            else if (szField.Equals("type", StringComparison.InvariantCultureIgnoreCase))
            {
                switch (szOpt.ToLowerInvariant())
                {
                    case "test":
                        return (map.Type & (ulong) data) != 0;
                }
            }
            else if (szField.Equals("mapdoc", StringComparison.InvariantCultureIgnoreCase))
            {
                if (szOpt.Equals("="))
                {
                    map.MapDoc = (uint) data;
                    await map.SaveAsync();
                    return true;
                }

                x = (int) map.MapDoc;
            }
            else if (szField.Equals("portal0_x", StringComparison.InvariantCultureIgnoreCase))
            {
                if (szOpt.Equals("="))
                {
                    map.PortalX = (ushort) data;
                    await map.SaveAsync();
                    return true;
                }

                x = map.PortalX;
            }
            else if (szField.Equals("portal0_y", StringComparison.InvariantCultureIgnoreCase))
            {
                if (szOpt.Equals("="))
                {
                    map.PortalY = (ushort) data;
                    await map.SaveAsync();
                    return true;
                }

                x = map.PortalY;
            }
            else if (szField.Equals("res_lev", StringComparison.InvariantCultureIgnoreCase))
            {
                if (szOpt.Equals("="))
                {
                    map.ResLev = (byte) data;
                    await map.SaveAsync();
                    return true;
                }

                x = map.ResLev;
            }
            else
            {
                await Log.WriteLogAsync(LogLevel.Warning,
                                        $"ExecuteActionMapAttrib invalid field {szField} for action {action.Identity}, {param}");
                return false;
            }

            switch (szOpt)
            {
                case "==": return x == data;
                case ">=": return x >= data;
                case "<=": return x <= data;
                case "<":  return x < data;
                case ">":  return x > data;
            }

            return false;
        }

        private static async Task<bool> ExecuteActionMapRegionMonsterAsync(
            DbAction action, string param, Character user,
            Role role, Item item, string input)
        {
            string[] splitParam = SplitParam(param);
            if (splitParam.Length < 8)
            {
                await Log.WriteLogAsync(LogLevel.Warning,
                                        $"ERROR: Invalid param amount on actionid: [{action.Identity}]");
                return false;
            }

            string szOpt = splitParam[6];
            uint idMap = uint.Parse(splitParam[0]);
            uint idType = uint.Parse(splitParam[5]);
            ushort nRegionX = ushort.Parse(splitParam[1]),
                   nRegionY = ushort.Parse(splitParam[2]),
                   nRegionCX = ushort.Parse(splitParam[3]),
                   nRegionCY = ushort.Parse(splitParam[4]);
            int nData = int.Parse(splitParam[7]);

            GameMap map;
            if (idMap == 0)
            {
                if (user == null)
                    return false;

                map = user.Map;
            }
            else
            {
                map = MapManager.GetMap(idMap);
            }

            if (map == null)
                return false;

            int count = RoleManager.QueryRoleByMap<Monster>(idMap).Count(x =>
                                                                             (idType != 0 && x.Type == idType ||
                                                                              idType == 0) && x.MapX >= nRegionX &&
                                                                             x.MapX < nRegionX - nRegionCX
                                                                             && x.MapY >= nRegionY &&
                                                                             x.MapY < nRegionY - nRegionCY);

            switch (szOpt)
            {
                case "==": return count == nData;
                case "<=": return count <= nData;
                case ">=": return count >= nData;
                case "<":  return count < nData;
                case ">":  return count > nData;
            }

            return false;
        }

        private static async Task<bool> ExecuteActionMapRandDropItemAsync(DbAction action, string param, Character user,
                                                                          Role role, Item item, string input)
        {
            // Example: 728006 3030 186 187 304 307 250 3600
            //          ItemID MAP  X   Y   CX  CY  AMOUNT DURATION
            string[] splitParam = SplitParam(param, 8);
            if (splitParam.Length != 8)
            {
                await Log.WriteLogAsync(LogLevel.Warning,
                                        $"ExecuteActionMapRandDropItem: ItemID MAP  X   Y   CX  CY  AMOUNT DURATION :: {param} ({action.Identity})");
                return false;
            }

            uint idItemtype = uint.Parse(splitParam[0]); // the item to be dropped
            uint idMap = uint.Parse(splitParam[1]);      // the map
            ushort initX = ushort.Parse(splitParam[2]);  // start coordinates
            ushort initY = ushort.Parse(splitParam[3]);  // start coordinates 
            ushort endX = ushort.Parse(splitParam[4]);   // end coordinates
            ushort endY = ushort.Parse(splitParam[5]);   // end coordinates
            int amount = int.Parse(splitParam[6]);       // amount of items to be dropped
            int duration = int.Parse(splitParam[7]);     // duration of the item in the floor

            DbItemtype itemtype = ItemManager.GetItemtype(idItemtype);
            if (itemtype == null)
            {
                await Log.WriteLogAsync(LogLevel.Warning, $"Invalid itemtype {idItemtype}, {param}, {action.Identity}");
                return false;
            }

            GameMap map = MapManager.GetMap(idMap);
            if (map == null)
            {
                await Log.WriteLogAsync(LogLevel.Warning, $"Invalid map {idMap}, {param}, {action.Identity}");
                return false;
            }

            for (var i = 0; i < amount; i++)
            {
                var mapItem = new MapItem((uint) IdentityGenerator.MapItem.GetNextIdentity);
                var positionRetry = 0;
                var posSuccess = true;

                int deltaX = endX - initX;
                int deltaY = endY - initY;

                int targetX = initX + await Kernel.NextAsync(deltaX);
                int targetY = initY + await Kernel.NextAsync(deltaY);

                var pos = new Point(targetX, targetY);
                while (!map.FindDropItemCell(9, ref pos))
                {
                    if (positionRetry++ >= 5)
                    {
                        posSuccess = false;
                        break;
                    }

                    targetX = initX + await Kernel.NextAsync(deltaX);
                    targetY = initY + await Kernel.NextAsync(deltaY);

                    pos = new Point(targetX, targetY);
                }

                if (!posSuccess)
                    continue;

                if (!await mapItem.CreateAsync(map, pos, idItemtype, 0, 0, 0, 0, MapItem.DropMode.Common))
                    continue;

                mapItem.SetAliveTimeout(duration);
                await mapItem.EnterMapAsync();
            }

            return true;
        }

        private static async Task<bool> ExecuteActionMapChangeweatherAsync(
            DbAction action, string param, Character user,
            Role role, Item item, string input)
        {
            string[] pszParam = SplitParam(param);
            if (pszParam.Length < 5) return false;

            int nType = int.Parse(pszParam[0]), nIntensity = int.Parse(pszParam[1]), nDir = int.Parse(pszParam[2]);
            uint dwColor = uint.Parse(pszParam[3]), dwKeepSecs = uint.Parse(pszParam[4]);

            GameMap map;
            if (action.Data == 0)
            {
                if (user == null)
                    return false;
                map = user.Map;
            }
            else
            {
                map = MapManager.GetMap(action.Data);
            }

            if (map == null)
                return false;

            await map.Weather.SetNewWeatherAsync((Weather.WeatherType) nType, nIntensity, nDir, (int) dwColor,
                                                 (int) dwKeepSecs, 0);
            await map.Weather.SendWeatherAsync();
            return true;
        }

        private static async Task<bool> ExecuteActionMapChangelightAsync(DbAction action, string param, Character user,
                                                                         Role role, Item item, string input)
        {
            string[] splitParam = SplitParam(param);
            if (splitParam.Length < 2) return false;

            uint idMap = uint.Parse(splitParam[0]), dwRgb = uint.Parse(splitParam[1]);

            GameMap map;
            if (action.Data == 0)
            {
                if (user == null)
                    return false;
                map = user.Map;
            }
            else
            {
                map = MapManager.GetMap(idMap);
            }

            if (map == null)
                return false;

            map.Light = dwRgb;
            await map.BroadcastMsgAsync(new MsgAction
            {
                Identity = 1,
                Command = dwRgb,
                Argument = 0,
                Action = MsgAction<Client>.ActionType.MapArgb
            });
            return true;
        }

        private static async Task<bool> ExecuteActionMapMapeffectAsync(DbAction action, string param, Character user,
                                                                       Role role, Item item, string input)
        {
            string[] splitParam = SplitParam(param);

            if (splitParam.Length < 4) return false;

            uint idMap = uint.Parse(splitParam[0]);
            ushort posX = ushort.Parse(splitParam[1]), posY = ushort.Parse(splitParam[2]);
            string szEffect = splitParam[3];

            GameMap map = MapManager.GetMap(idMap);
            if (map == null)
                return false;

            await map.BroadcastRoomMsgAsync(posX, posY, new MsgName
            {
                Identity = 0,
                Action = StringAction.MapEffect,
                X = posX,
                Y = posY,
                Strings = new List<string>
                {
                    szEffect
                }
            });
            return true;
        }

        private static async Task<bool> ExecuteActionMapFireworksAsync(DbAction action, string param, Character user,
                                                                       Role role, Item item, string input)
        {
            if (user != null)
                await user.BroadcastRoomMsgAsync(new MsgName
                {
                    Identity = user.Identity,
                    Action = StringAction.Fireworks
                }, true);

            return true;
        }

        #endregion

        #region Lay Item

        private static async Task<bool> ExecuteActionItemRequestlaynpcAsync(
            DbAction action, string param, Character user,
            Role role, Item item, string input)
        {
            if (user == null)
                return false;

            string[] splitParams = SplitParam(param, 5);

            uint idNextTask = uint.Parse(splitParams[0]);
            uint dwType = uint.Parse(splitParams[1]);
            uint dwSort = uint.Parse(splitParams[2]);
            uint dwLookface = uint.Parse(splitParams[3]);
            uint dwRegion = 0;

            if (splitParams.Length > 4)
                uint.TryParse(splitParams[4], out dwRegion);

            if (idNextTask != 0)
                user.InteractingItem = idNextTask;

            await user.SendAsync(new MsgNpc
            {
                Identity = dwRegion,
                Data = dwLookface,
                Event = (ushort) dwType,
                RequestType = MsgNpc<Client>.NpcActionType.LayNpc
            });
            return true;
        }

        private static async Task<bool> ExecuteActionItemLaynpcAsync(DbAction action, string param, Character user,
                                                                     Role role, Item item, string input)
        {
            if (string.IsNullOrEmpty(input))
                return false;

            string[] splitParam = SplitParam(input, 5);
            if (splitParam.Length < 3)
            {
                await Log.WriteLogAsync(LogLevel.Error, $"Invalid input count for action [{action.Identity}]: {input}");
                return false;
            }

            if (!ushort.TryParse(splitParam[0], out ushort mapX)
                || !ushort.TryParse(splitParam[1], out ushort mapY)
                || !uint.TryParse(splitParam[2], out uint lookface))
            {
                await Log.WriteLogAsync(LogLevel.Error,
                                        $"Invalid input params for action [{action.Identity}]1: {input}");
                return false;
            }

            uint frame = 0;
            uint pose = 0;
            if (splitParam.Length >= 4)
            {
                uint.TryParse(splitParam[3], out frame);
                uint.TryParse(splitParam[4], out pose);
            }

            if (user.Map.IsSuperPosition(mapX, mapY))
            {
                await user.SendAsync(Language.StrLayNpcSuperPosition);
                return false;
            }

            splitParam = SplitParam(param, 21);

            if (param.Length < 5) return false;

            uint nRegionType = 0;
            string szName = splitParam[0];
            ushort usType = ushort.Parse(splitParam[1]);
            ushort usSort = ushort.Parse(splitParam[2]);
            uint dwOwnerType = uint.Parse(splitParam[4]);
            uint dwLife = 0;
            uint idBase = 0;
            uint idLink = 0;
            uint idTask0 = 0;
            uint idTask1 = 0;
            uint idTask2 = 0;
            uint idTask3 = 0;
            uint idTask4 = 0;
            uint idTask5 = 0;
            uint idTask6 = 0;
            uint idTask7 = 0;
            var idData0 = 0;
            var idData1 = 0;
            var idData2 = 0;
            var idData3 = 0;

            if (splitParam.Length >= 6)
                dwLife = uint.Parse(splitParam[5]);
            if (splitParam.Length >= 7)
                nRegionType = uint.Parse(splitParam[6]);
            if (splitParam.Length >= 8)
                idBase = uint.Parse(splitParam[7]);
            if (splitParam.Length >= 9)
                idLink = uint.Parse(splitParam[8]);
            if (splitParam.Length >= 10)
                idTask0 = uint.Parse(splitParam[9]);
            if (splitParam.Length >= 11)
                idTask1 = uint.Parse(splitParam[10]);
            if (splitParam.Length >= 12)
                idTask2 = uint.Parse(splitParam[11]);
            if (splitParam.Length >= 13)
                idTask3 = uint.Parse(splitParam[12]);
            if (splitParam.Length >= 14)
                idTask4 = uint.Parse(splitParam[13]);
            if (splitParam.Length >= 15)
                idTask5 = uint.Parse(splitParam[14]);
            if (splitParam.Length >= 16)
                idTask6 = uint.Parse(splitParam[15]);
            if (splitParam.Length >= 17)
                idTask7 = uint.Parse(splitParam[16]);
            if (splitParam.Length >= 18)
                idData0 = int.Parse(splitParam[17]);
            if (splitParam.Length >= 19)
                idData1 = int.Parse(splitParam[18]);
            if (splitParam.Length >= 20)
                idData2 = int.Parse(splitParam[19]);
            if (splitParam.Length >= 21)
                idData3 = int.Parse(splitParam[20]);

            if (usType == BaseNpc.SYNTRANS_NPC && user.Map.IsTeleportDisable())
            {
                _ = user.SendAsync(Language.StrLayNpcSynTransInvalidMap);
                return false;
            }

            if (usType == BaseNpc.STATUARY_NPC)
            {
                szName = user.Name;
                lookface = user.Mesh % 10;
                idTask0 = user.Headgear?.Type ?? 0;
                idTask1 = user.Armor?.Type ?? 0;
                idTask2 = user.RightHand?.Type ?? 0;
                idTask3 = user.LeftHand?.Type ?? 0;
                idTask4 = frame;
                idTask5 = pose;
                idTask6 = user.Mesh;
                idTask7 = ((uint) user.SyndicateRank << 16) + user.Hairstyle;
            }

            if (nRegionType > 0 && !user.Map.QueryRegion((RegionTypes) nRegionType, mapX, mapY))
                return false;

            uint idOwner = 0;
            switch (dwOwnerType)
            {
                case 1:
                    if (user.Identity == 0)
                        return false;

                    idOwner = user.Identity;
                    break;
                case 2:
                    if (user.SyndicateIdentity == 0)
                        return false;

                    idOwner = user.SyndicateIdentity;
                    break;
            }

            DynamicNpc npc = user.Map.QueryStatuary(user, lookface, idTask0) ?? RoleManager
                                 .QueryRoleByType<DynamicNpc>().FirstOrDefault(x => x.LinkId == idLink);
            if (npc == null)
            {
                npc = new DynamicNpc(new DbDynanpc
                {
                    Name = szName,
                    Ownerid = idOwner,
                    OwnerType = dwOwnerType,
                    Type = usType,
                    Sort = usSort,
                    Life = dwLife,
                    Maxlife = dwLife,
                    Base = idBase,
                    Linkid = idLink,
                    Task0 = idTask0,
                    Task1 = idTask1,
                    Task2 = idTask2,
                    Task3 = idTask3,
                    Task4 = idTask4,
                    Task5 = idTask5,
                    Task6 = idTask6,
                    Task7 = idTask7,
                    Data0 = idData0,
                    Data1 = idData1,
                    Data2 = idData2,
                    Data3 = idData3,
                    Datastr = "",
                    Defence = 0,
                    Cellx = mapX,
                    Celly = mapY,
                    Idxserver = 0,
                    Itemid = 0,
                    Lookface = (ushort) lookface,
                    MagicDef = 0,
                    Mapid = user.MapIdentity
                });

                if (!await npc.InitializeAsync())
                    return false;
            }
            else
            {
                npc.SetType(usType);
                // npc.OwnerIdentity = idOwner;
                npc.OwnerType = (byte) dwOwnerType;
                await npc.SetOwnerAsync(idOwner);
                npc.Name = szName;
                await npc.SetAttributesAsync(ClientUpdateType.Mesh, lookface);
                npc.SetSort(usSort);
                npc.SetTask(0, idTask0);
                npc.SetTask(1, idTask1);
                npc.SetTask(2, idTask2);
                npc.SetTask(3, idTask3);
                npc.SetTask(4, idTask4);
                npc.SetTask(5, idTask5);
                npc.SetTask(6, idTask6);
                npc.SetTask(7, idTask7);
                npc.Data0 = idData0;
                npc.Data1 = idData1;
                npc.Data2 = idData2;
                npc.Data3 = idData3;
                await npc.SetAttributesAsync(ClientUpdateType.MaxHitpoints, dwLife);
                npc.MapX = mapX;
                npc.MapY = mapY;
            }

            await npc.ChangePosAsync(user.MapIdentity, mapX, mapY);
            await npc.SaveAsync();

            role = npc;
            user.InteractingNpc = npc.Identity;
            return true;
        }

        private static async Task<bool> ExecuteActionItemDelthisAsync(DbAction action, string param, Character user,
                                                                      Role role, Item item, string input)
        {
            user.InteractingItem = 0;
            if (item != null) _ = user.UserPackage.SpendItemAsync(item);

            _ = user.SendAsync(Language.StrUseItem);
            return true;
        }

        #endregion

        #region Item

        private static async Task<bool> ExecuteActionItemAddAsync(DbAction action, string param, Character user,
                                                                  Role role,
                                                                  Item item, string input)
        {
            if (user?.UserPackage == null)
                return false;

            if (!user.UserPackage.IsPackSpare(1))
                return false;

            DbItemtype itemtype = ItemManager.GetItemtype(action.Data);
            if (itemtype == null)
            {
                await Log.WriteLogAsync(LogLevel.Warning,
                                        $"Invalid itemtype: {action.Identity}, {action.Type}, {action.Data}");
                return false;
            }

            string[] splitParam = SplitParam(param);
            DbItem newItem = Item.CreateEntity(action.Data);
            newItem.PlayerId = user.Identity;

            for (var i = 0; i < splitParam.Length; i++)
            {
                if (!int.TryParse(splitParam[i], out int value))
                    continue;

                switch (i)
                {
                    case 0: // amount
                        if (value > 0)
                            newItem.Amount = (ushort) Math.Min(value, ushort.MaxValue);
                        break;
                    case 1: // amount limit
                        if (value > 0)
                            newItem.AmountLimit = (ushort) Math.Min(value, ushort.MaxValue);
                        break;
                    case 2: // socket progress
                        newItem.Data = (uint) Math.Min(value, ushort.MaxValue);
                        break;
                    case 3: // gem 1
                        if (Enum.IsDefined(typeof(Item.SocketGem), (byte) value))
                            newItem.Gem1 = (byte) value;
                        break;
                    case 4: // gem 2
                        if (Enum.IsDefined(typeof(Item.SocketGem), (byte) value))
                            newItem.Gem2 = (byte) value;
                        break;
                    case 5: // effect magic 1
                        if (Enum.IsDefined(typeof(Item.ItemEffect), (ushort) value))
                            newItem.Magic1 = (byte) value;
                        break;
                    case 6: // magic 2
                        newItem.Magic2 = (byte) value;
                        break;
                    case 7: // magic 3
                        newItem.Magic3 = (byte) value;
                        break;
                    case 8: // reduce dmg
                        newItem.ReduceDmg = (byte) Math.Min(byte.MaxValue, value);
                        break;
                    case 9: // add life
                        newItem.AddLife = (byte) Math.Min(byte.MaxValue, value);
                        break;
                    case 10: // plunder
                        newItem.Plunder = null;
                        break;
                    case 11: // color
                        if (Enum.IsDefined(typeof(Item.ItemColor), (byte) value))
                            newItem.Color = (byte) value;
                        break;
                    case 12: // monopoly
                        newItem.Monopoly = (byte) Math.Min(byte.MaxValue, Math.Max(0, value));
                        break;
                    case 13: // mount color
                        newItem.Data = (uint) value;
                        newItem.ReduceDmg = (byte)(value & 0xFF);
                        newItem.AddLife = (byte)((value >> 8) & 0xFF);
                        newItem.AntiMonster = (byte)((value >> 16) & 0xFF);
                        break;
                    case 17: // Accumulate Num
                        newItem.AccumulateNum = (uint) value;
                        break;
                }
            }

            item = new Item(user);
            if (!await item.CreateAsync(newItem))
                return false;

            return await user.UserPackage.AddItemAsync(item);
        }

        private static async Task<bool> ExecuteActionItemDelAsync(DbAction action, string param, Character user,
                                                                  Role role,
                                                                  Item item, string input)
        {
            if (user?.UserPackage == null)
                return false;

            if (action.Data != 0)
                return await user.UserPackage.MultiSpendItemAsync(action.Data, action.Data, 1);

            if (!string.IsNullOrEmpty(param))
            {
                item = user.UserPackage[param];
                if (item == null)
                    return false;
                return await user.UserPackage.SpendItemAsync(item);
            }

            return false;
        }

        private static async Task<bool> ExecuteActionItemCheckAsync(DbAction action, string param, Character user,
                                                                    Role role,
                                                                    Item item, string input)
        {
            if (user?.UserPackage == null)
                return false;

            if (action.Data != 0)
                return user.UserPackage.MultiCheckItem(action.Data, action.Data, 1);

            if (!string.IsNullOrEmpty(param)) return user.UserPackage[param] != null;

            return false;
        }

        private static async Task<bool> ExecuteActionItemHoleAsync(DbAction action, string param, Character user,
                                                                   Role role,
                                                                   Item item, string input)
        {
            if (user?.UserPackage == null)
                return false;

            string[] splitParam = SplitParam(param, 2);
            if (param.Length < 2)
            {
                await Log.WriteLogAsync(LogLevel.Error,
                                        $"ExecuteActionItemHole invalid [{param}] param split length for action {action.Identity}");
                return false;
            }

            string opt = splitParam[0];
            if (!int.TryParse(splitParam[1], out int value))
            {
                await Log.WriteLogAsync(LogLevel.Error,
                                        $"ExecuteActionItemHole invalid value number [{param}] for action {action.Identity}");
                return false;
            }

            Item target = user.UserPackage[Item.ItemPosition.RightHand];
            if (target == null)
            {
                await user.SendAsync(Language.StrItemErrRepairItem);
                return false;
            }

            if (opt.Equals("chkhole", StringComparison.InvariantCultureIgnoreCase))
            {
                if (value == 1)
                    return target.SocketOne > Item.SocketGem.NoSocket;
                if (value == 2)
                    return target.SocketTwo > Item.SocketGem.NoSocket;
                return false;
            }

            if (opt.Equals("makehole", StringComparison.InvariantCultureIgnoreCase))
            {
                if (value == 1 && target.SocketOne == Item.SocketGem.NoSocket)
                    target.SocketOne = Item.SocketGem.EmptySocket;
                else if (value == 2 && target.SocketTwo == Item.SocketGem.NoSocket)
                    target.SocketTwo = Item.SocketGem.EmptySocket;
                else
                    return false;

                await user.SendAsync(new MsgItemInfo(target, MsgItemInfo<Client>.ItemMode.Update));
                await target.SaveAsync();
                return true;
            }

            return false;
        }

        private static async Task<bool> ExecuteActionItemMultidelAsync(DbAction action, string param, Character user,
                                                                       Role role, Item item, string input)
        {
            if (user?.UserPackage == null)
                return false;

            string[] splitParams = SplitParam(param);
            var first = 0;
            var last = 0;
            byte amount = 0;

            if (action.Data == Item.TYPE_METEOR)
            {
                if (!byte.TryParse(splitParams[0], out amount))
                    amount = 1;
                if (splitParams.Length <= 1 || !int.TryParse(splitParams[1], out first))
                    first = 0; // bound check
                // todo set meteor bind check
                return await user.UserPackage.SpendMeteorsAsync(amount);
            }

            if (action.Data == Item.TYPE_DRAGONBALL)
            {
                if (!byte.TryParse(splitParams[0], out amount))
                    amount = 1;
                if (splitParams.Length <= 1 || !int.TryParse(splitParams[1], out first))
                    first = 0;
                return await user.UserPackage.SpendDragonBallsAsync(amount, first != 0);
            }

            if (action.Data != 0)
                return false; // only Mets and DBs are supported

            if (splitParams.Length < 3)
                return false; // invalid format

            first = int.Parse(splitParams[0]);
            last = int.Parse(splitParams[1]);
            amount = byte.Parse(splitParams[2]);

            if (splitParams.Length < 4)
                return await user.UserPackage.MultiSpendItemAsync((uint) first, (uint) last, amount, true);
            return await user.UserPackage.MultiSpendItemAsync((uint) first, (uint) last, amount,
                                                              int.Parse(splitParams[3]) != 0);
        }

        private static async Task<bool> ExecuteActionItemMultichkAsync(DbAction action, string param, Character user,
                                                                       Role role, Item item, string input)
        {
            if (user?.UserPackage == null)
                return false;

            string[] splitParams = SplitParam(param);
            var first = 0;
            var last = 0;
            byte amount = 0;

            if (action.Data == Item.TYPE_METEOR)
            {
                if (!byte.TryParse(splitParams[0], out amount))
                    amount = 1;
                if (splitParams.Length <= 1 || !int.TryParse(splitParams[1], out first))
                    first = 0; // bound check
                // todo set meteor bind check
                return user.UserPackage.MeteorAmount() >= amount;
            }

            if (action.Data == Item.TYPE_DRAGONBALL)
            {
                if (!byte.TryParse(splitParams[0], out amount))
                    amount = 1;
                if (splitParams.Length <= 1 || !int.TryParse(splitParams[1], out first))
                    first = 0;
                return user.UserPackage.DragonBallAmount(first != 0) >= amount;
            }

            if (action.Data != 0)
                return false; // only Mets and DBs are supported

            if (splitParams.Length < 3)
                return false; // invalid format

            first = int.Parse(splitParams[0]);
            last = int.Parse(splitParams[1]);
            amount = byte.Parse(splitParams[2]);

            if (splitParams.Length < 4)
                return user.UserPackage.MultiCheckItem((uint) first, (uint) last, amount, true);
            return user.UserPackage.MultiCheckItem((uint) first, (uint) last, amount, int.Parse(splitParams[3]) != 0);
        }

        private static async Task<bool> ExecuteActionItemLeavespaceAsync(DbAction action, string param, Character user,
                                                                         Role role, Item item, string input)
        {
            return user?.UserPackage != null && user.UserPackage.IsPackSpare((int) action.Data);
        }

        private static async Task<bool> ExecuteActionItemUpequipmentAsync(DbAction action, string param, Character user,
                                                                          Role role, Item item, string input)
        {
            if (user?.UserPackage == null)
                return false;

            string[] pszParam = SplitParam(param);
            if (pszParam.Length < 2)
                return false;

            string szCmd = pszParam[0];
            byte nPos = byte.Parse(pszParam[1]);

            Item pItem = user.UserPackage[(Item.ItemPosition) nPos];
            if (pItem == null)
                return false;

            switch (szCmd)
            {
                case "up_lev":
                {
                    return await pItem.UpEquipmentLevelAsync();
                }

                case "recover_dur":
                {
                    var szPrice = (uint) pItem.GetRecoverDurCost();
                    return await user.SpendMoneyAsync((int) szPrice) && await pItem.RecoverDurabilityAsync();
                }

                case "up_levultra":
                case "up_levultra2":
                {
                    return await pItem.UpUltraEquipmentLevelAsync();
                }

                case "up_quality":
                {
                    return await pItem.UpItemQualityAsync();
                }

                default:
                    await Log.WriteLogAsync(LogLevel.Warning, $"ERROR: [509] [0] [{param}] not properly handled.");
                    return false;
            }
        }

        private static async Task<bool> ExecuteActionItemEquiptestAsync(DbAction action, string param, Character user,
                                                                        Role role, Item item, string input)
        {
            if (user?.UserPackage == null)
                return false;

            /* param: position type opt value (4 quality == 9) */
            string[] pszParam = SplitParam(param);
            if (pszParam.Length < 4)
                return false;

            byte nPosition = byte.Parse(pszParam[0]);
            string szCmd = pszParam[1];
            string szOpt = pszParam[2];
            int nData = int.Parse(pszParam[3]);

            Item pItem = user.UserPackage[(Item.ItemPosition) nPosition];
            if (pItem == null)
                return false;

            var nTestData = 0;
            switch (szCmd)
            {
                case "level":
                    nTestData = pItem.GetLevel();
                    break;
                case "quality":
                    nTestData = pItem.GetQuality();
                    break;
                case "durability":
                    if (nData == -1)
                        nData = pItem.MaximumDurability / 100;
                    nTestData = pItem.MaximumDurability / 100;
                    break;
                case "max_dur":
                {
                    if (nData == -1)
                        nData = pItem.Itemtype.AmountLimit / 100;
                    // TODO Kylin Gem Support
                    nTestData = pItem.MaximumDurability / 100;
                    break;
                }

                default:
                    await Log.WriteLogAsync(LogLevel.Warning, $"ACTION: EQUIPTEST error {szCmd}");
                    return false;
            }

            if (szOpt == "==")
                return nTestData == nData;
            if (szOpt == "<=")
                return nTestData <= nData;
            if (szOpt == ">=")
                return nTestData >= nData;
            return false;
        }

        private static async Task<bool> ExecuteActionItemEquipexistAsync(DbAction action, string param, Character user,
                                                                         Role role, Item item, string input)
        {
            if (user?.UserPackage == null)
                return false;

            string[] @params = SplitParam(param);
            if (param.Length >= 1 && user.UserPackage[(Item.ItemPosition) action.Data] != null)
                return user.UserPackage[(Item.ItemPosition) action.Data].GetItemSubType() == ushort.Parse(@params[0]);
            return user.UserPackage[(Item.ItemPosition) action.Data] != null;
        }

        private static async Task<bool> ExecuteActionItemEquipcolorAsync(DbAction action, string param, Character user,
                                                                         Role role, Item item, string input)
        {
            if (user?.UserPackage == null)
                return false;

            string[] pszParam = SplitParam(param);

            if (pszParam.Length < 2)
                return false;
            if (!Enum.IsDefined(typeof(Item.ItemColor), byte.Parse(pszParam[1])))
                return false;

            Item pItem = user.UserPackage[(Item.ItemPosition) byte.Parse(pszParam[0])];
            if (pItem == null)
                return false;

            Item.ItemPosition pos = pItem.GetPosition();
            if (pos != Item.ItemPosition.Armor
                && pos != Item.ItemPosition.Headwear
                && (pos != Item.ItemPosition.LeftHand || pItem.GetItemSort() != Item.ItemSort.ItemsortWeaponShield))
                return false;

            pItem.Color = (Item.ItemColor) byte.Parse(pszParam[1]);
            await pItem.SaveAsync();
            await user.SendAsync(new MsgItemInfo(pItem, MsgItemInfo<Client>.ItemMode.Update));
            return true;
        }

        private static async Task<bool> ExecuteActionItemCheckrandAsync(DbAction action, string param, Character user,
                                                                        Role role, Item item, string input)
        {
            if (user?.UserPackage == null)
                return false;

            string[] pszParam = SplitParam(param);
            if (pszParam.Length < 6)
                return false;

            byte initValue = byte.Parse(pszParam[3]), endValue = byte.Parse(pszParam[5]);

            var lPos = new List<Item.ItemPosition>(15);

            byte pIdx = byte.Parse(pszParam[1]);

            if (initValue == 0 && pIdx == 14)
                initValue = 1;

            for (var i = Item.ItemPosition.EquipmentBegin; i <= Item.ItemPosition.EquipmentEnd; i++)
                if (user.UserPackage[i] != null)
                {
                    if (pIdx == 14 && user.UserPackage[i].Position == Item.ItemPosition.Steed)
                        continue;

                    if (user.UserPackage[i].IsArrowSort())
                        continue;

                    switch (pIdx)
                    {
                        case 14:
                            if (user.UserPackage[i].ReduceDamage >= initValue
                                && user.UserPackage[i].ReduceDamage <= endValue)
                                continue;
                            break;
                    }

                    lPos.Add(i);
                }

            byte pos = 0;

            if (lPos.Count > 0)
                pos = (byte) lPos[await Kernel.NextAsync(lPos.Count) % lPos.Count];

            if (pos == 0)
                return false;

            Item pItem = user.UserPackage[(Item.ItemPosition) pos];
            if (pItem == null)
                return false;

            byte pPos = byte.Parse(pszParam[0]);
            string opt = pszParam[2];

            switch (pIdx)
            {
                case 14: // bless
                    user.VarData[7] = pos;
                    return true;
                default:
                    await Log.WriteLogAsync(LogLevel.Warning, $"ACTION: 516: {pIdx} not handled id: {action.Identity}");
                    break;
            }

            return false;
        }

        private static async Task<bool> ExecuteActionItemModifyAsync(DbAction action, string param, Character user,
                                                                     Role role, Item item, string input)
        {
            if (user?.UserPackage == null)
                return false;

            // structure param:
            // pos  type    action  value   update
            // 1    7       ==      1       1
            // pos = Item Position
            // type = 7 Reduce Damage
            // action = Operator == or set
            // value = value lol
            // update = if the client will update live

            string[] pszParam = SplitParam(param);
            if (pszParam.Length < 5)
            {
                await Log.WriteLogAsync(LogLevel.Error,
                                        $"ACTION: incorrect param, pos type action value update, for action (id:{action.Identity})");
                return false;
            }

            int pos = int.Parse(pszParam[0]);
            int type = int.Parse(pszParam[1]);
            string opt = pszParam[2];
            long value = int.Parse(pszParam[3]);
            bool update = int.Parse(pszParam[4]) > 0;

            Item pItem = user.UserPackage[(Item.ItemPosition) pos];
            if (pItem == null)
            {
                await user.SendAsync(Language.StrUnableToUseItem);
                return false;
            }

            switch (type)
            {
                case 1: // itemtype
                {
                    if (opt == "set")
                    {
                        DbItemtype itemt = ItemManager.GetItemtype((uint) value);
                        if (itemt == null)
                        {
                            // new item doesnt exist
                            await Log.WriteLogAsync(LogLevel.Error,
                                                    $"ACTION: itemtype not found (type:{value}, action:{action.Identity})");
                            return false;
                        }

                        if (pItem.Type / 1000 != itemt.Type / 1000)
                        {
                            await Log.WriteLogAsync(LogLevel.Error,
                                                    $"ACTION: cant change to different type (type:{pItem.Type}, new:{value}, action:{action.Identity})");
                            return false;
                        }

                        if (!await pItem.ChangeTypeAsync(itemt.Type))
                            return false;
                    }
                    else if (opt == "==")
                    {
                        return pItem.Type == value;
                    }
                    else if (opt == "<")
                    {
                        return pItem.Type < value;
                    }
                    else
                    {
                        return false;
                    }

                    break;
                }

                case 2: // owner id
                case 3: // player id
                    return false;
                case 4: // dura
                {
                    if (opt == "set")
                    {
                        if (value > ushort.MaxValue)
                            value = ushort.MaxValue;
                        else if (value < 0)
                            value = 0;

                        pItem.Durability = (ushort) value;
                    }
                    else if (opt == "==")
                    {
                        return pItem.Durability == value;
                    }
                    else if (opt == "<")
                    {
                        return pItem.Durability < value;
                    }
                    else
                    {
                        return false;
                    }

                    break;
                }

                case 5: // max dura
                {
                    if (opt == "set")
                    {
                        if (value > ushort.MaxValue)
                            value = ushort.MaxValue;
                        else if (value < 0)
                            value = 0;

                        if (value < pItem.Durability)
                            pItem.Durability = (ushort) value;

                        pItem.MaximumDurability = (ushort) value;
                    }
                    else if (opt == "==")
                    {
                        return pItem.MaximumDurability == value;
                    }
                    else if (opt == "<")
                    {
                        return pItem.MaximumDurability < value;
                    }
                    else
                    {
                        return false;
                    }

                    break;
                }

                case 6:
                case 7: // position
                {
                    return false;
                }

                case 8: // gem1
                {
                    if (opt == "set")
                        pItem.SocketOne = (Item.SocketGem) value;
                    else if (opt == "==")
                        return pItem.SocketOne == (Item.SocketGem) value;
                    else if (opt == "<")
                        return pItem.SocketOne < (Item.SocketGem) value;
                    else
                        return false;

                    break;
                }

                case 9: // gem2
                {
                    if (opt == "set")
                        pItem.SocketTwo = (Item.SocketGem) value;
                    else if (opt == "==")
                        return pItem.SocketTwo == (Item.SocketGem) value;
                    else if (opt == "<")
                        return pItem.SocketTwo < (Item.SocketGem) value;
                    else
                        return false;

                    break;
                }

                case 10: // magic1
                {
                    if (opt == "set")
                    {
                        if (value is < 200 or > 203)
                            return false;
                        pItem.Effect = (Item.ItemEffect) value;
                    }
                    else if (opt == "==")
                    {
                        return pItem.Effect == (Item.ItemEffect) value;
                    }
                    else
                    {
                        return false;
                    }

                    break;
                }

                case 11: // magic2
                    return false;
                case 12: // magic3
                {
                    if (opt == "set")
                    {
                        if (value < 0)
                            value = 0;
                        else if (value > 12)
                            value = 12;

                        pItem.ChangeAddition((byte) value);
                    }
                    else if (opt == "==")
                    {
                        return pItem.Plus == value;
                    }
                    else if (opt == "<")
                    {
                        return pItem.Plus < value;
                    }
                    else
                    {
                        return false;
                    }

                    break;
                }

                case 13: // data
                {
                    if (opt == "set")
                    {
                        if (value < 0)
                            value = 0;
                        else if (value > 20000)
                            value = 20000;

                        pItem.SocketProgress = (ushort)value;
                    }
                    else if (opt == "==")
                    {
                        return pItem.SocketProgress == value;
                    }
                    else if (opt == "<")
                    {
                        return pItem.SocketProgress < value;
                    }
                    else
                    {
                        return false;
                    }

                    break;
                }

                case 14: // reduce damage
                {
                    if (opt == "set")
                    {
                        if (value < 0)
                            value = 0;
                        else if (value > 7)
                            value = 7;

                        pItem.ReduceDamage = (byte) value;
                    }
                    else if (opt == "==")
                    {
                        return pItem.ReduceDamage == value;
                    }
                    else if (opt == "<")
                    {
                        return pItem.ReduceDamage < value;
                    }
                    else
                    {
                        return false;
                    }

                    break;
                }

                case 15: // add life
                {
                    if (opt == "set")
                    {
                        if (value < 0)
                            value = 0;
                        else if (value > 255)
                            value = 255;

                        pItem.Enchantment = (byte) value;
                    }
                    else if (opt == "==")
                    {
                        return pItem.Enchantment == value;
                    }
                    else if (opt == "<")
                    {
                        return pItem.Enchantment < value;
                    }
                    else
                    {
                        return false;
                    }

                    break;
                }

                case 16: // anti monster
                case 17: // chk sum
                case 18: // plunder
                case 19: // special flag
                    return false;
                case 20: // color
                {
                    if (opt == "set")
                    {
                        if (!Enum.IsDefined(typeof(Item.ItemColor), value))
                            return false;

                        pItem.Color = (Item.ItemColor) value;
                    }
                    else if (opt == "==")
                    {
                        return pItem.Color == (Item.ItemColor) value;
                    }
                    else if (opt == "<")
                    {
                        return pItem.Color < (Item.ItemColor) value;
                    }
                    else
                    {
                        return false;
                    }

                    break;
                }

                case 21: // add lev exp
                    {
                        if (opt == "set")
                        {
                            if (value < 0)
                                value = 0;
                            if (value > ushort.MaxValue)
                                value = ushort.MaxValue;

                            pItem.CompositionProgress = (ushort)value;
                        }
                        else if (opt == "==")
                        {
                            return pItem.CompositionProgress == value;
                        }
                        else if (opt == "<")
                        {
                            return pItem.CompositionProgress < value;
                        }
                        else
                        {
                            return false;
                        }

                        break;
                    }
                default:
                    return false;
            }

            await pItem.SaveAsync();
            if (update)
                await user.SendAsync(new MsgItemInfo(pItem, MsgItemInfo<Client>.ItemMode.Update));
            return true;
        }

        private static async Task<bool> ExecuteActionItemJarCreateAsync(DbAction action, string param, Character user,
                                                                        Role role, Item item, string input)
        {
            if (user?.UserPackage == null)
                return false;

            if (!user.UserPackage.IsPackSpare(1))
                return false;

            if (user.UserPackage.GetItemByType(Item.TYPE_JAR) != null)
                await user.UserPackage.SpendItemAsync(user.UserPackage.GetItemByType(Item.TYPE_JAR));

            DbItemtype itemtype = ItemManager.GetItemtype(action.Data);
            if (itemtype == null)
                return false;

            string[] pszParam = SplitParam(param);

            var newItem = new DbItem
            {
                AddLife = 0,
                AddlevelExp = 0,
                AntiMonster = 0,
                ChkSum = 0,
                Color = 3,
                Data = 0,
                Gem1 = 0,
                Gem2 = 0,
                Ident = 0,
                Magic1 = 0,
                Magic2 = 0,
                ReduceDmg = 0,
                Plunder = null,
                Specialflag = 0,
                Type = itemtype.Type,
                Position = 0,
                PlayerId = user.Identity,
                Monopoly = 0,
                Magic3 = itemtype.Magic3,
                Amount = 0,
                AmountLimit = 0
            };
            for (var i = 0; i < pszParam.Length; i++)
            {
                uint value = uint.Parse(pszParam[i]);
                if (value <= 0) continue;

                switch (i)
                {
                    case 0:
                        newItem.Amount = (ushort) value;
                        break;
                    case 1:
                        newItem.AmountLimit = (ushort) value; //(ushort) (1 << ((ushort) value));
                        break;
                    case 2:
                        // Socket Progress
                        newItem.Data = value;
                        break;
                    case 3:
                        if (Enum.IsDefined(typeof(Item.SocketGem), (byte) value))
                            newItem.Gem1 = (byte) value;
                        break;
                    case 4:
                        if (Enum.IsDefined(typeof(Item.SocketGem), (byte) value))
                            newItem.Gem2 = (byte) value;
                        break;
                    case 5:
                        if (Enum.IsDefined(typeof(Item.ItemEffect), (ushort) value))
                            newItem.Magic1 = (byte) value;
                        break;
                    case 6:
                        // magic2.. w/e
                        break;
                    case 7:
                        if (value < 256)
                            newItem.Magic3 = (byte) value;
                        break;
                    case 8:
                        if (value < 8)
                            newItem.ReduceDmg = (byte) value;
                        break;
                    case 9:
                        if (value < 256)
                            newItem.AddLife = (byte) value;
                        break;
                    case 10:
                        newItem.Plunder = null;
                        break;
                    case 11:
                        if (Enum.IsDefined(typeof(Item.ItemColor), value))
                            newItem.Color = (byte) value;
                        break;
                    case 12:
                        if (value < 256)
                            newItem.Monopoly = (byte) value;
                        break;
                    case 13:
                    case 14:
                    case 15:
                        // R -> For Steeds only
                        // G -> For Steeds only
                        // B -> For Steeds only
                        // G == 8 R == 16
                        newItem.Data = value | (uint.Parse(pszParam[14]) << 8) | (uint.Parse(pszParam[13]) << 16);
                        break;
                }
            }

            var pItem = new Item(user);
            if (!await pItem.CreateAsync(newItem))
                return false;

            await user.UserPackage.AddItemAsync(pItem);
            return true;
        }

        private static async Task<bool> ExecuteActionItemJarVerifyAsync(DbAction action, string param, Character user,
                                                                        Role role, Item item, string input)
        {
            if (user?.UserPackage == null)
                return false;

            if (!user.UserPackage.IsPackSpare(1))
                return false;

            string[] pszParam = SplitParam(param);

            if (pszParam.Length < 2) return false;

            uint amount = uint.Parse(pszParam[1]);
            uint monster = uint.Parse(pszParam[0]);

            Item jar = user.UserPackage.GetItemByType(action.Data);
            return jar != null && jar.MaximumDurability == monster && amount <= jar.Data;
        }

        #endregion

        #region Syndicate

        private static async Task<bool> ExecuteActionSynCreateAsync(DbAction action, string param, Character user,
                                                                    Role role,
                                                                    Item item, string input)
        {
            if (user == null || user.Syndicate != null)
                return false;

            string[] splitParam = SplitParam(param);
            if (splitParam.Length < 2)
            {
                await Log.WriteLogAsync(LogLevel.Warning,
                                        $"Invalid param count for guild creation: {param}, {action.Identity}");
                return false;
            }

            if (!int.TryParse(splitParam[0], out int level)) return false;

            if (user.Level < level)
            {
                await user.SendAsync(Language.StrNotEnoughLevel);
                return false;
            }

            if (!int.TryParse(splitParam[1], out int price)) return false;

            if (user.Silvers < price)
            {
                await user.SendAsync(Language.StrNotEnoughMoney);
                return false;
            }

            return await user.CreateSyndicateAsync(input, price);
        }


        private static async Task<bool> ExecuteActionSynDestroyAsync(DbAction action, string param, Character user,
                                                                     Role role, Item item, string input)
        {
            if (user?.Syndicate == null)
                return false;

            if (user.SyndicateRank != SyndicateMember.SyndicateRank.GuildLeader)
            {
                await user.SendAsync(Language.StrSynNotLeader);
                return false;
            }

            return await user.DisbandSyndicateAsync();
        }

        private static async Task<bool> ExecuteActionSynSetAssistantAsync(DbAction action, string param, Character user,
                                                                          Role role, Item item, string input)
        {
            if (user?.Syndicate == null || user.SyndicateRank != SyndicateMember.SyndicateRank.GuildLeader)
                return false;
            return await user.Syndicate.PromoteAsync(user, input, SyndicateMember.SyndicateRank.DeputyLeader);
        }

        private static async Task<bool> ExecuteActionSynClearRankAsync(DbAction action, string param, Character user,
                                                                       Role role, Item item, string input)
        {
            if (user?.Syndicate == null || user.SyndicateRank != SyndicateMember.SyndicateRank.GuildLeader)
                return false;
            return await user.Syndicate.DemoteAsync(user, input);
        }

        private static async Task<bool> ExecuteActionSynChangeLeaderAsync(DbAction action, string param, Character user,
                                                                          Role role, Item item, string input)
        {
            if (user?.Syndicate == null || user.SyndicateRank != SyndicateMember.SyndicateRank.GuildLeader)
                return false;
            return await user.Syndicate.PromoteAsync(user, input, SyndicateMember.SyndicateRank.GuildLeader);
        }

        private static async Task<bool> ExecuteActionSynAntagonizeAsync(DbAction action, string param, Character user,
                                                                        Role role, Item item, string input)
        {
            if (user?.Syndicate == null)
                return false;

            if (user.SyndicateRank != SyndicateMember.SyndicateRank.GuildLeader)
                return false;

            Syndicate target = SyndicateManager.GetSyndicate(input);
            if (target == null)
                return false;

            if (target.Identity == user.SyndicateIdentity)
                return false;

            return await user.Syndicate.AntagonizeAsync(user, target);
        }

        private static async Task<bool> ExecuteActionSynClearAntagonizeAsync(
            DbAction action, string param, Character user,
            Role role, Item item, string input)
        {
            if (user?.Syndicate == null)
                return false;

            if (user.SyndicateRank != SyndicateMember.SyndicateRank.GuildLeader)
                return false;

            Syndicate target = SyndicateManager.GetSyndicate(input);
            if (target == null)
                return false;

            if (target.Identity == user.SyndicateIdentity)
                return false;

            return await user.Syndicate.PeaceAsync(user, target);
        }

        private static async Task<bool> ExecuteActionSynAllyAsync(DbAction action, string param, Character user,
                                                                  Role role,
                                                                  Item item, string input)
        {
            if (user?.Syndicate == null)
                return false;

            if (user.SyndicateRank != SyndicateMember.SyndicateRank.GuildLeader)
                return false;

            Syndicate target = user.Team?.Members.FirstOrDefault(x => x.Identity != user.Identity)?.Syndicate;
            if (target == null)
                return false;

            if (target.Identity == user.SyndicateIdentity)
                return false;

            return await user.Syndicate.CreateAllianceAsync(user, target);
        }

        private static async Task<bool> ExecuteActionSynClearAllyAsync(DbAction action, string param, Character user,
                                                                       Role role, Item item, string input)
        {
            if (user?.Syndicate == null)
                return false;

            if (user.SyndicateRank != SyndicateMember.SyndicateRank.GuildLeader)
                return false;

            Syndicate target = SyndicateManager.GetSyndicate(input);
            if (target == null)
                return false;

            if (target.Identity == user.SyndicateIdentity)
                return false;

            return await user.Syndicate.DisbandAllianceAsync(user, target);
        }

        private static async Task<bool> ExecuteActionSynAttrAsync(DbAction action, string param, Character user,
                                                                  Role role,
                                                                  Item item, string input)
        {
            string[] splitParam = SplitParam(param, 4);
            if (splitParam.Length < 3)
                return false;

            string field = splitParam[0],
                   opt = splitParam[1];
            long value = long.Parse(splitParam[2]);

            Syndicate target = null;
            if (splitParam.Length < 4)
                target = user.Syndicate;
            else
                target = SyndicateManager.GetSyndicate(int.Parse(splitParam[3]));

            if (target == null)
                return true;

            var data = 0;
            if (field.Equals("money", StringComparison.InvariantCultureIgnoreCase))
            {
                if (opt.Equals("+="))
                {
                    if (target.Money + value < 0)
                        return false;

                    target.Money = (int) Math.Max(0, target.Money + value);
                    return await target.SaveAsync();
                }

                data = target.Money;
            }
            else if (field.Equals("membernum", StringComparison.InvariantCultureIgnoreCase))
            {
                data = target.MemberCount;
            }

            switch (opt)
            {
                case "==": return data == value;
                case ">=": return data >= value;
                case "<=": return data <= value;
                case ">":  return data > value;
                case "<":  return data < value;
            }

            return false;
        }

        #endregion

        #region Monster

        private static async Task<bool> ExecuteActionMstDropitemAsync(DbAction action, string param, Character user,
                                                                      Role role, Item item, string input)
        {
            if (role == null || !(role is Monster monster))
                return false;

            string[] splitParam = SplitParam(param, 2);
            if (splitParam.Length < 2)
                return false;

            string ope = splitParam[0];
            uint data = uint.Parse(splitParam[1]);

            var percent = 100;
            if (splitParam.Length >= 3)
                percent = int.Parse(splitParam[2]);

            var flag = 0;
            if (splitParam.Length >= 4)
                flag = int.Parse(splitParam[3]);

            if (ope.Equals("dropitem"))
            {
                await monster.DropItemAsync(data, user, (MapItem.DropMode) flag);
                return true;
            }

            if (ope.Equals("dropmoney"))
            {
                percent %= 100;
                var dwMoneyDrop = (uint) (data * (percent + await Kernel.NextAsync(100 - percent)) / 100);
                if (dwMoneyDrop <= 0)
                    return false;
                uint idUser = user?.Identity ?? 0u;
                await monster.DropMoneyAsync(dwMoneyDrop, idUser, (MapItem.DropMode) flag);
                return true;
            }

            return false;
        }

        #endregion

        #region User

        private static async Task<bool> ExecuteUserAttrAsync(DbAction action, string param, Character user, Role role,
                                                             Item item, string input)
        {
            if (user == null)
                return false;

            string[] parsedParam = SplitParam(param);
            if (parsedParam.Length < 3)
            {
                await Log.WriteLogAsync(LogLevel.Error,
                                        $"GameAction::ExecuteUserAttr[{action.Identity}] invalid param num {param}");
                return false;
            }

            string type = "", opt = "", value = "", last = "";
            type = parsedParam[0];
            opt = parsedParam[1];
            value = parsedParam[2];
            if (parsedParam.Length > 3)
                last = parsedParam[3];

            switch (type.ToLower())
            {
                #region Force (>, >=, <, <=, =, +=, set)

                case "force":
                case "strength":
                    int forceValue = int.Parse(value);
                    if (opt.Equals(">"))
                        return user.Strength > forceValue;
                    if (opt.Equals(">="))
                        return user.Strength >= forceValue;
                    if (opt.Equals("<"))
                        return user.Strength < forceValue;
                    if (opt.Equals("<="))
                        return user.Strength <= forceValue;
                    if (opt.Equals("=") || opt.Equals("=="))
                        return user.Strength == forceValue;
                    if (opt.Equals("+="))
                    {
                        await user.AddAttributesAsync(ClientUpdateType.Strength, forceValue);
                        return true;
                    }

                    if (opt.Equals("set"))
                    {
                        await user.SetAttributesAsync(ClientUpdateType.Strength, (ulong) forceValue);
                        return true;
                    }

                    break;

                #endregion

                #region Speed (>, >=, <, <=, =, +=, set)

                case "agility":
                case "speed":
                    int speedValue = int.Parse(value);
                    if (opt.Equals(">"))
                        return user.Agility > speedValue;
                    if (opt.Equals(">="))
                        return user.Agility >= speedValue;
                    if (opt.Equals("<"))
                        return user.Agility < speedValue;
                    if (opt.Equals("<="))
                        return user.Agility <= speedValue;
                    if (opt.Equals("=") || opt.Equals("=="))
                        return user.Agility == speedValue;
                    if (opt.Equals("+="))
                    {
                        await user.AddAttributesAsync(ClientUpdateType.Agility, speedValue);
                        return true;
                    }

                    if (opt.Equals("set"))
                    {
                        await user.SetAttributesAsync(ClientUpdateType.Agility, (ulong) speedValue);
                        return true;
                    }

                    break;

                #endregion

                #region Health (>, >=, <, <=, =, +=, set)

                case "vitality":
                case "health":
                    int healthValue = int.Parse(value);
                    if (opt.Equals(">"))
                        return user.Vitality > healthValue;
                    if (opt.Equals(">="))
                        return user.Vitality >= healthValue;
                    if (opt.Equals("<"))
                        return user.Vitality < healthValue;
                    if (opt.Equals("<="))
                        return user.Vitality <= healthValue;
                    if (opt.Equals("=") || opt.Equals("=="))
                        return user.Vitality == healthValue;
                    if (opt.Equals("+="))
                    {
                        await user.AddAttributesAsync(ClientUpdateType.Vitality, healthValue);
                        return true;
                    }

                    if (opt.Equals("set"))
                    {
                        await user.SetAttributesAsync(ClientUpdateType.Vitality, (ulong) healthValue);
                        return true;
                    }

                    break;

                #endregion

                #region Soul (>, >=, <, <=, =, +=, set)

                case "spirit":
                case "soul":
                    int soulValue = int.Parse(value);
                    if (opt.Equals(">"))
                        return user.Spirit > soulValue;
                    if (opt.Equals(">="))
                        return user.Spirit >= soulValue;
                    if (opt.Equals("<"))
                        return user.Spirit < soulValue;
                    if (opt.Equals("<="))
                        return user.Spirit <= soulValue;
                    if (opt.Equals("=") || opt.Equals("=="))
                        return user.Spirit == soulValue;
                    if (opt.Equals("+="))
                    {
                        await user.AddAttributesAsync(ClientUpdateType.Spirit, soulValue);
                        return true;
                    }

                    if (opt.Equals("set"))
                    {
                        await user.SetAttributesAsync(ClientUpdateType.Spirit, (ulong) soulValue);
                        return true;
                    }

                    break;

                #endregion

                #region Attribute Points (>, >=, <, <=, =, +=, set)

                case "attr_points":
                case "attr":
                    int attrValue = int.Parse(value);
                    if (opt.Equals(">"))
                        return user.AttributePoints > attrValue;
                    if (opt.Equals(">="))
                        return user.AttributePoints >= attrValue;
                    if (opt.Equals("<"))
                        return user.AttributePoints < attrValue;
                    if (opt.Equals("<="))
                        return user.AttributePoints <= attrValue;
                    if (opt.Equals("=") || opt.Equals("=="))
                        return user.AttributePoints == attrValue;
                    if (opt.Equals("+="))
                    {
                        await user.AddAttributesAsync(ClientUpdateType.Atributes, attrValue);
                        return true;
                    }

                    if (opt.Equals("set"))
                    {
                        await user.SetAttributesAsync(ClientUpdateType.Atributes, (ulong) attrValue);
                        return true;
                    }

                    break;

                #endregion

                #region Level (>, >=, <, <=, =, +=, set)

                case "level":
                    int levelValue = int.Parse(value);
                    if (opt.Equals(">"))
                        return user.Level > levelValue;
                    if (opt.Equals(">="))
                        return user.Level >= levelValue;
                    if (opt.Equals("<"))
                        return user.Level < levelValue;
                    if (opt.Equals("<="))
                        return user.Level <= levelValue;
                    if (opt.Equals("=") || opt.Equals("=="))
                        return user.Level == levelValue;
                    if (opt.Equals("+="))
                    {
                        await user.AddAttributesAsync(ClientUpdateType.Level, levelValue);
                        return true;
                    }

                    if (opt.Equals("set"))
                    {
                        await user.SetAttributesAsync(ClientUpdateType.Level, (ulong) levelValue);
                        return true;
                    }

                    break;

                #endregion

                #region Metempsychosis (>, >=, <, <=, =, +=, set)

                case "metempsychosis":
                    int metempsychosisValue = int.Parse(value);
                    if (opt.Equals(">"))
                        return user.Metempsychosis > metempsychosisValue;
                    if (opt.Equals(">="))
                        return user.Metempsychosis >= metempsychosisValue;
                    if (opt.Equals("<"))
                        return user.Metempsychosis < metempsychosisValue;
                    if (opt.Equals("<="))
                        return user.Metempsychosis <= metempsychosisValue;
                    if (opt.Equals("=") || opt.Equals("=="))
                        return user.Metempsychosis == metempsychosisValue;
                    if (opt.Equals("+="))
                    {
                        await user.AddAttributesAsync(ClientUpdateType.Reborn, metempsychosisValue);
                        return true;
                    }

                    if (opt.Equals("set"))
                    {
                        await user.SetAttributesAsync(ClientUpdateType.Reborn, (ulong) metempsychosisValue);
                        return true;
                    }

                    break;

                #endregion

                #region Money (>, >=, <, <=, =, +=, set)

                case "money":
                case "silver":
                {
                    int moneyValue = int.Parse(value);
                    if (opt.Equals(">"))
                        return user.Silvers > moneyValue;
                    if (opt.Equals(">="))
                        return user.Silvers >= moneyValue;
                    if (opt.Equals("<"))
                        return user.Silvers < moneyValue;
                    if (opt.Equals("<="))
                        return user.Silvers <= moneyValue;
                    if (opt.Equals("=") || opt.Equals("=="))
                        return user.Silvers == moneyValue;
                    if (opt.Equals("+=")) return await user.ChangeMoneyAsync(moneyValue);

                    if (opt.Equals("set"))
                    {
                        await user.SetAttributesAsync(ClientUpdateType.Money, (ulong) moneyValue);
                        return true;
                    }

                    break;
                }

                #endregion

                #region Emoney (>, >=, <, <=, =, +=, set)

                case "emoney":
                case "e_money":
                case "cps":
                    int emoneyValue = int.Parse(value);
                    if (opt.Equals(">"))
                        return user.ConquerPoints > emoneyValue;
                    if (opt.Equals(">="))
                        return user.ConquerPoints >= emoneyValue;
                    if (opt.Equals("<"))
                        return user.ConquerPoints < emoneyValue;
                    if (opt.Equals("<="))
                        return user.ConquerPoints <= emoneyValue;
                    if (opt.Equals("=") || opt.Equals("=="))
                        return user.ConquerPoints == emoneyValue;
                    if (opt.Equals("+=")) return await user.ChangeConquerPointsAsync(emoneyValue);

                    if (opt.Equals("set"))
                    {
                        await user.SetAttributesAsync(ClientUpdateType.ConquerPoints, (ulong) emoneyValue);
                        return true;
                    }

                    break;

                #endregion

                #region Rankshow (>, >=, <, <=, =)

                case "rank":
                case "rankshow":
                case "rank_show":
                    int rankShowValue = int.Parse(value);
                    if (opt.Equals(">"))
                        return user.SyndicateRank > (SyndicateMember.SyndicateRank) rankShowValue;
                    if (opt.Equals(">="))
                        return user.SyndicateRank >= (SyndicateMember.SyndicateRank) rankShowValue;
                    if (opt.Equals("<"))
                        return user.SyndicateRank < (SyndicateMember.SyndicateRank) rankShowValue;
                    if (opt.Equals("<="))
                        return user.SyndicateRank <= (SyndicateMember.SyndicateRank) rankShowValue;
                    if (opt.Equals("==") || opt.Equals("="))
                        return user.SyndicateRank == (SyndicateMember.SyndicateRank) rankShowValue;
                    break;

                #endregion

                #region Syn Time (>, >=, <, <=, =)

                case "syn_time":
                {
                    int synTime = int.Parse(value);
                    if (user.Syndicate == null)
                        return false;

                    var synDays = (int) (DateTime.Now - user.Syndicate.CreationDate).TotalDays;
                    if (opt.Equals("==") || opt.Equals("="))
                        return synDays == synTime;
                    if (opt.Equals(">="))
                        return synDays >= synTime;
                    if (opt.Equals(">"))
                        return synDays > synTime;
                    if (opt.Equals("<="))
                        return synDays <= synTime;
                    if (opt.Equals("<"))
                        return synDays < synTime;
                    break;
                }

                #endregion

                #region Syn User Time (>, >=, <, <=, =)

                case "syn_user_time":
                {
                    int synTime = int.Parse(value);
                    if (user.Syndicate == null)
                        return false;

                    var synDays = (int) (DateTime.Now - user.SyndicateMember.JoinDate).TotalDays;
                    if (opt.Equals("==") || opt.Equals("="))
                        return synDays == synTime;
                    if (opt.Equals(">="))
                        return synDays >= synTime;
                    if (opt.Equals(">"))
                        return synDays > synTime;
                    if (opt.Equals("<="))
                        return synDays <= synTime;
                    if (opt.Equals("<"))
                        return synDays < synTime;
                    break;
                }

                #endregion

                #region Experience (>, >=, <, <=, =, +=, set)

                case "exp":
                case "experience":
                    ulong expValue = ulong.Parse(value);
                    if (opt.Equals(">"))
                        return user.Experience > expValue;
                    if (opt.Equals(">="))
                        return user.Experience >= expValue;
                    if (opt.Equals("<"))
                        return user.Experience < expValue;
                    if (opt.Equals("<="))
                        return user.Experience <= expValue;
                    if (opt.Equals("=") || opt.Equals("=="))
                        return user.Experience == expValue;
                    if (opt.Equals("+="))
                        return await user.AwardExperienceAsync((long) expValue, last.Equals("nocontribute"));

                    if (opt.Equals("set"))
                    {
                        await user.SetAttributesAsync(ClientUpdateType.Experience, expValue);
                        return true;
                    }

                    break;

                #endregion

                #region Stamina (>, >=, <, <=, =, +=, set)

                case "ep":
                case "energy":
                case "stamina":
                    int energyValue = int.Parse(value);
                    if (opt.Equals(">"))
                        return user.Energy > energyValue;
                    if (opt.Equals(">="))
                        return user.Energy >= energyValue;
                    if (opt.Equals("<"))
                        return user.Energy < energyValue;
                    if (opt.Equals("<="))
                        return user.Energy <= energyValue;
                    if (opt.Equals("=") || opt.Equals("=="))
                        return user.Energy == energyValue;
                    if (opt.Equals("+=")) return await user.AddAttributesAsync(ClientUpdateType.Stamina, energyValue);

                    if (opt.Equals("set"))
                    {
                        await user.SetAttributesAsync(ClientUpdateType.Stamina, (ulong) energyValue);
                        return true;
                    }

                    break;

                #endregion

                #region Life (>, >=, <, <=, =, +=, set)

                case "life":
                case "hp":
                    int lifeValue = int.Parse(value);
                    if (opt.Equals(">"))
                        return user.Life > lifeValue;
                    if (opt.Equals(">="))
                        return user.Life >= lifeValue;
                    if (opt.Equals("<"))
                        return user.Life < lifeValue;
                    if (opt.Equals("<="))
                        return user.Life <= lifeValue;
                    if (opt.Equals("=="))
                        return user.Life == lifeValue;
                    if (opt.Equals("+="))
                    {
                        user.QueueAction(async () =>
                        {
                            await user.AddAttributesAsync(ClientUpdateType.Hitpoints, lifeValue);
                            if (!user.IsAlive)
                                await user.BeKillAsync(null);
                        });
                        return true;
                    }

                    if (opt.Equals("set") || opt.Equals("="))
                    {
                        user.QueueAction(async () =>
                        {
                            await user.SetAttributesAsync(ClientUpdateType.Hitpoints, (ulong) lifeValue);
                            if (!user.IsAlive)
                                await user.BeKillAsync(null);
                        });
                        return true;
                    }

                    break;

                #endregion

                #region Mana (>, >=, <, <=, =, +=, set)

                case "mana":
                case "mp":
                    int manaValue = int.Parse(value);
                    if (opt.Equals(">"))
                        return user.Mana > manaValue;
                    if (opt.Equals(">="))
                        return user.Mana >= manaValue;
                    if (opt.Equals("<"))
                        return user.Mana < manaValue;
                    if (opt.Equals("<="))
                        return user.Mana <= manaValue;
                    if (opt.Equals("=") || opt.Equals("=="))
                        return user.Mana == manaValue;
                    if (opt.Equals("+=")) return await user.AddAttributesAsync(ClientUpdateType.Mana, manaValue);

                    if (opt.Equals("set"))
                    {
                        await user.SetAttributesAsync(ClientUpdateType.Mana, (ulong) manaValue);
                        return true;
                    }

                    break;

                #endregion

                #region Pk (>, >=, <, <=, =, +=, set)

                case "pk":
                case "pkp":
                    int pkValue = int.Parse(value);
                    if (opt.Equals(">"))
                        return user.PkPoints > pkValue;
                    if (opt.Equals(">="))
                        return user.PkPoints >= pkValue;
                    if (opt.Equals("<"))
                        return user.PkPoints < pkValue;
                    if (opt.Equals("<="))
                        return user.PkPoints <= pkValue;
                    if (opt.Equals("=") || opt.Equals("=="))
                        return user.PkPoints == pkValue;
                    if (opt.Equals("+=")) return await user.AddAttributesAsync(ClientUpdateType.PkPoints, pkValue);

                    if (opt.Equals("set"))
                    {
                        await user.SetAttributesAsync(ClientUpdateType.PkPoints, (ulong) pkValue);
                        return true;
                    }

                    break;

                #endregion

                #region Profession (>, >=, <, <=, =, +=, set)

                case "profession":
                case "pro":
                {
                    int proValue = int.Parse(value);
                    if (opt.Equals(">"))
                        return user.Profession > proValue;
                    if (opt.Equals(">="))
                        return user.Profession >= proValue;
                    if (opt.Equals("<"))
                        return user.Profession < proValue;
                    if (opt.Equals("<="))
                        return user.Profession <= proValue;
                    if (opt.Equals("=") || opt.Equals("=="))
                        return user.Profession == proValue;
                    if (opt.Equals("+=")) return await user.AddAttributesAsync(ClientUpdateType.Class, proValue);

                    if (opt.Equals("set"))
                    {
                        await user.SetAttributesAsync(ClientUpdateType.Class, (ulong) proValue);
                        return true;
                    }

                    break;
                }

                #endregion

                #region First Profession (>, >=, <, <=, =)

                case "first_prof":
                {
                    int proValue = int.Parse(value);
                    if (opt.Equals(">"))
                        return user.FirstProfession > proValue;
                    if (opt.Equals(">="))
                        return user.FirstProfession >= proValue;
                    if (opt.Equals("<"))
                        return user.FirstProfession < proValue;
                    if (opt.Equals("<="))
                        return user.FirstProfession <= proValue;
                    if (opt.Equals("=") || opt.Equals("=="))
                        return user.FirstProfession == proValue;
                    break;
                }

                #endregion

                #region Last Profession (>, >=, <, <=, =)

                case "old_prof":
                {
                    int proValue = int.Parse(value);
                    if (opt.Equals(">"))
                        return user.PreviousProfession > proValue;
                    if (opt.Equals(">="))
                        return user.PreviousProfession >= proValue;
                    if (opt.Equals("<"))
                        return user.PreviousProfession < proValue;
                    if (opt.Equals("<="))
                        return user.PreviousProfession <= proValue;
                    if (opt.Equals("=") || opt.Equals("=="))
                        return user.PreviousProfession == proValue;
                    break;
                }

                #endregion

                #region Transform (>, >=, <, <=, =, ==)

                case "transform":
                    int transformValue = int.Parse(value);
                    if (opt.Equals(">"))
                        return user.TransformationMesh > transformValue;
                    if (opt.Equals(">="))
                        return user.TransformationMesh >= transformValue;
                    if (opt.Equals("<"))
                        return user.TransformationMesh < transformValue;
                    if (opt.Equals("<="))
                        return user.TransformationMesh <= transformValue;
                    if (opt.Equals("=") || opt.Equals("=="))
                        return user.TransformationMesh == transformValue;
                    break;

                #endregion

                #region Virtue (>, >=, <, <=, =, +=, set)

                case "virtue":
                case "vp":
                    int virtueValue = int.Parse(value);
                    if (opt.Equals(">"))
                        return user.VirtuePoints > virtueValue;
                    if (opt.Equals(">="))
                        return user.VirtuePoints >= virtueValue;
                    if (opt.Equals("<"))
                        return user.VirtuePoints < virtueValue;
                    if (opt.Equals("<="))
                        return user.VirtuePoints <= virtueValue;
                    if (opt.Equals("=") || opt.Equals("=="))
                        return user.VirtuePoints == virtueValue;
                    if (opt.Equals("+="))
                    {
                        if (virtueValue > 0)
                            user.VirtuePoints += (uint) virtueValue;
                        else
                            user.VirtuePoints -= (uint) (virtueValue * -1);
                        return await user.SaveAsync();
                    }

                    if (opt.Equals("set"))
                    {
                        user.VirtuePoints = (uint) virtueValue;
                        return await user.SaveAsync();
                    }

                    break;

                #endregion

                #region Vip (>, >=, <, <=, =, ==)

                case "vip":
                {
                    int vipValue = int.Parse(value);
                    if (opt.Equals(">"))
                        return user.VipLevel > vipValue;
                    if (opt.Equals(">="))
                        return user.VipLevel >= vipValue;
                    if (opt.Equals("<"))
                        return user.VipLevel < vipValue;
                    if (opt.Equals("<="))
                        return user.VipLevel <= vipValue;
                    if (opt.Equals("=") || opt.Equals("=="))
                        return user.VipLevel == vipValue;
                    break;
                }

                #endregion

                #region SVip (>, >=, <, <=, =, ==)

                case "svip":
                {
                    int svipValue = int.Parse(value);
                    if (opt.Equals(">"))
                        return user.VipLevel > svipValue;
                    if (opt.Equals(">="))
                        return user.VipLevel >= svipValue;
                    if (opt.Equals("<"))
                        return user.VipLevel < svipValue;
                    if (opt.Equals("<="))
                        return user.VipLevel <= svipValue;
                    if (opt.Equals("=="))
                        return user.VipLevel == svipValue;
                    if (opt.Equals("set") || opt.Equals("="))
                    {
                        if (string.IsNullOrEmpty(last) || int.TryParse(last, out int minutes))
                            minutes = 1440; // 24 hours
                        uint vipValue = uint.Parse(value);
                        if (vipValue == user.VipLevel)
                            user.VipExpiration = user.VipExpiration < DateTime.Now
                                                     ? DateTime.Now.AddMinutes(minutes)
                                                     : user.VipExpiration.AddMinutes(minutes);
                        else
                            user.VipExpiration = DateTime.Now.AddMinutes(minutes);

                        await user.SetAttributesAsync(ClientUpdateType.VipLevel, vipValue);
                        return true;
                    }

                    break;
                }

                #endregion

                #region Xp (>, >=, <, <=, =, +=, set)

                case "xp":
                    int xpValue = int.Parse(value);
                    if (opt.Equals(">"))
                        return user.XpPoints > xpValue;
                    if (opt.Equals(">="))
                        return user.XpPoints >= xpValue;
                    if (opt.Equals("<"))
                        return user.XpPoints < xpValue;
                    if (opt.Equals("<="))
                        return user.XpPoints <= xpValue;
                    if (opt.Equals("=") || opt.Equals("=="))
                        return user.XpPoints == xpValue;
                    if (opt.Equals("+="))
                    {
                        await user.AddXpAsync((byte) xpValue);
                        return true;
                    }

                    if (opt.Equals("set"))
                    {
                        await user.SetXpAsync((byte) xpValue);
                        return true;
                    }

                    break;

                #endregion

                #region Iterator (>, >=, <, <=, =, +=, set)

                case "iterator":
                    int iteratorValue = int.Parse(value);
                    if (opt.Equals(">"))
                        return user.Iterator > iteratorValue;
                    if (opt.Equals(">="))
                        return user.Iterator >= iteratorValue;
                    if (opt.Equals("<"))
                        return user.Iterator < iteratorValue;
                    if (opt.Equals("<="))
                        return user.Iterator <= iteratorValue;
                    if (opt.Equals("=="))
                        return user.Iterator == iteratorValue;
                    if (opt.Equals("+="))
                    {
                        user.Iterator += iteratorValue;
                        return true;
                    }

                    if (opt.Equals("set") || opt == "=")
                    {
                        user.Iterator = iteratorValue;
                        return true;
                    }

                    break;

                #endregion

                #region Merchant (==, set)

                case "business":
                    int merchantValue = int.Parse(value);
                    if (opt.Equals("=="))
                        return user.Merchant == merchantValue;
                    if (opt.Equals("set") || opt == "=")
                    {
                        if (merchantValue == 0)
                            await user.RemoveMerchantAsync();
                        else
                            await user.SetMerchantAsync();

                        return true;
                    }

                    break;

                #endregion

                #region Look (==, set)

                case "look":
                {
                    switch (opt)
                    {
                        case "==": return user.Mesh % 10 == ushort.Parse(value);
                        case "set":
                        {
                            ushort usVal = ushort.Parse(value);
                            if (user.Gender == 1 && (usVal == 3 || usVal == 4))
                            {
                                user.Body = (BodyType) (1000 + usVal);
                                await user.SynchroAttributesAsync(ClientUpdateType.Mesh, user.Mesh, true);
                                await user.SaveAsync();
                                return true;
                            }

                            if (user.Gender == 2 && (usVal == 1 || usVal == 2))
                            {
                                user.Body = (BodyType) (2000 + usVal);
                                await user.SynchroAttributesAsync(ClientUpdateType.Mesh, user.Mesh, true);
                                await user.SaveAsync();
                                return true;
                            }

                            return false;
                        }
                    }

                    return false;
                }

                #endregion

                #region Body (set)

                case "body":
                {
                    switch (opt)
                    {
                        case "set":
                        {
                            ushort usNewBody = ushort.Parse(value);
                            if (usNewBody == 1003 || usNewBody == 1004)
                                if (user.Body != BodyType.AgileFemale && user.Body != BodyType.MuscularFemale)
                                    return false; // to change body use the fucking item , asshole

                            if (usNewBody == 2001 || usNewBody == 2002)
                                if (user.Body != BodyType.AgileMale && user.Body != BodyType.MuscularMale)
                                    return false; // to change body use the fucking item , asshole

                            if (user.UserPackage[Item.ItemPosition.Garment] != null)
                                await user.UserPackage.UnEquipAsync(Item.ItemPosition.Garment);

                            user.Body = (BodyType) usNewBody;
                            await user.SynchroAttributesAsync(ClientUpdateType.Mesh, user.Mesh, true);
                            await user.SaveAsync();
                            return true;
                            ;
                        }
                    }

                    return false;
                }

                #endregion

                #region Cultivation

                case "cultivation":
                {
                    int cultivationValue = int.Parse(value);
                    if (opt.Equals(">"))
                        return user.StudyPoints > cultivationValue;
                    if (opt.Equals(">="))
                        return user.StudyPoints >= cultivationValue;
                    if (opt.Equals("<"))
                        return user.StudyPoints < cultivationValue;
                    if (opt.Equals("<="))
                        return user.StudyPoints <= cultivationValue;
                    if (opt.Equals("=") || opt.Equals("=="))
                        return user.StudyPoints == cultivationValue;
                    if (opt.Equals("+=")) return await user.ChangeCultivationAsync(cultivationValue);
                    break;
                }

                #endregion

                #region Strength Value

                case "strengthvalue":
                {
                    int strengthValue = int.Parse(value);
                    if (opt.Equals(">"))
                        return user.ChiPoints > strengthValue;
                    if (opt.Equals(">="))
                        return user.ChiPoints >= strengthValue;
                    if (opt.Equals("<"))
                        return user.ChiPoints < strengthValue;
                    if (opt.Equals("<="))
                        return user.ChiPoints <= strengthValue;
                    if (opt.Equals("=") || opt.Equals("=="))
                        return user.ChiPoints == strengthValue;
                    if (opt.Equals("+=")) return await user.ChangeStrengthValueAsync(strengthValue);
                    break;
                }

                #endregion

                #region Mentor

                case "mentor":
                {
                    int mentorValue = int.Parse(value);
                    if (opt.Equals(">"))
                        return user.EnlightenPoints > mentorValue;
                    if (opt.Equals(">="))
                        return user.EnlightenPoints >= mentorValue;
                    if (opt.Equals("<"))
                        return user.EnlightenPoints < mentorValue;
                    if (opt.Equals("<="))
                        return user.EnlightenPoints <= mentorValue;
                    if (opt.Equals("=") || opt.Equals("=="))
                        return user.EnlightenPoints == mentorValue;

                    if (opt.Equals("+="))
                    {
                        if (mentorValue > 0)
                        {
                            user.EnlightenPoints += (uint) mentorValue;
                        }
                        else if (mentorValue < 0)
                        {
                            if (mentorValue > user.EnlightenPoints)
                                return false;

                            user.EnlightenPoints -= (uint) mentorValue;
                        }
                        else
                        {
                            break;
                        }

                        await user.SynchroAttributesAsync(ClientUpdateType.EnlightenPoints, user.EnlightenPoints, true);
                    }

                    break;
                }

                #endregion

                default:
                {
                    await Log.WriteLogAsync(LogLevel.Warning,
                                            $"Unhandled {type} to ExecuteUserAttrAsync [{action.Identity}]");
                    break;
                }
            }

            return false;
        }

        private static async Task<bool> ExecuteUserFullAsync(DbAction action, string param, Character user, Role role,
                                                             Item item, string input)
        {
            if (user == null)
                return false;

            if (param.Equals("life", StringComparison.InvariantCultureIgnoreCase))
            {
                user.QueueAction(() => user.SetAttributesAsync(ClientUpdateType.Hitpoints, user.MaxLife));
                return true;
            }

            if (param.Equals("mana", StringComparison.InvariantCultureIgnoreCase))
            {
                user.QueueAction(() => user.SetAttributesAsync(ClientUpdateType.Mana, user.MaxMana));
                return true;
            }

            if (param.Equals("xp", StringComparison.InvariantCultureIgnoreCase))
            {
                await user.SetXpAsync(100);
                await user.BurstXpAsync();
                return true;
            }

            if (param.Equals("sp", StringComparison.InvariantCultureIgnoreCase))
            {
                await user.SetAttributesAsync(ClientUpdateType.Stamina, user.MaxEnergy);
                return true;
            }

            return false;
        }

        private static async Task<bool> ExecuteUserChgMapAsync(DbAction action, string param, Character user, Role role,
                                                               Item item, string input)
        {
            if (user == null)
                return false;

            string[] paramStrings = SplitParam(param);
            if (paramStrings.Length < 3)
            {
                await Log.WriteLogAsync(LogLevel.Warning,
                                        $"Action {action.Identity}:{action.Type} invalid param length: {param}");
                return false;
            }

            if (!uint.TryParse(paramStrings[0], out uint idMap)
                || !ushort.TryParse(paramStrings[1], out ushort x)
                || !ushort.TryParse(paramStrings[2], out ushort y))
                return false;

            GameMap map = MapManager.GetMap(idMap);
            if (map == null)
            {
                await Log.WriteLogAsync(LogLevel.Warning, $"Invalid map identity {idMap} for action {action.Identity}");
                return false;
            }

            if (!user.Map.IsTeleportDisable() ||
                paramStrings.Length >= 4 && byte.TryParse(paramStrings[3], out byte forceTeleport) &&
                forceTeleport != 0)
                return await user.FlyMapAsync(idMap, x, y);

            return false;
        }

        private static async Task<bool> ExecuteUserRecordpointAsync(DbAction action, string param, Character user,
                                                                    Role role,
                                                                    Item item, string input)
        {
            if (user == null)
                return false;

            string[] paramStrings = SplitParam(param);
            if (paramStrings.Length < 3)
            {
                await Log.WriteLogAsync(LogLevel.Warning,
                                        $"Action {action.Identity}:{action.Type} invalid param length: {param}");
                return false;
            }

            if (!uint.TryParse(paramStrings[0], out uint idMap)
                || !ushort.TryParse(paramStrings[1], out ushort x)
                || !ushort.TryParse(paramStrings[2], out ushort y))
                return false;

            if (idMap == 0)
            {
                await user.SavePositionAsync(user.MapIdentity, user.MapX, user.MapY);
                return true;
            }

            GameMap map = MapManager.GetMap(idMap);
            if (map == null)
            {
                await Log.WriteLogAsync(LogLevel.Warning, $"Invalid map identity {idMap} for action {action.Identity}");
                return false;
            }

            await user.SavePositionAsync(idMap, x, y);
            return true;
        }

        private static async Task<bool> ExecuteUserHairAsync(DbAction action, string param, Character user, Role role,
                                                             Item item, string input)
        {
            if (user == null)
                return false;

            string[] splitParams = SplitParam(param);

            if (splitParams.Length < 1)
            {
                await Log.WriteLogAsync(LogLevel.Warning,
                                        $"Action {action.Identity}:{action.Type} has not enough argments: {param}");
                return false;
            }

            var cmd = "style";
            var value = 0;
            if (splitParams.Length > 1)
            {
                cmd = splitParams[0];
                value = int.Parse(splitParams[1]);
            }
            else
            {
                value = int.Parse(splitParams[0]);
            }

            if (cmd.Equals("style", StringComparison.InvariantCultureIgnoreCase))
            {
                await user.SetAttributesAsync(ClientUpdateType.HairStyle,
                                              (ushort) (value + (user.Hairstyle - user.Hairstyle % 100)));
                return true;
            }

            if (cmd.Equals("color", StringComparison.InvariantCultureIgnoreCase))
            {
                await user.SetAttributesAsync(ClientUpdateType.HairStyle,
                                              (ushort) (user.Hairstyle % 100 + value * 100));
                return true;
            }

            return false;
        }

        private static async Task<bool> ExecuteUserChgmaprecordAsync(DbAction action, string param, Character user,
                                                                     Role role, Item item, string input)
        {
            if (user == null)
                return false;

            await user.FlyMapAsync(user.RecordMapIdentity, user.RecordMapX, user.RecordMapY);
            return true;
        }

        private static async Task<bool> ExecuteActionUserChglinkmapAsync(DbAction action, string param, Character user,
                                                                         Role role, Item item, string input)
        {
            if (user?.Map == null)
                return false;

            if (user.IsPm())
                await user.SendAsync("ExecuteActionUserChglinkmap");

            return true;
        }

        private static async Task<bool> ExecuteUserTransformAsync(DbAction action, string param, Character user,
                                                                  Role role,
                                                                  Item item, string input)
        {
            if (user == null)
                return false;

            string[] splitParam = SplitParam(param);

            if (splitParam.Length < 4)
            {
                await Log.WriteLogAsync(LogLevel.Warning,
                                        $"Invalid param count for {action.Identity}:{action.Type}, {param}");
                return false;
            }

            uint transformation = uint.Parse(splitParam[2]);
            int time = int.Parse(splitParam[3]);
            return await user.TransformAsync(transformation, time, true);
        }

        private static async Task<bool> ExecuteActionUserIspureAsync(DbAction action, string param, Character user,
                                                                     Role role, Item item, string input)
        {
            if (user == null)
                return false;
            return user.ProfessionSort == user.PreviousProfession / 10 &&
                   user.FirstProfession / 10 == user.ProfessionSort;
        }

        private static async Task<bool> ExecuteActionUserTalkAsync(DbAction action, string param, Character user,
                                                                   Role role,
                                                                   Item item, string input)
        {
            if (user == null)
            {
                await Log.WriteLogAsync(LogLevel.Warning, $"Action type 1010 {action.Identity} no user");
                return false;
            }

            await user.SendAsync(param, (TalkChannel) action.Data, Color.White);
            return true;
        }

        private static async Task<bool> ExecuteActionUserMagicAsync(DbAction action, string param, Character user,
                                                                    Role role,
                                                                    Item item, string input)
        {
            if (user == null)
                return false;

            string[] splitParam = SplitParam(param);
            if (splitParam.Length < 2)
            {
                await Log.WriteLogAsync(LogLevel.Warning,
                                        $"Invalid ActionUserMagic param length: {action.Identity}, {param}");
                return false;
            }

            switch (splitParam[0].ToLowerInvariant())
            {
                case "check":
                    if (splitParam.Length >= 3)
                        return user.MagicData.CheckLevel(ushort.Parse(splitParam[1]), ushort.Parse(splitParam[2]));
                    return user.MagicData.CheckType(ushort.Parse(splitParam[1]));

                case "learn":
                    if (splitParam.Length >= 3)
                        return await user.MagicData.CreateAsync(ushort.Parse(splitParam[1]), byte.Parse(splitParam[2]));
                    return await user.MagicData.CreateAsync(ushort.Parse(splitParam[1]), 0);

                case "uplev":
                case "up_lev":
                case "uplevel":
                case "up_level":
                    return await user.MagicData.UpLevelByTaskAsync(ushort.Parse(splitParam[1]));

                case "addexp":
                    return await user.MagicData.AwardExpAsync(ushort.Parse(splitParam[1]), 0, int.Parse(splitParam[2]));

                default:
                    await Log.WriteLogAsync(LogLevel.Warning,
                                            $"[ActionType: {action.Type}] Unknown {splitParam[0]} param {action.Identity}");
                    return false;
            }
        }

        private static async Task<bool> ExecuteActionUserWeaponSkillAsync(DbAction action, string param, Character user,
                                                                          Role role, Item item, string input)
        {
            if (user == null)
                return false;

            string[] splitParam = SplitParam(param);

            if (splitParam.Length < 3)
            {
                await Log.WriteLogAsync(LogLevel.Warning, $"Invalid param amount: {param} [{action.Identity}]");
                return false;
            }

            if (!ushort.TryParse(splitParam[1], out ushort type)
                || !int.TryParse(splitParam[2], out int value))
            {
                await Log.WriteLogAsync(LogLevel.Warning,
                                        $"Invalid weapon skill type {param} for action {action.Identity}");
                return false;
            }

            switch (splitParam[0].ToLowerInvariant())
            {
                case "check":
                    return user.WeaponSkill[type]?.Level >= value;

                case "learn":
                    return await user.WeaponSkill.CreateAsync(type, (byte) value);

                case "addexp":
                    await user.AddWeaponSkillExpAsync(type, value, true);
                    return true;

                default:
                    await Log.WriteLogAsync(LogLevel.Warning,
                                            $"ExecuteActionUserWeaponSkill {splitParam[0]} invalid {action.Identity}");
                    return false;
            }
        }

        private static async Task<bool> ExecuteActionUserLogAsync(DbAction action, string param, Character user,
                                                                  Role role,
                                                                  Item item, string input)
        {
            if (user == null)
                return false;

            string[] splitParam = SplitParam(param, 2);

            if (splitParam.Length < 2)
            {
                await Log.WriteLogAsync(LogLevel.Warning,
                                        $"ExecuteActionUserLog length [id:{action.Identity}], {param}");
                return true;
            }

            string file = splitParam[0];
            string message = splitParam[1];

            if (file.StartsWith("gmlog/"))
                file = file.Remove(0, "gmlog/".Length);

            await Log.GmLogAsync(file, message);
            return true;
        }

        private static async Task<bool> ExecuteActionUserBonusAsync(DbAction action, string param, Character user,
                                                                    Role role,
                                                                    Item item, string input)
        {
            if (user == null)
                return false;
            return await user.DoBonusAsync();
        }

        private static async Task<bool> ExecuteActionUserDivorceAsync(DbAction action, string param, Character user,
                                                                      Role role, Item item, string input)
        {
            if (user == null)
                return false;

            if (user.MateIdentity == 0)
                return false;

            Character mate = RoleManager.GetUser(user.MateIdentity);
            if (mate == null)
            {
                DbCharacter dbMate = await CharactersRepository.FindByIdentityAsync(user.MateIdentity);
                if (dbMate == null)
                    return false;
                dbMate.Mate = 0;
                await ServerDbContext.SaveAsync(dbMate);

                DbItem dbItem = Item.CreateEntity(Item.TYPE_METEORTEAR);
                dbItem.PlayerId = user.Identity;
                await ServerDbContext.SaveAsync(dbItem);
            }
            else
            {
                mate.MateIdentity = 0;
                mate.MateName = Language.StrNone;
                await mate.UserPackage.AwardItemAsync(Item.TYPE_METEORTEAR);

                await mate.SendAsync(new MsgName
                {
                    Action = StringAction.Mate,
                    Identity = mate.Identity,
                    Strings = new List<string>
                    {
                        mate.MateName
                    }
                });
            }

            user.MateIdentity = 0;
            user.MateName = Language.StrNone;
            await user.SaveAsync();

            await user.SendAsync(new MsgName
            {
                Action = StringAction.Mate,
                Identity = user.Identity,
                Strings = new List<string>
                {
                    user.MateName
                }
            });
            return true;
        }

        private static async Task<bool> ExecuteActionUserMarriageAsync(DbAction action, string param, Character user,
                                                                       Role role, Item item, string input)
        {
            return user?.MateIdentity != 0;
        }

        private static async Task<bool> ExecuteActionUserSexAsync(DbAction action, string param, Character user,
                                                                  Role role,
                                                                  Item item, string input)
        {
            return user?.Gender != 0;
        }

        private static async Task<bool> ExecuteActionUserEffectAsync(DbAction action, string param, Character user,
                                                                     Role role, Item item, string input)
        {
            if (user == null)
                return false;

            string[] parsedString = SplitParam(param);
            if (parsedString.Length < 2)
            {
                await Log.WriteLogAsync(LogLevel.Error,
                                        $"Invalid parsed param[{param}] ExecuteActionUserEffect[{action.Identity}]");
                return false;
            }

            var msg = new MsgName
            {
                Identity = user.Identity,
                Action = StringAction.RoleEffect
            };
            msg.Strings.Add(parsedString[1]);
            switch (parsedString[0].ToLower())
            {
                case "self":
                    await user.BroadcastRoomMsgAsync(msg, true);
                    return true;

                case "couple":
                    await user.BroadcastRoomMsgAsync(msg, true);

                    Character couple = RoleManager.GetUser(user.MateIdentity);
                    if (couple == null)
                        return true;

                    msg.Identity = couple.Identity;
                    await couple.BroadcastRoomMsgAsync(msg, true);
                    return true;

                case "team":
                    if (user.Team == null)
                        return false;

                    foreach (Character member in user.Team.Members)
                    {
                        msg.Identity = member.Identity;
                        await member.BroadcastRoomMsgAsync(msg, true);
                    }

                    return true;
            }

            return false;
        }

        private static async Task<bool> ExecuteActionUserTaskmaskAsync(DbAction action, string param, Character user,
                                                                       Role role, Item item, string input)
        {
            if (user == null)
                return false;

            string[] parsedParam = SplitParam(param);
            if (parsedParam.Length < 2)
            {
                await Log.WriteLogAsync(LogLevel.Warning,
                                        $"ExecuteActionUserTaskmask invalid param num [{param}] for action {action.Identity}");
                return false;
            }

            if (!int.TryParse(parsedParam[1], out int flag) || flag < 0 || flag >= 32)
            {
                await Log.WriteLogAsync(LogLevel.Warning, $"ExecuteActionUserTaskmask invalid mask num {param}");
                return false;
            }

            switch (parsedParam[0].ToLower())
            {
                case "check":
                case "chk":
                    return user.CheckTaskMask(flag);
                case "add":
                    await user.AddTaskMaskAsync(flag);
                    return true;
                case "cls":
                case "clr":
                case "clear":
                    await user.ClearTaskMaskAsync(flag);
                    return true;
            }

            return false;
        }

        private static async Task<bool> ExecuteActionUserMediaplayAsync(DbAction action, string param, Character user,
                                                                        Role role, Item item, string input)
        {
            if (user == null)
                return false;

            string[] pszParam = SplitParam(param);
            if (pszParam.Length < 2)
                return false;

            var msg = new MsgName {Action = StringAction.PlayerWave};
            msg.Strings.Add(pszParam[1]);

            switch (pszParam[0].ToLower())
            {
                case "play":
                    await user.SendAsync(msg);
                    return true;
                case "broadcast":
                    await user.BroadcastRoomMsgAsync(msg, true);
                    return true;
            }

            return false;
        }

        private static async Task<bool> ExecuteActionUserCreatemapAsync(DbAction action, string param, Character user,
                                                                        Role role, Item item, string input)
        {
            if (user == null)
                return false;

            string[] safeParam = SplitParam(param);

            if (safeParam.Length < 10)
            {
                await Log.WriteLogAsync(LogLevel.Error,
                                        $"ExecuteActionUserCreatemap ({action.Identity}) with invalid param length [{param}]");
                return false;
            }

            string szName = safeParam[0];
            uint idOwner = uint.Parse(safeParam[2]),
                 idRebornMap = uint.Parse(safeParam[7]);
            byte nOwnerType = byte.Parse(safeParam[1]);
            uint nMapDoc = uint.Parse(safeParam[3]);
            uint nType = uint.Parse(safeParam[4]);
            uint nRebornPortal = uint.Parse(safeParam[8]);
            byte nResLev = byte.Parse(safeParam[9]);
            ushort usPortalX = ushort.Parse(safeParam[5]),
                   usPortalY = ushort.Parse(safeParam[6]);

            var pMapInfo = new DbDynamap
            {
                Name = szName,
                OwnerIdentity = idOwner,
                OwnerType = nOwnerType,
                Description = $"{user.Name}`{szName}",
                RebornMap = idRebornMap,
                PortalX = usPortalX,
                PortalY = usPortalY,
                LinkMap = user.MapIdentity,
                LinkX = user.MapX,
                LinkY = user.MapY,
                MapDoc = nMapDoc,
                Type = nType,
                RebornPortal = nRebornPortal,
                ResourceLevel = nResLev,
                ServerIndex = (int) Kernel.GameConfiguration.ServerIdentity
            };

            if (!await ServerDbContext.SaveAsync(pMapInfo) || pMapInfo.Identity < 1000000)
            {
                await Log.WriteLogAsync(LogLevel.Error,
                                        $"ExecuteActionUserCreatemap error when saving map\n\t{JsonConvert.SerializeObject(pMapInfo)}");
                return false;
            }

            var map = new GameMap(pMapInfo);
            if (!await map.InitializeAsync())
                return false;

            user.HomeIdentity = pMapInfo.Identity;
            await user.SaveAsync();
            return await MapManager.AddMapAsync(map);
        }

        private static async Task<bool> ExecuteActionUserEnterHomeAsync(DbAction action, string param, Character user,
                                                                        Role role, Item item, string input)
        {
            if (user == null || user.HomeIdentity == 0)
                return false;

            GameMap target = MapManager.GetMap(user.HomeIdentity);

            await user.FlyMapAsync(target.Identity, target.PortalX, target.PortalY);

            if (user.Team != null)
                foreach (Character member in user.Team.Members)
                {
                    if (member.Identity == user.Identity || member.GetDistance(user) > 5)
                        continue;
                    await member.FlyMapAsync(target.Identity, target.PortalX, target.PortalY);
                }

            return true;
        }

        private static async Task<bool> ExecuteActionUserEnterMateHomeAsync(
            DbAction action, string param, Character user,
            Role role, Item item, string input)
        {
            if (user == null)
                return false;

            uint idMap = 0;
            Character mate = RoleManager.GetUser(user.MateIdentity);
            if (mate == null)
            {
                DbCharacter dbMate = await CharactersRepository.FindByIdentityAsync(user.MateIdentity);
                idMap = dbMate.HomeIdentity;
            }
            else
            {
                idMap = mate.HomeIdentity;
            }

            if (idMap == 0)
                return false;

            GameMap map = MapManager.GetMap(idMap);
            if (map == null)
                return false;

            await user.FlyMapAsync(map.Identity, map.PortalX, map.PortalY);
            return true;
        }

        private static async Task<bool> ExecuteActionUserUnlearnMagicAsync(
            DbAction action, string param, Character user,
            Role role, Item item, string input)
        {
            if (user == null)
                return false;

            string[] magicsIds = SplitParam(param);

            foreach (string id in magicsIds)
            {
                ushort idMagic = ushort.Parse(id);
                if (user.MagicData.CheckType(idMagic)) await user.MagicData.UnlearnMagicAsync(idMagic, false);
            }

            return true;
        }

        private static async Task<bool> ExecuteActionUserRebirthAsync(DbAction action, string param, Character user,
                                                                      Role role, Item item, string input)
        {
            if (user == null)
                return false;

            string[] splitParam = SplitParam(param);

            if (!ushort.TryParse(splitParam[0], out ushort prof)
                || !ushort.TryParse(splitParam[1], out ushort look))
            {
                await Log.WriteLogAsync(LogLevel.Warning, $"Invalid parameter to rebirth {param}, {action.Identity}");
                return false;
            }

            return await user.RebirthAsync(prof, look);
        }

        private static async Task<bool> ExecuteActionUserWebpageAsync(DbAction action, string param, Character user,
                                                                      Role role, Item item, string input)
        {
            if (user == null)
                return false;

            await user.SendAsync(param, TalkChannel.Website);
            return true;
        }

        private static async Task<bool> ExecuteActionUserBbsAsync(DbAction action, string param, Character user,
                                                                  Role role,
                                                                  Item item, string input)
        {
            if (user == null)
                return false;

            await user.SendAsync(param, TalkChannel.Bbs);
            return true;
        }

        private static async Task<bool> ExecuteActionUserUnlearnSkillAsync(
            DbAction action, string param, Character user,
            Role role, Item item, string input)
        {
            if (user == null)
                return false;

            return await user.UnlearnAllSkillAsync();
        }

        private static async Task<bool> ExecuteActionUserDropMagicAsync(DbAction action, string param, Character user,
                                                                        Role role, Item item, string input)
        {
            if (user == null)
                return false;

            string[] magicsIds = SplitParam(param);

            foreach (string id in magicsIds)
            {
                ushort idMagic = ushort.Parse(id);
                if (user.MagicData.CheckType(idMagic)) await user.MagicData.UnlearnMagicAsync(idMagic, true);
            }

            return true;
        }

        private static async Task<bool> ExecuteActionUserOpenDialogAsync(DbAction action, string param, Character user,
                                                                         Role role, Item item, string input)
        {
            if (user == null)
                return false;

            switch ((OpenWindow) action.Data)
            {
                case OpenWindow.VipWarehouse:
                    if (user.BaseVipLevel == 0)
                        return false;
                    break;
            }

            await user.SendAsync(new MsgAction
            {
                Identity = user?.Identity ?? role?.Identity ?? 0,
                Command = action.Data,
                Action = MsgAction<Client>.ActionType.ClientDialog,
                Map = user?.MapIdentity ?? role?.MapIdentity ?? 0,
                ArgumentX = user?.MapX ?? role?.MapX ?? 0,
                ArgumentY = user?.MapY ?? role?.MapY ?? 0
            });
            return true;
        }

        private static async Task<bool> ExecuteActionUserFixAttrAsync(DbAction action, string param, Character user,
                                                                      Role role, Item item, string input)
        {
            if (user == null)
                return false;

            var attr = (ushort) (user.Agility + user.Vitality + user.Strength + user.Spirit + user.AttributePoints -
                                 10);
            ushort profSort = user.ProfessionSort;
            if (profSort == 13 || profSort == 14)
                profSort = 10;

            DbPointAllot pointAllot = ExperienceManager.GetPointAllot(profSort, 1);
            if (pointAllot != null)
            {
                await user.SetAttributesAsync(ClientUpdateType.Strength, pointAllot.Strength);
                await user.SetAttributesAsync(ClientUpdateType.Agility, pointAllot.Agility);
                await user.SetAttributesAsync(ClientUpdateType.Vitality, pointAllot.Vitality);
                await user.SetAttributesAsync(ClientUpdateType.Spirit, pointAllot.Spirit);
            }
            else
            {
                await user.SetAttributesAsync(ClientUpdateType.Strength, 5);
                await user.SetAttributesAsync(ClientUpdateType.Agility, 2);
                await user.SetAttributesAsync(ClientUpdateType.Vitality, 3);
                await user.SetAttributesAsync(ClientUpdateType.Spirit, 0);
            }

            await user.SetAttributesAsync(ClientUpdateType.Atributes, attr);
            return true;
        }

        private static async Task<bool> ExecuteActionUserExpMultiplyAsync(DbAction action, string param, Character user,
                                                                          Role role, Item item, string input)
        {
            string[] pszParam = SplitParam(param);
            if (pszParam.Length < 2)
                return false;

            uint time = uint.Parse(pszParam[1]);
            float multiply = int.Parse(pszParam[0]) / 100f;
            await user.SetExperienceMultiplierAsync(time, multiply);
            return true;
        }

        private static async Task<bool> ExecuteActionUserWhPasswordAsync(DbAction action, string param, Character user,
                                                                         Role role, Item item, string input)
        {
            if (user == null)
                return false;

            if (user.SecondaryPassword == 0)
                return true;

            if (string.IsNullOrEmpty(input))
                return false;

            if (input.Length < 1 || input.Length > ulong.MaxValue.ToString().Length)
                return false;

            if (!ulong.TryParse(input, out ulong password))
                return false;

            return user.SecondaryPassword == password;
        }

        private static async Task<bool> ExecuteActionUserSetWhPasswordAsync(
            DbAction action, string param, Character user,
            Role role, Item item, string input)
        {
            if (user == null)
                return false;

            bool parsed = ulong.TryParse(input, out ulong password);
            if (string.IsNullOrEmpty(input) || parsed && password == 0)
            {
                user.SecondaryPassword = 0;
                await user.SaveAsync();
                return true;
            }

            if (input.Length < 1 || !parsed)
                return false;

            user.SecondaryPassword = password;
            await user.SaveAsync();
            return true;
        }

        private static async Task<bool> ExecuteActionUserOpeninterfaceAsync(
            DbAction action, string param, Character user,
            Role role, Item item, string input)
        {
            if (user == null) return false;

            await user.SendAsync(new MsgAction
            {
                Identity = user.Identity,
                Command = action.Data,
                Action = MsgAction<Client>.ActionType.ClientCommand,
                ArgumentX = user.MapX,
                ArgumentY = user.MapY
            });
            return true;
        }

        private static async Task<bool> ExecuteActionUserTaskManagerAsync(DbAction action, string param, Character user,
                                                                          Role role, Item item, string input)
        {
            if (user?.TaskDetail == null)
                return false;

            if (action.Data == 0)
                return false;

            switch (param.ToLowerInvariant())
            {
                case "new":
                    if (user.TaskDetail.QueryTaskData(action.Data) != null)
                        return false;
                    return await user.TaskDetail.CreateNewAsync(action.Data);
                case "isexit":
                    return user.TaskDetail.QueryTaskData(action.Data) != null;
                case "delete":
                    return await user.TaskDetail.DeleteTaskAsync(action.Data);
            }

            return false;
        }

        private static async Task<bool> ExecuteActionUserTaskOpeAsync(DbAction action, string param, Character user,
                                                                      Role role, Item item, string input)
        {
            if (user?.TaskDetail == null)
                return false;

            if (action.Data == 0)
                return false;

            string[] splitParam = SplitParam(param, 3);
            if (splitParam.Length != 3)
                return false;

            string ope = splitParam[0].ToLowerInvariant(),
                   opt = splitParam[1].ToLowerInvariant();
            int data = int.Parse(splitParam[2]);

            if (ope.Equals("complete"))
            {
                if (opt.Equals("==")) return user.TaskDetail.QueryTaskData(action.Data)?.CompleteFlag == data;

                if (opt.Equals("set")) return await user.TaskDetail.SetCompleteAsync(action.Data, data);

                return false;
            }

            if (ope.StartsWith("data"))
            {
                switch (opt)
                {
                    case ">":
                        return user.TaskDetail.GetData(action.Data, ope) > data;
                    case "<":
                        return user.TaskDetail.GetData(action.Data, ope) < data;
                    case ">=":
                        return user.TaskDetail.GetData(action.Data, ope) >= data;
                    case "<=":
                        return user.TaskDetail.GetData(action.Data, ope) <= data;
                    case "==":
                        return user.TaskDetail.GetData(action.Data, ope) == data;
                    case "+=":
                        return await user.TaskDetail.AddDataAsync(action.Data, ope, data);
                    case "set":
                        return await user.TaskDetail.SetDataAsync(action.Data, ope, data);
                }

                return false;
            }

            if (ope.Equals("notify"))
            {
                DbTaskDetail detail = user.TaskDetail.QueryTaskData(action.Data);
                if (detail == null)
                    return false;

                detail.NotifyFlag = (byte) data;
                return await user.TaskDetail.SaveAsync(detail);
            }

            if (ope.Equals("overtime"))
            {
                DbTaskDetail detail = user.TaskDetail.QueryTaskData(action.Data);
                if (detail == null)
                    return false;

                detail.TaskOvertime = (uint) data;
                return await user.TaskDetail.SaveAsync(detail);
            }

            return true;
        }

        private static async Task<bool> ExecuteActionUserTaskLocaltimeAsync(
            DbAction action, string param, Character user,
            Role role, Item item, string input)
        {
            if (user?.TaskDetail == null)
                return false;

            if (action.Data == 0)
                return false;

            string[] splitParam = SplitParam(param, 3);
            if (splitParam.Length != 3)
                return false;

            string ope = splitParam[0].ToLowerInvariant(),
                   opt = splitParam[1].ToLowerInvariant();
            int data = int.Parse(splitParam[2]);

            if (ope.StartsWith("interval", StringComparison.InvariantCultureIgnoreCase))
            {
                DbTaskDetail detail = user.TaskDetail.QueryTaskData(action.Data);
                if (detail == null)
                    return true;

                int mode = int.Parse(GetParenthesys(ope));
                switch (mode)
                {
                    case 0: // seconds
                    {
                        DateTime timeStamp = DateTime.Now;
                        var nDiff = (int) (timeStamp - UnixTimestamp.ToDateTime(detail.TaskOvertime)).TotalSeconds;
                        switch (opt)
                        {
                            case "==": return nDiff == data;
                            case "<":  return nDiff < data;
                            case ">":  return nDiff > data;
                            case "<=": return nDiff <= data;
                            case ">=": return nDiff >= data;
                            case "<>":
                            case "!=": return nDiff != data;
                        }

                        return false;
                    }

                    case 1: // days
                        int interval = int.Parse(DateTime.Now.ToString("yyyyMMdd")) -
                                       int.Parse(UnixTimestamp.ToDateTime(detail.TaskOvertime).ToString("yyyyMMdd"));
                        switch (opt)
                        {
                            case "==": return interval == data;
                            case "<":  return interval < data;
                            case ">":  return interval > data;
                            case "<=": return interval <= data;
                            case ">=": return interval >= data;
                            case "!=":
                            case "<>": return interval != data;
                        }

                        return false;
                    default:
                        await Log.WriteLogAsync(LogLevel.Warning,
                                                $"Unhandled Time mode ({mode}) on action (id:{action.Identity})");
                        return false;
                }
            }

            if (opt.Equals("set"))
            {
                DbTaskDetail detail = user.TaskDetail.QueryTaskData(action.Data);
                if (detail == null)
                    return false;

                detail.TaskOvertime = (uint) data;
                return await user.TaskDetail.SaveAsync(detail);
            }

            return false;
        }

        private static async Task<bool> ExecuteActionUserTaskFindAsync(DbAction action, string param, Character user,
                                                                       Role role, Item item, string input)
        {
            await Log.WriteLogAsync(LogLevel.Warning, "ExecuteActionUserTaskFind unhandled");
            return false;
        }

        private static async Task<bool> ExecuteActionUserVarCompareAsync(DbAction action, string param, Character user,
                                                                         Role role, Item item, string input)
        {
            string[] pszParam = SplitParam(param);
            if (pszParam.Length < 3)
                return false;

            byte varId = VarId(pszParam[0]);
            string opt = pszParam[1];
            long value = long.Parse(pszParam[2]);

            if (varId >= Role.MAX_VAR_AMOUNT)
                return false;

            switch (opt)
            {
                case "==":
                    return user.VarData[varId] == value;
                case ">=":
                    return user.VarData[varId] >= value;
                case "<=":
                    return user.VarData[varId] <= value;
                case ">":
                    return user.VarData[varId] > value;
                case "<":
                    return user.VarData[varId] < value;
                case "!=":
                    return user.VarData[varId] != value;
                default:
                    return false;
            }
        }

        private static async Task<bool> ExecuteActionUserVarDefineAsync(DbAction action, string param, Character user,
                                                                        Role role, Item item, string input)
        {
            string[] safeParam = SplitParam(param);
            if (safeParam.Length < 3)
                return false;

            byte varId = VarId(safeParam[0]);
            string opt = safeParam[1];
            long value = long.Parse(safeParam[2]);

            if (varId >= Role.MAX_VAR_AMOUNT)
                return false;

            try
            {
                switch (opt)
                {
                    case "set":
                        user.VarData[varId] = value;
                        return true;
                }
            }
            catch
            {
                return false;
            }

            return false;
        }

        private static async Task<bool> ExecuteActionUserVarCalcAsync(DbAction action, string param, Character user,
                                                                      Role role, Item item, string input)
        {
            string[] safeParam = SplitParam(param);
            if (safeParam.Length < 3)
                return false;

            byte varId = VarId(safeParam[0]);
            string opt = safeParam[1];
            long value = long.Parse(safeParam[2]);

            if (opt == "/=" && value == 0)
                return false; // division by zero

            if (varId >= Role.MAX_VAR_AMOUNT)
                return false;

            switch (opt)
            {
                case "+=":
                    user.VarData[varId] += value;
                    return true;
                case "-=":
                    user.VarData[varId] -= value;
                    return true;
                case "*=":
                    user.VarData[varId] *= value;
                    return true;
                case "/=":
                    user.VarData[varId] /= value;
                    return true;
                case "mod=":
                    user.VarData[varId] %= value;
                    return true;
                default:
                    return false;
            }
        }

        private static async Task<bool> ExecuteActionUserTestEquipmentAsync(
            DbAction action, string param, Character user,
            Role role, Item item, string input)
        {
            if (user == null)
                return false;

            string[] splitParams = SplitParam(param, 2);
            if (!ushort.TryParse(splitParams[0], out ushort pos)
                || !int.TryParse(splitParams[1], out int type))
            {
                await Log.WriteLogAsync(LogLevel.Error,
                                        $"Invalid parsed param ExecuteActionUserTestEquipment, id[{action.Identity}]");
                return false;
            }

            if (!Enum.IsDefined(typeof(Item.ItemPosition), pos))
                return false;

            Item temp = user.UserPackage[(Item.ItemPosition) pos];
            if (temp == null)
                return false;
            return temp.GetItemSubType() == type;
        }

        private static async Task<bool> ExecuteActionUserExecActionAsync(DbAction action, string param, Character user,
                                                                         Role role, Item item, string input)
        {
            if (user == null)
                return false;

            string[] splitParams = SplitParam(param, 3);
            if (splitParams.Length < 3) return false;

            if (!int.TryParse(splitParams[0], out int secSpan)
                || !uint.TryParse(splitParams[1], out uint idAction))
                return false;

            EventManager.QueueAction(new QueuedAction(secSpan, idAction, user.Identity));
            return true;
        }

        private static async Task<bool> ExecuteActionUserStcCompareAsync(DbAction action, string param, Character user,
                                                                         Role role, Item item, string input)
        {
            if (user == null)
                return false;

            string[] pszParam = SplitParam(param);
            if (pszParam.Length < 3)
                return false;

            string szStc = GetParenthesys(pszParam[0]);
            string opt = pszParam[1];
            long value = long.Parse(pszParam[2]);

            string[] pStc = szStc.Trim().Split(',');

            if (pStc.Length < 2)
                return false;

            uint idEvent = uint.Parse(pStc[0]);
            uint idType = uint.Parse(pStc[1]);

            DbStatistic dbStc = user.Statistic.GetStc(idEvent, idType);

            long data = dbStc?.Data ?? 0L;
            switch (opt)
            {
                case ">=":
                    return data >= value;
                case "<=":
                    return data <= value;
                case ">":
                    return data > value;
                case "<":
                    return data < value;
                case "!=":
                case "<>":
                    return data != value;
                case "=":
                case "==":
                    return data == value;
            }

            return false;
        }

        private static async Task<bool> ExecuteActionUserStcOpeAsync(DbAction action, string param, Character user,
                                                                     Role role, Item item, string input)
        {
            string[] pszParam = SplitParam(param);
            if (pszParam.Length < 3)
                return false;

            string szStc = GetParenthesys(pszParam[0]);
            string opt = pszParam[1];
            long value = long.Parse(pszParam[2]);
            bool bUpdate = pszParam[3] != "0";

            string[] pStc = szStc.Trim().Split(',');

            if (pStc.Length < 2)
                return false;

            uint idEvent = uint.Parse(pStc[0]);
            uint idType = uint.Parse(pStc[1]);

            if (!user.Statistic.HasEvent(idEvent, idType))
                return await user.Statistic.AddOrUpdateAsync(idEvent, idType, (uint) value, bUpdate);

            switch (opt)
            {
                case "+=":
                    if (value == 0) return false;

                    long tempValue = user.Statistic.GetValue(idEvent, idType) + value;
                    return await user.Statistic.AddOrUpdateAsync(idEvent, idType, (uint) Math.Max(0, tempValue),
                                                                 bUpdate);
                case "=":
                case "set":
                    if (value < 0) return false;
                    return await user.Statistic.AddOrUpdateAsync(idEvent, idType, (uint) Math.Max(0, value), bUpdate);
            }

            return false;
        }

        private static async Task<bool> ExecuteActionUserDataSyncAsync(DbAction action, string param, Character user,
                                                                       Role role, Item item, string input)
        {
            if (user == null)
                return false;

            string[] splitParam = SplitParam(param);
            if (splitParam.Length < 3)
                return false;

            string act = splitParam[0];
            uint type = uint.Parse(splitParam[1]);
            uint data = uint.Parse(splitParam[2]);

            if (act.Equals("send"))
            {
                await user.SendAsync(new MsgAction
                {
                    Identity = user.Identity,
                    Action = (MsgAction<Client>.ActionType) type,
                    Command = data,
                    ArgumentX = user.MapX,
                    ArgumentY = user.MapY,
                    X = user.MapX,
                    Y = user.MapY
                });
                return true;
            }

            return false;
        }

        private static async Task<bool> ExecuteActionUserSelectToDataAsync(
            DbAction action, string param, Character user,
            Role role, Item item, string input)
        {
            if (user == null)
                return false;

            try
            {
                user.VarData[action.Data] = long.Parse(await ServerDbContext.ScalarAsync(param));
            }
            catch (Exception ex)
            {
                await Log.WriteLogAsync(LogLevel.Exception, ex.ToString());
                return false;
            }

            return true;
        }

        private static async Task<bool> ExecuteActionUserStcTimeCheckAsync(
            DbAction action, string param, Character user,
            Role role, Item item, string input)
        {
            string[] pszParam = SplitParam(param);
            if (pszParam.Length < 3)
                return false;

            string szStc = GetParenthesys(pszParam[0]);
            string opt = pszParam[1];
            long value = long.Parse(pszParam[2]);

            string[] pStc = szStc.Trim().Split(',');

            if (pStc.Length <= 2)
                return false;

            uint idEvent = uint.Parse(pStc[0]);
            uint idType = uint.Parse(pStc[1]);
            byte mode = byte.Parse(pStc[2]);

            if (value < 0)
                return false;

            DbStatistic dbStc = user.Statistic.GetStc(idEvent, idType);
            if (dbStc?.Timestamp == null)
                return true;

            switch (mode)
            {
                case 0: // seconds
                {
                    DateTime timeStamp = DateTime.Now;
                    var nDiff = (int) (timeStamp - dbStc.Timestamp.Value).TotalSeconds;
                    switch (opt)
                    {
                        case "==": return nDiff == value;
                        case "<":  return nDiff < value;
                        case ">":  return nDiff > value;
                        case "<=": return nDiff <= value;
                        case ">=": return nDiff >= value;
                        case "<>":
                        case "!=": return nDiff != value;
                    }

                    return false;
                }

                case 1: // days
                    int interval = int.Parse(DateTime.Now.ToString("yyyyMMdd")) -
                                   int.Parse(dbStc.Timestamp.Value.ToString("yyyyMMdd"));
                    switch (opt)
                    {
                        case "==": return interval == value;
                        case "<":  return interval < value;
                        case ">":  return interval > value;
                        case "<=": return interval <= value;
                        case ">=": return interval >= value;
                        case "!=":
                        case "<>": return interval != value;
                    }

                    return false;
                default:
                    await Log.WriteLogAsync(LogLevel.Warning,
                                            $"Unhandled Time mode ({mode}) on action (id:{action.Identity})");
                    return false;
            }
        }

        private static async Task<bool> ExecuteActionUserStcTimeOpeAsync(DbAction action, string param, Character user,
                                                                         Role role, Item item, string input)
        {
            string[] pszParam = SplitParam(param);
            if (pszParam.Length < 3)
                return false;

            string szStc = GetParenthesys(pszParam[0]);
            string opt = pszParam[1];
            long value = long.Parse(pszParam[2]);

            string[] pStc = szStc.Trim().Split(',');

            uint idEvent = uint.Parse(pStc[0]);
            uint idType = uint.Parse(pStc[1]);

            switch (opt)
            {
                case "set":
                {
                    if (value > 0)
                        return await user.Statistic.SetTimestampAsync(idEvent, idType, DateTime.Now);
                    return await user.Statistic.SetTimestampAsync(idEvent, idType, DateTime.Now);
                }
            }

            return false;
        }

        private static async Task<bool> ExecuteActionUserAttachStatusAsync(
            DbAction action, string param, Character user,
            Role role, Item item, string input)
        {
            // self add 64 200 900 0
            string[] pszParam = SplitParam(param);
            if (pszParam.Length < 6)
                return false;

            string target = pszParam[0].ToLower();
            string opt = pszParam[1].ToLower();
            int status = int.Parse(pszParam[2]) + 3;
            int multiply = int.Parse(pszParam[3]);
            uint seconds = uint.Parse(pszParam[4]);
            int times = int.Parse(pszParam[5]);
            // last param unknown

            if (target == "team" && user.Team == null)
                return false;

            if (target == "self")
            {
                if (opt == "add")
                    await user.AttachStatusAsync(user, status, multiply, (int) seconds, times, 0);
                else if (opt == "del")
                    await user.DetachStatusAsync(status);
                return true;
            }

            if (target == "team")
            {
                foreach (Character member in user.Team.Members)
                    if (opt == "add")
                        await member.AttachStatusAsync(member, status, multiply, (int) seconds, times, 0);
                    else if (opt == "del")
                        await member.DetachStatusAsync(status);

                return true;
            }

            if (target == "couple")
            {
                Character mate = RoleManager.GetUser(user.MateIdentity);
                if (mate == null)
                    return false;

                if (opt == "add")
                {
                    await user.AttachStatusAsync(user, status, multiply, (int) seconds, times, 0);
                    await mate.AttachStatusAsync(user, status, multiply, (int) seconds, times, 0);
                }
                else if (opt == "del")
                {
                    await user.DetachStatusAsync(status);
                    await mate.DetachStatusAsync(status);
                }

                return true;
            }

            return false;
        }

        private static async Task<bool> ExecuteActionUserGodTimeAsync(DbAction action, string param, Character user,
                                                                      Role role, Item item, string input)
        {
            string[] pszParma = SplitParam(param);

            if (pszParma.Length < 2)
                return false;

            string opt = pszParma[0];
            uint minutes = uint.Parse(pszParma[1]);

            switch (opt)
            {
                case "+=":
                {
                    return await user.AddBlessingAsync(minutes);
                }
            }

            return true;
        }

        private static async Task<bool> ExecuteActionUserExpballExpAsync(DbAction action, string param, Character user,
                                                                         Role role, Item item, string input)
        {
            string[] pszParam = SplitParam(param);

            if (pszParam.Length < 2)
                return false;

            int dwExpTimes = int.Parse(pszParam[0]);
            byte idData = byte.Parse(pszParam[1]);

            if (idData >= user.VarData.Length) return false;

            long exp = user.CalculateExpBall(dwExpTimes);
            user.VarData[idData] = exp;
            return true;
        }

        private static async Task<bool> ExecuteActionUserStatusCreateAsync(
            DbAction action, string param, Character user,
            Role role, Item item, string input)
        {
            if (user == null)
                return false;

            // sort leave_times remain_time end_time interval_time
            // 200 0 604800 0 604800 1
            if (action.Data == 0)
            {
                await Log.WriteLogAsync(LogLevel.Error, $"ERROR: invalid data num {action.Identity}");
                return false;
            }

            string[] pszParam = SplitParam(param);
            if (pszParam.Length < 5)
            {
                await Log.WriteLogAsync(LogLevel.Error, $"ERROR: invalid param num {action.Identity}");
                return false;
            }

            uint sort = uint.Parse(pszParam[0]);
            uint leaveTimes = uint.Parse(pszParam[1]);
            uint remainTime = uint.Parse(pszParam[2]);
            uint intervalTime = uint.Parse(pszParam[4]);
            bool save = pszParam[5] != "0"; // ??

            await user.AttachStatusAsync(user, (int) action.Data, 0, (int) remainTime, (int) leaveTimes, 0, save);
            return true;
        }

        private static async Task<bool> ExecuteActionUserStatusCheckAsync(DbAction action, string param, Character user,
                                                                          Role role, Item item, string input)
        {
            if (user?.StatusSet == null) return false;

            string[] status = SplitParam(param);

            switch (action.Data)
            {
                case 0: // check
                    foreach (string st in status)
                        if (user.QueryStatus(int.Parse(st)) == null)
                            return false;
                    return true;

                case 1:
                    foreach (string st in status)
                        if (user.QueryStatus(int.Parse(st)) != null)
                        {
                            await user.DetachStatusAsync(int.Parse(st));
                            DbStatus db =
                                (await StatusRepository.GetAsync(user.Identity)).FirstOrDefault(x =>
                                    x.Status == uint.Parse(st));
                            if (db != null)
                                await ServerDbContext.DeleteAsync(db);
                        }

                    return true;
            }

            return false;
        }

        #endregion

        #region Team

        private static async Task<bool> ExecuteActionTeamBroadcastAsync(DbAction action, string param, Character user,
                                                                        Role role, Item item, string input)
        {
            if (user?.Team == null || user.Team.MemberCount < 1)
            {
                await Log.WriteLogAsync(LogLevel.Action,
                                        $"ExecuteActionTeamBroadcast user or team is null {action.Identity}");
                return false;
            }

            if (!user.Team.IsLeader(user.Identity))
                return false;

            await user.Team.SendAsync(new MsgTalk(user.Identity, TalkChannel.Team, Color.White, param));
            return true;
        }

        private static async Task<bool> ExecuteActionTeamAttrAsync(DbAction action, string param, Character user,
                                                                   Role role,
                                                                   Item item, string input)
        {
            if (user?.Team == null)
            {
                await Log.WriteLogAsync(LogLevel.Action,
                                        $"ExecuteActionTeamAttr user or team is null {action.Identity}");
                return false;
            }

            string[] splitParams = SplitParam(param, 3);
            if (splitParams.Length < 3) // invalid param num
                return false;

            string cmd = splitParams[0].ToLower();
            string opt = splitParams[1];
            int.TryParse(splitParams[2], out int value);

            if (cmd.Equals("count"))
            {
                if (opt.Equals("<"))
                    return user.Team.MemberCount < value;
                if (opt.Equals("=="))
                    return user.Team.MemberCount == value;
            }

            foreach (Character member in user.Team.Members)
                if (cmd.Equals("money"))
                {
                    if (opt.Equals("+="))
                        await member.ChangeMoneyAsync(value);
                    else if (opt.Equals("<"))
                        return member.Silvers < value;
                    else if (opt.Equals("=="))
                        return member.Silvers == value;
                    else if (opt.Equals(">")) return member.Silvers > value;
                }
                else if (cmd.Equals("emoney"))
                {
                    if (opt.Equals("+="))
                        await member.ChangeConquerPointsAsync(value);
                    else if (opt.Equals("<"))
                        return member.ConquerPoints < value;
                    else if (opt.Equals("=="))
                        return member.ConquerPoints == value;
                    else if (opt.Equals(">")) return member.ConquerPoints > value;
                }
                else if (cmd.Equals("level"))
                {
                    if (opt.Equals("<"))
                        return member.Level < value;
                    if (opt.Equals("=="))
                        return member.Level == value;
                    if (opt.Equals(">")) return member.Level > value;
                }
                else if (cmd.Equals("vip"))
                {
                    if (opt.Equals("<"))
                        return member.BaseVipLevel < value;
                    if (opt.Equals("=="))
                        return member.BaseVipLevel == value;
                    if (opt.Equals(">")) return member.BaseVipLevel > value;
                }
                else if (cmd.Equals("mate"))
                {
                    if (member.Identity == user.Identity)
                        continue;

                    if (member.MateIdentity != user.Identity)
                        return false;
                }
                else if (cmd.Equals("friend"))
                {
                    if (member.Identity == user.Identity)
                        continue;

                    if (!user.IsFriend(member.Identity))
                        return false;
                }
                else if (cmd.Equals("count_near"))
                {
                    if (member.Identity == user.Identity)
                        continue;

                    if (!(member.MapIdentity == user.MapIdentity && member.IsAlive))
                        return false;
                }

            return true;
        }

        private static async Task<bool> ExecuteActionTeamLeavespaceAsync(DbAction action, string param, Character user,
                                                                         Role role, Item item, string input)
        {
            if (user?.Team == null)
            {
                await Log.WriteLogAsync(LogLevel.Action,
                                        $"ExecuteActionTeamLeavespace user or team is null {action.Identity}");
                return false;
            }

            if (!int.TryParse(param, out int space))
                return false;

            foreach (Character member in user.Team.Members)
                if (!member.UserPackage.IsPackSpare(space))
                    return false;
            return true;
        }

        private static async Task<bool> ExecuteActionTeamItemAddAsync(DbAction action, string param, Character user,
                                                                      Role role, Item item, string input)
        {
            if (user?.Team == null)
            {
                await Log.WriteLogAsync(LogLevel.Action,
                                        $"ExecuteActionTeamItemAdd user or team is null {action.Identity}");
                return false;
            }

            return true;
        }

        private static async Task<bool> ExecuteActionTeamItemDelAsync(DbAction action, string param, Character user,
                                                                      Role role, Item item, string input)
        {
            if (user?.Team == null)
            {
                await Log.WriteLogAsync(LogLevel.Action,
                                        $"ExecuteActionTeamItemDel user or team is null {action.Identity}");
                return false;
            }

            foreach (Character member in user.Team.Members)
                await member.UserPackage.AwardItemAsync(action.Data);
            return true;
        }

        private static async Task<bool> ExecuteActionTeamItemCheckAsync(DbAction action, string param, Character user,
                                                                        Role role, Item item, string input)
        {
            if (user?.Team == null)
            {
                await Log.WriteLogAsync(LogLevel.Action,
                                        $"ExecuteActionTeamItemCheck user or team is null {action.Identity}");
                return false;
            }

            foreach (Character member in user.Team.Members)
                await member.UserPackage.SpendItemAsync(action.Data);

            return true;
        }

        private static async Task<bool> ExecuteActionTeamChgmapAsync(DbAction action, string param, Character user,
                                                                     Role role, Item item, string input)
        {
            if (user?.Team == null)
            {
                await Log.WriteLogAsync(LogLevel.Action,
                                        $"ExecuteActionTeamChgmap user or team is null {action.Identity}");
                return false;
            }

            foreach (Character member in user.Team.Members)
                if (member.UserPackage.GetItemByType(action.Data) == null)
                    return false;

            return true;
        }

        private static async Task<bool> ExecuteActionTeamChkIsleaderAsync(DbAction action, string param, Character user,
                                                                          Role role, Item item, string input)
        {
            if (user?.Team == null)
            {
                await Log.WriteLogAsync(LogLevel.Action,
                                        $"ExecuteActionTeamChkIsleader user or team is null {action.Identity}");
                return false;
            }

            return user.Team.IsLeader(user.Identity);
        }

        #endregion

        #region General

        private static async Task<bool> ExecuteGeneralLotteryAsync(DbAction action, string param, Character user,
                                                                   Role role,
                                                                   Item item,
                                                                   string input)
        {
            if (user == null)
            {
                await Log.WriteLogAsync(LogLevel.Error, $"No user for ExecuteGeneralLottery, {action.Identity}");
                return false;
            }
            
            for (int i = 0; i < 10; i++) // 10 tries
            {
                if (await LotteryManager.QueryPrizeAsync(user, (int) action.Data))
                    break;
            }

            return true;
        }

        private static async Task<bool> ExecuteActionChgMapSquareAsync(DbAction action, string param, Character user,
                                                                       Role role, Item item,
                                                                       string input)
        {
            if (user == null)
                return false;

            string[] splitParams = SplitParam(param, 5);

            if (!uint.TryParse(splitParams[0], out uint idMap)
                || !ushort.TryParse(splitParams[1], out ushort x)
                || !ushort.TryParse(splitParams[2], out ushort y)
                || !ushort.TryParse(splitParams[3], out ushort cx)
                || !ushort.TryParse(splitParams[4], out ushort cy))
                return false;

            GameMap map = MapManager.GetMap(idMap);
            if (map == null)
            {
                await Log.WriteLogAsync(LogLevel.Warning, $"GameAction::ExecuteActionChgMapSquare invalid map {idMap}");
                return false;
            }

            for (var i = 0; i < 10; i++)
            {
                x = (ushort) (x + await Kernel.NextAsync(cx));
                y = (ushort) (y + await Kernel.NextAsync(cy));

                if (map.IsStandEnable(x, y))
                    break;
            }

            await user.FlyMapAsync(idMap, x, y);
            return true;
        }

        #endregion

        #region Event

        private static async Task<bool> ExecuteActionEventSetstatusAsync(DbAction action, string param, Character user,
                                                                         Role role, Item item, string input)
        {
            string[] pszParam = SplitParam(param);

            if (pszParam.Length < 3) return false;

            uint idMap = uint.Parse(pszParam[0]);
            ulong nStatus = ulong.Parse(pszParam[1]);
            int nFlag = int.Parse(pszParam[2]);

            GameMap map = MapManager.GetMap(idMap);
            if (map == null)
                return false;

            if (nFlag == 0)
                map.Flag &= ~nStatus;
            else
                map.Flag |= nStatus;
            return true;
        }

        private static async Task<bool> ExecuteActionEventDelnpcGenidAsync(
            DbAction action, string param, Character user,
            Role role, Item item, string input)
        {
            foreach (Role monster in RoleManager
                         .QueryRoles(x => x is Monster monster && monster.GeneratorId == action.Data))
                await monster.LeaveMapAsync();

            return true;
        }

        private static async Task<bool> ExecuteActionEventCompareAsync(DbAction action, string param, Character user,
                                                                       Role role, Item item, string input)
        {
            string[] pszParam = SplitParam(param);

            if (pszParam.Length < 3)
                return false;

            long nData1 = long.Parse(pszParam[0]), nData2 = long.Parse(pszParam[2]);
            string szOpt = pszParam[1];

            switch (szOpt)
            {
                case "==":
                    return nData1 == nData2;
                case "<":
                    return nData1 < nData2;
                case ">":
                    return nData1 > nData2;
                case "<=":
                    return nData1 <= nData2;
                case ">=":
                    return nData1 >= nData2;
            }

            return false;
        }

        private static async Task<bool> ExecuteActionEventCompareUnsignedAsync(
            DbAction action, string param, Character user,
            Role role, Item item, string input)
        {
            string[] pszParam = SplitParam(param);

            if (pszParam.Length < 3)
                return false;

            ulong nData1 = ulong.Parse(pszParam[0]), nData2 = ulong.Parse(pszParam[2]);
            string szOpt = pszParam[1];

            switch (szOpt)
            {
                case "==":
                    return nData1 == nData2;
                case "<":
                    return nData1 < nData2;
                case ">":
                    return nData1 > nData2;
                case "<=":
                    return nData1 <= nData2;
                case ">=":
                    return nData1 >= nData2;
            }

            return false;
        }

        private static async Task<bool> ExecuteActionEventChangeweatherAsync(
            DbAction action, string param, Character user,
            Role role, Item item, string input)
        {
            return true;
        }

        private static async Task<bool> ExecuteActionEventCreatepetAsync(DbAction action, string param, Character user,
                                                                         Role role, Item item, string input)
        {
            string[] pszParam = SplitParam(param);

            if (pszParam.Length < 7) return false;

            uint dwOwnerType = uint.Parse(pszParam[0]);
            uint idOwner = uint.Parse(pszParam[1]);
            uint idMap = uint.Parse(pszParam[2]);
            ushort usPosX = ushort.Parse(pszParam[3]);
            ushort usPosY = ushort.Parse(pszParam[4]);
            uint idGen = uint.Parse(pszParam[5]);
            uint idType = uint.Parse(pszParam[6]);
            uint dwData = 0;
            var szName = "";

            if (pszParam.Length >= 8)
                dwData = uint.Parse(pszParam[7]);
            if (pszParam.Length >= 9)
                szName = pszParam[8];

            DbMonstertype monstertype = RoleManager.GetMonstertype(idType);
            GameMap map = MapManager.GetMap(idMap);

            if (monstertype == null || map == null)
            {
                await Log.WriteLogAsync(LogLevel.Warning,
                                        $"ExecuteActionEventCreatepet [{action.Identity}] invalid monstertype or map: {param}");
                return false;
            }

            var msg = new MsgAiSpawnNpc
            {
                Mode = AiSpawnNpcMode.Spawn
            };
            msg.List.Add(new MsgAiSpawnNpc<AiClient>.SpawnNpc
            {
                GeneratorId = idGen,
                MapId = idMap,
                MonsterType = idType,
                OwnerId = idOwner,
                X = usPosX,
                Y = usPosY,
                OwnerType = dwOwnerType,
                Data = dwData
            });
            await Kernel.BroadcastWorldMsgAsync(msg);
            return true;
        }

        private static async Task<bool> ExecuteActionEventCreatenewNpcAsync(
            DbAction action, string param, Character user,
            Role role, Item item, string input)
        {
            string[] pszParam = SplitParam(param);
            if (pszParam.Length < 9)
                return false;

            string szName = pszParam[0];
            ushort nType = ushort.Parse(pszParam[1]);
            ushort nSort = ushort.Parse(pszParam[2]);
            ushort nLookface = ushort.Parse(pszParam[3]);
            uint nOwnerType = uint.Parse(pszParam[4]);
            uint nOwner = uint.Parse(pszParam[5]);
            uint idMap = uint.Parse(pszParam[6]);
            ushort nPosX = ushort.Parse(pszParam[7]);
            ushort nPosY = ushort.Parse(pszParam[8]);
            uint nLife = 0;
            uint idBase = 0;
            uint idLink = 0;
            uint setTask0 = 0;
            uint setTask1 = 0;
            uint setTask2 = 0;
            uint setTask3 = 0;
            uint setTask4 = 0;
            uint setTask5 = 0;
            uint setTask6 = 0;
            uint setTask7 = 0;
            var setData0 = 0;
            var setData1 = 0;
            var setData2 = 0;
            var setData3 = 0;
            var szData = "";
            if (pszParam.Length > 9)
            {
                nLife = uint.Parse(pszParam[9]);
                if (pszParam.Length > 10)
                    idBase = uint.Parse(pszParam[10]);
                if (pszParam.Length > 11)
                    idLink = uint.Parse(pszParam[11]);
                if (pszParam.Length > 12)
                    setTask0 = uint.Parse(pszParam[12]);
                if (pszParam.Length > 13)
                    setTask1 = uint.Parse(pszParam[13]);
                if (pszParam.Length > 14)
                    setTask2 = uint.Parse(pszParam[14]);
                if (pszParam.Length > 15)
                    setTask3 = uint.Parse(pszParam[15]);
                if (pszParam.Length > 16)
                    setTask4 = uint.Parse(pszParam[16]);
                if (pszParam.Length > 17)
                    setTask5 = uint.Parse(pszParam[17]);
                if (pszParam.Length > 18)
                    setTask6 = uint.Parse(pszParam[18]);
                if (pszParam.Length > 19)
                    setTask7 = uint.Parse(pszParam[19]);
                if (pszParam.Length > 20)
                    setData0 = int.Parse(pszParam[20]);
                if (pszParam.Length > 21)
                    setData1 = int.Parse(pszParam[21]);
                if (pszParam.Length > 22)
                    setData2 = int.Parse(pszParam[22]);
                if (pszParam.Length > 23)
                    setData3 = int.Parse(pszParam[23]);
                if (pszParam.Length > 24)
                    szData = pszParam[24];
            }

            GameMap map = MapManager.GetMap(idMap);
            if (map == null)
            {
                await Log.WriteLogAsync(LogLevel.Warning,
                                        $"ExecuteActionEventCreatenewNpc invalid {idMap} map identity for action {action.Identity}");
                return false;
            }

            var npc = new DbDynanpc
            {
                Name = szName,
                Base = idBase,
                Cellx = nPosX,
                Celly = nPosY,
                Data0 = setData0,
                Data1 = setData1,
                Data2 = setData2,
                Data3 = setData3,
                Datastr = szData,
                Defence = 0,
                Life = nLife,
                Maxlife = nLife,
                Linkid = idLink,
                Task0 = setTask0,
                Task1 = setTask1,
                Task2 = setTask2,
                Task3 = setTask3,
                Task4 = setTask4,
                Task5 = setTask5,
                Task6 = setTask6,
                Task7 = setTask7,
                Ownerid = nOwner,
                OwnerType = nOwnerType,
                Lookface = nLookface,
                Type = nType,
                Mapid = idMap,
                Sort = nSort
            };

            if (!await ServerDbContext.SaveAsync(npc))
            {
                await Log.WriteLogAsync(LogLevel.Warning, "ExecuteActionEventCreatenewNpc could not save dynamic npc");
                return false;
            }

            var dynaNpc = new DynamicNpc(npc);
            if (!await dynaNpc.InitializeAsync())
                return false;

            await dynaNpc.EnterMapAsync();
            return true;
        }

        private static async Task<bool> ExecuteActionEventCountmonsterAsync(
            DbAction action, string param, Character user,
            Role role, Item item, string input)
        {
            string[] pszParam = SplitParam(param);

            if (pszParam.Length < 5)
                return false;

            uint idMap = uint.Parse(pszParam[0]);
            string szField = pszParam[1];
            string szData = pszParam[2];
            string szOpt = pszParam[3];
            int nNum = int.Parse(pszParam[4]);
            var nCount = 0;

            switch (szField.ToLowerInvariant())
            {
                case "name":
                    nCount += RoleManager
                              .QueryRoles(x => x is Monster mob && mob.MapIdentity == idMap && mob.Name.Equals(szData))
                              .Count;
                    break;
                case "gen_id":
                    nCount += RoleManager
                              .QueryRoles(x => x is Monster mob && mob.GeneratorId == uint.Parse(szData) && mob.IsAlive)
                              .Count;
                    break;
            }

            switch (szOpt)
            {
                case "==":
                    return nCount == nNum;
                case "<":
                    return nCount < nNum;
                case ">":
                    return nCount > nNum;
            }

            return false;
        }

        private static async Task<bool> ExecuteActionEventDeletemonsterAsync(
            DbAction action, string param, Character user,
            Role role, Item item, string input)
        {
            string[] pszParam = SplitParam(param);

            if (pszParam.Length < 2)
                return false;

            uint idMap = uint.Parse(pszParam[0]);
            uint idType = uint.Parse(pszParam[1]);
            var nData = 0;
            var szName = "";

            if (pszParam.Length >= 3)
                nData = int.Parse(pszParam[2]);
            if (pszParam.Length >= 4)
                szName = pszParam[3];

            var ret = false;


            if (!string.IsNullOrEmpty(szName))
                foreach (Role monster in RoleManager.QueryRoles(
                             x => x is Monster && x.MapIdentity == idMap && x.Name.Equals(szName)))
                {
                    await monster.LeaveMapAsync();
                    ret = true;
                }

            if (idType != 0)
                foreach (Role monster in RoleManager.QueryRoles(
                             x => x is Monster mob && x.MapIdentity == idMap && mob.Type == idType))
                {
                    await monster.LeaveMapAsync();
                    ret = true;
                }

            return ret;
        }

        private static async Task<bool> ExecuteActionEventBbsAsync(DbAction action, string param, Character user,
                                                                   Role role,
                                                                   Item item, string input)
        {
            await RoleManager.BroadcastMsgAsync(param);
            return true;
        }

        private static async Task<bool> ExecuteActionEventEraseAsync(DbAction action, string param, Character user,
                                                                     Role role, Item item, string input)
        {
            string[] pszParam = SplitParam(param);
            if (pszParam.Length < 2)
                return false;

            uint npcType = uint.Parse(pszParam[1]);
            foreach (DynamicNpc dynaNpc in RoleManager.QueryRoleByMap<DynamicNpc>(uint.Parse(pszParam[0])))
                if (dynaNpc.Type == npcType)
                    await dynaNpc.DelNpcAsync();

            return true;
        }

        private static async Task<bool> ExecuteActionEventTeleportAsync(DbAction action, string param, Character user,
                                                                        Role role, Item item, string input)
        {
            string[] pszParam = SplitParam(param);

            if (pszParam.Length < 4)
                return false;

            if (!uint.TryParse(pszParam[0], out uint idSource) || !uint.TryParse(pszParam[1], out uint idTarget) ||
                !ushort.TryParse(pszParam[2], out ushort usMapX) || !ushort.TryParse(pszParam[3], out ushort usMapY))
                return false;

            GameMap sourceMap = MapManager.GetMap(idSource);
            GameMap targetMap = MapManager.GetMap(idTarget);

            if (sourceMap == null || targetMap == null)
                return false;

            if (sourceMap.IsTeleportDisable())
                return false;

            if (!sourceMap.IsStandEnable(usMapX, usMapY))
                return false;

            foreach (Character player in RoleManager.QueryRoleByType<Character>()
                                                    .Where(x => x.MapIdentity == sourceMap.Identity))
                await player.FlyMapAsync(idTarget, usMapX, usMapY);

            return true;
        }

        private static async Task<bool> ExecuteActionEventMassactionAsync(DbAction action, string param, Character user,
                                                                          Role role, Item item, string input)
        {
            string[] pszParam = SplitParam(param);
            if (pszParam.Length < 3)
                return false;

            if (!uint.TryParse(pszParam[0], out uint idMap) || !uint.TryParse(pszParam[1], out uint idAction)
                                                            || !int.TryParse(pszParam[2], out int nAmount))
                return false;

            GameMap map = MapManager.GetMap(idMap);
            if (map == null)
                return false;

            if (nAmount <= 0)
                nAmount = int.MaxValue;

            foreach (Character player in RoleManager.QueryRoleByMap<Character>(idMap))
            {
                if (nAmount-- <= 0)
                    break;

                await ExecuteActionAsync(idAction, player, role, null, input);
            }

            return true;
        }

        private static async Task<bool> ExecuteActionEventRegisterAsync(DbAction action, string param, Character user,
                                                                        Role role,
                                                                        Item item, string input)
        {
            if (user == null)
                return false;

            GameEvent baseEvent;
            switch (param.ToLower())
            {
                case "timedguildwar":
                {
                    baseEvent = EventManager.GetEvent(GameEvent.EventType.TimedGuildWar);
                    break;
                }
                case "lineskillpk":
                {
                    baseEvent = EventManager.GetEvent(GameEvent.EventType.LineSkillPk);
                    break;
                }
                case "syndicatecontest":
                {
                    baseEvent = EventManager.GetEvent(GameEvent.EventType.GuildContest);
                    break;
                }

                default:
                    return false;
            }

            if (baseEvent == null)
                return false;

            return await user.SignInEventAsync(baseEvent);
        }

        private static async Task<bool> ExecuteActionEventExitAsync(DbAction action, string param, Character user,
                                                                    Role role,
                                                                    Item item, string input)
        {
            if (user == null)
                return false;

            await user.SignOutEventAsync();
            return true;
        }

        private static async Task<bool> ExecuteActionEventCmdAsync(DbAction action, string param, Character user,
                                                                   Role role,
                                                                   Item item, string input)
        {
            var eventType = (GameEvent.EventType) action.Data;
            if (eventType is GameEvent.EventType.None or >= GameEvent.EventType.Limit)
                return false;

            GameEvent @event = EventManager.GetEvent(eventType);
            if (@event == null)
                return false;

            return await @event.OnActionCommandAsync(param, user, role, item, input);
        }

        #endregion

        #region Family

        private static async Task<bool> ExecuteActionFamilyAttrAsync(DbAction action, string param, Character user,
                                                                     Role role, Item item, string input)
        {
            string[] splitParam = SplitParam(param, 3);
            if (splitParam.Length < 3)
                return false;

            string field = splitParam[0],
                   opt = splitParam[1];
            long value = long.Parse(splitParam[2]);

            long data = -1;
            if (user?.Family != null)
            {
                if (field.Equals("money"))
                {
                    if (opt.Equals("+="))
                    {
                        if (value < 0)
                        {
                            var temp = (ulong) (value * -1);
                            if (user.Family.Money < temp)
                                return false;
                            user.Family.Money -= temp;
                        }
                        else
                        {
                            var temp = (ulong) value;
                            user.Family.Money += temp;
                        }

                        return await user.Family.SaveAsync();
                    }

                    data = (long) user.Family.Money;
                }
                else if (field.Equals("rank"))
                {
                    if (opt.Equals("="))
                    {
                        user.Family.Rank = (byte) Math.Min(4, value);
                        return await user.Family.SaveAsync();
                    }

                    data = user.Family.Rank;
                }
                else if (field.Equals("star_tower"))
                {
                    if (opt.Equals("="))
                    {
                        user.Family.BattlePowerTower = (byte) Math.Min(4, value);
                        return await user.Family.SaveAsync();
                    }

                    data = user.Family.BattlePowerTower;
                }
            }

            switch (opt)
            {
                case "==": return data == value;
                case ">=": return data >= value;
                case "<=": return data <= value;
                case ">":  return data > value;
                case "<":  return data < value;
            }

            return false;
        }

        private static async Task<bool> ExecuteActionFamilyMemberAttrAsync(
            DbAction action, string param, Character user,
            Role role, Item item, string input)
        {
            string[] splitParam = SplitParam(param, 3);
            if (splitParam.Length < 3)
                return false;

            string field = splitParam[0],
                   opt = splitParam[1];
            long value = long.Parse(splitParam[2]);
            long data = 0;

            if (user?.Family != null)
                if (field.Equals("rank"))
                    data = (long) user.FamilyPosition;

            switch (opt)
            {
                case "==": return data == value;
                case ">=": return data >= value;
                case "<=": return data <= value;
                case ">":  return data > value;
                case "<":  return data < value;
            }

            return true;
        }

        private static async Task<bool> ExecuteActionFamilyWarActivityCheckAsync(
            DbAction action, string param, Character user,
            Role role, Item item, string input)
        {
            if (user?.Family == null)
                return false;

            var npc = RoleManager.FindRole<DynamicNpc>(x => x.Identity == action.Data);
            if (npc == null)
                return false;

            if (npc.Data1 != user.Family.ChallengeMap && npc.Data1 != user.Family.FamilyMap)
                return false;

            var war = EventManager.GetEvent<FamilyWar>();
            if (war == null)
                return false;

            return await war.ValidateResultAsync(user, npc.Identity);
        }

        private static async Task<bool> ExecuteActionFamilyWarAuthorityCheckAsync(
            DbAction action, string param, Character user,
            Role role, Item item, string input)
        {
            if (user?.Family == null)
                return false;

            var npc = RoleManager.FindRole<DynamicNpc>(x => x.Identity == action.Data);
            if (npc == null)
                return false;

            if (npc.Data1 != user.Family.ChallengeMap)
                return false;
            return true;
        }

        private static async Task<bool> ActionFamilyWarRegisterCheckAsync(DbAction action, string param, Character user,
                                                                          Role role, Item item, string input)
        {
            if (user?.Family == null)
                return false;

            var npc = RoleManager.FindRole<DynamicNpc>(x => x.Identity == action.Data);
            if (npc == null)
                return false;

            if (npc.Data1 != user.Family.FamilyMap)
                return false;
            return true;
        }

        #endregion

        #region Trap

        private static async Task<bool> ExecuteActionTrapCreateAsync(DbAction action, string param, Character user,
                                                                     Role role, Item item, string input)
        {
            string[] splitParams = SplitParam(param, 7);

            if (splitParams.Length < 7)
            {
                await Log.WriteLogAsync(LogLevel.Error,
                                        $"Invalid param length ExecuteActionTrapCreate {action.Identity}");
                return false;
            }

            uint type = uint.Parse(splitParams[0]),
                 look = uint.Parse(splitParams[1]),
                 owner = uint.Parse(splitParams[2]),
                 idMap = uint.Parse(splitParams[3]);
            ushort posX = ushort.Parse(splitParams[4]),
                   posY = ushort.Parse(splitParams[5]),
                   data = ushort.Parse(splitParams[6]);

            if (MapManager.GetMap(idMap) == null)
            {
                await Log.WriteLogAsync(LogLevel.Error,
                                        $"Invalid map for ExecuteActionTrapCreate {idMap}:{action.Identity}");
                return false;
            }

            var trap = new MapTrap(new DbTrap
            {
                TypeId = type,
                Look = look,
                OwnerId = owner,
                Data = data,
                MapId = idMap,
                PosX = posX,
                PosY = posY,
                Id = (uint) IdentityGenerator.Traps.GetNextIdentity
            });

            if (!await trap.InitializeAsync())
            {
                await Log.WriteLogAsync(LogLevel.Error,
                                        $"could not start trap for ExecuteActionTrapCreate {action.Identity}");
                return false;
            }

            return true;
        }

        private static async Task<bool> ExecuteActionTrapEraseAsync(DbAction action, string param, Character user,
                                                                    Role role, Item item, string input)
        {
            var trap = role as MapTrap;
            if (trap == null)
                return false;

            await trap.LeaveMapAsync();
            return true;
        }

        private static async Task<bool> ExecuteActionTrapCountAsync(DbAction action, string param, Character user,
                                                                    Role role, Item item, string input)
        {
            throw new NotImplementedException("No action for this yet.");
            // return true;
        }

        #endregion

        #region Detain

        private static async Task<bool> ExecuteActionDetainDialogAsync(DbAction action, string param, Character user,
                                                                       Role role, Item item, string input)
        {
            if (param.Equals("target"))
            {
                await user.SendAsync(new MsgAction
                {
                    Action = MsgAction<Client>.ActionType.ClientDialog,
                    X = user.MapX,
                    Y = user.MapY,
                    Identity = user.Identity,
                    Data = 336
                });
                return true;
            }

            if (param.Equals("hunter"))
            {
                await user.SendAsync(new MsgAction
                {
                    Action = MsgAction<Client>.ActionType.ClientDialog,
                    X = user.MapX,
                    Y = user.MapY,
                    Identity = user.Identity,
                    Data = 337
                });
                return true;
            }

            return false;
        }

        #endregion

        private static async Task<string> FormatParamAsync(DbAction action, Character user, Role role, Item item,
                                                           string input)
        {
            string result = action.Param;

            result = result.Replace("%user_name", user?.Name ?? Language.StrNone)
                           .Replace("%user_id", user?.Identity.ToString() ?? "0")
                           .Replace("%user_lev", user?.Level.ToString() ?? "0")
                           .Replace("%user_mate", user?.MateName ?? Language.StrNone)
                           .Replace("%user_pro", user?.Profession.ToString() ?? "0")
                           .Replace("%user_map_id", user?.Map.Identity.ToString() ?? "0")
                           .Replace("%user_map_name", user?.Map.Name ?? Language.StrNone)
                           .Replace("%user_map_x", user?.MapX.ToString() ?? "0")
                           .Replace("%user_map_y", user?.MapY.ToString() ?? "0")
                           .Replace("%map_owner_id", user?.Map.OwnerIdentity.ToString() ?? "0")
                           .Replace("%user_nobility_rank", ((int) (user?.NobilityRank ?? 0)).ToString())
                           .Replace("%user_nobility_position", user?.NobilityPosition.ToString() ?? "0")
                           .Replace("%user_home_id", user?.HomeIdentity.ToString() ?? "0")
                           .Replace("%syn_id", user?.SyndicateIdentity.ToString() ?? "0")
                           .Replace("%syn_name", user?.SyndicateName ?? Language.StrNone)
                           .Replace("%account_id", user?.Client.AccountIdentity.ToString() ?? "0")
                           .Replace("%user_virtue", user?.VirtuePoints.ToString() ?? "0")
                           .Replace("%map_owner_id", user?.Map.OwnerIdentity.ToString() ?? "0")
                           .Replace("%last_add_item_id", user?.LastAddItemIdentity.ToString() ?? "0")
                           .Replace("%online_time",
                                    $"{user?.OnlineTime.TotalDays:0} days, {user?.OnlineTime.Hours:00} hours, {user?.OnlineTime.Minutes} minutes and {user?.OnlineTime.Seconds} seconds")
                           .Replace("%session_time",
                                    $"{user?.SessionOnlineTime.TotalDays:0} days, {user?.SessionOnlineTime.Hours:00} hours, {user?.SessionOnlineTime.Minutes} minutes and {user?.SessionOnlineTime.Seconds} seconds")
                           .Replace("%businessman_days", $"{user?.BusinessManDays}")
                           .Replace("%user_vip", user?.BaseVipLevel.ToString())
                           .Replace("%user_svip", user?.VipLevel.ToString());

            if (result.Contains("%levelup_exp"))
            {
                DbLevelExperience db = ExperienceManager.GetLevelExperience(user?.Level ?? 0);
                result = result.Replace("%levelup_exp", db != null ? db.Exp.ToString() : "0");
            }

            if (user != null)
            {
                if (result.Contains("%vip_expire"))
                {
                    string s = Language.StrExpired;
                    if (user.VipExpiration > DateTime.Now)
                        s = user.VipExpiration.ToString("f");
                    result = result.Replace("%vip_expire", s);
                }

                while (result.Contains("%stc("))
                {
                    int start = result.IndexOf("%stc(", StringComparison.InvariantCultureIgnoreCase);
                    string strEvent = "", strStage = "";
                    var comma = false;
                    for (int i = start + 5; i < result.Length; i++)
                        if (!comma)
                        {
                            if (result[i] == ',')
                            {
                                comma = true;
                                continue;
                            }

                            strEvent += result[i];
                        }
                        else
                        {
                            if (result[i] == ')')
                                break;

                            strStage += result[i];
                        }

                    uint.TryParse(strEvent, out uint stcEvent);
                    uint.TryParse(strStage, out uint stcStage);

                    DbStatistic stc = user.Statistic.GetStc(stcEvent, stcStage);
                    result = result.Replace($"%stc({stcEvent},{stcStage})", stc?.Data.ToString() ?? "0");
                }

                for (var i = 0; i < Role.MAX_VAR_AMOUNT; i++)
                {
                    result = result.Replace($"%iter_var_data{i}", user.VarData[i].ToString());
                    result = result.Replace($"%iter_var_str{i}", user.VarString[i]);
                }
            }

            if (role != null)
            {
                if (role is BaseNpc npc)
                {
                    result = result.Replace("%data0", npc.GetData("data0").ToString())
                                   .Replace("%data1", npc.GetData("data1").ToString())
                                   .Replace("%data2", npc.GetData("data2").ToString())
                                   .Replace("%data3", npc.GetData("data3").ToString())
                                   .Replace("%npc_ownerid", npc.OwnerIdentity.ToString())
                                   .Replace("%map_owner_id", role.Map.OwnerIdentity.ToString() ?? "0")
                                   .Replace("%npc_x", npc.MapX.ToString())
                                   .Replace("%npc_y", npc.MapY.ToString());
                }
                else if (role is Monster monster)
                {
                }

                result = result.Replace("%map_owner_id", role.Map.OwnerIdentity.ToString());
            }

            if (item != null)
                result = result.Replace("%item_data", item.Identity.ToString())
                               .Replace("%item_name", item.Name)
                               .Replace("%item_type", item.Type.ToString())
                               .Replace("%item_id", item.Identity.ToString());

            if (result.Contains("%iter_upquality_gem"))
            {
                Item pItem = user?.UserPackage[(Item.ItemPosition) user.Iterator];
                if (pItem != null)
                    result = result.Replace("%iter_upquality_gem", pItem.GetUpQualityGemAmount().ToString());
                else
                    result = result.Replace("%iter_upquality_gem", "0");
            }

            if (result.Contains("%iter_itembound"))
            {
                Item pItem = user?.UserPackage[(Item.ItemPosition) user.Iterator];
                if (pItem != null)
                    result = result.Replace("%iter_itembound", pItem.IsBound ? "1" : "0");
                else
                    result = result.Replace("%iter_itembound", "0");
            }

            if (result.Contains("%iter_uplevel_gem"))
            {
                Item pItem = user?.UserPackage[(Item.ItemPosition) user.Iterator];
                if (pItem != null)
                    result = result.Replace("%iter_uplevel_gem", pItem.GetUpgradeGemAmount().ToString());
                else
                    result = result.Replace("%iter_uplevel_gem", "0");
            }

            result = result.Replace("%map_name", user?.Map?.Name ?? role?.Map?.Name ?? Language.StrNone)
                           .Replace("%iter_time", UnixTimestamp.Now().ToString());

            while (result.Contains("%random"))
                if (result.Contains("%random"))
                {
                    int start = result.IndexOf("%random(", StringComparison.InvariantCultureIgnoreCase);
                    var random = "";
                    for (int i = start + 8; i < result.Length; i++)
                    {
                        if (result[i] == ')')
                            break;

                        random += result[i];
                    }

                    var value = 0;
                    if (int.TryParse(random, out int chance)) value = await Kernel.NextAsync(chance);
                    result = result.Replace($"%random({chance})", value.ToString());
                }

            return result;
        }

        private static string[] SplitParam(string param, int count = 0)
        {
            return count > 0
                       ? param.Split(new[] {' '}, count, StringSplitOptions.RemoveEmptyEntries)
                       : param.Split(' ');
        }

        private static string GetParenthesys(string szParam)
        {
            int varIdx = szParam.IndexOf("(", StringComparison.CurrentCulture) + 1;
            int endIdx = szParam.IndexOf(")", StringComparison.CurrentCulture);
            return szParam.Substring(varIdx, endIdx - varIdx);
        }

        private static byte VarId(string szParam)
        {
            int varIdx = szParam.IndexOf("(", StringComparison.CurrentCulture) + 1;
            return byte.Parse(szParam.Substring(varIdx, 1));
        }
    }

    public class QueuedAction
    {
        private readonly TimeOut m_timeOut = new();

        public QueuedAction(int secs, uint action, uint idUser)
        {
            m_timeOut.Startup(secs);
            Action = action;
            UserIdentity = idUser;
        }

        public uint UserIdentity { get; }
        public uint Action { get; }
        public bool CanBeExecuted => m_timeOut.IsActive() && m_timeOut.IsTimeOut();
        public bool IsValid => RoleManager.GetUser(UserIdentity) != null;

        public void Clear()
        {
            m_timeOut.Clear();
        }
    }

    public enum TaskActionType
    {
        // System
        ActionSysFirst = 100,
        ActionMenutext = 101,
        ActionMenulink = 102,
        ActionMenuedit = 103,
        ActionMenupic = 104,
        ActionMenuMessage = 105,
        ActionMenubutton = 110,
        ActionMenulistpart = 111,
        ActionMenucreate = 120,
        ActionRand = 121,
        ActionRandaction = 122,
        ActionChktime = 123,
        ActionPostcmd = 124,
        ActionBrocastmsg = 125,
        ActionMessagebox = 126,
        ActionExecutequery = 127,
        ActionInviteTrans = 130,
        ActionSysPathFinding = 131,
        ActionVipFunctionCheck = 144, // data is flag data << 1UL
        ActionSysLimit = 199,

        //NPC
        ActionNpcFirst = 200,
        ActionNpcAttr = 201,
        ActionNpcErase = 205,
        ActionNpcModify = 206,
        ActionNpcResetsynowner = 207,
        ActionNpcFindNextTable = 208,
        ActionNpcFamilyCreate = 218,
        ActionNpcFamilyDestroy = 219,
        ActionNpcLimit = 299,

        // Map
        ActionMapFirst = 300,
        ActionMapMovenpc = 301,
        ActionMapMapuser = 302,
        ActionMapBrocastmsg = 303,
        ActionMapDropitem = 304,
        ActionMapSetstatus = 305,
        ActionMapAttrib = 306,
        ActionMapRegionMonster = 307,
        ActionMapRandDropItem = 308,
        ActionMapChangeweather = 310,
        ActionMapChangelight = 311,
        ActionMapMapeffect = 312,
        ActionMapFireworks = 314,
        ActionMapLimit = 399,

        // Lay item
        ActionItemonlyFirst = 400,
        ActionItemRequestlaynpc = 401,
        ActionItemCountnpc = 402,
        ActionItemLaynpc = 403,
        ActionItemDelthis = 498,
        ActionItemonlyLimit = 499,

        // Item
        ActionItemFirst = 500,
        ActionItemAdd = 501,
        ActionItemDel = 502,
        ActionItemCheck = 503,
        ActionItemHole = 504,
        ActionItemRepair = 505,
        ActionItemMultidel = 506,
        ActionItemMultichk = 507,
        ActionItemLeavespace = 508,
        ActionItemUpequipment = 509,
        ActionItemEquiptest = 510,
        ActionItemEquipexist = 511,
        ActionItemEquipcolor = 512,
        ActionItemRemoveAny = 513,
        ActionItemCheckrand = 516,
        ActionItemModify = 517,
        ActionItemJarCreate = 528,
        ActionItemJarVerify = 529,
        ActionItemLimit = 599,

        // Dyn NPCs
        ActionNpconlyFirst = 600,
        ActionNpconlyCreatenewPet = 601,
        ActionNpconlyDeletePet = 602,
        ActionNpconlyMagiceffect = 603,
        ActionNpconlyMagiceffect2 = 604,
        ActionNpconlyLimit = 699,

        // Syndicate
        ActionSynFirst = 700,
        ActionSynCreate = 701,
        ActionSynDestroy = 702,
        ActionSynSetAssistant = 705,
        ActionSynClearRank = 706,
        ActionSynChangeLeader = 709,
        ActionSynAntagonize = 711,
        ActionSynClearAntagonize = 712,
        ActionSynAlly = 713,
        ActionSynClearAlly = 714,
        ActionSynAttr = 717,
        ActionSynLimit = 799,

        //Monsters
        ActionMstFirst = 800,
        ActionMstDropitem = 801,
        ActionMstMagic = 802,
        ActionMstRefinery = 803,
        ActionMstLimit = 899,

        //User
        ActionUserFirst = 1000,
        ActionUserAttr = 1001,
        ActionUserFull = 1002,        // Fill the user attributes. param is the attribute name. life/mana/xp/sp
        ActionUserChgmap = 1003,      // Mapid Mapx Mapy savelocation
        ActionUserRecordpoint = 1004, // Records the user location, so he can be teleported back there later.
        ActionUserHair = 1005,
        ActionUserChgmaprecord = 1006,
        ActionUserChglinkmap = 1007,
        ActionUserTransform = 1008,
        ActionUserIspure = 1009,
        ActionUserTalk = 1010,
        ActionUserMagic = 1020,
        ActionUserWeaponskill = 1021,
        ActionUserLog = 1022,
        ActionUserBonus = 1023,
        ActionUserDivorce = 1024,
        ActionUserMarriage = 1025,
        ActionUserSex = 1026,
        ActionUserEffect = 1027,
        ActionUserTaskmask = 1028,
        ActionUserMediaplay = 1029,
        ActionUserSupermanlist = 1030,
        ActionUserAddTitle = 1031,
        ActionUserRemoveTitle = 1032,
        ActionUserCreatemap = 1033,
        ActionUserEnterHome = 1034,
        ActionUserEnterMateHome = 1035,
        ActionUserChkinCard2 = 1036,
        ActionUserChkoutCard2 = 1037,
        ActionUserFlyNeighbor = 1038,
        ActionUserUnlearnMagic = 1039,
        ActionUserRebirth = 1040,
        ActionUserWebpage = 1041,
        ActionUserBbs = 1042,
        ActionUserUnlearnSkill = 1043,
        ActionUserDropMagic = 1044,
        ActionUserFixAttr = 1045,
        ActionUserOpenDialog = 1046,
        ActionUserPointAllot = 1047,
        ActionUserExpMultiply = 1048,
        ActionUserDelWpgBadge = 1049,
        ActionUserChkWpgBadge = 1050,
        ActionUserTakestudentexp = 1051,
        ActionUserWhPassword = 1052,
        ActionUserSetWhPassword = 1053,
        ActionUserOpeninterface = 1054,
        ActionUserTaskManager = 1056,
        ActionUserTaskOpe = 1057,
        ActionUserTaskLocaltime = 1058,
        ActionUserTaskFind = 1059,
        ActionUserVarCompare = 1060,
        ActionUserVarDefine = 1061,
        ActionUserVarCalc = 1064,
        ActionUserTestEquipment = 1065,
        ActionUserExecAction = 1071,
        ActionUserTestPos = 1072,
        ActionUserStcCompare = 1073,
        ActionUserStcOpe = 1074,
        ActionUserDataSync = 1075,
        ActionUserSelectToData = 1077,
        ActionUserStcTimeOperation = 1080,
        ActionUserStcTimeCheck = 1081,
        ActionUserAttachStatus = 1082,
        ActionUserGodTime = 1083,
        ActionUserLogEvent = 1085,
        ActionUserExpballExp = 1086,
        ActionSomethingRelatedToRebirth = 1095,
        ActionUserStatusCreate = 1096,
        ActionUserStatusCheck = 1098,

        //User -> Team
        ActionTeamBroadcast = 1101,
        ActionTeamAttr = 1102,
        ActionTeamLeavespace = 1103,
        ActionTeamItemAdd = 1104,
        ActionTeamItemDel = 1105,
        ActionTeamItemCheck = 1106,
        ActionTeamChgmap = 1107,
        ActionTeamChkIsleader = 1501,

        // User -> Events???
        ActionElitePKInscribe = 1301,
        ActionElitePKExit = 1302,
        ActionElitePKCheck = 1303,

        ActionTeamPKInscribe = 1311,
        ActionTeamPKExit = 1312,
        ActionTeamPKCheck = 1313,
        ActionTeamPKUnknown1314 = 1314,
        ActionTeamPKUnknown1315 = 1315,

        ActionSkillTeamPKInscribe = 1321,
        ActionSkillTeamPKExit = 1322,
        ActionSkillTeamPKCheck = 1323,
        ActionSkillTeamPKUnknown1324 = 1324,
        ActionSkillTeamPKUnknown1325 = 1325,

        // User -> General
        ActionGeneralLottery = 1508,
        ActionChgMapSquare = 1509,
        ActionAchievements = 1554,

        ActionUserLimit = 1999,

        //Events
        ActionEventFirst = 2000,
        ActionEventSetstatus = 2001,
        ActionEventDelnpcGenid = 2002,
        ActionEventCompare = 2003,
        ActionEventCompareUnsigned = 2004,
        ActionEventChangeweather = 2005,
        ActionEventCreatepet = 2006,
        ActionEventCreatenewNpc = 2007,
        ActionEventCountmonster = 2008,
        ActionEventDeletemonster = 2009,
        ActionEventBbs = 2010,
        ActionEventErase = 2011,
        ActionEventTeleport = 2012,
        ActionEventMassaction = 2013,

        ActionEventRegister = 2050,
        ActionEventExit = 2051,
        ActionEventCmd = 2052,
        ActionEventLimit = 2099,

        //Traps
        ActionTrapFirst = 2100,
        ActionTrapCreate = 2101,
        ActionTrapErase = 2102,
        ActionTrapCount = 2103,
        ActionTrapAttr = 2104,
        ActionTrapLimit = 2199,

        // Detained Item
        ActionDetainFirst = 2200,
        ActionDetainDialog = 2205,
        ActionDetainLimit = 2299,

        //Wanted
        ActionWantedFirst = 3000,
        ActionWantedNext = 3001,
        ActionWantedName = 3002,
        ActionWantedBonuty = 3003,
        ActionWantedNew = 3004,
        ActionWantedOrder = 3005,
        ActionWantedCancel = 3006,
        ActionWantedModifyid = 3007,
        ActionWantedSuperadd = 3008,
        ActionPolicewantedNext = 3010,
        ActionPolicewantedOrder = 3011,
        ActionPolicewantedCheck = 3012,
        ActionWantedLimit = 3099,

        // Family
        ActionFamilyFirst = 3500,
        ActionFamilyAttr = 3501,
        ActionFamilyMemberAttr = 3510,
        ActionFamilyWarActivityCheck = 3521,
        ActionFamilyWarAuthorityCheck = 3523,
        ActionFamilyWarRegisterCheck = 3524,
        ActionFamilyLast = 3599,

        //Magic
        ActionMagicFirst = 4000,
        ActionMagicAttachstatus = 4001,
        ActionMagicAttack = 4002,
        ActionMagicLimit = 4099
    }

    public enum OpenWindow
    {
        Compose = 1,
        Craft = 2,
        Warehouse = 4,
        ClanWindow = 64,
        DetainRedeem = 336,
        DetainClaim = 337,
        VipWarehouse = 341,
        Breeding = 368,
        PurificationWindow = 455,
        StabilizationWindow = 459,
        TalismanUpgrade = 347,
        GemComposing = 422,
        OpenSockets = 425,
        Blessing = 426,
        TortoiseGemComposing = 438,
        HorseRacingStore = 464,
        EditCharacterName = 489,
        GarmentTrade = 502,
        DegradeEquipment = 506
    }
}