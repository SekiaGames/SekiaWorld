@echo off
chcp 65001 >nul
setlocal enabledelayedexpansion
set "HERE=%~dp0"
set "BASH="
if exist "%ProgramFiles%\Git\bin\bash.exe" set "BASH=%ProgramFiles%\Git\bin\bash.exe"
if not defined BASH for /f "delims=" %%i in ('where bash 2^>nul') do if not defined BASH set "BASH=%%i"
if not defined BASH echo [X] bash not found. Install Git for Windows first.
if not defined BASH pause
if not defined BASH exit /b 1

set "CASE=%*"
if not "%CASE%"=="" goto :run

echo.
echo   可选的测试用例：
set "N=0"
for %%f in ("%HERE%cases\*.sh") do (
    set /a N+=1
    echo     !N!. %%~nf
    set "PICK!N!=%%~nf"
)
echo.
set /p "SEL=  输入编号或用例名（回车 = 默认用例）："
echo.
if "!SEL!"=="" goto :run
set "CASE=!PICK%SEL%!"
if not defined CASE set "CASE=!SEL!"

:run
"%BASH%" "%HERE%run.sh" %CASE%
set "RC=%ERRORLEVEL%"
echo.
pause
exit /b %RC%
