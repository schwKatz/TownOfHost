using System.Linq;
using TownOfHostY.Roles.Core;
using static TownOfHostY.CatchCat.Option;
using static TownOfHostY.CatchCat.Common;

namespace TownOfHostY.CatchCat;

static class LeaderPlayer
{
    // Leader Kill Cooldown
    public static float CalculateKillCooldown(PlayerControl pc)
    {
        float decrease = 0;
        if (T_OwnLeaderKillcoolDecrease.GetInt() != 0)
        {
            foreach (var cat in Main.AllPlayerControls.Where(p => p.GetCustomRole().IsCCColorCatRoles()))
            {
                if (IsSameCamp_LederCat(pc.GetCustomRole(), cat.GetCustomRole()))
                {
                    var ts = cat.GetPlayerTaskState();
                    int per = (int)(((float)ts.CompletedTasksCount / ts.AllTasksCount) * 100);
                    if (per >= T_OwnLeaderKillcoolDecrease.GetInt())
                    {
                        Logger.Info($"KillCooldown decrease[{pc.GetRealName()}]", "KillCooldown");
                        decrease += 1;
                    }
                }
            }
        }
        Logger.Info($"KillCooldown decrease : {decrease} ", "KillCooldown");
        return Options.DefaultKillCooldown - decrease;
    }

    // Leader Killed
    public static void OnCheckMurder(MurderInfo info)
    {
        (var killer, var target) = info.AttemptTuple;
        var targetRole = target.GetCustomRole();
        if (!targetRole.IsCCLeaderRoles()) return;

        bool isGuard = !LeaderKilled.GetBool();     //リーダーはキルされない設定

        if (LeaderKilled.GetBool())
        {
            if (LK_CatCount.GetBool())
            {
                int count = Main.AllPlayerControls.Where(p => IsSameCamp_LederCat(targetRole, p.GetCustomRole())).Count();

                if (count < LK_CatCount.GetInt()) isGuard = true;                       // 設定人数未満なのでガード
                else if (!LK_OneGuard.GetBool()) CanGuard[target.PlayerId] = false;     // ガード表示外す
            }
            if (!isGuard && LK_OneGuard.GetBool() && CanGuard[target.PlayerId])         // 既にガードtrueの時はこのガードを使用しない
            {
                isGuard = true;
                CanGuard[target.PlayerId] = false;
            }
        }
        if (isGuard)    // ガード処理
        {
            killer.RpcProtectedMurderPlayer(target);
            target.RpcProtectedMurderPlayer(target);
            info.CanKill = false;
        }
    }
}