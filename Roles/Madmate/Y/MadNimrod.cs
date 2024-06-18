using System.Collections.Generic;
using AmongUs.GameOptions;

using TownOfHostY.Modules;
using TownOfHostY.Roles.Core;

namespace TownOfHostY.Roles.Madmate;
public sealed class MadNimrod : RoleBase
{
    public static readonly SimpleRoleInfo RoleInfo =
        SimpleRoleInfo.Create(
            typeof(MadNimrod),
            player => new MadNimrod(player),
            CustomRoles.MadNimrod,
            () => OptionCanVent.GetBool() ? RoleTypes.Engineer : RoleTypes.Crewmate,
            CustomRoleTypes.Madmate,
            (int)Options.offsetId.MadY + 400,
            SetupOptionItem,
            "マッドニムロッド",
            introSound: () => GetIntroSound(RoleTypes.Impostor)
        );
    public MadNimrod(PlayerControl player)
    : base(
        RoleInfo,
        player
    )
    {
        playerIdList = new();
        IsExecutionMeeting = byte.MaxValue;
    }
    public override void OnDestroy()
    {
        playerIdList.Clear();
    }

    private static OptionItem OptionCanVent;

    public static List<byte> playerIdList = new();
    private static byte IsExecutionMeeting = byte.MaxValue;

    public static void SetupOptionItem()
    {
        OptionCanVent = BooleanOptionItem.Create(RoleInfo, 10, GeneralOption.CanVent, false, false);
        Options.SetUpAddOnOptions(RoleInfo.ConfigId + 20, RoleInfo.RoleName, RoleInfo.Tab);
    }
    public override void Add()
    {
        playerIdList.Add(Player.PlayerId);
    }

    public static NetworkedPlayerInfo VoteChange(NetworkedPlayerInfo Exiled)
    {
        if (Exiled == null || !playerIdList.Contains(Exiled.PlayerId)) return Exiled;

        _ = new LateTask(() =>
        {
            IsExecutionMeeting = Exiled.PlayerId;
            Utils.GetPlayerById(Exiled.PlayerId).ReportDeadBody(Exiled);
        }, 15f, "NimrodExiled");
        return null;
    }
    public override void OnStartMeeting()
    {
        if (IsExecutionMeeting == byte.MaxValue) return;

        Utils.SendMessage(Translator.GetString("IsNimrodMeetingText"),
            title: $"<color={Utils.GetRoleColorCode(CustomRoles.Nimrod)}>{Translator.GetString("IsNimrodMeetingTitle")}</color>");
    }
    public static (string, int) AddMeetingDisplay()
    {
        if (IsExecutionMeeting == byte.MaxValue) return ("", 0);

        string text = Translator.GetString("MDisplay.NimrodTitle").Color(Utils.GetRoleColor(CustomRoles.Nimrod));
        text += "\n";
        return (text, 1);
    }

    public override (byte? votedForId, int? numVotes, bool doVote) ModifyVote(byte voterId, byte sourceVotedForId, bool isIntentional)
    {
        // 既定値
        var (votedForId, numVotes, doVote) = base.ModifyVote(voterId, sourceVotedForId, isIntentional);
        var baseVote = (votedForId, numVotes, doVote);
        if (IsExecutionMeeting != Player.PlayerId || voterId != Player.PlayerId)
        {
            return baseVote;
        }
        MeetingHudPatch.TryAddAfterMeetingDeathPlayers(CustomDeathReason.Vote, Player.PlayerId);

        if (sourceVotedForId <= 15)
        {
            Utils.GetPlayerById(sourceVotedForId).SetRealKiller(Player);
            PlayerState.GetByPlayerId(sourceVotedForId).DeathReason = CustomDeathReason.Execution;
        }
        MeetingVoteManager.Instance.ClearAndExile(Player.PlayerId, sourceVotedForId);
        IsExecutionMeeting = byte.MaxValue;
        return (votedForId, numVotes, false);
    }
}