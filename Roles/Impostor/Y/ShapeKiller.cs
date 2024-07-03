using AmongUs.GameOptions;

using TownOfHostY.Roles.Core;
using TownOfHostY.Roles.Core.Interfaces;

namespace TownOfHostY.Roles.Impostor;
public sealed class ShapeKiller : RoleBase, IImpostor, ISidekickable
{
    public static readonly SimpleRoleInfo RoleInfo =
        SimpleRoleInfo.Create(
            typeof(ShapeKiller),
            player => new ShapeKiller(player),
            CustomRoles.ShapeKiller,
            () => RoleTypes.Shapeshifter,
            CustomRoleTypes.Impostor,
            (int)Options.offsetId.ImpY + 800,
            SetUpOptionItem,
            "シェイプキラー"
        );
    public ShapeKiller(PlayerControl player)
    : base(
        RoleInfo,
        player
    )
    {
        CanDeadReport = OptionCanDeadReport.GetBool();

        ShapeTarget = null;
    }
    private static OptionItem OptionCanDeadReport;
    enum OptionName
    {
        ShapeKillerCanDeadReport
    }
    private static bool CanDeadReport;

    public PlayerControl ShapeTarget = null;

    private static void SetUpOptionItem()
    {
        OptionCanDeadReport = BooleanOptionItem.Create(RoleInfo, 10, OptionName.ShapeKillerCanDeadReport, true, false);
    }
    public override void OnShapeshift(PlayerControl target)
    {
        if (!AmongUsClient.Instance.AmHost) return;

        var shapeshifting = !Is(target);
        if (!shapeshifting) ShapeTarget = null;
        else ShapeTarget = target;
        Logger.Info($"{Player.GetNameWithRole()}のターゲットを {target?.GetNameWithRole()} に設定", "ShepeKillerTarget");
    }
    public static bool DummyReportDeadBody(PlayerControl reporter, NetworkedPlayerInfo target)
    {
        if (target == null) return false;
        if (reporter == null || !reporter.Is(CustomRoles.ShapeKiller)) return false;
        if (reporter.PlayerId == target.PlayerId) return false;

        var shapeKiller = (ShapeKiller)reporter.GetRoleClass();
        if (shapeKiller.ShapeTarget != null && (CanDeadReport || shapeKiller.ShapeTarget.IsAlive()))
        {
            RPC.ReportDeadBodyForced(shapeKiller.ShapeTarget, target);
            Logger.Info($"ShapeKillerの偽装通報 player: {shapeKiller.ShapeTarget?.name}, target: {target?.PlayerName}", "ShepeKillerReport");
            return true;
        }

        return false;
    }
}