using System;
using System.Collections.Generic;
using System.Linq;
using AmongUs.GameOptions;
using HarmonyLib;
using Hazel;

using TownOfHostY.Attributes;
using TownOfHostY.Modules;
using TownOfHostY.Roles;
using TownOfHostY.Roles.Core;
using TownOfHostY.Roles.Neutral;
using static TownOfHostY.Translator;

namespace TownOfHostY;

[HarmonyPatch(typeof(AmongUsClient), nameof(AmongUsClient.CoStartGame))]
class ChangeRoleSettings
{
    public static void Postfix(AmongUsClient __instance)
    {
        //注:この時点では役職は設定されていません。
        Main.NormalOptions.roleOptions.SetRoleRate(RoleTypes.Shapeshifter, 0, 0);
        Main.NormalOptions.roleOptions.SetRoleRate(RoleTypes.Phantom, 0, 0);
        Main.NormalOptions.roleOptions.SetRoleRate(RoleTypes.Engineer, 0, 0);
        Main.NormalOptions.roleOptions.SetRoleRate(RoleTypes.Scientist, 0, 0);
        Main.NormalOptions.roleOptions.SetRoleRate(RoleTypes.Tracker, 0, 0);
        Main.NormalOptions.roleOptions.SetRoleRate(RoleTypes.Noisemaker, 0, 0);
        Main.NormalOptions.roleOptions.SetRoleRate(RoleTypes.GuardianAngel, 0, 0);

        if (Options.IsCCMode) Main.NormalOptions.NumImpostors = 1;
        //if (Options.IsONMode) Main.NormalOptions.NumEmergencyMeetings = 0;

        Main.AllPlayerKillCooldown = new Dictionary<byte, float>();
        Main.AllPlayerSpeed = new Dictionary<byte, float>();

        Main.SKMadmateNowCount = 0;

        Main.AfterMeetingDeathPlayers = new();
        Main.clientIdList = new();

        Main.CheckShapeshift = new();
        Main.ShapeshiftTarget = new();

        Main.ShowRoleInfoAtMeeting = new();

        ReportDeadBodyPatch.CannotReportList = new();
        ReportDeadBodyPatch.CannotReportByDeadBodyList = new();
        ReportDeadBodyPatch.DontReportMarkList = new();
        MeetingHudPatch.RevengeTargetPlayer = new();
        Options.UsedButtonCount = 0;
        Main.RealOptionsData = new OptionBackupData(GameOptionsManager.Instance.CurrentGameOptions);

        Main.introDestroyed = false;

        RandomSpawn.CustomNetworkTransformPatch.FirstTP = new();

        Main.DefaultCrewmateVision = Main.RealOptionsData.GetFloat(FloatOptionNames.CrewLightMod);
        Main.DefaultImpostorVision = Main.RealOptionsData.GetFloat(FloatOptionNames.ImpostorLightMod);

        Main.LastNotifyNames = new();

        Main.PlayerColors = new();
        //名前の記録
        Main.AllPlayerNames = new();

        Main.tempImpostorNum = 0;

        var invalidColor = Main.AllPlayerControls.Where(p => p.Data.DefaultOutfit.ColorId < 0 || Palette.PlayerColors.Length <= p.Data.DefaultOutfit.ColorId);
        if (invalidColor.Any())
        {
            var msg = Translator.GetString("Error.InvalidColor");
            Logger.SendInGame(msg);
            msg += "\n" + string.Join(",", invalidColor.Select(p => $"{p.name}({p.Data.DefaultOutfit.ColorId})"));
            Utils.SendMessage(msg);
            Logger.Error(msg, "CoStartGame");
        }

        GameModuleInitializerAttribute.InitializeAll();

        foreach (var target in Main.AllPlayerControls)
        {
            foreach (var seer in Main.AllPlayerControls)
            {
                var pair = (target.PlayerId, seer.PlayerId);
                Main.LastNotifyNames[pair] = target.name;
            }
        }
        foreach (var pc in Main.AllPlayerControls)
        {
            var colorId = pc.Data.DefaultOutfit.ColorId;
            if (AmongUsClient.Instance.AmHost)
            {
                if (Options.GetNameChangeModes() == NameChange.Color)
                {
                    if (pc.Is(CustomRoles.Rainbow)) pc.RpcSetName(GetString("RainbowColor"));
                    else pc.RpcSetName(Palette.GetColorName(colorId));
                }
            }
            PlayerState.Create(pc.PlayerId);
            Main.AllPlayerNames[pc.PlayerId] = pc?.Data?.PlayerName;
            Main.PlayerColors[pc.PlayerId] = Palette.PlayerColors[colorId];
            Main.AllPlayerSpeed[pc.PlayerId] = Main.RealOptionsData.GetFloat(FloatOptionNames.PlayerSpeedMod); //移動速度をデフォルトの移動速度に変更
            ReportDeadBodyPatch.WaitReport[pc.PlayerId] = new();
            pc.cosmetics.nameText.text = pc.name;

            RandomSpawn.CustomNetworkTransformPatch.FirstTP.Add(pc.PlayerId, true);
            var outfit = pc.Data.DefaultOutfit;
            Camouflage.PlayerSkins[pc.PlayerId] = new NetworkedPlayerInfo.PlayerOutfit().Set(outfit.PlayerName, outfit.ColorId, outfit.HatId, outfit.SkinId, outfit.VisorId, outfit.PetId);
            Main.clientIdList.Add(pc.GetClientId());

            // 初手会議での役職説明表示
            if (Options.ShowRoleInfoAtFirstMeeting.GetBool())
            {
                Main.ShowRoleInfoAtMeeting.Add(pc.PlayerId);
            }
        }
        Main.VisibleTasksCount = true;
        if (__instance.AmHost)
        {
            RPC.SyncCustomSettingsRPC();
            if (Options.CurrentGameMode == CustomGameMode.HideAndSeek)
            {
                Options.HideAndSeekKillDelayTimer = Options.KillDelay.GetFloat();
            }
            if (Options.IsStandardHAS)
            {
                Options.HideAndSeekKillDelayTimer = Options.StandardHASWaitingTime.GetFloat();
            }
        }

        IRandom.SetInstanceById(Options.RoleAssigningAlgorithm.GetValue());

        MeetingStates.FirstMeeting = true;
        GameStates.AlreadyDied = false;
    }
}
[HarmonyPatch(typeof(RoleManager), nameof(RoleManager.SelectRoles))]
class SelectRolesPatch
{
    private static bool AssignedStrayWolf = false;
    public static void Prefix()
    {
        if (!AmongUsClient.Instance.AmHost) return;
        //CustomRpcSenderとRpcSetRoleReplacerの初期化
        RpcSetRoleReplacer.StartReplace();

        RoleAssignManager.SelectAssignRoles();

        if (Options.IsCCMode)
        {
            if (CatchCat.Option.T_CanUseVent.GetBool())
            { // Engineer Setting
                int CatCount = Main.AllPlayerControls.Count() - 2 - CustomRoles.CCYellowLeader.GetCount();
                Main.NormalOptions.roleOptions.SetRoleRate(RoleTypes.Engineer, CatCount, 100);
            }
            List<PlayerControl> AllPlayers = new();
            foreach (var pc in Main.AllPlayerControls)
            {
                AllPlayers.Add(pc);
            }

            if (Options.EnableGM.GetBool())
            {
                AllPlayers.RemoveAll(x => x == PlayerControl.LocalPlayer);
                PlayerControl.LocalPlayer.RpcSetCustomRole(CustomRoles.GM);
                PlayerControl.LocalPlayer.RpcSetRole(RoleTypes.Crewmate);
                PlayerControl.LocalPlayer.Data.IsDead = true;
            }

            AssignDesyncRole(CustomRoles.CCYellowLeader, AllPlayers, BaseRole: RoleTypes.Impostor);
            AssignDesyncRole(CustomRoles.CCBlueLeader, AllPlayers, BaseRole: RoleTypes.Impostor);
        }
        //else if (Options.IsONMode())
        //{
        //    List<PlayerControl> AllPlayers = new();
        //    foreach (var pc in Main.AllPlayerControls)
        //    {
        //        AllPlayers.Add(pc);
        //        //ついでに初期化
        //        ONDeadTargetArrow.Add(pc.PlayerId);
        //    }

        //    if (Options.EnableGM.GetBool())
        //    {
        //        AllPlayers.RemoveAll(x => x == PlayerControl.LocalPlayer);
        //        PlayerControl.LocalPlayer.RpcSetCustomRole(CustomRoles.GM);
        //        PlayerControl.LocalPlayer.RpcSetRole(RoleTypes.Crewmate);
        //        PlayerControl.LocalPlayer.Data.IsDead = true;
        //    }

        //    Dictionary<(byte, byte), RoleTypes> rolesMap = new();

        //    AssignDesyncRole(CustomRoles.ONDiviner, AllPlayers, senders, rolesMap, BaseRole: RoleTypes.Impostor);
        //    AssignDesyncRole(CustomRoles.ONPhantomThief, AllPlayers, senders, rolesMap, BaseRole: RoleTypes.Impostor);

        //    MakeDesyncSender(senders, rolesMap);
        //}
        else if (Options.CurrentGameMode != CustomGameMode.HideAndSeek)
        {
            RoleTypes[] RoleTypesList = { RoleTypes.Scientist, RoleTypes.Engineer, RoleTypes.Tracker, RoleTypes.Noisemaker, RoleTypes.Shapeshifter, RoleTypes.Phantom };
            foreach (var roleTypes in RoleTypesList)
            {
                var roleOpt = Main.NormalOptions.roleOptions;
                int numRoleTypes = GetRoleTypesCount(roleTypes);
                roleOpt.SetRoleRate(roleTypes, numRoleTypes, numRoleTypes > 0 ? 100 : 0);
            }

            List<PlayerControl> AllPlayers = new();
            foreach (var pc in Main.AllPlayerControls)
            {
                AllPlayers.Add(pc);
            }

            if (Options.EnableGM.GetBool())
            {
                AllPlayers.RemoveAll(x => x == PlayerControl.LocalPlayer);
                PlayerControl.LocalPlayer.RpcSetRole(RoleTypes.Crewmate);
                PlayerState.GetByPlayerId(PlayerControl.LocalPlayer.PlayerId).SetMainRole(CustomRoles.GM);
                PlayerControl.LocalPlayer.Data.IsDead = true;
            }
            foreach (var (role, info) in CustomRoleManager.AllRolesInfo)
            {
                if (info.IsDesyncImpostor)
                {
                    switch (role)
                    {
                        case CustomRoles.StrayWolf:
                            AssignedStrayWolf = AssignDesyncRole(CustomRoles.StrayWolf, AllPlayers, BaseRole: RoleTypes.Impostor, IsImpostorRole: true);
                            continue;
                        case CustomRoles.Opportunist:
                            if (!Opportunist.OptionCanKill.GetBool()) continue;
                            break;
                    }

                    AssignDesyncRole(role, AllPlayers, BaseRole: info.BaseRoleType.Invoke());
                }
            }
        }
        //以下、バニラ側の役職割り当てが入る
    }
    public static void Postfix()
    {
        if (!AmongUsClient.Instance.AmHost) return;
        RpcSetRoleReplacer.Release(); //保存していたSetRoleRpcを一気に書く

        //Utils.ApplySuffix();

        var rand = IRandom.Instance;

        List<PlayerControl> Crewmates = new();
        List<PlayerControl> Impostors = new();
        List<PlayerControl> Scientists = new();
        List<PlayerControl> Engineers = new();
        List<PlayerControl> Trackers = new();
        List<PlayerControl> Noisemakers = new();
        List<PlayerControl> GuardianAngels = new();
        List<PlayerControl> Shapeshifters = new();
        List<PlayerControl> Phantoms = new();

        List<PlayerControl> allPlayersbySub = new();

        foreach (var pc in Main.AllPlayerControls)
        {
            pc.Data.IsDead = false; //プレイヤーの死を解除する

            if (!pc.Is(CustomRoles.GM)) allPlayersbySub.Add(pc);

            var state = PlayerState.GetByPlayerId(pc.PlayerId);
            if (state.MainRole != CustomRoles.NotAssigned) continue; //既にカスタム役職が割り当てられていればスキップ
            var role = CustomRoles.NotAssigned;
            switch (pc.Data.Role.Role)
            {
                case RoleTypes.Crewmate:
                    Crewmates.Add(pc);
                    role = CustomRoles.Crewmate;
                    break;
                case RoleTypes.Impostor:
                    Impostors.Add(pc);
                    role = CustomRoles.Impostor;
                    break;
                case RoleTypes.Scientist:
                    Scientists.Add(pc);
                    role = CustomRoles.Scientist;
                    break;
                case RoleTypes.Engineer:
                    Engineers.Add(pc);
                    role = CustomRoles.Engineer;
                    break;
                case RoleTypes.Tracker:
                    Trackers.Add(pc);
                    role = CustomRoles.Tracker;
                    break;
                case RoleTypes.Noisemaker:
                    Noisemakers.Add(pc);
                    role = CustomRoles.Noisemaker;
                    break;
                case RoleTypes.GuardianAngel:
                    GuardianAngels.Add(pc);
                    role = CustomRoles.GuardianAngel;
                    break;
                case RoleTypes.Shapeshifter:
                    Shapeshifters.Add(pc);
                    role = CustomRoles.Shapeshifter;
                    break;
                case RoleTypes.Phantom:
                    Phantoms.Add(pc);
                    role = CustomRoles.Phantom;
                    break;
                default:
                    Logger.SendInGame(string.Format(GetString("Error.InvalidRoleAssignment"), pc?.Data?.PlayerName));
                    break;
            }
            state.SetMainRole(role);
        }

        if (Options.CurrentGameMode == CustomGameMode.HideAndSeek)
        {
            SetColorPatch.IsAntiGlitchDisabled = true;
            foreach (var pc in Main.AllPlayerControls)
            {
                if (pc.Is(CustomRoleTypes.Impostor))
                    pc.RpcSetColor(0);
                else if (pc.Is(CustomRoleTypes.Crewmate))
                    pc.RpcSetColor(1);
            }

            //役職設定処理
            AssignCustomRolesFromList(CustomRoles.HASFox, Crewmates);
            AssignCustomRolesFromList(CustomRoles.HASTroll, Crewmates);
            foreach (var pair in PlayerState.AllPlayerStates)
            {
                //RPCによる同期
                ExtendedPlayerControl.RpcSetCustomRole(pair.Key, pair.Value.MainRole);
            }
            //色設定処理
            SetColorPatch.IsAntiGlitchDisabled = true;

            GameEndChecker.SetPredicateToHideAndSeek();
        }
        else if (Options.IsCCMode)
        {
            //役職設定処理
            //Impostorsを割り当て
            {
                SetColorPatch.IsAntiGlitchDisabled = true;
                foreach (var imp in Impostors)
                {
                    PlayerState.GetByPlayerId(imp.PlayerId).SetMainRole(CustomRoles.CCRedLeader);
                    Logger.Info("役職設定:" + imp?.Data?.PlayerName + " = " + CustomRoles.CCRedLeader.ToString(), "AssignRoles");
                }
            }
            //残りを割り当て
            {
                foreach (var crew in CatchCat.Option.T_CanUseVent.GetBool() ? Engineers : Crewmates)
                {
                    PlayerState.GetByPlayerId(crew.PlayerId).SetMainRole(CustomRoles.CCNoCat);
                    Logger.Info("役職設定:" + crew?.Data?.PlayerName + " = " + CustomRoles.CCNoCat.ToString(), "AssignRoles");
                }
                SetColorPatch.IsAntiGlitchDisabled = false;
            }

            foreach (var pair in PlayerState.AllPlayerStates)
            {
                //RPCによる同期
                ExtendedPlayerControl.RpcSetCustomRole(pair.Key, pair.Value.MainRole);
            }

            foreach (var pc in Main.AllPlayerControls)
            {
                HudManager.Instance.SetHudActive(true);
                Main.AllPlayerKillCooldown[pc.PlayerId] = Options.DefaultKillCooldown; //キルクールをデフォルトキルクールに変更

                CatchCat.Common.Add(pc);
            }
            GameEndChecker.SetPredicateToCatchCat();

            GameOptionsSender.AllSenders.Clear();
            foreach (var pc in Main.AllPlayerControls)
            {
                GameOptionsSender.AllSenders.Add(
                    new PlayerGameOptionsSender(pc)
                );
            }
        }
        //else if (Options.IsONMode)
        //{
        //    //役職設定処理
        //    AssignCustomRolesFromList(CustomRoles.ONBigWerewolf, Impostors);
        //    AssignCustomRolesFromList(CustomRoles.ONWerewolf, Impostors);
        //    AssignCustomRolesFromList(CustomRoles.ONMadman, Crewmates);
        //    AssignCustomRolesFromList(CustomRoles.ONMadFanatic, Crewmates);
        //    AssignCustomRolesFromList(CustomRoles.ONMayor, Crewmates);
        //    AssignCustomRolesFromList(CustomRoles.ONHunter, Crewmates);
        //    AssignCustomRolesFromList(CustomRoles.ONBakery, Crewmates);
        //    AssignCustomRolesFromList(CustomRoles.ONTrapper, Crewmates);
        //    AssignCustomRolesFromList(CustomRoles.ONHangedMan, Crewmates);
        //    AssignCustomRolesFromList(CustomRoles.ONVillager, Crewmates);

        //    //残りを割り当て
        //    {
        //        SetColorPatch.IsAntiGlitchDisabled = true;
        //        foreach (var imp in Impostors)
        //        {
        //            PlayerState.GetByPlayerId(imp.PlayerId).SetMainRole(CustomRoles.ONWerewolf);
        //            Logger.Info("役職設定:" + imp?.Data?.PlayerName + " = " + CustomRoles.ONWerewolf.ToString(), "AssignRoles");
        //        }
        //        foreach (var crew in Crewmates)
        //        {
        //            PlayerState.GetByPlayerId(crew.PlayerId).SetMainRole(CustomRoles.ONVillager);
        //            Logger.Info("役職設定:" + crew?.Data?.PlayerName + " = " + CustomRoles.ONVillager.ToString(), "AssignRoles");
        //        }
        //        SetColorPatch.IsAntiGlitchDisabled = false;
        //    }

        //    foreach (var pair in PlayerState.AllPlayerStates)
        //    {
        //        //RPCによる同期
        //        ExtendedPlayerControl.RpcSetCustomRole(pair.Key, pair.Value.MainRole);
        //    }

        //    foreach (var pc in Main.AllPlayerControls)
        //    {
        //        switch (pc.GetCustomRole())
        //        {
        //            case CustomRoles.ONWerewolf:
        //                ONWerewolf.Add(pc.PlayerId);
        //                break;
        //            case CustomRoles.ONBigWerewolf:
        //                ONBigWerewolf.Add(pc.PlayerId);
        //                break;
        //            case CustomRoles.ONDiviner:
        //                ONDiviner.Add(pc.PlayerId);
        //                break;
        //            case CustomRoles.ONPhantomThief:
        //                ONPhantomThief.Add(pc.PlayerId);
        //                break;
        //        }
        //        Main.DefaultRole[pc.PlayerId] = pc.GetCustomRole();
        //        Main.MeetingSeerDisplayRole[pc.PlayerId] = pc.GetCustomRole();
        //        Main.ChangeRolesTarget.Add(pc.PlayerId, null);
        //        RPC.SendRPCDefaultRole(pc.PlayerId);

        //        HudManager.Instance.SetHudActive(true);
        //        pc.ResetKillCooldown();
        //    }
        //    GameEndChecker.SetPredicateToOneNight();

        //    GameOptionsSender.AllSenders.Clear();
        //    foreach (var pc in Main.AllPlayerControls)
        //    {
        //        GameOptionsSender.AllSenders.Add(
        //            new PlayerGameOptionsSender(pc)
        //        );
        //    }

        //    // ResetCamが必要なプレイヤーのリストにクラス化が済んでいない役職のプレイヤーを追加
        //    Main.ResetCamPlayerList.AddRange(PlayerControl.AllPlayerControls.ToArray().Where(p =>
        //    p.Is(CustomRoles.ONDiviner) || p.Is(CustomRoles.ONPhantomThief)).Select(p => p.PlayerId));
        //}
        else
        {
            foreach (var role in CustomRolesHelper.AllStandardRoles)
            {
                if (role.IsVanilla()) continue;

                if (role == CustomRoles.Opportunist && Opportunist.OptionCanKill.GetBool()) continue;
                if (role == CustomRoles.StrayWolf && AssignedStrayWolf) continue;
                if (role is not CustomRoles.Opportunist and not CustomRoles.StrayWolf &&
                    CustomRoleManager.GetRoleInfo(role)?.IsDesyncImpostor == true) continue;

                var baseRoleTypes = role.GetRoleTypes() switch
                {
                    RoleTypes.Impostor => Impostors,
                    RoleTypes.Shapeshifter => Shapeshifters,
                    RoleTypes.Phantom => Phantoms,
                    RoleTypes.Scientist => Scientists,
                    RoleTypes.Engineer => Engineers,
                    RoleTypes.Tracker => Trackers,
                    RoleTypes.Noisemaker => Noisemakers,
                    RoleTypes.GuardianAngel => GuardianAngels,
                    _ => Crewmates,
                };
                AssignCustomRolesFromList(role, baseRoleTypes);
            }

            // Random-Addon
            if (!CustomRoles.PlatonicLover.IsEnable() && CustomRoles.Lovers.IsEnable())
                AssignCustomSubRolesFromList(CustomRoles.Lovers, allPlayersbySub, 2);
            AssignCustomSubRolesFromList(CustomRoles.AddWatch, allPlayersbySub);
            AssignCustomSubRolesFromList(CustomRoles.Sunglasses, allPlayersbySub);
            AssignCustomSubRolesFromList(CustomRoles.AddLight, allPlayersbySub);
            AssignCustomSubRolesFromList(CustomRoles.AddSeer, allPlayersbySub);
            AssignCustomSubRolesFromList(CustomRoles.Autopsy, allPlayersbySub);
            AssignCustomSubRolesFromList(CustomRoles.VIP, allPlayersbySub);
            AssignCustomSubRolesFromList(CustomRoles.Clumsy, allPlayersbySub);
            AssignCustomSubRolesFromList(CustomRoles.Revenger, allPlayersbySub);
            AssignCustomSubRolesFromList(CustomRoles.Management, allPlayersbySub);
            AssignCustomSubRolesFromList(CustomRoles.InfoPoor, allPlayersbySub);
            AssignCustomSubRolesFromList(CustomRoles.Sending, allPlayersbySub);
            AssignCustomSubRolesFromList(CustomRoles.TieBreaker, allPlayersbySub);
            AssignCustomSubRolesFromList(CustomRoles.NonReport, allPlayersbySub);
            AssignCustomSubRolesFromList(CustomRoles.PlusVote, allPlayersbySub);
            AssignCustomSubRolesFromList(CustomRoles.Guarding, allPlayersbySub);
            AssignCustomSubRolesFromList(CustomRoles.AddBait, allPlayersbySub);
            AssignCustomSubRolesFromList(CustomRoles.Refusing, allPlayersbySub);

            foreach (var pair in PlayerState.AllPlayerStates)
            {
                ExtendedPlayerControl.RpcSetCustomRole(pair.Key, pair.Value.MainRole);

                foreach (var subRole in pair.Value.SubRoles)
                    ExtendedPlayerControl.RpcSetCustomRole(pair.Key, subRole);
            }

            CustomRoleManager.CreateInstance();
            foreach (var pc in Main.AllPlayerControls)
            {
                HudManager.Instance.SetHudActive(true);
                pc.ResetKillCooldown();

                // DirectAssign-Addon
                if (pc.GetCustomRole().IsAddAddOn()
                    && (Options.AddOnBuffAssign[pc.GetCustomRole()].GetBool() || Options.AddOnDebuffAssign[pc.GetCustomRole()].GetBool()))
                {
                    foreach (var Addon in CustomRolesHelper.AllAddOnRoles)
                    {
                        if (Options.AddOnRoleOptions.TryGetValue((pc.GetCustomRole(), Addon), out var option) && option.GetBool())
                        {
                            pc.RpcSetCustomRole(Addon);
                        }
                    }
                }

                //通常モードでかくれんぼをする人用
                if (Options.IsStandardHAS)
                {
                    foreach (var seer in Main.AllPlayerControls)
                    {
                        if (seer == pc) continue;
                        if (pc.GetCustomRole().IsImpostor() || pc.IsNeutralKiller()) //変更対象がインポスター陣営orキル可能な第三陣営
                            NameColorManager.Add(seer.PlayerId, pc.PlayerId);
                    }
                }
                foreach (var seer in Main.AllPlayerControls)
                {
                    if (seer == pc) continue;
                    if (pc.Is(CustomRoles.GM)
                        || (pc.Is(CustomRoles.Workaholic) && Workaholic.Seen)
                        || pc.Is(CustomRoles.Rainbow))
                        NameColorManager.Add(seer.PlayerId, pc.PlayerId, pc.GetRoleColorCode());
                }
            }

            RoleTypes[] RoleTypesList = { RoleTypes.Scientist, RoleTypes.Engineer, RoleTypes.Tracker, RoleTypes.Noisemaker, RoleTypes.Shapeshifter, RoleTypes.Phantom };
            foreach (var roleTypes in RoleTypesList)
            {
                var roleOpt = Main.NormalOptions.roleOptions;
                roleOpt.SetRoleRate(roleTypes, 0, 0);
            }
            GameEndChecker.SetPredicateToNormal();

            GameOptionsSender.AllSenders.Clear();
            foreach (var pc in Main.AllPlayerControls)
            {
                GameOptionsSender.AllSenders.Add(
                    new PlayerGameOptionsSender(pc)
                );
            }
        }
        Utils.CountAlivePlayers(true);
        Utils.SyncAllSettings();
        SetColorPatch.IsAntiGlitchDisabled = false;
    }
    public static void AssignRolesNormal(Dictionary<RoleTypes, int> roleTypesList)
    {
        var list = AmongUsClient.Instance.allClients.ToArray()
        .Where(c => c.Character != null && c.Character.Data != null &&
                    !c.Character.Data.Disconnected && !c.Character.Data.IsDead)
        .OrderBy(c => c.Id).Select(c => c.Character.Data).ToList();
        int adjustedNumImpostors = Main.NormalOptions.GetInt(Int32OptionNames.NumImpostors);
        Logger.Info($"NomalAssign list: {list.Count}, impostor: {adjustedNumImpostors}", "AssignRoles");
        AssignRolesForTeam(list, roleTypesList, RoleTeamTypes.Impostor, adjustedNumImpostors, RoleTypes.Impostor);
        AssignRolesForTeam(list, roleTypesList, RoleTeamTypes.Crewmate, int.MaxValue, RoleTypes.Crewmate);
    }
    private static void AssignRolesForTeam(List<NetworkedPlayerInfo> players, Dictionary<RoleTypes, int> roleTypesList, RoleTeamTypes team, int teamMax, RoleTypes defaultRole)
    {
        int num = 0;
        List<RoleTypes> list = new();

        if (roleTypesList != null)
        {
            IEnumerable<RoleBehaviour> source = from role in DestroyableSingleton<RoleManager>.Instance.AllRoles
                                                where role.TeamType == team && !RoleManager.IsGhostRole(role.Role)
                                                select role;
            foreach (var roleBehaviour in source)
            {
                if (!roleTypesList.TryGetValue(roleBehaviour.Role, out int count)) continue;
                Logger.Info($"NomalAssign team: {team}, role: {roleBehaviour.Role}, count: {count}", "AssignRolesForTeam");
                for (int i = 0; i < count; i++)
                {
                    list.Add(roleBehaviour.Role);
                }
            }
            AssignRolesFromList(players, teamMax, list, ref num);
        }

        while (list.Count < players.Count && list.Count + num < teamMax)
        {
            list.Add(defaultRole);
        }
        Logger.Info($"DefaultAssign team: {team}, role: {defaultRole}, count: {list.Count}", "AssignRolesForTeam");
        AssignRolesFromList(players, teamMax, list, ref num);
    }
    private static void AssignRolesFromList(List<NetworkedPlayerInfo> players, int teamMax, List<RoleTypes> roleList, ref int rolesAssigned)
    {
        while (roleList.Count > 0 && players.Count > 0 && rolesAssigned < teamMax)
        {
            int index = HashRandom.FastNext(roleList.Count);
            RoleTypes roleType = roleList[index];
            roleList.RemoveAt(index);
            int index2 = HashRandom.FastNext(players.Count);
            players[index2].Object.RpcSetRole(roleType, false);
            players.RemoveAt(index2);
            rolesAssigned++;
        }
    }
    private static bool AssignDesyncRole(CustomRoles role, List<PlayerControl> AllPlayers, RoleTypes BaseRole, RoleTypes hostBaseRole = RoleTypes.Crewmate, bool IsImpostorRole = false)
    {
        if (!role.IsPresent()) return false;

        var hostId = PlayerControl.LocalPlayer.PlayerId;
        var rand = IRandom.Instance;
        var realAssigned = 0;
        var rolesMap = RpcSetRoleReplacer.RolesMap;

        if (IsImpostorRole)
        {
            var impostorNum = Main.NormalOptions.GetInt(Int32OptionNames.NumImpostors);
            if (impostorNum == role.GetRealCount()) return false;
            if (Main.tempImpostorNum == 0)
                Main.tempImpostorNum = impostorNum;
        }

        for (var i = 0; i < role.GetRealCount(); i++)
        {
            if (AllPlayers.Count <= 0) break;
            var player = AllPlayers[rand.Next(0, AllPlayers.Count)];
            AllPlayers.Remove(player);
            PlayerState.GetByPlayerId(player.PlayerId).SetMainRole(role);

            var selfRole = player.PlayerId == hostId ? hostBaseRole : BaseRole;
            var othersRole = player.PlayerId == hostId ? RoleTypes.Crewmate : RoleTypes.Scientist;

            //Desync役職視点
            foreach (var target in Main.AllPlayerControls)
            {
                if (player.PlayerId != target.PlayerId)
                {
                    rolesMap[(player.PlayerId, target.PlayerId)] = othersRole;
                }
                else
                {
                    rolesMap[(player.PlayerId, target.PlayerId)] = selfRole;
                }
            }

            //他者視点
            foreach (var seer in Main.AllPlayerControls)
            {
                if (player.PlayerId != seer.PlayerId)
                {
                    rolesMap[(seer.PlayerId, player.PlayerId)] = othersRole;
                }
            }
            RpcSetRoleReplacer.OverriddenSenderList.Add(player.PlayerId);
            //ホスト視点はロール決定
            player.StartCoroutine(player.CoSetRole(othersRole, false));
            player.Data.IsDead = true;
            realAssigned++;

            Logger.Info("役職設定(desync):" + player?.Data?.PlayerName + " = " + role.ToString(), "AssignRoles");
        }

        if (IsImpostorRole) Main.NormalOptions.NumImpostors -= realAssigned;

        return realAssigned > 0;
    }
    private static List<PlayerControl> AssignCustomRolesFromList(CustomRoles role, List<PlayerControl> players, int RawCount = -1)
    {
        if (players == null || players.Count <= 0) return null;
        var rand = IRandom.Instance;
        var count = Math.Clamp(RawCount, 0, players.Count);
        if (RawCount == -1) count = Math.Clamp(role.GetRealCount(), 0, players.Count);
        if (count <= 0) return null;
        List<PlayerControl> AssignedPlayers = new();
        SetColorPatch.IsAntiGlitchDisabled = true;
        for (var i = 0; i < count; i++)
        {
            var player = players[rand.Next(0, players.Count)];
            AssignedPlayers.Add(player);
            players.Remove(player);
            PlayerState.GetByPlayerId(player.PlayerId).SetMainRole(role);
            Logger.Info("役職設定:" + player?.Data?.PlayerName + " = " + role.ToString(), "AssignRoles");

            if (Options.CurrentGameMode == CustomGameMode.HideAndSeek)
            {
                if (player.Is(CustomRoles.HASTroll))
                    player.RpcSetColor(2);
                else if (player.Is(CustomRoles.HASFox))
                    player.RpcSetColor(3);
            }
        }
        SetColorPatch.IsAntiGlitchDisabled = false;
        return AssignedPlayers;
    }

