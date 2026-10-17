#!/bin/bash
# 安装开机自启 —— 把 mongod 注册成 LaunchAgent，登录时自动起库
#
#   双击本文件，或 bash .FrameWork/Database/安装开机自启.command
#
# 装到 ~/Library/LaunchAgents/com.sekia.mongod.plist，由 launchd 直接拉起 mongod。
# ⚠️ 本机的「开机自启」= 登录自启（用户级 LaunchAgent，不需要 sudo，也不会在登录界面之前跑）；
#    想在开机、登录之前就跑得用 /Library/LaunchDaemons（要 root），便携版单机库没必要。
#
# 口径与 启动数据库.command 一致：端口 27017、dbpath Database/db、日志 Logs/mongod.log。
# ⚠️ plist 里的路径按本脚本所在位置现算（不写死绝对路径）—— 仓库换目录后重装一次即可，
#    避免出现「仓库副本是新的、装的那份还是旧路径」这种（com.rohspd.panel 就踩过）。
# ⚠️ KeepAlive 必须是 false：为 true 的话 launchd 会在 关闭数据库.command 杀掉 mongod 后立刻把它拉起来。

LABEL="com.sekia.mongod"
MONGO_PORT=27017
HERE="$(cd "$(dirname "$0")" && pwd)"
MONGOD="$HERE/bin/mongod"
MONGO_DBPATH="$HERE/db"
LOGSDIR="$(cd "$HERE/.." && pwd)/Logs"
MONGO_LOG="$LOGSDIR/mongod.log"
PLIST="$HOME/Library/LaunchAgents/$LABEL.plist"
DOMAIN="gui/$(id -u)"

printf '\033]0;安装开机自启\007'

pause() {
    if [ -t 0 ]; then
        printf "\n按任意键关闭…"
        read -n 1 -s -r _
        echo
    fi
}

if [ ! -x "$MONGOD" ]; then
    echo "[ERROR] 找不到 mongod: $MONGOD"
    echo "        把 MongoDB Community Server (macOS arm64) zip 里的 bin/ 放到 Database/bin/ 下。"
    pause
    exit 1
fi

[ -d "$MONGO_DBPATH" ] || mkdir -p "$MONGO_DBPATH"
[ -d "$LOGSDIR" ] || mkdir -p "$LOGSDIR"
mkdir -p "$HOME/Library/LaunchAgents"

cat > "$PLIST" <<PLIST_EOF
<?xml version="1.0" encoding="UTF-8"?>
<!DOCTYPE plist PUBLIC "-//Apple//DTD PLIST 1.0//EN" "http://www.apple.com/DTDs/PropertyList-1.0.dtd">
<plist version="1.0">
<dict>
  <key>Label</key>
  <string>$LABEL</string>

  <key>ProgramArguments</key>
  <array>
    <string>$MONGOD</string>
    <string>--dbpath</string>
    <string>$MONGO_DBPATH</string>
    <string>--port</string>
    <string>$MONGO_PORT</string>
    <string>--logpath</string>
    <string>$MONGO_LOG</string>
    <string>--logappend</string>
  </array>

  <key>WorkingDirectory</key>
  <string>$HERE</string>

  <key>RunAtLoad</key>
  <true/>

  <key>KeepAlive</key>
  <false/>

  <key>ProcessType</key>
  <string>Background</string>

  <key>StandardOutPath</key>
  <string>$LOGSDIR/mongod.launchd.out.log</string>
  <key>StandardErrorPath</key>
  <string>$LOGSDIR/mongod.launchd.err.log</string>
</dict>
</plist>
PLIST_EOF

if ! plutil -lint "$PLIST" >/dev/null 2>&1; then
    echo "[ERROR] 生成的 plist 语法校验不通过："
    plutil -lint "$PLIST"
    pause
    exit 1
fi

echo "[1/3] 已写入 $PLIST"

launchctl bootout "$DOMAIN/$LABEL" >/dev/null 2>&1
launchctl bootout "$DOMAIN" "$PLIST" >/dev/null 2>&1
echo "[2/3] 已清掉同名旧任务（若之前装过）"

# 端口已被「手工起的」实例占着就先优雅停掉 —— 否则 launchd 那份 mongod 会因 dbpath 被锁而退出，
# 装完看着像失败（实际是那份没起来、手工那份还在）。
OLDPID="$(lsof -nP -iTCP:"$MONGO_PORT" -sTCP:LISTEN -t 2>/dev/null | head -1)"
if [ -n "$OLDPID" ]; then
    echo "      端口 $MONGO_PORT 已被 pid $OLDPID 占用，先优雅停掉，交给 launchd 接管"
    kill "$OLDPID" 2>/dev/null
    W=0
    while lsof -nP -iTCP:"$MONGO_PORT" -sTCP:LISTEN -t >/dev/null 2>&1; do
        W=$((W+1))
        if [ "$W" -ge 5 ]; then
            kill -9 "$OLDPID" 2>/dev/null
            break
        fi
        sleep 1
    done
fi

if ! launchctl bootstrap "$DOMAIN" "$PLIST" >/dev/null 2>&1; then
    if ! launchctl load -w "$PLIST" >/dev/null 2>&1; then
        echo "[ERROR] 加载失败，手工看：launchctl bootstrap $DOMAIN $PLIST"
        pause
        exit 1
    fi
fi

W=0
while [ "$W" -lt 30 ]; do
    if lsof -nP -iTCP:"$MONGO_PORT" -sTCP:LISTEN -t >/dev/null 2>&1; then
        echo "[3/3] 已生效，mongod 正在监听端口 $MONGO_PORT (pid $(lsof -nP -iTCP:"$MONGO_PORT" -sTCP:LISTEN -t | head -1))"
        echo
        echo "以后每次登录自动起库；停库仍用 关闭数据库.command。"
        echo "撤销跑 卸载开机自启.command。"
        pause
        exit 0
    fi
    W=$((W+1))
    sleep 1
done

echo "[WARN] 任务已装上，但 30s 内 $MONGO_PORT 没监听"
echo "       看 $LOGSDIR/mongod.launchd.err.log 与 $MONGO_LOG 末尾"
pause
exit 1
