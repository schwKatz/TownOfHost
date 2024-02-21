using System;

namespace TownOfHostY.Roles.Core.Class;

public abstract class VoteGuesser : RoleBase
{
    public VoteGuesser(
        SimpleRoleInfo roleInfo,
        PlayerControl player,
        Func<HasTask> hasTasks = null,
        bool? hasAbility = null
    )
    : base(
        roleInfo,
        player,
        hasTasks,
        hasAbility)
    {
    }
}
