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
        trackerDelay = OptionTrackerDelay.GetFloat();
        trackerDuration = OptionTrackerDuration.GetFloat();
    }

    private static OptionItem OptionTrackerCooldown;
    private static OptionItem OptionTrackerDelay;
    private static OptionItem OptionTrackerDuration;

    enum OptionName
    {
        TrackerCooldown,
        TrackerDelay,
        TrackerDuration,
    }
    public static float trackerCooldown;
    public static float trackerDelay;
    public static float trackerDuration;
    private static void SetupOptionItem()
    {
        OptionTrackerCooldown = FloatOptionItem.Create(RoleInfo, 3, OptionName.TrackerCooldown, new(10f, 120f, 5f), 15f, false)
            .SetValueFormat(OptionFormat.Seconds);
        OptionTrackerDelay = FloatOptionItem.Create(RoleInfo, 4, OptionName.TrackerDelay, new(0f, 3f, 1f), 1f, false)
            .SetValueFormat(OptionFormat.Seconds);
        OptionTrackerDuration = FloatOptionItem.Create(RoleInfo, 5, OptionName.TrackerDuration, new(10f, 120f, 5f), 30f, false)
            .SetValueFormat(OptionFormat.Seconds);
    }
    public override void ApplyGameOptions(IGameOptions opt)
    {
        AURoleOptions.TrackerCooldown = trackerCooldown;
        AURoleOptions.TrackerDelay = trackerDelay;
        AURoleOptions.TrackerDuration = trackerDuration;
    }
}