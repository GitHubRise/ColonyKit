#Requires -Version 5.1
<#
.SYNOPSIS
	Headless smoke test for the Mod Template: deploy, quicktest, assert log markers.

.DESCRIPTION
	Requires a local RimWorld 1.6 install and a closed game process.
	The test creates an isolated -savedatafolder so the user's config is untouched.
	It runs build.ps1 -Deploy, starts RimWorld with -quicktest -modtemplate-selftest,
	waits for the self-test and diagnostics markers, then closes the game.

	Not suitable for GitHub-hosted CI (no RimWorld install); use a self-hosted runner.

.PARAMETER GamePath
	RimWorld install directory (contains RimWorldWin64.exe).

.PARAMETER ModsPath
	Mods directory; defaults to <GamePath>\Mods.

.PARAMETER TimeoutSeconds
	Maximum time to wait for the self-test markers.

.PARAMETER SkipBuild
	Reuse the currently deployed build instead of building/deploying.

.PARAMETER KeepTemp
	Keep the temporary savedata/log directory even on success.
#>
[CmdletBinding()]
param(
	[string]$GamePath = 'E:\SteamLibrary\steamapps\common\RimWorld',
	[string]$ModsPath,
	[int]$TimeoutSeconds = 180,
	[switch]$SkipBuild,
	[switch]$KeepTemp
)

$ErrorActionPreference = 'Stop'
$root = $PSScriptRoot
if ([string]::IsNullOrWhiteSpace($ModsPath)) {
	$ModsPath = Join-Path $GamePath 'Mods'
}

$exe = Join-Path $GamePath 'RimWorldWin64.exe'
if (-not (Test-Path $exe)) {
	throw "RimWorld executable not found: $exe"
}

if (Get-Process -Name 'RimWorldWin64' -ErrorAction SilentlyContinue) {
	throw 'RimWorld is running. Close it before running the smoke test.'
}

if (-not $SkipBuild) {
	Write-Host '[smoke] verify' -ForegroundColor Cyan
	& (Join-Path $root 'verify.ps1')
	if ($LASTEXITCODE -ne 0) { throw 'verify.ps1 failed.' }

	Write-Host '[smoke] build + deploy' -ForegroundColor Cyan
	& (Join-Path $root 'build.ps1') -Deploy -ModsPath $ModsPath
	if ($LASTEXITCODE -ne 0) { throw 'build.ps1 failed.' }
}

$runDir = Join-Path $env:TEMP ('ModTemplate-smoke-' + [guid]::NewGuid().ToString('N'))
$configDir = Join-Path $runDir 'Config'
New-Item -ItemType Directory -Force -Path $configDir | Out-Null

$modsConfig = @'
<?xml version="1.0" encoding="utf-8"?>
<ModsConfigData>
  <version>1.6.4871 rev590</version>
  <activeMods>
    <li>ludeon.rimworld</li>
    <li>brrainz.harmony</li>
    <li>local.modtemplate</li>
  </activeMods>
  <knownExpansions />
</ModsConfigData>
'@
[IO.File]::WriteAllText((Join-Path $configDir 'ModsConfig.xml'), $modsConfig, [Text.UTF8Encoding]::new($false))

$settings = @'
<?xml version="1.0" encoding="utf-8"?>
<SettingsBlock>
	<ModSettings Class="ModTemplate.ModTemplateSettings">
		<general Class="ModTemplate.GeneralSettings">
			<trackSpawns>True</trackSpawns>
			<enablePawnMenu>True</enablePawnMenu>
			<showTechTreeButton>True</showTechTreeButton>
			<controlMode>Dropdown</controlMode>
			<language>Auto</language>
		</general>
		<logging Class="ModTemplate.LoggingSettings">
			<detail>Verbose</detail>
			<logLevel>Debug</logLevel>
			<verboseSpawnLog>True</verboseSpawnLog>
			<logIntervalTicks>600</logIntervalTicks>
			<customPrefix></customPrefix>
		</logging>
		<notifications Class="ModTemplate.NotificationSettings">
			<enabled>True</enabled>
			<cooldownTicks>300</cooldownTicks>
		</notifications>
	</ModSettings>
