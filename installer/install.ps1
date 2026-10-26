#Requires -Version 5.1
<#
.SYNOPSIS
	ColonyKit 便携安装脚本（由同目录 install.bat 调用）。

.DESCRIPTION
	自动定位 RimWorld、备份旧版、复制到 Mods\ColonyKit，
	并检查 Harmony、外部 RimJobWorld 与安装结果。
	成功信息用绿色，错误用红色，警告用黄色。

	用法：
		install.bat [RimWorld 路径] [/auto]
		install.ps1 [RimWorld 路径] [/auto]
#>

$ErrorActionPreference = 'Stop'

# ---- 参数：路径 + /auto（兼容 cmd 风格） ----
$RimWorldPath = $null
$Auto = $false
foreach ($arg in $args) {
	if ($arg -eq '/auto' -or $arg -eq '-auto' -or $arg -eq '--auto') {
		$Auto = $true
	}
	elseif ([string]::IsNullOrWhiteSpace($RimWorldPath)) {
		$RimWorldPath = $arg
	}
}

try {
	[Console]::OutputEncoding = [System.Text.Encoding]::UTF8
	$OutputEncoding = [System.Text.Encoding]::UTF8
}
catch { }

function Write-Ok   { param([string]$Text) Write-Host $Text -ForegroundColor Green }
function Write-Err  { param([string]$Text) Write-Host $Text -ForegroundColor Red }
function Write-Warn { param([string]$Text) Write-Host $Text -ForegroundColor Yellow }
function Write-Info { param([string]$Text) Write-Host $Text -ForegroundColor Cyan }

