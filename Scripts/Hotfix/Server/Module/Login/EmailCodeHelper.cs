namespace ET.Server
{
    /// <summary>
    /// 验证码组件的取用入口（同一个 Realm 只挂一份，挂在其根场景上）。
    /// 单独成文件：本仓约定一类一文件。
    /// </summary>
    public static class EmailCodeHelper
    {
        public static EmailCodeComponent GetOrAdd(Scene scene)
        {
            EmailCodeComponent component = scene.GetComponent<EmailCodeComponent>();
            if (component == null)
            {
                component = scene.AddComponent<EmailCodeComponent>();
            }

            return component;
        }
    }
}
