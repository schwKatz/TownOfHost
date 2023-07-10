using AmongUs.GameOptions;
using Hazel;

using TownOfHostY.Roles.Core;
using TownOfHostY.Roles.Core.Interfaces;

namespace TownOfHostY.Roles.Impostor;
public sealed class CursedWolf : RoleBase, IImpostor
{
    public static readonly SimpleRoleInfo RoleInfo =
         SimpleRoleInfo.Create(
            typeof(CursedWolf),
            player => new CursedWolf(player),
            CustomRoles.CursedWolf,
            () => RoleTypes.Impostor,
            CustomRoleTypes.Impostor,
            20200,
            SetupOptionItem,
            "呪狼"
        );
    public CursedWolf(PlayerControl player)
    : base(
        RoleInfo,
        player
    )
    {
        GuardSpellTimes = OptionGuardSpellTimes.GetInt();
    }
    private static OptionItem OptionGuardSpellTimes;
    enum OptionName
    {
        CursedWolfGuardSpellTimes,
    }
    private static int GuardSpellTimes;
    int SpellCount;

    public static void SetupOptionItem()
    {
        OptionGuardSpellTimes = IntegerOptionItem.Create(RoleInfo, 10, OptionName.CursedWolfGuardSpellTimes, new(1, 15, 1), 3, false)
            .SetValueFormat(OptionFormat.Times);
    }
    public override void Add()
    {
        var playerId = Player.PlayerId;

        SpellCount = GuardSpellTimes;
        Logger.Info($"{Utils.GetPlayerById(playerId)?.GetNameWithRole()} : 残り{SpellCount}回", "CursedWolf");
    }
    private void SendRPC()
    {
        using var sender = CreateSender(CustomRPC.SetCursedWolfSpellCount);
        sender.Writer.Write(SpellCount);
    }
    public override void ReceiveRPC(MessageReader reader, CustomRPC rpcType)
    {
        if (rpcType != CustomRPC.SetCursedWolfSpellCount) return;

        SpellCount = reader.ReadInt32();
    }

    public override bool OnCheckMurderAsTarget(MurderInfo info)
    {
        (var killer, var target) = info.AttemptTuple;
        if (SpellCount <= 0) return true;

        // ガード
        killer.RpcGuardAndKill(target);
        target.RpcGuardAndKill(target);
        SpellCount -= 1;
        SendRPC();
        Logger.Info($"{target.GetNameWithRole()} : 残り{SpellCount}回", "CursedWolf");

        //切り返す
        PlayerState.GetByPlayerId(killer.PlayerId).DeathReason = CustomDeathReason.Spell;
        target.RpcMurderPlayerEx(killer);
        // 自身は斬られない
        info.CanKill = false;
        return false;
    }
    public override string GetProgressText(bool comms = false) => Utils.ColorString(Palette.ImpostorRed, $"({SpellCount})");
}