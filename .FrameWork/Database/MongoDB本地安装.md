# 本地 MongoDB 安装

1. 下载 **MongoDB Community Server**：<https://www.mongodb.com/try/download/community>
   - Version 选当前稳定版
   - Platform 选 `Windows x64`
   - Package 选 `zip`（不要 msi、不要 atlas）
   优点：便携、免管理员、免安装

2. 解压，把压缩包里 `bin/` 目录（含 `mongod.exe`）放到本目录下 ⇒ `.FrameWork/Database/bin/mongod.exe`

3. 数据目录 `.FrameWork/Database/db` **不用手动建** —— 启库脚本会自动 `mkdir`。
   它已在 `.gitignore` 里（不入库），所以全新 clone 后是空的。

4. **启动** —— 三种入口口径完全一样（端口 `27017`、`dbpath Database\db`、日志 `Logs\mongod.log`、**隐藏后台无窗口**）：
   - **只想起库**：双击本目录的 `启动数据库.bat`
   - **连服务端一起起**：双击 `.FrameWork/Launch02_RunServer.bat`（它第 1 步就是起库；已在跑就跳过）
   - 手工命令行：
     `D:\SekiaWorld\.FrameWork\Database\bin\mongod.exe --dbpath D:\SekiaWorld\.FrameWork\Database\db --port 27017 --logpath D:\SekiaWorld\.FrameWork\Logs\mongod.log --logappend`

5. 看到日志 `"msg":"Listening on","attr":{"address":"127.0.0.1:27017"}` → `"msg":"mongod startup complete"` 即启动成功。
   它**没有窗口**，不用管它。

6. **停库**：双击本目录的 `关闭数据库.bat`。它按 `netstat` 拿到监听 `27017` 的 PID 再杀，
   **只动本项目这一个 mongod**，且 `taskkill` 不带 `/F` ⇒ mongod 走**完整优雅关闭**（flush journal + checkpoint + 释放锁）。

   ⚠️ 别用 `taskkill /IM mongod.exe /F` —— 那是按**镜像名**匹配，会把这台机器上**所有** `mongod.exe` 一起杀掉
   （自己装的 MongoDB Windows 服务版、别的项目起的实例都算）。

   ⛔ `mongod --shutdown` 在 **Windows 版不存在**（`mongod --help` 里没有这个选项）；便携版 zip 里也没有 `mongosh`。
   细节与实测日志 ⇒ `DocAgent/Godot/Feature/Launch脚本.md`
