using AmongUs.GameOptions;

using TownOfHost.Roles.Core;

namespace TownOfHost.Roles.Crewmate;
public sealed class Nekomata : RoleBase
{
    public static readonly SimpleRoleInfo RoleInfo =
        new(
            typeof(Nekomata),
            player => new Nekomata(player),
            CustomRoles.Nekomata,
            () => RoleTypes.Crewmate,
            CustomRoleTypes.Crewmate,
            35300,
            null,
            "ne",
            "#00ffff"
        );
    public Nekomata(PlayerControl player)
    : base(
        RoleInfo,
        player
    )
    { }
}