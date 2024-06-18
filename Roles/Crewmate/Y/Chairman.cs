using AmongUs.GameOptions;

using TownOfHostY.Modules;
using TownOfHostY.Roles.Core;

namespace TownOfHostY.Roles.Crewmate;
public sealed class Chairman : RoleBase
{
    public static readonly SimpleRoleInfo RoleInfo =
         SimpleRoleInfo.Create(
            typeof(Chairman),
            player => new Chairman(player),
            CustomRoles.Chairman,
            () => RoleTypes.Engineer,
            CustomRoleTypes.Crewmate,
            (int)Options.offsetId.CrewY + 300,
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
    public override void OnReportDeadBody(PlayerControl reporter, NetworkedPlayerInfo target)
    {
        if (reporter == Player && target == null) //ボタン
            LeftButtonCount--;
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

    public override (byte? votedForId, int? numVotes, bool doVote) ModifyVote(byte voterId, byte sourceVotedForId, bool isIntentional)
    {
        // 既定値
        var (votedForId, numVotes, doVote) = base.ModifyVote(voterId, sourceVotedForId, isIntentional);
        var baseVote = (votedForId, numVotes, doVote);
        if (IgnoreSkip || voterId != Player.PlayerId || sourceVotedForId == Player.PlayerId || sourceVotedForId >= 253 || !Player.IsAlive())
        {
            return baseVote;
        }
        MeetingVoteManager.Instance.ClearAndExile(Player.PlayerId, 253);
        return (votedForId, numVotes, false);
    }

    public override string GetSuffix(PlayerControl seer, PlayerControl seen = null, bool isForMeeting = false)
    {
        if (!isForMeeting || !Player.IsAlive() || IgnoreSkip) return string.Empty;

        //seenが省略の場合seer
        seen ??= seer;
        //seeおよびseenが自分である場合以外は関係なし
        if (!Is(seer) || !Is(seen)) return "";

        return Translator.GetString("ChairmanVote").Color(RoleInfo.RoleColor);
    }

    public override void AfterMeetingTasks()=> Player.RpcResetAbilityCooldown();
}