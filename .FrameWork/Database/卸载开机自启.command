#!/bin/bash
# 卸载开机自启 —— 撤掉 LaunchAgent（只动 launchd 任务，不碰数据库、不删数据）
#
#   双击本文件，或 bash .FrameWork/Database/卸载开机自启.command

LABEL="com.sekia.mongod"
PLIST="$HOME/Library/LaunchAgents/$LABEL.plist"
DOMAIN="gui/$(id -u)"

printf '\033]0;卸载开机自启\007'

pause() {
    if [ -t 0 ]; then
        printf "\n按任意键关闭…"
        read -n 1 -s -r _
        echo
    fi
}

if [ ! -f "$PLIST" ]; then
    echo "[SKIP] 没装过：$PLIST 不存在"
    pause
    exit 0
fi

launchctl bootout "$DOMAIN/$LABEL" >/dev/null 2>&1
launchctl unload -w "$PLIST" >/dev/null 2>&1
rm -f "$PLIST"

if [ -f "$PLIST" ]; then
    echo "[ERROR] 删不掉 $PLIST，检查权限"
    pause
    exit 1
fi

echo "[OK] 已卸载开机自启：$PLIST 已删除"
echo "     launchd 会把由它启动的 mongod 一并停掉；"
echo "     若还有残留实例，用 关闭数据库.command 收尾。"
pause
exit 0
