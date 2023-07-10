using AmongUs.GameOptions;

using TownOfHostY.Roles.Core;
using TownOfHostY.Roles.Core.Interfaces;

namespace TownOfHostY.Roles.Madmate;
public sealed class MadNatureCalls : RoleBase, IKillFlashSeeable, IDeathReasonSeeable
{
    public static readonly SimpleRoleInfo RoleInfo =
        SimpleRoleInfo.Create(
            typeof(MadNatureCalls),
            player => new MadNatureCalls(player),
            CustomRoles.MadNatureCalls,
            () => RoleTypes.Engineer,
            CustomRoleTypes.Madmate,
            5400,
            SetupOptionItem,
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

    public static void SetupOptionItem()
    {
        Options.SetUpAddOnOptions(RoleInfo.ConfigId + 10, RoleInfo.RoleName, RoleInfo.Tab);
    }
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
