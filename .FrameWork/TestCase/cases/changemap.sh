#!/bin/bash
# 用例：传送（跨地图 prt_1 → 普隆德拉）
#
# 「在哪个图」是**角色信息**的一部分（存在角色存档 RoleInfo.MapId 里）：进图按存档里的图出生，
# 传送（C2G_ChangeMap）= 改存档里的图 + 按目标图重建 Unit + 客户端切场景。
# 本用例走完整链路：登录 → 清场 → 创角 → 进首图 prt_1 → 调试菜单传送 → 断言切到普隆德拉 → 重启重登验证已落库。
#
# 判据（在 launch 的四条之上再加六条）：
#   ① 首图 = prt_1（角色刚建出来时存档里的默认图 id=2）
#   ② 传送被服务端接受（G2C_ChangeMap Error 0 + SceneName=SceneProntera）
#   ③ 服务端换图（[传送] 旧图 → 新图，并按目标图出生点重建 Unit）
#   ④ 客户端加载普隆德拉场景（[进图] 加载地图场景：res://Scene/Prontera/SceneProntera.tscn）
#   ⑤ 换图**立刻落库**：重启重登后仍在 SceneProntera（[进图] 用存档位置出生 … mapId=1（SceneProntera））
#   ⑥ 全程无 Log.Error
#   ⑦ 地面碰撞体：客户端给普隆德拉补出来的可行走网格数 > 0（点地寻路的前提）
#   ⑧ 传送阵摆出来：prt_1 1 个（→普隆德拉）、普隆德拉 2 个（→prt_1 与同图 ex_5）
#   ⑨ 走到传送阵 → 真的切回 prt_1（端到端，含点地射线打中地面）
#
# ⚠️ 前置（客户侧，Agent 不管）：
#   · MongoDB 在 127.0.0.1:27017、测试账号在 .FrameWork/Config/TestAccount/
#   · 美术包（.pck）里要有 res://Scene/Prontera/SceneProntera.tscn
#   · 服务端导航数据 .FrameWork/Config/RecastOutput/SceneProntera
#     （没有就跑：bash .Art/NavExport-SekiaArt_Map.command Prontera）
#   缺导航时服务端会回 ERR_MapNavMissing（200142），本用例会在判据②失败。

CASE_NAME="传送（跨地图 prt_1 → 普隆德拉）"

log "用例 ${CASE_NAME}（无头客户端 + 登录 + 创角进图 + 传送 + 重启验证落库）"

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

# ---- 登录 → 选角 ----
send "1 $EMAIL $PASSWORD"
wait_for "已提交账号密码登录" 20 "登录命令已执行" || finish
wait_for '"Error" : 0, "Message" : null, "Address"' 40 "登录成功（R2C_Login Error 0）" || finish
wait_for "[选角] 已激活" 60 "选角相机已就位" || finish
wait_for "当前界面：选角界面" 40 "已切到选角界面" || finish
wait_for "[选角界面] 已打开：槽 " 20 "选角界面按服务端列表填槽" || finish

# 清场：把这个测试账号已有的角色删掉（空位是 no-op），保证用例可重复跑
# ⚠️ 菜单第 4 项删的是**当前选中**的槽 ⇒ 必须先 "3 <槽号>" 选中再删。
#    （2026-10-14 修：原来只发 "4"、没选中 ⇒ 一直是 no-op，残留角色会让下一次「创角」报 200131 ERR_RoleSlotTaken）
# ⚠️ 2026-10-15 补：槽点击是 **toggle** —— 打开选角界面时**默认已选中**第一个有角色的槽，
#    这时再 "3 1" 反而是**取消选中** ⇒「先选中再删」对那个槽失效（实测：残留角色没删掉，
#    新角色落到槽 2，后面等「槽 1」的判据直接超时）。所以先直接发一次 "4" 删默认选中的那个，
#    再逐槽「选中 → 删除」，跑两轮兜底。
log "清场：删掉账号里已有的角色"
for _round in 1 2; do
    send "4"
    sleep 1.0
    for slot in 1 2 3; do
        send "3 $slot"
        sleep 0.6
        send "4"
        sleep 1.0
    done
