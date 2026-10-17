#!/bin/bash
# 测试用例公共库 —— 构建 / 起无头客户端 / 发命令 / 断言
# 由 cases/*.sh source，不要单独跑。
#
# 同一套用例要在 Windows(Git Bash) 与 macOS 上跑。差异只有四处，都在本文件里分平台处理：
#   ① Godot 可执行文件在哪   ② 仓库根在哪   ③ dotnet 输出的语言   ④ 怎么把命令喂进游戏 stdin
# 下面每个分平台处都标了「平台差异」，cases/*.sh 与 run.sh 是平台无关的，不要往里塞 if uname。
#
# ⚠️ 写日志时变量一律加花括号：`${VAR}（中文）`，不要写 `$VAR（中文）`。
#    macOS 自带的是 GNU bash 3.2（/bin/bash），它有个老 bug —— `$VAR` 后面紧跟**多字节字符**时
#    会把变量值和那个字符的头一个字节一起吞掉，日志里变成乱码「��」。
#    `$VAR 中文`（中间有空格）和 `${VAR}中文` 都没事，只有紧贴着的裸变量名会中招。
#    改用例时别把花括号去掉。

# ---------- 平台 ----------

case "$(uname -s)" in
    Darwin)               PLATFORM=mac ;;
    MINGW*|MSYS*|CYGWIN*) PLATFORM=windows ;;
    *)                    PLATFORM=unix ;;
esac

# 平台差异①：Godot 可执行文件。
# Windows 是 console 版 exe（不带 console 的话 stdout 拿不到）；macOS 是 .app 里的二进制。
# 两边都可以用 GODOT=... 覆盖。
if [ -z "${GODOT:-}" ]; then
    if [ "$PLATFORM" = windows ]; then
        GODOT="D:/Ghost/GameDev/Godot_v4.7.2-stable_mono_win64/Godot_v4.7.2-stable_mono_win64_console.exe"
    else
        for c in \
            "/Applications/Godot_mono.app/Contents/MacOS/Godot" \
            "$HOME/Applications/Godot_mono.app/Contents/MacOS/Godot" \
            "$HOME/Godot_mono.app/Contents/MacOS/Godot"
        do
            [ -x "$c" ] && { GODOT="$c"; break; }
        done
        [ -z "${GODOT:-}" ] && GODOT="$(command -v godot 2>/dev/null || echo godot)"
    fi
fi

# 平台差异②：仓库根。
# 本套用例住在仓库内（`.FrameWork/TestCase/`）⇒ 从本文件往上数三级就是仓库根
# （lib → TestCase → .FrameWork → 仓库根），换机器/换克隆位置都不用改。
# Windows 沿用 D:/SekiaWorld（Git Bash 里给 dotnet 传 /d/... 有被 MSYS 改写路径的风险，不折腾）。
# 两边都可以用 REPO=... 覆盖。
if [ -z "${REPO:-}" ]; then
    if [ "$PLATFORM" = windows ]; then
        REPO="D:/SekiaWorld"
    else
        REPO="$(cd "$(dirname "${BASH_SOURCE[0]}")/../../.." && pwd)"
    fi
fi

# dotnet 同理：双击 .command 起的 shell PATH 很干净，/usr/local/share/dotnet 不在里面
if [ -z "${DOTNET:-}" ]; then
    if command -v dotnet >/dev/null 2>&1; then
        DOTNET=dotnet
    elif [ -x /usr/local/share/dotnet/dotnet ]; then
        DOTNET=/usr/local/share/dotnet/dotnet
    elif [ -x "$HOME/.dotnet/dotnet" ]; then
        DOTNET="$HOME/.dotnet/dotnet"
    else
        DOTNET=dotnet
    fi
fi

RUN_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)/.run"
CMD_FILE="$RUN_DIR/agent_cmd.txt"
OUT_FILE="$RUN_DIR/agent_out.txt"
mkdir -p "$RUN_DIR"

FAILED=0
GAME_PID=""

log() { echo "[$(date +%H:%M:%S)] $*"; }
ok()  { echo "  ok   $*"; }
bad() { echo "  FAIL $*"; FAILED=$((FAILED + 1)); }

