using System.Collections.Generic;
using System.Linq;
using AmongUs.GameOptions;
using Hazel;

using TownOfHostY.Roles.Core;
using TownOfHostY.Roles.Core.Interfaces;
using static TownOfHostY.Utils;

namespace TownOfHostY.Roles.Impostor;
public sealed class CharisMastar : RoleBase, IImpostor, ISidekickable
{
    public static readonly SimpleRoleInfo RoleInfo =
        SimpleRoleInfo.Create(
            typeof(CharisMastar),
            player => new CharisMastar(player),
            CustomRoles.CharisMastar,
            () => RoleTypes.Phantom,
            CustomRoleTypes.Impostor,
            (int)Options.offsetId.ImpY + 2000,//仮
            SetUpOptionItem,
            "カリスマスター"
        );
    public CharisMastar(PlayerControl player)
    : base(
        RoleInfo,
        player
    )
    {
        KillCooldown = OptionKillCooldown.GetFloat();
        GatherCount = OptionGatherCount.GetInt();
        GatherCooldown = OptionGatherCooldown.GetFloat();
        NotGatherPlayerKill = OptionNotGatherPlayerKill.GetBool();
        GathersMode = (GatherMode)OptionGatherMode.GetValue();
    }
    public enum GatherMode
    {
        CanChoose,
        EveryoneGather,
    };
    enum OptionName
    {
        CharisMastarGatherCount,
        CharisMastarGatherCooldown,
        CharisMastarNotGatherPlayerKill,
        CharisMastarGatherMode,
    }
    private static float KillCooldown;
    private static float GatherCooldown;
    public static bool NotGatherPlayerKill;
    private static OptionItem OptionKillCooldown;
    private static OptionItem OptionGatherCount;
    private static OptionItem OptionGatherCooldown;
    private static OptionItem OptionNotGatherPlayerKill;
    public static StringOptionItem OptionGatherMode;
    public static GatherMode GathersMode;
    int GatherCount;
    private static void SetUpOptionItem()
    {
        OptionKillCooldown = FloatOptionItem.Create(RoleInfo, 10, GeneralOption.KillCooldown, new(2.5f, 180f, 2.5f), 30f, false)
                .SetValueFormat(OptionFormat.Seconds);
        OptionGatherCount = IntegerOptionItem.Create(RoleInfo, 11, OptionName.CharisMastarGatherCount, new(1, 10, 1), 3, false)
            .SetValueFormat(OptionFormat.Pieces);
        OptionGatherCooldown = FloatOptionItem.Create(RoleInfo, 12, OptionName.CharisMastarGatherCooldown, new(5f, 900f, 5f), 30f, false)
            .SetValueFormat(OptionFormat.Seconds);
        OptionNotGatherPlayerKill = BooleanOptionItem.Create(RoleInfo, 13, OptionName.CharisMastarNotGatherPlayerKill, true, false);
        OptionGatherMode = StringOptionItem.Create(RoleInfo, 14, OptionName.CharisMastarGatherMode, GatherModeText, 2, false);
    }
}