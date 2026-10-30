#Requires -Version 5.1
<#
.SYNOPSIS
	把 sources/preparecarefully 的源码与内容同步进 ColonyKit（内置精心准备的「进货」脚本）。

.DESCRIPTION
	内置的「精心准备」（EdB Prepare Carefully，MIT）是 sources/preparecarefully 的一份副本：
	源码进 Source\PrepareCarefully（由 ColonyKit.PrepareCarefully.csproj 单独编成 Assemblies\EdBPrepareCarefully.dll，
	不并入 ColonyKit.dll），Defs / Textures / Languages 合并进 ColonyKit 对应目录。

	同步规则（与 sync-rjw.ps1 同一套思路）：
	- Source\**\*.cs            → 镜像（上游删了什么，这里也删；排除 obj/bin）；
	- Resources\Common\Defs     → 镜像到 Defs\PrepareCarefully；
	- Resources\Common\Textures → 合并到 Textures（只补新文件，不删 ColonyKit 自己的资源）；
	- Resources\Common\Languages→ 合并到 Languages（中文词条是 ColonyKit 本地追加，会保留）。

	本脚本不改上游源码，也不做包名回退：内置 PC 的程序集名仍是 EdBPrepareCarefully，
	由 ColonyKit 的 PrepareCarefullyModule 控制是否挂补丁（见 Source\Modules\PrepareCarefully）。

	同步完成后重写 docs\内置精心准备同步记录.md。

.PARAMETER DryRun
	只报告将要发生的增删改，不动文件。

.PARAMETER NoRecord
	不重写同步记录。

.PARAMETER SourceRepo
	preparecarefully 仓库根目录，默认 <工作区>\sources\preparecarefully。

.EXAMPLE
	.\sync-preparecarefully.ps1 -DryRun
	.\sync-preparecarefully.ps1
#>
[CmdletBinding()]
param(
	[switch]$DryRun,
	[switch]$NoRecord,
	[string]$SourceRepo = ''
)

$ErrorActionPreference = 'Stop'

$root      = $PSScriptRoot
$workspace = Split-Path (Split-Path $root -Parent) -Parent
if ([string]::IsNullOrWhiteSpace($SourceRepo)) { $SourceRepo = Join-Path $workspace 'sources\preparecarefully' }

$srcSource = Join-Path $SourceRepo 'Source'
$srcDefs   = Join-Path $SourceRepo 'Resources\Common\Defs'
$srcTex    = Join-Path $SourceRepo 'Resources\Common\Textures'
$srcLang   = Join-Path $SourceRepo 'Resources\Common\Languages'

foreach ($p in $srcSource, $srcDefs, $srcTex, $srcLang) {
	if (-not (Test-Path -LiteralPath $p)) { throw "找不到上游目录：$p`n用 -SourceRepo 指定 preparecarefully 仓库根目录。" }
}

$textExt = @('.xml', '.txt', '.cs', '.md', '.json', '.yml', '.yaml', '.csv')
$exclude = @('(^|\\)(obj|bin)(\\|$)', '\.TMP$', '~RF')

