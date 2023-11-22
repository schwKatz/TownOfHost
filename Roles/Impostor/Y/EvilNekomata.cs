using System;
using System.Text;
using UnityEngine;
using AmongUs.GameOptions;

using TownOfHostY.Roles.Core;
using TownOfHostY.Roles.Core.Interfaces;
using static TownOfHostY.Translator;
using static TownOfHostY.Options;

namespace TownOfHostY.Roles.Impostor;
public sealed class EvilNekomata : RoleBase, IImpostor
{
    public static readonly SimpleRoleInfo RoleInfo =
         SimpleRoleInfo.Create(
            typeof(EvilNekomata),
            player => new EvilNekomata(player),
            CustomRoles.EvilNekomata,
            () => RoleTypes.Impostor,
            CustomRoleTypes.Impostor,
            (int)Options.offsetId.ImpY + 0,
            null,
            "イビル猫又"
        );
    public EvilNekomata(PlayerControl player)
    : base(
        RoleInfo,
        player
    )
    {
    }
}