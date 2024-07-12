using System.Collections.Generic;
using AmongUs.GameOptions;
using TownOfHostY.Roles.Core;

namespace TownOfHostY.Roles.Impostor;
public sealed class GodfatherAndJanitor : RoleBase
{
    public static readonly SimpleRoleInfo RoleInfo =
        SimpleRoleInfo.Create(
            typeof(GodfatherAndJanitor),
            player => new GodfatherAndJanitor(player),
            CustomRoles.GodfatherAndJanitor,
            () => RoleTypes.Impostor,
            CustomRoleTypes.Unit,
            //(int)Options.offsetId.UnitImp + 100,
            (int)Options.offsetId.UnitSpecial + 0,
            SetupOptionItem,
            "ゴッドファーザー&ジャニター",
            "#ffffff",
            tab: TabGroup.UnitRoles,
            assignInfo: new RoleAssignInfo(CustomRoles.GodfatherAndJanitor, CustomRoleTypes.Impostor)
            {
                AssignCountRule = new(1, 1, 1),
                AssignUnitRoles = new CustomRoles[2] { CustomRoles.Godfather, CustomRoles.Janitor }
            }
        );
    public GodfatherAndJanitor(PlayerControl player)
    : base(
        RoleInfo,
        player
    )
    {
    }
    // ジャニターターゲット格納
    public static HashSet<byte> JanitorTarget = new(15);
    // ゴッドファーザー
    public static PlayerControl godfather = null;
    // ジャニター
    public static PlayerControl janitor = null;

    public static OptionItem OptionGodfatherKillCooldown;
    public static OptionItem OptionGodfatherLockDistance;
    public static OptionItem OptionJanitorCleanCooldown;
    public static OptionItem OptionJanitorSeeSelectedTiming;
    public static OptionItem OptionJanitorTrackTarget;
    public static OptionItem OptionJanitorTrackGodfather;
    public static OptionItem OptionAfterGotfatherDeadIsAlive;
    public static OptionItem OptionJanitorCanKill;
    public static OptionItem OptionJanitorKillCooldown;
    enum OptionName
    {
        GodfatherKillCooldown,
        GodfatherLockDistance,
        JanitorCleanCooldown,
        JanitorSeeSelectedTiming,
        JanitorTrackTarget,
        JanitorTrackGodfather,
        JanitorAfterGotfatherDeadIsAlive,
        JanitorCanKill,
        JanitorKillCooldown,
    }
    private static void SetupOptionItem()
    {
        OptionGodfatherKillCooldown = FloatOptionItem.Create(RoleInfo, 10, OptionName.GodfatherKillCooldown, new(5.0f, 180f, 2.5f), 30f, false)
            .SetValueFormat(OptionFormat.Seconds);
        OptionGodfatherLockDistance = FloatOptionItem.Create(RoleInfo, 11, OptionName.GodfatherLockDistance, new(1.0f, 20f, 0.5f), 10f, false)
            .SetValueFormat(OptionFormat.Multiplier);
        OptionJanitorCleanCooldown = FloatOptionItem.Create(RoleInfo, 12, OptionName.JanitorCleanCooldown, new(5.0f, 180f, 2.5f), 30f, false)
            .SetValueFormat(OptionFormat.Seconds);
        OptionJanitorSeeSelectedTiming = BooleanOptionItem.Create(RoleInfo, 13, OptionName.JanitorSeeSelectedTiming, true, false);
        OptionJanitorTrackTarget = BooleanOptionItem.Create(RoleInfo, 14, OptionName.JanitorTrackTarget, true, false);
        OptionJanitorTrackGodfather = BooleanOptionItem.Create(RoleInfo, 15, OptionName.JanitorTrackGodfather, true, false);
        OptionAfterGotfatherDeadIsAlive = BooleanOptionItem.Create(RoleInfo, 16, OptionName.JanitorAfterGotfatherDeadIsAlive, true, false);
        OptionJanitorCanKill = BooleanOptionItem.Create(RoleInfo, 17, OptionName.JanitorCanKill, true, false, OptionAfterGotfatherDeadIsAlive);
        OptionJanitorKillCooldown = FloatOptionItem.Create(RoleInfo, 18, OptionName.JanitorKillCooldown, new(0f, 180f, 2.5f), 30f, false, OptionJanitorCanKill)
            .SetValueFormat(OptionFormat.Seconds);
    }
}