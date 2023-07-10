using System.Collections.Generic;
using UnityEngine;
using TownOfHostY.Roles.Core;
using static TownOfHostY.Options;

namespace TownOfHostY.Roles.AddOns.Common;

public static class InfoPoor
{
    private static readonly int Id = 85200;
    private static Color RoleColor = Utils.GetRoleColor(CustomRoles.InfoPoor);
    public static string SubRoleMark = Utils.ColorString(RoleColor, "Ｉ");
    private static List<byte> playerIdList = new();

    public static void SetupCustomOption()
    {
        SetupRoleOptions(Id, TabGroup.Addons, CustomRoles.InfoPoor);
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