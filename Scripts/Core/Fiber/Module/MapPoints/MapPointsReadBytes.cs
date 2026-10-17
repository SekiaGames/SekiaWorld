namespace ET
{
    /// <summary>
    /// 布点数据文件的**读取请求** —— 与 <see cref="NavmeshComponent.RecastFileLoader"/>、
    /// <see cref="ConfigLoader.GetOneConfigBytes"/> 同一套：处理器是 Loader 层的
    /// <c>MapPointsBytesReader</c>，而请求类型**必须住 Core**。
    ///
    /// <para>
    /// 为什么在 Core：ET 的分层里 **Model 看不到 Loader 程序集**（Model 不引用主程序集），
    /// **Loader 层也看不到 Model 程序集**（Model 是运行时从 <c>res://Bundles/Code</c> 动态加载的热更 dll，
    /// 编译期没有引用）；两边唯一的共同引用就是 Core ⇒ 走 Invoke 的参数类型只能放这儿。
    /// </para>
    /// <para>
    /// 数据由美术在 <c>.Art/SekiaArt_Map/Demos/MapPoints/&lt;地图名&gt;.tscn</c> 摆、跑那张场景导出；
    /// 解析与业务 API（<c>Load</c>/<c>DefaultBornPoint</c>/<c>FindBornPoint</c>/<c>MainCamera</c>）在
    /// <c>Scripts/Model/Share/Module/MapPoints/MapPointsLoader.cs</c>。
    /// </para>
    /// </summary>
    public struct MapPointsReadBytes
    {
        /// <summary>地图名 = <c>MapConfig.NameEn</c>（<c>prt_1</c> / <c>Prontera</c>）</summary>
        public string MapName { get; set; }
    }
}
