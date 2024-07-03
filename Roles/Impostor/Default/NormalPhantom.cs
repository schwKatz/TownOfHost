using AmongUs.GameOptions;

using TownOfHostY.Roles.Core;
using TownOfHostY.Roles.Core.Interfaces;

namespace TownOfHostY.Roles.Impostor;
public sealed class NormalPhantom : RoleBase, IImpostor
{
    public static readonly SimpleRoleInfo RoleInfo =
         SimpleRoleInfo.Create(
            typeof(NormalPhantom),
            player => new NormalPhantom(player),
            CustomRoles.NormalPhantom,
            () => RoleTypes.Phantom,
            CustomRoleTypes.Impostor,
            (int)Options.offsetId.ImpDefault + 200,
            SetupOptionItem,
            "ファントム"
        );
    public NormalPhantom(PlayerControl player)
    : base(
        RoleInfo,
        player
    )
    {
        phantomDuration = OptionPhantomDuration.GetFloat();
        phantomCooldown = OptionPhantomCooldown.GetFloat();
    }
    private static OptionItem OptionPhantomDuration;
    private static OptionItem OptionPhantomCooldown;
    enum OptionName
    {
        PhantomDuration,
        PhantomCooldown,
    }
    private static float phantomDuration;
    private static float phantomCooldown;

    public static void SetupOptionItem()
    {
        OptionPhantomDuration = FloatOptionItem.Create(RoleInfo, 3, OptionName.PhantomDuration, new(5f, 90f, 5f), 30f, false)
            .SetValueFormat(OptionFormat.Seconds);
        OptionPhantomCooldown = FloatOptionItem.Create(RoleInfo, 4, OptionName.PhantomCooldown, new(2.5f, 60, 2.5f), 15f, false)
            .SetValueFormat(OptionFormat.Seconds);
    }

    public override void ApplyGameOptions(IGameOptions opt)
    {
        AURoleOptions.PhantomDuration = phantomDuration;
        AURoleOptions.PhantomCooldown = phantomCooldown;
    }
}