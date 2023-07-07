using AmongUs.GameOptions;
using TownOfHost.Roles.Core;

using static TownOfHost.Translator;
using static TownOfHost.Utils;

namespace TownOfHost.Roles.Crewmate;
public sealed class SeeingOff : RoleBase
{
    public static readonly SimpleRoleInfo RoleInfo =
         SimpleRoleInfo.Create(
            typeof(SeeingOff),
            player => new SeeingOff(player),
            CustomRoles.SeeingOff,
            () => RoleTypes.Crewmate,
            CustomRoleTypes.Crewmate,
            35600,
            null,
            "見送り人",
            "#883fd1"
        );
    public SeeingOff(PlayerControl player)
    : base(
        RoleInfo,
        player
    )
    {
    }
    //sendingに記載
}