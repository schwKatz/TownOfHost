using AmongUs.GameOptions;

using TownOfHost.Roles.Core;
using TownOfHost.Roles.Core.Interfaces;

namespace TownOfHost.Roles.Neutral;

public sealed class PlatonicLover : RoleBase, IKiller
{
    public static readonly SimpleRoleInfo RoleInfo =
         SimpleRoleInfo.Create(
            typeof(PlatonicLover),
            player => new PlatonicLover(player),
            CustomRoles.PlatonicLover,
            () => RoleTypes.Impostor,
            CustomRoleTypes.Neutral,
            60400,
            SetupOptionItem,
            "純愛者",
            "#ff6be4"
        );
    public PlatonicLover(PlayerControl player)
    : base(
        RoleInfo,
        player
    )
    {
        AddWin = OptionAddWin.GetBool();
    }
    public static OptionItem OptionAddWin;
    enum OptionName
    {
        LoversAddWin,
    }
    public bool isMadeLover;
    bool AddWin;

    private static void SetupOptionItem()
    {
        OptionAddWin = BooleanOptionItem.Create(RoleInfo, 10, OptionName.LoversAddWin, false, false);
    }

    public override void Add()
    {
        var playerId = Player.PlayerId;
        isMadeLover = false;

        if (!Main.ResetCamPlayerList.Contains(playerId))
            Main.ResetCamPlayerList.Add(playerId);
    }
    public float CalculateKillCooldown() => CanUseKillButton() ? 0.1f : 0f;
    public bool CanUseKillButton() => Player.IsAlive() && !isMadeLover;
    public override bool CanSabotage(SystemTypes systemType) => false;
    public override void ApplyGameOptions(IGameOptions opt) => opt.SetVision(false);
    public void OnCheckMurderAsKiller(MurderInfo info)
    {
        if (Is(info.AttemptKiller) && !info.IsSuicide)
        {
            (var killer, var target) = info.AttemptTuple;

            isMadeLover = true;
            killer.RpcGuardAndKill(target);
            target.RpcGuardAndKill(target);
            Logger.Info($"{killer.GetNameWithRole()} : 恋人を作った", "PlatonicLover");

            Main.LoversPlayers.Clear();
            Main.isLoversDead = false;
            killer.RpcSetCustomRole(CustomRoles.Lovers);
            target.RpcSetCustomRole(CustomRoles.Lovers);
            Main.LoversPlayers.Add(killer);
            Main.LoversPlayers.Add(target);
            RPC.SyncLoversPlayers();

            Utils.NotifyRoles();
        }
    }

    public bool OverrideKillButtonText(out string text)
    {
        text = Translator.GetString("PlatonicLoverButtonText");
        return true;
    }
}
