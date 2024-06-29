using System.Text;
using AmongUs.GameOptions;
using UnityEngine;

using TownOfHostY.Roles.Core;
using TownOfHostY.Roles.Core.Interfaces;
using static TownOfHostY.Roles.Impostor.GodfatherAndJanitor;
using System.Linq;

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
        TrackTarget = OptionJanitorTrackTarget.GetBool();
        TrackGodfather = OptionJanitorTrackGodfather.GetBool();
        LastCanKill = OptionJanitorLastCanKill.GetBool();
        KillCooldown = OptionJanitorKillCooldown.GetFloat();
        GFDeadMode = (AfterGotfatherDeadMode)OptionAfterGotfatherDeadMode.GetValue();
    }
    // ゴッドファーザーの死亡後、通常キルをする際にtrueにする
    private static bool canNormalKill;

    private static float CleanCooldown;
    public static bool TrackTarget;
    private static bool TrackGodfather;
    private static bool LastCanKill;
    private static float KillCooldown;

    public override void Add()
    {
        janitor = Player;
        Logger.Info($"{Player.GetNameWithRole()} : Janitor登録", "G&J");
        canNormalKill = false;

        // ジャニター視点の矢印表示追加
        if (TrackGodfather)
        {
            var god = Main.AllPlayerControls.Where(pc => pc.Is(CustomRoles.Godfather)).FirstOrDefault();
            if (god != null)
            {
                TargetArrow.Add(janitor.PlayerId, god.PlayerId);
                Logger.Info($"{Player.GetNameWithRole()} : Janitor.TargetArrowAdd", "G&J");
            }
        }
    }

    public float CalculateKillCooldown() => canNormalKill ? KillCooldown : CleanCooldown;
    public void OnCheckMurderAsKiller(MurderInfo info)
    {
        // ゴッドファーザー死亡後、設定により通常キル可
        if (canNormalKill) return;

        // Janitorは必ずキルを防ぐ
        info.DoKill = false;

        var (killer, target) = info.AttemptTuple;

        // ターゲットがいない場合、ターゲットが対象じゃない場合は処理しない
        if (JanitorTarget.Count <= 0 || !JanitorTarget.Contains(target.PlayerId)) return;

        // ターゲットの状態を取得
        var targetPlayerState = PlayerState.GetByPlayerId(target.PlayerId);

        /* ターゲットを死体なしで霊界転送する */
        targetPlayerState.SetDead();
        target.RpcExileV2();
        targetPlayerState.DeathReason = CustomDeathReason.Clean;
        Logger.Info($"{Player.GetNameWithRole()} : ターゲット({target.GetNameWithRole()})を掃除", "G&J");

        // 掃除したプレイヤーはリストから削除
        JanitorTarget.Remove(target.PlayerId);
        // ターゲットの足止め解除
        Main.AllPlayerSpeed[target.PlayerId] = Main.RealOptionsData.GetFloat(FloatOptionNames.PlayerSpeedMod);
        target.MarkDirtySettings();

        // 自身のキルクールリセット
        killer.SetKillCooldown();

        // バニラの表示更新:ターゲットの名前色、マーク表示更新の為全員をまわす
        Utils.NotifyRoles(ForceLoop: true);
    }

    public static void KillSuicide(byte deadTargetId)
    {
        var target = Utils.GetPlayerById(deadTargetId);
        if (target != godfather) return;
        // ゴッドファーザー死亡後、キルできる設定の時
        if (GFDeadMode == AfterGotfatherDeadMode.LastCanKill)
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
        if (GFDeadMode == AfterGotfatherDeadMode.LastCanKill)
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
    public override string GetSuffix(PlayerControl seer, PlayerControl seen = null, bool isForMeeting = false)
    {
        seen ??= seer;
        //seerおよびseenが自分である場合以外、または会議中は関係なし
        if (!Is(seer) || !Is(seen) || isForMeeting) return string.Empty;

        StringBuilder sb = new();
        // ゴッドファーザーへの矢印表示
        if (TrackGodfather)
        {
            // 矢印の取得
            string arrow = TargetArrow.GetArrows(Player, godfather.PlayerId);
            // 矢印表示があれば
            if (arrow.Length >= 0)
            {
                // 色を付けてsb追加
                sb.Append(arrow.Color(Palette.ImpostorRed));
            }
        }

        // ジャニターターゲットへの矢印表示
        if (TrackTarget && JanitorTarget.Count > 0)
        {
            string arrow = "";
            foreach (var targetId in JanitorTarget)
            {
                // 矢印の取得
                arrow += TargetArrow.GetArrows(Player, targetId);
            }
            // 矢印表示があれば
            if (arrow.Length >= 0)
            {
                // 色を付けてsb追加
                sb.Append("<color=#e6ccff>").Append(arrow).Append("</color>");
            }
        }

        return sb.ToString();
    }
}