using System.Collections.Generic;
using UnityEngine;
using TownOfHostY.Roles.Core;
using static TownOfHostY.Options;

namespace TownOfHostY.Roles.AddOns.Common;

public static class NonReport
{
    private static readonly int Id = 85300;
    private static Color RoleColor = Utils.GetRoleColor(CustomRoles.NonReport);
    public static string SubRoleMark = Utils.ColorString(RoleColor, "Ｎ");
    private static List<byte> playerIdList = new();

    public static void SetupCustomOption()
    {
        SetupRoleOptions(Id, TabGroup.Addons, CustomRoles.NonReport);
    }
    public static void Init()
    {
        playerIdList = new();
    }
    public static void Add(byte playerId)
    {
        if (!playerIdList.Contains(playerId))
            playerIdList.Add(playerId);
    }
    public static bool IsEnable => playerIdList.Count > 0;
    public static bool IsThisRole(byte playerId) => playerIdList.Contains(playerId);

}