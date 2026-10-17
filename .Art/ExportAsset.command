#!/bin/bash
# 美术工程打包 —— 把 .Art/ 下每个 Godot 工程导出成 .pck
#
#   双击本文件                             弹菜单选工程
#   bash .Art/ExportAsset.command                 同上
#   bash .Art/ExportAsset.command SekiaArt_Map    只导指定工程（名字 = 含 project.godot 的那个目录名）
#   bash .Art/ExportAsset.command all             导出全部
#   bash .Art/ExportAsset.command --list          只列出「名字|相对路径」（一行一个，给脚本用）
#
# 判定「一个美术工程」的唯一标准：**目录里有 project.godot**（不认名字、不认登记表）。
# 发现是递归的 ⇒ .Art/ 下新建任何层级的 Godot 工程都会被自动带上，不用改本脚本。
#
# 产物落 .Art/.pck/<工程目录名>.pck（不入库，见 .gitignore）。
#
# 退出码：0 = 全部成功，1 = 有工程失败 / 找不到 Godot，2 = 没找到任何美术工程
#         （--list 时：0 = 有工程，2 = 一个都没有）

set -o pipefail

HERE="$(cd "$(dirname "$0")" && pwd)"
cd "$HERE" || exit 1
printf '\033]0;Art Pack\007'

ART="$HERE"
OUT="$ART/.pck"
# 原来那台 Windows 机器上的默认位置，探测时排在 $GODOT 之后、/Applications 之前。
GODOT_DEFAULT="D:/Ghost/GameDev/Godot_v4.7.2-stable_mono_win64/Godot_v4.7.2-stable_mono_win64_console.exe"

FAILED=0
FOUND=0

# 只有「双击」（不带参数）才在结尾停一下等回车；命令行带参数跑完就退出。
PAUSE=0
[ $# -eq 0 ] && PAUSE=1

log()  { echo "$*"; }
ok()   { echo "  ok   $*"; }
bad()  { echo "  FAIL $*"; FAILED=$((FAILED + 1)); }

pause_if_double_click() {
    [ "$PAUSE" -eq 1 ] && [ -t 0 ] && read -r -p "按回车键关闭…" _
    return 0
}

list_projects() {
    find "$ART" -name "project.godot" -not -path "*/.godot/*" | sort | while IFS= read -r f; do
        d="$(dirname "$f")"
        echo "$(basename "$d")|${d#"$ART"/}"
    done
}

if [ "$1" = "--list" ]; then
    out="$(list_projects)"
    [ -z "$out" ] && exit 2
    printf '%s\n' "$out"
    exit 0
fi

# 修「编辑器文件系统缓存里 deps 记成空」的资源。
#
# Godot 只在文件 mtime 变化时才重算依赖（editor_file_system.cpp:1327「mtime 没变 ⇒ 沿用
# 旧缓存」），--import 和打开编辑器都只做「重扫」不做「重算」。于是在编辑器外写进去的资源
# （Agent 生成 .material / .tscn 之类）一旦被记成空 deps 就一直是空的；而 resources / scenes
# 模式的导出是**跟着缓存走**的（_export_find_dependencies 读 EditorFileSystem::get_file_deps）
# ⇒ 漏掉贴图、网格、子场景，运行时才炸。
#
# 判据：缓存里该行 deps 字段为空（行以 :: 收尾），但文件里确实有引用（文本资源看
# [ext_resource]，二进制资源看 res:// 串）⇒ 缓存是错的。只 touch 这些文件让 mtime 变化，
# 随后 --import 就会重算；不删整份缓存（全量重扫代价太大）。
fix_stale_deps() {
    local proj_dir="$1"
    local cache path full n=0
    cache="$(ls "$proj_dir"/.godot/editor/filesystem_cache* 2>/dev/null | head -1)"
    [ -z "$cache" ] && return 0

    while IFS= read -r path; do
        [ -z "$path" ] && continue
        full="$proj_dir/${path#res://}"
        [ -f "$full" ] || continue
        case "$path" in
            *.tscn | *.tres | *.escn | *.prefab | *.anim | *.controller)
                grep -qa "\[ext_resource" "$full" || continue
                ;;
            *.material | *.mat | *.res)
                grep -qa "res://" "$full" || continue
                ;;
            *)
                continue
                ;;
        esac
        touch "$full"
        n=$((n + 1))
    done < <(awk '
        /^::res:/ { d = substr($0, 3); sub(/::[0-9]+$/, "", d); next }
        /::$/ { n = $0; sub(/::.*/, "", n); print d n }
    ' "$cache")

    [ "$n" -gt 0 ] && log "  修缓存 deps：touch $n 个资源"
    return 0
}

