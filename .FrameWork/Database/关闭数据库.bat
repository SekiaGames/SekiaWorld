@echo off
chcp 65001 >nul
setlocal EnableExtensions

set "MONGO_PORT=27017"
set "WAIT_MAX=5"

call :find_pid
if errorlevel 1 (
    echo [SKIP] MongoDB is not listening on port %MONGO_PORT%
    goto :done
)

echo [1/2] Stopping MongoDB ^(pid %MONGO_PID%, port %MONGO_PORT%^)
taskkill /PID %MONGO_PID% >nul 2>nul

call :wait_gone
if not errorlevel 1 goto :stopped

echo       Graceful stop did not finish, forcing
taskkill /PID %MONGO_PID% /F >nul 2>nul

call :wait_gone
if errorlevel 1 (
    echo [ERROR] pid %MONGO_PID% is still listening on port %MONGO_PORT%
    echo         Try again: taskkill /PID %MONGO_PID% /F
    goto :fail
)

:stopped
echo [2/2] MongoDB stopped, port %MONGO_PORT% is free
goto :done

:find_pid
set "MONGO_PID="
for /f "tokens=5" %%p in ('netstat -ano ^| findstr /C:":%MONGO_PORT% " ^| findstr /C:"LISTENING"') do set "MONGO_PID=%%p"
if not defined MONGO_PID exit /b 1
exit /b 0

:wait_gone
set /a "W=0"
:wait_gone_loop
call :find_pid
if errorlevel 1 exit /b 0
set /a "W+=1"
if %W% GEQ %WAIT_MAX% exit /b 1
ping -n 2 127.0.0.1 >nul
goto :wait_gone_loop

:done
echo.
pause
exit /b 0

:fail
pause
exit /b 1
