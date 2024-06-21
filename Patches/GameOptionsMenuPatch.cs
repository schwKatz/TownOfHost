using System;
using System.Linq;
using HarmonyLib;
using System.Collections.Generic;
using TMPro;
using TownOfHostY.Roles.Core;
using UnityEngine;
using Object = UnityEngine.Object;
using static UnityEngine.RemoteConfigSettingsHelper;

namespace TownOfHostY;
[HarmonyPatch(typeof(GameSettingMenu))]
public class GameSettingMenuPatch
{
    public enum GameSettingMenuTab
    {
        GamePresets = 0,
        GameSettings,
        RoleSettings,
        Mod_MainSettings,
        Mod_ImpostorRoles,
        Mod_MadmateRoles,
        Mod_CrewmateRoles,
        Mod_NeutralRoles,
        Mod_UnitRoles,
        Mod_AddOns,

        MaxCount,
    }

    public static string[] buttonName = new string[]{
        "Game Setup",
        "Mod Setup",
        "Impostors",
        "Madmates",
        "Crewmates",
        "Neutrals",
        "Unit Roles",
        "Add-Ons"
    };

    static Dictionary<TabGroup, PassiveButton> ModSettingsButtons = new();
    static Dictionary<TabGroup, GameOptionsMenu> ModSettingsTabs = new();

