using System;
using System.Linq;
using HarmonyLib;
using System.Collections.Generic;
using TMPro;
using TownOfHostY.Roles.Core;
using UnityEngine;
using Object = UnityEngine.Object;

namespace TownOfHostY;
[HarmonyPatch(typeof(GameSettingMenu))]
public class GameSettingMenuPatch
{
    enum GameSettingMenuTab
    {
        GamePresets = 0,
        GameSettings,
        RoleSettings,
        Mod_MainSettings,
        //Mod_ImpostorSettings,

        MaxCount,
    }

    public static string[] buttonName = new string[]{
        "Y_Main Setting",
        //"Y_Impostor Setting",
    };
    public static string[] tabName = new string[]{
        "mainSettingTab",
        //"Y_Impostor Setting",
    };

    static PassiveButton ModSettingsButton;
    static GameOptionsMenu ModSettingsTab;
    //static GameOptionsMenu[] ModSettingsTab = new GameOptionsMenu[(int)GameSettingMenuTab.MaxCount - 3];
    //static PassiveButton[] ModSettingsButton = new PassiveButton[(int)GameSettingMenuTab.MaxCount - 3];
    //static Dictionary<TabGroup, GameOptionsMenu> list = new();

    [HarmonyPatch(typeof(GameSettingMenu), nameof(GameSettingMenu.Start))]
    [HarmonyPriority(Priority.First)]
    public static class StartPatch
    {
        public static void Postfix(GameSettingMenu __instance)
        {
            // ModSettingsTabの作成
            for (int i = 0; i < (int)GameSettingMenuTab.MaxCount - 3; i++)
            {
                var button = ModSettingsButton;
                // MOD設定
                button = Object.Instantiate(__instance.GameSettingsButton, __instance.GameSettingsButton.transform.parent);
                button.name = buttonName[i];
                var label = button.GetComponentInChildren<TextMeshPro>();
                label.DestroyTranslator();
                label.text = buttonName[i];
                button.activeTextColor = Color.yellow;
                button.inactiveTextColor = Color.yellow;
                button.inactiveSprites.GetComponent<SpriteRenderer>().sprite = Utils.LoadSprite("TownOfHost_Y.Resources.TabIcon_MainSettings.png", 100f);
                button.activeSprites.GetComponent<SpriteRenderer>().sprite = Utils.LoadSprite("TownOfHost_Y.Resources.TabIcon_MainSettings.png", 100f);
                button.selectedSprites.GetComponent<SpriteRenderer>().sprite = Utils.LoadSprite("TownOfHost_Y.Resources.TabIcon_MainSettings.png", 100f);
                button.transform.localPosition = new(-3f, -1.2f + -0.7f * i, 0f);
                var buttonComponent = button.GetComponent<PassiveButton>();
                buttonComponent.OnClick = new();
                buttonComponent.OnClick.AddListener(
                    (Action)(() => __instance.ChangeTab((int)GameSettingMenuTab.Mod_MainSettings, false)));
            }
            // プリセット設定 非表示
            __instance.GamePresetsButton.gameObject.SetActive(false);

            // ゲーム設定
            __instance.GameSettingsButton.transform.localPosition = new(-3f, -0.5f, 0f);

            // バニラ役職設定 非表示
            __instance.RoleSettingsButton.gameObject.SetActive(false);

            // 
            Logger.Info("111111", "GameSettingMenu");

            var template = GameObject.Find("Main Camera/PlayerOptionsMenu(Clone)/MainArea/GAME SETTINGS TAB/Scroller/SliderInner/GameOption_String(Clone)").GetComponent<StringOption>();
            if (template == null) return;
            Logger.Info("22222", "GameSettingMenu");

            Logger.Info("33333", "GameSettingMenu");

            //foreach (var tab in EnumHelper.GetAllValues<TabGroup>())
            {
                //if (__instance.name != GameSettingMenuPatch.tabName[(int)tab]) continue;
                ModSettingsTab = Object.Instantiate(__instance.GameSettingsTab, __instance.GameSettingsTab.transform.parent);
                ModSettingsTab.name = tabName[0];
                //var tab = TabGroup.MainSettings;
                //OptionBehaviourを破棄
                ModSettingsTab.GetComponentsInChildren<OptionBehaviour>().Do(x => Object.Destroy(x.gameObject));
            }
            Logger.Info("4444444444", "GameSettingMenu");

            //var tohSettings = Object.Instantiate(gameSettings, gameSettings.transform.parent);
            //tohSettings.name = tab + "Tab";
            //var tohMenu = tohSettings.transform.FindChild("GameGroup/SliderInner").GetComponent<GameOptionsMenu>();

            //foreach (var tab in EnumHelper.GetAllValues<TabGroup>())
            {
                var tab = TabGroup.MainSettings;
                //ModSettingsTab = GameObject.Find(tabName[0]).GetComponent<GameOptionsMenu>(); ;
                Il2CppSystem.Collections.Generic.List<OptionBehaviour> scOptions = new();

                foreach (var option in OptionItem.AllOptions)
                {
                    if (option.Tab != tab) continue;

                    if (option.OptionBehaviour == null)
                    {
                        var stringOption = Object.Instantiate(template, ModSettingsTab.transform);
                        scOptions.Add(stringOption);
                        stringOption.OnValueChanged = new System.Action<OptionBehaviour>((o) => { });
                        stringOption.TitleText.text = option.Name;
                        stringOption.Value = stringOption.oldValue = option.CurrentValue;
                        stringOption.ValueText.text = option.GetString();
                        stringOption.name = option.Name;
                        stringOption.transform.FindChild("LabelBackground").localScale = new Vector3(1.6f, 1f, 1f);
                        stringOption.transform.FindChild("LabelBackground").SetLocalX(-2.2695f);
                        stringOption.transform.FindChild("PlusButton (1)").localPosition += new Vector3(option.IsFixValue ? 100f : 1.1434f, option.IsFixValue ? 100f : 0f, option.IsFixValue ? 100f : 0f);
                        stringOption.transform.FindChild("MinusButton (1)").localPosition += new Vector3(option.IsFixValue ? 100f : 0.3463f, option.IsFixValue ? 100f : 0f, option.IsFixValue ? 100f : 0f);
                        stringOption.transform.FindChild("Value_TMP (1)").localPosition += new Vector3(0.7322f, 0f, 0f);
                        stringOption.transform.FindChild("ValueBox").localScale += new Vector3(0.2f, 0f, 0f);
                        stringOption.transform.FindChild("ValueBox").localPosition += new Vector3(0.7322f, 0f, 0f);
                        stringOption.transform.FindChild("Title Text").localPosition += new Vector3(-1.096f, 0f, 0f);
                        stringOption.transform.FindChild("Title Text").GetComponent<RectTransform>().sizeDelta = new Vector2(6.5f, 0.37f);
                        stringOption.transform.FindChild("Title Text").GetComponent<TMPro.TextMeshPro>().alignment = TMPro.TextAlignmentOptions.MidlineLeft;
                        Logger.Info("555555555555", "GameSettingMenu");
                        stringOption.SetClickMask(ModSettingsTab.ButtonClickMask);

                        option.OptionBehaviour = stringOption;
                    }
                    option.OptionBehaviour.gameObject.SetActive(true);
                }
                Logger.Info("666666666666", "GameSettingMenu");
                {
                    ModSettingsTab.Children = scOptions;
                    Logger.Info("7777777777", "GameSettingMenu");

                    ModSettingsTab.gameObject.SetActive(false);
                    Logger.Info("8888888888888", "GameSettingMenu");
                    ModSettingsTab.enabled = true;
                    Logger.Info("999999999999999", "GameSettingMenu");
                }
            }
            //foreach (var tab in EnumHelper.GetAllValues<TabGroup>())
            {
                var tab = TabGroup.MainSettings;
                Logger.Info("10", "GameSettingMenu");

                
                Logger.Info("111111111111111", "GameSettingMenu");
            }



        }
    }
    [HarmonyPatch(typeof(GameSettingMenu), nameof(GameSettingMenu.ChangeTab))]
    public static class ChangeTabPatch
    {
        public static void Prefix(GameSettingMenu __instance, ref int tabNum, [HarmonyArgument(1)] bool previewOnly)
        {
            if (tabNum == (int)GameSettingMenuTab.GamePresets) {
                tabNum = (int)GameSettingMenuTab.GameSettings;
                __instance.MenuDescriptionText.text = "test";

            }
        }
        public static void Postfix(GameSettingMenu __instance, [HarmonyArgument(0)] int tabNum, [HarmonyArgument(1)] bool previewOnly)
        {
            if (!previewOnly)
            {
                Logger.Info("111111111111111", "ChangeTabPatch");

                //ModSettingsTab = GameObject.Find("Main Camera/PlayerOptionsMenu(Clone)/MainArea/" + tabName[0]).GetComponent<GameOptionsMenu>();
                if (ModSettingsTab == null) return;
                Logger.Info("22222222", "ChangeTabPatch");
                //ModSettingsTab.gameObject.SetActive(false);
                //ModSettingsButton.SelectButton(false);
                Logger.Info("3333333333", "ChangeTabPatch");

                if (tabNum < (int)GameSettingMenuTab.Mod_MainSettings) return;

                ModSettingsTab.gameObject.SetActive(true);
                __instance.MenuDescriptionText.DestroyTranslator();
                __instance.MenuDescriptionText.text = "MODのロールや機能の設定ができる。";
                Logger.Info("5555555555555", "ChangeTabPatch");

                __instance.ToggleLeftSideDarkener(true);
                __instance.ToggleRightSideDarkener(false);
                Logger.Info("66666666666", "ChangeTabPatch");

                ModSettingsTab.OpenMenu();
                ModSettingsButton.SelectButton(true);
            }
        }
    }
}

