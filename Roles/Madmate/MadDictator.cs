using System.Collections.Generic;
using AmongUs.GameOptions;

using TownOfHost.Roles.Core;
using TownOfHost.Roles.Core.Interfaces;
using static TownOfHost.CheckForEndVotingPatch;

namespace TownOfHost.Roles.Crewmate;
public sealed class MadDictator : RoleBase, IKillFlashSeeable, IDeathReasonSeeable
{
    public static readonly SimpleRoleInfo RoleInfo =
        new(
            typeof(MadDictator),
            player => new MadDictator(player),
            CustomRoles.MadDictator,
            () => OptionCanVent.GetBool() ? RoleTypes.Engineer : RoleTypes.Crewmate,
            CustomRoleTypes.Madmate,
            10300,
            SetupOptionItem,
            "マッドディクテーター",
            introSound: () => GetIntroSound(RoleTypes.Impostor)
        );
    public MadDictator(PlayerControl player)
    : base(
        RoleInfo,
        player
    )
    {
        canSeeKillFlash = Options.MadmateCanSeeKillFlash.GetBool();
        canSeeDeathReason = Options.MadmateCanSeeDeathReason.GetBool();

        canVent = OptionCanVent.GetBool();
    }

    private static OptionItem OptionCanVent;

    private static bool canSeeKillFlash;
    private static bool canSeeDeathReason;
    private static bool canVent;

    private static void SetupOptionItem()
    {
        OptionCanVent = BooleanOptionItem.Create(RoleInfo, 10, GeneralOption.CanVent, false, false);
    }
    public override bool OnCheckForEndVoting(ref List<MeetingHud.VoterState> statesList, PlayerVoteArea pva)
    {
        //死んでいないディクテーターが投票済み
        if (pva.DidVote &&
            pva.VotedFor != Player.PlayerId &&
            pva.VotedFor < 253 &&
            Player.IsAlive())
        {
            var voteTarget = Utils.GetPlayerById(pva.VotedFor);
            TryAddAfterMeetingDeathPlayers(CustomDeathReason.Suicide, Player.PlayerId);
            statesList.Add(new()
            {
                VoterId = pva.TargetPlayerId,
                VotedForId = pva.VotedFor
            });
            var states = statesList.ToArray();
            if (AntiBlackout.OverrideExiledPlayer)
            {
                MeetingHud.Instance.RpcVotingComplete(states, null, true);
                ExileControllerWrapUpPatch.AntiBlackout_LastExiled = voteTarget.Data;
            }
            else MeetingHud.Instance.RpcVotingComplete(states, voteTarget.Data, false); //通常処理

            CheckForDeathOnExile(CustomDeathReason.Vote, pva.VotedFor);
            Logger.Info($"ディクテーターによる強制会議終了(追放者:{voteTarget.GetNameWithRole()})", "Special Phase");
            voteTarget.SetRealKiller(Player);
        }
        return false;
    }

    public bool CheckKillFlash(MurderInfo info) => canSeeKillFlash;
    public bool CheckSeeDeathReason(PlayerControl seen) => canSeeDeathReason;
}