    private static List<PlayerControl> AssignCustomSubRolesFromList(CustomRoles role, List<PlayerControl> allPlayersbySub, int RawCount = -1)
    {
        if (allPlayersbySub == null || allPlayersbySub.Count <= 0) return null;
        var rand = IRandom.Instance;
        var count = Math.Clamp(RawCount, 0, allPlayersbySub.Count);
        if (RawCount == -1) count = Math.Clamp(role.GetCount(), 0, allPlayersbySub.Count);
        if (count <= 0) return null;
        List<PlayerControl> AssignedPlayers = new();

        for (var i = 0; i < count; i++)
        {
            var player = allPlayersbySub[rand.Next(0, allPlayersbySub.Count)];
            AssignedPlayers.Add(player);
            allPlayersbySub.Remove(player);
            PlayerState.GetByPlayerId(player.PlayerId).SetSubRole(role);
            Logger.Info("属性設定:" + player?.Data?.PlayerName + " = " + player.GetCustomRole().ToString() + " + " + role.ToString(), "AssignSubRoles");
        }
        return AssignedPlayers;
    }

    public static int GetRoleTypesCount(RoleTypes roleTypes)
    {
        int count = 0;
        foreach (var role in CustomRolesHelper.AllStandardRoles)
        {
            if (role == CustomRoles.Opportunist && Opportunist.OptionCanKill.GetBool()) continue;
            if (role is not CustomRoles.Opportunist &&
                CustomRoleManager.GetRoleInfo(role)?.IsDesyncImpostor == true) continue;
            if (role == CustomRoles.Egoist && Main.NormalOptions.GetInt(Int32OptionNames.NumImpostors) <= 1) continue;
            if (role.GetRoleTypes() == roleTypes)
                count += role.GetRealCount();
        }
        return count;
    }
}
    [HarmonyPatch(typeof(PlayerControl), nameof(PlayerControl.RpcSetRole)), HarmonyPriority(Priority.High)]
    public class RpcSetRoleReplacer
    {
        private static bool doReplace = false;
        private static Dictionary<byte, CustomRpcSender> senders;
        public static List<(PlayerControl, RoleTypes)> StoragedData = new();
        // 役職DesyncなどRolesMapでSetRoleRpcを書き込みするリスト
        public static List<byte> OverriddenSenderList;
        public static Dictionary<(byte, byte), RoleTypes> RolesMap;
        public static bool DoReplace() => doReplace;

        public static bool Prefix(PlayerControl __instance, [HarmonyArgument(0)] RoleTypes roleType)
        {
            if (doReplace && senders != null)
            {
                StoragedData.Add((__instance, roleType));
                return false;
            }
            else return true;
        }
        public static void Release()
        {
            ReleaseNormalSetRole(true);
            ReleaseDesyncSetRole(true);
            senders.Do(kvp => kvp.Value.SendMessage());

            DummySetRole();

            // 不要なオブジェクトの削除
            EndReplace();
        }
        public static void DummySetRole()
        {
            foreach (var pc in Main.AllPlayerControls)
            {
                if (pc.PlayerId == PlayerControl.LocalPlayer.PlayerId) continue;
                DummySetRole(pc);
            }
        }
        public static void DummySetRole(PlayerControl target)
        {
            int targetClientId = target.GetClientId();
            if (!RolesMap.TryGetValue((target.PlayerId, target.PlayerId), out var roleType))
            {
                roleType = StoragedData.FirstOrDefault(x => x.Item1.PlayerId == target.PlayerId).Item2;
            }
            Logger.Info($"sent {target.name} {roleType}", "DummySetRole");

            var stream = MessageWriter.Get(SendOption.Reliable);
            stream.StartMessage(6);
            stream.Write(AmongUsClient.Instance.GameId);
            stream.WritePacked(targetClientId);
            {
                SetDisconnectedMessage(stream, true);

                stream.StartMessage(2);
                stream.WritePacked(target.NetId);
                stream.Write((byte)RpcCalls.SetRole);
                stream.Write((ushort)roleType);
                stream.Write(true);     //canOverrideRole
                stream.EndMessage();
                Logger.Info($"DummySetRole to:{target?.name}({targetClientId}) player:{target?.name}({roleType})", "★RpcSetRole");

                SetDisconnectedMessage(stream, false);
            }
            stream.EndMessage();
            AmongUsClient.Instance.SendOrDisconnect(stream);
            stream.Recycle();
        }
        private static void SetDisconnectedMessage(MessageWriter stream, bool disconnected)
        {
            foreach (var pc in Main.AllPlayerControls)
            {
                //if (pc.PlayerId != target.PlayerId) continue;
                pc.Data.Disconnected = disconnected;

                stream.StartMessage(1);
                stream.WritePacked(pc.Data.NetId);
                pc.Data.Serialize(stream, false);
                stream.EndMessage();
            }
        }
        private static void ReleaseDesyncSetRole(bool skipSelf)
        {
            foreach (var seer in Main.AllPlayerControls)
            {
                foreach (var target in Main.AllPlayerControls)
                {
                    if (skipSelf && seer.PlayerId == target.PlayerId &&
                        seer.PlayerId != PlayerControl.LocalPlayer.PlayerId) continue;
                    if (RolesMap.TryGetValue((seer.PlayerId, target.PlayerId), out var roleType))
                    {
                        if (roleType == RoleTypes.Scientist &&
                            StoragedData.Any(x => x.Item1.PlayerId == seer.PlayerId && x.Item2 == RoleTypes.Noisemaker))
                        {
                            Logger.Info($"ChangeNoisemaker seer: {seer.PlayerId}, target: {target.PlayerId},{roleType}=>{RoleTypes.Noisemaker}", "MakeDesyncSender");
                            roleType = RoleTypes.Noisemaker;
                        }

                        var sender = senders[seer.PlayerId];
                        sender.AutoStartRpc(seer.NetId, (byte)RpcCalls.SetRole, target.GetClientId())
                            .Write((ushort)roleType)
                            .Write(true) //canOverrideRole
                            .EndRpc();
                        Logger.Info($"ReleaseDesyncSetRole to:{target?.name}({target.GetClientId()}) player:{seer?.name}({roleType})", "RpcSetRole");
                    }
                }
            }
        }
        private static void ReleaseNormalSetRole(bool skipSelf)
        {
            foreach (var senderPair in senders)
            {
                var sender = senderPair.Value;
                var targetId = senderPair.Key;
                if (OverriddenSenderList.Contains(targetId)) continue;
                if (sender.CurrentState != CustomRpcSender.State.InRootMessage)
                    throw new InvalidOperationException("A CustomRpcSender had Invalid State.");

                foreach (var pair in StoragedData)
                {
                    if (skipSelf && targetId == pair.Item1.PlayerId &&
                        targetId != PlayerControl.LocalPlayer.PlayerId) continue;

                    var player = pair.Item1;
                    var roleType = pair.Item2;
                    var clientId = Utils.GetPlayerById(targetId).GetClientId();

                    player.StartCoroutine(player.CoSetRole(roleType, false));
                    sender.AutoStartRpc(player.NetId, (byte)RpcCalls.SetRole, clientId)
                        .Write((ushort)roleType)
                        .Write(true)           //canOverrideRole = false
                        .EndRpc();
                    Logger.Info($"ReleaseNormalSetRole toClientId:{clientId} player:{player?.name}({roleType})", "RpcSetRole");
                }
                sender.EndMessage();
            }
            doReplace = false;
        }
        public static void StartReplace()
        {
            senders = new();
            foreach (var pc in Main.AllPlayerControls)
            {
                senders[pc.PlayerId] = new CustomRpcSender($"{pc.name}'s SetRole Sender", SendOption.Reliable, false)
                        .StartMessage(pc.GetClientId());
            }
            StoragedData = new();
            OverriddenSenderList = new();
            RolesMap = new();
            doReplace = true;
        }
        public static void EndReplace()
        {
            senders = null;
            OverriddenSenderList = null;
            RolesMap = null;
            StoragedData = null;
        }
    }