function Get-RelFileMap {
	param([string]$Path, [string[]]$Exclude = @())
	$map = @{}
	if (-not (Test-Path -LiteralPath $Path)) { return $map }
	$prefix = $Path.TrimEnd('\') + '\'
	foreach ($f in Get-ChildItem -LiteralPath $Path -Recurse -File) {
		$rel = $f.FullName.Substring($prefix.Length)
		$skip = $false
		foreach ($rx in $Exclude) { if ($rel -match $rx) { $skip = $true; break } }
		if (-not $skip) { $map[$rel] = $f.FullName }
	}
	return $map
}

function Get-HashOf { param([string]$Path) (Get-FileHash -LiteralPath $Path -Algorithm SHA256).Hash }

function Copy-EnsureDir {
	param([string]$Source, [string]$Target)
	$parent = Split-Path $Target -Parent
	if (-not (Test-Path -LiteralPath $parent)) { New-Item -ItemType Directory -Path $parent -Force | Out-Null }
	Copy-Item -LiteralPath $Source -Destination $Target -Force
}

function Invoke-Mirror {
	param([string]$Name, [string]$SrcDir, [string]$DstDir)
	$result = @{ Name = $Name; Mode = '镜像'; Added = 0; Updated = 0; Deleted = 0; Unchanged = 0 }
	$srcMap = Get-RelFileMap -Path $SrcDir -Exclude $exclude
	$dstMap = Get-RelFileMap -Path $DstDir -Exclude $exclude
	if ($srcMap.Count -eq 0) { throw "源目录为空：$SrcDir" }
	if (-not (Test-Path -LiteralPath $DstDir)) {
		if (-not $DryRun) { New-Item -ItemType Directory -Path $DstDir -Force | Out-Null }
	}
	foreach ($rel in @($srcMap.Keys)) {
		$src = $srcMap[$rel]; $dst = Join-Path $DstDir $rel
		if (Test-Path -LiteralPath $dst) {
			if ((Get-HashOf $src) -eq (Get-HashOf $dst)) { $result.Unchanged++; continue }
			$result.Updated++
			if (-not $DryRun) { Copy-EnsureDir -Source $src -Target $dst }
		}
		else {
			$result.Added++
			if (-not $DryRun) { Copy-EnsureDir -Source $src -Target $dst }
		}
	}
	foreach ($rel in @($dstMap.Keys)) {
		if ($srcMap.ContainsKey($rel)) { continue }
		$result.Deleted++
		if (-not $DryRun) { Remove-Item -LiteralPath $dstMap[$rel] -Force }
	}
	return $result
}

function Invoke-Merge {
	param([string]$Name, [string]$SrcDir, [string]$DstDir, [string]$Prefix = '')
	$result = @{ Name = $Name; Mode = '合并'; Added = 0; Updated = 0; Unchanged = 0; LocalExtra = 0 }
	$srcMap = Get-RelFileMap -Path $SrcDir -Exclude $exclude
	foreach ($rel in @($srcMap.Keys)) {
		$src = $srcMap[$rel]
		$dst = Join-Path $DstDir ($Prefix + $rel)
		if (-not (Test-Path -LiteralPath $dst)) {
			$result.Added++
			if (-not $DryRun) { Copy-EnsureDir -Source $src -Target $dst }
			continue
		}
		if ((Get-HashOf $src) -eq (Get-HashOf $dst)) { $result.Unchanged++; continue }
		# 目标里有本地追加（例如中文词条）时保留本地版本，只报告。
		$result.LocalExtra++
	}
	return $result
}

Write-Host '内置精心准备同步' -ForegroundColor Cyan
Write-Host ("  源：{0}" -f $SourceRepo) -ForegroundColor DarkGray
if ($DryRun) { Write-Host '  [dry-run] 只报告，不改文件' -ForegroundColor Yellow }

$results = @()
$r = Invoke-Mirror -Name 'Source' -SrcDir $srcSource -DstDir (Join-Path $root 'Source\PrepareCarefully')
$results += $r
Write-Host ("[mirror] {0,-9} 新增 {1,3}  更新 {2,3}  删除 {3,3}  未变 {4,4}" -f 'Source', $r.Added, $r.Updated, $r.Deleted, $r.Unchanged) -ForegroundColor Green

$r = Invoke-Mirror -Name 'Defs' -SrcDir $srcDefs -DstDir (Join-Path $root 'Defs\PrepareCarefully')
$results += $r
Write-Host ("[mirror] {0,-9} 新增 {1,3}  更新 {2,3}  删除 {3,3}  未变 {4,4}" -f 'Defs', $r.Added, $r.Updated, $r.Deleted, $r.Unchanged) -ForegroundColor Green

$r = Invoke-Merge -Name 'Textures' -SrcDir $srcTex -DstDir (Join-Path $root 'Textures')
$results += $r
Write-Host ("[merge ] {0,-9} 新增 {1,3}  未变 {2,4}  本地保留 {3,3}" -f 'Textures', $r.Added, $r.Unchanged, $r.LocalExtra) -ForegroundColor Green

$r = Invoke-Merge -Name 'Languages' -SrcDir $srcLang -DstDir (Join-Path $root 'Languages')
$results += $r
Write-Host ("[merge ] {0,-9} 新增 {1,3}  未变 {2,4}  本地保留 {3,3}" -f 'Languages', $r.Added, $r.Unchanged, $r.LocalExtra) -ForegroundColor Green

if (-not $NoRecord -and -not $DryRun) {
	$commit = ''
	try { $commit = (& git -C $SourceRepo rev-parse --short HEAD 2>$null) } catch { }
	$lines = @()
	$lines += '# 内置精心准备同步记录'
	$lines += ''
	$lines += '> 本文件由 `sync-preparecarefully.ps1` 自动生成，请勿手改。最近一次同步：'
	$lines += ''
	$lines += '| 项 | 值 |'
	$lines += '|---|---|'
	$lines += ("| 同步时间 | {0} |" -f (Get-Date -Format 'yyyy-MM-dd HH:mm:ss zzz'))
	$lines += ("| 上游提交 | {0} |" -f $(if ($commit) { $commit } else { '（未知）' }))
	$lines += ''
	$lines += '同步目录：`Source`（镜像 → `Source\PrepareCarefully`）、`Resources\Common\Defs`（镜像 → `Defs\PrepareCarefully`）、`Resources\Common\Textures`（合并 → `Textures`）、`Resources\Common\Languages`（合并 → `Languages`）。'
	$lines += ''
	$lines += '同步完记得运行 `build-with-preparecarefully.ps1` 重编 `Assemblies\EdBPrepareCarefully.dll`。'
	$recordPath = Join-Path $root 'docs\内置精心准备同步记录.md'
	[System.IO.File]::WriteAllText($recordPath, (($lines -join "`r`n") + "`r`n"), (New-Object System.Text.UTF8Encoding($false)))
	Write-Host ("[record] {0}" -f $recordPath) -ForegroundColor Cyan
}

$global:LASTEXITCODE = 0
