using System.Linq;
using UnityEngine;
using AmongUs.GameOptions;
using TownOfHostY.Roles.AddOns.Crewmate;
using TownOfHostY.Roles.Core;
using Epic.OnlineServices.Presence;

namespace TownOfHostY.Roles.Madmate;

public sealed class MadSnitch : RoleBase
{
    public static readonly SimpleRoleInfo RoleInfo =
        SimpleRoleInfo.Create(
            typeof(MadSnitch),
            player => new MadSnitch(player),
            CustomRoles.MadSnitch,
            () => OptionCanVent.GetBool() ? RoleTypes.Engineer : RoleTypes.Crewmate,
            CustomRoleTypes.Madmate,
            28200,
            SetupOptionItem,
            "マッドスニッチ",
            introSound: () => GetIntroSound(RoleTypes.Impostor)
        );
    public MadSnitch(PlayerControl player)
    : base(
        RoleInfo,
        player,
        () => HasTask.ForRecompute)
    {
        canVent = OptionCanVent.GetBool();
        canAlsoBeExposedToImpostor = OptionCanAlsoBeExposedToImpostor.GetBool();
        TaskTrigger = OptionTaskTrigger.GetInt();

        CustomRoleManager.MarkOthers.Add(GetMarkOthers);
    }

    private static OptionItem OptionCanVent;
    private static OptionItem OptionCanAlsoBeExposedToImpostor;
    /// <summary>能力発動タスク数</summary>
    private static OptionItem OptionTaskTrigger;
    private static Options.OverrideTasksData Tasks;
    enum OptionName
    {
        MadSnitchCanAlsoBeExposedToImpostor,
        MadSnitchTaskTrigger,
    }

    private static bool canVent;
    private static bool canAlsoBeExposedToImpostor;
    private static int TaskTrigger;

    public static void SetupOptionItem()
    {
        OptionCanVent = BooleanOptionItem.Create(RoleInfo, 10, GeneralOption.CanVent, false, false);
        OptionCanAlsoBeExposedToImpostor = BooleanOptionItem.Create(RoleInfo, 11, OptionName.MadSnitchCanAlsoBeExposedToImpostor, false, false);
        OptionTaskTrigger = IntegerOptionItem.Create(RoleInfo, 12, OptionName.MadSnitchTaskTrigger, new(0, 99, 1), 1, false).SetValueFormat(OptionFormat.Pieces);
        Tasks = Options.OverrideTasksData.Create(RoleInfo, 20);
        Options.SetUpAddOnOptions(RoleInfo.ConfigId + 30, RoleInfo.RoleName, RoleInfo.Tab);
    }

    private bool KnowsImpostor()
    {
        return MyTaskState.HasCompletedEnoughCountOfTasks(TaskTrigger);
    }
    private void CheckAndAddNameColorToImpostors()
    {
        if (!KnowsImpostor()) return;

        foreach (var impostor in Main.AllPlayerControls.Where(player => player.Is(CustomRoleTypes.Impostor)))
        {
            NameColorManager.Add(Player.PlayerId, impostor.PlayerId, impostor.GetRoleColorCode());
        }
    }

    public override void Add()
    {
        CheckAndAddNameColorToImpostors();
    }
    public override bool OnCompleteTask()
    {
        CheckAndAddNameColorToImpostors();
        return true;
    }
    public static string GetMarkOthers(PlayerControl seer, PlayerControl seen = null, bool isForMeeting = false)
    {
        seen ??= seer;
        if (
            // オプションが無効
            !canAlsoBeExposedToImpostor ||
            // インポスター→MadSnitchではない
            !seer.Is(CustomRoleTypes.Impostor) ||
            seen.GetRoleClass() is not MadSnitch madSnitch ||
            // マッドスニッチがまだインポスターを知らない
            !madSnitch.KnowsImpostor())
        {
            return string.Empty;
        }

        return Utils.ColorString(RoleInfo.RoleColor, "★");
    }
    public override void OverrideProgressTextAsSeer(PlayerControl seen, ref bool enabled, ref string text)
    {
        if (seen != Player) return;

        var taskState = Player.GetPlayerTaskState();
        if (taskState == null || !taskState.hasTasks) return;

        Color TextColor = Color.white;
        var TaskCompleteColor = RoleInfo.RoleColor.ShadeColor(0.5f); //タスク完了後の色
        var NonCompleteColor = Color.white; //カウントされない人外は白色

        TextColor = KnowsImpostor() ? TaskCompleteColor : NonCompleteColor;
        int KnowTasksCount = taskState.AllTasksCount > TaskTrigger ? TaskTrigger : taskState.AllTasksCount;

        text = Utils.ColorString(TextColor, $"({taskState.CompletedTasksCount}/{KnowTasksCount})");
    }
}
