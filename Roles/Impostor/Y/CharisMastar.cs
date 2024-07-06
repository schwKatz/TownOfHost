using System.Collections.Generic;
using System.Linq;
using AmongUs.GameOptions;
using Hazel;

using TownOfHostY.Roles.Core;
using TownOfHostY.Roles.Core.Interfaces;
using static TownOfHostY.Utils;

namespace TownOfHostY.Roles.Impostor;
public sealed class CharisMastar : RoleBase, IImpostor, ISidekickable
{
    public static readonly SimpleRoleInfo RoleInfo =
        SimpleRoleInfo.Create(
            typeof(CharisMastar),
            player => new CharisMastar(player),
            CustomRoles.CharisMastar,
            () => RoleTypes.Phantom,
            CustomRoleTypes.Impostor,
            (int)Options.offsetId.ImpY + 2000,//仮
            SetUpOptionItem,
            "カリスマスター"
        );
    public CharisMastar(PlayerControl player)
    : base(
        RoleInfo,
        player
    )
    {
        KillCooldown = OptionKillCooldown.GetFloat();
        GatherCount = OptionGatherCount.GetInt();
        GatherCooldown = OptionGatherCooldown.GetFloat();
        NotGatherPlayerKill = OptionNotGatherPlayerKill.GetBool();
        GathersMode = (GatherMode)OptionGatherMode.GetValue();
    }
    public enum GatherMode
    {
        CanChoose,
        EveryoneGather,
    };
    public static readonly string[] GatherModeText =
    {
        "CharisMastarGatherMode.CanChoose",
        "CharisMastarGatherMode.EveryoneGather",
    };
    enum OptionName
    {
        CharisMastarGatherCount,
        CharisMastarGatherCooldown,
        CharisMastarNotGatherPlayerKill,
        CharisMastarGatherMode,
    }
    private static float KillCooldown;
    private static float GatherCooldown;
    public static bool NotGatherPlayerKill;
    private static OptionItem OptionKillCooldown;
    private static OptionItem OptionGatherCount;
    private static OptionItem OptionGatherCooldown;
    private static OptionItem OptionNotGatherPlayerKill;
    public static StringOptionItem OptionGatherMode;
    public static GatherMode GathersMode;
    public List<byte> GatherChoosePlayer = new();
    int GatherCount;
    int NowGatherCount;
    private static void SetUpOptionItem()
    {
        OptionKillCooldown = FloatOptionItem.Create(RoleInfo, 10, GeneralOption.KillCooldown, new(2.5f, 180f, 2.5f), 30f, false)
                .SetValueFormat(OptionFormat.Seconds);
        OptionGatherCount = IntegerOptionItem.Create(RoleInfo, 11, OptionName.CharisMastarGatherCount, new(1, 10, 1), 3, false)
            .SetValueFormat(OptionFormat.Pieces);
        OptionGatherCooldown = FloatOptionItem.Create(RoleInfo, 12, OptionName.CharisMastarGatherCooldown, new(5f, 900f, 5f), 30f, false)
            .SetValueFormat(OptionFormat.Seconds);
        OptionNotGatherPlayerKill = BooleanOptionItem.Create(RoleInfo, 13, OptionName.CharisMastarNotGatherPlayerKill, true, false);
        OptionGatherMode = StringOptionItem.Create(RoleInfo, 14, OptionName.CharisMastarGatherMode, GatherModeText, 2, false);
    }
    public override bool OnCheckVanish()
    {
        if (NowGatherCount == 0) return false;

        /* 生存者全員をワープする処理*/
        foreach (var Worptarget in Main.AllAlivePlayerControls)
        {
            /* 梯子を使っている場合*/
            if (Worptarget.MyPhysics.Animations.IsPlayingAnyLadderAnimation() && !Worptarget.Is(CustomRoleTypes.Impostor))
            {
                if (NotGatherPlayerKill)
                {
                    Worptarget.SetRealKiller(Player);
                    Worptarget.RpcMurderPlayer(Worptarget);//集合しない時キルする設定なのでkillする
                    GatherChoosePlayer.Remove(Worptarget.PlayerId); // キル後にリストから削除
                    PlayerState.GetByPlayerId(Worptarget.PlayerId).DeathReason = CustomDeathReason.NotGather;
                }
                else
                {
                    return false;
                }
                Logger.Info($"ワープできませんでした。", "CharisMastar");
            }
            /* 生存者のみ、飛ばす*/
            if (Worptarget.IsAlive())
            {
                if (GathersMode == GatherMode.EveryoneGather)
                {
                    var NearestVent = GetNearestVent();
                    Worptarget.MyPhysics.RpcExitVent(NearestVent.Id);
                }
                if (GathersMode == GatherMode.CanChoose)
                {
                    if (GatherChoosePlayer.Contains(Worptarget.PlayerId))
                    {
                        var NearestVent = GetNearestVent();
                        Worptarget.MyPhysics.RpcExitVent(NearestVent.Id);
                    }
                }
            }
        }
        NowGatherCount--;
        Player.RpcResetAbilityCooldown();
        return false;
    }
    Vent GetNearestVent()
    {
        var vents = ShipStatus.Instance.AllVents.OrderBy(v => (Player.transform.position - v.transform.position).magnitude);
        return vents.First();
    }
    public void OnCheckMurderAsKiller(MurderInfo info)
    {
        if (!info.CanKill || NowGatherCount == 0) return;

        var (killer, target) = info.AttemptTuple;
        if (GathersMode == GatherMode.CanChoose)
        {
            info.DoKill = killer.CheckDoubleTrigger(target, () => { SetGatherPlayer(target); });
        }
        else
        {
            info.DoKill = false;//killをしない。
            SetGatherPlayer(target);
        }
    }
}