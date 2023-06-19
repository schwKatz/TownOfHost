using AmongUs.GameOptions;
using System.Collections.Generic;
using UnityEngine;

using TownOfHost.Roles.Core;
using TownOfHost.Roles.Core.Interfaces;

namespace TownOfHost.Roles.Impostor;
public sealed class EvilDiviner : RoleBase, IImpostor
{
    public static readonly SimpleRoleInfo RoleInfo =
        new(
            typeof(EvilDiviner),
            player => new EvilDiviner(player),
            CustomRoles.EvilDiviner,
            () => RoleTypes.Impostor,
            CustomRoleTypes.Impostor,
            3600,
            SetupOptionItem,
            "イビルディバイナー"
        );
    public EvilDiviner(PlayerControl player)
    : base(
        RoleInfo,
        player
    )
    {
        KillCooldown = OptionKillCooldown.GetFloat();
        DivinationMaxCount = OptionDivinationMaxCount.GetInt();
    }
    private static OptionItem OptionKillCooldown;
    private static OptionItem OptionDivinationMaxCount;
    enum OptionName
    {
        EvilDivinerDivinationMaxCount,
    }
    private static float KillCooldown;
    private static int DivinationMaxCount;

    static int DivinationCount;
    static List<byte> DivinationTarget = new();

    public static void SetupOptionItem()
    {
        OptionKillCooldown = FloatOptionItem.Create(RoleInfo, 10, GeneralOption.KillCooldown, new(0f, 180f, 2.5f), 30f, false)
            .SetValueFormat(OptionFormat.Seconds);
        OptionDivinationMaxCount = IntegerOptionItem.Create(RoleInfo, 11, OptionName.EvilDivinerDivinationMaxCount, new(1, 15, 1), 5, false)
            .SetValueFormat(OptionFormat.Times);
    }
    public override void Add()
    {
        DivinationCount = DivinationMaxCount;
        DivinationTarget = new();
        Player.AddDoubleTrigger();
    }
    public float CalculateKillCooldown() => KillCooldown;
    public override string GetProgressText(bool comms = false) => Utils.ColorString(DivinationCount > 0 ? Palette.ImpostorRed : Color.gray, $"({DivinationCount})");

    public override void OverrideRoleNameAsSeer(PlayerControl seen, bool isMeeting, ref bool enabled, ref Color roleColor, ref string roleText)
    {
        if (DivinationTarget != null && DivinationTarget.Contains(seen.PlayerId) && Player.IsAlive())
            enabled = true;
    }

    public static bool OnCheckMurder(PlayerControl killer, PlayerControl target)
    {
        if (DivinationCount > 0)
        {
            return killer.CheckDoubleTrigger(target, () => { SetDivination(killer, target); });
        }
        else return true;
    }
    public static void SetDivination(PlayerControl killer, PlayerControl target)
    {
        if (!DivinationTarget.Contains(target.PlayerId))
        {
            DivinationCount--;
            DivinationTarget.Add(target.PlayerId);
            Logger.Info($"{killer.GetNameWithRole()}：占った 占い先→{target.GetNameWithRole()} || 残り{DivinationCount}回", "EvilDiviner");
            Utils.NotifyRoles(SpecifySeer: killer);

            //キルクールの適正化
            killer.SetKillCooldown();
        }
    }
}