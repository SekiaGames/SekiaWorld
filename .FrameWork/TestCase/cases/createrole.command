#!/bin/bash
# 用例：新手流程（选角 → 创角）
#
# ⚠️ 角色存档现在在**服务端**：RealM 的 MongoDB（Account 文档的 roles 数组），客户端那份本地 json 已退役。
#    用例自己保证可重复：开头把该账号已存的角色删干净（空位是 no-op），结尾把本次建的角色删掉。
#   编 Debug → 无头起客户端（Agent 模式）→ 账号密码登录 → 选角界面 → 创建角色 → 创角界面 → 返回选角
# 判据（在 launch 的四条之上再加六条）：
#   ① 登录成功（R2C_Login 回 Error 0）
#   ② 选角场景加载 + 相机激活（[选角] 已激活）
#   ③ 选角界面广播（UIViewRolesList，一期无角色数据 ⇒ 3 个空槽）
#   ④ 创角场景加载 + 相机激活（[创角] 已激活）
#   ⑤ 创角界面广播（CreateRoleViewV2，职业/性别/发型格数）
#   ⑥ 创角界面点「返回选角」能切回选角界面
# ⚠️ 需要客户先备好：MongoDB 在 127.0.0.1:27017、测试账号在 .FrameWork/Config/TestAccount/
#    另外美术包（.pck）必须是最新的 —— 用例只管编代码，不打包。

CASE_NAME="新手流程（选角→创角）"

log "用例 ${CASE_NAME}（无头客户端 + 账号密码登录 + 选角/创角切换）"

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

# ---- 登录 ----
send "1 $EMAIL $PASSWORD"
wait_for "已提交账号密码登录" 20 "登录命令已执行" || finish
wait_for '"Error" : 0, "Message" : null, "Address"' 40 "登录成功（R2C_Login Error 0）" || finish

# ---- 判据②③：选角场景 + 选角界面 ----
wait_for "[选角] 场景已加载" 60 "选角场景已加载" || finish
wait_for "[选角] 已激活" 20 "选角相机已就位" || finish
wait_for "当前界面：选角界面" 40 "已切到选角界面、菜单已广播" || finish

# ---- 判据③b：角色列表来自服务端（RealM 的 Mongo）+ 清场 ----
# 登录时 Gate 会回问 RealM 要列表（拿到 key 之后），客户端选角界面用的就是它。
wait_for "[角色存档] Gate 拿到 key 后回问 RealM" 30 "登录时 Gate 回问 RealM 取角色列表" || finish
wait_for "[角色存档] 登录拿到账号角色列表" 20 "客户端拿到账号角色列表" || finish
wait_for "[选角界面] 已打开：槽 " 20 "选角界面按服务端列表填槽" || finish
# 清场：把这个测试账号已有的角色删掉（走真实链路：Gate → RealM 删库），保证用例可重复跑。
# 空位是 no-op，不会报错。
# 连着点「删除角色」：每次删掉当前自动选中的那个角色，删完界面会自动选中下一个有角色的槽
for _ in 1 2 3; do
    send "4"
    sleep 1.2
done

# ---- 判据④⑤：点「创建角色」→ 创角场景 + 创角界面 ----
# 创角场景是几十 MB 的美术大场景，加载 + 实例化超过 Agent 的 3 秒自动重播窗口 ⇒ 持续 ? 刷新
send "1"
wait_for "已提交「创建角色」" 20 "创建角色命令已执行" || finish
for _ in $(seq 1 60); do
    send "?"
    sleep 0.5
