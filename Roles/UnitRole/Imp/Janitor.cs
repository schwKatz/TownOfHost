using System.Text;
using AmongUs.GameOptions;
using UnityEngine;

using TownOfHostY.Roles.Core;
using TownOfHostY.Roles.Core.Interfaces;
using static TownOfHostY.Roles.Impostor.GodfatherAndJanitor;

namespace TownOfHostY.Roles.Impostor;
public sealed class Janitor : RoleBase, IImpostor
{
    public static readonly SimpleRoleInfo RoleInfo =
        SimpleRoleInfo.Create(
            typeof(Janitor),
            player => new Janitor(player),
            CustomRoles.Janitor,
            () => RoleTypes.Impostor,
            CustomRoleTypes.Impostor,
            (int)Options.offsetId.UnitImp + 100,//使用しない
            null,
            "ジャニター"
        );
    public Janitor(PlayerControl player)
    : base(
        RoleInfo,
        player
    )
    {
        CleanCooldown = OptionJanitorCleanCooldown.GetFloat();
        LastCanKill = OptionJanitorLastCanKill.GetBool();
        KillCooldown = OptionJanitorKillCooldown.GetFloat();
    }
    private static bool canNormalKill;
    private static float CleanCooldown;
    private static bool LastCanKill;
    private static float KillCooldown;

    public override void Add()
    {
        janitor = Player;
        canNormalKill = false;
    }

    public float CalculateKillCooldown() => canNormalKill ? KillCooldown : CleanCooldown;
    public void OnCheckMurderAsKiller(MurderInfo info)
    {
        // ゴッドファーザー死亡後、設定により通常キル可
        if (canNormalKill) return;

        // Janitorは必ずキルを防ぐ
        info.DoKill = false;
        // ターゲットがいない場合は処理しない
        if (JanitorTarget.Count <= 0) return;
        
        var (killer, target) = info.AttemptTuple;
        // ターゲットの状態を取得
        var targetPlayerState = PlayerState.GetByPlayerId(target.PlayerId);

        /* ターゲットを死体なしで霊界転送する */
        targetPlayerState.SetDead();
        target.RpcExileV2();
        targetPlayerState.DeathReason = CustomDeathReason.Clean;

        // 掃除したプレイヤーはリストから削除
        JanitorTarget.Remove(target.PlayerId);

        // 自身のキルクールリセット
        killer.SetKillCooldown();
    }
    public override string GetSuffix(PlayerControl seer, PlayerControl seen = null, bool isForMeeting = false)
    {
        seen ??= seer;
        //seerおよびseenが自分である場合以外は関係なし
        // ターゲットがいるかつタスクターン中に矢印を表示
        if (!Is(seer) || !Is(seen) || isForMeeting || JanitorTarget.Count <= 0)
            return string.Empty;

        StringBuilder sb = new();
        foreach (var targetId in JanitorTarget)
        {
            // 矢印の取得
            sb.Append(TargetArrow.GetArrows(Player, targetId));
        }

        // 文字が何もない場合は空白を返す
        if (sb.Length <= 0) return string.Empty;
        // 矢印を色付けで返す
        return sb.ToString().Color(Palette.ImpostorRed);
    }

    public static void KillSuicide(byte deadTargetId)
    {
        var target = Utils.GetPlayerById(deadTargetId);
        if (target != godfather) return;
        // ゴッドファーザー死亡後、キルできる設定の時
        if (LastCanKill)
        {
            // ノーマルキルできる設定への切り替え
            canNormalKill = true;
            janitor.ResetKillCooldown();
            return;
        }

        /* 後追い処理 */
        godfather.RpcMurderPlayer(janitor);
        godfather.SetRealKiller(janitor);
        PlayerState.GetByPlayerId(janitor.PlayerId).DeathReason = CustomDeathReason.FollowingSuicide;
        Logger.Info($"{janitor.GetNameWithRole()}の後追い:{godfather.GetNameWithRole()}", "KillFollowingSuicide");
    }
    public static void VoteSuicide(byte deadTargetId)
    {
        var target = Utils.GetPlayerById(deadTargetId);
        if (target != godfather) return;
        // ゴッドファーザー死亡後、キルできる設定の時
        if (LastCanKill)
        {
            // ノーマルキルできる設定への切り替え
            canNormalKill = true;
            janitor.ResetKillCooldown();
            return;
        }

        /* 後追い処理 */
        MeetingHudPatch.TryAddAfterMeetingDeathPlayers(CustomDeathReason.FollowingSuicide, janitor.PlayerId);
        godfather.SetRealKiller(janitor);
        Logger.Info($"{janitor.GetNameWithRole()}のLover後追い:{godfather.GetNameWithRole()}", "VoteFollowingSuicide");
    }

    public override void OverrideDisplayRoleNameAsSeer(PlayerControl seen, bool isMeeting, ref bool enabled, ref Color roleColor, ref string roleText)
    {
        // 相方の役職名を表示させる
        if (seen.Is(CustomRoles.Godfather)) enabled = true;
    }
}