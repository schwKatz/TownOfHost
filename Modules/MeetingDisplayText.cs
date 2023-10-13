using System.Linq;
using UnityEngine;
using TownOfHostY.Roles.Core;
using TownOfHostY.Roles.Crewmate;
using static TownOfHostY.Translator;

namespace TownOfHostY.Modules;
public static class MeetingDisplayText
{
    public static string AddTextReftUpForClient(string name)
    {
        // 左上(1番目)

        string addText = "";
        // パン屋が居ればパン屋生存中を表示する
        addText = Bakery.AddMeetingDisplay();
        if (addText == "") return name;

        // 高さ調節用
        float height = 3.5f;

        string plusDisplay = $"<line-height={height}em><align={"left"}>" +
            $"{addText}</align>\n</line-height>";
        string adjust = $"<line-height={height}em>\nㅤ</line-height>";

        return plusDisplay + name + adjust;
    }

    public static string AddTextForVanilla
        (PlayerControl pc, string name, string suffix, string roleText, bool isMeeting)
    {
        if (!isMeeting) return name;

        // 表示するテキスト
        string addText = "";
        // 高さ調節用
        float height = 2;
        // 0 = 左上(1番目)
        if (pc == Main.AllAlivePlayerControls.ElementAtOrDefault(0))
        {
            // パン屋が居ればパン屋生存中を表示する
            addText = Bakery.AddMeetingDisplay();
            if (addText == "") return name;
            height = 3.5f;
        }
        // 2 = 右上(3番目)
        else if (pc == Main.AllAlivePlayerControls.ElementAtOrDefault(2))
        {
            // ModName&Version
            addText = $"<color={Main.ModColor}>TOH_Y</color> <color=#ffffff>v{Main.PluginVersion}</color>";
            height = 6;
        }
        // それ以外は早期リターン
        else return name;

        // 名前が数行になる時に一段ぶん少なくする
        if (suffix != "" || roleText != "") height--;

        string plusDisplay = $"<line-height={height}em><align={"left"}>" +
            $"{addText}</align>\n</line-height>";
        string adjust = $"<line-height={height}em>\nㅤ</line-height>";

        return plusDisplay + name + adjust;
    }
}
