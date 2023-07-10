using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Runtime.CompilerServices;

namespace TownOfHostY
{
    public static class SendDiscord
    {
        public static string HostRandomName = "Randomer";

        public enum MassageType
        {
            Impostor,
            Madmate,
            Crewmate,
            Neutral,
            Result,
            Feature,
        }

        public static void SendWebhook(MassageType massageType, string text, string userName = "Town Of Host_Y")
        {
            HttpClient client = new();
            Dictionary<string, string> message = new()
            {
                { "content", text },
                { "username", userName },
                { "avatar_url", null }
            };
            string webhookUrlImpo = "https://discord.com/api/webhooks/1124908306360709180/5eZhcjeF2m3jvF8mzC19H4HQS_hKc6FjAndzR-RAIRzaHpx7kkZEwjMnlfbFBHga6O8G";
            string webhookUrlMadm = "https://discord.com/api/webhooks/1124932555293085726/jDX9xTMEkCwL8zw3cf4N9QX0YlcpLK5Hn1x5lP0ueZ1xXwqIQjCTt6_tTlMusYWje8Ee";
            string webhookUrlCrew = "https://discord.com/api/webhooks/1124932006262865961/E3MHi8bEDPlEmveLDjM_hvYQFMxf0B5nHGvmRykSBVw_W70D5U-rrZDezQPFd6Lh5sJT";
            string webhookUrlNeut = "https://discord.com/api/webhooks/1124932317819973632/dx4QbzWfnwKVwN4EY57J3-v9LJqVPyW8MrbTsj_f6KsGPc1nPmfeIPrrs9VLxM7GMlac";
            string webhookUrlFeat = "https://discord.com/api/webhooks/1124932725422424094/jGtG3_Tqa4JZTU_EJnWevacx6OD1E-FVkYspnAEIKSgLSK-alJUiLYLeEELtKJvOBdfo";
            string webhookUrlResu = "https://discord.com/api/webhooks/1124933672454340698/YmWM9Qv7R_i7GP5DOPakqLPtPkfNMhX3pXBnLNT_yDWJWi6NMaiu1022O2Zl2PY66GYH";
            string webhookUrl = webhookUrlFeat;
            switch(massageType)
            {
                case MassageType.Feature: webhookUrl = webhookUrlFeat; break;
                case MassageType.Result: webhookUrl = webhookUrlResu; break;
                case MassageType.Impostor: webhookUrl = webhookUrlImpo; break;
                case MassageType.Madmate: webhookUrl = webhookUrlMadm; break;
                case MassageType.Crewmate: webhookUrl = webhookUrlCrew; break;
                case MassageType.Neutral: webhookUrl = webhookUrlNeut; break;
            }
            try
            {
                TaskAwaiter<HttpResponseMessage> awaiter = client.PostAsync(webhookUrl, new FormUrlEncodedContent(message)).GetAwaiter();
                var response = awaiter.GetResult();
                Logger.Info("ウェブフックを送信しました", "Webhook");
                if (!response.IsSuccessStatusCode)
                    Logger.Warn("応答が異常です", "Webhook");
                Logger.Info($"{(int)response.StatusCode} {response.ReasonPhrase}", "Webhook");  // 正常な応答: 204 No Content
            }
            catch (Exception ex)
            {
                Logger.Error(ex.ToString(), "Webhook");
            }
        }
        public static string ColorIdToDiscordEmoji(int colorId, bool alive)
        {
            if (alive)
            {
                return colorId switch
                {
                    0 => "<:aured:866558066921177108>",
                    1 => "<:aublue:866558066484183060>",
                    2 => "<:augreen:866558066568986664>",
                    3 => "<:aupink:866558067004538891>",
                    4 => "<:auorange:866558066902958090>",
                    5 => "<:auyellow:866558067243221002>",
                    6 => "<:aublack:866558066442895370>",
                    7 => "<:auwhite:866558067026165770>",
                    8 => "<:aupurple:866558066966396928>",
                    9 => "<:aubrown:866558066564136970>",
                    10 => "<:aucyan:866558066525601853>",
                    11 => "<:aulime:866558066963382282>",
                    12 => "<:aumaroon:866558066917113886>",
                    13 => "<:aurose:866558066921439242>",
                    14 => "<:aubanana:866558065917558797>",
                    15 => "<:augray:866558066174459905>",
                    16 => "<:autan:866558066820382721>",
                    17 => "<:aucoral:866558066552209448>",
                    _ => "?"
                };
            }
            else
            {
                return colorId switch
                {
                    0 => "<:aureddead:866558067255279636>",
                    1 => "<:aubluedead:866558066660999218>",
                    2 => "<:augreendead:866558067088949258>",
                    3 => "<:aupinkdead:866558066945556512>",
                    4 => "<:auorangedead:866558067508510730>",
                    5 => "<:auyellowdead:866558067206520862>",
                    6 => "<:aublackdead:866558066668339250>",
                    7 => "<:auwhitedead:866558067231293450>",
                    8 => "<:aupurpledead:866558067223298048>",
                    9 => "<:aubrowndead:866558066945163304>",
                    10 => "<:aucyandead:866558067051200512>",
                    11 => "<:aulimedead:866558067344408596>",
                    12 => "<:aumaroondead:866558067238895626>",
                    13 => "<:aurosedead:866558067083444225>",
                    14 => "<:aubananadead:866558066342625350>",
                    15 => "<:augraydead:866558067049758740>",
                    16 => "<:autandead:866558067230638120>",
                    17 => "<:aucoraldead:866558067024723978>",
                    _ => "?"
                };
            }
        }
    }
}
