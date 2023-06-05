using System;
using System.Text;
using UnityEngine;
using AmongUs.GameOptions;

using TownOfHost.Roles.Core;
using TownOfHost.Roles.Core.Interfaces;
using static TownOfHost.Translator;
using static TownOfHost.Options;

namespace TownOfHost.Roles.Impostor;
public sealed class EvilNekomata : RoleBase, IImpostor
{
    public static readonly SimpleRoleInfo RoleInfo =
        new(
            typeof(EvilNekomata),
            player => new EvilNekomata(player),
            CustomRoles.EvilNekomata,
            () => RoleTypes.Impostor,
            CustomRoleTypes.Impostor,
            3000,
            null,
            "en"
        );
    public EvilNekomata(PlayerControl player)
    : base(
        RoleInfo,
        player
    )
    {
    }
}