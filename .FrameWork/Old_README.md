# SekiaWorld
轻量级联网MMO组队平台，买断制为主+服务性收费。
本体内容类似于奥比岛梦想国度的简化版，通过升级家园等级解锁服装家具。
  每天升1级，解锁一套主题资源。
  服装家具奖励能提供即时满足。
游戏大厅：可以和其他玩家组队体验与本体“弱关联”的游戏内容。
  MMO刷怪练级
  宝石TD
  其他可能的适合2D画风的游戏

# 项目技术选型

## 项目整体
1. 运行时：前后端都用.NET8
2. 网络框架ET，自定义的ET8
  客户端Godot引擎单一程序集，服务端代码和客户端代码结构类似。
3. 代码分析器：Tools/Share/Analyzer用于规范化代码提交。
  目前仅服务端使用，等后续双端代码更清晰了再扩展到客户端。
4. 网络方案：仅TCP，适合休闲游戏。
5. 网络消息（客户端与服务端之间）：MemoryPack
6. 表格配置：Tools/Share/Tool/ExcelExporter、Excel(EPPlus)
  从表格生成C#类型+JSON => C#对象 => BSON 序列化 .bytes
7. AI：ET AI 框架（行为机，非行为树）
  Model/Module/AI、Hotfix/Module/AI、AAIHandler
8. 寻路：DotRecast
9. 双端共享代码-为了高效前后端协同：是，但是不使用Link引用方式。
  不走Link引用方式，避免造成过多的程序集、域名、文件夹变动。
  通过DotNetFileMap.md来记录应该保持相同的文件名，文件保存两份；
  用脚本DotNetFileCheck.bat来确认两份文件一致。
  Core层、ThirdParty等底层基建还是走Link方式，因为这部分基本上不会改。
10. 服务端-客户端 网络同步方案：状态同步
  服务端下发状态快照，客户端插值/预测。
  不同于帧同步的只同步用户输入-各客户端跑同一套确定性模拟。

## 服务端
1. 数据库：MongoDB
  数据库对象持久化用到MongoDB.Bson
2. 软路由Router：不使用
3. 服务端热重载：DllHelper.cs从DLL文件加载
4. Map、Location服务器：无
  无ActorLocation消息，Gate兼任Map，客户端与Gate直连。

## 客户端
1. 客户端引擎：Godot4.7
2. 客户端GUI方案：使用Godot的原生UI。
3. 去掉反射 自动注册Publish

服务端日志 NLog 4.7.15（`Server/Core/Log/NLogger.cs`，客户端走 `GodotLogger` 不引 NLog）；CLI CommandLineParser（服务端）

# ET10特性

本项目已合并 ET10 的一批核心改进（详见 `ET10-Combine.md`，使用约定见 `AGENTS.md`）。简要好处与必要性：

| 特性 | 好处 / 必要性 |
| --- | --- |
| `EntityRef`/`EntityWeakRef` 强化 | 跨帧、跨 await 引用实体时避免拿到已销毁的空壳；配合编译期分析器（ET0033）强制包装，把“引用已回收实体”这类难查的 bug 挡在编译期 |
  EntityRef：await和跨帧逻辑临时持有Enity时，防止目标已销毁了还在用。
  EntityWeakRef：缓存、观察者，防止持有了就无法GC。
| `ETCancellationToken` 增强 | 协程可被主动取消或超时结束，避免请求取消后逻辑还在后台跑、`GetContextAsync` 永久挂起 |
| `CoroutineLock` 重写 | 超时自动释放，防止一处异常导致整条业务锁死；支持最大并发与帧内定量通知，锁行为可控可观测 |
| `ETTask` 上下文传播 | 取消凭证等上下文在协程链上自动传递，业务不必层层显式传参；链路统一后 `GetContextAsync` 才能可靠工作 |
| `ETVoid` 约束（只 await ET 任务） | 编译期防止协程链上混入 `.NET 原生 Task` 把上下文切断（断链即永久挂起） |
| ObjectPool 增强（`IPool.Clear` + 源码生成器 + 重复回收检测） | 回收前自动清理残留状态，避免复用对象带出上一次的数据；调试构建检测重复回收，把池化错误尽早暴露成异常而不是随机故障 |
| `ValueTypeWrap<T>` | 值类型装箱成池化对象的标准写法，为定时器/行为树等保存值类型场景省分配 |
| `ChildrenCollection`/`ComponentsCollection` + `IsSerializeWithParent` | 子实体/组件单集合存储（去掉双份列表），序列化只写需要持久化的成员；写库前 `ClearUnSerialized()` 丢掉运行时实体，存档更小、GC 更少 |
| `ProcessFiberAddressSingleton` | 纤程地址按 SceneType 登记查表，替代散落的硬编码纤程 id，扩容/拆分纤程时不再到处改常量 |
| 消息池化强制（`[DisableNew]` 分析器） | 禁止 `new` 消息，保证消息走对象池，减少高频协议消息的分配与回收压力 |
| 行为树运行时（`Godot/Scripts/Core/BT`） | AI/玩法逻辑用节点树（顺序/选择/取反 + 业务叶子节点）描述，替代手写 `if/else` 状态机；树数据走 `Config/Excel/BTConfig.xlsx` 表格（改表即改 AI，参数写错建树时报错），并提供 `BTDebug` 运行路径与 `BTNodeMeta` 元数据供调试面板/编辑器使用；可视化编辑器 UI 待有 Godot 环境时单独立项 |

