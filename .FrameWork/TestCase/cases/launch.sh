#!/bin/bash
# 用例：启动（默认用例）
#   编 Debug → 无头起客户端（Agent 模式）→ 四条硬判据 → 退出
# 判据（前三条 = 原「冒烟」口径，2026-10-02 并入本用例）：
#   ① scene create 齐（ClientServer/Server 5 条、Client 1 条）
#   ② 无 ERROR   ③ 无「配置缺失」   ④ 收到「登录界面」的菜单广播
# 账号密码登录及其之后由客户自己保证，本用例不跑。

CASE_NAME="启动"

log "用例 ${CASE_NAME}（无头客户端 + Agent 菜单）"

log "[1/2] 构建 Godot（Debug）"
if [ -n "${FULL:-}" ]; then
    build_server_side || finish
fi
build_godot || finish

log "[2/2] 起无头客户端并断言"
start_game
wait_for "当前界面：登录界面" 60 "登录界面就绪、菜单已广播" || finish
assert_scene_create || finish
assert_no_error || finish
assert_no_missing_config || finish

stop_game
finish
