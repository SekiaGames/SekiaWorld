@echo off
chcp 65001 >nul
setlocal EnableExtensions

set "MONGO_PORT=27017"
set "ROOT=%~dp0"
set "MONGOD=%ROOT%bin\mongod.exe"
set "MONGO_DBPATH=%ROOT%db"
for %%i in ("%ROOT%..\Logs") do set "LOGSDIR=%%~fi"
set "MONGO_LOG=%LOGSDIR%\mongod.log"

if not exist "%MONGOD%" (
    echo [ERROR] mongod.exe not found: %MONGOD%
    echo         Put the MongoDB Community Server zip's bin folder under Database\bin.
    goto :fail
)

call :port_listening
if not errorlevel 1 (
    echo [SKIP] MongoDB is already listening on port %MONGO_PORT%
    goto :done
)

if not exist "%MONGO_DBPATH%" mkdir "%MONGO_DBPATH%"
if not exist "%LOGSDIR%" mkdir "%LOGSDIR%"

echo [1/2] Starting MongoDB on port %MONGO_PORT%
echo       data dir : %MONGO_DBPATH%
echo       log file : %MONGO_LOG%
echo       Hidden background process, no window.
powershell -NoProfile -ExecutionPolicy Bypass -Command "Start-Process -FilePath $env:MONGOD -ArgumentList '--dbpath', $env:MONGO_DBPATH, '--port', $env:MONGO_PORT, '--logpath', $env:MONGO_LOG, '--logappend' -WindowStyle Hidden"

call :wait_ready
if errorlevel 1 (
    echo [ERROR] waited 30s and mongod is still not listening on port %MONGO_PORT%
    echo         Check the tail of %MONGO_LOG% for the reason.
    goto :fail
)

call :find_pid
echo [2/2] MongoDB is ready on port %MONGO_PORT% ^(pid %MONGO_PID%^)
goto :done

:port_listening
netstat -ano | findstr /C:":%MONGO_PORT% " | findstr /C:"LISTENING" >nul 2>nul
if errorlevel 1 exit /b 1
exit /b 0

:find_pid
set "MONGO_PID="
for /f "tokens=5" %%p in ('netstat -ano ^| findstr /C:":%MONGO_PORT% " ^| findstr /C:"LISTENING"') do set "MONGO_PID=%%p"
if not defined MONGO_PID exit /b 1
exit /b 0

:wait_ready
set /a "W=0"
:wait_ready_loop
call :port_listening
if not errorlevel 1 exit /b 0
set /a "W+=1"
if %W% GEQ 30 exit /b 1
ping -n 2 127.0.0.1 >nul
goto :wait_ready_loop

:done
echo.
pause
exit /b 0

:fail
pause
exit /b 1