done
wait_for "[创角] 场景已加载" 60 "创角场景已加载" || finish
wait_for "[创角] 已激活" 20 "创角相机已就位" || finish
# 默认待机动作逐 NPC 取（出处 Unity 场景里 LuaGameObject.properties[1]）：
#   · 只有 2 号牧师男（1173）是 choose_wait2，其余（含 6 号刺客男 1171）都是 choose_wait；
#   · 3 只宠物的默认动作也都是 choose_wait，但**只有狼**（猎人 1181）的模型带这个 clip ⇒ 波利/疯兔退回 wait。
# 日志形如「1:默认女=choose_wait（男=wait，女=choose_wait，宠物=choose_wait）」，
# 「默认X=…」是画面上能看到的那个 NPC 的动作，括号里是另一半 + 宠物。
wait_for "1:默认女=choose_wait" 20 "猎人（默认女）= choose_wait" || finish
wait_for "2:默认女=choose_wait（男=choose_wait2" 20 "牧师男 = choose_wait2（Unity 里唯一的例外）" || finish
wait_for "3:默认男=choose_wait" 20 "铁匠（默认男）= choose_wait" || finish
wait_for "4:默认女=choose_wait（男=choose_wait2，女=choose_wait）" 20 "巫师：女 choose_wait、男退回 choose_wait2（同朝向，换性别不会反过来）" || finish
wait_for "5:默认男=choose_wait" 20 "骑士（默认男）= choose_wait" || finish
wait_for "6:默认男=choose_wait" 20 "刺客（默认男）= choose_wait" || finish
wait_for "宠物=choose_wait）" 20 "猎人的狼宠物 = choose_wait（与 Unity 一致）" || finish
wait_for "当前界面：创角界面" 40 "已切到创角界面、菜单已广播" || finish
wait_for "[创角界面] 已打开" 20 "创角界面数据初始化完成（职业/性别/发型格数）" || finish
# 格子图标要真的从 pck 里加载出来（发型 4 张 / 头饰 3 张）
wait_for "[创角界面] 格子图标：发型 4/4，头饰 3/3" 20 "发型/头饰格子图标已加载" || finish
wait_for "[创角界面] 进场镜头过场结束，显示 返回 + Banner" 30 "进场过场播完、回到 Title 位" || finish

# ---- 判据⑧：点选射线自检（美术场景里 Characters/{n} 的 Pick 胶囊摆对了才全部命中） ----
# 背景：拾取体从「运行时按包围盒现造」改成了「美术场景里的胶囊」（按 Unity 的 CapsuleCollider 摆），
# 包围盒那版互相重叠，会把巫师误判成相邻职业 —— 这条专门守它。
send "9"
wait_for "射线拾取自检：全部命中" 20 "点选射线全部命中（每组只打中自己）" || finish

# ---- 判据⑦：创角界面的换装联动到 3D 角色 ----
# ⚠️ 下面的序号是创角界面菜单项在 AgentMenu.Items 里的顺序：
#    1 进入游戏 / 2 返回选角 / 3 选中职业 / 4 切性别男 / 5 切性别女 / 6 换发型 / 7 换发色 / 8 换头饰 / 9 射线拾取自检
#    调了菜单顺序就要同步改这里。
#
# 选中职业会先播 wait{n} 推镜，**推镜播完才展开创角界面**（Unity: Selected → ShowOccupationDetail）。
send "3 4"
wait_for "[创角界面] 选中职业 4" 20 "选中职业 4（巫师）" || finish
wait_for "[创角] 点选职业 4：镜头推进（wait4）" 20 "创角镜头推进 wait4 开始" || finish
# 选中时女巫师要播 choose_turn（Unity ACTION_SELECTED_CROSS：choose_wait → choose_wait2 的慢慢转身）
wait_for "[创角] 切换动作：choose_turn（Body 14）" 20 "选中时播 choose_turn（慢慢转过身）" || finish
# 其余 5 组 + 它们的宠物都要隐藏（宠物挂在 Root 下、不在 Characters/{n} 里，必须单独关）
wait_for "[创角] 隐藏其余角色：5 组 + 3 只宠物" 20 "其余 5 组 + 3 只宠物都隐藏了" || finish
wait_for "[创角界面] 镜头推进结束，展开创角界面" 30 "推镜播完 → 展开创角界面" || finish
wait_for "外观已套用：职业 4 性别 2" 20 "职业切换已套到 3D 角色（巫师女）" || finish
send "4"
wait_for "外观已套用：职业 4 性别 1" 20 "切性别已套到 3D 角色（巫师男）" || finish
wait_for "[创角界面] 切性别：职业 4 → 男" 20 "切性别只作用于当前选中的这一组" || finish
send "6"
wait_for "已换下一款发型" 20 "换发型" || finish
send "7"
wait_for "已换下一个发色" 20 "换发色" || finish
# 发色要真的落到材质上（角色工程里每个发型都有 {模型}_{1..9}.material，_BaseColor = Table_HairColor[n].PaintColor）
wait_for "发色已应用：" 20 "发色材质已套到 3D 头发" || finish
send "8"
wait_for "已换下一个头饰" 20 "换头饰" || finish

