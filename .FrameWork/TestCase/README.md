# 测试用例（`.FrameWork/TestCase/`）

> **Agent 的用法**：按改动范围在这张表里挑一个用例 → 跑 → **只看 `PASS` / `FAIL` 与最后几行**。
> 完整输出在 `.FrameWork/TestCase/.run/agent_out.txt`，**别整个读进上下文**（几万行，费 token）。
> 用例覆盖不了、要看「按测试场景怎么跑构建」→ 见 `DocAgent/Godot/Feature/测试用例.md` §1。
> ⚠️ **跑用例就是正式测试流程**，日常不涉及具体模块的改动跑它就算通过。

## 怎么跑

```bash
bash .FrameWork/TestCase/run.command            # 默认用例（launch）
bash .FrameWork/TestCase/run.command launch     # 指定用例
FULL=1 bash .FrameWork/TestCase/run.command     # 连 Tools/Server 一起编（改了那两个工程时才要）
```

**Windows(Git Bash) 与 macOS 跑的是同一套脚本**，平台差异全在 `lib/common.command` 里分平台处理
（Godot 路径 / 仓库根 / dotnet 输出语言 / 怎么喂 stdin），`run.command` 与 `cases/*.command` 是平台无关的。
要覆盖默认值：`GODOT=... REPO=... DOTNET=... bash .FrameWork/TestCase/run.command launch`。

**平台上的差异：**

- Godot 默认找 `/Applications/Godot_mono.app/Contents/MacOS/Godot`（也认 `~/Applications/` 和 PATH 里的 `godot`）；
  Windows 默认找 `D:/Ghost/GameDev/…_console.exe`，都可用 `GODOT=…` 覆盖。
- 仓库根从脚本位置往上推三级自动得出，换克隆位置不用改。
- 给人用可以**双击 `run.command`**（列用例 → 选一个 → 跑完停住等你看结果）。
  ⚠️ `.workbuddy-ai` 在 Finder 里是隐藏目录，要么 `Cmd+Shift+.` 显示隐藏文件，要么直接跑
  `bash .FrameWork/TestCase/run.command`。
- `build_tool.command`（场景 4）**两个平台都能跑**：Python 按 `python3` / `python` / 常见安装位置探测，
  探测不到只警告「跳过占用检查」，不会误拦（可用 `PYTHON=…` 覆盖）。

⚠️ **用例 = 测试场景 1**（日常开发 / 客户端 All-in-One，只编客户端 Debug）。
它**替代不了场景 2 / 3 / 4**：服务端 / 工具 / `ExportRelease` 一概不编；
反过来那些构建不跑代码、验不了运行期。对照表见 `DocAgent/Godot/Feature/测试用例.md` §2.0。
⇒ 日常改客户端跑用例即可；**部署调服务端 / 出包 / 改工具**时才跑对应场景的构建（§1）。
**改工具走 `build_tool.command`**（见下）—— 别手拼 `dotnet build`，会绕过占用检查。

退出码 `0` = PASS、`1` = FAIL、`2` = 用例不存在。

⚡ **用例不管美术包（`.pck`）** —— 资源打包由客户负责，测试流程只编代码（`build_godot()` 只编 `SekiaWorld.csproj`）。
跑用例前进图需要的美术包要**客户先自己打好**：`bash .Art/ExportAsset.command`（全部）/
`bash .Art/ExportAsset.command <工程名>`（单个），落 `.Art/.pck/`。

**双击 `run.command`**：

- 不带参数双击 → **列出现有用例**，输入编号或用例名回车即可；直接回车 = 默认用例。
- 菜单是从 `cases/` 目录**自动发现**的 ⇒ 加用例只要丢一个 `.command` 进去，不用改菜单。
- 跑完停在窗口里等你看 `PASS` / `FAIL`。
- 要从命令行指定：`run.command launch`（**有参数就不弹菜单**；非终端（Agent 管道）也不弹、不等回车）。

## 用例表

| 用例 | 覆盖什么 | 什么时候跑 |
|---|---|---|
| **`launch`**（默认） | 编 Debug → 无头起客户端（Agent 模式）→ **四条硬判据**：`scene create` 齐 / 无 `ERROR` / 无「配置缺失」/ 收到「登录界面」的菜单广播 | **日常改动拿不准就跑它。** 覆盖构建链路、Godot 启动、AOT + 热更两层加载、客户端初始化、服务端纤程起没起、Agent 模块与 UI 广播 |
| **`entermap`** | `launch` 四条 + **最短的「登录 → 创角 → 进首图」**：`R2C_Login` / `G2C_LoginGate` / 选角界面 → 创建角色 → 创角界面 → `进入游戏` / `[进图] 加载地图场景：res://Scene/prt_1/Sceneprt_1.tscn` / `M2C_StartSceneChange`(Sceneprt_1) / `M2C_CreateMyUnit` | **动了登录、会话、网络、进图、场景切换时跑它。** ⚠️ 比 `launch` 多两个前置条件（MongoDB + 测试账号），并且会**多跑约 1 分钟**。<br>（2026-10-13 更新：原来它断言 ET demo 的「大厅界面」并点大厅的「进入地图」按钮 —— 那条路已废弃，断言永远过不了；现在改成当前流程。） |
| **`createrole`** | `entermap` + **新手流程**：选角 → 创角场景/界面 → 换职业/性别/发型/发色/头饰 → 进入游戏进首图 prt_1 → 主界面/相机/寻路/设置 → 切换角色 → 掉线回写 → 重启重登读回位置 → 删角 | **动了角色存档（创角/删角/回写）、选角/创角、进图出生点、主相机时跑它。** 最完整也最慢（含 40 秒冻结模拟掉线） |
| **`changemap`** | `createrole` 的「登录 + 清场 + 创角 + 进首图」+ **传送**：`C2G_ChangeMap`(mapId=1) → `G2C_ChangeMap`(SceneProntera) → 服务端换图重建 Unit → 客户端加载 `res://Scene/Prontera/SceneProntera.tscn` → **重启重登仍在普隆德拉**（验证换图立刻落库）→ **地面碰撞体补齐**（`[地面碰撞] SceneProntera：新增 141`）→ **传送阵**（prt_1 1 个 →普隆德拉；普隆德拉 2 个 →prt_1 与同图 ex_5；菜单第 9 项走过去 → `[传送阵] 走进传送阵` → 真的切回 prt_1）→ **相机按图复位**（普隆德拉 `[相机] 主相机已就位：SceneProntera` + `fov 24.0`） | **动了地图身份/传送（`GateMapConst`、`Unit.MapId`、`C2G_ChangeMap*`、角色存档的 `MapId`、地面碰撞 `MapGroundCollision`、传送阵 `MapWarp`/`WarpComponent`、每图相机 `ApplyMapCameraInfo`）时跑它。** ⚠️ 前置多两条：`.pck` 里要有 `Scene/Prontera`、`.FrameWork/Config/RecastOutput/SceneProntera` 要烘好 |

