using System.Collections.Generic;
using HarmonyLib;
using Hazel;

namespace TownOfHostY;

[HarmonyPatch(typeof(VentilationSystem), nameof(VentilationSystem.UpdateSystem))]
class VentilationSystemPatch
{
    private static VentilationSystem Instance;
    public static void Postfix(VentilationSystem __instance, PlayerControl player, MessageReader msgReader)
    {
        Instance = __instance;
    }
    public static void ClearVent()
    {
        if (Instance == null) return;
        if (Main.NormalOptions.MapId != (byte)MapNames.Skeld &&
            Main.NormalOptions.MapId != (byte)MapNames.Mira &&
            Main.NormalOptions.MapId != (byte)MapNames.Dleks &&
            Main.NormalOptions.MapId != (byte)MapNames.Airship) return;

        List <(byte playerId, byte ventId)> list = new();
        foreach (var vent in Instance.PlayersInsideVents)
        {
            list.Add((vent.Key, vent.Value));
        }

        foreach (var vent in list)
        {
            var player = Utils.GetPlayerById(vent.playerId);
            Logger.Info($"ClearVent player: {player?.name}, vent: {vent.ventId}", "VentilationSystem");
            if (vent.playerId == PlayerControl.LocalPlayer.PlayerId)
            {
                MessageWriter messageWriter = AmongUsClient.Instance.StartRpc(PlayerControl.LocalPlayer.NetId, (byte)RpcCalls.ExitVent, SendOption.Reliable);
                messageWriter.WritePacked(vent.ventId);
                messageWriter.EndMessage();
            }
            else
            {
                player?.MyPhysics.RpcExitVent(vent.ventId);
            }
        }
    }
}
