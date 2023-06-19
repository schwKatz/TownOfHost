using System.Collections.Generic;
using AmongUs.GameOptions;

using UnityEngine;
using TownOfHost.Roles.Core;
using static TownOfHost.Utils;

namespace TownOfHost.Roles.Neutral;

public sealed class AntiComplete : RoleBase
{
    public static readonly SimpleRoleInfo RoleInfo =
        new(
            typeof(AntiComplete),
            player => new AntiComplete(player),
            CustomRoles.AntiComplete,
            () => RoleTypes.Crewmate,
            CustomRoleTypes.Neutral,
            60000,
            SetupOptionItem,
            "アンチコンプリート",
            "#ec62a5"
        );
    public AntiComplete(PlayerControl player)
    : base(
        RoleInfo,
        player,
        () => KnowOption ? HasTask.ForRecompute : HasTask.False
    )
    {
        StartGuardCount = OptionGuardCount.GetInt();
        KnowOption = OptionKnowOption.GetBool();
        KnowNotask = OptionKnowNotask.GetBool();
        KnowCompTask = OptionKnowCompTask.GetBool();
        AddGuardCount = OptionKnowCompTask.GetInt();
    }

    private static OptionItem OptionGuardCount;
    private static OptionItem OptionKnowOption;
    private static OptionItem OptionKnowNotask;
    private static OptionItem OptionKnowCompTask;
    private static OptionItem OptionAddGuardCount;
    private static Options.OverrideTasksData Tasks;
    private enum OptionName
    {
        AntiCompGuardCount,
        AntiCompKnowOption,
        AntiCompKnowNotask,
        AntiCompKnowCompTask,
        AntiCompAddGuardCount,
    }
    private static int StartGuardCount;
    private static bool KnowOption;
    private static bool KnowNotask;
    private static bool KnowCompTask;
    private static int AddGuardCount;

    int GuardCount;

    private static void SetupOptionItem()
    {
        OptionGuardCount = IntegerOptionItem.Create(RoleInfo, 10, OptionName.AntiCompGuardCount, new(0, 20, 1), 2, false)
                .SetValueFormat(OptionFormat.Seconds);
        OptionKnowOption = BooleanOptionItem.Create(RoleInfo, 11, OptionName.AntiCompKnowOption, false, false);
        OptionKnowNotask = BooleanOptionItem.Create(RoleInfo, 12, OptionName.AntiCompKnowNotask, true, false, OptionKnowOption);
        OptionKnowCompTask = BooleanOptionItem.Create(RoleInfo, 13, OptionName.AntiCompKnowCompTask, false, false, OptionKnowOption);
        OptionAddGuardCount = IntegerOptionItem.Create(RoleInfo, 14, OptionName.AntiCompAddGuardCount, new(0, 10, 1), 0, false, OptionKnowOption)
                .SetValueFormat(OptionFormat.Seconds);
        // 20-23を使用
        Tasks = Options.OverrideTasksData.Create(RoleInfo, 20, OptionKnowOption);
    }
    public override void Add()
    {
        GuardCount = StartGuardCount;
    }

    public override bool OnCheckMurderAsTarget(MurderInfo info)
    {
        (var killer, var target) = info.AttemptTuple;
        if (GuardCount <= 0) return true;//普通にキル

        killer.RpcGuardAndKill(target);
        target.RpcGuardAndKill(target);
        GuardCount--;
        Logger.Info($"{target.GetNameWithRole()} : ガード残り{GuardCount}回", "AntiComp");
        return false;
    }
    public override bool OnCompleteTask()
    {
        if (IsTaskFinished && Player.IsAlive())
            GuardCount += AddGuardCount;
        return true;
    }

    public override string GetMark(PlayerControl seer, PlayerControl seen, bool isForMeeting = false)
    {
        //seenが省略の場合seer
        seen ??= seer;

        if (seer.Is(CustomRoles.AntiComplete) && KnowOption && seer.GetPlayerTaskState().IsTaskFinished && seer != seen)
        {
            if (KnowCompTask && seen.GetPlayerTaskState().IsTaskFinished && !isForMeeting)
                return ColorString(RoleInfo.RoleColor, "◎");

            if (KnowNotask && !seen.GetPlayerTaskState().hasTasks)
                return ColorString(RoleInfo.RoleColor, "×");
        }
        return string.Empty;
    }

    public override string GetProgressText(bool comms = false) => Utils.ColorString(GuardCount > 0 ? RoleInfo.RoleColor : Color.gray, $"({GuardCount})");

    public override bool OnCheckForEndVoting(ref List<MeetingHud.VoterState> statesList, PlayerVoteArea pva)
    {
        if (pva.DidVote && Player.PlayerId != pva.VotedFor
            && pva.VotedFor < 253 && Player.IsAlive())
        {
            statesList.Add(new()
            {
                VoterId = pva.TargetPlayerId,
                VotedForId = pva.VotedFor
            });
            var states = statesList.ToArray();
            if (AntiBlackout.OverrideExiledPlayer)
            {
                MeetingHud.Instance.RpcVotingComplete(states, null, true);
                ExileControllerWrapUpPatch.AntiBlackout_LastExiled = Player.Data;
            }
            else MeetingHud.Instance.RpcVotingComplete(states, Player.Data, false); //通常処理
            Logger.Info("アンチコンプによる強制会議終了", "Special Phase");

            var taskState = PlayerState.GetByPlayerId(pva.VotedFor).GetTaskState();
            if (taskState.IsTaskFinished) MyState.DeathReason = CustomDeathReason.Win;
            else MyState.DeathReason = CustomDeathReason.Suicide;

            return true;
        }

        return true;
    }

    public override void OnExileWrapUp(GameData.PlayerInfo exiled, ref bool DecidedWinner)
    {
        if (!AmongUsClient.Instance.AmHost || Player.PlayerId != exiled.PlayerId) return;
        if (MyState.DeathReason != CustomDeathReason.Win) return;

        CustomWinnerHolder.ResetAndSetWinner(CustomWinner.AntiComplete);
        CustomWinnerHolder.WinnerIds.Add(exiled.PlayerId);
        DecidedWinner = true;
    }
}
