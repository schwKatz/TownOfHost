using System.Collections.Generic;
using UnityEngine;
using TownOfHost.Roles.Core;

using static TownOfHost.Options;
using static TownOfHost.Translator;
using static TownOfHost.Utils;

namespace TownOfHost.Roles.AddOns.Common;

public static class Sending
{
    private static readonly int Id = 77600;
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