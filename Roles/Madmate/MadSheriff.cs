using AmongUs.GameOptions;

using TownOfHost.Roles.Core;
using TownOfHost.Roles.Core.Interfaces;

namespace TownOfHost.Roles.Madmate;
public sealed class MadSheriff : RoleBase, IKiller, IKillFlashSeeable, IDeathReasonSeeable
{
    public static readonly SimpleRoleInfo RoleInfo =
        new(
            typeof(MadSheriff),
            player => new MadSheriff(player),
            CustomRoles.MadSheriff,
            () => RoleTypes.Impostor,
            CustomRoleTypes.Madmate,
            10600,
            SetupOptionItem,
            "マッドシェリフ",
            requireResetCam: true
        );
    public MadSheriff(PlayerControl player)
    : base(
        RoleInfo,
        player,
        () => HasTask.False
    )
    {
        canSeeKillFlash = Options.MadmateCanSeeKillFlash.GetBool();
        canSeeDeathReason = Options.MadmateCanSeeDeathReason.GetBool();
        KillCooldown = OptionKillCooldown.GetFloat();
        MisfireKillsTarget = OptionMisfireKillsTarget.GetBool();
        CanVent = OptionCanVent.GetBool();
    }

    private static OptionItem OptionKillCooldown;
    private static OptionItem OptionMisfireKillsTarget;
    private static OptionItem OptionCanVent;

    enum OptionName
    {
        SheriffMisfireKillsTarget,
    }
    private static float KillCooldown;
    private static bool canSeeKillFlash;
    private static bool canSeeDeathReason;
    private static bool MisfireKillsTarget;
    public static bool CanVent;

    private static void SetupOptionItem()
    {
        OptionKillCooldown = FloatOptionItem.Create(RoleInfo, 10, GeneralOption.KillCooldown, new(0f, 180f, 2.5f), 30f, false)
            .SetValueFormat(OptionFormat.Seconds);
        OptionMisfireKillsTarget = BooleanOptionItem.Create(RoleInfo, 11, OptionName.SheriffMisfireKillsTarget, false, false);
        OptionCanVent = BooleanOptionItem.Create(RoleInfo, 12, GeneralOption.CanVent, false, false);
    }
    public override void Add()
    {
        var playerId = Player.PlayerId;

        if (!Main.ResetCamPlayerList.Contains(playerId))
            Main.ResetCamPlayerList.Add(playerId);
    }
    public float CalculateKillCooldown() => CanUseKillButton() ? KillCooldown : 0f;
    public bool CanUseKillButton() => Player.IsAlive();
    public override bool CanSabotage(SystemTypes systemType) => false;
    public override void ApplyGameOptions(IGameOptions opt)
    {
        opt.SetVision(false);
    }
    public void OnCheckMurderAsKiller(MurderInfo info)
    {
        if (Is(info.AttemptKiller) && !info.IsSuicide)
        {
            (var killer, var target) = info.AttemptTuple;

            PlayerState.GetByPlayerId(killer.PlayerId).DeathReason = CustomDeathReason.Misfire;
            killer.RpcMurderPlayer(killer);

            if (MisfireKillsTarget) killer.RpcMurderPlayer(target);
        }
    }

    public bool CheckKillFlash(MurderInfo info) => canSeeKillFlash;
    public bool CheckSeeDeathReason(PlayerControl seen) => canSeeDeathReason;
}