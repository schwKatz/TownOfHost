using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using TownOfHostY.Roles.Core;
using TownOfHostY.Attributes;
using TownOfHostY.Roles.Madmate;

namespace TownOfHostY;

public static class TargetDeadArrow
{
    private static HashSet<DeadBody> DeadBodyList = new();
    private static HashSet<byte> SeerList = new();
    private static Dictionary<ArrowInfo, string> TargetArrows = new();

    [GameModuleInitializer]
    public static void Init()
    {
        DeadBodyList.Clear();
        SeerList.Clear();
        TargetArrows.Clear();
    }

    private static bool IsEnable()
    {
        return CustomRoles.Detector.IsEnable()
            || MadConnecter.IsEnableDeadArrow();
    }
    private static bool IsEnableRole(CustomRoles role)
    {
        return role is CustomRoles.Detector
            or CustomRoles.MadConnecter;
    }

    //死体が生まれた時、発見される側の死体リストに追加
    public static void UpdateDeadBody()
    {
        if (!IsEnable()) return;

        DeadBody[] AllBody = UnityEngine.Object.FindObjectsOfType<DeadBody>();
        DeadBody targetBody = null;

        foreach (var body in AllBody)
        {
            if (!DeadBodyList.Contains(body))
            {
                DeadBodyList.Add(body);
                targetBody = body;
                Logger.Info($"DeadBodyList.Add({body.ParentId})", "TargetDeadArrow");
                break;
            }
        }

        if (SeerList.Count != 0)
        {
            foreach (var seerId in SeerList)
            {
                TargetArrowAdd(seerId, targetBody);
            }
        }
    }

    // Seerが死体への矢印を表示できるようになった時にAddする
    public static void AddSeer(byte playerId)
    {
        SeerList.Add(playerId);
        Logger.Info($"SeerList.Add({playerId})", "TargetDeadArrow");

        if (DeadBodyList.Count != 0)
        {
            foreach (var target in DeadBodyList)
            {
                TargetArrowAdd(playerId, target);
            }
        }
    }

    /// <summary>
    /// タスク終了プレイヤーから死体への矢印
    /// </summary>
    public static string GetDeadBodiesArrow(PlayerControl seer, PlayerControl target)
    {
        if (!IsEnableRole(seer.GetCustomRole()) || seer != target) return string.Empty;
        var arrows = string.Empty;
        foreach (var targetBody in DeadBodyList)
        {
            var arrow = TargetArrowGetArrows(seer, targetBody);
            arrows += arrow;
        }
        return arrows.Length == 0 ? string.Empty : Utils.ColorString(Palette.CrewmateBlue, arrows);
    }

    public static void OnStartMeeting()
    {
        DeadBodyList.Clear();
        if (SeerList.Count != 0)
        {
            foreach (var seerId in SeerList)
            {
                TargetArrowRemoveAllTarget(seerId);
                Logger.Info($"TargetArrowRemoveAllTarget({seerId});", "TargetDeadArrow");
            }
        }
    }

    class ArrowInfo
    {
        public byte From;
        public DeadBody To;
        public ArrowInfo(byte from, DeadBody to)
        {
            From = from;
            To = to;
        }
        public bool Equals(ArrowInfo obj)
        {
            return From == obj.From && To == obj.To;
        }
        public override string ToString()
        {
            return $"(From:{From} To:{To})";
        }
    }

    static readonly string[] Arrows = {
        "↑",
        "↗",
        "→",
        "↘",
        "↓",
        "↙",
        "←",
        "↖",
        "・"
    };

    /// <summary>
    /// 新たにターゲット矢印対象を登録
    /// </summary>
    /// <param name="seer"></param>
    /// <param name="target"></param>
    /// <param name="coloredArrow"></param>
    public static void TargetArrowAdd(byte seer, DeadBody target)
    {
        var arrowInfo = new ArrowInfo(seer, target);
        if (!TargetArrows.Any(a => a.Key.Equals(arrowInfo)))
            TargetArrows[arrowInfo] = "・";

        Logger.Info($"TargetArrowAdd({seer}, targetBody)", "TargetDeadArrow");
    }
    /// <summary>
    /// ターゲットの削除
    /// </summary>
    /// <param name="seer"></param>
    /// <param name="target"></param>
    public static void TargetArrowRemove(byte seer, DeadBody target)
    {
        var arrowInfo = new ArrowInfo(seer, target);
        var removeList = new List<ArrowInfo>(TargetArrows.Keys.Where(k => k.Equals(arrowInfo)));
        foreach (var a in removeList)
        {
            TargetArrows.Remove(a);
        }
    }
    /// <summary>
    /// タイプの同じターゲットの全削除
    /// </summary>
    /// <param name="seer"></param>
    public static void TargetArrowRemoveAllTarget(byte seer)
    {
        var removeList = new List<ArrowInfo>(TargetArrows.Keys.Where(k => k.From == seer));
        foreach (var arrowInfo in removeList)
        {
            TargetArrows.Remove(arrowInfo);
        }
    }
    /// <summary>
    /// ターゲット矢印を取得
    /// </summary>
    /// <param name="seer"></param>
    /// <returns></returns>
    public static string TargetArrowGetArrows(PlayerControl seer, DeadBody target)
    {
        var arrows = "";
        foreach (var arrowInfo in TargetArrows.Keys.Where(ai => ai.From == seer.PlayerId && ai.To == target))
        {
            arrows += TargetArrows[arrowInfo];
        }
        return arrows;
    }
    /// <summary>
    /// FixedUpdate毎にターゲット矢印を確認
    /// 更新があったらNotifyRolesを発行
    /// </summary>
    /// <param name="seer"></param>
    public static void OnFixedUpdate(PlayerControl seer)
    {
        if (!GameStates.IsInTask) return;

        var seerId = seer.PlayerId;
        var seerIsDead = !seer.IsAlive();

        var arrowList = new List<ArrowInfo>(TargetArrows.Keys.Where(a => a.From == seer.PlayerId));
        if (arrowList.Count == 0) return;

        var update = false;
        foreach (var arrowInfo in arrowList)
        {
            var target = arrowInfo.To;
            if (seerIsDead || target == null)
            {
                TargetArrows.Remove(arrowInfo);
                update = true;
                continue;
            }
            //対象の方角ベクトルを取る
            var dir = target.transform.position - seer.transform.position;
            int index;
            if (dir.magnitude < 2)
            {
                //近い時はドット表示
                index = 8;
            }
            else
            {
                //-22.5～22.5度を0とするindexに変換
                // 下が0度、左側が+180まで右側が-180まで
                // 180度足すことで上が0度の時計回り
                // 45度単位のindexにするため45/2を加算
                var angle = Vector3.SignedAngle(Vector3.down, dir, Vector3.back) + 180 + 22.5;
                index = ((int)(angle / 45)) % 8;
            }
            var arrow = Arrows[index];
            if (TargetArrows[arrowInfo] != arrow)
            {
                TargetArrows[arrowInfo] = arrow;
                update = true;
            }
        }
        if (update)
        {
            Utils.NotifyRoles(SpecifySeer: seer);
        }
    }
}