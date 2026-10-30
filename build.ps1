#Requires -Version 5.1
<#
.SYNOPSIS
	构建 Colony Kit（正常构建：只编 ColonyKit.dll，不重编内置 RJW），可选部署到本机 Mods 目录。

.DESCRIPTION
	使用工作区内置的 .NET SDK（.tools/dotnet）与离线包目录（.tools/nuget）。

	内置 RJW 由独立工程 Source\Rjw\RimJobWorld.Main.csproj 产出 Assemblies\RJW.dll。
	本脚本传 -p:BuildRjw=false，跳过该子工程，直接复用上一次生成的 RJW.dll（快，适合改 ColonyKit 代码）。
	RJW 源码/资源有改动时请改用 .\build-with-rjw.ps1；同步 RJW 源码用 .\sync-rjw.ps1。

.PARAMETER Deploy
	构建成功后复制 About / Assemblies / Defs / Patches / Languages / Translations / Textures / Sounds / Blueprints
	到游戏 Mods\ColonyKit，并校验 ColonyKit.dll 与 RJW.dll 的 SHA256。
	游戏运行时会锁住 DLL，部署前会自动检查。

.PARAMETER Clean
	构建前删除 Source\obj、Source\bin、Source\Rjw\obj、Source\Rjw\bin；
	正常构建只删 Assemblies\ColonyKit.*（保留 RJW.dll），-WithRjw 时连 Assemblies 整个目录一起删。

.PARAMETER NoRebuild
	跳过 -t:Rebuild（日常增量构建用；发布/CI 不要用，避免漏掉编译警告）。

.PARAMETER WithRjw
	内部开关，由 build-with-rjw.ps1 转发；一般不要直接调用。

.EXAMPLE
	.\build.ps1
	.\build.ps1 -Clean
	.\build.ps1 -Deploy
#>
[CmdletBinding()]
param(
	[switch]$Deploy,
	[switch]$Clean,
	[switch]$NoRebuild,
	[switch]$WithRjw,
	[ValidateSet('Debug', 'Release')]
	[string]$Configuration = 'Release',
	[string]$ModsPath = 'E:\SteamLibrary\steamapps\common\RimWorld\Mods'
)

$ErrorActionPreference = 'Stop'

$root       = $PSScriptRoot
$workspace  = Split-Path (Split-Path $root -Parent) -Parent
$project    = Join-Path $root 'Source\ColonyKit.csproj'
$rjwProject = Join-Path $root 'Source\Rjw\RimJobWorld.Main.csproj'
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

# 正常构建不重编 RJW，必须已经有上一次的产物；否则进游戏只有空壳。
$rjwDll = Join-Path $assemblies 'RJW.dll'
if (-not $WithRjw -and -not (Test-Path $rjwDll)) {
	throw "缺少内置 RJW 的程序集：$rjwDll`n先运行 .\build-with-rjw.ps1 生成一次，之后的正常构建会直接复用它。"
}

$buildRjw = if ($WithRjw) { 'true' } else { 'false' }
if ($WithRjw -and -not (Test-Path $rjwProject)) { throw "RJW 工程文件不存在：$rjwProject" }

$env:DOTNET_CLI_TELEMETRY_OPTOUT = '1'
$env:DOTNET_NOLOGO = '1'

