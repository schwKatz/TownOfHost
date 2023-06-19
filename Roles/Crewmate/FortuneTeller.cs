using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using AmongUs.GameOptions;
using TownOfHost.Roles.Core;

using static TownOfHost.Utils;
using static TownOfHost.Translator;

namespace TownOfHost.Roles.Crewmate;
public sealed class FortuneTeller : RoleBase
{
    public static readonly SimpleRoleInfo RoleInfo =
        new(
            typeof(FortuneTeller),
            player => new FortuneTeller(player),
            CustomRoles.FortuneTeller,
            () => RoleTypes.Crewmate,
            CustomRoleTypes.Crewmate,
            36400,
            SetupOptionItem,
            "占い師",
            "#9370db"
        );
    public FortuneTeller(PlayerControl player)
    : base(
        RoleInfo,
        player
    )
    {
        NumOfForecast = OptionNumOfForecast.GetInt();
        ForecastTaskTrigger = OptionForecastTaskTrigger.GetInt();
        CanForecastNoDeadBody = OptionCanForecastNoDeadBody.GetBool();
        ConfirmCamp = OptionConfirmCamp.GetBool();
        KillerOnly = OptionKillerOnly.GetBool();

        Target = null;
    }

    public static OptionItem OptionNumOfForecast;
    public static OptionItem OptionForecastTaskTrigger;
    public static OptionItem OptionCanForecastNoDeadBody;
    public static OptionItem OptionConfirmCamp;
    public static OptionItem OptionKillerOnly;
    enum OptionName
    {
        FortuneTellerNumOfForecast,
        FortuneTellerForecastTaskTrigger,
        FortuneTellerCanForecastNoDeadBody,
        FortuneTellerConfirmCamp,
        FortuneTellerKillerOnly,
    }

    public static int NumOfForecast;
    public static int ForecastTaskTrigger;
    public static bool CanForecastNoDeadBody;
    public static bool ConfirmCamp;
    public static bool KillerOnly;

    static PlayerControl Target;
    static Dictionary<byte, PlayerControl> TargetResult = new ();

    private static void SetupOptionItem()
    {
        OptionNumOfForecast = IntegerOptionItem.Create(RoleInfo, 10, OptionName.FortuneTellerNumOfForecast, new(1, 20, 1), 2, false)
            .SetValueFormat(OptionFormat.Times);
        OptionForecastTaskTrigger = IntegerOptionItem.Create(RoleInfo, 11, OptionName.FortuneTellerForecastTaskTrigger, new(0, 20, 1), 5, false)
            .SetValueFormat(OptionFormat.Pieces);
        OptionCanForecastNoDeadBody = BooleanOptionItem.Create(RoleInfo, 12, OptionName.FortuneTellerCanForecastNoDeadBody, false, false);
        OptionConfirmCamp = BooleanOptionItem.Create(RoleInfo, 13, OptionName.FortuneTellerConfirmCamp, true, false);
        OptionKillerOnly = BooleanOptionItem.Create(RoleInfo, 14, OptionName.FortuneTellerKillerOnly, true, false);
    }

    public override string GetProgressText(bool comms = false)
    {
        if ((Player?.GetPlayerTaskState()?.CompletedTasksCount ?? -1) < ForecastTaskTrigger) return string.Empty;

        return ColorString(RoleInfo.RoleColor, $"[{NumOfForecast - TargetResult.Count}]");
    }
    public override bool OnCheckForEndVoting(ref List<MeetingHud.VoterState> statesList, PlayerVoteArea pva)
    {
        Logger.Info($"MeetingPrefix voter: {Player.name}, vote: {pva.DidVote} target: {pva.name}, notSelf: {Player.PlayerId != pva.VotedFor}, pcIsDead: {Player.Data.IsDead}, voteFor: {pva.VotedFor}", "FortuneTeller");
        if (pva.DidVote && pva.VotedFor != Player.PlayerId && pva.VotedFor < 253 && !Player.Data.IsDead) //自分以外に投票
            VoteForecastTarget(Player,pva.VotedFor);

        return true;
    }
    public override string GetMark(PlayerControl seer, PlayerControl seen, bool isForMeeting = false)
    {
        //seenが省略の場合seer
        seen ??= seer;

        if (isForMeeting && HasForecastResult(seer, seen.PlayerId))
            return ColorString(RoleInfo.RoleColor, "★");

        return string.Empty;
    }
    public override bool OnReportDeadBody(PlayerControl reporter, GameData.PlayerInfo target)
    {
        if (Player.Is(CustomRoles.FortuneTeller) && Player.IsAlive() && Target != null)
            SetForecastResult(Player);
        return true;
    }
    public override void OverrideRoleNameAsSeer(PlayerControl seen, bool isMeeting, ref bool enabled, ref Color roleColor, ref string roleText)
    {
        if (!isMeeting) return;

        if (IsShowTargetRole(Player, seen))
        {
            enabled = true;
        }
        else if (IsShowTargetCamp(Player, seen, out bool onlyKiller))
        {
            enabled = true;
            if (seen.GetCustomRole().IsImpostor() ||
                (!onlyKiller && seen.GetCustomRole().IsMadmate()))
            {
                roleColor = Palette.ImpostorRed;
                roleText = GetString("TeamImpostor");
            }
            else if (seen.GetCustomRole().IsNeutral() &&
                (!onlyKiller || seen.IsNeutralKiller()))
            {
                roleColor = Color.gray;
                roleText = GetString("Neutral");
            }
            else
            {
                roleColor = Palette.CrewmateBlue;
                roleText = GetString("TeamCrewmate");
            }
        }
    }

