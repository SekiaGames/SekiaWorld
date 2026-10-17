#!/bin/bash
# 导航网格批量烘焙（SekiaArt_Map）—— 遍历「地图列表」里的每一张地图，逐个导出 + 烘焙寻路
#
#   双击本文件                                        烘焙全部地图
#   bash .Art/ExportNav.command          同上
#   bash .Art/ExportNav.command prt_1    只跑路径里含 prt_1 的（可给多个关键字）
#
# 「地图列表」= Demos/NavExport/NavExporter.cs 下拉框扫的那两处来源：
#   res://Bundles/Scenes/*.tscn|*.scn      （平铺）
#   res://Scene/<地图名>/*.tscn|*.scn      （RO 美术工程，每张图一个子目录）
# 地图名 = 场景文件名去扩展名（与 NavExporter 一致）：
#   Scene/prt_1/Sceneprt_1.scn  →  Sceneprt_1
#
# 每张地图起一个 Godot headless 进程跑 `--nav-scene <res://…>`（带烘焙）：
#   中间产物 → SekiaArt_Map/.temp/RecastInput/<地图名>/
#   烘焙产物 → .FrameWork/Config/RecastOutput/<地图名>   （服务端 RecastFileReader 按此名读）
#
# 布点导出是另一件事，见 .Art/ExportMapPoints.command。
#
# Godot 路径探测顺序：$GODOT → /Applications → ~/Applications → PATH。
# 探测不对时显式指定：
#   GODOT=/path/to/Godot.app/Contents/MacOS/Godot bash .Art/ExportNav.command

HERE="$(cd "$(dirname "$0")" && pwd)"
PROJ_NAME="SekiaArt_Map"
PROJ="$HERE/$PROJ_NAME"
REPO="$(cd "$HERE/.." && pwd)"
FW="$REPO/.FrameWork"

cd "$HERE" || exit 1
printf '\033]0;Nav Export\007'

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
    echo "  GODOT=/path/to/Godot.app/Contents/MacOS/Godot bash \"$HERE/ExportNav.command\""
    pause
    exit 1
fi

# --- 找 dotnet（烘焙器要它）---
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
    echo "[ERROR] 找不到 dotnet（烘焙器要它）。装 .NET SDK 8.0：brew install --cask dotnet-sdk"
    pause
    exit 1
fi

# 让 Godot 子进程也能通过 PATH 找到 dotnet（它内部用 OS.Execute("dotnet", …) 跑烘焙器）
export PATH="$(dirname "$DOTNET"):$PATH"

echo "Godot : $GODOT_BIN"
echo "dotnet: $DOTNET"
echo "工程  : $PROJ"

# --- 编译（保证 headless 用的是最新产物）---
echo
echo "=== 编译 SekiaArt_Map（NavExporter 等） ==="
"$DOTNET" build "$PROJ/$PROJ_NAME.csproj" -c Debug -v q --nologo
if [ $? -ne 0 ]; then
    echo "[ERROR] $PROJ_NAME 编译失败"
    pause
    exit 1
fi

echo
echo "=== 编译烘焙器 RecastNavBaker ==="
"$DOTNET" build "$FW/Tools/RecastNavBaker/RecastNavBaker.csproj" -c Release -v q --nologo
if [ $? -ne 0 ]; then
    echo "[ERROR] 烘焙器编译失败"
    pause
    exit 1
fi

# --- 收集地图列表（与 NavExporter 的扫描规则一致）---
shopt -s nullglob
scenes=()
for f in "$PROJ/Bundles/Scenes"/*.tscn "$PROJ/Bundles/Scenes"/*.scn; do
    scenes+=("res://Bundles/Scenes/$(basename "$f")")
done
for f in "$PROJ/Scene"/*/*.tscn "$PROJ/Scene"/*/*.scn; do
    # ⚠️ 每张图的目录里除了地图本体（`Scene<目录名>.tscn`）还可能放着拼场景用的 `part_*.tscn`
    #    （普隆德拉 = `Scene/Prontera/SceneProntera.tscn` + 6 个 `part_*.tscn`）—— 只收地图本体那个文件，
    #    否则会把 part_* 当成独立地图去烘：烘焙器对它们只会空跑/失败，白等一轮（而且中途 Ctrl-C 会留下孤儿进程）。
    dir=$(basename "$(dirname "$f")")
    base=$(basename "$f"); base=${base%.*}
    if [ "$base" != "Scene$dir" ]; then
        continue
    fi
    scenes+=("res://${f#"$PROJ"/}")
done
shopt -u nullglob

if [ "${#scenes[@]}" -gt 0 ]; then
    sorted=()
    while IFS= read -r line; do
        sorted+=("$line")
    done < <(printf '%s\n' "${scenes[@]}" | LC_ALL=C sort -u)
    scenes=("${sorted[@]}")
fi

# --- 可选：按关键字过滤 ---
if [ "$#" -gt 0 ] && [ "${#scenes[@]}" -gt 0 ]; then
    filtered=()
    for s in "${scenes[@]}"; do
        for kw in "$@"; do
            case "$s" in
                *"$kw"*)
                    filtered+=("$s")
                    break
                    ;;
            esac
        done
    done
    scenes=("${filtered[@]}")
fi

total="${#scenes[@]}"
if [ "$total" -eq 0 ]; then
    echo
    echo "地图列表为空（$PROJ/Bundles/Scenes 与 $PROJ/Scene/*/ 下都没有 .tscn / .scn）"
    [ "$#" -gt 0 ] && echo "（也可能是关键字 $* 没匹配上）"
    pause
    exit 2
fi

echo
echo "==== 共 $total 张地图 ===="
for s in "${scenes[@]}"; do
    echo "  - $s"
done
echo

# --- 逐张导出 + 烘焙 ---
ok=0
fail=0
failed=()
idx=0

for s in "${scenes[@]}"; do
    idx=$((idx + 1))
    map="${s##*/}"
    map="${map%.*}"

    echo
    echo "==== [$idx/$total] $map   ($s) ===="
    # ⚠️ 必须**显式给出 NavExport 工具场景**：本工程的 run/main_scene 是
    #    res://Scene/CharacterSelect/SceneCharacterSelect.tscn，不给场景时 Godot 会去跑它、
    #    那里面没有 NavExporter 接 --nav-scene ⇒ 进程干等着不退（看着像卡死）。
    "$GODOT_BIN" --headless --path "$PROJ" "res://Demos/NavExport/NavExport.tscn" -- --nav-scene "$s"
    rc=$?

    if [ "$rc" -eq 0 ]; then
        ok=$((ok + 1))
        echo "---- [$idx/$total] $map 完成"
    else
        fail=$((fail + 1))
        failed+=("$map")
        echo "---- [$idx/$total] $map 失败（退出码 ${rc}）"
    fi
done

echo
echo "==== 汇总：烘焙成功 ${ok} / 失败 ${fail}（共 ${total}） ===="
if [ "$fail" -gt 0 ]; then
    echo "失败的地图："
    for m in "${failed[@]}"; do
        echo "  - $m"
    done
fi
echo "烘焙产物：$FW/Config/RecastOutput/"

pause
[ "$fail" -eq 0 ] && exit 0
exit 1
