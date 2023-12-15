using System.Linq;
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
            (int)Options.offsetId.NeuSpecial + 100,
            //(int)Options.offsetId.NeuFox + 100,
            SetupOptionItem,
            "背徳者",
            "#d11aff",
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

        CustomRoleManager.MarkOthers.Add(GetMarkOthers);
    }

    private static OptionItem OptionCanAlsoBeExposedToJackal;
    private static Options.OverrideTasksData Tasks;
    enum OptionName
    {
        ImmoralistCanAlsoBeExposedToFox
    }

    private static bool CanAlsoBeExposedToFox;

    public static void SetupOptionItem()
    {
        OptionCanAlsoBeExposedToJackal = BooleanOptionItem.Create(RoleInfo, 10, OptionName.ImmoralistCanAlsoBeExposedToFox, false, false);
        Tasks = Options.OverrideTasksData.Create(RoleInfo, 20);
    }

    public override bool OnCompleteTask()
    {
        if (IsTaskFinished)
        {
            foreach (var fox in Main.AllPlayerControls.Where(player => player.Is(CustomRoles.FoxSpirit)).ToArray())
            {
                NameColorManager.Add(Player.PlayerId, fox.PlayerId, RoleInfo.RoleColorCode);
            }
        }

        return true;
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
