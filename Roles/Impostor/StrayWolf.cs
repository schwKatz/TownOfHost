using System.Collections.Generic;
using System.Linq;
using AmongUs.GameOptions;

using TownOfHostY.Roles.Core;
using TownOfHostY.Roles.Core.Interfaces;

namespace TownOfHostY.Roles.Impostor;
public sealed class StrayWolf : RoleBase, IImpostor
{
    public static readonly SimpleRoleInfo RoleInfo =
         SimpleRoleInfo.Create(
            typeof(StrayWolf),
            player => new StrayWolf(player),
            CustomRoles.StrayWolf,
            () => RoleTypes.Impostor,
            CustomRoleTypes.Impostor,
            20900,
            SetupOptionItem,
            "はぐれ狼",
            requireResetCam : true
        );
    public StrayWolf(PlayerControl player)
    : base(
        RoleInfo,
        player
    )
    {
        KillCooldown = OptionKillCooldown.GetFloat();
        GuardByImpostor = OptionGuardByImpostor.GetBool();
        useGuard = new();
    }
    private static OptionItem OptionKillCooldown;
    private static OptionItem OptionGuardByImpostor;
    enum OptionName
    {
        StrayWolfGuardByImpostor,
    }
    private static float KillCooldown;
    private static bool GuardByImpostor;
    List<byte> useGuard = new();

    public static void SetupOptionItem()
    {
        OptionKillCooldown = FloatOptionItem.Create(RoleInfo, 10, GeneralOption.KillCooldown, new(2.5f, 180f, 2.5f), 20f, false)
            .SetValueFormat(OptionFormat.Seconds);
        OptionGuardByImpostor = BooleanOptionItem.Create(RoleInfo, 11, OptionName.StrayWolfGuardByImpostor, true, false);
    }
    public float CalculateKillCooldown() => KillCooldown;

    public void OnCheckMurderAsKiller(MurderInfo info)
    {
        if (!Is(info.AttemptKiller) || info.IsSuicide || !info.CanKill) return;

        (var killer, var target) = info.AttemptTuple;
        if (!GuardByImpostor || useGuard.Contains(target.PlayerId)) return;   //ガードないのでそのままtrueで返す
        if (!target.Is(CustomRoleTypes.Impostor)) return;   //インポスターじゃないならそのままtrueで返す

        // ガード
        killer.RpcGuardAndKill(target);
        target.RpcGuardAndKill(target);
        NameColorManager.Add(killer.PlayerId, target.PlayerId);
        NameColorManager.Add(target.PlayerId, killer.PlayerId);

        useGuard.Add(target.PlayerId);
        Logger.Info($"{killer.GetNameWithRole()} : インポスター({target.GetNameWithRole()})からのキルガード", "StrayWolf");
        Utils.NotifyRoles();

        // 自身は斬られない
        info.DoKill = false;
        return;
    }
    public override bool OnCheckMurderAsTarget(MurderInfo info)
    {
        (var killer, var target) = info.AttemptTuple;
        // 直接キル出来る役職チェック
        if (killer.GetCustomRole().IsDirectKillRole()) return true;
        if (!GuardByImpostor || useGuard.Contains(killer.PlayerId)) return true;  //ガードないのでそのままtrueで返す
        if (!killer.Is(CustomRoleTypes.Impostor)) return true;  //インポスターじゃないならそのままtrueで返す

        // ガード
        killer.RpcGuardAndKill(target);
        target.RpcGuardAndKill(target);
        NameColorManager.Add(killer.PlayerId, target.PlayerId);
        NameColorManager.Add(target.PlayerId, killer.PlayerId);

        useGuard.Add(killer.PlayerId);
        Logger.Info($"{target.GetNameWithRole()} : インポスター({killer.GetNameWithRole()})へのキルガード", "StrayWolf");
        Utils.NotifyRoles();

        // 自身は斬られない
        info.CanKill = false;
        return true;
    }
}