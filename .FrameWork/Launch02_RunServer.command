#!/bin/bash
# 起服务端 —— 双击本文件，或 bash .FrameWork/Launch02_RunServer.command [StartConfig]
#
# 三步：
#   ① 端口 27017 没在听就后台起 mongod（Database/bin/mongod，日志 Logs/mongod.log；已在跑就跳过）
#   ② dotnet build Server/App/Server.App.csproj -c Release（App 会把 Loader/Model/Core/ThirdParty 一起带出来，
#      并由它自带的 BuildHotfixLayers 补编 Server.Hotfix ⇒ 产出 Bin/Hotfix.dll）
#   ③ cd Bin 起 dotnet App.dll --AppType=Server --Process=1 --StartConfig=StartConfig/<名字>
#
# StartConfig 默认 Localhost，第 1 个参数或 STARTCONFIG=... 可覆盖。
# ⚠️ 停服用 Ctrl+C，别直接关窗口 —— 关窗口不给进程发 SIGINT，端口可能还占着。
# ⚠️ mongod 不能用 --fork（macOS 上报 fork+exec via --fork is incompatible），
#    也不能只 nohup（mongod 自己把 SIGHUP 当优雅关闭请求）⇒ 与 Database/启动数据库.command 同一招：
#    用系统自带的 perl 建新会话（setsid）彻底脱离控制终端，再 disown。

HERE="$(cd "$(dirname "$0")" && pwd)"
APPPROJ="$HERE/Server/App/Server.App.csproj"
BINDIR="$HERE/Bin"
MONGO_PORT=27017
MONGOD="$HERE/Database/bin/mongod"
MONGO_DBPATH="$HERE/Database/db"
MONGO_LOG="$HERE/Logs/mongod.log"
STARTCONFIG="${1:-${STARTCONFIG:-Localhost}}"

cd "$HERE" || exit 1
printf '\033]0;Run Server\007'

pause() {
    if [ -t 0 ]; then
        printf "\n按任意键关闭…"
        read -n 1 -s -r _
        echo
    fi
}

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
    pause
    exit 1
fi

echo "dotnet: $DOTNET"

# --- ① 数据库 ---
mongo_pid() {
    lsof -nP -iTCP:"$MONGO_PORT" -sTCP:LISTEN -t 2>/dev/null | head -1
}

if [ -n "$(mongo_pid)" ]; then
    echo "[1/3] MongoDB 已在监听端口 $MONGO_PORT (pid $(mongo_pid))，跳过启动"
else
    if [ ! -x "$MONGOD" ]; then
        echo "[ERROR] 找不到 mongod: $MONGOD"
        echo "        把 MongoDB Community Server (macOS arm64) zip 里的 bin/ 放到 Database/bin/ 下。"
        pause
        exit 1
    fi

    [ -d "$MONGO_DBPATH" ] || mkdir -p "$MONGO_DBPATH"
    [ -d "$HERE/Logs" ] || mkdir -p "$HERE/Logs"

    echo "[1/3] 启动 MongoDB，端口 $MONGO_PORT"
    echo "      数据目录: $MONGO_DBPATH"
    echo "      日志文件: $MONGO_LOG"

    STARTUP_OUT="$(mktemp -t mongod_startup)"
    nohup perl -MPOSIX -e 'POSIX::setsid() >= 0 or print STDERR "setsid failed: $!\n"; exec @ARGV' \
        "$MONGOD" --dbpath "$MONGO_DBPATH" --port "$MONGO_PORT" --logpath "$MONGO_LOG" --logappend \
        </dev/null >"$STARTUP_OUT" 2>&1 &
    disown 2>/dev/null || true

    W=0
    while [ -z "$(mongo_pid)" ]; do
        W=$((W+1))
        if [ "$W" -ge 30 ]; then
            echo "[ERROR] 等了 30s，mongod 仍未在端口 $MONGO_PORT 监听"
            echo "--- mongod 启动输出 ---"
            cat "$STARTUP_OUT" 2>/dev/null
            echo "--- $MONGO_LOG 末尾 ---"
            tail -n 20 "$MONGO_LOG" 2>/dev/null
            rm -f "$STARTUP_OUT"
            pause
            exit 1
        fi
        sleep 1
    done
    rm -f "$STARTUP_OUT"
    echo "      MongoDB 就绪 (pid $(mongo_pid))"
fi

# --- ② 编服务端 ---
if [ ! -f "$APPPROJ" ]; then
    echo "[ERROR] 找不到工程：$APPPROJ"
    pause
    exit 1
fi

echo
echo "[2/3] dotnet build Server/App/Server.App.csproj -c Release"
"$DOTNET" build "$APPPROJ" -c Release -v q --nologo
if [ $? -ne 0 ]; then
    echo "[ERROR] 服务端编译失败，先修上面的报错再跑"
    pause
    exit 1
fi

# --- ③ 起服 ---
echo
echo "[3/3] dotnet App.dll --AppType=Server --Process=1 --StartConfig=StartConfig/$STARTCONFIG"
echo "      日志写在 Logs/，Info 以上同时回显到本窗口。"
echo "      停服用 Ctrl+C。"
echo

cd "$BINDIR" || { pause; exit 1; }
"$DOTNET" App.dll --AppType=Server --Process=1 --StartConfig="StartConfig/$STARTCONFIG"
RC=$?
cd "$HERE" || true

if [ "$RC" -eq 0 ]; then
    exit 0
fi

echo
echo "[3/3] 服务端退出，退出码 $RC"
echo "      Ctrl+C / 被杀掉时非 0 是正常的；只有自己挂掉才需要去 Logs/ 里查。"
pause
exit 1
