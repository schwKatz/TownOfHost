using System.Collections.Generic;
using AmongUs.GameOptions;
using Hazel;
using MS.Internal.Xml.XPath;
using TownOfHostY.Roles.Core;
using TownOfHostY.Roles.Core.Interfaces;
using TownOfHostY.Roles.Crewmate;
using UnityEngine;
using static TownOfHostY.Roles.Impostor.GodfatherAndJanitor;

namespace TownOfHostY.Roles.Impostor;
public sealed class Godfather : RoleBase, IImpostor
{
    public static readonly SimpleRoleInfo RoleInfo =
        SimpleRoleInfo.Create(
            typeof(Godfather),
            player => new Godfather(player),
            CustomRoles.Godfather,
            () => RoleTypes.Impostor,
            CustomRoleTypes.Impostor,
            (int)Options.offsetId.UnitImp + 100,//使用しない
            null,
            "ゴッドファーザー"
        );
    public Godfather(PlayerControl player)
    : base(
        RoleInfo,
        player
    )
    {
        GodfatherKillCooldown = OptionGodfatherKillCooldown.GetFloat();
        LockDistance = OptionGodfatherLockDistance.GetFloat();
    }
    private static float GodfatherKillCooldown;
    private static float LockDistance;

    public override void Add()
    {
        godfather = Player;
        JanitorTarget.Clear();
    }
    public float CalculateKillCooldown() => GodfatherKillCooldown;

    public void OnCheckMurderAsKiller(MurderInfo info)
    {
        var (killer, target) = info.AttemptTuple;

        // ゴッドファーザーとジャニターの距離
        var janitorDist = Vector2.Distance(killer.transform.position, janitor.transform.position);
        // 設定距離に満たない場合は通常のキルをそのまま行う
        if (janitorDist > LockDistance) return;

        /* ジャニターターゲットの設定*/
        // キルしない
        info.CanKill = false;
        // ジャニターへのキルフラッシュ通知
        janitor.KillFlash();

        // ジャニターターゲットの追加
        JanitorTarget.Add(target.PlayerId);
        // ジャニター視点の矢印表示追加
        TargetArrow.Add(janitor.PlayerId, target.PlayerId);
        Utils.NotifyRoles(SpecifySeer: janitor);

        // 自身のキルクールリセット
        killer.SetKillCooldown();
    }

    public override void OnReportDeadBody(PlayerControl _, GameData.PlayerInfo __)
    {
        // ターゲットがいない場合は処理しない
        if (JanitorTarget.Count <= 0) return;

        // ジャニターによるキルが行われずに残ったターゲットは会議前にキル
        foreach (var targetId in JanitorTarget)
        {
            var target = Utils.GetPlayerById(targetId);
            target.SetRealKiller(Player);
            target.RpcMurderPlayer(target, true);
        }

        // ターゲットをリセット
        JanitorTarget.Clear();
    }
}