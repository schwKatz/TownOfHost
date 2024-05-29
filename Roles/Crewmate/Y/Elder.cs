using System.Linq;
using AmongUs.GameOptions;
using TownOfHostY.Roles.Core;

namespace TownOfHostY.Roles.Crewmate;
public sealed class Elder : RoleBase
{
    public static readonly SimpleRoleInfo RoleInfo =
         SimpleRoleInfo.Create(
            typeof(Elder),
            player => new Elder(player),
            CustomRoles.Elder,
            () => RoleTypes.Crewmate,
            CustomRoleTypes.Crewmate,
            (int)Options.offsetId.CrewY + 1900,//仮
            SetupOptionItem,
            "エルダー",
            "#000080"
        );
    public Elder(PlayerControl player) : base(RoleInfo, player)
    {
        DiaInLife = OptionDiaInLife.GetBool();
        roleChanged = false;
        GuardCount = 0;
    }

    private static OptionItem OptionDiaInLife;
    private static bool roleChanged;
    private int GuardCount;
    private static bool DiaInLife;
    public static readonly CustomRoles[] ChangeRoles = { CustomRoles.Crewmate };

    enum OptionName
    {
        ElderDiaInLife,
    }
    private static void SetupOptionItem()
    {
        var cRolesString = ChangeRoles.Select(x => x.ToString()).ToArray();
        OptionDiaInLife = BooleanOptionItem.Create(RoleInfo, 10, OptionName.ElderDiaInLife, false, false);
    }
    public override bool OnCheckMurderAsTarget(MurderInfo info)
    {
        (var killer, var target) = info.AttemptTuple;
        if (!target.Is(CustomRoles.Elder)) return false;

        if (GuardCount > 0)
        {
            // GuardCountが1以上の場合はキルを通す
            info.CanKill = true;
        }
        else
        {
            // GuardCountが0の場合はキルを通さない。
            info.CanKill = false;
            roleChanged = true;
            killer.RpcProtectedMurderPlayer(target);
            killer.SetKillCooldown();
        }

        GuardCount++; // GuardCountを増やす
        return true;
    }
    public override void OnFixedUpdate(PlayerControl player)
    {
        if (!Player.IsAlive() && roleChanged)
        {
            ChangeRole();
        }
    }
    public void ChangeRole()
    {
        var playersCrewmate = Main.AllAlivePlayerControls.Where(player => player.Is(CustomRoleTypes.Crewmate));
        foreach (var player in playersCrewmate)
        {
            player.RpcSetCustomRole(ChangeRoles[0]); // クルーメイトに変更
        }
        Utils.NotifyRoles(); // 役職変更を通知
        Utils.MarkEveryoneDirtySettings();

        roleChanged = false;
    }
}