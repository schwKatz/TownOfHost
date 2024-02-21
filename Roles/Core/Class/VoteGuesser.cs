using System;
using System.Linq;
using System.Text;
using System.Collections.Generic;
using UnityEngine;

using static TownOfHostY.Translator;
using HarmonyLib;

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

        selecting = false;
        guessed = false;
        targetGuess = null;
        targetForRole = null;
    }

    protected int NumOfGuess = 1;

    private GuesserInfo guesserInfo;

    private bool selecting = false;
    private bool guessed = false;
    private PlayerControl targetGuess = null;
    private PlayerControl targetForRole = null;

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
    public override bool CheckVoteAsVoter(PlayerControl votedFor)
    {
        if (Player == null) return true;

        if (NumOfGuess <= 0) return true;
        if (guessed) return true;

        if (selecting)
        {
            if (votedFor == null)
            {
                //スキップでモード解除
                selecting = false;
                targetGuess = null;
                targetForRole = null;

                Utils.SendMessage(GetString("Message.GuesserSelectionCancel"), Player.PlayerId);
                SendMessageGuide();
                return false;
            }

            if (targetGuess == null)
            {
                targetGuess = votedFor;
                guesserInfo.ResetList();
                Logger.Info($"GuesserSetTarget1 guesser: {Player?.name}, target: {targetGuess?.name}", "Guesser.CheckVoteAsVoter");
            }
            else
            {
                targetForRole = votedFor;
                Logger.Info($"GuesserSetTarget2 guesser: {Player?.name}, target: {targetForRole?.name}", "Guesser.CheckVoteAsVoter");
            }

            if (targetGuess == null)
            {
                Utils.SendMessage(GetString("Message.GuesserSelectionTarget"), Player.PlayerId);
                return false;
            }
            if (targetForRole == null)
            {
                Utils.SendMessage(string.Format(GetString("Message.GuesserSelectionRole"), targetGuess.name, guesserInfo.PageNo, guesserInfo.GetRoleGuide()), Player.PlayerId);
                return false;
            }

            Logger.Info($"GuesserSelectMeetingEnd guesser: {Player?.name}, target: {targetGuess?.name}, {targetForRole?.name}", "Guesser.CheckVoteAsVoter");

            var role = guesserInfo.GetRole(targetForRole);

            if (role == CustomRoles.NotAssigned)
            {
                Logger.Info($"InvalidSelection playerId: {targetForRole?.PlayerId}", "Guesser.CheckVoteAsVoter");
                targetForRole = null;
                return false; //無効選択
            }
            if (role == CustomRoles.DummyNext)
            {
                //ページ切り替え
                Logger.Info($"NextPage pageNo: {guesserInfo.PageNo}", "Guesser.CheckVoteAsVoter");
                guesserInfo.NextPage();
                Utils.SendMessage(string.Format(GetString("Message.GuesserSelectionRole"), targetGuess.name, guesserInfo.PageNo, guesserInfo.GetRoleGuide()), Player.PlayerId);
                return false;
            }

            if (targetGuess == null || !targetGuess.IsAlive() || targetGuess.Data.Disconnected)
            {
                //ターゲット存在なしは強制スキップ
                selecting = false;
                targetGuess = null;
                targetForRole = null;

                Utils.SendMessage(GetString("Message.GuesserSelectionCancel"), Player.PlayerId);
                SendMessageGuide();
                return false;
            }

            UseGuesserAbility(role);

            selecting = false;
            targetGuess = null;
            targetForRole = null;
            SendMessageGuide();

            return false;
        }
        if (votedFor == null) return true;
        if (Player.PlayerId == votedFor.PlayerId && NumOfGuess > 0)
        {
            Logger.Info($"GuesserSelectStart guesser: {Player?.name}", "Guesser.CheckVoteAsVoter");

            selecting = true;
            guesserInfo.ResetList();
            Utils.SendMessage(GetString("Message.GuesserSelectionTarget"), Player.PlayerId);

            return false;
        }

        return true;
    }
    private void UseGuesserAbility(CustomRoles role)
    {
        guessed = true;
        NumOfGuess--;

        PlayerControl target;
        if (targetGuess.Is(role))
        {
            target = targetGuess;
            RpcGuesserMurderPlayer(target, CustomDeathReason.Kill);
        }
        else
        {
            target = Player;
            RpcGuesserMurderPlayer(target, CustomDeathReason.Misfire);
        }
        SendGuessedMessage(target);
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
    private void SendGuessedMessage(PlayerControl target)
    {
        string decoration = $"<color=#ffff00><size=100%>{GetString("Message.GuesserAbilityUse")}</size></color>\n";

        string targetText = $"{Palette.GetColorName(target.Data.DefaultOutfit.ColorId)} {target.name}";
        targetText = Utils.ColorString(Palette.PlayerColors[target.Data.DefaultOutfit.ColorId], targetText);

        targetText = $"<size=100%>{targetText}\n<color=#ffffff> {GetString("Message.GuesserDead")}</color></size>";

        string dispText = $"{decoration}\n{targetText}\n";
        string chatText = "\n\n";

        PlayerControl sender = PlayerControl.LocalPlayer;
        if (sender.Data.IsDead)
        {
            sender = PlayerControl.AllPlayerControls.ToArray().OrderBy(x => x.PlayerId).Where(x => !x.Data.IsDead).FirstOrDefault();
        }
        string name = sender.Data.PlayerName;
        ChatCommands.SendCustomChat(dispText, chatText, name, sender);
    }
    public override void OnStartMeeting()
    {
        SendMessageGuide();
    }
    public override void AfterMeetingTasks()
    {
        selecting = false;
        guessed = false;
        targetGuess = null;
        targetForRole = null;

        guesserInfo = null;
    }
    public void RpcGuesserMurderPlayer(PlayerControl target, CustomDeathReason reason)
    {
        target.Data.IsDead = true;
        target.RpcExileV2();

        var targetState = PlayerState.GetByPlayerId(target.PlayerId);
        targetState.DeathReason = reason;
        targetState.SetDead();

        //キルフラッシュ表示
        Main.AllPlayerControls.Do(pc => pc.KillFlash());

        foreach (var va in MeetingHud.Instance.playerStates)
        {
            if (va.VotedFor != target.PlayerId) continue;
            var voter = Utils.GetPlayerById(va.TargetPlayerId);
            MeetingHud.Instance.RpcClearVote(voter.GetClientId());
        }
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
