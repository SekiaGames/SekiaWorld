#!/bin/bash
# 用例：进入场景（最短的「登录 → 创角 → 进首图」链路）
#   编 Debug → 无头起客户端（Agent 模式）→ 账号密码登录 → 选角界面 → 创建角色 → 创角界面 → 进入游戏 → 断言进图链路
# 判据（在 launch 的四条之上再加六条）：
#   ① 登录成功（R2C_Login 回 Error 0）
#   ② 登录后进**选角界面**（不是 ET demo 的「大厅界面」—— 那条路已废弃，见下）
#   ③ 创角提交成功（C2G_CreateRole → RealM 写库 → 回带列表）
#   ④ 首图场景真的加载（[进图] 加载地图场景：res://Scene/prt_1/Sceneprt_1.tscn）
#   ⑤ 服务端下发换场景（M2C_StartSceneChange + "SceneName" : "Sceneprt_1"）
#   ⑥ 自己的单位已创建（M2C_CreateMyUnit）
#
# ⚠️ 2026-10-13 更新：本用例原来断言「大厅界面」并点大厅的「进入地图」按钮，那条路**已经不存在了** ——
#    `LoginFinish_CreateLobbyUI` 里创建 UILobby 的那行是注释掉的，登录后走 `LoginFinish_EnterCharacterChoose`
#    直接进选角界面，进图必须"建/选一个角色"（原来那条断言永远过不了，会误报 FAIL）。
#    现在改成当前流程。只想要"登录 + 进图"这条冒烟链路时跑它；要完整新手流程/存档/相机/寻路跑 `createrole`；
#    要看跨图传送跑 `changemap`。
# ⚠️ 需要客户先备好：MongoDB 在 127.0.0.1:27017、测试账号在 .FrameWork/Config/TestAccount/
#    另外美术包（.pck）必须是新的 —— 用例只管编代码，不打包。

CASE_NAME="进入场景"

log "用例 ${CASE_NAME}（无头客户端 + 账号密码登录 + 创角进首图）"

log "[1/3] 构建 Godot（Debug）"
build_godot || finish

log "[2/3] 读测试账号"
ACCOUNT=$(load_test_account) || finish
EMAIL=${ACCOUNT%% *}
PASSWORD=${ACCOUNT##* }
log "账号 ${EMAIL}（密码 ${#PASSWORD} 位）"

log "[3/3] 起无头客户端并断言"
start_game

# 起始状态（等同 launch 的四条）
wait_for "当前界面：登录界面" 60 "登录界面就绪、菜单已广播" || finish
assert_scene_create || finish
assert_no_error || finish
assert_no_missing_config || finish

# 账号密码登录。⚠️ 断言串取自实际日志：{"_t":"ET.R2C_Login", "RpcId":1, "Error":0, ...}
#   注意 JSON 是带空格的 BSON 风格（`"Error" : 0`），不能写成 `Error:0`；
#   类型名是 `ET.R2C_Login`，前缀的引号在 `ET.` 之前，所以不能拿 `"R2C_Login"` 去匹配。
send "1 $EMAIL $PASSWORD"
wait_for "已提交账号密码登录" 20 "登录命令已执行（UILoginComponentSystem.OnLogin）" || finish
wait_for "ET.R2C_Login" 40 "登录请求已发出（C2R_Login → R2C_Login）" || finish
wait_for '"Error" : 0, "Message" : null, "Address"' 40 "登录成功（R2C_Login Error 0 + Gate 地址）" || finish
wait_for "ET.G2C_LoginGate" 40 "Gate 登录成功（G2C_LoginGate）" || finish

# 判据②：登录后进**选角界面**（角色列表来自 RealM 的 Mongo；ET demo 大厅已废弃）
wait_for "[选角] 已激活" 60 "选角场景已加载、相机就位" || finish
wait_for "当前界面：选角界面" 40 "已切到选角界面、菜单已广播" || finish
wait_for "[选角界面] 已打开：槽 " 20 "选角界面按服务端列表填槽" || finish

# 清场：把该账号已有的角色删掉（空位是 no-op），保证用例可重复跑（同样是"走真实链路删库"）
for _ in 1 2 3; do
    send "4"
    sleep 1.2
done

# 判据③：创建角色（选角界面 → 创角场景/界面 → 填名字 → 进入游戏）
# 创角场景是大美术场景，加载 + 实例化超过 Agent 的 3 秒自动重播窗口 ⇒ 发完命令要持续 ? 刷新
send "1"
wait_for "已提交「创建角色」" 20 "创建角色命令已执行" || finish
for _ in $(seq 1 60); do
    send "?"
    sleep 0.5
done
wait_for "当前界面：创角界面" 40 "已切到创角界面、菜单已广播" || finish
send "3 4"
wait_for "[创角界面] 镜头推进结束，展开创角界面" 30 "选中职业 4 并展开界面" || finish
# ⚠️ 名字长度限制是 **2..8 字节**（中文 3 字节/字）⇒ 只能用两个字的名字
send "10 进图"
wait_for "已输入名字「进图」" 20 "名字已填" || finish

send "1"
wait_for "[创角界面] 进入游戏：名字「进图」" 20 "点「进入游戏」→ 创角提交（本地校验通过）" || finish
wait_for "[角色存档] 创角成功（RealM 已写库）：槽 1 名字「进图」" 30 "创角已落库（客户端 → Gate → RealM → MongoDB）" || finish

# 判据④⑤⑥：首图场景真的起来 + 服务端下发的场景名 + 自己的单位
# ⚠️ 地图名断言照抄实际日志：SceneName = GateMapConst.SceneNameOf(存档里的图)（新角色 = 默认图 prt_1）
wait_for "[进图] 加载地图场景：res://Scene/prt_1/Sceneprt_1.tscn" 60 "按美术约定找到首图场景" || finish
wait_for "[创角界面] 已进入首图" 90 "首图加载 + 建 Unit 完成" || finish
for _ in $(seq 1 20); do
    send "?"
    sleep 0.5
done
wait_for "ET.M2C_StartSceneChange" 40 "服务端已下发换场景（M2C_StartSceneChange）" || finish
wait_for '"SceneName" : "Sceneprt_1"' 40 "换到 prt_1（Sceneprt_1，角色存档里的默认图）" || finish
wait_for "ET.M2C_CreateMyUnit" 40 "自己的单位已创建（M2C_CreateMyUnit）" || finish

# 进图失败会在 EnterMapHelper 里落 Log.Error ⇒ 到这里再查一遍
assert_no_error || finish

stop_game
finish
