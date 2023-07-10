using System.Collections.Generic;
using UnityEngine;
using TownOfHostY.Roles.Core;
using static TownOfHostY.Options;
using System.Linq;

namespace TownOfHostY.Roles.AddOns.Common;

public static class Loyalty
{
    private static Color RoleColor = Utils.GetRoleColor(CustomRoles.Loyalty);
    public static string SubRoleMark = Utils.ColorString(RoleColor, "Ｌ");
    private static List<byte> playerIdList = new();

    public static void Init()
    {
        playerIdList = new();
    }
    public static void Add(byte playerId)
    {
        playerIdList.Add(playerId);
        foreach (var target in Main.AllPlayerControls.Where(x => x.GetCustomRole().IsImpostor()))
        {
            NameColorManager.Add(playerId, target.PlayerId);
        }
    }
    public static bool IsEnable => playerIdList.Count > 0;
    public static bool IsThisRole(byte playerId) => playerIdList.Contains(playerId);
}