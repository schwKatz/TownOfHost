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
            (int)Options.offsetId.UnitImp + 100,
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
    public static OptionItem OptionJanitorLastCanKill;
    public static OptionItem OptionJanitorKillCooldown;
    enum OptionName
    {
        GodfatherKillCooldown,
        GodfatherLockDistance,
        JanitorCleanCooldown,
        JanitorLastCanKill,
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
        OptionJanitorLastCanKill = BooleanOptionItem.Create(RoleInfo, 13, OptionName.JanitorLastCanKill, false, false);
        OptionJanitorKillCooldown = FloatOptionItem.Create(RoleInfo, 14, OptionName.GodfatherKillCooldown, new(5.0f, 180f, 2.5f), 30f, false).SetParent(OptionJanitorLastCanKill)
            .SetValueFormat(OptionFormat.Seconds);
    }
}