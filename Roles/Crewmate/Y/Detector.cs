using AmongUs.GameOptions;
using MS.Internal.Xml.XPath;
using TownOfHostY.Roles.Core;
using TownOfHostY.Roles.Core.Interfaces;

namespace TownOfHostY.Roles.Crewmate;
public sealed class Detector : RoleBase, IKillFlashSeeable
{
    public static readonly SimpleRoleInfo RoleInfo =
        SimpleRoleInfo.Create(
            typeof(Detector),
            player => new Detector(player),
            CustomRoles.Detector,
            () => RoleTypes.Crewmate,
            CustomRoleTypes.Crewmate,
            (int)Options.offsetId.CrewY + 1400,
            SetupOptionItem,
            "ディテクター",
            "#7700cc"
        );
    public Detector(PlayerControl player)
    : base(
        RoleInfo,
        player
    )
    {
        TaskTrigger = OptionTaskTrigger.GetInt();

        IsSet = false;
    }

    private static OptionItem OptionTaskTrigger;
    enum OptionName
    {
        TaskTrigger
    }

    private static int TaskTrigger;
    bool IsSet = false;

    private static void SetupOptionItem()
    {
        OptionTaskTrigger = IntegerOptionItem.Create(RoleInfo, 12, OptionName.TaskTrigger, new(1, 20, 1), 5, false)
            .SetValueFormat(OptionFormat.Pieces);
    }

    public bool CheckKillFlash(MurderInfo info) => IsTaskFinished || MyTaskState.CompletedTasksCount >= TaskTrigger;

    public override bool OnCompleteTask()
    {
        if (IsSet || (!IsTaskFinished && MyTaskState.CompletedTasksCount < TaskTrigger))
        {
            Logger.Info($"return", "Detector");
            return true;
        }
        IsSet = true;
        // 死体への矢印が表示できるようにする
        TargetDeadArrow.AddSeer(Player.PlayerId);
        return true;
    }
}