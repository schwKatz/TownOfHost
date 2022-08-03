using System.Collections.Generic;
using UnityEngine;

namespace TownOfHost
{
    public static class SchrodingerCat
    {
        private static readonly int Id = 50400;
        public static List<byte> playerIdList = new();

        public static CustomOption CanWinTheCrewmateBeforeChange;
        private static CustomOption ExiledTeamChanges;


        public static void SetupCustomOption()
        {
            Options.SetupRoleOptions(Id, CustomRoles.SchrodingerCat);
            CanWinTheCrewmateBeforeChange = CustomOption.Create(Id + 10, Color.white, "CanBeforeSchrodingerCatWinTheCrewmate", false, Options.CustomRoleSpawnChances[CustomRoles.SchrodingerCat]);
            ExiledTeamChanges = CustomOption.Create(Id + 11, Color.white, "SchrodingerCatExiledTeamChanges", false, Options.CustomRoleSpawnChances[CustomRoles.SchrodingerCat]);
        }
        public static void Init()
        {
            playerIdList = new();
        }
        public static void Add(byte mare)
        {
            playerIdList.Add(mare);
        }
        public static bool IsEnable => playerIdList.Count > 0;
        public static bool CanUseKillButton(PlayerControl player)
        {
            if (player.Data.IsDead)
                return false;

            return player.GetCustomRole() switch
            {
                CustomRoles.SchrodingerCat or CustomRoles.CSchrodingerCat => false,
                CustomRoles.ISchrodingerCat or CustomRoles.JSchrodingerCat or CustomRoles.EgoSchrodingerCat => true,
                _ => false
            };
        }
        public static void OnKilled(PlayerControl killer, PlayerControl target)
        {
            killer.RpcGuardAndKill(target);
            if (PlayerState.GetDeathReason(target.PlayerId) == PlayerState.DeathReason.Sniped)
            {
                //スナイプされた時
                target.RpcSetCustomRole(CustomRoles.ISchrodingerCat);
                var sniperId = Sniper.GetSniper(target.PlayerId);
                NameColorManager.Instance.RpcAdd(sniperId, target.PlayerId, $"{Utils.GetRoleColorCode(CustomRoles.SchrodingerCat)}");
            }
            else if (killer.GetBountyTarget() == target)
                killer.ResetBountyTarget();//ターゲットの選びなおし
            else
            {
                SerialKiller.OnCheckMurder(killer, isKilledSchrodingerCat: true);
                switch (killer.GetCustomRole())
                {
                    case CustomRoles.Sheriff:
                        target.RpcSetCustomRole(CustomRoles.CSchrodingerCat);
                        break;
                    case CustomRoles.Egoist:
                    case CustomRoles.EgoSchrodingerCat:
                        target.RpcSetCustomRole(CustomRoles.EgoSchrodingerCat);
                        break;
                    case CustomRoles.Jackal:
                    case CustomRoles.JSchrodingerCat:
                        target.RpcSetCustomRole(CustomRoles.JSchrodingerCat);
                        break;
                    default:
                        if (killer.GetCustomRole().IsImpostor())
                            target.RpcSetCustomRole(CustomRoles.ISchrodingerCat);
                        break;
                }

                NameColorManager.Instance.RpcAdd(killer.PlayerId, target.PlayerId, $"{Utils.GetRoleColorCode(CustomRoles.SchrodingerCat)}");
            }
            Utils.NotifyRoles();
            Utils.CustomSyncAllSettings();
        }
        public static void FixedUpdate(PlayerControl player)
        {
        }
        public static void ExiledCatTeamChange(PlayerControl player)
        {
            if (player == null || !(player.Is(CustomRoles.SchrodingerCat) && ExiledTeamChanges.GetBool())) return;

            var rand = new System.Random();
            List<CustomRoles> RandSchrodinger = new()
            {
                CustomRoles.CSchrodingerCat,
                CustomRoles.ISchrodingerCat
            };
            foreach (var pc in PlayerControl.AllPlayerControls)
            {
                if (CustomRoles.Egoist.IsEnable() && pc.Is(CustomRoles.Egoist) && !pc.Data.IsDead)
                    RandSchrodinger.Add(CustomRoles.EgoSchrodingerCat);

                if (CustomRoles.Jackal.IsEnable() && pc.Is(CustomRoles.Jackal) && !pc.Data.IsDead)
                    RandSchrodinger.Add(CustomRoles.JSchrodingerCat);
            }
            var SchrodingerTeam = RandSchrodinger[rand.Next(RandSchrodinger.Count)];
            player.RpcSetCustomRole(SchrodingerTeam);
        }
    }
}