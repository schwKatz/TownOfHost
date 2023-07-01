using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using Hazel;
using AmongUs.GameOptions;
using UnityEngine;

using TownOfHost.Roles.Core;

namespace TownOfHost.Roles.Neutral;
public sealed class Lawyer : RoleBase
{
    public static readonly SimpleRoleInfo RoleInfo =
        SimpleRoleInfo.Create(
            typeof(Lawyer),
            player => new Lawyer(player),
            CustomRoles.Lawyer,
            () => RoleTypes.Crewmate,
            CustomRoleTypes.Neutral,
            60500,
            SetupOptionItem,
            "弁護士",
            "#daa520",
            introSound: () => GetIntroSound(RoleTypes.Impostor)
        );
    public Lawyer(PlayerControl player)
    : base(
        RoleInfo,
        player
    )
    {
        HasImpostorVision = OptionHasImpostorVision.GetBool();
        KnowTargetRole = OptionKnowTargetRole.GetBool();
        TargetKnows = OptionTargetKnows.GetBool();
        PursuerGuardNum = OptionPursuerGuardNum.GetInt();

        CustomRoleManager.MarkOthers.Add(GetMarkOthers);
        CustomRoleManager.OnMurderPlayerOthers.Add(OnMurderPlayerOthers);
    }
    public override void OnDestroy()
    {
        CustomRoleManager.MarkOthers.Remove(GetMarkOthers);
        if (Target.Count <= 0)
        {
            CustomRoleManager.OnMurderPlayerOthers.Remove(OnMurderPlayerOthers);
        }
        Pursuers.Clear();
    }

    public static OptionItem OptionHasImpostorVision;
    public static OptionItem OptionKnowTargetRole;
    private static OptionItem OptionTargetKnows;
    public static OptionItem OptionPursuerGuardNum;
    enum OptionName
    {
        LawyerTargetKnows,
        LawyerKnowTargetRole,
        PursuerGuardNum
    }
    public static bool HasImpostorVision;
    public static bool KnowTargetRole;
    private static bool TargetKnows;
    public static int PursuerGuardNum;

    /// <summary>
    /// Key: LawyerのPlayerId, Value: ターゲットのPlayerControl
    /// </summary>
    public static Dictionary<byte, PlayerControl> Target = new();
    public static List<byte> Pursuers = new();

    public int GuardCount = 0;

    private static void SetupOptionItem()
    {
        OptionHasImpostorVision = BooleanOptionItem.Create(RoleInfo, 10, GeneralOption.ImpostorVision, false, false);
        OptionKnowTargetRole = BooleanOptionItem.Create(RoleInfo, 11, OptionName.LawyerTargetKnows, false, false);
        OptionTargetKnows = BooleanOptionItem.Create(RoleInfo, 12, OptionName.LawyerKnowTargetRole, false, false);
        OptionPursuerGuardNum = IntegerOptionItem.Create(RoleInfo, 13, OptionName.PursuerGuardNum, new(0, 20, 1), 1, false)
            .SetValueFormat(OptionFormat.Times);
    }
    public override void Add()
    {
        Pursuers.Clear();
        //ターゲット割り当て
        if (AmongUsClient.Instance.AmHost)
        {
            List<PlayerControl> targetList = new();
            var rand = IRandom.Instance;
            foreach (var target in Main.AllPlayerControls)
            {
                if (Player == target) continue;
                if ((target.Is(CustomRoleTypes.Impostor)
                    || target.IsNeutralKiller())
                    && !target.Is(CustomRoles.Lovers)
                ) targetList.Add(target);
            }
            var SelectedTarget = targetList[rand.Next(targetList.Count)];
            Target.Add(Player.PlayerId, SelectedTarget);
            SendRPC(Player.PlayerId, SelectedTarget.PlayerId, "SetTarget");
            Logger.Info($"{Player.GetNameWithRole()}:{SelectedTarget.GetNameWithRole()}", "Lawyer");
        }

        GuardCount = PursuerGuardNum;
    }
    private void SendRPC(byte LawyerId, byte targetId = byte.MaxValue, string Progress = "")
    {
        switch (Progress)
        {
            case "SetTarget":
                var sender = CreateSender(CustomRPC.SetLawyerTarget);
                sender.Writer.Write(LawyerId);
                sender.Writer.Write(targetId);
                break;
            case "":
                if (!AmongUsClient.Instance.AmHost) return;
                var Rsender = CreateSender(CustomRPC.SetRemoveLawyerTarget);
                Rsender.Writer.Write(LawyerId);
                break;
        }
    }
    public override void ReceiveRPC(MessageReader reader, CustomRPC rpcType)
    {
        if (rpcType == CustomRPC.SetBountyTarget)
        {
            byte LawyerId = reader.ReadByte();
            byte TargetId = reader.ReadByte();
            Target[LawyerId] = Utils.GetPlayerById(TargetId);
        }
        else if (rpcType == CustomRPC.SetBountyTarget)
        {
            Target.Remove(reader.ReadByte());
        }
    }
    public static bool IsPursuer(PlayerControl bakery)
    {
        foreach (var ba in Pursuers)
        {
            if (ba == bakery.PlayerId)
                return true;
        }
        return false;
    }

