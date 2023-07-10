using AmongUs.GameOptions;
using TownOfHostY.Roles.Core;

namespace TownOfHostY.Roles.Crewmate;
public sealed class NiceWatcher : RoleBase
{
    public static readonly SimpleRoleInfo RoleInfo =
        SimpleRoleInfo.Create(
            typeof(NiceWatcher),
            player => new NiceWatcher(player),
            CustomRoles.NiceWatcher,
            () => RoleTypes.Crewmate,
            CustomRoleTypes.Crewmate,
            30000,
            null,
            "ナイスウォッチャー",
            "#800080"
        );
    public NiceWatcher(PlayerControl player)
    : base(
        RoleInfo,
        player
    )
    {
    }

    public override void ApplyGameOptions(IGameOptions opt)
    {
        opt.SetBool(BoolOptionNames.AnonymousVotes, false);
    }
}