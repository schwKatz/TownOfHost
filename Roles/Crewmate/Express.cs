using System.Linq;
using System.Collections.Generic;

using AmongUs.GameOptions;

using TownOfHost.Roles.Core;

namespace TownOfHost.Roles.Crewmate;
public sealed class Express : RoleBase
{
    public static readonly SimpleRoleInfo RoleInfo =
        new(
            typeof(Express),
            player => new Express(player),
            CustomRoles.Express,
            () => RoleTypes.Crewmate,
            CustomRoleTypes.Crewmate,
            35500,
            SetupOptionItem,
            "エクスプレス",
            "#00ffff"
        );
    public Express(PlayerControl player)
    : base(
        RoleInfo,
        player
    )
    {
        Speed = OptionSpeed.GetFloat();
    }

    private static OptionItem OptionSpeed;
    enum OptionName
    {
        ExpressSpeed
    }
    private static float Speed;

    private static void SetupOptionItem()
    {
        OptionSpeed = FloatOptionItem.Create(RoleInfo, 10, OptionName.ExpressSpeed, new(1.5f, 3f, 0.25f), 2.0f, false)
            .SetValueFormat(OptionFormat.Multiplier);
    }

    public override void ApplyGameOptions(IGameOptions opt)
    {
        Main.AllPlayerSpeed[Player.PlayerId] = Speed;
    }
}