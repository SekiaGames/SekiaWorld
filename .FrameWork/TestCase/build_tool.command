#!/bin/bash
# 场景 4：改工具后的构建
#   bash .FrameWork/TestCase/build_tool.command              # 列出可选工具
#   bash .FrameWork/TestCase/build_tool.command configtools  # 编指定工具
#   bash .FrameWork/TestCase/build_tool.command all          # 三个都编
# 退出码：0 = PASS，1 = FAIL（含「被占用，先别编」），2 = 没有这个工具
#
# ⚠️ 这里把「编前检查占用」固化成流程：MCP（et-excel）常驻时会锁住
#    ET.ExcelMcp.dll，直接编会报一堆 MSB3026/MSB3021/MSB3027 —— 看着像代码错了，
#    其实全是文件占用重试。被占用时本脚本直接拦下并说明怎么解，不让你白编一轮。

# 仓库根 = 本文件所在目录往上两级（TestCase → .FrameWork → 仓库根）
REPO="$(cd "$(dirname "$0")/../.." && pwd)"
cd "$REPO" || exit 1

# python 只用来做「能否独占打开 dll」的探测（见 is_free）。
# ⚠️ 别写死某一个平台：默认值原来是 Windows 上的绝对路径，macOS 上会变成
#    「command not found（127）」，而 is_free 把非 0 一律当「被占用」⇒ 明明没锁也拦下来。
# 可用 PYTHON 环境变量覆盖。
if [ -z "${PYTHON:-}" ]; then
    for c in python3 python \
             "$HOME/.workbuddy-ai/binaries/python/versions/3.13.12/python.exe" \
             "C:/Users/Sekia/.workbuddy-ai/binaries/python/versions/3.13.12/python.exe"; do
        if command -v "$c" >/dev/null 2>&1 || [ -x "$c" ]; then
            PYTHON="$c"
            break
        fi
    done
fi
PY="${PYTHON:-}"

ok()   { echo "  ok   $*"; }
bad()  { echo "  FAIL $*"; FAILED=$((FAILED + 1)); }
log()  { echo "[$(date +%H:%M:%S)] $*"; }
FAILED=0

# 工具名 → "工程路径|产物 dll|占用说明"
tool_spec() {
    case "$1" in
        configtools)   echo ".FrameWork/Tools/ConfigTools/ConfigTools.csproj|.FrameWork/Bin/Tool.dll|" ;;
        recastnavbaker) echo ".FrameWork/Tools/RecastNavBaker/RecastNavBaker.csproj|.FrameWork/Bin/RecastNavBaker.dll|" ;;
        excelmcp)      echo ".FrameWork/Tools/ExcelMcp/ET.ExcelMcp.csproj|.FrameWork/Tools/ExcelMcp/bin/Release/net8.0/ET.ExcelMcp.dll|MCP(et-excel)" ;;
        *) return 1 ;;
    esac
}

list_tools() {
    echo "可选工具："
    echo "  configtools      导表（程序集名 Tool，产物 .FrameWork/Bin/Tool.dll）"
    echo "  recastnavbaker   烘导航网格（Launch05 的入口）"
    echo "  excelmcp         MCP et-excel（⚠️ 编前必须先停 MCP）"
    echo "  all              三个都编（excelmcp 若被占用会跳过并提示）"
}

# 探测 dll 是否被占用。退出码：0 = 没被占用（含产物不存在、以及探测本身不可用）；1 = 被占用。
# 用 Python 而不是 PowerShell：权限检查一致、退出码干净。
# ⚠️ 不是「找哪个进程锁的」—— 那种要 handle.exe / 提权；能不能独占打开就足够判定。
# ⚠️ 只有 **恰好 1** 才算被占用：python 缺席时会返回 127，那是探测坏了、不是被锁，
#    必须放行（否则 macOS 上会误拦，见文件头 PY 的注释）。
is_free() {
    if [ -z "$PY" ]; then
        echo "  warn 找不到 python（可设 PYTHON=… 覆盖），跳过占用检查"
        return 0
    fi

    "$PY" -c "
import sys
try:
    f = open(sys.argv[1], 'r+b'); f.close(); sys.exit(0)   # 没锁
except FileNotFoundError:
    sys.exit(0)                                            # 产物还没生成，谈不上锁
except PermissionError:
    sys.exit(1)                                            # 被占用
" "$1"
    local rc=$?

    if [ "$rc" -gt 1 ]; then
        echo "  warn python 探测异常（退出码 $rc），跳过占用检查"
        return 0
    fi

    return "$rc"
}

build_one() {
    local key="$1"
    local spec proj dll who
    spec=$(tool_spec "$key")
    IFS='|' read -r proj dll who <<< "$spec"

    log "构建 $key → $proj"

    # ★ 编前占用检查（固化点）
    if is_free "$dll"; then
        ok "产物未被占用：$dll"
    else
        if [ -n "$who" ]; then
            echo "  FAIL $dll 被占用（$who 常驻中）—— 先停掉它再编。"
            echo "        停法：断开 / 重启该 MCP 连接（改了 MCP 配置要重启 + 点「信任」）。"
        else
            echo "  FAIL $dll 被占用 —— 先关掉正在用它 / 它所属的进程再编。"
        fi
        FAILED=$((FAILED + 1))
        return 1
    fi

    # 工具只有 Release + 恒优化（显式 -c Debug 报 MSB4126）
    local log_file=".FrameWork/TestCase/.run/build_tool_$key.log"
    mkdir -p .FrameWork/TestCase/.run
    dotnet build "$proj" -c Release -t:Rebuild > "$log_file" 2>&1
    local rc=$?

    # ⚠️ dotnet CLI 的输出语言随机器走：中文界面是「已成功生成 / N 个警告 / N 个错误」，
    #    英文是「Build succeeded / N Warning(s) / N Error(s)」。只认一种会在另一种机器上**静默误判**
    #    （实测 macOS 这台是英文 ⇒ 明明编成功却报「构建失败」）。两边都认。
    local w e
    w=$(grep -oE '[0-9]+ 个警告|[0-9]+ Warning\(s\)' "$log_file" | tail -1)
    e=$(grep -oE '[0-9]+ 个错误|[0-9]+ Error\(s\)' "$log_file" | tail -1)

    if [ $rc -ne 0 ] || ! grep -qE "已成功生成|Build succeeded" "$log_file"; then
        bad "构建失败：$proj（${w:-?} ${e:-?}）—— 详见 $log_file"
        return 1
    fi
    ok "构建 $proj → ${w:-?} ${e:-?}"
    return 0
}

KEY="${1:-}"

if [ -z "$KEY" ]; then
    list_tools
    exit 0
fi

if [ "$KEY" != "all" ] && ! tool_spec "$KEY" >/dev/null; then
    echo "没有这个工具：$KEY"
    echo
    list_tools
    exit 2
fi

if [ "$KEY" = "all" ]; then
    for k in configtools recastnavbaker excelmcp; do
        build_one "$k" || true
    done
else
    build_one "$KEY" || true
fi

echo
if [ "$FAILED" -eq 0 ]; then
    echo "PASS  工具构建"
    exit 0
fi
echo "FAIL  工具构建（$FAILED 项不通过）"
exit 1
