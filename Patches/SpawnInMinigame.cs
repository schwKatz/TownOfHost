using System.Linq;
using HarmonyLib;
using TownOfHostY.Roles.Core;

namespace TownOfHostY.Patches;

// reference :: TOH-H Patches/SpawnInMinigamePatch.cs
[HarmonyPatch(typeof(SpawnInMinigame), nameof(SpawnInMinigame.SpawnAt))]
public static class SpawnInMinigameSpawnAtPatch
{
    public static void Postfix()
    {
        if (AmongUsClient.Instance.AmHost)
        {
            PlayerControl.LocalPlayer.RpcResetAbilityCooldown();
            if (Options.FixFirstKillCooldown.GetBool() && MeetingStates.FirstMeeting)
            {
                PlayerControl.LocalPlayer.SetKillCooldown(Main.AllPlayerKillCooldown[PlayerControl.LocalPlayer.PlayerId]);
            }
            else if (Main.isProtectRoleExist) Utils.ProtectedFirstPlayer(MeetingStates.FirstMeeting);
            if (Options.RandomSpawn.GetBool())
            {
                new RandomSpawn.AirshipSpawnMap().RandomTeleport(PlayerControl.LocalPlayer);
            }
        }
    }
}