# ---------- 构建 ----------

# build <相对仓库根的 csproj> <配置，可空>
build() {
    local proj="$1" cfg="$2"
    local log_file="$RUN_DIR/build.log"
    local args=("$proj")
    [ -n "$cfg" ] && args+=("-c" "$cfg")

    ( cd "$REPO" && "$DOTNET" build "${args[@]}" > "$log_file" 2>&1 )
    local rc=$?

    # 平台差异③：dotnet CLI 的输出语言跟系统走 —— Windows 上是中文（「已成功生成」「个警告」），
    # macOS 上是英文（Build succeeded / Warning(s)）。两种都认，别只看一种。
    local w e
    w=$(grep -oE '[0-9]+ 个警告|[0-9]+ Warning\(s\)' "$log_file" | tail -1)
    e=$(grep -oE '[0-9]+ 个错误|[0-9]+ Error\(s\)' "$log_file" | tail -1)

    if [ $rc -ne 0 ] || ! grep -qE '已成功生成|Build succeeded' "$log_file"; then
        bad "构建失败：${proj} ${cfg}（${w:-?} ${e:-?}）—— 详见 $log_file"
        return 1
    fi
    ok "构建 $proj $cfg → $w $e"
    return 0
}

# 恒定标准的前两条（只有改了 Tools/Server 工程级代码才需要，All-in-One 跑测不需要）
# ⚠️ 全仓禁止编 .sln（各 csproj 里有 RejectSolutionBuild 兜底）⇒ 服务端逐个编 csproj。
#    编 App 会传递拉起 Loader / Model / Core / ThirdParty，并**由 App 自己的 BuildHotfixLayers
#    target 补编 Server.Hotfix**（它没人 ProjectReference，产出 Bin/Hotfix.dll，启动时 CodeLoader 要读）。
build_server_side() {
    build .FrameWork/Tools/ConfigTools/ConfigTools.csproj Release || return 1
    build .FrameWork/Server/App/Server.App.csproj Release || return 1
}

# 客户端侧：**只编 Debug**。编 SekiaWorld.csproj，不编 sln（sln 只给人看项目完整性，不参与测试环节）。
# ⚡ `Bundles/Code` 跟着最后一次编译的配置走（Directory.Build.targets 的拷贝不带配置条件），
#    所以只编 Debug 就能让热更层也是 Debug 那份 —— Agent 模块套了 `#if DEBUG`，Release 里没有它。
# ⚠️ 美术包（.pck）不在测试流程内 —— 资源打包由客户负责，用例只管编代码。
build_godot() {
    build SekiaWorld.csproj Debug || return 1
}

# ---------- 起客户端 / 交互 ----------

# 起无头客户端（Agent 模式）。
# ⚠️ 两边都必须避开「管道写法」：`tail -f | godot` 或 `mkfifo` 用普通管道接，tail 与脚本会互相等，死锁。
start_game() {
    : > "$OUT_FILE"

    case "$PLATFORM" in
        windows)
            # 进程替换 + GNU tail 的 `--pid=$$`：脚本一退 tail 自己就收，游戏随即 EOF。
            : > "$CMD_FILE"
            "$GODOT" --headless --path "$REPO" -- --AgentMode=1 \
                < <(tail -f -n 0 --pid=$$ "$CMD_FILE") > "$OUT_FILE" 2>&1 &
            ;;
        *)
            # 平台差异④：macOS/BSD 的 tail 没有 --pid，改用 FIFO。
            #   ① `exec 3<>` 把读写端一起占住 —— FIFO 只开写端会一直等读者，正是要避开的死锁；
            #   ② 这个 fd 还兼作「游戏 stdin 的总开关」：脚本退出 ⇒ fd 关 ⇒ 游戏收到 EOF ⇒ 自己退，
            #      顶上 Windows 那边 --pid 的作用（见 cleanup）。
            rm -f "$CMD_FILE"
            mkfifo "$CMD_FILE"
            exec 3<> "$CMD_FILE"
            "$GODOT" --headless --path "$REPO" -- --AgentMode=1 \
                < "$CMD_FILE" > "$OUT_FILE" 2>&1 &
            ;;
    esac

    GAME_PID=$!
    log "客户端已起（pid ${GAME_PID}，平台 ${PLATFORM}），输出 → $OUT_FILE"
}

