using System.Collections.Generic;
using AmongUs.GameOptions;
using Hazel;
using InnerNet;

using static TownOfHostY.Options;
using TownOfHostY.Roles.Core;
using TownOfHostY.Roles.Core.Interfaces;

namespace TownOfHostY.Roles.Neutral
{
    public sealed class DarkHide : RoleBase, IKiller
    {
        public static readonly SimpleRoleInfo RoleInfo =
            SimpleRoleInfo.Create(
                typeof(DarkHide),
                player => new DarkHide(player),
                CustomRoles.DarkHide,
                () => RoleTypes.Impostor,
                CustomRoleTypes.Neutral,
                60200,
                SetupOptionItem,
                "ダークハイド",
                "#483d8b",
                true,
                countType: CountTypes.Crew,
                assignCountRule: new(1, 1, 1)
            );
        public DarkHide(PlayerControl player)
        : base(
            RoleInfo,
            player,
            () => HasTask.False
        )
        {
            KillCooldown = OptionKillCooldown.GetFloat();
            HasImpostorVision = OptionHasImpostorVision.GetBool();
            CanCountNeutralKiller = OptionCanCountNeutralKiller.GetBool();

            IsWinKill = false;
        }

        private static OptionItem OptionKillCooldown;
        private static OptionItem OptionHasImpostorVision;
        public static OptionItem OptionCanCountNeutralKiller;
        enum OptionName
        {
            DarkHideCanCountNeutralKiller,
        }
        private static float KillCooldown;
        private static bool HasImpostorVision;
        public static bool CanCountNeutralKiller;

        public bool IsWinKill = false;

        private static void SetupOptionItem()
        {
            OptionKillCooldown = FloatOptionItem.Create(RoleInfo, 10, GeneralOption.KillCooldown, new(2.5f, 180f, 2.5f), 30f, false)
                .SetValueFormat(OptionFormat.Seconds);
            OptionHasImpostorVision = BooleanOptionItem.Create(RoleInfo, 11, GeneralOption.ImpostorVision, false, false);
            OptionCanCountNeutralKiller = BooleanOptionItem.Create(RoleInfo, 12, OptionName.DarkHideCanCountNeutralKiller, false, false);
        }

        public void OnMurderPlayerAsKiller(MurderInfo info)
        {
            if (!info.IsSuicide)
            {
                (var killer, var target) = info.AttemptTuple;

                var targetRole = target.GetCustomRole();
                if (!IsWinKill) IsWinKill = targetRole.IsImpostor();
                if (CanCountNeutralKiller && target.IsNeutralKiller()) IsWinKill = true;

                foreach (var pc in Main.AllPlayerControls)
                {
                    if (pc.Data.Disconnected) continue;
                    MessageWriter SabotageFixWriter = AmongUsClient.Instance.StartRpcImmediately(ShipStatus.Instance.NetId, (byte)RpcCalls.RepairSystem, SendOption.Reliable, pc.GetClientId());
                    SabotageFixWriter.Write((byte)SystemTypes.Electrical);
                    MessageExtensions.WriteNetObject(SabotageFixWriter, pc);
                    AmongUsClient.Instance.FinishRpcImmediately(SabotageFixWriter);
                }
            }
        }

        public float CalculateKillCooldown() => KillCooldown;
        public override void ApplyGameOptions(IGameOptions opt) => opt.SetVision(HasImpostorVision);
        public override bool OnInvokeSabotage(SystemTypes systemType) => false;
    }
}