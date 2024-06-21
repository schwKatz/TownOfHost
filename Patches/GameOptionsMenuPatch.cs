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

[HarmonyPatch(typeof(GameOptionsMenu), nameof(GameOptionsMenu.Initialize))]
[HarmonyPriority(Priority.First)]
public static class GameOptionsMenuPatch
{
    public static void Postfix(GameOptionsMenu __instance)
    {
        if (__instance.transform.name != "GAME SETTINGS TAB") return;

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
        // デフォルトのタブには変更を加えないため返す
        if (__instance.transform.name == "GAME SETTINGS TAB") return;

        // 選択されるタブを使用する
        foreach (var tab in EnumHelper.GetAllValues<TabGroup>())
        {
            // 設定されている名前と一致する（＝選択されているタブである）
            if (__instance.transform.name != ((GameSettingMenuPatch.GameSettingMenuTab)(tab + 3)).ToString()) continue;

            _timer += Time.deltaTime;
            if (_timer < 0.1f) return;
            _timer = 0f;


            float numItems = __instance.Children.Count;
            var offset = 2.7f;
            var y = 0.713f;

            // 全てのオプションをまわす
            foreach (var option in OptionItem.AllOptions)
            {
                // オプションのタブと一致するグループのみ通す
                if (tab != option.Tab) continue;
                // ビヘイビアが設定済み、objectがある
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

                // ヘッダー用（TODO:HeaderMaskに移動？ ）
                if (option.IsText)
                {
                    opt.color = new(0, 0, 0);
                    //opt.transform.localPosition = new(100f, 100f, 100f);
                }
                // 有効である者のみ表示する
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
