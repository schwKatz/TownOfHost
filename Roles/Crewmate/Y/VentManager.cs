using System.Linq;
using System.Collections.Generic;

using AmongUs.GameOptions;

using TownOfHostY.Roles.Core;

namespace TownOfHostY.Roles.Crewmate;
public sealed class VentManager : RoleBase
{
    public static readonly SimpleRoleInfo RoleInfo =
         SimpleRoleInfo.Create(
            typeof(VentManager),
            player => new VentManager(player),
            CustomRoles.VentManager,
            () => RoleTypes.Crewmate,
            CustomRoleTypes.Crewmate,
            (int)Options.offsetId.CrewY + 1500,
            SetupOptionItem,
            "ベントマネージャー",
            "#00ffff"
        );
    public VentManager(PlayerControl player)
    : base(
        RoleInfo,
        player
    )
    {
    }

    enum OptionName
    {
        TaskCount,
    }

    public static OptionItem TaskCount;
    private static void SetupOptionItem()
    {
        TaskCount = IntegerOptionItem.Create(RoleInfo, 10, OptionName.TaskCount, new(1, 30, 1), 15, false).SetValueFormat(OptionFormat.Pieces);
    }
}