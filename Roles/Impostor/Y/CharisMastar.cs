using System.Collections.Generic;
using static TownOfHostY.Translator;
using System.Linq;
using AmongUs.GameOptions;
using Hazel;

using TownOfHostY.Roles.Core;
using TownOfHostY.Roles.Core.Interfaces;
using static TownOfHostY.Utils;
using UnityEngine;

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
        NotGatherPlayerKill = OptionNotGatherPlayerKill.GetBool();
        CanAllPlayerGather = OptionCanAllPlayerGather.GetBool();

    }
    enum OptionName
    {
        CharisMastarGatherCount,
        CharisMastarNotGatherPlayerKill,
        CharisMastarCanAllPlayerGather,
    }
    private static float KillCooldown;
    public static bool NotGatherPlayerKill;
    public static bool CanAllPlayerGather;
    private static OptionItem OptionKillCooldown;
    private static OptionItem OptionGatherCount;
    private static OptionItem OptionNotGatherPlayerKill;
    private static OptionItem OptionCanAllPlayerGather;
    public List<byte> GatherChoosePlayer = new();
    int GatherCount;
    int NowGatherCount;
    public float CalculateKillCooldown() => KillCooldown;
    public override string GetAbilityButtonText() => GetString("CharisMastarGatherButtonText");

    private static void SetUpOptionItem()
    {
        OptionKillCooldown = FloatOptionItem.Create(RoleInfo, 10, GeneralOption.KillCooldown, new(2.5f, 180f, 2.5f), 30f, false)
                .SetValueFormat(OptionFormat.Seconds);
        OptionGatherCount = IntegerOptionItem.Create(RoleInfo, 11, OptionName.CharisMastarGatherCount, new(1, 10, 1), 3, false)
            .SetValueFormat(OptionFormat.Pieces);
        OptionNotGatherPlayerKill = BooleanOptionItem.Create(RoleInfo, 13, OptionName.CharisMastarNotGatherPlayerKill, true, false);
        OptionCanAllPlayerGather = BooleanOptionItem.Create(RoleInfo, 14, OptionName.CharisMastarCanAllPlayerGather, true, false);
    }
    public override void Add()
    {
        NowGatherCount = GatherCount;
        GatherChoosePlayer.Clear();
    }
    public override bool OnCheckVanish()
    {
        if (NowGatherCount == 0) return false;
        Vector2[] targetPositions = new Vector2[]
        {
            new(7.76f, 8.56f),
        };
        //Vector2 targetPosition = new(7.76f, 8.56f);//ジップラインの座標

        /* 生存者全員をワープする処理*/
        foreach (Vector2 targetPosition in targetPositions)
        {
            foreach (var Worptarget in Main.AllAlivePlayerControls)
            {
                if (Worptarget.MyPhysics.Animations.IsPlayingAnyLadderAnimation() || Vector2.Distance(Worptarget.GetTruePosition(), targetPosition) <= 1.9f)
                {

                    if (NotGatherPlayerKill)//集まらないplayerをキルするがONの時
                    {
                        Worptarget.SetRealKiller(Player);
                        Worptarget.RpcMurderPlayer(Worptarget);//集合しない時キルする設定なのでkillする
                        GatherChoosePlayer.Remove(Worptarget.PlayerId); // キル後にリストから削除
                        PlayerState.GetByPlayerId(Worptarget.PlayerId).DeathReason = CustomDeathReason.NotGather;
                        Logger.Info($"ターゲットが特定の位置にいたためキルしました。", "CharisMastar");
                    }
                    else
                    {
                        return false;
                    }
                    Logger.Info($"ワープできませんでした。", "CharisMastar");
                }
                else if (Worptarget.IsAlive())//全員を飛ばす。
                {
                    if (CanAllPlayerGather || !GatherChoosePlayer.Any())//全員を集めるがtrueの時 または、リストが空の時
                    {
                        var NearestVent = GetNearestVent();
                        Worptarget.MyPhysics.RpcExitVent(NearestVent.Id);
                    }
                    if (!CanAllPlayerGather)//全員を集めるがfalseの時
                    {
                        if (GatherChoosePlayer.Contains(Worptarget.PlayerId))
                        {
                            var NearestVent = GetNearestVent();
                            Worptarget.MyPhysics.RpcExitVent(NearestVent.Id);
                        }
                    }
                }
            }
        }
        NowGatherCount--;
        return false;
    }
    Vent GetNearestVent()
    {
        var vents = ShipStatus.Instance.AllVents.OrderBy(v => (Player.transform.position - v.transform.position).magnitude);
        return vents.First();
    }
    public void OnCheckMurderAsKiller(MurderInfo info)
    {
        if (!info.CanKill || NowGatherCount == 0 || CanAllPlayerGather) return;

        var (killer, target) = info.AttemptTuple;
        if (!CanAllPlayerGather)//全員を集めるがfalseの時
        {
            info.DoKill = killer.CheckDoubleTrigger(target, () => { SetGatherPlayer(target); });
        }
        else
        {
            info.DoKill = false;//killをしない。
            SetGatherPlayer(target);
        }
    }
    public void SetGatherPlayer(PlayerControl target)
    {
        if (target.IsAlive())
        {
            GatherChoosePlayer.Add(target.PlayerId);
            GatherChoosePlayer.Add(Player.PlayerId);
            SendRPC(target.PlayerId);
        }
    }
    public void SendRPC(byte targetId)
    {
        using var sender = CreateSender(CustomRPC.SetCharisMastarMark);
        sender.Writer.Write(targetId);
    }
    public override void ReceiveRPC(MessageReader reader, CustomRPC rpcType)
    {
        if (rpcType != CustomRPC.SetCharisMastarMark) return;

        GatherChoosePlayer.Add(reader.ReadByte());
    }
    public override string GetMark(PlayerControl seer, PlayerControl seen, bool isForMeeting = false)
    {
        // seenが省略の場合seer
        seen ??= seer;

        if (seer.Is(CustomRoles.CharisMastar) && seer != seen && !CanAllPlayerGather)
        {
            if (GatherChoosePlayer.Contains(seen.PlayerId))
            {
                return ColorString(RoleInfo.RoleColor, "◎");
            }
        }

        return string.Empty;
    }
}