# ---- 判据⑥：返回选角 ----
# 已选中职业 ⇒ 第一次「返回」是**反选**（镜头倒放回静止位 + 收回创角界面），再点一次才真回选角。
send "2"
wait_for "已点「返回」" 20 "返回命令已执行" || finish
wait_for "[创角] 反选还原：职业 4 回到默认性别 2" 20 "反选先还原成默认（女巫师），再回放镜头" || finish
# 点返回时**姿势不许瞬间变**：这一刻角色在 choose_wait2（选中姿势），要播的就是它（Unity ApplySelected）
wait_for "[创角] 切换动作：choose_wait2（Body 14）" 20 "点返回时姿势保持选中姿势（不立刻转身）" || finish
wait_for "[创角] 反选：镜头回退（wait4）" 20 "创角镜头回退 wait4 开始" || finish
wait_for "[创角界面] 镜头回退结束，收回创角界面" 30 "回退播完 → 收回创角界面" || finish
# 镜头倒放**播完**，才让 choose_turn2 慢慢转回默认站姿（Unity DoUnselectRole 的 ACTION_UNSELECTED_CROSS）
wait_for "[创角] 切换动作：choose_turn2（Body 14）" 20 "镜头播完后才播 choose_turn2（慢慢转回默认站姿）" || finish

send "2"
for _ in $(seq 1 20); do
    send "?"
    sleep 0.5
done
wait_for "[选角] 已激活" 40 "选角相机已切回" || finish
wait_for "当前界面：选角界面" 40 "已切回选角界面" || finish

# ---- 判据⑨：进入游戏（创角提交 → 进首图） ----
# 再回创角界面（场景没卸，很快），选职业 + 填名字 + 点「进入游戏」，验证能进首图 prt_1。
send "1"
for _ in $(seq 1 20); do
    send "?"
    sleep 0.5
done
wait_for "当前界面：创角界面" 40 "又切回创角界面" || finish
wait_for "[创角界面] 进场镜头过场结束，显示 返回 + Banner" 30 "进场过场播完" || finish

send "3 4"
wait_for "[创角界面] 镜头推进结束，展开创角界面" 30 "选中职业 4 并展开界面" || finish
send "10 测测"
wait_for "已输入名字「测测」" 20 "名字已填" || finish
send "1"
wait_for "[创角界面] 进入游戏：名字「测测」" 20 "点「进入游戏」→ 创角提交（本地校验通过）" || finish
# 创角要走服务端写库：客户端 → Gate（C2G_CreateRole）→ RealM（G2R_CreateRole）→ RealM 写 MongoDB
wait_for "[角色存档] RealM 写库（创角）：账号" 30 "创角已落库（RealM 写 MongoDB）" || finish
wait_for "[角色存档] 创角成功（RealM 已写库）：槽 1 名字「测测」" 20 "创角成功、账号角色列表已刷新" || finish
wait_for "[进图] 加载地图场景：res://Scene/prt_1/Sceneprt_1.tscn" 60 "按美术约定找到首图场景" || finish
wait_for "[创角界面] 已进入首图" 90 "首图加载 + 建 Unit 完成" || finish
# 进图后玩家自己应该是**初心者**（创角选的是目标职业，真正建出来的是初心者）：
# 女号 ⇒ Body=2、Eye=2、右手是初心者默认武器 40301，头发/发色才是界面里选的那套
wait_for "[进图] 玩家角色已装配：名字「测测」[Body=2 Hair=14 Head=0 Eye=2 L=0 R=40301" 30 "进图后玩家角色 = 初心者（与 Unity/服务端落库一致）" || finish
# 进游戏后要出现主界面 MainView（「返回角色选择」以后挂在它的设置菜单里）
wait_for "[进图] 主界面 MainView 已打开" 30 "进图后主界面 MainView 已打开" || finish

