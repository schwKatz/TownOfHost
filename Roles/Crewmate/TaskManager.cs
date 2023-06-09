using AmongUs.GameOptions;

using TownOfHost.Roles.Core;
using static TownOfHost.Utils;

namespace TownOfHost.Roles.Crewmate;
public sealed class TaskManager : RoleBase
{
    public static readonly SimpleRoleInfo RoleInfo =
        new(
            typeof(TaskManager),
            player => new TaskManager(player),
            CustomRoles.TaskManager,
            () => RoleTypes.Crewmate,
            CustomRoleTypes.Crewmate,
            35200,
            SetupOptionItem,
            "tam",
            "#80ffdd",
            introSound: () => GetIntroSound(RoleTypes.Scientist)
        );
    public TaskManager(PlayerControl player)
    : base(
        RoleInfo,
        player
    )
    {
        SeeNowtask = OptionSeeNowtask.GetBool();
    }

    private static OptionItem OptionSeeNowtask;
    enum OptionName
    {
        TaskmanagerSeeNowtask,
    }
    private static bool SeeNowtask;

    private static void SetupOptionItem()
    {
        OptionSeeNowtask = BooleanOptionItem.Create(RoleInfo, 10, OptionName.TaskmanagerSeeNowtask, false, false);
    }

    public override string GetProgressText(bool comms = false)
    {
        var nowtask = "?";
        int completetask;
        int alltask;
        (completetask, alltask) = GetTasksState();

        if ((GameStates.IsMeeting || !Player.IsAlive() || SeeNowtask)
            && !comms)
            nowtask = $"{completetask}";

        return ColorString(RoleInfo.RoleColor, $"({nowtask}/{alltask})");
    }
}