done
wait_for "已点「删除角色」" 10 "清场：删角命令已生效" || log "⚠️ 清场没生效（没看到删角回执）—— 残留角色可能让下一步创角报 ERR_RoleSlotTaken"

# ---- 创角（最短路径：选职业 → 填名字 → 进入游戏）----
send "1"
wait_for "已提交「创建角色」" 20 "创建角色命令已执行" || finish
for _ in $(seq 1 60); do
    send "?"
    sleep 0.5
done
wait_for "当前界面：创角界面" 40 "已切到创角界面、菜单已广播" || finish
send "3 4"
wait_for "[创角界面] 镜头推进结束，展开创角界面" 30 "选中职业 4 并展开界面" || finish
send "10 传送"
wait_for "已输入名字「传送」" 20 "名字已填" || finish

# ---- 判据①：首图 = prt_1 ----
send "1"
wait_for "[创角界面] 进入游戏：名字「传送」" 20 "点「进入游戏」→ 创角提交" || finish
wait_for "[角色存档] 创角成功（RealM 已写库）：槽 1 名字「传送」" 30 "创角已落库" || finish
wait_for "[进图] 加载地图场景：res://Scene/prt_1/Sceneprt_1.tscn" 60 "首图是 prt_1（存档里的默认图 id=2）" || finish
wait_for "[创角界面] 已进入首图" 90 "首图加载 + 建 Unit 完成" || finish
wait_for "[进图] 主界面 MainView 已打开" 30 "进图后主界面已打开" || finish
wait_for "[传送阵] Sceneprt_1：1 个" 30 "prt_1 摆出 1 个传送阵（通往普隆德拉）" || finish

# ---- 判据②③：传送（主界面菜单第 8 项 = 传送到指定地图）----
# ⚠️ 序号 8 = AgentMenu.Items 里 MainView 那一组的倒数第二项；往中间插菜单项会让它位移
send "8 1"
wait_for "已请求传送到地图 1" 20 "传送命令已执行（Agent 调试入口）" || finish
wait_for '"Error" : 0, "Message" : null, "SceneName" : "SceneProntera"' 40 "服务端接受传送（G2C_ChangeMap Error 0 + SceneName）" || finish
wait_for "[传送] 服务端已接受" 20 "客户端收到 G2C_ChangeMap（ChangeMapHelper）" || finish
wait_for "[传送] 账号 " 40 "服务端换图：回写旧图 → 清旧 Unit → 按目标图重建" || finish
wait_for "[传送] 完成" 20 "换图完成（含立刻落库）" || finish

# ---- 判据④：客户端切到普隆德拉 ----
for _ in $(seq 1 20); do
    send "?"
    sleep 0.5
done
wait_for '"SceneName" : "SceneProntera"' 40 "服务端下发 M2C_StartSceneChange(SceneProntera)" || finish
wait_for "[进图] 加载地图场景：res://Scene/Prontera/SceneProntera.tscn" 90 "客户端加载普隆德拉场景" || finish
# 相机信息按图复位（每张图一套，来源是 Unity 的相机 prefab）：普隆德拉 fov 24 / 距离 23（prt_1 是 20 / 30）
wait_for "[相机] 主相机已就位：SceneProntera 跟随玩家（fov 24.0" 30 "普隆德拉用自己那套相机信息接管" || finish
wait_for "[进图] 玩家角色已装配：名字「传送」" 40 "目标图里角色已装配" || finish
assert_no_error || finish

