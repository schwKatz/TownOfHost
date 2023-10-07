using HarmonyLib;

namespace TownOfHostY.Patches;

[HarmonyPatch(typeof(Constants), nameof(Constants.GetBroadcastVersion))]
class GetBroadcastVersionPatch
{
    static void Postfix(ref int __result)
    {
        if (GameStates.IsLocalGame) return;

        __result += 25;
    }
}
