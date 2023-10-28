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

        List<(byte playerId, byte ventId)> list = new();
        foreach (var vent in Instance.PlayersInsideVents)
        {
            list.Add((vent.Key, vent.Value));
        }

        foreach (var vent in list)
        {
            var player = Utils.GetPlayerById(vent.playerId);
            Logger.Info($"ClearVent player: {player?.name}, vent: {vent.ventId}", "VentilationSystem");
            player?.MyPhysics.RpcExitVent(vent.ventId);
        }
    }
}
