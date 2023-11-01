using HarmonyLib;
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
            if (Options.FixFirstKillCooldown.GetBool() && !MeetingStates.MeetingCalled)
            {
                PlayerControl.LocalPlayer.SetKillCooldown(Main.AllPlayerKillCooldown[PlayerControl.LocalPlayer.PlayerId]);
            }
            if (Options.RandomSpawn.GetBool())
            {
                new RandomSpawn.AirshipSpawnMap().RandomTeleport(PlayerControl.LocalPlayer);
            }
        }
    }
}