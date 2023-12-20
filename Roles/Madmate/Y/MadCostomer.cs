using AmongUs.GameOptions;

using TownOfHostY.Roles.Core;
using TownOfHostY.Roles.Core.Interfaces;

namespace TownOfHostY.Roles.Madmate;
public sealed class MadCostomer : RoleBase, IKiller
{
    public static readonly SimpleRoleInfo RoleInfo =
         SimpleRoleInfo.Create(
            typeof(MadCostomer),
            player => new MadCostomer(player),
            CustomRoles.MadCostomer,
            () => RoleTypes.Impostor,
            CustomRoleTypes.Madmate,
            (int)Options.offsetId.MadSpecial + 0,
            //(int)Options.offsetId.MadY + 500,
            SetupOptionItem,
            "MadCostomer",
            isDesyncImpostor: true
        );
    public MadCostomer(PlayerControl player)
    : base(
        RoleInfo,
        player,
        () => HasTask.False
    )
    {
        CanVent = OptionCanVent.GetBool();
        CanSabotage = OptionCanSabotage.GetBool();

        TaskMaxCount = OptionTaskMaxCount.GetInt();
    }

    private static OptionItem OptionCanVent;
    private static OptionItem OptionCanSabotage;
    private static OptionItem OptionTaskMaxCount;
    private static bool CanVent;
    private static bool CanSabotage;
    private static int TaskMaxCount;
    enum OptionName
    {
        VentEnterTaskMaxCount
    }

    private static void SetupOptionItem()
    {
        OptionTaskMaxCount = IntegerOptionItem.Create(RoleInfo, 10, OptionName.VentEnterTaskMaxCount, new(1, 30, 1), 3, false)
            .SetValueFormat(OptionFormat.Pieces);
        OptionCanVent = BooleanOptionItem.Create(RoleInfo, 11, GeneralOption.CanVent, true, false);
        OptionCanSabotage = BooleanOptionItem.Create(RoleInfo, 12, GeneralOption.CanUseSabotage, false, false);
        Options.SetUpAddOnOptions(RoleInfo.ConfigId + 20, RoleInfo.RoleName, RoleInfo.Tab);
    }
    public bool CanKill => false;
    public float CalculateKillCooldown() => 0f;
    public bool CanUseSabotageButton() => CanSabotage;
    public override void ApplyGameOptions(IGameOptions opt)
    {
        opt.SetVision(Options.AddOnRoleOptions[(CustomRoles.MadCostomer, CustomRoles.AddLight)].GetBool());
    }

    public override void Add()
    {
        VentEnterTask.Add(Player, TaskMaxCount, useVent: CanVent);
    }
}