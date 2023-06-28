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
         SimpleRoleInfo.Create(
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
        TargetResult = new();
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

    private static int NumOfForecast;
    private static int ForecastTaskTrigger;
    private static bool CanForecastNoDeadBody;
    private static bool ConfirmCamp;
    private static bool KillerOnly;

    private PlayerControl Target;
    private Dictionary<byte, PlayerControl> TargetResult = new ();

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

    public override (byte? votedForId, int? numVotes, bool doVote) OnVote(byte voterId, byte sourceVotedForId)
    {
        var baseVote = base.OnVote(voterId, sourceVotedForId);
        //Logger.Info($"MeetingPrefix voter: {Player.name}, vote: {pva.DidVote} target: {pva.name}, notSelf: {Player.PlayerId != pva.VotedFor}, pcIsDead: {Player.Data.IsDead}, voteFor: {pva.VotedFor}", "FortuneTeller");
        if (voterId == Player.PlayerId && sourceVotedForId != Player.PlayerId && sourceVotedForId < 253 && Player.IsAlive())
        {
            VoteForecastTarget(sourceVotedForId);
        }
        return baseVote;
    }
    private void VoteForecastTarget(byte targetId)
    {
        if (!CanForecastNoDeadBody &&
            GameData.Instance.AllPlayers.ToArray().Where(x => x.IsDead).Count() <= 0) //死体無し
        {
            Logger.Info($"VoteForecastTarget NotForecast NoDeadBody player: {Player.name}, targetId: {targetId}", "FortuneTeller");
            return;
        }
        if (MyTaskState.CompletedTasksCount < ForecastTaskTrigger) //占い可能タスク数
        {
            Logger.Info($"VoteForecastTarget NotForecast LessTasks player: {Player.name}, targetId: {targetId}, task: {MyTaskState.CompletedTasksCount}/{ForecastTaskTrigger}", "FortuneTeller");
            return;
        }

        var target = GetPlayerById(targetId);
        if (target == null || !target.IsAlive()) return;
        if (TargetResult.ContainsKey(targetId)) return;  //既に占い結果があるときはターゲットにならない

        Target = target;
        Logger.Info($"SetForecastTarget player: {Player.name}, target: {target.name}", "FortuneTeller");
    }
    public override bool OnReportDeadBody(PlayerControl reporter, GameData.PlayerInfo target)
    {
        SetForecastResult();
        return true;
    }
    private void SetForecastResult()
    {
        if (Target == null)
        {
            Logger.Info($"SetForecastResult NotSet NotHasForecastTarget player: {Player.name}", "FortuneTeller");
            return;
        }
        if (Target == null || !Target.IsAlive())
        {
            Logger.Info($"SetForecastResult NotSet TargetNotValid player: {Player.name}, target: {Target?.name} dead: {Target?.Data.IsDead}, disconnected: {Target?.Data.Disconnected}", "FortuneTeller");
            return;
        }

        if (TargetResult.Count == 0)
        {
            TargetResult = new();
        }
        if (TargetResult.Count >= NumOfForecast)
        {
            Logger.Info($"SetForecastResult NotSet ForecastCountOver player: {Player.name}, target: {Target.name} forecastCount: {TargetResult.Count}, canCount: {NumOfForecast}", "FortuneTeller");
            Target = null;
            return;
        }

        TargetResult[Target.PlayerId] = Target;
        Logger.Info($"SetForecastResult SetTarget player: {Player.name}, target: {Target.name}", "FortuneTeller");
        Target = null;
    }
    public bool HasForecastResult() => TargetResult.Count > 0;
    private int ForecastLimit => NumOfForecast - TargetResult.Count;
    public override string GetProgressText(bool comms = false)
    {
        if (MyTaskState.CompletedTasksCount < ForecastTaskTrigger) return string.Empty;

        return ColorString(ForecastLimit > 0 ? RoleInfo.RoleColor : Color.gray, $"[{ForecastLimit}]");
    }
    public override string GetMark(PlayerControl seer, PlayerControl seen, bool isForMeeting = false)
    {
        if (seen == null || !isForMeeting) return string.Empty;

        if (TargetResult.ContainsKey(seen.PlayerId))
            return ColorString(RoleInfo.RoleColor, "★");

        return string.Empty;
    }
    public override void OverrideRoleNameAsSeer(PlayerControl seen, bool isMeeting, ref bool enabled, ref Color roleColor, ref string roleText)
    {
        if (!isMeeting) return;
        if (!TargetResult.ContainsKey(seen.PlayerId)) return;
        if (KillerOnly &&
            !(seen.GetCustomRole().IsImpostor() || seen.IsNeutralKiller() || seen.IsCrewKiller())) return;

        enabled = true;

        if (!ConfirmCamp) return;   //役職表示

        //陣営表示
        if (seen.GetCustomRole().IsImpostor() || seen.GetCustomRole().IsMadmate())
        {
            roleColor = Palette.ImpostorRed;
            roleText = GetString("TeamImpostor");
        }
        else if (seen.GetCustomRole().IsNeutral())
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
    public bool KnowTargetRoleColor(PlayerControl target)
    {
        if (!TargetResult.ContainsKey(target.PlayerId)) return false;
        if (ConfirmCamp) return false;
        if (KillerOnly &&
            !(target.GetCustomRole().IsImpostor() || target.IsNeutralKiller() || target.IsCrewKiller())) return false;
        return true;
    }
}