#!/bin/bash
# 配置导出 —— 双击本文件，或 bash .FrameWork/Launch01_ExportConfig.command
#
# 三步：BTImport（有 json 才跑）→ ExcelExporter → Proto2CS，
# 统一由 dotnet 跑 Bin/Tool.dll。工具内部的相对路径（../Config/…、../../Scripts/…）
# 都以 Bin/ 为基准，所以每次都在 Bin/ 里执行。

HERE="$(cd "$(dirname "$0")" && pwd)"
CSPROJ="$HERE/Tools/ConfigTools/ConfigTools.csproj"
BINDIR="$HERE/Bin"
BTJSON="$HERE/Config/Excel_TestJsonExport/c/BTConfig.txt"

cd "$HERE" || exit 1
printf '\033]0;Export Config\007'

# --- 找 dotnet ---
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
    echo "[ERROR] 找不到 dotnet，先装 .NET SDK 8.0（brew install --cask dotnet-sdk）"
    [ -t 0 ] && read -n 1 -s -r -p "按任意键关闭…"
    echo
    exit 1
fi

pause() {
    if [ -t 0 ]; then
        printf "\n按任意键关闭…"
        read -n 1 -s -r _
        echo
    fi
}

# 在 Bin/ 里跑 Tool.dll <工具名>；返回非零 = 该步失败
run() {
    echo
    echo "=== Run $1 ==="
    cd "$BINDIR" || return 1
    "$DOTNET" Tool.dll "$1"
    local rc=$?
    cd "$HERE" || true
    if [ "$rc" -ne 0 ]; then
        echo "[ERROR] $1 failed"
        return 1
    fi
    return 0
}

echo "dotnet: $DOTNET"

echo "=== Build Tool ==="
"$DOTNET" build "$CSPROJ" -c Release -v q --nologo
if [ $? -ne 0 ]; then
    echo "[ERROR] Tool build failed"
    pause
    exit 1
fi

if [ -f "$BTJSON" ]; then
    run BTImport || { pause; exit 1; }
else
    echo "=== Skip BTImport: BTConfig json not found ==="
fi

run ExcelExporter || { pause; exit 1; }
run Proto2CS || { pause; exit 1; }

echo
echo "=== Done ==="
pause
exit 0
