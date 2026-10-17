@echo off
chcp 65001 >nul
setlocal

set "ROOT=%~dp0"
set "CSPROJ=%ROOT%Tools\ConfigTools\ConfigTools.csproj"
set "BINDIR=%ROOT%Bin"
set "BTJSON=%ROOT%Config\Excel_TestJsonExport\c\BTConfig.txt"

where dotnet >nul 2>nul
if errorlevel 1 (
    echo [ERROR] dotnet not found, please install .NET SDK 8.0
    goto :fail
)

echo === Build Tool ===
dotnet build "%CSPROJ%" -c Release -v q --nologo
if errorlevel 1 (
    echo [ERROR] Tool build failed
    goto :fail
)

if exist "%BTJSON%" (
    call :run BTImport
    if errorlevel 1 goto :fail
) else (
    echo === Skip BTImport: BTConfig json not found ===
)

call :run ExcelExporter
if errorlevel 1 goto :fail
call :run Proto2CS
if errorlevel 1 goto :fail

echo === Done ===
pause
exit /b 0

:fail
pause
exit /b 1

:run
echo === Run %1 ===
pushd "%BINDIR%"
dotnet Tool.dll %1
set "RC=%ERRORLEVEL%"
popd
if "%RC%"=="0" exit /b 0
echo [ERROR] %1 failed
exit /b 1
