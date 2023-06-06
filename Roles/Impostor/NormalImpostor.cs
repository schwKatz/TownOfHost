using AmongUs.GameOptions;

using TownOfHost.Roles.Core;
using TownOfHost.Roles.Core.Interfaces;

namespace TownOfHost.Roles.Impostor;
public sealed class NormalImpostor : RoleBase, IImpostor
{
    public static readonly SimpleRoleInfo RoleInfo =
        new(
            typeof(NormalImpostor),
            player => new NormalImpostor(player),
            CustomRoles.NormalImpostor,
            () => RoleTypes.Impostor,
            CustomRoleTypes.Impostor,
            990,
            null,
            "en"
        );
    public NormalImpostor(PlayerControl player)
    : base(
        RoleInfo,
        player
    )
    {
    }
}