@echo off
setlocal EnableExtensions EnableDelayedExpansion
chcp 65001 >nul
title ColonyKit 安装到 RimWorld

rem ============================================================
rem  ColonyKit 便携安装脚本
rem  用法：install.bat [RimWorld 路径] [/auto]
rem  /auto 不弹 Harmony 提示、结束不暂停（供静默安装/自测）
rem ============================================================

set "ROOT=%~dp0"
set "SRC=%ROOT%ColonyKit"
if not exist "%SRC%\About\About.xml" if exist "%ROOT%About\About.xml" set "SRC=%ROOT%"
set "AUTO="
set "RW="

:parse
if "%~1"=="" goto :parsed
if /i "%~1"=="/auto" (
	set "AUTO=1"
	shift
	goto :parse
)
if /i "%~1"=="-auto" (
	set "AUTO=1"
	shift
	goto :parse
)
if not defined RW set "RW=%~1"
shift
goto :parse
:parsed

echo ============================================================
echo   ColonyKit 殖民地套件 - 安装到 RimWorld
echo ============================================================
echo.

if not exist "%SRC%\About\About.xml" (
	echo [错误] 找不到 Mod 文件：%SRC%\About\About.xml
	echo        请先把整个 zip 完整解压，再双击 install.bat。
	echo.
	if not defined AUTO pause
	exit /b 1
)

rem ---------- 1. 定位 RimWorld ----------
if defined RW if exist "%RW%\RimWorldWin64.exe" goto :found
if defined RIMWORLD if exist "%RIMWORLD%\RimWorldWin64.exe" (
	set "RW=%RIMWORLD%"
	goto :found
)

rem Steam 安装路径（注册表）
set "STEAM="
for /f "tokens=2,*" %%A in ('reg query "HKCU\Software\Valve\Steam" /v SteamPath 2^>nul ^| findstr /i "SteamPath"') do set "STEAM=%%B"
if not defined STEAM for /f "tokens=2,*" %%A in ('reg query "HKLM\SOFTWARE\WOW6432Node\Valve\Steam" /v InstallPath 2^>nul ^| findstr /i "InstallPath"') do set "STEAM=%%B"
if defined STEAM (
	set "STEAM=!STEAM:/=\!"
	if exist "!STEAM!\steamapps\common\RimWorld\RimWorldWin64.exe" set "RW=!STEAM!\steamapps\common\RimWorld"
)
if defined RW goto :found

rem 常见 Steam / GOG 路径
for %%D in (C D E F G H) do (
	call :try_rw "%%D:\SteamLibrary\steamapps\common\RimWorld"
	call :try_rw "%%D:\Steam\steamapps\common\RimWorld"
	call :try_rw "%%D:\Program Files (x86)\Steam\steamapps\common\RimWorld"
	call :try_rw "%%D:\Program Files\Steam\steamapps\common\RimWorld"
	call :try_rw "%%D:\Games\Steam\steamapps\common\RimWorld"
	call :try_rw "%%D:\GOG Games\RimWorld"
)
call :try_rw "%ProgramFiles(x86)%\Steam\steamapps\common\RimWorld"
call :try_rw "%ProgramFiles%\Steam\steamapps\common\RimWorld"
call :try_rw "%ProgramFiles(x86)%\GOG Galaxy\Games\RimWorld"
call :try_rw "%ProgramFiles%\GOG Galaxy\Games\RimWorld"
if defined RW goto :found

:ask_path
echo [提示] 没有自动找到 RimWorld。
echo        请把 RimWorld 文件夹（里面要有 RimWorldWin64.exe）拖到本窗口，然后回车。
echo        直接回车则退出。
echo.
set "RW="
set /p "RW=路径： "
if not defined RW goto :cancelled
set "RW=!RW:"=!"
if "!RW:~-1!"=="\" set "RW=!RW:~0,-1!"
if not exist "!RW!\RimWorldWin64.exe" (
	echo [错误] 这个目录里没有 RimWorldWin64.exe：!RW!
	echo.
	goto :ask_path
)

:found
if "!RW:~-1!"=="\" set "RW=!RW:~0,-1!"
if not exist "!RW!\RimWorldWin64.exe" (
	echo [错误] 不是有效的 RimWorld 目录：!RW!
	echo.
	goto :ask_path
)

echo [路径] !RW!

rem ---------- 2. 游戏必须已退出 ----------
tasklist /FI "IMAGENAME eq RimWorldWin64.exe" 2>nul | find /i "RimWorldWin64.exe" >nul
if not errorlevel 1 (
	echo.
	echo [错误] RimWorld 正在运行，请先完全退出游戏再安装。
	echo.
	if not defined AUTO pause
	exit /b 1
)

set "MODS=!RW!\Mods"
if not exist "!MODS!" mkdir "!MODS!" >nul 2>&1
if not exist "!MODS!" (
	echo [错误] 无法创建 Mods 目录：!MODS!
	echo.
	if not defined AUTO pause
	exit /b 1
)

