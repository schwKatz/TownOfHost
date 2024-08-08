using UnityEngine;
using AmongUs.GameOptions;

using TownOfHostY.Roles.Core;
using TownOfHostY.Roles.Core.Interfaces;
using Hazel;

namespace TownOfHostY.Roles.Impostor;
public sealed class EvilHacker : RoleBase, IImpostor
{
    public static readonly SimpleRoleInfo RoleInfo =
        SimpleRoleInfo.Create(
            typeof(EvilHacker),
            player => new EvilHacker(player),
            CustomRoles.EvilHacker,
            () => RoleTypes.Phantom,
            CustomRoleTypes.Impostor,
            //(int)Options.offsetId.ImpSpecial + 1900,
            (int)Options.offsetId.ImpY + 1900,
            SetUpOptionItem,
            "イビルハッカー"
        );
    public EvilHacker(PlayerControl player)
    : base(
        RoleInfo,
        player
    )
    {
        KillCooldown = OptionKillCooldown.GetFloat();
        AdminCooldown = OptionAdminCooldown.GetFloat();
    }
    private static OptionItem OptionKillCooldown;
    private static OptionItem OptionAdminCooldown;
    private static float KillCooldown;
    private static float AdminCooldown;
    public float CalculateKillCooldown() => KillCooldown;
    public override void ApplyGameOptions(IGameOptions opt) => AURoleOptions.PhantomCooldown = AdminCooldown;

    enum OptionName
    {
        EvilHackerAdminCooldown,
    }
    private static void SetUpOptionItem()
    {
        OptionKillCooldown = FloatOptionItem.Create(RoleInfo, 10, GeneralOption.KillCooldown, new(2.5f, 180f, 2.5f), 30f, false)
                .SetValueFormat(OptionFormat.Seconds);
        OptionAdminCooldown = FloatOptionItem.Create(RoleInfo, 11, OptionName.EvilHackerAdminCooldown, new(2.5f, 180f, 2.5f), 30f, false)
                .SetValueFormat(OptionFormat.Seconds);
    }
    public override bool OnCheckVanish()
    {
        return true;
    }
}