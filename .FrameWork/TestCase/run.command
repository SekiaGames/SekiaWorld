#!/bin/bash
# macOS 上给人用的入口（对应 Windows 的 run.bat）。
#
#   Finder 里双击本文件       → 列出现有用例，输入编号或用例名回车即可（直接回车 = 默认用例）
#   终端里 ./run.command xxx  → 有参数就不弹菜单，直接跑 xxx
#   （也可以 bash .FrameWork/TestCase/run.command xxx）
#
# 跑完停在窗口里等你看 PASS / FAIL；退出码与 run.sh 一致：0 = PASS，1 = FAIL，2 = 用例不存在。
# Agent 走 run.sh，别走本文件（本文件会等回车）。
#
# ⚠️ 首次双击可能被 Gatekeeper 拦（"无法打开，因为它来自身份不明的开发者"）——
#    终端里跑一次 `chmod +x run.command` 即可；本文件已在仓库里带了可执行位。

HERE="$(cd "$(dirname "$0")" && pwd)"
cd "$HERE" || exit 1

# 双击起的 shell PATH 很干净（只有 /usr/bin:/bin:/usr/sbin:/sbin），
# dotnet / godot 都不在里面 —— 补上常见安装位置，省得还要手改 PATH 或环境变量。
export PATH="$PATH:/usr/local/share/dotnet:$HOME/.dotnet:/opt/homebrew/bin:/usr/local/bin"

CASE="$*"
if [ -z "$CASE" ]; then
    echo
    echo "  可选的测试用例："
    names=()
    for f in "$HERE"/cases/*.sh; do
        [ -e "$f" ] || continue
        names+=("$(basename "$f" .sh)")
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

bash "$HERE/run.sh" $CASE
RC=$?

echo
printf "  按回车关闭…"
read -r _
exit $RC
