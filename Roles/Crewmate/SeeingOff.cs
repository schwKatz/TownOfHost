using AmongUs.GameOptions;
using TownOfHost.Roles.Core;

using static TownOfHost.Translator;
using static TownOfHost.Utils;

namespace TownOfHost.Roles.Crewmate;
public sealed class SeeingOff : RoleBase
{
    public static readonly SimpleRoleInfo RoleInfo =
        new(
            typeof(SeeingOff),
            player => new SeeingOff(player),
            CustomRoles.SeeingOff,
            () => RoleTypes.Crewmate,
            CustomRoleTypes.Crewmate,
            35600,
            null,
            "見送り人",
            "#883fd1"
        );
    public SeeingOff(PlayerControl player)
    : base(
        RoleInfo,
        player
    )
    {
        ExiledPlayer = null;
    }

    private static PlayerControl ExiledPlayer = null;

    public override void OnExileWrapUp(GameData.PlayerInfo exiled, ref bool DecidedWinner)
    {
        if (!Player.IsAlive()) return;

        ExiledPlayer = GetPlayerById(exiled.PlayerId);
    }
    public override void OnStartMeeting()
    {
        ExiledPlayer = null;
    }

    public static string RealNameChange(string Name)
    {
        if (ExiledPlayer != null)
        {
            if (ExiledPlayer == null) { Logger.Info($"Debug:RealNameChange, PlayerControl = Null", "SeeingOff"); return Name; }
            var ExiledPlayerName = ExiledPlayer.Data.PlayerName;

            if (ExiledPlayer.Is(CustomRoleTypes.Impostor))
                return ColorString(RoleInfo.RoleColor, string.Format(GetString("isImpostor"), ExiledPlayerName));
            else
                return ColorString(RoleInfo.RoleColor, string.Format(GetString("isNotImpostor"), ExiledPlayerName));
        }
        else
            return Name;
    }
}