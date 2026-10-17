#!/bin/bash
# 关闭数据库 —— 关闭数据库.bat 的 macOS 版
#
#   双击本文件，或 bash .FrameWork/Database/关闭数据库.command
#
# 按 lsof 取监听 27017 的 PID，只动本项目这一个 mongod；
# 先 kill（SIGTERM，mongod 走完整优雅关闭：flush journal + checkpoint + 释放锁），
# 5s 内没退才 kill -9 兜底。

MONGO_PORT=27017
WAIT_MAX=5

printf '\033]0;关闭数据库\007'

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

wait_gone() {
    local w=0
    while port_listening; do
        w=$((w+1))
        if [ "$w" -ge "$WAIT_MAX" ]; then
            return 1
        fi
        sleep 1
    done
    return 0
}

PID="$(find_pid)"
if [ -z "$PID" ]; then
    echo "[SKIP] MongoDB 没有在监听端口 $MONGO_PORT"
    pause
    exit 0
fi

echo "[1/2] 停止 MongoDB (pid $PID, 端口 $MONGO_PORT)"
kill "$PID" 2>/dev/null

if wait_gone; then
    echo "[2/2] MongoDB 已停止，端口 $MONGO_PORT 已释放"
    pause
    exit 0
fi

echo "      优雅停止未完成，强制结束"
kill -9 "$PID" 2>/dev/null

if wait_gone; then
    echo "[2/2] MongoDB 已停止，端口 $MONGO_PORT 已释放"
    pause
    exit 0
fi

echo "[ERROR] pid $PID 仍在监听端口 $MONGO_PORT"
echo "        再试一次: kill -9 $PID"
pause
exit 1
