using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Hazel;
using InnerNet;
using UnityEngine;
using static TownOfHost.Translator;

namespace TownOfHost
{
    static class ExtendedPlayerControl
    {
        public static void RpcSetCustomRole(this PlayerControl player, CustomRoles role)
        {
            if (role < CustomRoles.NotAssigned)
            {
                Main.PlayerStates[player.PlayerId].MainRole = role;
            }
            else if (role >= CustomRoles.NotAssigned)   //500:NoSubRole 501~:SubRole
            {
                Main.PlayerStates[player.PlayerId].SetSubRole(role);
            }
            if (AmongUsClient.Instance.AmHost)
            {
                MessageWriter writer = AmongUsClient.Instance.StartRpcImmediately(PlayerControl.LocalPlayer.NetId, (byte)CustomRPC.SetCustomRole, Hazel.SendOption.Reliable, -1);
                writer.Write(player.PlayerId);
                writer.WritePacked((int)role);
                AmongUsClient.Instance.FinishRpcImmediately(writer);
            }
        }
        public static void RpcSetCustomRole(byte PlayerId, CustomRoles role)
        {
            if (AmongUsClient.Instance.AmHost)
            {
                MessageWriter writer = AmongUsClient.Instance.StartRpcImmediately(PlayerControl.LocalPlayer.NetId, (byte)CustomRPC.SetCustomRole, Hazel.SendOption.Reliable, -1);
                writer.Write(PlayerId);
                writer.WritePacked((int)role);
                AmongUsClient.Instance.FinishRpcImmediately(writer);
            }
        }

        public static void RpcExile(this PlayerControl player)
        {
            RPC.ExileAsync(player);
        }
        public static InnerNet.ClientData GetClient(this PlayerControl player)
        {
            var client = AmongUsClient.Instance.allClients.ToArray().Where(cd => cd.Character.PlayerId == player.PlayerId).FirstOrDefault();
            return client;
        }
        public static int GetClientId(this PlayerControl player)
        {
            var client = player.GetClient();
            return client == null ? -1 : client.Id;
        }
        public static CustomRoles GetCustomRole(this GameData.PlayerInfo player)
        {
            return player == null || player.Object == null ? CustomRoles.Crewmate : player.Object.GetCustomRole();
        }
        /// <summary>
        /// ※サブロールは取得できません。
        /// </summary>
        public static CustomRoles GetCustomRole(this PlayerControl player)
        {
            if (player == null)
            {
                var caller = new System.Diagnostics.StackFrame(1, false);
                var callerMethod = caller.GetMethod();
                string callerMethodName = callerMethod.Name;
                string callerClassName = callerMethod.DeclaringType.FullName;
                Logger.Warn(callerClassName + "." + callerMethodName + "がCustomRoleを取得しようとしましたが、対象がnullでした。", "GetCustomRole");
                return CustomRoles.Crewmate;
            }
            var GetValue = Main.PlayerStates.TryGetValue(player.PlayerId, out var State);

            return GetValue ? State.MainRole : CustomRoles.Crewmate;
        }

        public static List<CustomRoles> GetCustomSubRoles(this PlayerControl player)
        {
            if (player == null)
            {
                Logger.Warn("CustomSubRoleを取得しようとしましたが、対象がnullでした。", "getCustomSubRole");
                return new() { CustomRoles.NotAssigned };
            }
            return Main.PlayerStates[player.PlayerId].SubRoles;
        }
        public static void RpcSetNameEx(this PlayerControl player, string name)
        {
            foreach (var seer in PlayerControl.AllPlayerControls)
            {
                Main.LastNotifyNames[(player.PlayerId, seer.PlayerId)] = name;
            }
            HudManagerPatch.LastSetNameDesyncCount++;

            Logger.Info($"Set:{player?.Data?.PlayerName}:{name} for All", "RpcSetNameEx");
            player.RpcSetName(name);
        }