rem ---------- 3. 检查 Harmony ----------
set "HARMONY="
if exist "!MODS!\Harmony\About\About.xml" set "HARMONY=1"
if not defined HARMONY if exist "!RW!\..\..\workshop\content\294100\2009463077\About\About.xml" set "HARMONY=1"
if not defined HARMONY (
	for /d %%D in ("!MODS!\*") do (
		if exist "%%~fD\About\About.xml" (
			findstr /i /c:"brrainz.harmony" "%%~fD\About\About.xml" >nul 2>&1
			if not errorlevel 1 set "HARMONY=1"
		)
	)
)
if defined HARMONY goto :harmony_done

echo.
echo [警告] 没有检测到 Harmony。
echo        ColonyKit 需要 Harmony 才能运行；请先在创意工坊订阅（免费）：
echo        https://steamcommunity.com/sharedfiles/filedetails/?id=2009463077
if defined AUTO goto :harmony_done
choice /c YN /n /m "现在打开 Steam 创意工坊页面？[Y/N] "
if errorlevel 2 goto :harmony_done
start "" "steam://url/CommunityFilePage/2009463077"

:harmony_done

rem ---------- 4. 备份旧版本 ----------
set "TARGET=!MODS!\ColonyKit"
if not exist "!TARGET!\About\About.xml" goto :copy
set "BAK="
for /l %%N in (1,1,99) do (
	if not defined BAK if not exist "!MODS!\ColonyKit.bak-%%N" set "BAK=!MODS!\ColonyKit.bak-%%N"
)
if not defined BAK (
	echo [错误] 备份目录太多（ColonyKit.bak-1..99），请先清理 %MODS%。
	echo.
	if not defined AUTO pause
	exit /b 1
)
echo [备份] 旧版本 -^> "!BAK!"
move /y "!TARGET!" "!BAK!" >nul
if errorlevel 1 (
	echo [错误] 备份失败，可能文件被占用。请关闭游戏、杀毒软件后重试。
	echo.
	if not defined AUTO pause
	exit /b 1
)

:copy
rem ---------- 5. 复制 ----------
echo [安装] "!SRC!"  -^>  "!TARGET!"
robocopy "!SRC!" "!TARGET!" /MIR /R:1 /W:1 /NFL /NDL /NJH /NJS /NP
set "RC=!ERRORLEVEL!"
if !RC! GEQ 8 (
	echo.
	echo [错误] 复制失败（robocopy 退出码 !RC!）。
	echo        请确认目标目录可写，并关闭占用文件的程序。
	echo.
	if not defined AUTO pause
	exit /b 1
)

if not exist "!TARGET!\About\About.xml" (
	echo [错误] 安装后缺少 About\About.xml，安装不完整。
	echo.
	if not defined AUTO pause
	exit /b 1
)
if not exist "!TARGET!\Assemblies\ColonyKit.dll" (
	echo [错误] 安装后缺少 Assemblies\ColonyKit.dll，安装不完整。
	echo.
	if not defined AUTO pause
	exit /b 1
)

set "VER="
for /f "tokens=3 delims=<>" %%V in ('findstr /i "<modVersion>" "!SRC!\About\About.xml"') do set "VER=%%V"
if not defined VER set "VER=?"

if exist "!MODS!\RimJobWorld\About\About.xml" (
	echo.
	echo [提示] 检测到 Mods\RimJobWorld。ColonyKit 已内置 RJW 1.6，
	echo        请不要同时启用外部 RimJobWorld，否则会重复加载 Def 报错。
)
set "CFG=%USERPROFILE%\AppData\LocalLow\Ludeon Studios\RimWorld by Ludeon Studios\Config\ModsConfig.xml"
if exist "!CFG!" (
	findstr /i /c:"rim.job.world" "!CFG!" >nul 2>&1
	if not errorlevel 1 (
		echo.
		echo [提示] 当前 ModsConfig.xml 仍启用外部 RimJobWorld（rim.job.world）。
		echo        进游戏后请在 Mod 设置里取消勾选它，只保留 ColonyKit。
	)
)

echo.
echo ============================================================
echo   安装完成：ColonyKit !VER!
echo ============================================================
echo   Mod 位置：!TARGET!
echo.
echo   接下来：
echo     1. 启动 RimWorld 1.6
echo     2. 进入「选项」-「Mod 设置」
echo     3. 勾选 Harmony，再勾选「殖民地套件」
echo     4. 点「自动排序」后按提示重启游戏
echo.
echo   注意：本 Mod 已内置 RJW 1.6，不要再启用外部 RimJobWorld。
echo ============================================================
echo.
if not defined AUTO pause
exit /b 0

:try_rw
if defined RW exit /b 0
if exist "%~1\RimWorldWin64.exe" set "RW=%~1"
exit /b 0

:cancelled
echo.
echo 已取消安装。
echo.
if not defined AUTO pause
exit /b 1
