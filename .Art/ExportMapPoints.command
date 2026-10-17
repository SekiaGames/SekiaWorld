#!/bin/bash
# 地图布点批量导出（SekiaArt_Map）—— 遍历「布点列表」里的每一张图，逐个导出
#
#   双击本文件                              导出全部地图的布点
#   bash .Art/ExportMapPoints.command       同上
#   bash .Art/ExportMapPoints.command prt_1 只跑名字里含 prt_1 的（可给多个关键字）
#
# 「布点列表」= SekiaArt_Map/Demos/MapPoints/*.tscn，文件名 = 地图名
#   （工具自己的 Demos/MapPoints/MapPointsExporter.tscn 不算）
#
# 每张图起一个 Godot headless 进程直接跑那张布点场景 —— 导出脚本就挂在场景的 MapPoints 节点上
# （Demos/MapPoints/MapPointsExporter.cs），编辑器里按 F6 是同一个效果。
#   ⚠️ 布点场景里挂着地图实例（Root，给人眼看位置）⇒ 每张图要实例化一次地图，约 30 秒/图。
#   产物 → .FrameWork/Config/MapPoints_BytesExport/{c,cs,s}/（服务端）
#          SekiaWorld/Bundles/MapPoints/{c,cs,s}/（客户端）
#
# 导航网格烘焙是另一件事，见 .Art/ExportNav.command。
#
# Godot 路径探测顺序：$GODOT → /Applications → ~/Applications → PATH。
# 探测不对时显式指定：
#   GODOT=/path/to/Godot.app/Contents/MacOS/Godot bash .Art/ExportMapPoints.command

HERE="$(cd "$(dirname "$0")" && pwd)"
PROJ_NAME="SekiaArt_Map"
PROJ="$HERE/$PROJ_NAME"
REPO="$(cd "$HERE/.." && pwd)"
FW="$REPO/.FrameWork"

cd "$HERE" || exit 1
printf '\033]0;MapPoints Export\007'

pause() {
    if [ -t 0 ]; then
        printf "\n按回车键关闭…"
        read -r _
        echo
    fi
}

if [ ! -f "$PROJ/project.godot" ]; then
    echo "[ERROR] 找不到工程：$PROJ"
    pause
    exit 1
fi

if [ ! -d "$FW" ]; then
    echo "[ERROR] 找不到框架层：$FW"
    pause
    exit 1
fi

# --- 找 Godot ---
GODOT_BIN=""
if [ -n "$GODOT" ] && [ -x "$GODOT" ]; then
    GODOT_BIN="$GODOT"
else
    shopt -s nullglob
    for c in /Applications/Godot*.app/Contents/MacOS/Godot \
             "$HOME"/Applications/Godot*.app/Contents/MacOS/Godot; do
        if [ -x "$c" ]; then
            GODOT_BIN="$c"
            break
        fi
    done
    shopt -u nullglob
fi
if [ -z "$GODOT_BIN" ]; then
    GODOT_BIN="$(command -v godot 2>/dev/null || command -v godot4 2>/dev/null)"
fi

if [ -z "$GODOT_BIN" ]; then
    echo "找不到 Godot。"
    echo "把 Godot.app 放进 /Applications 或 ~/Applications，或显式指定："
    echo "  GODOT=/path/to/Godot.app/Contents/MacOS/Godot bash \"$HERE/ExportMapPoints.command\""
    pause
    exit 1
fi

# --- 找 dotnet（布点导出脚本是 C#，要编译）---
DOTNET=""
for c in "$(command -v dotnet 2>/dev/null)" \
         /usr/local/share/dotnet/dotnet \
         /opt/homebrew/bin/dotnet \
         "$HOME/.dotnet/dotnet"; do
    if [ -n "$c" ] && [ -x "$c" ]; then
        DOTNET="$c"
        break
    fi
done

if [ -z "$DOTNET" ]; then
    echo "[ERROR] 找不到 dotnet。装 .NET SDK 8.0：brew install --cask dotnet-sdk"
    pause
    exit 1
fi

echo "Godot : $GODOT_BIN"
echo "dotnet: $DOTNET"
echo "工程  : $PROJ"

# --- 编译（保证 headless 用的是最新产物）---
echo
echo "=== 编译 SekiaArt_Map（MapPointsExporter 等） ==="
"$DOTNET" build "$PROJ/$PROJ_NAME.csproj" -c Debug -v q --nologo
if [ $? -ne 0 ]; then
    echo "[ERROR] $PROJ_NAME 编译失败"
    pause
    exit 1
fi

# --- 收集布点场景（Demos/MapPoints/*.tscn；文件名 = 地图名）---
shopt -s nullglob
points=()
for f in "$PROJ/Demos/MapPoints"/*.tscn; do
    base=$(basename "$f"); base=${base%.*}
    [ "$base" = "MapPointsExporter" ] && continue
    points+=("$base")
done
shopt -u nullglob

if [ "${#points[@]}" -gt 0 ]; then
    sorted=()
    while IFS= read -r line; do
        sorted+=("$line")
    done < <(printf '%s\n' "${points[@]}" | LC_ALL=C sort -u)
    points=("${sorted[@]}")
fi

# --- 可选：按关键字过滤 ---
if [ "$#" -gt 0 ] && [ "${#points[@]}" -gt 0 ]; then
    kept=()
    for p in "${points[@]}"; do
        for kw in "$@"; do
            case "$p" in
                *"$kw"*)
                    kept+=("$p")
                    break
                    ;;
            esac
        done
    done
    points=("${kept[@]}")
fi

total="${#points[@]}"
if [ "$total" -eq 0 ]; then
    echo
    echo "布点列表为空（$PROJ/Demos/MapPoints 下没有 .tscn）"
    [ "$#" -gt 0 ] && echo "（也可能是关键字 $* 没匹配上）"
    pause
    exit 2
fi

echo
echo "==== 共 $total 张图的布点 ===="
for p in "${points[@]}"; do
    echo "  - $p"
done
echo

# --- 逐张导出 ---
ok=0
fail=0
failed=()
idx=0

for p in "${points[@]}"; do
    idx=$((idx + 1))
    echo
    echo "==== [$idx/$total] 布点 $p ===="
    # 布点导出脚本就挂在各图 MapPoints 节点上 ⇒ 直接跑那张场景即导出该图。
    "$GODOT_BIN" --headless --path "$PROJ" "res://Demos/MapPoints/$p.tscn"
    rc=$?

    if [ "$rc" -eq 0 ]; then
        ok=$((ok + 1))
        echo "---- [$idx/$total] 布点 $p 完成"
    else
        fail=$((fail + 1))
        failed+=("$p")
        echo "---- [$idx/$total] 布点 $p 失败（退出码 ${rc}）"
    fi
done

echo
echo "==== 汇总：布点成功 ${ok} / 失败 ${fail}（共 ${total}） ===="
if [ "$fail" -gt 0 ]; then
    echo "失败的图："
    for m in "${failed[@]}"; do
        echo "  - $m"
    done
fi
echo "布点产物：$FW/Config/MapPoints_BytesExport/{c,cs,s}/（服务端）"
echo "          $REPO/SekiaWorld/Bundles/MapPoints/{c,cs,s}/（客户端）"

pause
[ "$fail" -eq 0 ] && exit 0
exit 1
