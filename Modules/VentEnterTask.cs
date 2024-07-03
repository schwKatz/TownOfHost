using System.Collections.Generic;
using HarmonyLib;
using UnityEngine;

using TownOfHostY.Attributes;
using static TownOfHostY.Utils;

namespace TownOfHostY;
static class VentEnterTask
{
    public static List<byte> PlayerIdList = new();

    private static Dictionary<byte, bool> nowTurnFinish = new();
    private static Dictionary<byte, bool> UseVent = new();
    private static Dictionary<byte, bool> taskWinCount = new();
    private static Dictionary<byte, int> taskCountNow = new();
    private static Dictionary<byte, int> taskCountMax = new();
    private static Dictionary<byte, Vent> nowVTask = new();

    public struct Vent
    {
        public int id;
        public string name;
    };

    [GameModuleInitializer]
    public static void Init()
    {
        PlayerIdList = new();
        nowTurnFinish = new();
        UseVent = new();
        taskWinCount = new();
        taskCountNow = new();
        taskCountMax = new();
        nowVTask = new();
    }
    public static void Add(PlayerControl pc, int maxTaskCount, bool winCount = false, bool useVent = true)
    {
        PlayerIdList.Add(pc.PlayerId);
        nowTurnFinish.Add(pc.PlayerId, false);
        UseVent.Add(pc.PlayerId, useVent);
        taskWinCount.Add(pc.PlayerId, winCount);
        taskCountNow.Add(pc.PlayerId, 0);
        taskCountMax.Add(pc.PlayerId, maxTaskCount);
        nowVTask.Add(pc.PlayerId, SetTask(pc));
    }
    public static bool HaveTask(PlayerControl pc) => PlayerIdList.Contains(pc.PlayerId);
    public static bool NowTurnFinish(byte id) => nowTurnFinish[id];
    public static int NowTaskCountNow(byte id) => taskCountNow[id];
    public static Vent NowVentTaskData(byte id) => nowVTask[id];

    public static (int complete, int total) TaskWinCountData()
    {
        int comp = 0;
        int total = 0;
        foreach (var id in PlayerIdList)
        {
            if (taskWinCount[id])
            {
                comp += taskCountNow[id];
                total += taskCountMax[id];
            }
        }
        return (comp, total);
    }
    public static void TaskWinCountAllComplete(byte id)
    {
        if (!PlayerIdList.Contains(id)) return;

        if (taskWinCount[id])
        {
            taskCountNow[id] = taskCountMax[id];
        }
    }
    public static void TaskRemove(byte id)
    {
        if (!PlayerIdList.Contains(id)) return;

        PlayerIdList.Remove(id);
    }

    public static bool OnEnterVent(PlayerPhysics physics, int ventId)
    {
        var player = physics.myPlayer;
        var playerId = physics.myPlayer.PlayerId;
        if (!PlayerIdList.Contains(playerId)) return true;
        if (nowTurnFinish[playerId]) return false;
        if (taskCountNow[playerId] >= taskCountMax[playerId]) return UseVent[playerId];
        if (nowVTask[playerId].id != ventId)
        {
            if (UseVent[playerId]) return true;
            else
            {
                nowTurnFinish[playerId] = true;
                return false;
            }
        }

        taskCountNow[playerId]++;
        Logger.Info($"{player.GetNameWithRole()}：Id={ventId}/タスクCountUp ({taskCountNow[playerId]}/{taskCountMax[playerId]})", "VentEnterTask");
        nowTurnFinish[playerId] = true;

        nowVTask[playerId] = SetTask(player);
        NotifyRoles(SpecifySeer: player);
        return false;
    }
    public static void AfterMeetingTasks()
    {
        PlayerIdList.Do(id => nowTurnFinish[id] = false);
    }

    public static string GetProgressText(byte id, bool comms = false)
    {
        if (!PlayerIdList.Contains(id)) return string.Empty;

        var info = GetPlayerInfoById(id);
        var role = info.GetCustomRole();
        var TaskCompleteColor = taskWinCount[id] ? Color.green : GetRoleColor(role).ShadeColor(0.5f); //タスク完了後の色
        var NonCompleteColor = taskWinCount[id] ? Color.yellow : Color.white; //カウントされない人外は白色

        var NormalColor = taskCountNow[id] >= taskCountMax[id] ? TaskCompleteColor : NonCompleteColor;
        Color TextColor = comms ? Color.gray : NormalColor;

        string Completed = comms ? "?" : $"{taskCountNow[id]}";
        return ColorString(TextColor, $"({Completed}/{taskCountMax[id]})");
    }
    public static string GetLowerText(PlayerControl seer, PlayerControl seen = null, bool isForMeeting = false, bool isForHud = false)
    {
        var id = seer.PlayerId;
        if (!PlayerIdList.Contains(id)) return string.Empty;
        if (isForMeeting) return "";

        //seenが省略の場合seer
        seen ??= seer;

        string text = $"Task：{nowVTask[id].name}";
        if (taskCountNow[id] >= taskCountMax[id]) text = "";
        else if (nowTurnFinish[id]) text = "Task：Next Turn";

        var color = taskWinCount[id] ? Color.white : GetRoleColor(seer.GetCustomRole());
        return text.Color(color);
    }

