using System;
using System.Linq;
using System.Text;
using System.Collections.Generic;
using UnityEngine;

using static TownOfHostY.Translator;

namespace TownOfHostY.Roles.Core.Class;

public abstract class VoteGuesser : RoleBase
{
    public VoteGuesser(
        SimpleRoleInfo roleInfo,
        PlayerControl player,
        Func<HasTask> hasTasks = null,
        bool? hasAbility = null
    )
    : base(
        roleInfo,
        player,
        hasTasks,
        hasAbility)
    {
        NumOfGuess = 1;

        guesserInfo = null;

        guessed = false;
    }

    protected int NumOfGuess = 1;

    private GuesserInfo guesserInfo;

    private bool guessed = false;

    public override string GetProgressText(bool comms = false) => Utils.ColorString(NumOfGuess > 0 ? Color.yellow : Color.gray, $"({NumOfGuess})");
    public override void OverrideDisplayRoleNameAsSeer(PlayerControl seen, bool isMeeting, ref bool enabled, ref Color roleColor, ref string roleText)
    {
        if (!isMeeting) return;
        if (Player == null || !Player.IsAlive()) return;
        if (seen == null || !seen.IsAlive() || seen.Data.Disconnected) return;
        if (NumOfGuess <= 0) return;

        if (guesserInfo == null) guesserInfo = new();

        if (!guesserInfo.PlayerNumbers.TryGetValue(seen.PlayerId, out int number)) return;

        if (!enabled)
        {
            roleText = "";
            enabled = true;
        }
        roleText = $"<color=#ffff00><size=110%>{number}</size></color>{roleText}";
    }
    private void SendMessageGuide()
    {
        if (NumOfGuess > 0 && !guessed)
        {
            Utils.SendMessage(GetString("Message.SelfVoteForActivate"), Player.PlayerId);
        }
        else
        {
            Utils.SendMessage(GetString("Message.SelfVoteUsed"), Player.PlayerId);
        }
    }
    public override void OnStartMeeting()
    {
        SendMessageGuide();
    }
    private class GuesserInfo
    {
        public Dictionary<byte, int> PlayerNumbers;
        private List<CustomRoles> roleList;
        private List<(PlayerControl target, int number, CustomRoles role)> dispList = new();

        private int indexDisplayed = -1;
        public int PageNo = 0;

        public GuesserInfo()
        {
            SetPlayerNumbers();
            SetRoleList();
            SetDispList();
        }
        public void ResetList()
        {
            indexDisplayed = -1;
            PageNo = 0;
            SetDispList();
        }
        public void NextPage()
        {
            SetDispList();
        }
        private void SetPlayerNumbers()
        {
            PlayerNumbers = new();
            var number = 1; //Numberは1始まり
            foreach (var pc in Main.AllAlivePlayerControls.OrderBy(x => x.PlayerId))
            {
                PlayerNumbers.Add(pc.PlayerId, number++);
            }
        }
        private void SetRoleList()
        {
            roleList = new();
            foreach (CustomRoles role in CustomRolesHelper.AllStandardRoles.Where(r => r.IsEnable()))
            {
                if (role is CustomRoles.LastImpostor or CustomRoles.Lovers or CustomRoles.Workhorse) continue;
                roleList.Add(role);
            }
        }
        private void SetDispList()
        {
            CustomRoles role;
            int index;
            int indexRole;

            dispList = new();

            var targetList = Main.AllAlivePlayerControls.Where(x => !x.Data.Disconnected).OrderBy(x => x.PlayerId);

            if (indexDisplayed >= roleList.Count - 1)
            {
                indexDisplayed = -1;
                PageNo = 0;
            }
            PageNo++;
            index = 0;
            indexRole = indexDisplayed + index + 1;
            foreach (var target in targetList)
            {
                if (!PlayerNumbers.TryGetValue(target.PlayerId, out int number)) continue;

                if (index >= targetList.Count() - 1 && (PageNo > 1 || indexRole < roleList.Count - 1))
                {
                    //次ページ表示
                    role = CustomRoles.DummyNext;
                }
                else if (indexRole >= roleList.Count)
                {
                    //無効分
                    role = CustomRoles.NotAssigned;
                }
                else
                {
                    role = roleList[indexRole];
                    indexDisplayed = indexRole;
                }
                dispList.Add((target, number, role));

                index++;
                indexRole++;
            }
        }
        public string GetRoleGuide()
        {
            string text;

            StringBuilder sb = new();
            foreach (var info in dispList)
            {
                if (info.role == CustomRoles.NotAssigned) continue;

                if (info.role == CustomRoles.DummyNext)
                {
                    text = $"\n{info.number} {GetString("Message.NextPage")}";
                }
                else
                {
                    text = $"\n{info.number} {Utils.GetRoleName(info.role)}";
                }
                sb.Append(text);
                Logger.Info($"dispRole {text}", "Guesser.RoleText");
            }
            return sb.ToString();
        }
        public CustomRoles GetRole(PlayerControl target)
        {
            var info = dispList.FirstOrDefault(x => x.target == target);
            if (info.target == null) return CustomRoles.NotAssigned;
            return info.role;
        }
    }
}
