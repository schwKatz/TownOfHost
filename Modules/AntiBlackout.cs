using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using Hazel;

using AmongUs.GameOptions;
using TownOfHostY.Attributes;
using TownOfHostY.Modules;
using TownOfHostY.Roles.Impostor;
using TownOfHostY.Roles.Neutral;
using System.ComponentModel;
using TownOfHostY.Roles.Core;

namespace TownOfHostY
{
    public static class AntiBlackout
    {
        ///<summary>
        ///追放処理を上書きするかどうか
        ///</summary>
        public static bool OverrideExiledPlayer => Options.NoGameEnd.GetBool() || Jackal.RoleInfo.IsEnable|| StrayWolf.RoleInfo.IsEnable || Options.IsCCMode;

        public static bool IsCached { get; private set; } = false;
        private static Dictionary<byte, (bool isDead, bool Disconnected)> isDeadCache = new();
        private readonly static LogHandler logger = Logger.Handler("AntiBlackout");

        private static CountTypes recognizeImpostor = CountTypes.Impostor;
        public static int ExiledPlayerId = -1;

        public static void SetIsDead(bool doSend = true, [CallerMemberName] string callerMethodName = "")
        {
            SetRoleChange();

            logger.Info($"SetIsDead is called from {callerMethodName}");
            if (IsCached)
            {
                logger.Info("再度SetIsDeadを実行する前に、RestoreIsDeadを実行してください。");
                return;
            }
            isDeadCache.Clear();
            foreach (var info in GameData.Instance.AllPlayers)
            {
                if (info == null) continue;
                isDeadCache[info.PlayerId] = (info.IsDead, info.Disconnected);
                info.IsDead = false;
                info.Disconnected = false;
            }
            IsCached = true;
            if (doSend) SendGameData();
        }
        private static void SetRoleChange()
        {
            if (CustomWinnerHolder.WinnerTeam != CustomWinner.Default) return;

            CountTypes countType = CountTypes.None;
            List<PlayerControl> list = new();
            foreach (var type in new List<CountTypes>{ CountTypes.Impostor, CountTypes.Jackal, CountTypes.Pirate })
            {
                list = Main.AllAlivePlayerControls.Where(x => x.GetCustomRole().GetRoleInfo().CountType == type && x.PlayerId != ExiledPlayerId).ToList();
                Logger.Info($"SetRoleChange type: {type}, count: {list.Count}, exiled: {ExiledPlayerId}", "AntiBlackout");
                if (list.Count > 0) break;
            }

            if (countType <= recognizeImpostor) return;
            recognizeImpostor = countType;

            Logger.Info($"SetRoleChange count:{list.Count}", "AntiBlackout");
            foreach (var pc in Main.AllPlayerControls.Where(x => !x.Data.Disconnected))
            {
                if (pc.PlayerId == PlayerControl.LocalPlayer.PlayerId) continue;
                if (pc.IsAlive() && pc.GetCustomRole().GetRoleInfo().IsDesyncImpostor) continue;
                foreach (var desync in list)
                {
                    desync.RpcSetRoleDesync(RoleTypes.Impostor, pc.GetClientId());
                }
            }
            ExiledPlayerId = -1;
        }
        public static void RestoreIsDead(bool doSend = true, [CallerMemberName] string callerMethodName = "")
        {
            logger.Info($"RestoreIsDead is called from {callerMethodName}");
            foreach (var info in GameData.Instance.AllPlayers)
            {
                if (info == null) continue;
                if (isDeadCache.TryGetValue(info.PlayerId, out var val))
                {
                    info.IsDead = val.isDead;
                    info.Disconnected = val.Disconnected;
                }
            }
            isDeadCache.Clear();
            IsCached = false;
            if (doSend) SendGameData();
        }

        public static void SendGameData([CallerMemberName] string callerMethodName = "")
        {
            logger.Info($"SendGameData is called from {callerMethodName}");
            foreach (var innerNetObject in GameData.Instance.AllPlayers)
            {
                innerNetObject.SetDirtyBit(uint.MaxValue);
            }
        }
        public static void OnDisconnect(NetworkedPlayerInfo player)
        {
            // 実行条件: クライアントがホストである, IsDeadが上書きされている, playerが切断済み
            if (!AmongUsClient.Instance.AmHost || !IsCached || !player.Disconnected) return;
            isDeadCache[player.PlayerId] = (true, true);
            player.IsDead = player.Disconnected = false;
            SendGameData();
        }

        ///<summary>
        ///一時的にIsDeadを本来のものに戻した状態でコードを実行します
        ///<param name="action">実行内容</param>
        ///</summary>
        public static void TempRestore(Action action)
        {
            logger.Info("==Temp Restore==");
            //IsDeadが上書きされた状態でTempRestoreが実行されたかどうか
            bool before_IsCached = IsCached;
            try
            {
                if (before_IsCached) RestoreIsDead(doSend: false);
                action();
            }
            catch (Exception ex)
            {
                logger.Warn("AntiBlackout.TempRestore内で例外が発生しました");
                logger.Exception(ex);
            }
            finally
            {
                if (before_IsCached) SetIsDead(doSend: false);
                logger.Info("==/Temp Restore==");
            }
        }

        [GameModuleInitializer]
        public static void Reset()
        {
            logger.Info("==Reset==");
            if (isDeadCache == null) isDeadCache = new();
            isDeadCache.Clear();
            IsCached = false;
            recognizeImpostor = CountTypes.Impostor;
        }
    }
}