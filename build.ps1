#Requires -Version 5.1
<#
.SYNOPSIS
	构建 Mod Template，可选部署到本机 RimWorld 1.6 的 Mods 目录。

.DESCRIPTION
	使用工作区内置的 .NET SDK（.tools/dotnet）与离线包目录（.tools/nuget）。
	产物输出到 Assemblies\ModTemplate.dll。

.PARAMETER Deploy
	构建成功后复制 About / Assemblies / Defs / Patches / Languages 到游戏 Mods\ModTemplate。
	游戏运行时会锁住 DLL，部署前会自动检查。

.PARAMETER Clean
	构建前删除 Source\obj、Source\bin、Assemblies。

.EXAMPLE
	.\build.ps1
	.\build.ps1 -Clean
	.\build.ps1 -Deploy
#>
[CmdletBinding()]
param(
	[switch]$Deploy,
	[switch]$Clean,
	[ValidateSet('Debug', 'Release')]
	[string]$Configuration = 'Release',
	[string]$ModsPath = 'E:\SteamLibrary\steamapps\common\RimWorld\Mods'
)

$ErrorActionPreference = 'Stop'

$root       = $PSScriptRoot
$workspace  = Split-Path (Split-Path $root -Parent) -Parent
$project    = Join-Path $root 'Source\ModTemplate.csproj'
$assemblies = Join-Path $root 'Assemblies'
$dotnet     = Join-Path $workspace '.tools\dotnet\dotnet.exe'
$nuget      = Join-Path $workspace '.tools\nuget'

function Resolve-Dotnet {
	if (Test-Path $dotnet) { return $dotnet }
	$cmd = Get-Command dotnet -ErrorAction SilentlyContinue
	if ($cmd) { return $cmd.Source }
	throw "未找到 .NET SDK：$dotnet（或 PATH 里的 dotnet）"
}

if (-not (Test-Path $project)) { throw "工程文件不存在：$project" }
$dotnet = Resolve-Dotnet

$env:DOTNET_CLI_TELEMETRY_OPTOUT = '1'
$env:DOTNET_NOLOGO = '1'

Push-Location (Join-Path $root 'Source')
try {
	if ($Clean) {
		foreach ($dir in 'obj', 'bin') {
			$path = Join-Path (Join-Path $root 'Source') $dir
			if (Test-Path $path) { Remove-Item $path -Recurse -Force }
		}
		if (Test-Path $assemblies) { Remove-Item $assemblies -Recurse -Force }
		Write-Host '[clean] obj / bin / Assemblies 已删除' -ForegroundColor DarkGray
	}

	# 用工作区的离线包目录；--packages 必须和首次 restore 的路径一致，
	# 否则 obj\project.assets.json 里记录的路径对不上，build 会重新找包。
	$assets = Join-Path (Join-Path $root 'Source') 'obj\project.assets.json'
	if (-not (Test-Path $assets)) {
		Write-Host '[restore] dotnet restore' -ForegroundColor Cyan
		& $dotnet restore $project --packages $nuget 2>&1 | Out-String | Write-Verbose
		if ($LASTEXITCODE -ne 0) { throw "restore 失败（exit $LASTEXITCODE）" }
	}

	Write-Host "[build] $Configuration" -ForegroundColor Cyan
	$log = & $dotnet build $project -c $Configuration --no-restore 2>&1 | Out-String
	$exit = $LASTEXITCODE

	$errors   = [regex]::Matches($log, ': error ')
	$warnings = [regex]::Matches($log, ': warning ')
	Write-Host ("[build] 错误 {0} 个，警告 {1} 个" -f $errors.Count, $warnings.Count) -ForegroundColor Yellow

	if ($exit -ne 0 -or $errors.Count -gt 0) {
		$log | Set-Content (Join-Path $root 'build-error.log') -Encoding utf8
		$log -split "`n" | Where-Object { $_ -match ': error ' } | Select-Object -First 20 | Write-Host
		throw "编译失败（exit $exit），完整日志：$root\build-error.log"
	}

	$dll = Join-Path $assemblies 'ModTemplate.dll'
	if (-not (Test-Path $dll)) { throw "未找到产物：$dll" }
	$file = Get-Item $dll
	Write-Host ("[ok] {0}  {1:N0} bytes" -f $file.FullName, $file.Length) -ForegroundColor Green

	if ($Deploy) {
		# 游戏运行时 Windows 会锁住已加载的 DLL，robocopy 必然失败；先拦一道给出可读错误。
		if (Get-Process -Name 'RimWorldWin64' -ErrorAction SilentlyContinue) {
			throw 'RimWorld 正在运行，请先退出游戏再部署（DLL 会被锁住）。'
		}

		$target = Join-Path $ModsPath 'ModTemplate'
		Write-Host "[deploy] $target" -ForegroundColor Cyan
		# robocopy 的 0–7 都是成功码（1 表示确实复制了文件），8 及以上才是失败。
		foreach ($name in 'About', 'Assemblies', 'Defs', 'Patches', 'Languages', 'Textures', 'Sounds') {
			$src = Join-Path $root $name
			if (-not (Test-Path $src)) { continue }
			& robocopy $src (Join-Path $target $name) /E /NFL /NDL /NJH /NJS /NP | Out-Null
			if ($LASTEXITCODE -ge 8) { throw "robocopy 失败：$name（exit $LASTEXITCODE）" }
		}

		$deployed = Join-Path $target 'Assemblies\ModTemplate.dll'
		$hashSrc = (Get-FileHash $dll -Algorithm SHA256).Hash
		$hashDst = (Get-FileHash $deployed -Algorithm SHA256).Hash
		if ($hashSrc -ne $hashDst) { throw '部署校验失败：DLL 哈希不一致' }
		Write-Host '[deploy] 完成，DLL 哈希已校验' -ForegroundColor Green
	}
}
finally {
	Pop-Location
}
