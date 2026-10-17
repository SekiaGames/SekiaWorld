@echo off
chcp 65001 >nul
setlocal EnableExtensions

set "BUILD=1"
set "BUILD_CONFIG=Release"
set "MONGO_PORT=27017"

set "ROOT=%~dp0"
set "APPPROJ=%ROOT%Server\App\Server.App.csproj"
set "BINDIR=%ROOT%Bin"
set "MONGOD=%ROOT%Database\bin\mongod.exe"
set "MONGO_DBPATH=%ROOT%Database\db"
set "MONGO_LOG=%ROOT%Logs\mongod.log"
set "STARTCONFIG=%~1"
if "%STARTCONFIG%"=="" set "STARTCONFIG=Localhost"

where dotnet >nul 2>nul
if errorlevel 1 (
    echo [ERROR] dotnet not found, please install .NET SDK 8.0
    goto :fail
)

call :mongo_port_listening
if not errorlevel 1 (
    echo [1/3] MongoDB is already listening on port %MONGO_PORT%, skipping start
    goto :db_done
)

if not exist "%MONGOD%" (
    echo [ERROR] MongoDB not found: %MONGOD%
    echo         Put the MongoDB Community Server zip's bin folder under Database\bin.
    goto :fail
)

if not exist "%MONGO_DBPATH%" mkdir "%MONGO_DBPATH%"
if not exist "%ROOT%Logs" mkdir "%ROOT%Logs"

echo [1/3] Starting MongoDB on port %MONGO_PORT%, data dir %MONGO_DBPATH%
echo       Hidden background process (no window); log: Logs\mongod.log
powershell -NoProfile -ExecutionPolicy Bypass -Command "Start-Process -FilePath $env:MONGOD -ArgumentList '--dbpath', $env:MONGO_DBPATH, '--port', $env:MONGO_PORT, '--logpath', $env:MONGO_LOG, '--logappend' -WindowStyle Hidden"

:db_done
echo.

if "%BUILD%"=="0" (
    echo [2/3] Build skipped, using what is already in Bin
    goto :wait_db
)

if not exist "%APPPROJ%" (
    echo [ERROR] project not found: %APPPROJ%
    goto :fail
)

echo [2/3] dotnet build Server\App\Server.App.csproj -c %BUILD_CONFIG%
dotnet build "%APPPROJ%" -c %BUILD_CONFIG% -v q --nologo
if errorlevel 1 (
    echo [ERROR] server build failed, fix the errors above and run again
    goto :fail
)

:wait_db
echo.

call :mongo_wait_ready
if errorlevel 1 (
    echo [ERROR] waited 30s and mongod is still not listening on port %MONGO_PORT%
    echo         Check the tail of Logs\mongod.log for the reason.
    goto :fail
)

echo [3/3] dotnet App.dll --AppType=Server --Process=1 --StartConfig=StartConfig/%STARTCONFIG%
echo       Logs are written to Logs\ and Info+ is echoed into this window.
echo       Stop with Ctrl+C -- do not just close this window, or the ports
echo       may stay occupied.
echo.

pushd "%BINDIR%"
dotnet App.dll --AppType=Server --Process=1 --StartConfig=StartConfig/%STARTCONFIG%
set "RC=%ERRORLEVEL%"
popd

if "%RC%"=="0" exit /b 0

echo.
echo [3/3] server stopped with exit code %RC%
echo       Non-zero is normal after Ctrl+C or if the process was killed.
echo       Look in Logs\ only if the server went down on its own.
goto :fail

:mongo_port_listening
netstat -ano | findstr /C:":%MONGO_PORT% " | findstr /C:"LISTENING" >nul 2>nul
if errorlevel 1 exit /b 1
exit /b 0

:mongo_wait_ready
set /a "MONGO_WAIT=0"
:wait_loop
call :mongo_port_listening
if not errorlevel 1 exit /b 0
set /a "MONGO_WAIT+=1"
if %MONGO_WAIT% GEQ 30 exit /b 1
ping -n 2 127.0.0.1 >nul
goto :wait_loop

:fail
pause
exit /b 1