[HarmonyPatch(typeof(GameOptionsMenu), nameof(GameOptionsMenu.Initialize))]
[HarmonyPriority(Priority.First)]
public static class GameOptionsMenuPatch
{
    public static void Postfix(GameOptionsMenu __instance)
    {
        foreach (var ob in __instance.Children)
        {
            switch (ob.Title)
            {
                case StringNames.GameShortTasks:
                case StringNames.GameLongTasks:
                case StringNames.GameCommonTasks:
                    ob.Cast<NumberOption>().ValidRange = new FloatRange(0, 99);
                    break;
                case StringNames.GameKillCooldown:
                    ob.Cast<NumberOption>().ValidRange = new FloatRange(0, 180);
                    break;
                case StringNames.GameNumImpostors:
                    if (DebugModeManager.IsDebugMode)
                    {
                        ob.Cast<NumberOption>().ValidRange.min = 0;
                    }
                    break;
                default:
                    break;
            }
        }
        if (__instance.transform.name == "GAME SETTINGS TAB") return;

    }
}

[HarmonyPatch(typeof(GameOptionsMenu), nameof(GameOptionsMenu.Update))]
public class GameOptionsMenuUpdatePatch
{
    private static float _timer = 1f;

    public static void Postfix(GameOptionsMenu __instance)
    {
        if (__instance.transform.name == "GAME SETTINGS TAB") return;
        foreach (var tab in EnumHelper.GetAllValues<TabGroup>())
        {
            if (__instance.transform.parent.parent.name != GameSettingMenuPatch.buttonName[(int)tab]) continue;

            //switch (tab)
            //{
            //    case TabGroup.MainSettings:
            //        tabName = "<color=#ffff00>Main Settings</color>";
            //        break;
            //    case TabGroup.ImpostorRoles:
            //        tabName = "<color=#ff0000>Impostor Settings</color>";
            //        break;
            //    case TabGroup.MadmateRoles:
            //        tabName = "<color=#ff4500>Madmate Settings</color>";
            //        break;
            //    case TabGroup.CrewmateRoles:
            //        tabName = "<color=#b6f0ff>Crewmate Settings</color>";
            //        break;
            //    case TabGroup.NeutralRoles:
            //        tabName = "<color=#ffa500>Neutral Settings</color>";
            //        break;
            //    case TabGroup.UnitRoles:
            //        tabName = "<color=#7fff00>Combination-Role Settings</color>";
            //        break;
            //    case TabGroup.Addons:
            //        tabName = "<color=#ee82ee>Add-Ons Settings</color>";
            //        break;
            //}

            _timer += Time.deltaTime;
            if (_timer < 0.1f) return;
            _timer = 0f;

            float numItems = __instance.Children.Count;
            var offset = 2.7f;

            foreach (var option in OptionItem.AllOptions)
            {
                if (tab != option.Tab) continue;
                if (option?.OptionBehaviour == null || option.OptionBehaviour.gameObject == null) continue;

                var enabled = true;
                var parent = option.Parent;

                enabled = AmongUsClient.Instance.AmHost &&
                    !option.IsHiddenOn(Options.CurrentGameMode);

                var opt = option.OptionBehaviour.transform.Find("LabelBackground").GetComponent<SpriteRenderer>();
                opt.size = new(5.0f, 0.45f);
                while (parent != null && enabled)
                {
                    enabled = parent.GetBool() && !parent.IsHiddenOn(Options.CurrentGameMode);
                    parent = parent.Parent;
                    opt.color = new(0f, 1f, 0f);
                    opt.size = new(4.8f, 0.45f);
                    opt.transform.localPosition = new Vector3(0.11f, 0f);
                    option.OptionBehaviour.transform.Find("Title Text").transform.localPosition = new Vector3(-1.08f, 0f);
                    option.OptionBehaviour.transform.FindChild("Title Text").GetComponent<RectTransform>().sizeDelta = new Vector2(5.1f, 0.28f);
                    if (option.Parent?.Parent != null)
                    {
                        opt.color = new(0f, 0f, 1f);
                        opt.size = new(4.6f, 0.45f);
                        opt.transform.localPosition = new Vector3(0.24f, 0f);
                        option.OptionBehaviour.transform.Find("Title Text").transform.localPosition = new Vector3(-0.88f, 0f);
                        option.OptionBehaviour.transform.FindChild("Title Text").GetComponent<RectTransform>().sizeDelta = new Vector2(4.9f, 0.28f);
                        if (option.Parent?.Parent?.Parent != null)
                        {
                            opt.color = new(1f, 0f, 0f);
                            opt.size = new(4.4f, 0.45f);
                            opt.transform.localPosition = new Vector3(0.37f, 0f);
                            option.OptionBehaviour.transform.Find("Title Text").transform.localPosition = new Vector3(-0.68f, 0f);
                            option.OptionBehaviour.transform.FindChild("Title Text").GetComponent<RectTransform>().sizeDelta = new Vector2(4.7f, 0.28f);
                        }
                    }
                }

                if (option.IsText)
                {
                    opt.color = new(0, 0, 0);
                    opt.transform.localPosition = new(100f, 100f, 100f);
                }

                option.OptionBehaviour.gameObject.SetActive(enabled);
                if (enabled)
                {
                    offset -= option.IsHeader ? 0.7f : 0.5f;
                    option.OptionBehaviour.transform.localPosition = new Vector3(
                        option.OptionBehaviour.transform.localPosition.x,
                        offset,
                        option.OptionBehaviour.transform.localPosition.z);

                    if (option.IsHeader)
                    {
                        numItems += 0.3f;
                    }
                }
                else
                {
                    numItems -= 10f;
                }
            }
            __instance.GetComponentInParent<Scroller>().ContentYBounds.max = (-offset) - 1.5f;
        }
    }
}

