#!/bin/bash
# 测试用例入口（Windows Git Bash 与 macOS 通用）
#   bash .FrameWork/TestCase/run.sh            跑默认用例（launch）
#   bash .FrameWork/TestCase/run.sh launch     跑指定用例
#   FULL=1 bash .FrameWork/TestCase/run.sh     连 Tools/Server 一起编（改了那两个工程时才要）
# 退出码：0 = PASS，1 = FAIL，2 = 用例不存在
#
# 平台差异（Godot 路径 / 仓库根 / dotnet 输出语言 / 喂 stdin 的办法）全在 lib/common.sh 里分平台处理，
# 本文件与 cases/*.sh 平台无关。要覆盖默认值：GODOT=... REPO=... DOTNET=... bash run.sh
# macOS 上给人用可以双击 run.command（对应 Windows 的 run.bat）。

HERE="$(cd "$(dirname "$0")" && pwd)"
CASE="${1:-launch}"

if [ ! -f "$HERE/cases/$CASE.sh" ]; then
    echo "没有这个用例：$CASE"
    echo "现有用例：$(ls "$HERE/cases" 2>/dev/null | sed 's/\.sh$//' | tr '\n' ' ')"
    exit 2
fi

source "$HERE/lib/common.sh"
source "$HERE/cases/$CASE.sh"
