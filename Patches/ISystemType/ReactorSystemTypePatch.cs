using HarmonyLib;
using Hazel;
using TownOfHostY.Roles.Core;
using TownOfHostY.Roles.Core.Interfaces;

namespace TownOfHostY.Patches.ISystemType;

[HarmonyPatch(typeof(ReactorSystemType), nameof(ReactorSystemType.UpdateSystem))]
public static class ReactorSystemTypeUpdateSystemPatch
{
    public static bool Prefix(ReactorSystemType __instance, [HarmonyArgument(0)] PlayerControl player, [HarmonyArgument(1)] MessageReader msgReader)
    {
        byte amount;
        {
            var newReader = MessageReader.Get(msgReader);
            amount = newReader.ReadByte();
            newReader.Recycle();
        }

        if (player.GetRoleClass() is ISystemTypeUpdateHook systemTypeUpdateHook && !systemTypeUpdateHook.UpdateReactorSystem(__instance, amount))
        {
            return false;
        }
        return true;
    }
}

//参考
//https://github.com/Koke1024/Town-Of-Moss/blob/main/TownOfMoss/Patches/MeltDownBoost.cs

[HarmonyPatch(typeof(ReactorSystemType), nameof(ReactorSystemType.Deteriorate))]
public static class ReactorSystemDetetiorateTypePatch
{
    public static void Prefix(ReactorSystemType __instance)
    {
        if (!__instance.IsActive || !Options.SabotageTimeControl.GetBool())
            return;
        if (ShipStatus.Instance.Type == ShipStatus.MapType.Pb)
        {
            if (__instance.Countdown >= Options.PolusReactorTimeLimit.GetFloat())
                __instance.Countdown = Options.PolusReactorTimeLimit.GetFloat();
            return;
        }
        else if (ShipStatus.Instance.Type == ShipStatus.MapType.Fungle)
        {
            if (__instance.Countdown >= Options.FungleReactorTimeLimit.GetFloat())
                __instance.Countdown = Options.FungleReactorTimeLimit.GetFloat();
        }
        return;
    }
}
