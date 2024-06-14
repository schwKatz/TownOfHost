using AmongUs.GameOptions;

using TownOfHostY.Roles.Core;
using TownOfHostY.Roles.Core.Interfaces;

namespace TownOfHostY.Roles.Impostor;
public sealed class Chaser : RoleBase, IImpostor, ISidekickable
{
    public static readonly SimpleRoleInfo RoleInfo =
        SimpleRoleInfo.Create(
            typeof(Chaser),
            player => new Chaser(player),
            CustomRoles.Chaser,
            () => RoleTypes.Shapeshifter,
            CustomRoleTypes.Impostor,
            (int)Options.offsetId.ImpY + 1700,//仮
            SetUpOptionItem,
            "チェイサー"
        );
    public Chaser(PlayerControl player)
    : base(
        RoleInfo,
        player
    )
    {
    }
    private static OptionItem OptionKillCooldown;
    private static OptionItem OptionChaseCooldown;
    private static OptionItem OptionReturnPosition;
    private static OptionItem OptionPositionTime;
    enum OptionName
    {
        ChaserChaseCooldown,
        ChaserReturnPosition,
        ChaserPositionTime,
    }
    private static void SetUpOptionItem()
    {
        OptionKillCooldown = FloatOptionItem.Create(RoleInfo, 10, GeneralOption.KillCooldown, new(2.5f, 180f, 2.5f), 30f, false)
                .SetValueFormat(OptionFormat.Seconds);
        OptionChaseCooldown = FloatOptionItem.Create(RoleInfo, 11, OptionName.ChaserChaseCooldown, new(1f, 180f, 1f), 30f, false)
                .SetValueFormat(OptionFormat.Seconds);
        OptionReturnPosition = BooleanOptionItem.Create(RoleInfo, 12, OptionName.ChaserReturnPosition, false, false);
        OptionPositionTime = FloatOptionItem.Create(RoleInfo, 13, OptionName.ChaserPositionTime, new(1f, 99f, 1f), 10f, false, OptionReturnPosition)
            .SetValueFormat(OptionFormat.Seconds);
    }
}