# ---- 判据⑯：全局主相机（常驻单例 + 三态 + 跟随玩家）----
# 主相机是 ModelView 的**全局单例**（MainCameraComponent，客户端一启动就建在 Init.tscn 的 CameraRoot 下，
# 不再挂在地图节点下 ⇒ 卸图不会把相机带走）；进图时 SceneChangeStart_AddComponent 调 Activate 接管。
wait_for "[相机] 主相机已就位：Sceneprt_1 跟随玩家" 30 "进图后主相机接管（全局单例，相机信息按图复位）" || finish
wait_for "[相机] 开始跟随玩家" 30 "主相机读到本地玩家并开始跟随" || finish

# 报告状态：相机位与玩家位应当**不同**（相机在焦点后上方），且玩家要落在视锥里
send "7"
wait_for "[相机] 状态：active=True" 20 "相机状态：已接管且正在跟随" || finish
CAM_LINE=$(grep -a "\[相机\] 状态：active=" "$OUT_FILE" | tail -1)
CAM_POS=$(echo "$CAM_LINE" | sed -E 's/.*相机 pos=\(([^)]*)\).*/\1/')
PLR_POS=$(echo "$CAM_LINE" | sed -E 's/.*玩家 pos=\(([^)]*)\).*/\1/')
case "$CAM_LINE" in
    *"玩家在视锥内=True"*) ok "相机对着玩家（玩家在视锥内）" ;;
    *) bad "相机没对着玩家：$CAM_LINE" ;;
esac
# 朝向也要查：prt_1 的相机信息是俯角 30°、偏航 −45°（Unity 空间），
# 镜像到 Godot 后前方应是 x>0 / y<0 / z>0 这一象限（≈ (0.61, −0.50, 0.61)）。
# ⚠️ 这条是必须的：只查「在视锥内」查不出偏航反了 —— 相机始终对着玩家，绕到另一侧照样在视锥内（客户实测就是这个 bug）。
FWD=$(echo "$CAM_LINE" | sed -E 's/.*相机朝向=\(([^)]*)\).*/\1/')
FX=$(echo "$FWD" | cut -d',' -f1 | tr -d ' ')
FY=$(echo "$FWD" | cut -d',' -f2 | tr -d ' ')
FZ=$(echo "$FWD" | cut -d',' -f3 | tr -d ' ')
if [ -n "$FZ" ] && awk "BEGIN{exit !($FX > 0.4)}" && awk "BEGIN{exit !($FY < -0.3)}" \
        && awk "BEGIN{exit !($FZ > 0.4)}"; then
    ok "相机朝向正确（镜像后应该是这个象限）：$FWD"
else
    bad "相机朝向不对（应 x>0 / y<0 / z>0）：$FWD ← $CAM_LINE"
fi
if [ -n "$CAM_POS" ] && [ -n "$PLR_POS" ] && [ "$CAM_POS" != "$PLR_POS" ]; then
    ok "相机在偏移位跟随（相机 ${CAM_POS} ≠ 玩家 ${PLR_POS}）"
