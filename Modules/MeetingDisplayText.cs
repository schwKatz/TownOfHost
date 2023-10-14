using System.Linq;
using UnityEngine;
using TownOfHostY.Roles.Core;
using TownOfHostY.Roles.Crewmate;
using static TownOfHostY.Translator;

namespace TownOfHostY.Modules;
public static class MeetingDisplayText
{
    private static (string, float, float) LeftUp()
    {
        string addText = "";
        float heightUp = 2.0f;
        float heightDown = 2.0f;

        // パン屋が居ればパン屋生存中を表示する
        addText = Bakery.AddMeetingDisplay();

        // Report
        if (Options.ShowReportReason.GetBool())
        {
            if (ReportDeadBodyPatch.ReportTarget == null)
                addText = "緊急ボタンでの会議";
            else
                addText = $"死体発見：{ReportDeadBodyPatch.ReportTarget.ColorName}\n{ReportDeadBodyPatch.ReportTarget.PlayerName}";
        }

        heightUp = 3f;
        heightDown = 4f;

        return (addText, heightUp, heightDown);
    }
    private static (string, float, float) RightUp(bool forVanilla)
    {
        string addText = "";
        float heightUp = 2.0f;
        float heightDown = 6.0f;

        // Revenge
        if (Options.ShowRevengeTarget.GetBool())
        {
            //int i = 1;
            foreach (var Exiled_Target in MeetingHudPatch.RevengeTargetPlayer)
            {
                addText += $"道連れ発生：{Exiled_Target.Item2.name}\n　<={Exiled_Target.Item1.name}追放による";
                //if (i < MeetingHudPatch.RevengeTargetPlayer.Count())
                //{
                //    addText += '\n';
                //    i++;
                //}
            }
            heightUp = 3f;
            heightDown = 4f;
        }
        // ModName&Version
        if (forVanilla)
        {
            addText = $"<color={Main.ModColor}>TOH_Y</color> <color=#ffffff>v{Main.PluginVersion}</color>\n\n" + addText;
            heightUp = 3f;
            heightDown = 6f;
        }

        return (addText, heightUp, heightDown);
    }

    public static string AddTextLeftUpForClient(string name)
    {
        // 左上(1番目)

        string addText = "";
        float heightUp = 3.5f;
        float heightDown = 3.5f;

        (addText, heightUp, heightDown) = LeftUp();
        if (addText == "") return name;

        string plusDisplay = $"<line-height={heightUp}em><align={"left"}>" +
            $"{addText}</align>\n</line-height>";
        string adjust = $"<line-height={heightDown}em>\nㅤ</line-height>";

        return plusDisplay + name + adjust;
    }
    public static string AddTextRightUpForClient(string name)
    {
        // 右上(3番目)

        string addText = "";
        float heightUp = 3.5f;
        float heightDown = 3.5f;

        (addText, heightUp, heightDown) = RightUp(false);
        if (addText == "") return name;

        string plusDisplay = $"<line-height={heightUp}em><align={"left"}>" +
            $"{addText}</align>\n</line-height>";
        string adjust = $"<line-height={heightDown}em>\nㅤ</line-height>";

        return plusDisplay + name + adjust;
    }

    public static string AddTextForVanilla
        (PlayerControl pc, string name, string suffix, string roleText, bool isMeeting)
    {
        if (!isMeeting) return name;

        // 表示するテキスト
        string addText = "";
        // 高さ調節用
        float heightUp = 2f;
        float heightDown = 3.5f;

        // 0 = 左上(1番目)
        if (pc == Main.AllAlivePlayerControls.ElementAtOrDefault(0))
        {
            (addText, heightUp, heightDown) = LeftUp();
            if (addText == "") return name;
        }
        // 2 = 右上(3番目)
        else if (pc == Main.AllAlivePlayerControls.ElementAtOrDefault(2))
        {
            (addText, heightUp, heightDown) = RightUp(true);
        }
        // それ以外は早期リターン
        else return name;

        // 名前が数行になる時に一段ぶん少なくする
        if (suffix != "" || roleText != "") { heightUp--; heightDown--; };

        string plusDisplay = $"<line-height={heightUp}em><align={"left"}>" +
            $"{addText}</align>\n</line-height>";
        string adjust = $"<line-height={heightDown}em>\nㅤ</line-height>";

        return plusDisplay + name + adjust;
    }
}
