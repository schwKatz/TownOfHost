using System.Text;
using UnityEngine;
using AmongUs.GameOptions;

using TownOfHostY.Roles.Core;
using TownOfHostY.Roles.Core.Interfaces;
using static TownOfHostY.Translator;

namespace TownOfHostY.Roles.Impostor;
public sealed class GrudgeCharger : RoleBase, IImpostor
{
    public static readonly SimpleRoleInfo RoleInfo =
        SimpleRoleInfo.Create(
            typeof(GrudgeCharger),
            player => new GrudgeCharger(player),
            CustomRoles.GrudgeCharger,
            () => RoleTypes.Shapeshifter,
            CustomRoleTypes.Impostor,
            //(int)Options.offsetId.ImpY + 1500,
            (int)Options.offsetId.ImpSpecial + 100,
            SetUpOptionItem,
            "グラージチャージャー"
        );
    public GrudgeCharger(PlayerControl player)
    : base(
        RoleInfo,
        player
    )
    {
        selectTargetCooldown = OptionSelectTargetCooldown.GetFloat();
        chargeKillCooldown = OptionChargeKillCooldown.GetFloat();
        chargeMaxKillCount = OptionChargeMaxKillCount.GetInt();
        maxKillCount = OptionMaxKillCount.GetInt();
    }
    private static OptionItem OptionSelectTargetCooldown;
    private static OptionItem OptionChargeKillCooldown;
    private static OptionItem OptionChargeMaxKillCount;
    private static OptionItem OptionMaxKillCount;
    enum OptionName
    {
        GrudgeChargerSelectTargetCooldown,
        GrudgeChargerChargeKillCooldown,
        GrudgeChargerMaxKillCount,
        GrudgeChargerChargeMaxKillCount,
    }
    private static float selectTargetCooldown;
    private static float chargeKillCooldown;
    private static int chargeMaxKillCount;
    private static int maxKillCount;

    /// <summary> このターンのキル回数 </summary>
    int killCount;
    PlayerControl KillWaitPlayer;

    private static void SetUpOptionItem()
    {
        OptionSelectTargetCooldown = FloatOptionItem.Create(RoleInfo, 10, OptionName.GrudgeChargerSelectTargetCooldown, new(2.5f, 180f, 2.5f), 30f, false)
            .SetValueFormat(OptionFormat.Seconds);
        OptionChargeKillCooldown = FloatOptionItem.Create(RoleInfo, 11, OptionName.GrudgeChargerChargeKillCooldown, new(0.5f, 180f, 0.5f), 2f, false)
            .SetValueFormat(OptionFormat.Seconds);
        OptionChargeMaxKillCount = IntegerOptionItem.Create(RoleInfo, 12, OptionName.GrudgeChargerMaxKillCount, new(1, 40, 1), 10, false)
            .SetValueFormat(OptionFormat.Seconds);
        OptionMaxKillCount = IntegerOptionItem.Create(RoleInfo, 13, OptionName.GrudgeChargerChargeMaxKillCount, new(1, 15, 1), 3, false)
            .SetValueFormat(OptionFormat.Seconds);
    }
    public float CalculateKillCooldown() => chargeKillCooldown;
    public override void ApplyGameOptions(IGameOptions opt)
    {
        AURoleOptions.ShapeshifterCooldown = killCount == 0 ? 0f : selectTargetCooldown;
        AURoleOptions.ShapeshifterDuration = 1f;
    }

    public override void Add()
    {
        killCount = 0;
        KillWaitPlayer = null;
    }
    public void OnCheckMurderAsKiller(MurderInfo info)
    {
        var killer = info.AttemptKiller;
        killer.MarkDirtySettings();
    }
    public override void OnReportDeadBody(PlayerControl reporter, GameData.PlayerInfo target)
    {
        KillWaitPlayer = null;
    }
    public override void OnFixedUpdate(PlayerControl player)
    {
        if (!GameStates.IsInTask) return;
        if (KillWaitPlayer == null) return;

        if (!Player.IsAlive())
        {
            KillWaitPlayer = null;
            return;
        }

        Vector2 GCpos = Player.transform.position; //GCの位置

        var target = KillWaitPlayer;
        float targetDistance = Vector2.Distance(GCpos, target.transform.position);

        var KillRange = GameOptionsData.KillDistances[Mathf.Clamp(Main.NormalOptions.KillDistance, 0, 2)];
        if (targetDistance <= KillRange && Player.CanMove && target.CanMove)
        {
            killCount++;
            Logger.Info($"{Player.GetNameWithRole()} : 残り{chargeMaxKillCount - killCount}発", "GrudgeCharger");
            Player.MarkDirtySettings();
            Player.RpcResetAbilityCooldown();

            target.SetRealKiller(Player);
            Player.RpcMurderPlayer(target);
            KillWaitPlayer = null;
        }
    }

    public override void AfterMeetingTasks()
    {
        if (KillWaitPlayer != null) TargetArrow.Remove(Player.PlayerId, KillWaitPlayer.PlayerId);

        if (Player.IsAlive())
        {
            Player.RpcResetAbilityCooldown();
            KillWaitPlayer = null;
        }
    }

    public override bool OnCheckShapeshift(PlayerControl target, ref bool animate)
    {
        KillWaitPlayer = target;
        TargetArrow.Add(Player.PlayerId, target.PlayerId);

        Logger.Info($"{Player.GetNameWithRole()}のターゲットを{target.GetNameWithRole()}に設定", "GrudgeCharger");
        Player.MarkDirtySettings();
        Utils.NotifyRoles();
        return false;
    }

    public override string GetAbilityButtonText() => GetString("GrudgeChargerSelectTarget");
    public override string GetLowerText(PlayerControl seer, PlayerControl seen = null, bool isForMeeting = false, bool isForHud = false)
    {
        //seenが省略の場合seer
        seen ??= seer;
        //seerおよびseenが自分である場合以外は関係なし
        if (!Is(seer) || !Is(seen) || Player.IsAlive() || isForMeeting) return string.Empty;

        var str = new StringBuilder();
        if (KillWaitPlayer == null)
            str.Append(GetString(isForHud ? "ShapeSelectPlayerTagBefore" : "ShapeSelectPlayerTagMiniBefore"));
        else
        {
            str.Append(GetString(isForHud ? "SelectPlayerTag" : "SelectPlayerTagMini"));
            str.Append(KillWaitPlayer.GetRealName(Options.GetNameChangeModes() == NameChange.Crew));
        }
        return str.ToString();
    }
    public override string GetProgressText(bool comms = false)
        => Utils.ColorString(Color.yellow, $"〈{chargeMaxKillCount - killCount}〉");

}