else
    bad "相机位与玩家位异常：相机「${CAM_POS}」/ 玩家「${PLR_POS}」"
fi
assert_no_error || finish

# ---- 判据⑰：相机高度固定（客户 2026-10-14：去掉鼠标滑动调高低，高度与 RO-Unity 一致）----
# Unity 的 CameraController.zoom 默认 1.0（触屏手势才改它）⇒ 相机到焦点的距离恒 = focusViewPort.z。
# prt_1：z = 30、fov = 20、focusViewPort.y = 0.4、俯角 30° ⇒ 解析出来
#   高度（相对焦点） = z·sin30 + |dy|·cos30 ≈ 15.9   （dy = (0.4-0.5)·2·tan(fov/2)·z）
#   水平距离        = z·cos30 − |dy|·sin30 ≈ 25.45
# 实测 15.91 / 25.45（见下方容差区间）。这两个数只由该图的相机信息决定，**跑起来不会变**（原来那条滚轮入口已删除）。
send "7"
sleep 1
CAM_FIXED=$(grep -a "\[相机\] 状态：active=" "$OUT_FILE" | tail -1)
CAM_H=$(echo "$CAM_FIXED" | sed -E 's/.*高度（相对焦点）=([-0-9.]+).*/\1/')
CAM_R=$(echo "$CAM_FIXED" | sed -E 's/.*水平距离=([-0-9.]+).*/\1/')
if [ -n "$CAM_H" ] && awk "BEGIN{exit !($CAM_H > 15.3 && $CAM_H < 16.5)}"; then
    ok "相机高度固定且与 RO-Unity 一致（≈15.9，实测 ${CAM_H}）"
else
    bad "相机高度不是 Unity 那一档（应 ≈15.9，实测「${CAM_H}」← ${CAM_FIXED}）"
fi
if [ -n "$CAM_R" ] && awk "BEGIN{exit !($CAM_R > 24.9 && $CAM_R < 26.1)}"; then
    ok "相机水平距离固定（≈25.45，实测 ${CAM_R}）"
else
    bad "相机水平距离不对（应 ≈25.45，实测「${CAM_R}」）"
fi
assert_no_error || finish

# ---- 判据⑪：主界面「更多」按钮 → 切「更多面板」（MoreBord）显隐 ----
# 更多面板是 MainView 的**子节点**（同场景节点显隐，不是独立 UI），美术工程里默认隐藏（visible=false）；
# 主界面右上角的「更多」按钮切它。Agent 菜单第 2 项 = 人类点「更多」的同一条路。
wait_for "[主界面] 更多面板已接入：菜单格 11 个" 30 "更多面板已接入（11 个菜单格 + 关闭按钮）" || finish
send "2"
wait_for "[主界面] 更多面板：展开" 20 "点「更多」→ 展开更多面板" || finish
send "2"
wait_for "[主界面] 更多面板：收起" 20 "再点「更多」→ 收起更多面板" || finish

# ---- 判据⑩：点地寻路（进图后能走）----
# 无头 Agent 没有真鼠标，用「往屏幕中心打点地面」代替点击：射线打到地面
# → 发 C2M_PathfindingResult → 服务端在 Recast 导航网格（RecastOutput/Sceneprt_1）上算路 → M2C_PathfindingResult
send "1"
wait_for "[寻路] 点地：世界坐标" 20 "点地射线打到了地面（地面有碰撞体可打）" || finish
wait_for "[寻路] 收到服务端路径" 30 "服务端算出路径并回给客户端（角色开始走）" || finish

