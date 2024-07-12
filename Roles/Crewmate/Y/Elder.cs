using System.Linq;
using AmongUs.GameOptions;
using TownOfHostY.Roles.Core;
using UnityEngine;

namespace TownOfHostY.Roles.Crewmate;
public sealed class Elder : RoleBase
{
    public static readonly SimpleRoleInfo RoleInfo =
         SimpleRoleInfo.Create(
            typeof(Elder),
            player => new Elder(player),
            CustomRoles.Elder,
            () => RoleTypes.Crewmate,
            CustomRoleTypes.Crewmate,
            (int)Options.offsetId.CrewSpecial + 0,
            //(int)Options.offsetId.CrewY + 1900,
            SetupOptionItem,
            "長老",
            "#2B6442"//千歳緑
        );
    public Elder(PlayerControl player)
    : base(
        RoleInfo,
        player
    )
    {
        DiaInLife = OptionDiaInLife.GetBool();
        Lifetime = OptionLifetime.GetFloat();
    }

    private static OptionItem OptionDiaInLife;
    private static OptionItem OptionLifetime;
    private bool IsUseGuard;
    private static bool DiaInLife;
    private float Lifetime; // 寿命の時間を管理するプロパティ

    enum OptionName
    {
        ElderDiaInLife,
        ElderLifetime,
    }
    private static void SetupOptionItem()
    {
        OptionDiaInLife = BooleanOptionItem.Create(RoleInfo, 10, OptionName.ElderDiaInLife, false, false);
        OptionLifetime = FloatOptionItem.Create(RoleInfo, 11, OptionName.ElderLifetime, new(5f, 1800f, 5f), 900f, false, OptionDiaInLife)
                .SetValueFormat(OptionFormat.Seconds);
    }

    public override void Add()
    {
        IsUseGuard = false;
    }

    public override bool OnCheckMurderAsTarget(MurderInfo info)
    {
        (var killer, var target) = info.AttemptTuple;
        // 直接キル出来る役職チェック
        if (killer.GetCustomRole().IsDirectKillRole()) return true;

        // タスク完了時の処理/ガード未使用に関わらず反撃を行う
        if (IsTaskFinished)
        {
            // killer側も死亡する
            PlayerState.GetByPlayerId(killer.PlayerId).DeathReason = CustomDeathReason.CounterAttack; //死因：反撃
            target.RpcMurderPlayer(killer);
            return true;
        }

        // キルガードする。        
        if (!IsUseGuard)
        {
            info.CanKill = false;
            IsUseGuard = true;
            killer.RpcProtectedMurderPlayer(target);
            killer.SetKillCooldown();
            return true;
        }

        // 既にガードを使用している場合はキルされる
        // クルー陣営はみんなクルーメイトになる
        ChangeRole();
        return true;
    }
    public override void OnFixedUpdate(PlayerControl player)
    {
        // 老衰設定でない、または長老が死んでいる時は関係ない
        if (!DiaInLife || !Player.IsAlive()) return;

        // 寿命のカウントダウン
        Lifetime -= Time.fixedDeltaTime;

        // 寿命が尽きたかどうかをチェック
        if (Lifetime <= 0f)
        {
            // プレイヤーを死亡させる
            MyState.DeathReason = CustomDeathReason.Senility; //死因：老衰
            Player.RpcMurderPlayer(Player);
            // タスクが終わっていない場合、クルーメイトにする
            if (!IsTaskFinished) {
                ChangeRole();
            }
        }
    }
    public void ChangeRole()
    {
        var crewPlayers = Main.AllAlivePlayerControls.Where(player => player.Is(CustomRoleTypes.Crewmate));
        foreach (var player in crewPlayers)
        {
            // クルーメイトに変更
            player.RpcSetCustomRole(CustomRoles.Crewmate);
        }
        // 役職変更を通知
        Utils.NotifyRoles(ForceLoop: true);
        Utils.MarkEveryoneDirtySettings();
    }
}