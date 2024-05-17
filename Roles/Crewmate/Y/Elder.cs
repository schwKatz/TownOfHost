using System.Linq;
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
        roleChanged = false;
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
    private static bool roleChanged;
    public static readonly CustomRoles[] ChangeRoles =
    {
            CustomRoles.Crewmate,
    };
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
            foreach (var pc in Main.AllAlivePlayerControls)
            {
                var role = PlayerState.GetByPlayerId(pc.PlayerId).MainRole;
                if (role.IsCrewmate())
                {
                    //Player.RpcSetCustomRole(ChangeRoleElderDead);
                    pc.RpcSetCustomRole(CustomRoles.Crewmate);
                    pc.RpcProtectedMurderPlayer(); // 変更が行われたことを通知
                    Utils.NotifyRoles();
                    Utils.MarkEveryoneDirtySettings(); // 全プレイヤーの設定を更新
                    break;
                }
            }
            roleChanged = false;
            if (!roleChanged)
            {
                return;
            }
        }
    }
}