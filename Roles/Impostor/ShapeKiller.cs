using AmongUs.GameOptions;

using TownOfHost.Roles.Core;
using TownOfHost.Roles.Core.Interfaces;

namespace TownOfHost.Roles.Impostor;
public sealed class ShapeKiller : RoleBase, IImpostor
{
    public static readonly SimpleRoleInfo RoleInfo =
        SimpleRoleInfo.Create(
            typeof(ShapeKiller),
            player => new ShapeKiller(player),
            CustomRoles.ShapeKiller,
            () => RoleTypes.Shapeshifter,
            CustomRoleTypes.Impostor,
            3800,
            SetUpOptionItem,
            "shk"
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

    public static PlayerControl ShapeTarget = null;

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
    public override bool OnReportDeadBody(PlayerControl reporter, GameData.PlayerInfo target)
    {
        if (target == null) return true;
        if (reporter == null || reporter.PlayerId != Player.PlayerId) return true;
        if (reporter.PlayerId == target.PlayerId) return true;

        if (ShapeTarget != null && (CanDeadReport || (!ShapeTarget.Data.IsDead && !ShapeTarget.Data.Disconnected)))
        {
            RPC.ReportDeadBodyForced(ShapeTarget, target);
            Logger.Info($"ShapeKillerの偽装通報 player: {ShapeTarget?.name}, target: {target?.PlayerName}", "ShepeKillerReport");
            return false;
        }

        return true;
    }
}