# --- 找 Godot ---
# ⚠️ 不写死某一台机器的路径：$GODOT → Windows 默认值 → /Applications → ~/Applications → PATH。
GODOT_BIN=""
if [ -n "$GODOT" ] && { [ -x "$GODOT" ] || [ -f "$GODOT" ]; }; then
    GODOT_BIN="$GODOT"
elif [ -f "$GODOT_DEFAULT" ]; then
    GODOT_BIN="$GODOT_DEFAULT"
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
    if [ -z "$GODOT_BIN" ]; then
        GODOT_BIN="$(command -v godot 2>/dev/null || command -v godot4 2>/dev/null)"
    fi
fi

if [ -z "$GODOT_BIN" ]; then
    echo "找不到 Godot（按 \$GODOT → $GODOT_DEFAULT → /Applications → ~/Applications → PATH 都找过）。"
    echo "用环境变量指定，例：GODOT=/path/to/godot bash .Art/ExportAsset.command"
    echo
    pause_if_double_click
    exit 1
fi
GODOT="$GODOT_BIN"

# --- 收集工程 ---
names=()
rels=()
while IFS='|' read -r n r; do
    [ -z "$n" ] && continue
    names+=("$n")
    rels+=("$r")
done < <(list_projects)

if [ "${#names[@]}" -eq 0 ]; then
    echo "在 $ART 下没找到任何美术工程（判定标准：目录里有 project.godot）。"
    echo
    pause_if_double_click
    exit 2
fi

# --- 重名预检：工程名（= 目录名）即产物名 ⇒ 重名会互相覆盖，必须先拦下 ---
dup="$(printf '%s\n' "${names[@]}" | sort | uniq -d)"
if [ -n "$dup" ]; then
    printf '%s\n' "$dup" | while IFS= read -r d; do
        echo "美术工程重名：$d"
        i=0
        while [ "$i" -lt "${#names[@]}" ]; do
            [ "${names[$i]}" = "$d" ] && echo "  ${rels[$i]}"
            i=$((i + 1))
        done
        echo "两个工程会产出同一个 .pck，请把目录名改得互不相同。"
    done
    echo
    pause_if_double_click
    exit 1
fi

# --- 选工程 ---
# 无参数 = 弹菜单；all / A = 全部；其它 = 工程名
TARGET=""
case "$1" in
    "" | all | ALL | a | A) TARGET="" ;;
    *) TARGET="$1" ;;
esac

