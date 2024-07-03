using HarmonyLib;
using Hazel;
using TownOfHostY.Roles.Core;
using TownOfHostY.Roles.Core.Interfaces;
using TownOfHostY.Roles.Crewmate;
using TownOfHostY.Roles.Neutral;

namespace TownOfHostY.Patches.ISystemType;

[HarmonyPatch(typeof(HqHudSystemType), nameof(HqHudSystemType.UpdateSystem))]
public static class HqHudSystemTypeUpdateSystemPatch
{
    public static bool Prefix(HqHudSystemType __instance, [HarmonyArgument(0)] PlayerControl player, [HarmonyArgument(1)] MessageReader msgReader)
    {
        byte amount;
        {
            var newReader = MessageReader.Get(msgReader);
            amount = newReader.ReadByte();
            newReader.Recycle();
        }

        var tags = (HqHudSystemType.Tags)(amount & HqHudSystemType.TagMask);
        var playerRole = player.GetRoleClass();
        var isMadmate =
            player.Is(CustomRoles.SKMadmate) ||
            // マッド属性化時に削除
            (playerRole is SchrodingerCat schrodingerCat && schrodingerCat.AmMadmate);
        if (tags == HqHudSystemType.Tags.FixBit && ((isMadmate && !Options.MadmateCanFixComms.GetBool())
                                                    || player.Is(CustomRoles.Clumsy)
                                                    || (player.Is(CustomRoles.Sheriff) && Sheriff.IsClumsy.GetBool())
                                                    || (player.Is(CustomRoles.SillySheriff) && SillySheriff.IsClumsy.GetBool())
                                                    || (player.Is(CustomRoles.Hunter) && Hunter.IsClumsy.GetBool())))
        {
            return false;
        }

        if (playerRole is ISystemTypeUpdateHook systemTypeUpdateHook && !systemTypeUpdateHook.UpdateHqHudSystem(__instance, amount))
        {
            return false;
        }
        return true;
    }
    public static void Postfix()
    {
        Camouflage.CheckCamouflage();
        Utils.NotifyRoles();
    }
}
