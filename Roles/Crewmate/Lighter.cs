using AmongUs.GameOptions;
using TownOfHost.Roles.Core;

namespace TownOfHost.Roles.Crewmate;
public sealed class Lighter : RoleBase
{
    public static readonly SimpleRoleInfo RoleInfo =
        new(
            typeof(Lighter),
            player => new Lighter(player),
            CustomRoles.Lighter,
            () => RoleTypes.Crewmate,
            CustomRoleTypes.Crewmate,
            20100,
            SetupOptionItem,
            "ライター",
            "#eee5be"
        );
    public Lighter(PlayerControl player)
    : base(
        RoleInfo,
        player
    )
    {
        TaskCompletedVision = OptionTaskCompletedVision.GetFloat();
        TaskCompletedDisableLightOut = OptionTaskCompletedDisableLightOut.GetBool();
        TaskTrigger = OptionTaskTrigger.GetInt();
    }

    private static OptionItem OptionTaskCompletedVision;
    private static OptionItem OptionTaskCompletedDisableLightOut;
    private static OptionItem OptionTaskTrigger;
    enum OptionName
    {
        LighterTaskCompletedVision,
        LighterTaskCompletedDisableLightOut,
        SpeedBoosterTaskTrigger
    }

    private static float TaskCompletedVision;
    private static bool TaskCompletedDisableLightOut;
    private static int TaskTrigger;

    private static void SetupOptionItem()
    {
        OptionTaskTrigger = IntegerOptionItem.Create(RoleInfo, 12, OptionName.SpeedBoosterTaskTrigger, new(1, 20, 1), 5, false)
            .SetValueFormat(OptionFormat.Pieces);
        OptionTaskCompletedVision = FloatOptionItem.Create(RoleInfo, 10, OptionName.LighterTaskCompletedVision, new(0f, 5f, 0.25f), 2f, false)
            .SetValueFormat(OptionFormat.Multiplier);
        OptionTaskCompletedDisableLightOut = BooleanOptionItem.Create(RoleInfo, 11, OptionName.LighterTaskCompletedDisableLightOut, true, false);
    }

    public override void ApplyGameOptions(IGameOptions opt)
    {
        if (!IsTaskFinished || MyTaskState.CompletedTasksCount < TaskTrigger) return;

        var crewLightMod = FloatOptionNames.CrewLightMod;

        opt.SetFloat(crewLightMod, TaskCompletedVision);
        if (TaskCompletedDisableLightOut && Utils.IsActive(SystemTypes.Electrical))
        {
            opt.SetFloat(crewLightMod, TaskCompletedVision * 5);
        }
    }
    public override bool OnCompleteTask()
    {
        if (IsTaskFinished || MyTaskState.CompletedTasksCount >= TaskTrigger)
        {
            Player.MarkDirtySettings();
        }

        return true;
    }
}