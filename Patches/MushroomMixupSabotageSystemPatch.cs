using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using Hazel;
using TownOfHostY.Roles.Core;
using TownOfHostY.Roles.Core.Interfaces;
using TownOfHostY.Roles.Neutral;

namespace TownOfHostY;

[HarmonyPatch(typeof(MushroomMixupSabotageSystem), nameof(MushroomMixupSabotageSystem.UpdateSystem))]
public static class MushroomMixupUpdateSystemPatch
{
    public static List<PlayerControl> TargetPlayers = new();
    public static List<PlayerControl> ChangedPlayers = new();
    public static void Prefix(MushroomMixupSabotageSystem __instance, PlayerControl player, MessageReader msgReader)
    {
        TargetPlayers.Clear();
        ChangedPlayers.Clear();
        foreach (PlayerControl pc in Main.AllAlivePlayerControls)
        {
            TargetPlayers.Add(pc);
        }

        foreach (PlayerControl pc in Main.AllAlivePlayerControls)
        {
            var role = pc.GetRoleClass();
            if (role is IKiller && role is not IImpostor &&
                !pc.Is(CustomRoles.Egoist) &&
                !(pc.Is(CustomRoles.Jackal) && Jackal.CanSeeNameMushroomMixup))
            {
                ChangedPlayers.Add(pc);
                foreach (PlayerControl target in TargetPlayers)
                {
                    target.RpcSetNamePrivate("<color=#00000000>.", seer: pc, force: true);
                }
            }
        }
    }
}
[HarmonyPatch(typeof(MushroomMixupSabotageSystem), nameof(MushroomMixupSabotageSystem.Deteriorate))]
public static class MushroomMixupDeterioratePatch
{
    public static void Prefix(MushroomMixupSabotageSystem __instance, float deltaTime)
    {
        if (!__instance.IsActive) return;
        if ((double)__instance.currentSecondsUntilHeal - deltaTime > 0.0) return;

        foreach (var changed in MushroomMixupUpdateSystemPatch.ChangedPlayers.Where(x => x != null))
        {
            foreach (var target in MushroomMixupUpdateSystemPatch.TargetPlayers.Where(x => x != null))
            {
                target.RpcSetNamePrivate(Main.AllPlayerNames[target.PlayerId], seer: changed, force: true);
            }
        }
        Utils.NotifyRoles();
    }
}