        public static void RpcSetNamePrivate(this PlayerControl player, string name, bool DontShowOnModdedClient = false, PlayerControl seer = null, bool force = false)
        {
            //player: 名前の変更対象
            //seer: 上の変更を確認することができるプレイヤー
            if (player == null || name == null || !AmongUsClient.Instance.AmHost) return;
            if (seer == null) seer = player;
            if (!force && Main.LastNotifyNames[(player.PlayerId, seer.PlayerId)] == name)
            {
                //Logger.info($"Cancel:{player.name}:{name} for {seer.name}", "RpcSetNamePrivate");
                return;
            }
            Main.LastNotifyNames[(player.PlayerId, seer.PlayerId)] = name;
            HudManagerPatch.LastSetNameDesyncCount++;
            Logger.Info($"Set:{player?.Data?.PlayerName}:{name} for {seer.GetNameWithRole()}", "RpcSetNamePrivate");

            var clientId = seer.GetClientId();
            MessageWriter writer = AmongUsClient.Instance.StartRpcImmediately(player.NetId, (byte)RpcCalls.SetName, Hazel.SendOption.Reliable, clientId);
            writer.Write(name);
            writer.Write(DontShowOnModdedClient);
            AmongUsClient.Instance.FinishRpcImmediately(writer);
        }
        public static void RpcSetRoleDesync(this PlayerControl player, RoleTypes role, int clientId)
        {
            //player: 名前の変更対象

            if (player == null) return;
            MessageWriter writer = AmongUsClient.Instance.StartRpcImmediately(player.NetId, (byte)RpcCalls.SetRole, Hazel.SendOption.Reliable, clientId);
            writer.Write((ushort)role);
            AmongUsClient.Instance.FinishRpcImmediately(writer);
        }

        public static void RpcGuardAndKill(this PlayerControl killer, PlayerControl target = null, int colorId = 0)
        {
            if (target == null) target = killer;
            // Host
            if (killer.AmOwner)
            {
                killer.ProtectPlayer(target, colorId);
                killer.MurderPlayer(target);
            }
            // Other Clients
            if (killer.PlayerId != 0)
            {
                var sender = CustomRpcSender.Create("GuardAndKill Sender", SendOption.None);
                sender.StartMessage(killer.GetClientId());
                sender.StartRpc(killer.NetId, (byte)RpcCalls.ProtectPlayer)
                    .WriteNetObject((InnerNetObject)target)
                    .Write(colorId)
                    .EndRpc();
                sender.StartRpc(killer.NetId, (byte)RpcCalls.MurderPlayer)
                    .WriteNetObject((InnerNetObject)target)
                    .EndRpc();
                sender.EndMessage();
                sender.SendMessage();
            }
        }
        public static void SetKillCooldown(this PlayerControl player, float time)
        {
            CustomRoles role = player.GetCustomRole();
            if (!(role.IsImpostor() || player.IsNeutralKiller() || role is CustomRoles.Arsonist or CustomRoles.Sheriff)) return;
            if (player.AmOwner)
            {
                player.SetKillTimer(time);
            }
            else
            {
                Main.AllPlayerKillCooldown[player.PlayerId] = time * 2;
                player.CustomSyncSettings();
                player.RpcGuardAndKill();
            }
        }
        public static void RpcSpecificMurderPlayer(this PlayerControl killer, PlayerControl target = null)
        {
            if (target == null) target = killer;
            if (killer.AmOwner)
            {
                killer.MurderPlayer(target);
            }
            else
            {
                MessageWriter messageWriter = AmongUsClient.Instance.StartRpcImmediately(killer.NetId, (byte)RpcCalls.MurderPlayer, SendOption.Reliable, killer.GetClientId());
                messageWriter.WriteNetObject(target);
                AmongUsClient.Instance.FinishRpcImmediately(messageWriter);
            }
        }
        [Obsolete]
        public static void RpcSpecificProtectPlayer(this PlayerControl killer, PlayerControl target = null, int colorId = 0)
        {
            if (AmongUsClient.Instance.AmClient)
            {
                killer.ProtectPlayer(target, colorId);
            }
            MessageWriter messageWriter = AmongUsClient.Instance.StartRpcImmediately(killer.NetId, (byte)RpcCalls.ProtectPlayer, SendOption.Reliable, killer.GetClientId());
            messageWriter.WriteNetObject(target);
            messageWriter.Write(colorId);
            AmongUsClient.Instance.FinishRpcImmediately(messageWriter);
        }
        public static void RpcResetAbilityCooldown(this PlayerControl target)
        {
            if (!AmongUsClient.Instance.AmHost) return; //ホスト以外が実行しても何も起こさない
            Logger.Info($"アビリティクールダウンのリセット:{target.name}({target.PlayerId})", "RpcResetAbilityCooldown");
            if (PlayerControl.LocalPlayer == target)
            {
                //targetがホストだった場合
                PlayerControl.LocalPlayer.Data.Role.SetCooldown();
            }
            else
            {
                //targetがホスト以外だった場合
                MessageWriter writer = AmongUsClient.Instance.StartRpcImmediately(target.NetId, (byte)RpcCalls.ProtectPlayer, SendOption.None, target.GetClientId());
                writer.WriteNetObject(target);
                writer.Write(0);
                AmongUsClient.Instance.FinishRpcImmediately(writer);
            }
            /*
                プレイヤーがバリアを張ったとき、そのプレイヤーの役職に関わらずアビリティーのクールダウンがリセットされます。
                ログの追加により無にバリアを張ることができなくなったため、代わりに自身に0秒バリアを張るように変更しました。
                この変更により、役職としての守護天使が無効化されます。
                ホストのクールダウンは直接リセットします。
            */
        }
        public static void RpcDesyncRepairSystem(this PlayerControl target, SystemTypes systemType, int amount)
        {
            MessageWriter messageWriter = AmongUsClient.Instance.StartRpcImmediately(ShipStatus.Instance.NetId, (byte)RpcCalls.RepairSystem, SendOption.Reliable, target.GetClientId());
            messageWriter.Write((byte)systemType);
            messageWriter.WriteNetObject(target);
            messageWriter.Write((byte)amount);
            AmongUsClient.Instance.FinishRpcImmediately(messageWriter);
        }

