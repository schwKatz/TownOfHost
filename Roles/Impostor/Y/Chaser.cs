using AmongUs.GameOptions;

using TownOfHostY.Roles.Core;
using TownOfHostY.Roles.Core.Interfaces;

namespace TownOfHostY.Roles.Impostor;
public sealed class Chaser : RoleBase, IImpostor, ISidekickable
{
    public static readonly SimpleRoleInfo RoleInfo =
        SimpleRoleInfo.Create(
            typeof(Chaser),
            player => new Chaser(player),
            CustomRoles.Chaser,
            () => RoleTypes.Shapeshifter,
            CustomRoleTypes.Impostor,
            (int)Options.offsetId.ImpY + 1700,//仮
            SetUpOptionItem,
            "チェイサー"
        );
    public Chaser(PlayerControl player)
    : base(
        RoleInfo,
        player
    )
    {
    }
    private static void SetUpOptionItem()
    {
    }
}