</SettingsBlock>
'@
[IO.File]::WriteAllText((Join-Path $configDir 'Mod_ModTemplate_ModTemplateMod.xml'), $settings, [Text.UTF8Encoding]::new($false))

$logPath = Join-Path $runDir 'Player.log'
$arguments = @(
	'-quicktest',
	'-modtemplate-selftest',
	('-savedatafolder={0}' -f $runDir),
	'-logfile',
	$logPath
)

$process = $null
$passed = $false
try {
	Write-Host "[smoke] start $exe" -ForegroundColor Cyan
	$process = Start-Process -FilePath $exe -ArgumentList $arguments -PassThru

	$deadline = (Get-Date).AddSeconds($TimeoutSeconds)
	while ((Get-Date) -lt $deadline) {
		if ($process.HasExited) { break }
		if (Test-Path $logPath) {
			$text = Get-Content $logPath -Raw -Encoding UTF8 -ErrorAction SilentlyContinue
			if ($text -match '\[ModTemplate\]\[Selftest\]' -and $text -match '\[ModTemplate\]\[Diag\]') {
				break
			}
		}
		Start-Sleep -Seconds 2
	}

	# Give the diagnostics file a moment to flush before closing.
	Start-Sleep -Seconds 2
}
finally {
	if ($process -and -not $process.HasExited) {
		$null = $process.CloseMainWindow()
		for ($i = 0; $i -lt 20 -and -not $process.HasExited; $i++) {
			Start-Sleep -Milliseconds 500
		}
		if (-not $process.HasExited) {
			Stop-Process -Id $process.Id -Force
			Start-Sleep -Seconds 1
		}
	}
}

try {
	if (-not (Test-Path $logPath)) {
		throw "Player.log not found: $logPath"
	}

	$log = Get-Content $logPath -Raw -Encoding UTF8
	$failures = New-Object System.Collections.Generic.List[string]

	$required = @(
		'\[ModTemplate\]\[Bootstrap\] loaded',
		'\[ModTemplate\]\[Migration\] settings migrated 0 -> 2',
		'\[ModTemplate\]\[Selftest\] modules=6, schema=2',
		'\[ModTemplate\]\[Selftest\] debug-level line',
		'\[ModTemplate\]\[Diag\] diagnostics report written'
	)
	foreach ($pattern in $required) {
		if ($log -notmatch $pattern) {
			$failures.Add("missing log: $pattern")
		}
	}

	if ($log -match '\[ModTemplate\]\[Bootstrap\] bootstrap failed') {
		$failures.Add('bootstrap failed in Player.log')
	}

	$diagPath = Join-Path $configDir 'ModTemplate-diagnostics.txt'
	if (-not (Test-Path $diagPath)) {
		$failures.Add("missing diagnostics file: $diagPath")
	}
	else {
		$diag = Get-Content $diagPath -Raw -Encoding UTF8
		if ($diag -notmatch 'logLevel=Debug') {
			$failures.Add('diagnostics report does not contain logLevel=Debug')
		}
	}

	$backup = Join-Path $configDir 'Mod_ModTemplate_ModTemplateMod.xml.bak'
	if (-not (Test-Path $backup)) {
		$failures.Add("missing settings backup: $backup")
	}

	if ($failures.Count -gt 0) {
		Write-Host '[smoke] FAILED' -ForegroundColor Red
		foreach ($failure in $failures) { Write-Host "  - $failure" -ForegroundColor Red }
		Write-Host "[smoke] run dir: $runDir" -ForegroundColor Yellow
		Write-Host '--- ModTemplate log tail ---'
		Select-String -Path $logPath -Pattern '\[ModTemplate\]|Exception' | Select-Object -Last 40 | ForEach-Object { $_.Line }
		exit 1
	}

	$passed = $true
	Write-Host '[smoke] PASSED' -ForegroundColor Green
	Write-Host '  modules=6 schema=2, debug logging, migration backup, diagnostics OK'
}
finally {
	if ($passed -and -not $KeepTemp -and (Test-Path $runDir)) {
		Remove-Item $runDir -Recurse -Force -ErrorAction SilentlyContinue
	}
	elseif (Test-Path $runDir) {
		Write-Host "[smoke] kept run dir: $runDir" -ForegroundColor Yellow
	}
}
