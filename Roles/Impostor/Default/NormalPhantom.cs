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
        phantomCooldown = OptionPhantomCooldown.GetFloat();
        phantomDuration = OptionPhantomDuration.GetFloat();
    }
    private static OptionItem OptionPhantomCooldown;
    private static OptionItem OptionPhantomDuration;
    enum OptionName
    {
        PhantomCooldown,
        PhantomDuration,
    }
    private static float phantomCooldown;
    private static float phantomDuration;

    public static void SetupOptionItem()
    {
        OptionPhantomCooldown = FloatOptionItem.Create(RoleInfo, 3, OptionName.PhantomCooldown, new(2.5f, 60, 2.5f), 15f, false)
            .SetValueFormat(OptionFormat.Seconds);
        OptionPhantomDuration = FloatOptionItem.Create(RoleInfo, 4, OptionName.PhantomDuration, new(5f, 90f, 5f), 30f, false)
            .SetValueFormat(OptionFormat.Seconds);
    }

    public override void ApplyGameOptions(IGameOptions opt)
    {
        AURoleOptions.PhantomCooldown = phantomCooldown;
        AURoleOptions.PhantomDuration = phantomDuration;
    }
}