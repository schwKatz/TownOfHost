using HarmonyLib;

namespace TownOfHostY
{
    [HarmonyPatch(typeof(AmongUsClient), nameof(AmongUsClient.OnDisconnected))]
    class OnDisconnectedPatch
    {
        public static void Postfix(AmongUsClient __instance)
        {
            Main.VisibleTasksCount = false;
        }
    }
}