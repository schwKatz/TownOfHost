using AmongUs.GameOptions;

using TownOfHost.Roles.Core;
using TownOfHost.Roles.Core.Interfaces;

namespace TownOfHost.Roles.Madmate;
public sealed class MadNatureCalls : RoleBase, IKillFlashSeeable, IDeathReasonSeeable
{
    public static readonly SimpleRoleInfo RoleInfo =
        new(
            typeof(MadNatureCalls),
            player => new MadNatureCalls(player),
            CustomRoles.MadNatureCalls,
            () => RoleTypes.Engineer,
            CustomRoleTypes.Madmate,
            10400,
            null,
            "マッドネイチャコール",
            introSound: () => GetIntroSound(RoleTypes.Impostor)
        );
    public MadNatureCalls(PlayerControl player)
    : base(
        RoleInfo,
        player
    )
    {
        canSeeKillFlash = Options.MadmateCanSeeKillFlash.GetBool();
        canSeeDeathReason = Options.MadmateCanSeeDeathReason.GetBool();
    }

    private static bool canSeeKillFlash;
    private static bool canSeeDeathReason;

    public override bool OnEnterVent(PlayerPhysics physics, int ventId)
    {
        ShipStatus.Instance.RpcRepairSystem(SystemTypes.Doors, 79);
        ShipStatus.Instance.RpcRepairSystem(SystemTypes.Doors, 80);
        ShipStatus.Instance.RpcRepairSystem(SystemTypes.Doors, 81);
        ShipStatus.Instance.RpcRepairSystem(SystemTypes.Doors, 82);
        return true;
    }

    public bool CheckKillFlash(MurderInfo info) => canSeeKillFlash;
    public bool CheckSeeDeathReason(PlayerControl seen) => canSeeDeathReason;
}
