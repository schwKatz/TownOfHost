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
    private Vector2 LastPosition; // 元の位置を保存する変数
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
    private Vector2 GetTeleportPosition()
    {
        // マップに応じた座標を選択
        switch ((MapNames)Main.NormalOptions.MapId)
        {
            case MapNames.Airship:
                return new Vector2(-22.13f, 0.56f);
            case MapNames.Skeld:
                return new Vector2(2.96f, -8.62f);
            case MapNames.Polus:
                return new Vector2(22.63f, -21.75f);
            case MapNames.Mira:
                return new Vector2(22.22f, 18.77f);
            default:
                // デフォルトの座標（必要に応じて設定）
                return new Vector2(0f, 0f);
        }
    }
    public override bool OnCheckVanish()
    {
        // 移動前の位置を保持
        LastPosition = Player.GetTruePosition();

        // プレイヤーの位置を指定された位置に強制的に変更
        var teleportPosition = GetTeleportPosition();
        Player.SnapToTeleport(teleportPosition);
        Utils.NotifyRoles();

        // プレイヤーの足止め
        Main.AllPlayerSpeed[Player.PlayerId] = Main.MinSpeed;
        Player.MarkDirtySettings();
        Logger.Info($"{Player.GetNameWithRole()} : プレイヤーの足止め", "EvilHacker");

        return true;
    }
}