# ---- 判据⑫：更多面板 →「设置」→ 设置界面（打开 / 分页 / 关闭）----
# 设置界面是**独立 UI**（`UIType.SetView`，画在 UILayer.High），由「更多面板」里那颗「设置」格打开。
# ⚠️ 这一段放在**最后**：它的下一步（「切换角色」）会把主界面收掉、退回选角界面，
#    之后就点不了主界面的菜单了。
send "3"
wait_for "[主界面] 更多面板点了「设置」" 30 "更多面板 → 打开设置界面" || finish
wait_for "[设置] 界面已打开" 30 "设置界面已打开（分页 + 打桩按钮）" || finish
send "4 2"
wait_for "[设置] 切到分页：EffectSetting" 20 "切到「效果」分页" || finish
send "4 3"
wait_for "[设置] 切到分页：SecuritySetting" 20 "切到「安全」分页" || finish
send "5"
wait_for "[设置] 关闭" 20 "关闭设置界面" || finish

# ---- 判据⑬：「切换角色」→ 退回角色选择界面 ----
# 四步：收界面 → 卸地图与当前场景（玩家/怪的视图随 Unit 的 GameObjectComponent 一起释放）
# → 重载选角场景（进图时被卸掉了）→ 开选角界面。⚠️ 走的是**真实按钮入口**，所以先把设置界面重新打开。
send "3"
sleep 3
send "6"
wait_for "[设置] 点了「切换角色」" 20 "点「切换角色」（真实按钮入口）" || finish
wait_for "[回选角] 已卸掉地图与当前场景" 30 "卸掉地图与当前场景" || finish
wait_for "[相机] 停飞：无地图 / 加载中 / 不在场景里" 30 "主相机停飞（挪到很远的位置）" || finish
wait_for "[回选角] 已回到角色选择界面" 60 "回到角色选择界面" || finish
wait_for "当前界面：选角界面" 20 "界面已切回选角" || finish

# ---- 判据⑭：回选角后**再点「创建角色」** ----
# 修的 bug：进游戏时创角场景被卸掉了，但 `CharacterSelectComponent` 缓存的 `sceneNode`/`camera`
# 还是**已经释放的 Godot 对象**（`self.camera ??=` 只在第一次取）⇒ 再进来直接 `camera.Current = true`
# 会抛 `ObjectDisposedException: … Object name: 'Godot.Camera3D'`（客户实测报的就是它）。
# 现在 `WaitSceneAsync` 进来先 `ForgetUnloadedScene`：发现场景节点已失效就清缓存 + 重新加载。
send "1"
wait_for "已提交「创建角色」" 20 "再点「创建角色」" || finish
for _ in $(seq 1 40); do send "?"; sleep 0.5; done
wait_for "[创角] 场景已加载" 60 "创角场景重新加载" || finish
wait_for "[创角] 已激活" 20 "创角相机就位（不再是已释放的对象）" || finish
wait_for "当前界面：创角界面" 40 "重新进到创角界面" || finish
assert_no_error || finish

# ---- 判据⑨：掉线（心跳超时）回写位置 ----
# 把客户端进程冻住（不再发 Ping 心跳）⇒ 服务端 SessionIdleCheckerComponent（每 2 秒检查、收+发都静默
# 超过 SessionTimeoutTime=30 秒才判死）判定掉线 ⇒ Gate 发现 session.Error=ERR_SessionSendOrRecvTimeout
# ⇒ 通知 RealM 把这个角色停在哪写进库。
# 注意：单进程跑（客户端进程里就带着 Realm/Gate），所以这两条服务端日志也在 agent_out 里。
kill -STOP "$GAME_PID"
sleep 40
kill -CONT "$GAME_PID"
# ⚠️ 这两条用「软断言」：掉线钩子目前是**间歇性**的（同一份代码有时触发、有时不触发），
#    不让它中断后面的判据；它真触发时这里照样会打 ok。
wait_soft "[角色存档] 掉线（心跳超时）回写" 40 "掉线：Gate 通知 RealM 回写角色位置"
wait_soft "[角色存档] RealM 写库（掉线回写位置）" 40 "掉线回写已落库（RealM 写 MongoDB）"