# ---- 判据⑦⑧：普隆德拉的地面碰撞体 + 点地寻路 ----
# 普隆德拉美术场景没烘地面碰撞体（prt_1 烘了 3 个地表）⇒ 客户端载图后按「可行走规则」在运行期补 trimesh
# （MapGroundCollision）。没有它，点地射线会打空、人走不动。
wait_for "[地面碰撞] SceneProntera：新增 " 30 "客户端给普隆德拉补了地面碰撞体" || finish
GROUND_LINE=$(grep -a "\[地面碰撞\] SceneProntera：新增 " "$OUT_FILE" | tail -1)
GROUND_N=$(echo "$GROUND_LINE" | sed -E 's/.*新增 ([0-9]+) 个.*/\1/')
if [ -n "$GROUND_N" ] && [ "$GROUND_N" -gt 0 ]; then
    ok "普隆德拉新增 ${GROUND_N} 个地面碰撞体（prt_1 实测是 36 —— 它只烘了 3 个地表）"
else
    bad "地面碰撞体没补上：${GROUND_LINE}"
fi

# 传送阵：普隆德拉那张图上的阵被摆出来（可见物 + 触发组件）。
# 点地/走进去那两条在最后一段（重启回来之后）验证 —— 那段会真的把角色送到阵上。
wait_for "[传送阵] SceneProntera：2 个" 30 "普隆德拉摆出 2 个传送阵（→prt_1 + 同图）" || finish
assert_no_error || finish

# ---- 判据⑤：换图立刻落库 —— 重启重登，仍在普隆德拉 ----
stop_game
start_game
wait_for "当前界面：登录界面" 60 "重启后回到登录界面" || finish
send "1 $EMAIL $PASSWORD"
wait_for "已提交账号密码登录" 20 "重启后再登录" || finish
wait_for '"Error" : 0, "Message" : null, "Address"' 40 "重启后登录成功" || finish
wait_for "[角色存档] 登录拿到账号角色列表：1 个" 30 "重启后从 RealM 读回角色" || finish
wait_for "当前界面：选角界面" 40 "重启后到选角界面" || finish

send "1"
wait_for "[选角界面] 进入游戏：槽 1" 30 "选中槽 1 → 进入游戏" || finish
wait_for "[进图] 用存档位置出生：账号 " 60 "按存档位置出生" || finish
wait_for "mapId=1（SceneProntera）" 20 "存档里的图就是普隆德拉（传送已落库）" || finish
wait_for "[进图] 加载地图场景：res://Scene/Prontera/SceneProntera.tscn" 90 "重登后加载的仍是普隆德拉" || finish
wait_for "[传送阵] SceneProntera：2 个" 30 "重登后普隆德拉的传送阵仍在（摆在地图节点下，随图重建）" || finish
assert_no_error || finish

# ---- 判据⑨：走到传送阵 → 真的切回 prt_1（传送阵端到端）----
# 菜单第 9 项：走到传送阵（Agent 只会点屏幕中心，瞄不准半径 1.3m 的阵）。
# 阵心在相机视野内 ⇒ 投影成屏幕坐标再点地（顺带再证一次地面碰撞体有效）；
# 不在视野内（相机是**每图一套固定朝向**、不随角色转，阵可能落在背后）⇒ 直接按世界坐标寻路。
send "9"
wait_for "已按传送阵的位置走去" 20 "走到传送阵命令已执行" || finish
if grep -qF "[寻路] 点地：世界坐标" "$OUT_FILE"; then
    ok "射线打中地面（顺带再证一次普隆德拉地面碰撞体有效）"
else
    wait_for "[寻路] 直接寻路：世界坐标" 20 "阵心不在视野内 ⇒ 按世界坐标直接寻路" || finish
fi
wait_for "[传送阵] 走进传送阵" 60 "走进传送阵 → 客户端发 C2G_ChangeMap" || finish
wait_for "[进图] 加载地图场景：res://Scene/prt_1/Sceneprt_1.tscn" 90 "传送阵真的把人送回了 prt_1" || finish
assert_no_error || finish

stop_game
finish
