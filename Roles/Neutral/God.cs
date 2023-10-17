using System.Linq;
using UnityEngine;
using AmongUs.GameOptions;

using TownOfHostY.Roles.Core;

namespace TownOfHostY.Roles.Neutral;

public sealed class God : RoleBase
{
    public static readonly SimpleRoleInfo RoleInfo =
        SimpleRoleInfo.Create(
            typeof(God),
            player => new God(player),
            CustomRoles.God,
            () => RoleTypes.Crewmate,
            CustomRoleTypes.Neutral,
            60800,
            SetupOptionItem,
            "神",
            "#ffff00"
        );
    public God(PlayerControl player)
    : base(
        RoleInfo,
        player,
        () => HasTask.ForRecompute
    )
    {
        taskCompleteToWin = OptionTaskCompleteToWin.GetBool();
        viewVoteFor = OptionViewVoteFor.GetBool();

        if (Player != null)
        {
            foreach (var pc in Main.AllPlayerControls)
            {
                NameColorManager.Add(Player.PlayerId, pc.PlayerId);
            }
        }
    }
    private static OptionItem OptionTaskCompleteToWin;
    private static OptionItem OptionViewVoteFor;
    private static Options.OverrideTasksData Tasks;
    enum OptionName
    {
        GodTaskCompleteToWin,
        GodViewVoteFor,
    }
    private static bool taskCompleteToWin;
    private static bool viewVoteFor;
    public static void SetupOptionItem()
    {
        OptionTaskCompleteToWin = BooleanOptionItem.Create(RoleInfo, 10, OptionName.GodTaskCompleteToWin, true, false);
        OptionViewVoteFor = BooleanOptionItem.Create(RoleInfo, 11, OptionName.GodViewVoteFor, false, false);
        Tasks = Options.OverrideTasksData.Create(RoleInfo, 20);
    }
    public override void ApplyGameOptions(IGameOptions opt)
    {
        if (viewVoteFor)
        {
            opt.SetBool(BoolOptionNames.AnonymousVotes, false);
        }
    }
    public override void OverrideDisplayRoleNameAsSeer(PlayerControl seen, bool isMeeting, ref bool enabled, ref Color roleColor, ref string roleText)
    {
        enabled = true;
    }
    public static bool CheckWin()
    {
        return Main.AllAlivePlayerControls.ToArray()
                .Any(p => p.Is(CustomRoles.God) &&
                          (!taskCompleteToWin || p.GetPlayerTaskState().IsTaskFinished));
    }
}
