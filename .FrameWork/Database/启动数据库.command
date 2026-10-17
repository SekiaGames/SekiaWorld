#!/bin/bash
# 启动数据库 —— 启动数据库.bat 的 macOS 版
#
#   双击本文件，或 bash .FrameWork/Database/启动数据库.command
#
# 口径与 Launch02_RunServer.bat 的第 1 步完全一致：
#   端口 27017、dbpath Database/db、日志 Logs/mongod.log、后台无窗口。
#
# ⚠️ 两条 macOS 特有的坑（8.3.11 实测）：
#   1. mongod 不支持 --fork —— 报 BadValue: fork+exec via --fork is incompatible with macOS。
#   2. nohup 挡不住关窗口 —— mongod 自己接管了 SIGHUP 并把它当优雅关闭请求
#      （日志 SignalHandler → exitCode:0）⇒ 光 nohup 的话关掉 Terminal 窗口会顺手把库停掉。
#   ⇒ 用系统自带的 perl 建新会话（setsid）彻底脱离控制终端，再 disown。

MONGO_PORT=27017
HERE="$(cd "$(dirname "$0")" && pwd)"
MONGOD="$HERE/bin/mongod"
MONGO_DBPATH="$HERE/db"
LOGSDIR="$(cd "$HERE/.." && pwd)/Logs"
MONGO_LOG="$LOGSDIR/mongod.log"

printf '\033]0;启动数据库\007'

pause() {
    if [ -t 0 ]; then
        printf "\n按任意键关闭…"
        read -n 1 -s -r _
        echo
    fi
}

find_pid() {
    lsof -nP -iTCP:"$MONGO_PORT" -sTCP:LISTEN -t 2>/dev/null | head -1
}

port_listening() {
    [ -n "$(find_pid)" ]
}

if [ ! -x "$MONGOD" ]; then
    echo "[ERROR] 找不到 mongod: $MONGOD"
    echo "        把 MongoDB Community Server (macOS arm64) zip 里的 bin/ 放到 Database/bin/ 下。"
    pause
    exit 1
fi

if port_listening; then
    echo "[SKIP] MongoDB 已在监听端口 $MONGO_PORT (pid $(find_pid))"
    pause
    exit 0
fi

[ -d "$MONGO_DBPATH" ] || mkdir -p "$MONGO_DBPATH"
[ -d "$LOGSDIR" ] || mkdir -p "$LOGSDIR"

echo "[1/2] 启动 MongoDB，端口 $MONGO_PORT"
echo "      数据目录: $MONGO_DBPATH"
echo "      日志文件: $MONGO_LOG"
echo "      后台进程，无窗口。"

STARTUP_OUT="$(mktemp -t mongod_startup)"
nohup perl -MPOSIX -e 'POSIX::setsid() >= 0 or print STDERR "setsid failed: $!\n"; exec @ARGV' \
    "$MONGOD" --dbpath "$MONGO_DBPATH" --port "$MONGO_PORT" --logpath "$MONGO_LOG" --logappend \
    </dev/null >"$STARTUP_OUT" 2>&1 &
disown 2>/dev/null || true

W=0
while ! port_listening; do
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
echo "[2/2] MongoDB 就绪，端口 $MONGO_PORT (pid $(find_pid))"
pause
exit 0
