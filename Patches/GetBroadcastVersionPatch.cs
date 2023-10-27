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

[HarmonyPatch(typeof(Constants), nameof(Constants.IsVersionModded))]
public static class IsVersionModdedPatch
{
    public static bool Prefix(ref bool __result)
    {
        __result = true;
        return false;
    }
}