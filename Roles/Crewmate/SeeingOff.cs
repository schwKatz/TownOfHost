using AmongUs.GameOptions;
using TownOfHostY.Roles.Core;

using static TownOfHostY.Translator;
using static TownOfHostY.Utils;

namespace TownOfHostY.Roles.Crewmate;
public sealed class SeeingOff : RoleBase
{
    public static readonly SimpleRoleInfo RoleInfo =
         SimpleRoleInfo.Create(
            typeof(SeeingOff),
            player => new SeeingOff(player),
            CustomRoles.SeeingOff,
            () => RoleTypes.Crewmate,
            CustomRoleTypes.Crewmate,
            40500,
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