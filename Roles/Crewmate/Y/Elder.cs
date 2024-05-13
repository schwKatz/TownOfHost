using AmongUs.GameOptions;

using TownOfHostY.Roles.Core;
using TownOfHostY.Roles.Core.Class;

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
    public Elder(PlayerControl player)
    : base(
        RoleInfo,
        player
    )
    {
        DiaInLife = OptionDiaInLife.GetBool();

        GuardCount = 0;
    }
    private static OptionItem OptionDiaInLife;

    enum OptionName
    {
        ElderDiaInLife,
    }
    private static bool DiaInLife;
    int GuardCount = 0;
    public static CustomRoles ChangeRoleElderDead;
    public static readonly CustomRoles[] ChangeRoles =
    {
            CustomRoles.Crewmate,
    };
    private static void SetupOptionItem()
    {
        OptionDiaInLife = BooleanOptionItem.Create(RoleInfo, 10, OptionName.ElderDiaInLife, false, false);
    }
    public override bool OnCheckMurderAsTarget(MurderInfo info)
    {
        (var killer, var target) = info.AttemptTuple;

        if (GuardCount > 0)
        {// GuardCountが1以上の場合はキルを通す
            info.CanKill = true;
        }
        else
        {// GuardCountが0の場合はキルを通さない。
            info.CanKill = false;
            killer.RpcProtectedMurderPlayer(target);
            killer.SetKillCooldown();
        }
        GuardCount++;// GuardCountを増やす
        return true;
    }
    public void ChangeRole()
    {
        Player.RpcSetCustomRole(ChangeRoleElderDead);
        Utils.NotifyRoles();
    }
    public override void OnFixedUpdate(PlayerControl player)
    {
        if (!Player.IsAlive())
        {
            ChangeRole();
        }
    }
}