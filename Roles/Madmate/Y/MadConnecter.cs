using System.Collections.Generic;
using System.Linq;
using AmongUs.GameOptions;

using TownOfHostY.Roles.Core;
using TownOfHostY.Roles.Core.Interfaces;

namespace TownOfHostY.Roles.Madmate;
public sealed class MadConnecter : RoleBase, IKiller
{
    public static readonly SimpleRoleInfo RoleInfo =
         SimpleRoleInfo.Create(
            typeof(MadConnecter),
            player => new MadConnecter(player),
            CustomRoles.MadConnecter,
            () => RoleTypes.Impostor,
            CustomRoleTypes.Madmate,
            (int)Options.offsetId.MadSpecial + 0,
            //(int)Options.offsetId.MadY + 800,
            SetupOptionItem,
            "マッドコネクター",
            isDesyncImpostor: true
        );
    public MadConnecter(PlayerControl player)
    : base(
        RoleInfo,
        player,
        () => HasTask.False
    )
    {
        CanVent = OptionCanVent.GetBool();
        KnowImpostorTasks = OptionKnowImpostor.GetInt();

        MadList = new();
        CustomRoleManager.MarkOthers.Add(GetMarkOthers);
        CustomRoleManager.SuffixOthers.Add(GetSuffixOthers);
    }

    private static OptionItem OptionCanVent;
    private static OptionItem OptionKnowImpostor;
    private static bool CanVent;
    private static int KnowImpostorTasks;
    private static HashSet<MadConnecter> MadList;
    private HashSet<byte> ConnectImpostorId;

    enum OptionName
    {
        MadSnitchTaskTrigger
    }

    private static void SetupOptionItem()
    {
        OptionCanVent = BooleanOptionItem.Create(RoleInfo, 10, GeneralOption.CanVent, true, false);
        OptionKnowImpostor = IntegerOptionItem.Create(RoleInfo, 11, OptionName.MadSnitchTaskTrigger, new(1, 30, 1), 2, false)
            .SetValueFormat(OptionFormat.Pieces);
        Options.SetUpAddOnOptions(RoleInfo.ConfigId + 20, RoleInfo.RoleName, RoleInfo.Tab);
    }
    public float CalculateKillCooldown() => 0.01f;
    public override void ApplyGameOptions(IGameOptions opt)
    {
        opt.SetVision(Options.AddOnRoleOptions[(CustomRoles.MadConnecter, CustomRoles.AddLight)].GetBool());
    }
    public override void Add()
    {
        MadList.Add(this);
        ConnectImpostorId = new();

        VentEnterTask.Add(Player, KnowImpostorTasks, useVent: CanVent);
    }

    private bool KnowsImpostor()
    {
        // タスク完了＝インポスターが誰か分かる
        return VentEnterTask.NowTaskCountNow(Player.PlayerId) >= KnowImpostorTasks;
    }
    private bool ConnectsImpostor(byte impostorId)
    {
        // 互いにコネクトした相方インポスター
        return ConnectImpostorId.Contains(impostorId);
    }

    public void OnCheckMurderAsKiller(MurderInfo info)
    {
        // ガード持ちに関わらず能力発動する直接キル役職
        (var killer, var target) = info.AttemptTuple;
        info.DoKill = false;

        // 視認前、またはインポスターでない、または既に繋がっている場合は関係ない
        if (!KnowsImpostor() || !target.Is(CustomRoleTypes.Impostor) || ConnectsImpostor(target.PlayerId)) return;

        // インポスターとコネクト
        ConnectImpostorId.Add(target.PlayerId);
        // マッドにはパリン
        killer.RpcProtectedMurderPlayer(target);
        // 相方インポスターにはキルフラッシュ
        target.KillFlash();

        Logger.Info($"{killer.GetNameWithRole()} : {target.GetNameWithRole()}とコネクト", "MadConnecter");

        // 互いに矢印追加
        TargetArrow.Add(killer.PlayerId, target.PlayerId);
        TargetArrow.Add(target.PlayerId, killer.PlayerId);

        // 表示更新
        Utils.NotifyRoles();
    }
    // オーバーライドでない
    public static new void OnCompleteTask()
    {
        foreach (var mad in MadList)
        {
            // (視認できるタスク数が未完了)なら関係ない
            if (!mad.KnowsImpostor()) continue;

            foreach (var impostor in Main.AllPlayerControls.Where(player => player.Is(CustomRoleTypes.Impostor)))
            {
                // インポスターの名前を赤くする
                NameColorManager.Add(mad.Player.PlayerId, impostor.PlayerId, impostor.GetRoleColorCode());
            }
            // マッドには終わらせた合図のパリン
            mad.Player.RpcProtectedMurderPlayer(mad.Player);
        }
    }
    public override string GetSuffix(PlayerControl seer, PlayerControl seen = null, bool isForMeeting = false)
    {
        seen ??= seer;
        //seerおよびseenが自分である場合以外、または会議中は関係なし
        if (!Is(seer) || !Is(seen) || isForMeeting) return string.Empty;

        string arrow = "";
        // インポスターへの矢印表示
        if (ConnectImpostorId.Count > 0)
        {
            foreach (var targetId in ConnectImpostorId)
            {
                // マッドからコネクトインポスターへの矢印
                arrow += TargetArrow.GetArrows(Player, targetId);
            }
            // 矢印表示があれば
            if (arrow.Length >= 0)
            {
                // 色を付ける
                arrow.Color(Palette.ImpostorRed);
            }
        }
        return arrow;
    }
    public static string GetMarkOthers(PlayerControl seer, PlayerControl seen = null, bool isForMeeting = false)
    {
        seen ??= seer;
        if (!seer.Is(CustomRoleTypes.Impostor) ||
            seen.GetRoleClass() is not MadConnecter madConnecter ||
            !madConnecter.ConnectsImpostor(seer.PlayerId))
        {
            return string.Empty;
        }
        // インポスターから見たマッドへの★
        return "★".Color(Palette.ImpostorRed);
    }
    public static string GetSuffixOthers(PlayerControl seer, PlayerControl seen = null, bool isForMeeting = false)
    {
        seen ??= seer;
        if (seer != seen) return string.Empty;
        // インポスターから見たマッドへの矢印
        return GetImpostorArrows(seer.PlayerId);
    }
    private static string GetImpostorArrows(byte seerId)
    {
        var arrow = "";
        MadList.RemoveWhere(mad => !mad.Player.IsAlive());

        foreach (var mad in MadList)
        {
            if (mad.ConnectImpostorId.Count <= 0) continue;

            // インポスターから見たマッドへの矢印
            arrow += TargetArrow.GetArrows(seerId, mad.Player.PlayerId);
        }

        arrow = arrow.Length > 0 ? arrow.Color(Palette.ImpostorRed) : "";
        return arrow;
    }
}