# 把用户给的路径归一成 RimWorld 根目录；不是有效目录时返回 $null。
function Resolve-RimWorldPath {
	param([string]$Path)
	if ([string]::IsNullOrWhiteSpace($Path)) { return $null }
	$p = $Path.Trim().Trim('"')
	if ($p.EndsWith('\')) { $p = $p.TrimEnd('\') }
	if (Test-Path -LiteralPath $p -PathType Leaf) { $p = Split-Path -Parent $p }
	$nested = Join-Path $p 'RimWorld'
	if (Test-Path -LiteralPath (Join-Path $nested 'RimWorldWin64.exe')) { return $nested }
	if (Test-Path -LiteralPath (Join-Path $p 'RimWorldWin64.exe')) { return $p }
	return $null
}

function Test-HarmonyInstalled {
	param([string]$ModsDir, [string]$RimWorldDir)
	if (Test-Path -LiteralPath (Join-Path $ModsDir 'Harmony\About\About.xml')) { return $true }
	$workshop = Join-Path $RimWorldDir '..\..\workshop\content\294100\2009463077\About\About.xml'
	if (Test-Path -LiteralPath $workshop) { return $true }
	foreach ($dir in Get-ChildItem -LiteralPath $ModsDir -Directory -ErrorAction SilentlyContinue) {
		$about = Join-Path $dir.FullName 'About\About.xml'
		if (-not (Test-Path -LiteralPath $about)) { continue }
		if (Select-String -LiteralPath $about -Pattern 'brrainz\.harmony' -Quiet -ErrorAction SilentlyContinue) {
			return $true
		}
	}
	return $false
}

function Find-RimWorld {
	if ($script:RimWorldPath) {
		$found = Resolve-RimWorldPath $script:RimWorldPath
		if ($found) { return $found }
		Write-Warn "[提示] 指定路径无效：$($script:RimWorldPath)，改为自动查找。"
	}
	if ($env:RIMWORLD) {
		$found = Resolve-RimWorldPath $env:RIMWORLD
		if ($found) { return $found }
	}

	# Steam 客户端路径
	$steam = $null
	foreach ($key in @('HKCU:\Software\Valve\Steam', 'HKLM:\SOFTWARE\WOW6432Node\Valve\Steam')) {
		$props = Get-ItemProperty -Path $key -ErrorAction SilentlyContinue
		if ($props) {
			if ($props.SteamPath) { $steam = $props.SteamPath }
			elseif ($props.InstallPath) { $steam = $props.InstallPath }
			if ($steam) { break }
		}
	}
	if ($steam) {
		$steam = $steam -replace '/', '\'
		$found = Resolve-RimWorldPath (Join-Path $steam 'steamapps\common\RimWorld')
		if ($found) { return $found }

		# 其它 Steam 库（libraryfolders.vdf）
		$vdf = Join-Path $steam 'steamapps\libraryfolders.vdf'
		if (Test-Path -LiteralPath $vdf) {
			foreach ($line in Get-Content -LiteralPath $vdf -ErrorAction SilentlyContinue) {
				if ($line -match '"path"\s+"([^"]+)"') {
					$lib = $matches[1].Replace('\\', '\')
					$found = Resolve-RimWorldPath (Join-Path $lib 'steamapps\common\RimWorld')
					if ($found) { return $found }
				}
			}
		}
	}

	# 常见路径
	$candidates = New-Object System.Collections.Generic.List[string]
	foreach ($d in @('C', 'D', 'E', 'F', 'G', 'H')) {
		$candidates.Add("$d`:\SteamLibrary\steamapps\common\RimWorld")
		$candidates.Add("$d`:\Steam\steamapps\common\RimWorld")
		$candidates.Add("$d`:\Program Files (x86)\Steam\steamapps\common\RimWorld")
		$candidates.Add("$d`:\Program Files\Steam\steamapps\common\RimWorld")
		$candidates.Add("$d`:\Games\Steam\steamapps\common\RimWorld")
		$candidates.Add("$d`:\GOG Games\RimWorld")
	}
	$pf86 = [Environment]::GetFolderPath('ProgramFilesX86')
	$pf   = [Environment]::GetFolderPath('ProgramFiles')
	if ($pf86) {
		$candidates.Add((Join-Path $pf86 'Steam\steamapps\common\RimWorld'))
		$candidates.Add((Join-Path $pf86 'GOG Galaxy\Games\RimWorld'))
	}
	if ($pf) {
		$candidates.Add((Join-Path $pf 'Steam\steamapps\common\RimWorld'))
		$candidates.Add((Join-Path $pf 'GOG Galaxy\Games\RimWorld'))
	}
	foreach ($c in $candidates) {
		$found = Resolve-RimWorldPath $c
		if ($found) { return $found }
	}

	# 手动输入
	while ($true) {
		Write-Host ''
		Write-Warn '[提示] 没有自动找到 RimWorld。'
		Write-Host '       请把 RimWorld 文件夹（里面有 RimWorldWin64.exe）拖到本窗口，然后回车。'
		Write-Host '       直接回车则退出。'
		$inputPath = Read-Host '路径'
		if ([string]::IsNullOrWhiteSpace($inputPath)) { throw '已取消安装。' }
		$found = Resolve-RimWorldPath $inputPath
		if ($found) { return $found }
		Write-Err "[错误] 这个目录里没有 RimWorldWin64.exe：$inputPath"
	}
}

$exitCode = 0
try {
	$root = $PSScriptRoot
	$src = Join-Path $root 'ColonyKit'
	if (-not (Test-Path -LiteralPath (Join-Path $src 'About\About.xml'))) {
		if (Test-Path -LiteralPath (Join-Path $root 'About\About.xml')) {
			$src = $root
		}
		else {
			throw "找不到 Mod 文件：$src\About\About.xml。请先把整个 zip 完整解压，再双击 install.bat。"
		}
	}

	Write-Info '============================================================'
	Write-Info '  ColonyKit 殖民地套件 - 安装到 RimWorld'
	Write-Info '============================================================'
	Write-Host ''

	$rw = Find-RimWorld
	Write-Info "[路径] $rw"

	if (Get-Process -Name 'RimWorldWin64' -ErrorAction SilentlyContinue) {
		throw 'RimWorld 正在运行，请先完全退出游戏再安装。'
	}

	$mods = Join-Path $rw 'Mods'
	if (-not (Test-Path -LiteralPath $mods)) {
		New-Item -ItemType Directory -Path $mods -Force | Out-Null
	}
	if (-not (Test-Path -LiteralPath $mods)) {
		throw "无法创建 Mods 目录：$mods"
	}

	# ---- Harmony：已装则跳过；没装则复制随包附带的 Harmony ----
	$harmony = Test-HarmonyInstalled -ModsDir $mods -RimWorldDir $rw
	if (-not $harmony) {
		$bundled = Join-Path $PSScriptRoot 'Harmony'
		$bundledDll = Join-Path $bundled '1.6\Assemblies\0Harmony.dll'
		if ((Test-Path -LiteralPath (Join-Path $bundled 'About\About.xml')) -and (Test-Path -LiteralPath $bundledDll)) {
			$harmonyTarget = Join-Path $mods 'Harmony'
			if (Test-Path -LiteralPath $harmonyTarget) {
				$hbak = $null
				for ($n = 1; $n -le 99; $n++) {
					$candidate = Join-Path $mods "Harmony.bak-$n"
					if (-not (Test-Path -LiteralPath $candidate)) { $hbak = $candidate; break }
				}
				if (-not $hbak) { throw 'Harmony 备份目录太多（Harmony.bak-1..99），请先清理 Mods。' }
				Write-Host "[备份] 旧 Harmony 目录 -> $hbak" -ForegroundColor Yellow
				Move-Item -LiteralPath $harmonyTarget -Destination $hbak -ErrorAction Stop
			}
			Write-Host "[Harmony] 安装随包附带的 Harmony -> $harmonyTarget" -ForegroundColor Cyan
			Copy-Item -LiteralPath $bundled -Destination $harmonyTarget -Recurse -Force
			$harmony = Test-HarmonyInstalled -ModsDir $mods -RimWorldDir $rw
			if (-not $harmony) { throw '随包 Harmony 复制失败，请检查 Mods 目录权限。' }
		}
	}
	if (-not $harmony) {
		Write-Host ''
		Write-Warn '[警告] 没有检测到 Harmony，且随包 Harmony 缺失。'
		Write-Host '       ColonyKit 需要 Harmony 才能运行；请先在创意工坊订阅（免费）：'
		Write-Host '       https://steamcommunity.com/sharedfiles/filedetails/?id=2009463077'
		if (-not $Auto) {
			$answer = Read-Host '现在打开 Steam 创意工坊页面？[Y/N]'
			if ($answer -match '^[Yy]') {
				try { Start-Process 'steam://url/CommunityFilePage/2009463077' } catch { }
			}
		}
	}

	# ---- 备份旧版本 ----
	$target = Join-Path $mods 'ColonyKit'
	if (Test-Path -LiteralPath (Join-Path $target 'About\About.xml')) {
		$bak = $null
		for ($i = 1; $i -le 99; $i++) {
			$candidate = Join-Path $mods "ColonyKit.bak-$i"
			if (-not (Test-Path -LiteralPath $candidate)) { $bak = $candidate; break }
		}
		if (-not $bak) { throw '备份目录太多（ColonyKit.bak-1..99），请先清理 Mods。' }
		Write-Host "[备份] 旧版本 -> $bak" -ForegroundColor Yellow
		Move-Item -LiteralPath $target -Destination $bak -ErrorAction Stop
	}

	# ---- 复制 ----
	Write-Host "[安装] $src  ->  $target" -ForegroundColor Cyan
	& robocopy $src $target /MIR /R:1 /W:1 /NFL /NDL /NJH /NJS /NP | Out-Null
	$rc = $LASTEXITCODE
	if ($rc -ge 8) {
		throw "复制失败（robocopy 退出码 $rc）。请确认目标目录可写，并关闭占用文件的程序。"
	}

	if (-not (Test-Path -LiteralPath (Join-Path $target 'About\About.xml'))) {
		throw '安装后缺少 About\About.xml，安装不完整。'
	}
	if (-not (Test-Path -LiteralPath (Join-Path $target 'Assemblies\ColonyKit.dll'))) {
		throw '安装后缺少 Assemblies\ColonyKit.dll，安装不完整。'
	}

	$ver = '?'
	try {
		[xml]$about = Get-Content -LiteralPath (Join-Path $src 'About\About.xml') -Encoding UTF8
		if ($about.ModMetaData.modVersion) { $ver = $about.ModMetaData.modVersion }
	}
	catch { }

	# ---- 外部 RJW 检查 ----
	$rjwFolder = Test-Path -LiteralPath (Join-Path $mods 'RimJobWorld\About\About.xml')
	$cfg = Join-Path $env:USERPROFILE 'AppData\LocalLow\Ludeon Studios\RimWorld by Ludeon Studios\Config\ModsConfig.xml'
	$rjwConfig = $false
	if (Test-Path -LiteralPath $cfg) {
		if (Select-String -LiteralPath $cfg -Pattern 'rim\.job\.world' -Quiet -ErrorAction SilentlyContinue) {
			$rjwConfig = $true
		}
	}
	if ($rjwFolder -or $rjwConfig) {
		Write-Host ''
		Write-Warn '[提示] 检测到外部 RimJobWorld（Mods 目录或 ModsConfig.xml）。'
		Write-Host '       ColonyKit 已内置 RJW 1.6，请不要再启用外部 RimJobWorld，否则会重复加载 Def 报错。'
		if ($rjwConfig) {
			Write-Host '       进游戏后请在 Mod 设置里取消勾选 rim.job.world，只保留 ColonyKit。'
		}
	}

	Write-Host ''
	Write-Ok '============================================================'
	Write-Ok "  安装完成：ColonyKit $ver"
	Write-Ok '============================================================'
	Write-Ok "  Mod 位置：$target"
	Write-Ok ''
	Write-Ok '  接下来：'
	Write-Ok '    1. 启动 RimWorld 1.6'
	Write-Ok '    2. 进入「选项」-「Mod 设置」'
	Write-Ok '    3. 勾选 Harmony，再勾选「殖民地套件」'
	Write-Ok '    4. 点「自动排序」后按提示重启游戏'
	Write-Ok ''
	Write-Ok '  注意：本 Mod 已内置 RJW 1.6，不要再启用外部 RimJobWorld。'
	Write-Ok '============================================================'
	Write-Host ''
}
catch {
	Write-Host ''
	Write-Err '============================================================'
	Write-Err ('[错误] ' + $_.Exception.Message)
	Write-Err '============================================================'
	Write-Host ''
	$exitCode = 1
}

if (-not $Auto) {
	try { [void](Read-Host '按回车键退出') } catch { }
}
exit $exitCode
