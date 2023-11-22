using AmongUs.GameOptions;

using TownOfHostY.Roles.Core;

namespace TownOfHostY.Roles.Crewmate;
public sealed class Nekomata : RoleBase
{
    public static readonly SimpleRoleInfo RoleInfo =
         SimpleRoleInfo.Create(
            typeof(Nekomata),
            player => new Nekomata(player),
            CustomRoles.Nekomata,
            () => RoleTypes.Crewmate,
            CustomRoleTypes.Crewmate,
            (int)Options.offsetId.CrewY + 200,
            null,
            "猫又",
            "#00ffff"
        );
    public Nekomata(PlayerControl player)
    : base(
        RoleInfo,
        player
    )
    { }
}