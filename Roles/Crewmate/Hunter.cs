using System.Collections.Generic;
using System.Linq;
using Hazel;
using UnityEngine;
using AmongUs.GameOptions;

using TownOfHost.Roles.Core;
using TownOfHost.Roles.Core.Interfaces;
using static TownOfHost.Translator;

namespace TownOfHost.Roles.Crewmate;
public sealed class Hunter : RoleBase, IKiller
{
    public static readonly SimpleRoleInfo RoleInfo =
        new(
            typeof(Hunter),
            player => new Hunter(player),
            CustomRoles.Hunter,
            () => RoleTypes.Impostor,
            CustomRoleTypes.Crewmate,
            35100,
            SetupOptionItem,
            "hu",
            "#f8cd46",
            true,
            introSound: () => GetIntroSound(RoleTypes.Crewmate)
        );
    public Hunter(PlayerControl player)
    : base(
        RoleInfo,
        player,
        () => HasTask.False
    )
    {
        ShotLimit = ShotLimitOpt.GetInt();
        CurrentKillCooldown = KillCooldown.GetFloat();
        isImpostor = 0;
    }

    private static OptionItem KillCooldown;
    private static OptionItem ShotLimitOpt;
    private static OptionItem CanKillAllAlive;
    private static OptionItem KnowTargetIsImpostor;

    enum OptionName
    {
        SheriffShotLimit,
        SheriffCanKillAllAlive,
        HunterKnowTargetIsImpostor
    }
    public static Dictionary<CustomRoles, OptionItem> KillTargetOptions = new();
    public int ShotLimit = 0;
    public float CurrentKillCooldown = 30;
    int isImpostor = 0;
    public static readonly string[] KillOption =
    {
            "SheriffCanKillAll", "SheriffCanKillSeparately"
        };
    private static void SetupOptionItem()
    {
        KillCooldown = FloatOptionItem.Create(RoleInfo, 10, GeneralOption.KillCooldown, new(0f, 180f, 2.5f), 30f, false)
            .SetValueFormat(OptionFormat.Seconds);
        ShotLimitOpt = IntegerOptionItem.Create(RoleInfo, 11, OptionName.SheriffShotLimit, new(1, 15, 1), 15, false)
            .SetValueFormat(OptionFormat.Times);
        CanKillAllAlive = BooleanOptionItem.Create(RoleInfo, 12, OptionName.SheriffCanKillAllAlive, true, false);
        KnowTargetIsImpostor = BooleanOptionItem.Create(RoleInfo, 13, OptionName.HunterKnowTargetIsImpostor, false, false);
    }
    public override void Add()
    {
        var playerId = Player.PlayerId;
        CurrentKillCooldown = KillCooldown.GetFloat();
        isImpostor = 0;

        if (!Main.ResetCamPlayerList.Contains(playerId))
            Main.ResetCamPlayerList.Add(playerId);

        ShotLimit = ShotLimitOpt.GetInt();
        Logger.Info($"{Utils.GetPlayerById(playerId)?.GetNameWithRole()} : 残り{ShotLimit}発", "Hunter");
    }
    private void SendRPC()
    {
        using var sender = CreateSender(CustomRPC.SetHunterShotLimit);
        sender.Writer.Write(ShotLimit);
        sender.Writer.Write(isImpostor);
    }
    public override void ReceiveRPC(MessageReader reader, CustomRPC rpcType)
    {
        if (rpcType != CustomRPC.SetHunterShotLimit) return;

        ShotLimit = reader.ReadInt32();
        isImpostor = reader.ReadInt32();
    }
    public float CalculateKillCooldown() => CanUseKillButton() ? CurrentKillCooldown : 0f;
    public bool CanUseKillButton()
        => Player.IsAlive()
        && (CanKillAllAlive.GetBool() || GameStates.AlreadyDied)
        && ShotLimit > 0;
    public override bool CanSabotage(SystemTypes systemType) => false;
    public override void ApplyGameOptions(IGameOptions opt)
    {
        opt.SetVision(false);
    }
    public void OnCheckMurderAsKiller(MurderInfo info)
    {
        if (Is(info.AttemptKiller) && !info.IsSuicide)
        {
            (var killer, var target) = info.AttemptTuple;

            Logger.Info($"{killer.GetNameWithRole()} : 残り{ShotLimit}発", "Hunter");
            if (ShotLimit <= 0)
            {
                info.DoKill = false;
                return;
            }
            ShotLimit--;
            if (target.Is(CustomRoleTypes.Impostor)) isImpostor = 1;
            else if (target.Is(CustomRoleTypes.Neutral)) isImpostor = 2;
            else isImpostor = 0;
            SendRPC();
            killer.ResetKillCooldown();
        }
    }
    public override string GetProgressText(bool comms = false) => Utils.ColorString(CanUseKillButton() ? Color.yellow : Color.gray, $"({ShotLimit})");

    public override string GetMark(PlayerControl seer, PlayerControl seen, bool isForMeeting = false)
    {
        //seenが省略の場合seer
        seen ??= seer;

        if (KnowTargetIsImpostor.GetBool() && seer.Is(CustomRoles.Hunter) && isImpostor == 1)
            return Utils.ColorString(Utils.GetRoleColor(CustomRoles.Hunter), "◎");
        if (KnowTargetIsImpostor.GetBool() && seer.Is(CustomRoles.Hunter) && isImpostor == 2)
            return Utils.ColorString(Utils.GetRoleColor(CustomRoles.Hunter), "▽");

        return "";
    }
    public override void OnStartMeeting()
    {
        isImpostor = 0;
    }
}