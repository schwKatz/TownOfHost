using AmongUs.GameOptions;

using TownOfHostY.Roles.Core;

namespace TownOfHostY.Roles.Crewmate;
public sealed class NormalTracker : RoleBase
{
    public static readonly SimpleRoleInfo RoleInfo =
         SimpleRoleInfo.Create(
            typeof(NormalTracker),
            player => new NormalTracker(player),
            CustomRoles.NormalTracker,
            () => RoleTypes.Tracker,
            CustomRoleTypes.Crewmate,
            (int)Options.offsetId.CrewDefault + 200,
            SetupOptionItem,
            "トラッカー",
            "#8cffff"
        );
    public NormalTracker(PlayerControl player)
    : base(
        RoleInfo,
        player
    )
    {
        trackerCooldown = OptionTrackerCooldown.GetFloat();
        trackerDuration = OptionTrackerDuration.GetFloat();
        trackerDelay = OptionTrackerDelay.GetFloat();
    }

    private static OptionItem OptionTrackerCooldown;
    private static OptionItem OptionTrackerDuration;
    private static OptionItem OptionTrackerDelay;

    enum OptionName
    {
        TrackerCooldown,
        TrackerDuration,
        TrackerDelay,
    }
    public static float trackerCooldown;
    public static float trackerDuration;
    public static float trackerDelay;
    private static void SetupOptionItem()
    {
        OptionTrackerCooldown = FloatOptionItem.Create(RoleInfo, 3, OptionName.TrackerCooldown, new(10f, 120f, 5f), 15f, false)
            .SetValueFormat(OptionFormat.Seconds);
        OptionTrackerDuration = FloatOptionItem.Create(RoleInfo, 4, OptionName.TrackerDuration, new(10f, 120f, 5f), 30f, false)
            .SetValueFormat(OptionFormat.Seconds);
        OptionTrackerDelay = FloatOptionItem.Create(RoleInfo, 5, OptionName.TrackerDelay, new(0f, 3f, 1f), 1f, false)
            .SetValueFormat(OptionFormat.Seconds);
    }
    public override void ApplyGameOptions(IGameOptions opt)
    {
        AURoleOptions.TrackerCooldown = trackerCooldown;
        AURoleOptions.TrackerDuration = trackerDuration;
        AURoleOptions.TrackerDelay = trackerDelay;
    }
}