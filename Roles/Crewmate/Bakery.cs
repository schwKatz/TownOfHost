using System.Collections.Generic;
using Hazel;

using AmongUs.GameOptions;
using static TownOfHost.Translator;
using static TownOfHost.Utils;
using TownOfHost.Roles.Core;

namespace TownOfHost.Roles.Crewmate;
public sealed class Bakery : RoleBase
{
    public static readonly SimpleRoleInfo RoleInfo =
        new(
            typeof(Bakery),
            player => new Bakery(player),
            CustomRoles.Bakery,
            () => RoleTypes.Crewmate,
            CustomRoleTypes.Crewmate,
            35000,
            SetupOptionItem,
            "bak",
            "#b58428"
        );
    public Bakery(PlayerControl player)
    : base(
        RoleInfo,
        player,

    )
    {
        ChangeChances = OptionChangeChances.GetInt();
    }
    public static OptionItem OptionChangeChances;
    enum OptionName
    {
        BakeryChangeChances,
    }
    private static int ChangeChances;
    PlayerControl PoisonTarget = null;

    private static void SetupOptionItem()
    {
        OptionChangeChances = FloatOptionItem.Create(RoleInfo, 10, OptionName.BakeryChangeChances, new(0, 20, 2), 10, false)
            .SetValueFormat(OptionFormat.Percent);
    }

    public override void OnStartMeeting()
    {
        var pc = Player;
        var BakeryTitle = $"<color={RoleInfo.RoleColorCode}>{GetString("PanAliveMessageTitle")}</color>";

        if (pc.Is(CustomRoles.NBakery) && !pc.IsAlive())
        {
            if (PoisonTarget.IsAlive())
            {
                SendMessage(GetString("BakeryChangeNow"), title: BakeryTitle);
            }
            else
            {
                PoisonTarget = null;
                SendMessage(GetString("BakeryChangeNONE"), title: BakeryTitle);
            }
        }
        if (pc.Is(CustomRoles.Bakery) && !pc.IsAlive())
        {
            string panMessage = "";
            int chance = UnityEngine.Random.Range(1, 101);
            if (chance <= ChangeChances)
            {
                panMessage = GetString("BakeryChange");
                pc.RpcSetCustomRole(CustomRoles.NBakery);
            }
            else if (chance <= 77) panMessage = GetString("PanAlive");
            else if (chance <= 79) panMessage = GetString("PanAlive1");
            else if (chance <= 81) panMessage = GetString("PanAlive2");
            else if (chance <= 82) panMessage = GetString("PanAlive3");
            else if (chance <= 84) panMessage = GetString("PanAlive4");
            else if (chance <= 86) panMessage = GetString("PanAlive5");
            else if (chance <= 87) panMessage = GetString("PanAlive6");
            else if (chance <= 88) panMessage = GetString("PanAlive7");
            else if (chance <= 90) panMessage = GetString("PanAlive8");
            else if (chance <= 92) panMessage = GetString("PanAlive9");
            else if (chance <= 94) panMessage = GetString("PanAlive10");
            else if (chance <= 96) panMessage = GetString("PanAlive11");
            else if (chance <= 98)
            {
                List<PlayerControl> targetList = new();
                var rand = IRandom.Instance;
                foreach (var p in Main.AllAlivePlayerControls)
                {
                    if (p.Is(CustomRoles.Bakery)) continue;
                    targetList.Add(p);
                }
                var TargetPlayer = targetList[rand.Next(targetList.Count)];
                panMessage = string.Format(Translator.GetString("PanAlive12"), TargetPlayer.GetRealName());
            }
            else if (chance <= 100)
            {
                List<PlayerControl> targetList = new();
                var rand = IRandom.Instance;
                foreach (var p in Main.AllAlivePlayerControls)
                {
                    if (p.Is(CustomRoles.Bakery)) continue;
                    targetList.Add(p);
                }
                var TargetPlayer = targetList[rand.Next(targetList.Count)];
                panMessage = string.Format(Translator.GetString("PanAlive13"), TargetPlayer.GetRealName());
            }

            SendMessage(panMessage, title: BakeryTitle);
        }
    }
}