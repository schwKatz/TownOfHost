using System.Collections.Generic;
using AmongUs.GameOptions;

using TownOfHost.Roles.Core;
using static TownOfHost.CheckForEndVotingPatch;

namespace TownOfHost.Roles.Crewmate;
public sealed class Chairman : RoleBase
{
    public static readonly SimpleRoleInfo RoleInfo =
        new(
            typeof(Chairman),
            player => new Chairman(player),
            CustomRoles.Chairman,
            () => RoleTypes.Engineer,
            CustomRoleTypes.Crewmate,
            35410,
            SetupOptionItem,
            "チェアマン",
            "#204d42",
            introSound: () => GetIntroSound(RoleTypes.Crewmate)
        );
    public Chairman(PlayerControl player)
    : base(
        RoleInfo,
        player
    )
    {
        NumOfUseButton = OptionNumOfUseButton.GetInt();
        IgnoreSkip = OptionIgnoreSkip.GetBool();

        LeftButtonCount = NumOfUseButton;
    }

    private static OptionItem OptionNumOfUseButton;
    private static OptionItem OptionIgnoreSkip;
    enum OptionName
    {
        MayorNumOfUseButton,
        ChairmanIgnoreSkip,
    }
    public static int NumOfUseButton;
    public static bool IgnoreSkip;

    public int LeftButtonCount;
    private static void SetupOptionItem()
    {
        OptionNumOfUseButton = IntegerOptionItem.Create(RoleInfo, 10, OptionName.MayorNumOfUseButton, new(1, 20, 1), 2, false)
            .SetValueFormat(OptionFormat.Times);
        OptionIgnoreSkip = BooleanOptionItem.Create(RoleInfo, 11, OptionName.ChairmanIgnoreSkip, false, false);
    }
    public override void ApplyGameOptions(IGameOptions opt)
    {
        Logger.Warn($"{LeftButtonCount} <= 0", "Mayor.ApplyGameOptions");
        AURoleOptions.EngineerCooldown =
            LeftButtonCount <= 0
            ? 255f
            : opt.GetInt(Int32OptionNames.EmergencyCooldown);
        AURoleOptions.EngineerInVentMaxTime = 1;
    }
    public override bool OnReportDeadBody(PlayerControl reporter, GameData.PlayerInfo target)
    {
        if (reporter == Player && target == null) //ボタン
            LeftButtonCount--;

        return true;
    }
    public override bool OnEnterVent(PlayerPhysics physics, int ventId)
    {
        if (LeftButtonCount > 0)
        {
            var user = physics.myPlayer;
            physics.RpcBootFromVent(ventId);
            user?.ReportDeadBody(null);
        }

        return false;
    }
    public override bool OnCheckForEndVoting(ref List<MeetingHud.VoterState> statesList, PlayerVoteArea pva)
    {
        //死んでいないチェアマンが投票済み
        if (!IgnoreSkip &&
            pva.DidVote &&
            pva.VotedFor != Player.PlayerId &&
            pva.VotedFor < 253 &&
            Player.IsAlive())
        {
            var voteTarget = Utils.GetPlayerById(pva.VotedFor);

            MeetingHud.Instance.RpcVotingComplete(new MeetingHud.VoterState[]{ new ()
                {
                    VoterId = pva.TargetPlayerId,
                    VotedForId = 253
                }}, null, false); //RPC

            Logger.Info("ディクテーターによる強制会議終了", "Special Phase");
            return false;
        }
        return true;
    }
    public override void AfterMeetingTasks()=> Player.RpcResetAbilityCooldown();
}