    private static Vent SetTask(PlayerControl pc)
    {
        Vent vent = new();
        if (!AmongUsClient.Instance.AmHost) return vent;

        switch ((MapNames)Main.NormalOptions.MapId)
        {
            case MapNames.Skeld:
                vent = new SkeldSpecialVentTask().SetVentTask(pc);
                break;
            case MapNames.Mira:
                vent = new MiraHQSpecialVentTask().SetVentTask(pc);
                break;
            case MapNames.Polus:
                vent = new PolusSpecialVentTask().SetVentTask(pc);
                break;
            case MapNames.Airship:
                vent = new AirshipSpecialVentTask().SetVentTask(pc);
                break;
            case MapNames.Fungle:
                vent = new FungleSpecialVentTask().SetVentTask(pc);
                break;
        }
        return vent;
    }

    public abstract class SpecialVentTask
    {
        public virtual Vent SetVentTask(PlayerControl player)
        {
            Vent ventPoint = GetVentPoint();
            Logger.Info($"{player.GetNameWithRole()}：タスクNextId:{ventPoint.id}/{ventPoint.name}", "VentEnterTask.Set");
            return ventPoint;
        }
        public abstract Vent GetVentPoint();
    }

    public class SkeldSpecialVentTask : SpecialVentTask
    {
        public enum VentPoint
        {
            Admin = 0,
            KONOJI,
            Cafeteria,
            Electrical,
            UpperEngine,
            Security,
            MedBay,
            Weapons,
            Reactor_Lower,
            LowerEngine,
            Shields,
            Reactor_Upper,
            Navigation_Upper,
            Navigation_Lower,

            MaxCount
        }
        public override Vent GetVentPoint()
        {
            Vent v;
            var rand = IRandom.Instance;
            v.id = rand.Next((int)VentPoint.MaxCount);
            v.name = Translator.GetString(((VentPoint)v.id).ToString());
            return v;
        }
    }
    public class MiraHQSpecialVentTask : SpecialVentTask
    {
        public enum VentPoint
        {
            Balcony = 1,
            Cafeteria_Up,
            Reactor,
            Laboratory,
            Office,
            Admin,
            Greenhouse,
            MedBay,
            Decontamination,
            LockerRoom,
            Launchpad,

            MaxCount
        }
        public override Vent GetVentPoint()
        {
            Vent v;
            var rand = IRandom.Instance;
            v.id = rand.Next((int)VentPoint.MaxCount);
            v.name = Translator.GetString(((VentPoint)v.id).ToString());
            return v;
        }
    }
    public class PolusSpecialVentTask : SpecialVentTask
    {
        public enum VentPoint
        {
            Security_Up = 0,
            Electrical_Down,
            O2,
            Communications_Down,
            Office,
            Admin,
            Laboratory,
            Laboratory_Down,
            Storage,
            Dropship_RightDown,
            Dropship_LeftDown,
            Admin_Left,

            MaxCount
        }
        public override Vent GetVentPoint()
        {
            Vent v;
            var rand = IRandom.Instance;
            v.id = rand.Next((int)VentPoint.MaxCount);
            v.name = Translator.GetString(((VentPoint)v.id).ToString());
            return v;
        }
    }
    public class AirshipSpecialVentTask : SpecialVentTask
    {
        public enum VentPoint
        {
            Vault = 0,
            Cockpit,
            ViewingDeck,
            Engine,
            Kitchen,
            MainHall_Down,
            MainHall_Up,
            GapRoom_Right,
            GapRoom_Left,
            Showers,
            Records,
            CargoBay,

            MaxCount,
        }
        public override Vent GetVentPoint()
        {
            Vent v;
            var rand = IRandom.Instance;
            v.id = rand.Next((int)VentPoint.MaxCount);
            v.name = Translator.GetString(((VentPoint)v.id).ToString());
            return v;
        }
    }
    public class FungleSpecialVentTask : SpecialVentTask
    {
        public enum VentPoint
        {
            Communications = 0,
            Kitchen,
            Lookout,
            TheDorm_Up,
            Laboratory,
            Reactor,
            Laboratory_Right,
            Jungle_RightDown,
            SplashZone,
            Dropship_Left,

            MaxCount
        }
        public override Vent GetVentPoint()
        {
            Vent v;
            var rand = IRandom.Instance;
            v.id = rand.Next((int)VentPoint.MaxCount);
            v.name = Translator.GetString(((VentPoint)v.id).ToString());
            return v;
        }
    }
}