using AmongUs.GameOptions;
using UnityEngine;

using TownOfHost.Roles.Core;

namespace TownOfHost.Roles.Crewmate;
public sealed class CandleLighter : RoleBase
{
    public static readonly SimpleRoleInfo RoleInfo =
        new(
            typeof(Lighter),
            player => new CandleLighter(player),
            CustomRoles.CandleLighter,
            () => RoleTypes.Crewmate,
            CustomRoleTypes.Crewmate,
            36200,
            SetupOptionItem,
            "cl",
            "#ff7f50"
        );
    public CandleLighter(PlayerControl player)
    : base(
        RoleInfo,
        player
    )
    {
        StartVision = OptionTaskStartVision.GetFloat();
        EndVisionTime = OptionTaskEndVisionTime.GetInt();
        TimeMoveMeeting = OptionTaskTimeMoveMeeting.GetBool();
    }

    private static OptionItem OptionTaskStartVision;
    private static OptionItem OptionTaskEndVisionTime;
    private static OptionItem OptionTaskTimeMoveMeeting;
    enum OptionName
    {
        CandleLighterStartVision,
        CandleLighterEndVisionTime,
        CandleLighterTimeMoveMeeting,
    }

    private static float StartVision;
    private static int EndVisionTime;
    private static bool TimeMoveMeeting;

    private static float UpdateTime;
    float ElapsedTime;

    private static void SetupOptionItem()
    {
        OptionTaskStartVision = FloatOptionItem.Create(RoleInfo, 10, OptionName.CandleLighterStartVision, new(0.5f, 5f, 0.1f), 2.0f, false)
            .SetValueFormat(OptionFormat.Multiplier);
        OptionTaskEndVisionTime = IntegerOptionItem.Create(RoleInfo, 11, OptionName.CandleLighterEndVisionTime, new(60, 1200, 60), 480, false)
            .SetValueFormat(OptionFormat.Seconds);
        OptionTaskTimeMoveMeeting = BooleanOptionItem.Create(RoleInfo, 12, OptionName.CandleLighterTimeMoveMeeting, false, false);
    }
    public override void Add()
    {
        UpdateTime = 1.0f;
        ElapsedTime = EndVisionTime;
    }
    public override void ApplyGameOptions(IGameOptions opt)
    {
        float Vision = StartVision * (ElapsedTime / EndVisionTime);
        opt.SetFloat(FloatOptionNames.CrewLightMod, Vision);
        if (Utils.IsActive(SystemTypes.Electrical))
        {
            opt.SetFloat(FloatOptionNames.CrewLightMod, Vision * 5);
        }
    }
    public override bool OnCompleteTask()
    {
        if (Player.IsAlive() && IsTaskFinished)
        {
            ElapsedTime = EndVisionTime;
        }
        return true;
    }

    public override void OnFixedUpdate(PlayerControl player)
    {
        if (!GameStates.IsInTask && !TimeMoveMeeting) return;

        UpdateTime -= Time.fixedDeltaTime;
        if (UpdateTime < 0) UpdateTime = 1.0f;

        if (ElapsedTime > 0f)
        {
            ElapsedTime -= Time.fixedDeltaTime; //時間をカウント

            if (UpdateTime == 1.0f) player.SyncSettings();
        }
    }
}