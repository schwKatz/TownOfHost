using System;
using TownOfHostY.Roles.Core;
using static TownOfHostY.Utils;
using static TownOfHostY.CatchCat.Option;
using UnityEngine;
using System.Collections.Generic;
using TownOfHostY.Attributes;

namespace TownOfHostY.CatchCat;

static class Common
{
    public static Dictionary<byte, bool> CanGuard = new();
    public static Dictionary<byte, bool> CanVent = new();
    public static Dictionary<byte, bool[]> IsSet = new();

    public static ColorCatKill NowColorCatKill;

    [GameModuleInitializer]
    public static void Init()
    {
        CanGuard.Clear();
        CanVent.Clear();
        IsSet.Clear();
    }
    public static void Add(PlayerControl pc)
    {
        NowColorCatKill = (ColorCatKill)WhenColorCatKilled.GetValue();

        if (pc.GetCustomRole().IsCCLeaderRoles() && !LeaderKilled.GetBool())
        {
            CanGuard.Add(pc.PlayerId, true);
        }
        else
        {
            CanGuard.Add(pc.PlayerId, false);
            CanVent.Add(pc.PlayerId, false);
        }
        IsSet.Add(pc.PlayerId, new bool[] { false, false, false, false });
    }

    // Common Mark
    public static string GetMark(PlayerControl pc)
    {
        string mark = string.Empty;
        if (IsSet[pc.PlayerId][0])
            mark += ColorString(Color.yellow, "Ｌ");
        if (CanGuard[pc.PlayerId])
            mark += ColorString(Color.cyan, "Ｇ");
        if (IsSet[pc.PlayerId][2])
            mark += ColorString(Color.green, "Ｖ");
        if (IsSet[pc.PlayerId][3])
            mark += ColorString(GetRoleColor(pc.GetCustomRole()), "Ｋ");
        return mark;
    }

    /// <summary>leaderRoleとcatRoleが同じ陣営であるかを返す</summary>
    public static bool IsSameCamp_LederCat(CustomRoles leaderRole, CustomRoles catRole)
    {
        switch (leaderRole)
        {
            case CustomRoles.CCRedLeader:
                if (catRole == CustomRoles.CCRedCat) return true;
                break;
            case CustomRoles.CCBlueLeader:
                if (catRole == CustomRoles.CCBlueCat) return true;
                break;
            case CustomRoles.CCYellowLeader:
                if (catRole == CustomRoles.CCYellowCat) return true;
                break;
        }
        return false;
    }

    /// <summary>各条件に合ったプレイヤーの人数を取得し、配列に同順で格納します。</summary>
    public static int[] CountLivingPlayersByPredicates(params Predicate<PlayerControl>[] predicates)
    {
        int[] counts = new int[predicates.Length];
        foreach (var pc in Main.AllAlivePlayerControls)
        {
            for (int i = 0; i < predicates.Length; i++)
            {
                if (predicates[i](pc)) counts[i]++;
            }
        }
        return counts;
    }
}
