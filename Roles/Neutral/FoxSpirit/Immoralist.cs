using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using AmongUs.GameOptions;

using TownOfHostY.Roles.Core;
using TownOfHostY.Roles.Core.Interfaces;

namespace TownOfHostY.Roles.Neutral;
public sealed class Immoralist : RoleBase, IAdditionalWinner, ISystemTypeUpdateHook
{
    public static readonly SimpleRoleInfo RoleInfo =
        SimpleRoleInfo.Create(
            typeof(Immoralist),
            player => new Immoralist(player),
            CustomRoles.Immoralist,
            () => RoleTypes.Crewmate,
            CustomRoleTypes.Neutral,
            (int)Options.offsetId.NeuFox + 100,
            SetupOptionItem,
            "背徳者",
            "#ad6ce0",
            introSound: () => GetIntroSound(RoleTypes.Shapeshifter),
            assignInfo: new(CustomRoles.Immoralist, CustomRoleTypes.Neutral)
            {
                IsInitiallyAssignableCallBack = ()
                    => (MapNames)Main.NormalOptions.MapId is not MapNames.Polus and not MapNames.Fungle
                        && CustomRoles.FoxSpirit.IsEnable()
            }
        );
    public Immoralist(PlayerControl player)
    : base(
        RoleInfo,
        player,
        () => HasTask.ForRecompute)
    {
        CanAlsoBeExposedToFox = OptionCanAlsoBeExposedToJackal.GetBool();
        AddGuard = OptionAddGuard.GetBool();
        AddGuardTiming = (AddGuardTimingOption)OptionAddGuardTiming.GetValue();

        CustomRoleManager.MarkOthers.Add(GetMarkOthers);
    }

    private static OptionItem OptionCanAlsoBeExposedToJackal;
    private static OptionItem OptionAddGuard;
    private static OptionItem OptionAddGuardTiming;
    private static Options.OverrideTasksData Tasks;

    private static bool CanAlsoBeExposedToFox;
    private static bool AddGuard;

    bool isGuardVote = false;
    PlayerControl guardPlayer = null;
    public static List<Immoralist> Immoralists = new();

    public enum AddGuardTimingOption
    {
        SelectFirstMeeting,
        SelectTaskFinish,
        SelectRandom,
    };
    AddGuardTimingOption AddGuardTiming;

    enum OptionName
    {
        ImmoralistCanAlsoBeExposedToFox,
        ImmoralistOptionAddGuard,
        ImmoralistOptionAddGuardTiming
    }

    public static void SetupOptionItem()
    {
        OptionAddGuard = BooleanOptionItem.Create(RoleInfo, 10, OptionName.ImmoralistOptionAddGuard, true, false);
        OptionAddGuardTiming = StringOptionItem.Create(RoleInfo, 11, OptionName.ImmoralistOptionAddGuardTiming, EnumHelper.GetAllNames<AddGuardTimingOption>(), 1, false).SetParent(OptionAddGuard);
        OptionCanAlsoBeExposedToJackal = BooleanOptionItem.Create(RoleInfo, 12, OptionName.ImmoralistCanAlsoBeExposedToFox, false, false);
        Tasks = Options.OverrideTasksData.Create(RoleInfo, 20);
    }
    public override void Add()
    {

        Immoralists.Add(this);
        guardPlayer = null;

        if (!AddGuard) return;

        isGuardVote = AddGuardTiming == AddGuardTimingOption.SelectFirstMeeting;

        if (AddGuardTiming == AddGuardTimingOption.SelectRandom)
        {
            //ターゲット割り当て
            if (AmongUsClient.Instance.AmHost)
            {
                guardPlayer = Main.AllPlayerControls.ElementAtOrDefault(IRandom.Instance.Next(Main.AllPlayerControls.Count()));
                Logger.Info($"{Player.GetNameWithRole()} guardPlayerSelect:{guardPlayer.GetNameWithRole()}", "Immoralist");
            }
        }
    }

    public override (byte? votedForId, int? numVotes, bool doVote) ModifyVote(byte voterId, byte sourceVotedForId, bool isIntentional)
    {
        // 既定値
        var (votedForId, numVotes, doVote) = base.ModifyVote(voterId, sourceVotedForId, isIntentional);

        if (voterId != Player.PlayerId || !AddGuard || !isGuardVote) return (votedForId, numVotes, doVote);

        if (sourceVotedForId != Player.PlayerId && sourceVotedForId < 253)
        {
            guardPlayer = Utils.GetPlayerById(sourceVotedForId);
        }
        else
        {
            guardPlayer = Main.AllPlayerControls.ElementAtOrDefault(IRandom.Instance.Next(0, Main.AllPlayerControls.Count()));
        }
        numVotes = 0;//投票を見えなくする
        isGuardVote = false;
        Logger.Info($"{Player.name} guardPlayerSelect:{guardPlayer.name}", "Immoralist");

        return (votedForId, numVotes, doVote);
    }