# 往 stdin 送一行
send() {
    if [ "$PLATFORM" = windows ]; then
        echo "$*" >> "$CMD_FILE"
    else
        echo "$*" >&3
    fi
}

# 读测试账号（返回 "<邮箱> <密码>"）。目录里只该放一个账号，多个就取第一个并提示。
# ⚠️ 账号由客户预先注册在 MongoDB 的 ET1 库里；这个文件只把密码喂给 Agent。
load_test_account() {
    local dir="$REPO/.FrameWork/Config/TestAccount"
    local f
    f=$(ls "$dir"/*.txt 2>/dev/null | head -1)
    if [ -z "$f" ]; then
        bad "没有测试账号：在 $dir/ 放一个 <邮箱>.txt（内容 = 密码）"
        return 1
    fi
    local email pwd
    email=$(basename "$f" .txt)
    pwd=$(tr -d '\r\n' < "$f")
    if [ -z "$email" ] || [ -z "$pwd" ]; then
        bad "测试账号文件为空或格式不对：$f"
        return 1
    fi
    echo "$email $pwd"
}

# wait_for <字面串> <超时秒> <说明>   —— 用固定串匹配，避免 [ ] 被当正则
wait_for() {
    local pat="$1" secs="${2:-40}" what="${3:-$1}"
    local n=$((secs * 4)) i=0
    while [ $i -lt $n ]; do
        if grep -qF -- "$pat" "$OUT_FILE" 2>/dev/null; then
            ok "$what"
            return 0
        fi
        sleep 0.25
        i=$((i + 1))
    done
    bad "超时 ${secs}s：$what"
    return 1
}

# 软断言：没等到只提示，不计入 FAILED（用于顺带覆盖、可能抖动的环节）
wait_soft() {
    local pat="$1" secs="${2:-20}" what="${3:-$1}"
    local n=$((secs * 4)) i=0
    while [ $i -lt $n ]; do
        if grep -qF -- "$pat" "$OUT_FILE" 2>/dev/null; then
            ok "$what"
            return 0
        fi
        sleep 0.25
        i=$((i + 1))
    done
    echo "  warn $what 未出现（不计入判定，需要时看 ${OUT_FILE}）"
    return 0
}

# ---------- 原「冒烟」的三条判据（2026-10-02 起并入用例） ----------

# 5 条 scene create 逐条齐（顺序无关）。条数跟着编译期角色走：
# ClientServer/Server = 5 条；Client = 1 条（纯客户端不起服务端纤程，是预期不是失败）。
assert_scene_create() {
    local mode
    mode=$(sed -n 's/.*<SekiaCodeMode>\([^<]*\)<\/SekiaCodeMode>.*/\1/p' \
        "$REPO/Scripts/Directory.Build.props" 2>/dev/null | head -1)

    local want=("Main -1 1")
    [ "$mode" != "Client" ] && want+=("NetInner -2 1" "Realm 1 1" "Gate 2 1" "Gate 3 1")

    local i=0 missing="" w
    while [ $i -lt 40 ]; do
        missing=""
        for w in "${want[@]}"; do
            grep -qF -- "scene create: $w" "$OUT_FILE" 2>/dev/null || missing="$missing[$w] "
        done
        if [ -z "$missing" ]; then
            ok "${#want[@]} 条 scene create 齐（角色 ${mode:-?}）"
            return 0
        fi
        sleep 0.25
        i=$((i + 1))
    done

    bad "scene create 缺：$missing"
    return 1
}

# 日志里不该出现 ERROR。
# ⚠️ 三条豁免，都不是回归：
#    ① `.godot/` 缓存被删会刷的 autoload 报错；
#    ② 退出阶段的收摊噪声（"BUG: Unreferenced static string" / "Pages in use exist at exit"
#       / "resources still in use at exit" / "RID allocations ... leaked"）—— 断言都在
#       stop_game 之前跑，正常轮不到，但用例中途被打断时别误报。
#    ③ headless 的 dummy 渲染器遇到「材质槽是空的 MeshInstance3D」会报 "Parameter \"material\" is null."
#       （servers/rendering/dummy/storage/material_storage.cpp）。真机（Vulkan）走默认材质不报；
#       主相机改成跟随玩家之后角色总在画面里，这条就出现得比较稳定 —— 属美术资产/引擎噪声，不是逻辑错
#       （工程代码全仓没有任何 set_shader_parameter / material_override 调用）。
assert_no_error() {
    local hit
    hit=$(grep -n "ERROR" "$OUT_FILE" 2>/dev/null \
        | grep -v "Failed to instantiate an autoload" \
        | grep -v "Unreferenced static string" \
        | grep -v "Pages in use exist at exit" \
        | grep -v "resources still in use at exit" \
        | grep -v "RID allocations of type" \
        | grep -v "ObjectDB instances were leaked" \
        | grep -v 'Parameter "material" is null' \
        | head -3)

    # ④ 未捕获的 C# 异常另外扫一遍：ET 的 `ETTask.ExceptionHandler`（Init.cs 里挂的 Log.Error）打出来的
    #    是**裸的** `System.NullReferenceException: …`，不含 "ERROR" 字样 ⇒ 上面那条 grep 抓不到。
    #    实测踩过：回选角后服务端还在推单位移动消息，客户端抛了一串 NRE，用例却报「无 ERROR」。
    if [ -z "$hit" ]; then
        hit=$(grep -nE "System\.[A-Za-z.]*Exception|Unhandled exception" "$OUT_FILE" 2>/dev/null | head -3)
    fi

    if [ -n "$hit" ]; then
        bad "出现 ERROR：$hit"
        return 1
    fi
    ok "无 ERROR"
    return 0
}

# 「配置缺失」= Init.codeMode 的位与实际要读的表不匹配
assert_no_missing_config() {
    local hit
    hit=$(grep -n "配置缺失" "$OUT_FILE" 2>/dev/null | head -3)
    if [ -n "$hit" ]; then
        bad "出现「配置缺失」：$hit"
        return 1
    fi
    ok "无配置缺失"
    return 0
}

# 日志里不该出现 RPC 错误（"Error" : 非 0）—— 留给将来走 RPC 的用例（launch 不发 RPC，用不上）
assert_no_rpc_error() {
    local hit
    hit=$(grep -oE '"Error" : [1-9][0-9]*' "$OUT_FILE" | head -3)
    if [ -n "$hit" ]; then
        bad "出现 RPC 错误：$hit"
        return 1
    fi
    ok "无 RPC 错误"
    return 0
}

# ---------- 收尾 ----------

stop_game() {
    [ -n "$GAME_PID" ] || return 0
    if ! kill -0 "$GAME_PID" 2>/dev/null; then
        ok "客户端已退出"
        return 0
    fi

    send quit
    local i=0
    while [ $i -lt 40 ]; do
        kill -0 "$GAME_PID" 2>/dev/null || { ok "客户端已退出"; return 0; }
        sleep 0.25
        i=$((i + 1))
    done

    bad "客户端 10 秒没退出，强杀"
    kill "$GAME_PID" 2>/dev/null
    return 1
}

cleanup() {
    if [ -n "$GAME_PID" ] && kill -0 "$GAME_PID" 2>/dev/null; then
        send quit
        sleep 2
        kill -0 "$GAME_PID" 2>/dev/null && kill "$GAME_PID" 2>/dev/null
    fi

    # FIFO 收摊：关掉 fd 3 ⇒ 游戏那边立刻 EOF（Windows 走 --pid，不用管）。
    # 顺序要紧：必须先关 fd 再删文件，反过来的话游戏可能还挂在打开中的管道上。
    if [ "$PLATFORM" != windows ]; then
        exec 3>&- 2>/dev/null
        rm -f "$CMD_FILE"
    fi
}
trap cleanup EXIT

finish() {
    echo
    if [ "$FAILED" -eq 0 ]; then
        echo "PASS  ${CASE_NAME:-$0}"
        exit 0
    fi
    echo "FAIL  ${CASE_NAME:-$0}（$FAILED 项不通过）"
    echo "      完整输出：$OUT_FILE"
    exit 1
}
