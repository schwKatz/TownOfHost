using System;
using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using UnityEngine;
using Hazel;
using TownOfHostY.Roles.Core;
using TownOfHostY.Roles.Crewmate;
using TownOfHostY.Roles.Neutral;

namespace TownOfHostY
{
    [HarmonyPatch(typeof(ShipStatus), nameof(ShipStatus.FixedUpdate))]
    class ShipFixedUpdatePatch
    {
        public static void Postfix(ShipStatus __instance)
        {
            //ここより上、全員が実行する
            if (!AmongUsClient.Instance.AmHost) return;
            //ここより下、ホストのみが実行する
            if ((Options.CurrentGameMode == CustomGameMode.HideAndSeek || Options.IsStandardHAS) && Main.introDestroyed)
            {
                if (Options.HideAndSeekKillDelayTimer > 0)
                {
                    Options.HideAndSeekKillDelayTimer -= Time.fixedDeltaTime;
                }
                else if (!float.IsNaN(Options.HideAndSeekKillDelayTimer))
                {
                    Utils.MarkEveryoneDirtySettings();
                    Options.HideAndSeekKillDelayTimer = float.NaN;
                    Logger.Info("キル能力解禁", "HideAndSeek");
                }
            }
        }
    }

    //[HarmonyPatch(typeof(ShipStatus), nameof(ShipStatus.UpdateSystem), typeof(SystemTypes), typeof(PlayerControl), typeof(MessageReader))]
    //public static class MessageReaderUpdateSystemPatch
    //{
    //    public static void Postfix(ShipStatus __instance, [HarmonyArgument(0)] SystemTypes systemType, [HarmonyArgument(1)] PlayerControl player, [HarmonyArgument(2)] MessageReader reader)
    //    {
    //        ShipStatusUpdateSystemPatch.Postfix();
    //    }
    //}
    [HarmonyPatch(typeof(ShipStatus), nameof(ShipStatus.UpdateSystem), typeof(SystemTypes), typeof(PlayerControl), typeof(byte))]
    class ShipStatusUpdateSystemPatch
    {
        public static void Prefix(ShipStatus __instance,
            [HarmonyArgument(0)] SystemTypes systemType,
            [HarmonyArgument(1)] PlayerControl player,
            [HarmonyArgument(2)] byte amount)
        {
            if (systemType != SystemTypes.Sabotage)
                Logger.Info("SystemType: " + systemType.ToString() + ", PlayerName: " + player.GetNameWithRole() + ", amount: " + amount, "UpdateSystem");

            if (RepairSender.enabled && AmongUsClient.Instance.NetworkMode != NetworkModes.OnlineGame)
            {
                Logger.SendInGame("SystemType: " + systemType.ToString() + ", PlayerName: " + player.GetNameWithRole() + ", amount: " + amount);
            }
        }
        //public static void Postfix()
        //{
        //    Camouflage.CheckCamouflage();
        //}
        public static void CheckAndOpenDoorsRange(ShipStatus __instance, byte amount, byte min, byte max)
        {
            var Ids = new List<byte>();
            for (var i = min; i <= max; i++)
            {
                Ids.Add(i);
            }
            CheckAndOpenDoors(__instance, amount, Ids.ToArray());
        }
        private static void CheckAndOpenDoors(ShipStatus __instance, byte amount, params byte[] DoorIds)
        {
            if (DoorIds.Contains(amount)) foreach (var id in DoorIds)
                {
                    __instance.RpcUpdateSystem(SystemTypes.Doors, id);
                }
        }
        public static bool OnSabotage(PlayerControl player, SystemTypes systemType, byte amount)
        {
            return true;
        }
    }
    [HarmonyPatch(typeof(ShipStatus), nameof(ShipStatus.CloseDoorsOfType))]
    class CloseDoorsPatch
    {
        public static bool Prefix(ShipStatus __instance)
        {
            if (Options.CurrentGameMode == CustomGameMode.HideAndSeek && !Options.AllowCloseDoors.GetBool()) return false;
            if (Options.IsStandardHAS) return false;
            if (Options.IsCCMode) return false;
            
            return true;
        }
    }
    [HarmonyPatch(typeof(ShipStatus), nameof(ShipStatus.Start))]
    class StartPatch
    {
        public static void Postfix()
        {
            Logger.CurrentMethod();
            Logger.Info("-----------ゲーム開始-----------", "Phase");

            Utils.CountAlivePlayers(true);
        }
    }
    [HarmonyPatch(typeof(ShipStatus), nameof(ShipStatus.StartMeeting))]
    class StartMeetingPatch
    {
        public static void Prefix(ShipStatus __instance, PlayerControl reporter, NetworkedPlayerInfo target)
        {
            MeetingStates.ReportTarget = target;
            MeetingStates.DeadBodies = UnityEngine.Object.FindObjectsOfType<DeadBody>();
        }
    }
    [HarmonyPatch(typeof(ShipStatus), nameof(ShipStatus.Begin))]
    class BeginPatch
    {
        public static void Postfix()
        {
            Logger.CurrentMethod();

            //ホストの役職初期設定はここで行うべき？
        }
    }
    [HarmonyPatch(typeof(GameManager), nameof(GameManager.CheckTaskCompletion))]
    class CheckTaskCompletionPatch
    {
        public static bool Prefix(ref bool __result)
        {
            if (Options.DisableTaskWin.GetBool() || Options.NoGameEnd.GetBool() || TaskState.InitialTotalTasks == 0
                || Options.IsCCMode/* || Options.IsONMode*/)
            {
                __result = false;
                return false;
            }
            return true;
        }
    }
}