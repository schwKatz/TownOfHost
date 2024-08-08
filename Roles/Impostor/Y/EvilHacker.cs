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
    }
    enum OptionName
    {
    }
    private static void SetUpOptionItem()
    {
    }
    public override bool OnCheckVanish()
    {
        return true;
    }
}