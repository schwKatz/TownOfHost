using System.Linq;
using UnityEngine;
using AmongUs.GameOptions;

using TownOfHostY.Roles.Core;

namespace TownOfHostY.Roles.Neutral;

public sealed class God : RoleBase
{
    public static readonly SimpleRoleInfo RoleInfo =
        SimpleRoleInfo.Create(
            typeof(God),
            player => new God(player),
            CustomRoles.God,
            () => RoleTypes.Crewmate,
            CustomRoleTypes.Neutral,
            60800,
            SetupOptionItem,
            "神",
            "#ffff00"
        );
    public God(PlayerControl player)
    : base(
        RoleInfo,
        player,
        () => HasTask.ForRecompute
    )
    {
        taskCompleteToWin = OptionTaskCompleteToWin.GetBool();
    }
    private static OptionItem OptionTaskCompleteToWin;
    private static Options.OverrideTasksData Tasks;
    enum OptionName
    {
        GodTaskCompleteToWin,
        GodViewVoteFor,
    }
    private static bool taskCompleteToWin;
    public static void SetupOptionItem()
    {
        OptionTaskCompleteToWin = BooleanOptionItem.Create(RoleInfo, 10, OptionName.GodTaskCompleteToWin, true, false);
        Tasks = Options.OverrideTasksData.Create(RoleInfo, 20);
    }
}
