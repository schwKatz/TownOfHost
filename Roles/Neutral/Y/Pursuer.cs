using System.Linq;
using AmongUs.GameOptions;
using UnityEngine;

using TownOfHostY.Roles.Core;
using TownOfHostY.Roles.Core.Interfaces;

namespace TownOfHostY.Roles.Neutral;
public sealed class Pursuer : RoleBase, IAdditionalWinner
{
    public static readonly SimpleRoleInfo RoleInfo =
        SimpleRoleInfo.Create(
            typeof(Pursuer),
            player => new Pursuer(player),
            CustomRoles.Pursuer,
            () => RoleTypes.Crewmate,
            CustomRoleTypes.Neutral,
            (int)Options.offsetId.NeuY + 590,
            null,
            "追跡者",
            "#daa520",
            introSound: () => GetIntroSound(RoleTypes.Impostor)
        );
    public Pursuer(PlayerControl player)
    : base(
        RoleInfo,
        player
    )
    {
        hasImpostorVision = Lawyer.HasImpostorVision;
        guardCount = Lawyer.PursuerGuardNum;
    }
    private static bool hasImpostorVision;
    private int guardCount = 0;

    public override void ApplyGameOptions(IGameOptions opt) => opt.SetVision(hasImpostorVision);

    private bool CanUseGuard() => Player.IsAlive() && guardCount > 0;
    public override bool OnCheckMurderAsTarget(MurderInfo info)
    {
        (var killer, var target) = info.AttemptTuple;

        if (guardCount <= 0) return true;

        // 直接キル出来る役職チェック
        if (killer.GetCustomRole().IsDirectKillRole()) return true;

        info.CanKill = false;
        guardCount--;

        killer.RpcProtectedMurderPlayer(target);
        target.RpcProtectedMurderPlayer(target);
        killer.SetKillCooldown();

        Utils.NotifyRoles(SpecifySeer: target);

        return true;
    }
    public override string GetProgressText(bool comms = false)
    {
        return Utils.ColorString(CanUseGuard() ? Color.yellow : Color.gray, $"〔{guardCount}〕");
    }
    public bool CheckWin(ref CustomRoles winnerRole)
    {
        return Player.IsAlive();
    }

}