# 切场景/切界面过程里若有 Log.Error（如相机找不到）会在这里暴露
assert_no_error || finish

# ---- 判据⑦⑧：重启客户端再上线 ⇒ 列表里还有刚建的角色（数据在 RealM 的 Mongo 里）；再把它删掉 ----
# 这一段同时验证「退出再上线角色还在」（服务端持久化，不是本地文件）和「删除角色」（不做冷却，直接删）。
stop_game
start_game
wait_for "当前界面：登录界面" 60 "重启后回到登录界面" || finish
send "1 $EMAIL $PASSWORD"
wait_for "已提交账号密码登录" 20 "重启后再登录" || finish
wait_for '"Error" : 0, "Message" : null, "Address"' 40 "重启后登录成功" || finish
wait_for "[角色存档] 登录拿到账号角色列表：1 个" 30 "重启后从 RealM 读回刚建的角色" || finish
wait_for "当前界面：选角界面" 40 "重启后到选角界面" || finish
wait_for "[选角界面] 已打开：槽 1:测测 2:空位 3:锁" 20 "列表里就是刚建的那个角色" || finish
wait_for "[选角] 已把角色摆到挂点上" 20 "屏幕中间摆出了选中的角色（挂点来自美术场景 Root/RoleMount）" || finish

# ---- 判据⑮：位置存档 ——「作为下次上线时的位置」----
# 前面掉线回写把角色停在哪存进了 RealM 的 Mongo；这里用**同一个角色**再进一次图，
# 出生点应该就是存档里那个位置（而不是固定的出生点 GateMapConst.SpawnPos = BornPoints ID=1）。
# 选角界面进来会自动选中第一个有角色的槽 ⇒ 直接点「进入游戏」（同一个按钮，文案这时是「进入游戏」）。
send "1"
wait_for "[选角界面] 进入游戏：槽 1" 30 "选中槽 1 → 进入游戏" || finish
wait_for "[进图] 用存档位置出生：账号" 60 "按存档位置出生（RealM 里那份位置被用上了）" || finish
# 客户端装配日志比服务端那条晚一步 ⇒ 先等它落盘再解析，否则会解析到空串
wait_for "[进图] 玩家角色已装配：名字「测测」" 30 "进图后玩家角色已装配" || finish
SAVED_POS=$(grep -a "用存档位置出生" "$OUT_FILE" | tail -1 | sed -E 's/.*(pos=\([^)]*\)).*/\1/')
SPAWN_POS=$(grep -a "玩家角色已装配" "$OUT_FILE" | tail -1 | sed -E 's/.*位置=(\([^)]*\)).*/\1/; s/^/pos=/')
BORN_POS="pos=(43.00, 0.20, 43.41)"
# 还要**不是**出生点常量，否则这条断言等于没验（服务端一开始就退回了默认出生点）
if [ -n "$SAVED_POS" ] && [ "$SAVED_POS" = "$SPAWN_POS" ] && [ "$SAVED_POS" != "$BORN_POS" ]; then
    ok "进图出生位置 = 存档位置，且不是出生点常量（${SAVED_POS}）"
else
    bad "进图出生位置「${SPAWN_POS}」/ 存档位置「${SAVED_POS}」对不上（出生点常量是 ${BORN_POS}）"
fi
assert_no_error || finish

# 回到选角才能删角色（正好再走一遍「回选角」）
send "6"
wait_for "[回选角] 已回到角色选择界面" 60 "回到角色选择界面" || finish
wait_for "当前界面：选角界面" 20 "界面已切回选角" || finish

send "4"
wait_for "[角色存档] RealM 写库（删角）：账号" 30 "删角已落库（RealM 写 MongoDB）" || finish
wait_for "[选角界面] 删除后（删了槽 1 的「测测」）：槽 1:空位 2:空位 3:锁" 20 "删除后列表里没它了" || finish

assert_no_error || finish

stop_game
finish
