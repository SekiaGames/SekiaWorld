# 本地 MongoDB 安装

1. 下载 **MongoDB Community Server**：<https://www.mongodb.com/try/download/community>
   - Version 选当前稳定版
   - Platform 选 `macOS arm64`（Intel 机器选 `macOS x86_64`）
   - Package 选 `tgz`（不要 msi、不要 atlas）
   优点：便携、免管理员、免安装

2. 解压，把压缩包里 `bin/` 目录（含 `mongod`）放到本目录下 ⇒ `.FrameWork/Database/bin/mongod`

3. 数据目录 `.FrameWork/Database/db` **不用手动建** —— 启库脚本会自动 `mkdir`。
   它已在 `.gitignore` 里（不入库），所以全新 clone 后是空的。

4. **启动** —— 三种入口口径完全一样（端口 `27017`、`dbpath Database/db`、日志 `Logs/mongod.log`、**后台无窗口**）：
   - **只想起库**：双击本目录的 `启动数据库.command`
   - **连服务端一起起**：双击 `.FrameWork/Launch02_RunServer.command`（它第 1 步就是起库；已在跑就跳过）
   - 手工命令行（在本目录下跑）：
     `bin/mongod --dbpath db --port 27017 --logpath ../Logs/mongod.log --logappend`

   ⚠️ macOS 版 `mongod` **不支持 `--fork`**（报 `BadValue: fork+exec via --fork is incompatible with macOS`）；
   `nohup` 也不够 —— mongod 自己接管 SIGHUP 并当优雅关闭请求，关掉终端窗口会顺手把库停掉。
   两个脚本都用 `perl -MPOSIX` 的 `setsid` 建新会话再 `disown`，手工起库要后台常驻就照抄这一招。

5. 看到日志 `"msg":"Listening on","attr":{"address":"127.0.0.1:27017"}` → `"msg":"mongod startup complete"` 即启动成功。
   它**没有窗口**，不用管它。

6. **停库**：双击本目录的 `关闭数据库.command`。它按 `lsof` 拿到监听 `27017` 的 PID 再杀，
   **只动本项目这一个 mongod**，先 `kill`（SIGTERM）⇒ mongod 走**完整优雅关闭**（flush journal + checkpoint + 释放锁），
   5s 内没退才 `kill -9` 兜底。

   ⚠️ 别用 `pkill -f mongod` —— 那是按命令行匹配，会把这台机器上**所有** mongod 一起杀掉
   （自己装的 MongoDB 服务版、别的项目起的实例都算）。

   细节与实测日志 ⇒ `DocAgent/Godot/Feature/Launch脚本.md`
