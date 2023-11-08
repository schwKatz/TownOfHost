using AmongUs.GameOptions;

using TownOfHostY.Roles.Core;

namespace TownOfHostY.Roles.Vanilla;

public sealed class GuardianAngel : RoleBase
{
    public static readonly SimpleRoleInfo RoleInfo =
        SimpleRoleInfo.CreateForVanilla(
            typeof(GuardianAngel),
            player => new GuardianAngel(player),
            RoleTypes.GuardianAngel,
            assignInfo: new RoleAssignInfo(CustomRoles.GuardianAngel, CustomRoleTypes.Crewmate)
            {
                IsInitiallyAssignableCallBack = () => false
            }
        );
    public GuardianAngel(PlayerControl player)
    : base(
        RoleInfo,
        player
    )
    { }
}