Push-Location (Join-Path $root 'Source')
try {
	if ($Clean) {
		foreach ($dir in 'obj', 'bin', 'Rjw\obj', 'Rjw\bin') {
			$path = Join-Path (Join-Path $root 'Source') $dir
			if (Test-Path $path) { Remove-Item $path -Recurse -Force }
		}
		if ($WithRjw) {
			if (Test-Path $assemblies) { Remove-Item $assemblies -Recurse -Force }
			Write-Host '[clean] obj / bin / Assemblies 已删除' -ForegroundColor DarkGray
		}
		else {
			# 正常构建不动 RJW.dll：它是上一次 build-with-rjw.ps1 的产物，删了就得重编。
			foreach ($leaf in 'ColonyKit.dll', 'ColonyKit.pdb', 'ColonyKit.xml') {
				$p = Join-Path $assemblies $leaf
				if (Test-Path $p) { Remove-Item $p -Force }
			}
			Write-Host '[clean] obj / bin / Assemblies\ColonyKit.* 已删除（保留 RJW.dll）' -ForegroundColor DarkGray
		}
	}

	# 用工作区的离线包目录；--packages 必须和首次 restore 的路径一致，
	# 否则 obj\project.assets.json 里记录的路径对不上，build 会重新找包。
	$assets = Join-Path (Join-Path $root 'Source') 'obj\project.assets.json'
	$rjwAssets = Join-Path (Join-Path $root 'Source\Rjw') 'obj\project.assets.json'
	$needRestore = -not (Test-Path $assets)
	if ($WithRjw -and -not (Test-Path $rjwAssets)) { $needRestore = $true }
	if ($needRestore) {
		Write-Host '[restore] dotnet restore' -ForegroundColor Cyan
		$restoreArgs = @('restore', $project, "-p:BuildRjw=$buildRjw", '--no-dependencies')
		# --no-dependencies：不带 -p 的嵌套 restore 会忽略上面的属性，把 RJW 又拉回来。
		if (Test-Path (Join-Path $workspace '.tools')) {
			$restoreArgs += @('--packages', $nuget)
		}
		& $dotnet @restoreArgs 2>&1 | Out-String | Write-Verbose
		if ($LASTEXITCODE -ne 0) { throw "restore 失败（exit $LASTEXITCODE）" }

		if ($WithRjw -and -not (Test-Path $rjwAssets)) {
			Write-Host '[restore] dotnet restore (RJW)' -ForegroundColor Cyan
			$rjwRestoreArgs = @('restore', $rjwProject)
			if (Test-Path (Join-Path $workspace '.tools')) {
				$rjwRestoreArgs += @('--packages', $nuget)
			}
			& $dotnet @rjwRestoreArgs 2>&1 | Out-String | Write-Verbose
			if ($LASTEXITCODE -ne 0) { throw "RJW restore 失败（exit $LASTEXITCODE）" }
		}
	}

	Write-Host ("[build] {0}（BuildRjw={1}）" -f $Configuration, $buildRjw) -ForegroundColor Cyan
	$buildArgs = @('build', $project, '-c', $Configuration, '--no-restore', "-p:BuildRjw=$buildRjw")
	if (-not $NoRebuild) { $buildArgs += '-t:Rebuild' }
	$log = & $dotnet @buildArgs 2>&1 | Out-String
	$exit = $LASTEXITCODE

	$errors   = [regex]::Matches($log, ': error ')
	$warnings = [regex]::Matches($log, ': warning ')
	Write-Host ("[build] 错误 {0} 个，警告 {1} 个" -f $errors.Count, $warnings.Count) -ForegroundColor Yellow

	if ($exit -ne 0 -or $errors.Count -gt 0 -or $warnings.Count -gt 0) {
		$log | Set-Content (Join-Path $root 'build-error.log') -Encoding utf8
		$log -split "`n" | Where-Object { $_ -match ': (error|warning) ' } | Select-Object -First 20 | Write-Host
		throw "编译失败或存在警告（exit $exit，error $($errors.Count)，warning $($warnings.Count)）：$root\build-error.log"
	}

	$dll = Join-Path $assemblies 'ColonyKit.dll'
	if (-not (Test-Path $dll)) { throw "未找到产物：$dll" }
	$file = Get-Item $dll
	Write-Host ("[ok] {0}  {1:N0} bytes" -f $file.FullName, $file.Length) -ForegroundColor Green
	$rjwFile = Get-Item $rjwDll -ErrorAction SilentlyContinue
	if ($rjwFile) {
		Write-Host ("[ok] {0}  {1:N0} bytes  {2:yyyy-MM-dd HH:mm}（{3}）" -f $rjwFile.FullName, $rjwFile.Length, $rjwFile.LastWriteTime, ($(if ($WithRjw) { '本次重编' } else { '复用既有产物' }))) -ForegroundColor Green
	}

	if ($Deploy) {
		# 游戏运行时 Windows 会锁住已加载的 DLL，robocopy 必然失败；先拦一道给出可读错误。
		if (Get-Process -Name 'RimWorldWin64' -ErrorAction SilentlyContinue) {
			throw 'RimWorld 正在运行，请先退出游戏再部署（DLL 会被锁住）。'
		}

		$target = Join-Path $ModsPath 'ColonyKit'
		Write-Host "[deploy] $target" -ForegroundColor Cyan
		# robocopy 的 0–7 都是成功码（1 表示确实复制了文件），8 及以上才是失败。
		# /MIR 同步删除源里已移除的旧文件，避免重命名 Def/Worker 后留下陈旧 XML。
		foreach ($name in 'About', 'Assemblies', 'Defs', 'Patches', 'Languages', 'Translations', 'Textures', 'Sounds', 'Blueprints') {
			$src = Join-Path $root $name
			if (-not (Test-Path $src)) { continue }
			& robocopy $src (Join-Path $target $name) /MIR /NFL /NDL /NJH /NJS /NP | Out-Null
			if ($LASTEXITCODE -ge 8) { throw "robocopy 失败：$name（exit $LASTEXITCODE）" }
		}

		foreach ($pair in @(@{ Src = $dll; Dst = (Join-Path $target 'Assemblies\ColonyKit.dll') }, @{ Src = $rjwDll; Dst = (Join-Path $target 'Assemblies\RJW.dll') })) {
			if (-not (Test-Path $pair.Src)) { throw "部署校验失败：源文件不存在 $($pair.Src)" }
			$hashSrc = (Get-FileHash $pair.Src -Algorithm SHA256).Hash
			$hashDst = (Get-FileHash $pair.Dst -Algorithm SHA256).Hash
			if ($hashSrc -ne $hashDst) { throw "部署校验失败：DLL 哈希不一致 $($pair.Dst)" }
		}
		Write-Host '[deploy] 完成，DLL 哈希已校验' -ForegroundColor Green
	}
}
finally {
	Pop-Location
}

# robocopy 的成功码是 0–7，直接在调用方看 $LASTEXITCODE 会误判为失败。
$global:LASTEXITCODE = 0
