using HarmonyLib;

using TownOfHostY.Roles.Core;
using TownOfHostY.Patches;

namespace TownOfHostY
{
    //参考元:https://github.com/yukieiji/ExtremeRoles/blob/master/ExtremeRoles/Patches/AirShipStatusPatch.cs
    [HarmonyPatch(typeof(AirshipStatus), nameof(AirshipStatus.PrespawnStep))]
    public static class AirshipStatusPrespawnStepPatch
    {
        public static bool Prefix()
        {
            if (PlayerControl.LocalPlayer.Is(CustomRoles.GM) && // GMは湧き画面をスキップ
                RandomSpawn.CustomNetworkTransformPatch.NumOfTP[PlayerControl.LocalPlayer.PlayerId] != 0)
                return false;

            return true;
        }
    }
}