if [ -z "$1" ]; then
    while :; do
        echo
        echo "美术工程（含 project.godot 的目录）："
        i=0
        while [ "$i" -lt "${#names[@]}" ]; do
            printf '  %2d. %s   [%s]\n' "$((i + 1))" "${names[$i]}" "${rels[$i]}"
            i=$((i + 1))
        done
        echo
        echo "   A. 全部导出"
        echo "   Q. 退出"
        echo
        read -r -p "  编号 / 工程名 / A / Q： " sel

        case "$sel" in
            "" | a | A)
                TARGET=""
                break
                ;;
            q | Q)
                exit 0
                ;;
            *[!0-9]*)
                TARGET="$sel"
                ;;
            *)
                i=$((10#$sel - 1))
                if [ "$i" -ge 0 ] && [ "$i" -lt "${#names[@]}" ]; then
                    TARGET="${names[$i]}"
                else
                    TARGET="$sel"
                fi
                ;;
        esac

        found=0
        [ -z "$TARGET" ] && found=1
        i=0
        while [ "$i" -lt "${#names[@]}" ]; do
            [ "${names[$i]}" = "$TARGET" ] && found=1
            i=$((i + 1))
        done
        if [ "$found" -eq 0 ]; then
            echo "  没有名为 $TARGET 的工程。"
            TARGET=""
            continue
        fi
        break
    done
fi

# --- 单个工程：导入 → 导出 ---
pack_one() {
    local name="$1"
    local rel="$2"
    local proj_dir="$ART/$rel"
    local presets="$proj_dir/export_presets.cfg"
    local preset pck logf size

    log "[$rel] 打包"

    if [ ! -f "$presets" ]; then
        bad "$rel 没有 export_presets.cfg —— 该工程导不出 pck"
        return
    fi

    # preset 名从 export_presets.cfg 里读（取第一个 name=），不硬编码
    preset="$(grep -m1 -E '^name=' "$presets" | sed -E 's/^name="(.*)"$/\1/')"
    if [ -z "$preset" ]; then
        bad "$rel 的 export_presets.cfg 里读不到 preset 名"
        return
    fi

    pck="$OUT/$name.pck"
    logf="$OUT/$name.log"
    mkdir -p "$OUT"

    # 0. 修缓存里 deps 记成空的资源（编辑器外写入的资源会踩，见函数注释）
    fix_stale_deps "$proj_dir"

    # 1. 导入资源（改了 .tscn / .gltf 都要重跑；幂等）
    if ! "$GODOT" --headless --path "$proj_dir" --import > "$logf" 2>&1; then
        bad "$rel 资源导入失败 —— 详见 $logf"
        return
    fi

    # 2. 导出 pck（--export-pack 只导数据、不需要导出模板）
    if ! "$GODOT" --headless --path "$proj_dir" \
            --export-pack "$preset" "$pck" >> "$logf" 2>&1; then
        bad "$rel 导出失败（preset \"$preset\"）—— 详见 $logf"
        rm -f "$pck"   # 失败会留下残缺 pck，删掉免得被当成有效产出
        return
    fi

    if [ ! -f "$pck" ]; then
        bad "$rel 导出命令返回成功但没产出 $pck —— 详见 $logf"
        return
    fi

    size=$(du -h "$pck" | cut -f1 | tr -d ' ')
    ok "$rel → .pck/$name.pck（${size}，preset \"$preset\"）"
    rm -f "$logf"
}

# --- 打包 ---
echo
if [ -z "$TARGET" ]; then
    echo "==== 打包全部美术工程 ===="
else
    echo "==== 打包 $TARGET ===="
fi
echo

i=0
while [ "$i" -lt "${#names[@]}" ]; do
    name="${names[$i]}"
    rel="${rels[$i]}"
    i=$((i + 1))
    if [ -n "$TARGET" ] && [ "$name" != "$TARGET" ]; then
        continue
    fi
    FOUND=$((FOUND + 1))
    pack_one "$name" "$rel"
done

if [ "$FOUND" -eq 0 ]; then
    echo "没有名为 $TARGET 的美术工程（判定标准：目录里有 project.godot）"
    echo "现有工程："
    i=0
    while [ "$i" -lt "${#names[@]}" ]; do
        echo "  - ${rels[$i]}"
        i=$((i + 1))
    done
    echo
    pause_if_double_click
    exit 2
fi

echo
if [ "$FAILED" -eq 0 ]; then
    echo "PASS  $FOUND 个美术工程全部导出成功 → $OUT"
    echo
    pause_if_double_click
    exit 0
fi

echo "FAIL  $FAILED / $FOUND 个工程导出失败"
echo
pause_if_double_click
exit 1
