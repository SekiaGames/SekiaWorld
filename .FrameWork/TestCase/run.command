#!/bin/bash
# 测试用例入口（唯一入口；Windows Git Bash 与 macOS 跑同一份）
#   bash .FrameWork/TestCase/run.command              跑默认用例（launch）
#   bash .FrameWork/TestCase/run.command launch       跑指定用例
#   FULL=1 bash .FrameWork/TestCase/run.command       连 Tools/Server 一起编（改了那两个工程时才要）
#   双击本文件                                          列出现有用例，输编号或用例名回车（跑完按回车关窗）
# 退出码：0 = PASS，1 = FAIL，2 = 用例不存在
#
# 平台差异（Godot 路径 / 仓库根 / dotnet 输出语言 / 喂 stdin 的办法）全在 lib/common.command 里分平台处理；
# 本文件与 cases/*.command 平台无关。要覆盖默认值：GODOT=... REPO=... DOTNET=... bash run.command launch
#
# ⚠️ `--exec` 是内部用的：外层拿到用例名后原样再起一个自己跑用例。
#    这样 cases 里 finish 的 exit 只结束子进程，外层才能拿到退出码、并在双击场景下等回车。

HERE="$(cd "$(dirname "$0")" && pwd)"
cd "$HERE" || exit 1

# 双击起的 shell PATH 很干净（只有 /usr/bin:/bin:/usr/sbin:/sbin），dotnet / godot 都不在里面
export PATH="$PATH:/usr/local/share/dotnet:$HOME/.dotnet:/opt/homebrew/bin:/usr/local/bin"

if [ "${1:-}" = "--exec" ]; then
    CASE="${2:-launch}"
    if [ ! -f "$HERE/cases/$CASE.command" ]; then
        echo "没有这个用例：$CASE"
        echo "现有用例：$(ls "$HERE/cases" 2>/dev/null | sed 's/\.command$//' | tr '\n' ' ')"
        exit 2
    fi
    source "$HERE/lib/common.command"
    source "$HERE/cases/$CASE.command"
    exit 0
fi

CASE="$*"

# 双击 = 无参数 + 标准输入输出都是终端。命令行用法（含 Agent）一律不弹菜单、不等回车。
DOUBLE_CLICK=0
if [ -z "$CASE" ] && [ -t 0 ] && [ -t 1 ]; then
    DOUBLE_CLICK=1
    echo
    echo "  可选的测试用例："
    names=()
    for f in "$HERE"/cases/*.command; do
        [ -e "$f" ] || continue
        names+=("$(basename "$f" .command)")
        echo "    ${#names[@]}. ${names[${#names[@]}-1]}"
    done
    echo
    printf "  输入编号或用例名（回车 = 默认用例）："
    read -r SEL
    echo

    if [ -n "$SEL" ]; then
        case "$SEL" in
            *[!0-9]*) CASE="$SEL" ;;                                  # 含非数字 = 当用例名
            *)        CASE="${names[$((SEL - 1))]:-}" ;;              # 纯数字 = 取第几个
        esac
    fi
fi

bash "$HERE/run.command" --exec ${CASE:+"$CASE"}
RC=$?

if [ "$DOUBLE_CLICK" = 1 ]; then
    echo
    printf "  按回车关闭…"
    read -r _
fi
exit $RC