    [HarmonyPatch(typeof(GameSettingMenu), nameof(GameSettingMenu.Start))]
    [HarmonyPriority(Priority.First)]
    public static class StartPatch
    {
        public static void Postfix(GameSettingMenu __instance)
        {
            Vector3 buttonPosition_Left = new(-3.9f, -0.4f, 0f);
            Vector3 buttonPosition_Right = new(-2.4f, -0.4f, 0f);
            Vector3 buttonSize = new(0.45f, 0.55f, 1f);

            // 設定ボタン(左側)
            foreach (var tab in EnumHelper.GetAllValues<TabGroup>())
            {
                var button = Object.Instantiate(__instance.GameSettingsButton, __instance.GameSettingsButton.transform.parent);
                button.name = "Button_" + buttonName[(int)tab + 1];
                var label = button.GetComponentInChildren<TextMeshPro>();
                label.DestroyTranslator();
                label.text = buttonName[(int)tab + 1];
                button.activeTextColor = Color.black;
                button.inactiveTextColor = Color.black;

                var tabSprite = Utils.LoadSprite($"TownOfHost_Y.Resources.SettingTab_{tab}.png", 100f);
                button.inactiveSprites.GetComponent<SpriteRenderer>().sprite = tabSprite;
                button.activeSprites.GetComponent<SpriteRenderer>().sprite = tabSprite;
                button.selectedSprites.GetComponent<SpriteRenderer>().sprite = tabSprite;

                Vector3 offset = new (0.0f, 0.5f * (((int)tab + 1) / 2), 0.0f);
                button.transform.localPosition = ((((int)tab + 1) % 2 == 0) ? buttonPosition_Left : buttonPosition_Right) - offset;
                button.transform.localScale = buttonSize;

                var buttonComponent = button.GetComponent<PassiveButton>();
                buttonComponent.OnClick = new();
                buttonComponent.OnClick.AddListener(
                    (Action)(() => __instance.ChangeTab((int)tab + 3, false)));

                // ボタン登録
                ModSettingsButtons.Add(tab, button);
            }
            // プリセット設定 非表示
            __instance.GamePresetsButton.gameObject.SetActive(false);

            // ゲーム設定
            __instance.GameSettingsButton.transform.localPosition = new(-3f, -0.5f, 0f);
            var textLabel = __instance.GameSettingsButton.GetComponentInChildren<TextMeshPro>();
            textLabel.DestroyTranslator();
            textLabel.text = buttonName[0];
            __instance.GameSettingsButton.activeTextColor = Color.black;
            __instance.GameSettingsButton.inactiveTextColor = Color.black;

            var vanillaTabSprite = Utils.LoadSprite($"TownOfHost_Y.Resources.SettingTab_VanillaGameSettings.png", 100f);
            __instance.GameSettingsButton.inactiveSprites.GetComponent<SpriteRenderer>().sprite = vanillaTabSprite;
            __instance.GameSettingsButton.activeSprites.GetComponent<SpriteRenderer>().sprite = vanillaTabSprite;
            __instance.GameSettingsButton.selectedSprites.GetComponent<SpriteRenderer>().sprite = vanillaTabSprite;
            __instance.GameSettingsButton.transform.localPosition = buttonPosition_Left;
            __instance.GameSettingsButton.transform.localScale = buttonSize;


            // バニラ役職設定 非表示
            __instance.RoleSettingsButton.gameObject.SetActive(false);


            var templateStringOption = GameObject.Find("Main Camera/PlayerOptionsMenu(Clone)/MainArea/GAME SETTINGS TAB/Scroller/SliderInner/GameOption_String(Clone)").GetComponent<StringOption>();
            if (templateStringOption == null) return;

            foreach (var tab in EnumHelper.GetAllValues<TabGroup>())
            {
                var setTab = Object.Instantiate(__instance.GameSettingsTab, __instance.GameSettingsTab.transform.parent);
                setTab.name = ((GameSettingMenuTab)tab + 3).ToString();
                // 中身を削除
                setTab.GetComponentsInChildren<OptionBehaviour>().Do(x => Object.Destroy(x.gameObject));
                setTab.GetComponentsInChildren<CategoryHeaderMasked>().Do(x => Object.Destroy(x.gameObject));

                ModSettingsTabs.Add(tab, setTab);
            }

            foreach (var tab in EnumHelper.GetAllValues<TabGroup>())
            {
                Il2CppSystem.Collections.Generic.List<OptionBehaviour> scOptions = new();

                foreach (var option in OptionItem.AllOptions)
                {
                    if (option.Tab != tab) continue;

                    if (option.OptionBehaviour == null)
                    {
                        var stringOption = Object.Instantiate(templateStringOption, GameObject.Find($"{ModSettingsTabs[tab].name}/Scroller/SliderInner").transform);
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
                        stringOption.SetClickMask(ModSettingsTabs[tab].ButtonClickMask);

                        option.OptionBehaviour = stringOption;
                    }
                    option.OptionBehaviour.gameObject.SetActive(true);
                }

                ModSettingsTabs[tab].Children = scOptions;
                ModSettingsTabs[tab].gameObject.SetActive(false);
                ModSettingsTabs[tab].enabled = true;
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
                //__instance.MenuDescriptionText.text = "test";
            }
        }
        public static void Postfix(GameSettingMenu __instance, [HarmonyArgument(0)] int tabNum, [HarmonyArgument(1)] bool previewOnly)
        {
            if (!previewOnly)
            {

                if (ModSettingsTabs == null) return;
                // 追加したTabの非表示(全リセット)
                ModSettingsTabs.Do(x => x.Value.gameObject.SetActive(false));
                ModSettingsButtons.Do(x => x.Value.SelectButton(false));

                // MODではない設定を次に表示させるときはここで終わり
                if (tabNum < (int)GameSettingMenuTab.Mod_MainSettings) return;

                // 次表示がMODで追加されたタブの場合の設定
                ModSettingsTabs[(TabGroup)tabNum - 3].gameObject.SetActive(true);
                __instance.MenuDescriptionText.DestroyTranslator();
                __instance.MenuDescriptionText.text = "MODのロールや機能の設定ができる。";

                __instance.ToggleLeftSideDarkener(true);
                __instance.ToggleRightSideDarkener(false);

                ModSettingsTabs[(TabGroup)tabNum - 3].OpenMenu();
                ModSettingsButtons[(TabGroup)tabNum - 3].SelectButton(true);
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
            if (__instance.transform.name != ((GameSettingMenuPatch.GameSettingMenuTab)(tab + 3)).ToString()) continue;

            _timer += Time.deltaTime;
            if (_timer < 0.1f) return;
            _timer = 0f;

            float numItems = __instance.Children.Count;
            var offset = 2.7f;
            var y = 0.713f;

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
                    opt.size = new(4.6f, 0.45f);
                    //opt.transform.localPosition = new Vector3(0.11f, 0f);
                    option.OptionBehaviour.transform.Find("Title Text").transform.localPosition = new Vector3(-1.8566f, 0f);
                    option.OptionBehaviour.transform.FindChild("Title Text").GetComponent<RectTransform>().sizeDelta = new Vector2(6.4f, 0.37f);

                    if (option.Parent?.Parent != null)
                    {
                        opt.color = new(0f, 0f, 1f);
                        opt.size = new(4.4f, 0.45f);
                        //opt.transform.localPosition = new Vector3(0.24f, 0f);
                        option.OptionBehaviour.transform.Find("Title Text").transform.localPosition = new Vector3(-1.4566f, 0f);
                        option.OptionBehaviour.transform.FindChild("Title Text").GetComponent<RectTransform>().sizeDelta = new Vector2(6.3f, 0.37f);

                        if (option.Parent?.Parent?.Parent != null)
                        {
                            opt.color = new(1f, 0f, 0f);
                            opt.size = new(4.2f, 0.45f);
                            //opt.transform.localPosition = new Vector3(0.37f, 0f);
                            option.OptionBehaviour.transform.Find("Title Text").transform.localPosition = new Vector3(-1.6566f, 0f);
                            option.OptionBehaviour.transform.FindChild("Title Text").GetComponent<RectTransform>().sizeDelta = new Vector2(6.2f, 0.37f);
                        }
                    }
                }

                if (option.IsText)
                {
                    opt.color = new(0, 0, 0);
                    //opt.transform.localPosition = new(100f, 100f, 100f);
                }

                option.OptionBehaviour.gameObject.SetActive(enabled);
                if (enabled)
                {
                    offset -= option.IsHeader ? 0.48f : 0.45f;
                    option.OptionBehaviour.transform.localPosition = new Vector3(0.952f, y, -120f);
                    y -= option.IsHeader ? 0.48f : 0.45f;

                    if (option.IsHeader)
                    {
                        numItems += 0.5f;
                    }
                }
                else
                {
                    numItems -= 10f;
                }
            }

            // TODO: 今動かずにエラー吐いてそう
            //__instance.GetComponentInParent<Scroller>().ContentYBounds.max = (-offset) - 1.5f;
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