        /*public static void RpcBeKilled(this PlayerControl player, PlayerControl KilledBy = null) {
            if(!AmongUsClient.Instance.AmHost) return;
            byte KilledById;
            if(KilledBy == null)
                KilledById = byte.MaxValue;
            else
                KilledById = KilledBy.PlayerId;

            MessageWriter writer = AmongUsClient.Instance.StartRpcImmediately(player.NetId, (byte)CustomRPC.BeKilled, Hazel.SendOption.Reliable, -1);
            writer.Write(player.PlayerId);
            writer.Write(KilledById);
            AmongUsClient.Instance.FinishRpcImmediately(writer);

            RPC.BeKilled(player.PlayerId, KilledById);
        }*/
        public static void CustomSyncSettings(this PlayerControl player)
        {
            if (player == null || !AmongUsClient.Instance.AmHost) return;
            if (Main.RealOptionsData == null)
            {
                Main.RealOptionsData = PlayerControl.GameOptions.DeepCopy();
            }

            var clientId = player.GetClientId();
            var opt = Main.RealOptionsData.DeepCopy();
            var state = Main.PlayerStates[player.PlayerId];
            opt.BlackOut(state.IsBlackOut);

            CustomRoles role = player.GetCustomRole();
            RoleType roleType = role.GetRoleType();
            switch (roleType)
            {
                case RoleType.Impostor:
                    opt.RoleOptions.ShapeshifterCooldown = Options.DefaultShapeshiftCooldown.GetFloat();
                    break;
                case RoleType.Madmate:
                    opt.RoleOptions.EngineerCooldown = Options.MadmateVentCooldown.GetFloat();
                    opt.RoleOptions.EngineerInVentMaxTime = Options.MadmateVentMaxTime.GetFloat();
                    if (Options.MadmateHasImpostorVision.GetBool())
                        opt.SetVision(player, true);
                    if (Options.MadmateCanSeeOtherVotes.GetBool() && opt.AnonymousVotes)
                        opt.AnonymousVotes = false;
                    break;
            }

            switch (player.GetCustomRole())
            {
                case CustomRoles.Terrorist:
                    goto InfinityVent;
                // case CustomRoles.ShapeMaster:
                //     opt.RoleOptions.ShapeshifterCooldown = 0.1f;
                //     opt.RoleOptions.ShapeshifterLeaveSkin = false;
                //     opt.RoleOptions.ShapeshifterDuration = Options.ShapeMasterShapeshiftDuration.GetFloat();
                //     break;
                case CustomRoles.Warlock:
                    opt.RoleOptions.ShapeshifterCooldown = Main.isCursed ? 1f : Options.DefaultKillCooldown;
                    break;
                case CustomRoles.SerialKiller:
                    SerialKiller.ApplyGameOptions(opt, player);
                    break;
                case CustomRoles.BountyHunter:
                    BountyHunter.ApplyGameOptions(opt);
                    break;
                case CustomRoles.EvilWatcher:
                case CustomRoles.NiceWatcher:
                    if (opt.AnonymousVotes)
                        opt.AnonymousVotes = false;
                    break;
                case CustomRoles.Sheriff:
                case CustomRoles.Arsonist:
                    opt.SetVision(player, false);
                    break;
                case CustomRoles.Lighter:
                    if (player.GetPlayerTaskState().IsTaskFinished)
                    {
                        opt.CrewLightMod = Options.LighterTaskCompletedVision.GetFloat();
                        if (Utils.IsActive(SystemTypes.Electrical) && Options.LighterTaskCompletedDisableLightOut.GetBool())
                            opt.CrewLightMod *= 5;
                    }
                    break;
                case CustomRoles.EgoSchrodingerCat:
                    opt.SetVision(player, true);
                    break;
                case CustomRoles.Doctor:
                    opt.RoleOptions.ScientistCooldown = 0f;
                    opt.RoleOptions.ScientistBatteryCharge = Options.DoctorTaskCompletedBatteryCharge.GetFloat();
                    break;
                case CustomRoles.Mayor:
                    opt.RoleOptions.EngineerCooldown =
                        Main.MayorUsedButtonCount.TryGetValue(player.PlayerId, out var count) && count < Options.MayorNumOfUseButton.GetInt()
                        ? opt.EmergencyCooldown
                        : 300f;
                    opt.RoleOptions.EngineerInVentMaxTime = 1;
                    break;
                case CustomRoles.Mare:
                    Mare.ApplyGameOptions(opt, player.PlayerId);
                    break;
                case CustomRoles.EvilTracker:
                    EvilTracker.ApplyGameOptions(opt, player.PlayerId);
                    break;
                case CustomRoles.Jackal:
                case CustomRoles.JSchrodingerCat:
                    Jackal.ApplyGameOptions(opt, player);
                    break;
                case CustomRoles.JClient:
                    opt.RoleOptions.EngineerCooldown = Options.JClientVentCooldown.GetFloat();
                    opt.RoleOptions.EngineerInVentMaxTime = Options.JClientVentMaxTime.GetFloat();
                    if (Options.JClientHasImpostorVision.GetBool())
                        opt.SetVision(player, true);
                    break;

                InfinityVent:
                    opt.RoleOptions.EngineerCooldown = 0;
                    opt.RoleOptions.EngineerInVentMaxTime = 0;
                    break;
            }
            if (Main.AllPlayerKillCooldown.ContainsKey(player.PlayerId))
            {
                foreach (var kc in Main.AllPlayerKillCooldown)
                {
                    if (kc.Key == player.PlayerId)
                        opt.KillCooldown = kc.Value > 0 ? kc.Value : 0.01f;
                }
            }
            if (Main.AllPlayerSpeed.ContainsKey(player.PlayerId))
            {
                foreach (var speed in Main.AllPlayerSpeed)
                {
                    if (speed.Key == player.PlayerId)
                        opt.PlayerSpeedMod = Mathf.Clamp(speed.Value, Main.MinSpeed, 3f);
                }
            }
            state.taskState.hasTasks = Utils.HasTasks(player.Data, false);
            if (Options.GhostCanSeeOtherVotes.GetBool() && player.Data.IsDead && opt.AnonymousVotes)
                opt.AnonymousVotes = false;
            if (Options.AdditionalEmergencyCooldown.GetBool() &&
                Options.AdditionalEmergencyCooldownThreshold.GetInt() <= PlayerControl.AllPlayerControls.ToArray().Count(x => !x.Data.IsDead))
                opt.EmergencyCooldown += Options.AdditionalEmergencyCooldownTime.GetInt();
            if (Options.SyncButtonMode.GetBool() && Options.SyncedButtonCount.GetValue() <= Options.UsedButtonCount)
                opt.EmergencyCooldown = 3600;
            if ((Options.CurrentGameMode == CustomGameMode.HideAndSeek || Options.IsStandardHAS) && Options.HideAndSeekKillDelayTimer > 0)
            {
                opt.ImpostorLightMod = 0f;
                if (player.GetCustomRole().IsImpostor() || player.Is(CustomRoles.Egoist)) opt.PlayerSpeedMod = Main.MinSpeed;
            }
            opt.DiscussionTime = Mathf.Clamp(Main.DiscussionTime, 0, 300);
            opt.VotingTime = Mathf.Clamp(Main.VotingTime, TimeThief.LowerLimitVotingTime.GetInt(), 300);

            if (Options.AllAliveMeeting.GetBool() && GameData.Instance.AllPlayers.ToArray().Where(x => !x.Object.Is(CustomRoles.GM)).All(x => !x.IsDead))
            {
                opt.DiscussionTime = 0;
                opt.VotingTime = Options.AllAliveMeetingTime.GetInt();
            }

            opt.RoleOptions.ShapeshifterCooldown = Mathf.Max(1f, opt.RoleOptions.ShapeshifterCooldown);
            opt.RoleOptions.ProtectionDurationSeconds = 0f;

            if (player.AmOwner) PlayerControl.GameOptions = opt;
            MessageWriter writer = AmongUsClient.Instance.StartRpcImmediately(PlayerControl.LocalPlayer.NetId, (byte)RpcCalls.SyncSettings, SendOption.Reliable, clientId);
            writer.WriteBytesAndSize(opt.ToBytes(5));
            AmongUsClient.Instance.FinishRpcImmediately(writer);
        }
        public static TaskState GetPlayerTaskState(this PlayerControl player)
        {
            return Main.PlayerStates[player.PlayerId].GetTaskState();
        }

