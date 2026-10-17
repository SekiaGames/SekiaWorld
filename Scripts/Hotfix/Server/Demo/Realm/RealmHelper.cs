using System.Collections.Generic;

namespace ET.Server
{
    /// <summary>
    /// 拿 RealM 的 ActorId —— Gate 要往 RealM 发消息（回问角色列表 / 创角 / 删角 / 掉线回写）时用。
    /// 与 <see cref="RealmGateAddressHelper"/>（RealM 挑 Gate）是反方向的一对。
    /// </summary>
    public static class RealmHelper
    {
        /// <summary>按 zone 找 RealM；本工程目前只有一个 zone 一个 RealM，找不到就退第一个</summary>
        public static StartSceneConfig GetRealm(int zone)
        {
            List<StartSceneConfig> realms = StartSceneConfigCategory.Instance.Realms;
            foreach (StartSceneConfig realm in realms)
            {
                if (realm.Zone == zone)
                {
                    return realm;
                }
            }
            return realms[0];
        }
    }
}
