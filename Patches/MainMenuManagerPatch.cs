using System;
using HarmonyLib;
using UnityEngine;

namespace TownOfHost
{
    [HarmonyPatch]
    public class MainMenuManagerPatch
    {
        public static GameObject template;
        //public static GameObject discordButton;
        public static GameObject githubButton;

        [HarmonyPatch(typeof(MainMenuManager), nameof(MainMenuManager.Start)), HarmonyPrefix]
        public static void Start_Prefix(MainMenuManager __instance)
        {
            if (template == null) template = GameObject.Find("/MainUI/ExitGameButton");
            if (template == null) return;

            //GitHubボタンを生成
            {
                if (githubButton == null) githubButton = UnityEngine.Object.Instantiate(template, template.transform.parent);
                githubButton.name = "GithubButton";
                githubButton.transform.position = Vector3.Reflect(template.transform.position, Vector3.left);

                var githubText = githubButton.transform.GetChild(0).GetComponent<TMPro.TMP_Text>();
                Color githubColor = new Color32(161, 161, 161, byte.MaxValue);
                PassiveButton githubPassiveButton = githubButton.GetComponent<PassiveButton>();
                SpriteRenderer githubButtonSprite = githubButton.GetComponent<SpriteRenderer>();
                githubPassiveButton.OnClick = new();
                githubPassiveButton.OnClick.AddListener((Action)(() => Application.OpenURL("https://github.com/Yumenopai/TownOfHost_Y")));
                githubPassiveButton.OnMouseOut.AddListener((Action)(() => githubButtonSprite.color = githubText.color = githubColor));
                __instance.StartCoroutine(Effects.Lerp(0.01f, new Action<float>((p) => githubText.SetText("GitHub"))));
                githubButtonSprite.color = githubText.color = githubColor;
                githubButton.gameObject.SetActive(true);
            }

            //ハウトゥプレイの無効化 discord
            var howtoplayButton = GameObject.Find("/MainUI/HowToPlayButton");
            if (howtoplayButton != null)
            {
                var discordText = howtoplayButton.transform.GetChild(0).GetComponent<TMPro.TMP_Text>();
                Color discordColor = new Color32(88, 101, 242, byte.MaxValue);
                PassiveButton discordPassiveButton = howtoplayButton.GetComponent<PassiveButton>();
                SpriteRenderer discordButtonSprite = howtoplayButton.GetComponent<SpriteRenderer>();
                discordPassiveButton.OnClick = new();
                discordPassiveButton.OnClick.AddListener((Action)(() => Application.OpenURL(Main.DiscordInviteUrl)));
                discordPassiveButton.OnMouseOut.AddListener((Action)(() => discordButtonSprite.color = discordText.color = discordColor));
                __instance.StartCoroutine(Effects.Lerp(0.01f, new Action<float>((p) => discordText.SetText("Discord"))));
                discordButtonSprite.color = discordText.color = discordColor;
            }
            //フリープレイの無効化 Twitter
            var freeplayButton = GameObject.Find("/MainUI/FreePlayButton");
            if (freeplayButton != null)
            {
                var twitterText = freeplayButton.transform.GetChild(0).GetComponent<TMPro.TMP_Text>();
                Color twitterColor = new Color32(29, 161, 242, byte.MaxValue);
                PassiveButton twitterPassiveButton = freeplayButton.GetComponent<PassiveButton>();
                SpriteRenderer twitterButtonSprite = freeplayButton.GetComponent<SpriteRenderer>();
                twitterPassiveButton.OnClick = new();
                twitterPassiveButton.OnClick.AddListener((Action)(() => Application.OpenURL("https://twitter.com/yumeno_AmongUs")));
                twitterPassiveButton.OnMouseOut.AddListener((Action)(() => twitterButtonSprite.color = twitterText.color = twitterColor));
                __instance.StartCoroutine(Effects.Lerp(0.01f, new Action<float>((p) => twitterText.SetText("Twitter"))));
                twitterButtonSprite.color = twitterText.color = twitterColor;
            }
        }
    }
}