# 项目设定
1. Godot编辑器：/Volumes/Data/Users/sekia/Applications/Godot_mono.app
  Godot官方编辑器
2. References目录：放参考项目资源，如果不存在时执行脚本拉取。
脚本执行命令：./References/DownloadReferences.sh
参考1：godot
  Godot引擎官方源码

3. Godot目录：前端app

# ET框架调研
后续将研究接入ET框架（github.com/egametang/ET），具体技术选型待定。
以下为各版本特点备忘：

## ET10（master）
1. 定位为AI编码时代的框架：AI写的代码必须能编译、过测试、遵守架构、可热重载、可分布式扩展
2. Package模式（自ET9起），功能以cn.etetet.*包在ET Package中心发布安装
3. cn.etetet.harness：AI skills（et-code/async/excel/luban/build/test/tdd等）+分析器，严格约束AI行为
4. cn.etetet.unitybridge：68个Unity指令，让AI直接操作引擎（场景、GameObject、Inspector、Prefab、编译、Play Mode、截图等）
5. cn.etetet.test：可模拟整条客户端-服务器交互的测试框架，AI写test自验证，UI Test也能做
6. 新增包：behavior tree（行为树+可视化编辑）、servicediscovery（服务发现/租约/故障恢复）、conditionexpr（Excel条件表达式编译成行为树）、spell（技能与Buff系统）、aspire（.NET Aspire云原生编排+OTLP/OpenTelemetry）
7. ETTask传递上下文（去掉CancellationToken传递）；Entity精简（去ChildrenDB/ComponentDB，自定义序列化）
8. 许可变更：开源版仅限学习，不可二次开源，商业使用需购买许可（4999）

# ET版本接入顺序
1. 移植ET-Godot
2. 合并ET6 release6.0 后续 4 个提交
3. 合并ET7？

# 构建流程
## 环境要求
1. .NET SDK 8.0
2. Godot 4.7.2 mono：/Volumes/Data/Users/sekia/Applications/Godot_mono.app
3. Unity.Mathematics 1.2.6：服务端编译依赖，已放在 Godot/Scripts/ThirdParty/Unity.Mathematics（服务端经 DotNet.ThirdParty 链接编译）
   重新获取：下载 https://download.packages.unity.com/com.unity.mathematics/-/com.unity.mathematics-1.2.6.tgz，解压到 Godot/Scripts/ThirdParty/Unity.Mathematics（tar --strip-components=1）

## 注意事项
1. 服务端工作目录必须是 Bin，`--StartConfig=StartConfig/Localhost` 不能省略 StartConfig/ 前缀
3. 首次 Godot 导入会输出少量警告（Map1.mtl 缺失、headless 对话框提示），第二次导入即干净，不影响运行
4. 非交互/后台运行服务端用 `--Console=0`；`--Console=1` 会启动交互控制台，stdin 关闭（非终端）时控制台循环会打印“标准输入已关闭”后退出，不再刷空引用异常
5. 改动 `Tools/Share/Share.SourceGenerator`（源码生成器）后，需重新构建并复制 `bin/Debug/Share.SourceGenerator.dll` 到 `Godot/Scripts/ThirdParty/Libs/`，客户端才会用上新生成器（服务端经 ProjectReference 自动生效）

## Excel 工具（ExcelMcp）
`Tools/ExcelMcp`（移植自 ET10，目标框架降级到 net8，EPPlus 8.0.8，共 25 个工具），供 AI/脚本直接读写 `Config/Excel` 源表；支持命令行（CLI）与模型上下文协议（MCP，标准输入输出）两种模式。

```powershell
dotnet build Tools/ExcelMcp/ET.ExcelMcp.csproj -c Debug   # 构建
Launch03_ExcelMcp.bat cli list                            # 列出全部工具
Launch03_ExcelMcp.bat cli excel_sheet '{"operation":"list","path":"Config/Excel/xxx.xlsx"}'
Launch03_ExcelMcp.bat                                     # 无参数 = 启动 MCP 服务(标准输入输出模式)
```

- MCP 客户端配置：命令 `dotnet`，参数 `<仓库根>/Tools/ExcelMcp/bin/Debug/net8.0/ET.ExcelMcp.dll`，工作目录设为仓库根
- 相对路径基于当前工作目录；出于安全校验，路径不允许包含 `..`，也不接受绝对路径
- 改完表后记得跑 `Launch01_ExportExcelAndProto.bat` 重新导出

## 行为树编辑器（Godot 工具场景）
`Godot/Demos/BTEditor/BTEditor.tscn` 是一个普通场景（F6 手动运行，不需要服务端、不依赖编辑器插件）：左侧方块 + 直线画树，点方块在右侧改参数，可加/删节点、校验、保存。

```powershell
# 1. Godot 里运行 Godot/Demos/BTEditor/BTEditor.tscn，编辑后点「保存 JSON」
# 2. 把 JSON 写回 Excel 源头，再重新导出
Launch04_BTImport.bat
Launch01_ExportExcelAndProto.bat
```

- 数据流：`Config/Excel/BTConfig.xlsx`(源头) → ExcelExporter → `Config/Excel_TestJsonExport/c/BTConfig.txt`(编辑器读写) → `Launch04_BTImport` 写回 Excel
- 校验：保存/导入前都会跑 `BTConfigValidator`（多根、父节点缺失、成环、节点类型/参数非法、同级顺序重复），错误直接显示在工具条上

# Agent约定
1. 项目使用中文思考
2. 文档与注释避免英文首字母缩写，使用通俗、易读的中文表述。