    /// <summary>
    /// 使用する時true
    /// </summary>
    public static bool GuardPlayerCheckMurder(MurderInfo info)
    {
        (var killer, var target) = info.AttemptTuple;

        if (!AddGuard) return false;

        // 守られていなければなにもせず返す
        if (!IsGuard(target)) return false;
        // 直接キル出来る役職チェック
        if (killer.GetCustomRole().IsDirectKillRole()) return false;

        killer.RpcProtectedMurderPlayer(target); //killer側のみ。斬られた側は見れない。
        info.CanKill = false;

        foreach (var immoralist in Immoralists)
        {
            if (immoralist.guardPlayer == target)
            {
                immoralist.guardPlayer = null; break;
            }
        }
        return true;
    }

    public static bool IsGuard(PlayerControl target)
    {
        foreach (var immoralist in Immoralists)
        {
            if (target == immoralist.guardPlayer) return true;
        }
        return false;
    }

    public override bool OnCompleteTask()
    {
        if (IsTaskFinished)
        {
            if (AddGuard) isGuardVote = AddGuardTiming == AddGuardTimingOption.SelectTaskFinish;
            foreach (var fox in Main.AllPlayerControls.Where(player => player.Is(CustomRoles.FoxSpirit)).ToArray())
            {
                NameColorManager.Add(Player.PlayerId, fox.PlayerId, RoleInfo.RoleColorCode);
            }
        }

        return true;
    }
    public override string GetMark(PlayerControl seer, PlayerControl seen, bool isForMeeting = false)
    {
        //seenが省略の場合seer
        seen ??= seer;

        if (AddGuard && seer == Player && seen == guardPlayer)
            return Utils.ColorString(Color.cyan, "Σ");

        return string.Empty;
    }
    public override string GetLowerText(PlayerControl seer, PlayerControl seen = null, bool isForMeeting = false, bool isForHud = false)
    {
        if (!AddGuard || !isGuardVote || !isForMeeting) return string.Empty;

        //seenが省略の場合seer
        seen ??= seer;
        //seeおよびseenが自分である場合以外は関係なし
        if (!Is(seer) || !Is(seen)) return "";

        return Translator.GetString("SelectGuardPlayer").Color(RoleInfo.RoleColor);
    }
    public static string GetMarkOthers(PlayerControl seer, PlayerControl seen = null, bool isForMeeting = false)
    {
        seen ??= seer;
        if (!CanAlsoBeExposedToFox ||
            !seer.Is(CustomRoles.FoxSpirit) || seen.GetRoleClass() is not Immoralist immoralist ||
            !immoralist.IsTaskFinished)
        {
            return string.Empty;
        }

        return Utils.ColorString(RoleInfo.RoleColor, "★");
    }
    public override void AfterMeetingTasks()
    {
        var fox = Main.AllPlayerControls.ToArray().Where(pc => pc.Is(CustomRoles.FoxSpirit)).FirstOrDefault();
        if (fox.IsAlive() && !Main.AfterMeetingDeathPlayers.ContainsKey(fox.PlayerId)) return;

        if (Player.IsAlive())
        {
            Main.AfterMeetingDeathPlayers.TryAdd(Player.PlayerId, CustomDeathReason.FollowingSuicide);
            Logger.Info($"FollowingDead Set:{Player.name}", "Immoralist");
        }
    }
    public bool CheckWin(ref CustomRoles winnerRole)
    {
        return CustomWinnerHolder.WinnerTeam == CustomWinner.FoxSpirit;
    }

    // コミュ
    bool ISystemTypeUpdateHook.UpdateHudOverrideSystem(HudOverrideSystemType switchSystem, byte amount)
    {
        if ((amount & HudOverrideSystemType.DamageBit) <= 0) return false;
        return true;
    }
    bool ISystemTypeUpdateHook.UpdateHqHudSystem(HqHudSystemType hqHudSystemType, byte amount)
    {
        var tags = (HqHudSystemType.Tags)(amount & HqHudSystemType.TagMask);
        if (tags == HqHudSystemType.Tags.FixBit) return false;
        return true;
    }
    // 停電
    bool ISystemTypeUpdateHook.UpdateSwitchSystem(SwitchSystem switchSystem, byte amount)
    {
        return false;
    }
}
