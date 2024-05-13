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
    }
    private static OptionItem OptionDiaInLife;

    enum OptionName
    {
        ElderDiaInLife,
    }
    private static bool DiaInLife;

    private static void SetupOptionItem()
    {
        OptionDiaInLife = BooleanOptionItem.Create(RoleInfo, 10, OptionName.ElderDiaInLife, false, false);
    }
}