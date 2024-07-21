using System.Globalization;
using HarmonyLib;
using InnerNet;
using UnityEngine;
using TownOfHostY.Modules;
using static TownOfHostY.Translator;

namespace TownOfHostY;

class CannotUsePublicRoom
{
    public static string ShowMessage()
    {
        var message = "";
        if (!Main.AllowPublicRoom) message = GetString("DisabledByProgram");
        else if (!Main.IsPublicAvailableOnThisVersion) message = GetString("PublicNotAvailableOnThisVersion");
        else if (ModUpdater.hasUpdate) message = GetString("CanNotJoinPublicRoomNoLatest");
        else if (!VersionChecker.IsSupported) message = GetString("UnsupportedVersion");
        return message;
    }
}

[HarmonyPatch(typeof(GameStartManager), nameof(GameStartManager.MakePublic))]
class MakePublicPatch
{
    public static bool Prefix(GameStartManager __instance)
    {
        // 定数設定による公開ルームブロック
        if (!Main.AllowPublicRoom || ModUpdater.hasUpdate || !VersionChecker.IsSupported || !Main.IsPublicAvailableOnThisVersion)
        {
            var message = CannotUsePublicRoom.ShowMessage();
            Logger.Info(message, "MakePublicPatch");
            Logger.SendInGame(message);
            return false;
        }

        return true;
    }
}
[HarmonyPatch(typeof(MMOnlineManager), nameof(MMOnlineManager.Start))]
class MMOnlineManagerStartPatch
{
    public static void Postfix(MMOnlineManager __instance)
    {
        if (VersionChecker.IsSupported && Main.AllowPublicRoom && Main.IsPublicAvailableOnThisVersion) return;

        var objF = GameObject.Find("Buttons/FindGameButton");
        if (objF)
        {
            objF?.SetActive(false);
        }
        var objJ = GameObject.Find("Buttons/JoinGameButton");
        if (objJ)
        {
            objJ?.SetActive(false);
        }

        var objmenu = GameObject.Find("NormalMenu");
        if (objmenu)
        {
            var objW = new GameObject("ModWarning");
            objW.transform.SetParent(objmenu.transform);
            objW.transform.localPosition = new Vector3(0f, -0.7f, 0f);
            objW.transform.localScale = new Vector3(2f, 2f, 2f);
            var renderer = objW.AddComponent<SpriteRenderer>();
            renderer.sprite = Utils.LoadSprite($"TownOfHost_Y.Resources.warning_online.png", 400f);
        }
    }
}
[HarmonyPatch(typeof(SplashManager), nameof(SplashManager.Update))]
class SplashLogoAnimatorPatch
{
    public static void Prefix(SplashManager __instance)
    {
        if (DebugModeManager.AmDebugger)
        {
            __instance.sceneChanger.AllowFinishLoadingScene();
            __instance.startedSceneLoad = true;
        }
    }
}
[HarmonyPatch(typeof(EOSManager), nameof(EOSManager.IsAllowedOnline))]
class RunLoginPatch
{
    public static void Prefix(ref bool canOnline)
    {
#if DEBUG
        if (CultureInfo.CurrentCulture.Name != "ja-JP") canOnline = false;
#endif
    }
}
[HarmonyPatch(typeof(BanMenu), nameof(BanMenu.SetVisible))]
class BanMenuSetVisiblePatch
{
    public static bool Prefix(BanMenu __instance, bool show)
    {
        if (!AmongUsClient.Instance.AmHost) return true;
        show &= PlayerControl.LocalPlayer && PlayerControl.LocalPlayer.Data != null;
        __instance.BanButton.gameObject.SetActive(AmongUsClient.Instance.CanBan());
        __instance.KickButton.gameObject.SetActive(AmongUsClient.Instance.CanKick());
        __instance.MenuButton.gameObject.SetActive(show);
        return false;
    }
}
[HarmonyPatch(typeof(InnerNet.InnerNetClient), nameof(InnerNet.InnerNetClient.CanBan))]
class InnerNetClientCanBanPatch
{
    public static bool Prefix(InnerNet.InnerNetClient __instance, ref bool __result)
    {
        __result = __instance.AmHost;
        return false;
    }
}
[HarmonyPatch(typeof(InnerNet.InnerNetClient), nameof(InnerNet.InnerNetClient.KickPlayer))]
class KickPlayerPatch
{
    public static void Prefix(InnerNet.InnerNetClient __instance, int clientId, bool ban)
    {
        if (!AmongUsClient.Instance.AmHost) return;
        if (ban) BanManager.AddBanPlayer(AmongUsClient.Instance.GetRecentClient(clientId));
    }
}
[HarmonyPatch(typeof(InnerNetClient), nameof(InnerNetClient.SendAllStreamedObjects))]
class InnerNetObjectSerializePatch
{
    public static void Prefix()
    {
        if (AmongUsClient.Instance.AmHost)
            GameOptionsSender.SendAllGameOptions();
    }
}