[HarmonyPatch(typeof(StringOption), nameof(StringOption.Initialize))]
public class StringOptionInitializePatch
{
    public static bool Prefix(StringOption __instance)
    {
        var option = OptionItem.AllOptions.FirstOrDefault(opt => opt.OptionBehaviour == __instance);
        if (option == null) return true;

        __instance.OnValueChanged = new Action<OptionBehaviour>((o) => { });
        __instance.TitleText.text = option.GetName();
        __instance.Value = __instance.oldValue = option.CurrentValue;
        __instance.ValueText.text = option.GetString();

        return false;
    }
}

[HarmonyPatch(typeof(StringOption), nameof(StringOption.Increase))]
    public class StringOptionIncreasePatch
    {
        public static bool Prefix(StringOption __instance)
        {
            var option = OptionItem.AllOptions.FirstOrDefault(opt => opt.OptionBehaviour == __instance);
            if (option == null) return true;

            option.SetValue(option.CurrentValue + (Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift) ? 5 : 1));
            return false;
        }
    }

    [HarmonyPatch(typeof(StringOption), nameof(StringOption.Decrease))]
    public class StringOptionDecreasePatch
    {
        public static bool Prefix(StringOption __instance)
        {
            var option = OptionItem.AllOptions.FirstOrDefault(opt => opt.OptionBehaviour == __instance);
            if (option == null) return true;

            option.SetValue(option.CurrentValue - (Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift) ? 5 : 1));
            return false;
        }
    }

    [HarmonyPatch(typeof(PlayerControl), nameof(PlayerControl.RpcSyncSettings))]
    public class RpcSyncSettingsPatch
    {
        public static void Postfix()
        {
            OptionItem.SyncAllOptions();
        }
    }

    //[HarmonyPatch(typeof(NormalGameOptionsV08), nameof(NormalGameOptionsV08.SetRecommendations))]
    //public static class SetRecommendationsPatch
    //{
    //    public static bool Prefix(NormalGameOptionsV08 __instance, int numPlayers, bool isOnline)
    //    {
    //        numPlayers = Mathf.Clamp(numPlayers, 4, 15);
    //        __instance.PlayerSpeedMod = __instance.MapId == 4 ? 1.25f : 1f; //AirShipなら1.25、それ以外は1
    //        __instance.CrewLightMod = 0.5f;
    //        __instance.ImpostorLightMod = 1.75f;
    //        __instance.KillCooldown = 25f;
    //        __instance.NumCommonTasks = 2;
    //        __instance.NumLongTasks = 4;
    //        __instance.NumShortTasks = 6;
    //        __instance.NumEmergencyMeetings = 1;
    //        if (!isOnline)
    //            __instance.NumImpostors = NormalGameOptionsV08.RecommendedImpostors[numPlayers];
    //        __instance.KillDistance = 0;
    //        __instance.DiscussionTime = 0;
    //        __instance.VotingTime = 150;
    //        __instance.IsDefaults = true;
    //        __instance.ConfirmImpostor = false;
    //        __instance.VisualTasks = false;

    //        __instance.roleOptions.SetRoleRate(RoleTypes.Shapeshifter, 0, 0);
    //        __instance.roleOptions.SetRoleRate(RoleTypes.Scientist, 0, 0);
    //        __instance.roleOptions.SetRoleRate(RoleTypes.GuardianAngel, 0, 0);
    //        __instance.roleOptions.SetRoleRate(RoleTypes.Engineer, 0, 0);
    //        __instance.roleOptions.SetRoleRecommended(RoleTypes.Shapeshifter);
    //        __instance.roleOptions.SetRoleRecommended(RoleTypes.Scientist);
    //        __instance.roleOptions.SetRoleRecommended(RoleTypes.GuardianAngel);
    //        __instance.roleOptions.SetRoleRecommended(RoleTypes.Engineer);

    //        if (Options.CurrentGameMode == CustomGameMode.HideAndSeek) //HideAndSeek
    //        {
    //            __instance.PlayerSpeedMod = 1.75f;
    //            __instance.CrewLightMod = 5f;
    //            __instance.ImpostorLightMod = 0.25f;
    //            __instance.NumImpostors = 1;
    //            __instance.NumCommonTasks = 0;
    //            __instance.NumLongTasks = 0;
    //            __instance.NumShortTasks = 10;
    //            __instance.KillCooldown = 10f;
    //        }
    //        if (Options.IsStandardHAS) //StandardHAS
    //        {
    //            __instance.PlayerSpeedMod = 1.75f;
    //            __instance.CrewLightMod = 5f;
    //            __instance.ImpostorLightMod = 0.25f;
    //            __instance.NumImpostors = 1;
    //            __instance.NumCommonTasks = 0;
    //            __instance.NumLongTasks = 0;
    //            __instance.NumShortTasks = 10;
    //            __instance.KillCooldown = 10f;
    //        }
    //        if (Options.IsCCMode)
    //        {
    //            __instance.PlayerSpeedMod = 1.5f;
    //            __instance.CrewLightMod = 0.5f;
    //            __instance.ImpostorLightMod = 0.75f;
    //            __instance.NumImpostors = 1;
    //            __instance.NumCommonTasks = 0;
    //            __instance.NumLongTasks = 0;
    //            __instance.NumShortTasks = 1;
    //            __instance.KillCooldown = 20f;
    //            __instance.NumEmergencyMeetings = 1;
    //            __instance.EmergencyCooldown = 30;
    //            __instance.KillDistance = 0;
    //            __instance.DiscussionTime = 0;
    //            __instance.VotingTime = 60;
    //        }
    //        //if (Options.IsONMode)
    //        //{
    //        //    __instance.NumCommonTasks = 1;
    //        //    __instance.NumLongTasks = 0;
    //        //    __instance.NumShortTasks = 1;
    //        //    __instance.KillCooldown = 20f;
    //        //    __instance.NumEmergencyMeetings = 0;
    //        //    __instance.KillDistance = 0;
    //        //    __instance.DiscussionTime = 0;
    //        //    __instance.VotingTime = 300;
    //        //}

    //        return false;
    //    }
    //}
//}