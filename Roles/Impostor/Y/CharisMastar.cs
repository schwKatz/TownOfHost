using System.Collections.Generic;
using System.Linq;
using AmongUs.GameOptions;
using Hazel;

using TownOfHostY.Roles.Core;
using TownOfHostY.Roles.Core.Interfaces;
using static TownOfHostY.Utils;

namespace TownOfHostY.Roles.Impostor;
public sealed class CharisMastar : RoleBase, IImpostor, ISidekickable
{
    public static readonly SimpleRoleInfo RoleInfo =
        SimpleRoleInfo.Create(
            typeof(CharisMastar),
            player => new CharisMastar(player),
            CustomRoles.CharisMastar,
            () => RoleTypes.Phantom,
            CustomRoleTypes.Impostor,
            (int)Options.offsetId.ImpY + 2000,//仮
            SetUpOptionItem,
            "カリスマスター"
        );
    public CharisMastar(PlayerControl player)
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