        public static GameOptionsData DeepCopy(this GameOptionsData opt)
        {
            var optByte = opt.ToBytes(5);
            return GameOptionsData.FromBytes(optByte);
        }

        public static string GetDisplayRoleName(this PlayerControl player)
        {
            return Utils.GetDisplayRoleName(player.PlayerId);
        }
        public static string GetSubRoleName(this PlayerControl player)
        {
            var SubRoles = Main.PlayerStates[player.PlayerId].SubRoles;
            if (SubRoles.Count == 0) return "";
            var sb = new StringBuilder();
            foreach (var role in SubRoles)
            {
                if (role == CustomRoles.NotAssigned) continue;
                sb.Append($" + {Utils.GetRoleName(role)}");
            }

            return sb.ToString();
        }
        public static string GetAllRoleName(this PlayerControl player)
        {
            if (!player) return null;
            var text = Utils.GetRoleName(player.GetCustomRole());
            text += player.GetSubRoleName();
            return text;
        }
        public static string GetNameWithRole(this PlayerControl player)
        {
            return $"{player?.Data?.PlayerName}" + (GameStates.IsInGame ? $"({player?.GetAllRoleName()})" : "");
        }
        public static string GetRoleColorCode(this PlayerControl player)
        {
            return Utils.GetRoleColorCode(player.GetCustomRole());
        }
        public static Color GetRoleColor(this PlayerControl player)
        {
            return Utils.GetRoleColor(player.GetCustomRole());
        }
        public static void ResetPlayerCam(this PlayerControl pc, float delay = 0f)
        {
            if (pc == null || !AmongUsClient.Instance.AmHost || pc.AmOwner) return;

            var systemtypes = SystemTypes.Reactor;
            if (PlayerControl.GameOptions.MapId == 2) systemtypes = SystemTypes.Laboratory;

            new LateTask(() =>
            {
                pc.RpcDesyncRepairSystem(systemtypes, 128);
            }, 0f + delay, "Reactor Desync");

            new LateTask(() =>
            {
                pc.RpcSpecificMurderPlayer();
            }, 0.2f + delay, "Murder To Reset Cam");

            new LateTask(() =>
            {
                pc.RpcDesyncRepairSystem(systemtypes, 16);
                if (PlayerControl.GameOptions.MapId == 4) //Airship用
                    pc.RpcDesyncRepairSystem(systemtypes, 17);
            }, 0.4f + delay, "Fix Desync Reactor");
        }
        public static void ReactorFlash(this PlayerControl pc, float delay = 0f)
        {
            if (pc == null) return;
            int clientId = pc.GetClientId();
            // Logger.Info($"{pc}", "ReactorFlash");
            var systemtypes = SystemTypes.Reactor;
            if (PlayerControl.GameOptions.MapId == 2) systemtypes = SystemTypes.Laboratory;
            float FlashDuration = Options.KillFlashDuration.GetFloat();

            pc.RpcDesyncRepairSystem(systemtypes, 128);

            new LateTask(() =>
            {
                pc.RpcDesyncRepairSystem(systemtypes, 16);

                if (PlayerControl.GameOptions.MapId == 4) //Airship用
                    pc.RpcDesyncRepairSystem(systemtypes, 17);
            }, FlashDuration + delay, "Fix Desync Reactor");
        }

