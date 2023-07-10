using System.Collections.Generic;
using UnityEngine;
using TownOfHostY.Roles.Core;

using static TownOfHostY.Options;
using static TownOfHostY.Translator;
using static TownOfHostY.Utils;

namespace TownOfHostY.Roles.AddOns.Common;

public static class Sending
{
    private static readonly int Id = 80600;
    private static Color RoleColor = GetRoleColor(CustomRoles.Sending);
    public static string SubRoleMark = ColorString(RoleColor, "Se");
    private static List<byte> playerIdList = new();

    private static PlayerControl ExiledPlayer = null;

    public static void SetupCustomOption()
    {
        SetupRoleOptions(Id, TabGroup.Addons, CustomRoles.Sending);
    }
    public static void Init()
    {
        playerIdList = new();
        ExiledPlayer = null;
    }
    public static void Add(byte playerId)
    {
        if (!playerIdList.Contains(playerId))
            playerIdList.Add(playerId);
    }
    public static void OnExileWrapUp(PlayerControl exiled)
    {
        ExiledPlayer = exiled;
    }
    public static void OnStartMeeting()
    {
        ExiledPlayer = null;
    }
    public static string RealNameChange(string Name)
    {
        if (ExiledPlayer == null) return Name;

        var ExiledPlayerName = ExiledPlayer.Data.PlayerName;
        if (ExiledPlayer.Is(CustomRoleTypes.Impostor))
            return ColorString(RoleColor, string.Format(GetString("isImpostor"), ExiledPlayerName));
        else
            return ColorString(RoleColor, string.Format(GetString("isNotImpostor"), ExiledPlayerName));
    }

    public static bool IsEnable => playerIdList.Count > 0;
    public static bool IsThisRole(byte playerId) => playerIdList.Contains(playerId);
}