> 加新用例：在 `cases/` 放一个 `.command`（`source` 公共库、用 `wait_for` 断言、末尾 `finish`），
> **并回到本表登记一行**（表是 Agent 选用例的依据，漏登记 = 用例白写）。
>
> ⚠️ **日志里的变量一律加花括号**：写 `${VAR}（中文）`，**别写** `$VAR（中文）`。
> macOS 自带的是 GNU bash 3.2，它有个老 bug —— `$VAR` 后面**紧贴多字节字符**时，
> 会把变量值和那个字符的头一个字节一起吞掉，日志变成乱码「��」（`$VAR 中文` 中间有空格就没事）。
> Windows 的 Git Bash 是 bash 5，不会暴露这个问题，所以以前没踩到。

⚠️ **断言串必须照抄实际日志**：日志是 BSON 风格 JSON（`"Error" : 0`，冒号两侧有空格），
类型名带命名空间前缀（`ET.R2C_Login`）⇒ 拿 `"R2C_Login"` 匹配不到（引号在 `ET.` 之前）。
先用 `grep` 看一眼真实文本再写断言，别照抄文档里的简写。

## 前置条件（客户侧，Agent 不管）

- MongoDB 已在 `127.0.0.1:27017` 监听（`.FrameWork/Launch02_RunServer.command` 那套是客户的事）。
- **`launch` 不需要测试账号**；**`entermap` 需要**
  （放 `.FrameWork/Config/TestAccount/<邮箱>.txt`：文件名 = 邮箱、内容 = 密码），且只用**账号密码**模式 ——
  免密 / 注册 / 重置密码都不接。约定见 `DocAgent/Godot/Feature/测试用例.md` §2.6。
  ⚠️ 账号是**客户预先注册在 MongoDB 里**的，这个文件只是把密码喂给 Agent，不代表库里真有这个账号。

## 目录

```
run.command        唯一入口：无参 = 默认用例；无参 + 终端双击 = 列用例菜单、跑完等回车
build_tool.command     场景 4：改工具后的构建（编前查占用，被占就拦下）
lib/common.command     公共库：build / start_game / send / wait_for / wait_soft / stop_game / finish
                  平台差异（Godot 路径 / 仓库根 / dotnet 语言 / 喂 stdin）都在这一个文件里分平台处理
cases/*.command        用例本体，一个用例一个文件（平台无关，别往里塞 if uname）
.run/             运行产物（构建日志、agent_cmd.txt、agent_out.txt），不入库
```

## 场景 4：改工具（`build_tool.command`）

```bash
bash .FrameWork/TestCase/build_tool.command              # 列出可选工具
bash .FrameWork/TestCase/build_tool.command configtools  # 编指定工具
bash .FrameWork/TestCase/build_tool.command all          # 三个都编
```

退出码 `0` = PASS、`1` = FAIL（含「被占用，先别编」）、`2` = 没有这个工具。

⭐ **编前会探测产物 dll 能不能独占打开** —— 被占用就**直接拦下、不白编一轮**，并说清是谁锁的、怎么解。
最典型的占用来源是 MCP `et-excel`（常驻进程锁着 `ET.ExcelMcp.dll`，直接编会报一堆 `MSB3026` 看着像代码错）。
**别手拼 `dotnet build` 编工具** —— 那样绕过了这道检查。

## 为什么能这么测

无头客户端本身起得来，但**没人点按钮** —— 登录是 UI 驱动的，没有鼠标事件就没有交互。
`--AgentMode=1` 让客户端把「当前界面的可操作项」编号广播出来、Agent 从 stdin 回编号，
模块直接跑按钮那段逻辑（等价于人类点击）。

`launch` 只取到**广播出来**这一步：它已经证明客户端一路初始化到了登录界面。
`entermap` 再往后走一步：登录 → 进图。
`createrole` 走新手流程第一段：登录 → 选角界面 → 创建角色 → 创角界面 → 返回选角。
原理与协议见 `DocAgent/Godot/Feature/测试用例.md` §2.6。

⚠️ 用例**只编 Debug**：`Bundles/Code` 跟着最后一次编译的配置走，而 Agent 模块套了 `#if DEBUG`
（Release 里没有它）⇒ 必须是 Debug 那份。细则见 `DocAgent/Godot/Feature/测试用例.md` §5。