        public static string GetRealName(this PlayerControl player, bool isMeeting = false)
        {
            return isMeeting ? player?.Data?.PlayerName : player?.name;
        }
        public static bool CanUseKillButton(this PlayerControl pc)
        {
            return pc.GetCustomRole() switch
            {
                CustomRoles.FireWorks => FireWorks.CanUseKillButton(pc),
                CustomRoles.Mafia => Utils.CanMafiaKill(),
                CustomRoles.Mare => Utils.IsActive(SystemTypes.Electrical),
                CustomRoles.Sniper => Sniper.CanUseKillButton(pc),
                CustomRoles.Sheriff => Sheriff.CanUseKillButton(pc.PlayerId),
                CustomRoles.Arsonist => !pc.IsDouseDone(),
                CustomRoles.Egoist or CustomRoles.Jackal => true,
                _ => pc.Is(RoleType.Impostor),
            };
        }
        public static bool IsDousedPlayer(this PlayerControl arsonist, PlayerControl target)
        {
            if (arsonist == null) return false;
            if (target == null) return false;
            if (Main.isDoused == null) return false;
            Main.isDoused.TryGetValue((arsonist.PlayerId, target.PlayerId), out bool isDoused);
            return isDoused;
        }
        public static void RpcSetDousedPlayer(this PlayerControl player, PlayerControl target, bool isDoused)
        {
            MessageWriter writer = AmongUsClient.Instance.StartRpcImmediately(PlayerControl.LocalPlayer.NetId, (byte)CustomRPC.SetDousedPlayer, SendOption.Reliable, -1);//RPCによる同期
            writer.Write(player.PlayerId);
            writer.Write(target.PlayerId);
            writer.Write(isDoused);
            AmongUsClient.Instance.FinishRpcImmediately(writer);
        }
        public static void ResetKillCooldown(this PlayerControl player)
        {
            Main.AllPlayerKillCooldown[player.PlayerId] = Options.DefaultKillCooldown; //キルクールをデフォルトキルクールに変更
            switch (player.GetCustomRole())
            {
                case CustomRoles.SerialKiller:
                    SerialKiller.ApplyKillCooldown(player.PlayerId); //シリアルキラーはシリアルキラーのキルクールに。
                    break;
                case CustomRoles.TimeThief:
                    TimeThief.SetKillCooldown(player.PlayerId); //タイムシーフはタイムシーフのキルクールに。
                    break;
                case CustomRoles.Mare:
                    Mare.SetKillCooldown(player.PlayerId);
                    break;
                case CustomRoles.Arsonist:
                    Main.AllPlayerKillCooldown[player.PlayerId] = Options.ArsonistCooldown.GetFloat(); //アーソニストはアーソニストのキルクールに。
                    break;
                case CustomRoles.Egoist:
                    Egoist.ApplyKillCooldown(player.PlayerId);
                    break;
                case CustomRoles.Jackal:
                    Jackal.SetKillCooldown(player.PlayerId);
                    break;
                case CustomRoles.Sheriff:
                    Sheriff.SetKillCooldown(player.PlayerId); //シェリフはシェリフのキルクールに。
                    break;
            }
            if (player.PlayerId == LastImpostor.currentId)
                LastImpostor.SetKillCooldown();
        }
        public static void TrapperKilled(this PlayerControl killer, PlayerControl target)
        {
            Logger.Info($"{target?.Data?.PlayerName}はTrapperだった", "Trapper");
            var tmpSpeed = Main.AllPlayerSpeed[killer.PlayerId];
            Main.AllPlayerSpeed[killer.PlayerId] = Main.MinSpeed;    //tmpSpeedで後ほど値を戻すので代入しています。
            ReportDeadBodyPatch.CanReport[killer.PlayerId] = false;
            killer.CustomSyncSettings();
            new LateTask(() =>
            {
                Main.AllPlayerSpeed[killer.PlayerId] = Main.AllPlayerSpeed[killer.PlayerId] - Main.MinSpeed + tmpSpeed;
                ReportDeadBodyPatch.CanReport[killer.PlayerId] = true;
                killer.CustomSyncSettings();
                RPC.PlaySoundRPC(killer.PlayerId, Sounds.TaskComplete);
            }, Options.TrapperBlockMoveTime.GetFloat(), "Trapper BlockMove");
        }
        public static void CanUseImpostorVent(this PlayerControl player)
        {
            switch (player.GetCustomRole())
            {
                case CustomRoles.Sheriff:
                    DestroyableSingleton<HudManager>.Instance.ImpostorVentButton.ToggleVisible(false);
                    player.Data.Role.CanVent = false;
                    return;
                case CustomRoles.Arsonist:
                    bool CanUse = player.IsDouseDone();
                    DestroyableSingleton<HudManager>.Instance.ImpostorVentButton.ToggleVisible(CanUse && !player.Data.IsDead);
                    player.Data.Role.CanVent = CanUse;
                    return;
                case CustomRoles.Jackal:
                    Jackal.CanUseVent(player);
                    return;
            }
        }
        public static bool IsDouseDone(this PlayerControl player)
        {
            if (!player.Is(CustomRoles.Arsonist)) return false;
            var count = Utils.GetDousedPlayerCount(player.PlayerId);
            return count.Item1 == count.Item2;
        }
        public static bool CanMakeMadmate(this PlayerControl player)
        {
            return Options.CanMakeMadmateCount.GetInt() > Main.SKMadmateNowCount
                    && player != null
                    && player.Data.Role.Role == RoleTypes.Shapeshifter
                    && player.GetCustomRole().CanMakeMadmate();
        }
        public static void RpcExileV2(this PlayerControl player)
        {
            player.Exiled();
            MessageWriter writer = AmongUsClient.Instance.StartRpcImmediately(player.NetId, (byte)RpcCalls.Exiled, SendOption.None, -1);
            AmongUsClient.Instance.FinishRpcImmediately(writer);
        }
        public static void RpcMurderPlayerV2(this PlayerControl killer, PlayerControl target)
        {
            if (target == null) target = killer;
            if (AmongUsClient.Instance.AmClient)
            {
                killer.MurderPlayer(target);
            }
            MessageWriter messageWriter = AmongUsClient.Instance.StartRpcImmediately(killer.NetId, (byte)RpcCalls.MurderPlayer, SendOption.None, -1);
            messageWriter.WriteNetObject(target);
            AmongUsClient.Instance.FinishRpcImmediately(messageWriter);
            Utils.NotifyRoles();
        }
        public static void NoCheckStartMeeting(this PlayerControl reporter, GameData.PlayerInfo target)
        { /*サボタージュ中でも関係なしに会議を起こせるメソッド
            targetがnullの場合はボタンとなる*/
            MeetingRoomManager.Instance.AssignSelf(reporter, target);
            DestroyableSingleton<HudManager>.Instance.OpenMeetingRoom(reporter);
            reporter.RpcStartMeeting(target);
        }
        public static bool IsModClient(this PlayerControl player) => Main.playerVersion.ContainsKey(player.PlayerId);
        ///<summary>
        ///プレイヤーのRoleBehaviourのGetPlayersInAbilityRangeSortedを実行し、戻り値を返します。
        ///</summary>
        ///<param name="ignoreColliders">trueにすると、壁の向こう側のプレイヤーが含まれるようになります。守護天使用</param>
        ///<returns>GetPlayersInAbilityRangeSortedの戻り値</returns>
        public static List<PlayerControl> GetPlayersInAbilityRangeSorted(this PlayerControl player, bool ignoreColliders = false) => GetPlayersInAbilityRangeSorted(player, pc => true, ignoreColliders);
        ///<summary>
        ///プレイヤーのRoleBehaviourのGetPlayersInAbilityRangeSortedを実行し、predicateの条件に合わないものを除外して返します。
        ///</summary>
        ///<param name="predicate">リストに入れるプレイヤーの条件 このpredicateに入れてfalseを返すプレイヤーは除外されます。</param>
        ///<param name="ignoreColliders">trueにすると、壁の向こう側のプレイヤーが含まれるようになります。守護天使用</param>
        ///<returns>GetPlayersInAbilityRangeSortedの戻り値から条件に合わないプレイヤーを除外したもの。</returns>
        public static List<PlayerControl> GetPlayersInAbilityRangeSorted(this PlayerControl player, Predicate<PlayerControl> predicate, bool ignoreColliders = false)
        {
            var rangePlayersIL = RoleBehaviour.GetTempPlayerList();
            List<PlayerControl> rangePlayers = new();
            player.Data.Role.GetPlayersInAbilityRangeSorted(rangePlayersIL, ignoreColliders);
            foreach (var pc in rangePlayersIL)
            {
                if (predicate(pc)) rangePlayers.Add(pc);
            }
            return rangePlayers;
        }
        public static bool IsNeutralKiller(this PlayerControl player)
        {
            return
                player.GetCustomRole() is
                CustomRoles.Egoist or
                CustomRoles.Jackal;
        }
        public static bool KnowDeathReason(this PlayerControl seer, PlayerControl target)
            => (seer.Is(CustomRoles.Doctor)
            || (seer.Is(RoleType.Madmate) && Options.MadmateCanSeeDeathReason.GetBool())
            || (seer.Data.IsDead && Options.GhostCanSeeDeathReason.GetBool()))
            && target.Data.IsDead;
        public static string GetRoleInfo(this PlayerControl player, bool InfoLong = false)
        {
            var role = player.GetCustomRole();
            if (role.IsVanilla())
            {
                var blurb = role switch
                {
                    CustomRoles.Impostor => StringNames.ImpostorBlurb,
                    CustomRoles.Scientist => InfoLong ? StringNames.ScientistBlurbLong : StringNames.ScientistBlurb,
                    CustomRoles.Engineer => InfoLong ? StringNames.EngineerBlurbLong : StringNames.EngineerBlurb,
                    CustomRoles.GuardianAngel => InfoLong ? StringNames.GuardianAngelBlurbLong : StringNames.GuardianAngelBlurb,
                    CustomRoles.Shapeshifter => InfoLong ? StringNames.ShapeshifterBlurbLong : StringNames.ShapeshifterBlurb,
                    _ => StringNames.CrewmateBlurb,
                };
                return (InfoLong ? "\n" : "") + DestroyableSingleton<TranslationController>.Instance.GetString(blurb);
            }

            var text = role.ToString();

            var Prefix = "";
            if (!InfoLong)
                switch (role)
                {
                    case CustomRoles.Mafia:
                        Prefix = Utils.CanMafiaKill() ? "After" : "Before";
                        break;
                    case CustomRoles.EvilWatcher:
                    case CustomRoles.NiceWatcher:
                        text = CustomRoles.Watcher.ToString();
                        break;
                    case CustomRoles.MadSnitch:
                    case CustomRoles.MadGuardian:
                        text = CustomRoles.Madmate.ToString();
                        Prefix = player.GetPlayerTaskState().IsTaskFinished ? "" : "Before";
                        break;
                };
            return GetString($"{Prefix}{text}Info" + (InfoLong ? "Long" : ""));
        }
        public static void SetRealKiller(this PlayerControl target, PlayerControl killer, bool NotOverRide = false)
        {
            if (target == null)
            {
                Logger.Info("target=null", "SetRealKiller");
                return;
            }
            var State = Main.PlayerStates[target.PlayerId];
            if (State.deathReason == PlayerState.DeathReason.Sniped) //スナイパー対策
                killer = Utils.GetPlayerById(Sniper.GetSniper(target.PlayerId));
            if (State.RealKiller.Item1 != DateTime.MinValue && NotOverRide) return; //既に値がある場合上書きしない
            byte killerId = killer == null ? byte.MaxValue : killer.PlayerId;
            RPC.SetRealKiller(target.PlayerId, killerId);
        }
        public static PlayerControl GetRealKiller(this PlayerControl target)
        {
            var killerId = Main.PlayerStates[target.PlayerId].GetRealKiller();
            return killerId == byte.MaxValue ? null : Utils.GetPlayerById(killerId);
        }

        //汎用
        public static bool Is(this PlayerControl target, CustomRoles role) =>
            role > CustomRoles.NotAssigned ? target.GetCustomSubRoles().Contains(role) : target.GetCustomRole() == role;
        public static bool Is(this PlayerControl target, RoleType type) { return target.GetCustomRole().GetRoleType() == type; }
        public static bool IsAlive(this PlayerControl target) { return target != null && !Main.PlayerStates[target.PlayerId].IsDead; }

    }
}