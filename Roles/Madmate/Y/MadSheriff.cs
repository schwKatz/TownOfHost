using AmongUs.GameOptions;

using TownOfHostY.Roles.Core;
using TownOfHostY.Roles.Core.Interfaces;

namespace TownOfHostY.Roles.Madmate;
public sealed class MadSheriff : RoleBase, IKiller
{
    public static readonly SimpleRoleInfo RoleInfo =
         SimpleRoleInfo.Create(
            typeof(MadSheriff),
            player => new MadSheriff(player),
            CustomRoles.MadSheriff,
            () => RoleTypes.Impostor,
            CustomRoleTypes.Madmate,
            (int)Options.offsetId.MadY + 300,
            SetupOptionItem,
            "マッドシェリフ",
            isDesyncImpostor: true
        );
    public MadSheriff(PlayerControl player)
    : base(
        RoleInfo,
        player,
        () => HasTask.False
    )
    {
        KillCooldown = OptionKillCooldown.GetFloat();
        nowSuicideMotion = (SuicideMotionOption)OptionSuicideMotion.GetValue();
        MisfireKillsTarget = OptionMisfireKillsTarget.GetBool();
        CanVent = OptionCanVent.GetBool();
    }

    private static OptionItem OptionKillCooldown;
    public static OptionItem OptionSuicideMotion;
    private static OptionItem OptionMisfireKillsTarget;
    private static OptionItem OptionCanVent;
    public enum SuicideMotionOption
    {
        Default,
        MotionKilled
    };
    SuicideMotionOption nowSuicideMotion;

    enum OptionName
    {
        SheriffMisfireKillsTarget,
        SillySheriffSuicideMotion,
    }
    private static float KillCooldown;
    private static bool MisfireKillsTarget;
    public static bool CanVent;

    private static void SetupOptionItem()
    {
        OptionKillCooldown = FloatOptionItem.Create(RoleInfo, 10, GeneralOption.KillCooldown, new(0f, 180f, 2.5f), 30f, false)
            .SetValueFormat(OptionFormat.Seconds);
        OptionSuicideMotion = StringOptionItem.Create(RoleInfo, 13, OptionName.SillySheriffSuicideMotion, EnumHelper.GetAllNames<SuicideMotionOption>(), 0, false);
        OptionMisfireKillsTarget = BooleanOptionItem.Create(RoleInfo, 11, OptionName.SheriffMisfireKillsTarget, false, false);
        OptionCanVent = BooleanOptionItem.Create(RoleInfo, 12, GeneralOption.CanVent, false, false);
        Options.SetUpAddOnOptions(RoleInfo.ConfigId + 20, RoleInfo.RoleName, RoleInfo.Tab);
    }
    public float CalculateKillCooldown() => KillCooldown;
    public bool CanUseImpostorVentButton() => CanVent;
    public override void ApplyGameOptions(IGameOptions opt)
    {
        opt.SetVision(Options.AddOnRoleOptions[(CustomRoles.MadSheriff, CustomRoles.AddLight)].GetBool());
    }
    public void OnCheckMurderAsKiller(MurderInfo info)
    {
        if (!Is(info.AttemptKiller) || info.IsSuicide) return;
        (var killer, var target) = info.AttemptTuple;
        // ガード持ちに関わらず能力発動する直接キル役職
        //自殺処理
        switch (nowSuicideMotion)
        {
            case SuicideMotionOption.Default://自殺モーション起こさない＝自身が自爆
                killer.RpcMurderPlayer(killer);
                break;
            case SuicideMotionOption.MotionKilled://相手に切られる
                target.RpcMurderPlayer(killer);
                break;
        }
        PlayerState.GetByPlayerId(killer.PlayerId).DeathReason = CustomDeathReason.Misfire;

        if (!MisfireKillsTarget) info.DoKill = false;
    }
}