    public override void OnMurderPlayerAsTarget(MurderInfo _)
    {
        Target[Player.PlayerId] = null;
        SendRPC(Player.PlayerId);
    }
    public static void OnMurderPlayerOthers(MurderInfo info)
    {
        var target = info.AttemptTarget;

        foreach (var lawyerId in Target.Keys)
        {
            if (Target[lawyerId] == target)
            {
                ChangeRole(lawyerId);
                break;
            }
        }
    }
    public override void OverrideRoleNameAsSeer(PlayerControl seen, bool isMeeting, ref bool enabled, ref Color roleColor, ref string roleText)
    {
        if (KnowTargetRole && Target.ContainsKey(Player.PlayerId) && seen == Target[Player.PlayerId])
            enabled = true;
    }

    private bool CanUseGuard() => Player.IsAlive() && GuardCount > 0;
    public override string GetProgressText(bool comms = false)
    {
        if (!Pursuers.Contains(Player.PlayerId)) return string.Empty;
        return Utils.ColorString(CanUseGuard() ? Color.yellow : Color.gray, $"({GuardCount})");
    }
    public override string GetMark(PlayerControl seer, PlayerControl seen, bool _ = false)
    {
        //seenが省略の場合seer
        seen ??= seer;

        if (seer == Player && seen == Target[Player.PlayerId])
            return Utils.ColorString(RoleInfo.RoleColor, "§");
        return string.Empty;
    }
    public static string GetMarkOthers(PlayerControl seer, PlayerControl seen = null, bool isForMeeting = false)
    {
        seen ??= seer;

        var mark = string.Empty;
        Target.Do(x =>
        {
            if (TargetKnows && seer == x.Value && seen == x.Value && Utils.GetPlayerById(x.Key).IsAlive())
                mark = Utils.ColorString(RoleInfo.RoleColor, "§");
        });
        return mark;
    }

    public override bool OnCheckMurderAsTarget(MurderInfo info)
    {
        (var killer, var target) = info.AttemptTuple;

        if (!Pursuers.Contains(Player.PlayerId) || GuardCount <= 0) return true;
        killer.RpcGuardAndKill(target);
        target.RpcGuardAndKill(target);
        killer.SetKillCooldown();
        GuardCount--;

        info.CanKill = false;
        return false;
    }

    public override void OnExileWrapUp(GameData.PlayerInfo exiled, ref bool DecidedWinner)
    {
        if (Target[Player.PlayerId].PlayerId == exiled.PlayerId && Player.IsAlive())
            ChangeRole(Player.PlayerId);
    }

    public static void ChangeRoleByTarget(PlayerControl target)
    {
        byte LawyerId = byte.MaxValue;
        Target.Do(x =>
        {
            if (x.Value == target)
                LawyerId = x.Key;
        });
        ChangeRole(LawyerId);
        Utils.NotifyRoles();
    }
    public static void ChangeRole(byte LawyerId)
    {
        Pursuers.Add(LawyerId);
        Target.Remove(LawyerId);
    }


    public static void EndGameCheck()
    {
        Target.Do(x =>
        {
            // 勝者に依頼人が含まれている時
            if (CustomWinnerHolder.WinnerIds.Contains(x.Value.PlayerId))
            {
                byte Lawyer = x.Key;
                // 弁護士が生きている時 リセットして単独勝利
                if (Utils.GetPlayerById(Lawyer).IsAlive())
                {
                    CustomWinnerHolder.ResetAndSetWinner(CustomWinner.Lawyer);
                    CustomWinnerHolder.WinnerIds.Add(Lawyer);
                }
                // 弁護士が死んでいる時 勝者と共に追加勝利
                else
                {
                    CustomWinnerHolder.WinnerIds.Add(Lawyer);
                    CustomWinnerHolder.AdditionalWinnerTeams.Add(AdditionalWinners.Lawyer);
                }
            }
        });

        // 追跡者が生き残った場合ここで追加勝利
        Main.AllPlayerControls
            .Where(p => p.Is(CustomRoles.Lawyer) && Pursuers.Contains(p.PlayerId) && p.IsAlive())
            .Do(p =>
            {
                CustomWinnerHolder.WinnerIds.Add(p.PlayerId);
                CustomWinnerHolder.AdditionalWinnerTeams.Add(AdditionalWinners.Pursuer);
            });
    }
}