    private void SetForecastResult(PlayerControl player)
    {
        if (Target == null)
        {
            Logger.Info($"SetForecastResult NotSet NotHasForecastTarget player: {player.name}", "FortuneTeller");
            return;
        }
        if (Target == null || !Target.IsAlive())
        {
            Logger.Info($"SetForecastResult NotSet TargetNotValid player: {player.name}, target: {Target?.name} dead: {Target?.Data.IsDead}, disconnected: {Target?.Data.Disconnected}", "FortuneTeller");
            return;
        }

        if (TargetResult.Count == 0)
        {
            TargetResult = new();
        }
        if (TargetResult.Count >= NumOfForecast)
        {
            Logger.Info($"SetForecastResult NotSet ForecastCountOver player: {player.name}, target: {Target.name} forecastCount: {TargetResult.Count}, canCount: {NumOfForecast}", "FortuneTeller");
            Target = null;
            return;
        }

        TargetResult[Target.PlayerId] = Target;
        Logger.Info($"SetForecastResult SetTarget player: {player.name}, target: {Target.name}", "FortuneTeller");
        Target = null;
    }

    public void VoteForecastTarget(PlayerControl player, byte targetId)
    {
        if (!CanForecastNoDeadBody &&
            GameData.Instance.AllPlayers.ToArray().Where(x => x.IsDead).Count() <= 0) //死体無し
        {
            Logger.Info($"VoteForecastTarget NotForecast NoDeadBody player: {player.name}, targetId: {targetId}", "FortuneTeller");
            return;
        }
        var completedTasks = player.GetPlayerTaskState().CompletedTasksCount;
        if (completedTasks < ForecastTaskTrigger) //占い可能タスク数
        {
            Logger.Info($"VoteForecastTarget NotForecast LessTasks player: {player.name}, targetId: {targetId}, task: {completedTasks}/{ForecastTaskTrigger}", "FortuneTeller");
            return;
        }

        SetForecastTarget(player, targetId);
    }
    private void SetForecastTarget(PlayerControl player, byte targetId)
    {
        var target = GetPlayerById(targetId);
        if (target == null || !target.IsAlive()) return;
        if (HasForecastResult(player, target.PlayerId)) return;  //既に占い結果があるときはターゲットにならない

        Target = target;
        Logger.Info($"SetForecastTarget player: {player.name}, target: {target.name}", "FortuneTeller");
    }
    public static bool HasForecastResult(PlayerControl player, byte targetId)
    {
        return TargetResult.ContainsKey(targetId);
    }
    public bool HasForecastResult(PlayerControl player)
    {
        return TargetResult.Count > 0;
    }
    public static bool IsShowTargetRole(PlayerControl seer, PlayerControl target)
    {
        if (!HasForecastResult(seer,target.PlayerId)) return false;
        if (ConfirmCamp) return false;
        if (KillerOnly &&
            !(target.GetCustomRole().IsImpostor() || target.IsNeutralKiller())) return false;
        return true;
    }
    public bool IsShowTargetCamp(PlayerControl seer, PlayerControl target, out bool onlyKiller)
    {
        onlyKiller = KillerOnly;
        if (!HasForecastResult(seer,target.PlayerId)) return false;
        return !IsShowTargetRole(seer, target);
    }
}