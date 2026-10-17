using System;


namespace ET.Client
{
    public static partial class EnterMapHelper
    {
        /// <summary>
        /// 没有角色上下文的进图（ET 自带的 Lobby 演示路径）：存档位未知 ⇒ 掉线时不会回写位置。
        /// </summary>
        public static ETTask EnterMapAsync(Scene root)
        {
            return EnterMapAsync(root, -1);
        }

        /// <summary>
        /// 进图。<paramref name="roleSequence"/> = 这次用的是哪个存档位上的角色（选角进游戏传选中的那个、创角后进游戏传刚建的那个）：
        /// 服务端要靠它配对角色的存档记录，玩家掉线（心跳超时）时才回写得了这个角色的位置。
        /// </summary>
        public static async ETTask EnterMapAsync(Scene root, int roleSequence)
        {
            try
            {
                EntityRef<Scene> rootRef = root;
                C2G_EnterMap c2GEnterMap = C2G_EnterMap.Create();
                c2GEnterMap.RoleSequence = roleSequence;
                G2C_EnterMap g2CEnterMap = await root.GetComponent<ClientSenderComponent>().Call(c2GEnterMap) as G2C_EnterMap;
                root = rootRef;

                // 等待场景切换完成
                await root.GetComponent<ObjectWait>().Wait<Wait_SceneChangeFinish>();
                root = rootRef;
                
                EventSystem.Instance.Publish(root, new EnterMapFinish());
            }
            catch (Exception e)
            {
                Log.Error(e);
            }	
        }
    }
}