#Requires -Version 5.1
<#
.SYNOPSIS
	把 sources/rjw 的 1.6 内容同步进 ColonyKit（内置 RJW 的「进货」脚本）。

.DESCRIPTION
	ColonyKit 内置的 RJW 是 sources/rjw 的一份副本，但有三处必须保留的本地改动
	（packageId 从 rim.job.world 回退到 local.colonykit、csproj 输出路径）。
	手工整目录复制会把这三点冲掉，而且编译不报错、只在运行时静默退化 —— 本脚本负责守住它们。

	同步规则：
	- Source / Defs\Rjw / Patches\Rjw  → 镜像（源删了什么，目标也删；受保护文件除外）；
	- Languages / Textures / Sounds   → 合并（只补新文件，不删目标里 ColonyKit 自己的资源）；
	  内容不同的文件默认不覆盖，按差异方向分成两类报告：
	    · 本地扩展：目标比源多出的行（ColonyKit 加的 CK_ 词条）——正常，保留；
	    · 上游更新：源里有目标没有的行 —— 需要人工合并，脚本只提示（-Force 可强制用源覆盖）。
	- 其余 1.6 目录（About / Assemblies）不同步：About 是 ColonyKit 自己的，RJW.dll 由 build-with-rjw.ps1 编译。

	同步完成后会重写 docs\内置RJW同步记录.md（源分支 / commit / 各目录文件数 / 冲突），
	再运行 .\build-with-rjw.ps1 重新编译内置 RJW。

.PARAMETER DryRun
	只报告将要发生的增删改，不动文件。

.PARAMETER Force
	合并目录里内容不同的文件也直接用源覆盖（会丢掉 ColonyKit 在词条文件里加的行，慎用）。

.PARAMETER NoRecord
	不重写 docs\内置RJW同步记录.md。

.PARAMETER SourceRepo
	RJW 仓库根目录，默认 <工作区>\sources\rjw。

.EXAMPLE
	.\sync-rjw.ps1 -DryRun
	.\sync-rjw.ps1
	.\sync-rjw.ps1 -Force
#>
[CmdletBinding()]
param(
	[switch]$DryRun,
	[switch]$Force,
	[switch]$NoRecord,
	[string]$SourceRepo = ''
)

$ErrorActionPreference = 'Stop'

$root      = $PSScriptRoot
$workspace = Split-Path (Split-Path $root -Parent) -Parent
if ([string]::IsNullOrWhiteSpace($SourceRepo)) { $SourceRepo = Join-Path $workspace 'sources\rjw' }

$srcVersion = Join-Path $SourceRepo '1.6'
if (-not (Test-Path -LiteralPath $srcVersion)) {
	throw "找不到 RJW 1.6 目录：$srcVersion`n用 -SourceRepo 指定 RJW 仓库根目录。"
}

# Source 里属于「上游工程基建」、不该进 ColonyKit 的文件/目录。
$sourceExclude = @(
	'^Analyzers(\\|$)'
	'\.slnx$'
	'\.sh$'
	'^packages\.lock\.json$'
	'\.xlsx$'
	'^\.editorconfig$'
	'^\.vs(\\|$)'
	'^obj(\\|$)'
	'^bin(\\|$)'
)

# 与源不同、必须保留 ColonyKit 版本的文件：Rel + 必须仍然包含的片段（防止被覆盖成上游版本）。
$protect = @(
	@{ Rel = 'RimJobWorld.Main.csproj'; Match = '..\..\Assemblies\' }
	@{ Rel = 'Harmony\First.cs'; Match = 'local.colonykit' }
	@{ Rel = 'Harmony\patch_recipes.cs'; Match = 'local.colonykit' }
)

$textExt = @('.xml', '.txt', '.cs', '.md', '.json', '.yml', '.yaml', '.csv', '.properties')

function Get-RelFileMap {
	param([string]$Path, [string[]]$Exclude = @())

	$map = @{}
	if (-not (Test-Path -LiteralPath $Path)) { return $map }
	$prefix = $Path.TrimEnd('\') + '\'
	foreach ($f in Get-ChildItem -LiteralPath $Path -Recurse -File) {
		$rel = $f.FullName.Substring($prefix.Length)
		$skip = $false
		foreach ($rx in $Exclude) {
			if ($rel -match $rx) { $skip = $true; break }
		}
		if (-not $skip) { $map[$rel] = $f.FullName }
	}
	return $map
}

function Get-HashOf {
	param([string]$Path)
	(Get-FileHash -LiteralPath $Path -Algorithm SHA256).Hash
}

function Copy-EnsureDir {
	param([string]$Source, [string]$Target)
	$parent = Split-Path $Target -Parent
	if (-not (Test-Path -LiteralPath $parent)) { New-Item -ItemType Directory -Path $parent -Force | Out-Null }
	Copy-Item -LiteralPath $Source -Destination $Target -Force
}

# 去掉 CR 后比较，用来识别「只有换行符不同」的文件（14 个 ThingCategories.xml 属于这类）。
function Get-TextWithoutCr {
	param([string]$Path)
	$bytes = [System.IO.File]::ReadAllBytes($Path)
	$ms = New-Object System.IO.MemoryStream
	foreach ($b in $bytes) {
		if ($b -ne 13) { $ms.WriteByte($b) }
	}
	$norm = [Convert]::ToBase64String($ms.ToArray())
	$ms.Dispose()
	return $norm
}

function Test-SameTextIgnoringCr {
	param([string]$A, [string]$B)
	return ((Get-TextWithoutCr -Path $A) -eq (Get-TextWithoutCr -Path $B))
}

function Get-TextLineSet {
	param([string]$Path)
	$text = [System.IO.File]::ReadAllText($Path, [System.Text.Encoding]::UTF8)
	$lines = @()
	foreach ($line in ($text -split "`r?`n")) {
		if ($line.Trim().Length -gt 0) { $lines += $line }
	}
	return $lines
}

function Invoke-Mirror {
	param([string]$Name, [string]$SrcDir, [string]$DstDir, [string[]]$Exclude)

	$result = @{ Name = $Name; Mode = '镜像'; Added = 0; Updated = 0; Deleted = 0; Unchanged = 0; Keep = 0 }
	$srcMap = Get-RelFileMap -Path $SrcDir -Exclude $Exclude
	$dstMap = Get-RelFileMap -Path $DstDir -Exclude @('(^|\\)(obj|bin)(\\|$)')

	if ($srcMap.Count -eq 0) { throw "源目录为空：$SrcDir" }
	if (-not (Test-Path -LiteralPath $DstDir)) { throw "目标目录不存在：$DstDir" }

	$protectRels = @($protect | ForEach-Object { $_.Rel })

	foreach ($rel in @($srcMap.Keys)) {
		$src = $srcMap[$rel]
		$dst = Join-Path $DstDir $rel
		if ($protectRels -contains $rel) { $result.Keep++; continue }

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
	param([string]$Name, [string]$SrcDir, [string]$DstDir, [string[]]$Exclude)

	$result = @{
		Name = $Name; Mode = '合并'; Added = 0; Updated = 0; Deleted = 0; Unchanged = 0
		EolOnly = 0; LocalExtra = @(); Conflicts = @()
	}
	$srcMap = Get-RelFileMap -Path $SrcDir -Exclude $Exclude
	if ($srcMap.Count -eq 0) { throw "源目录为空：$SrcDir" }

	foreach ($rel in @($srcMap.Keys)) {
		$src = $srcMap[$rel]
		$dst = Join-Path $DstDir $rel

		if (-not (Test-Path -LiteralPath $dst)) {
			$result.Added++
			if (-not $DryRun) { Copy-EnsureDir -Source $src -Target $dst }
			continue
		}
		if ((Get-HashOf $src) -eq (Get-HashOf $dst)) { $result.Unchanged++; continue }

		$ext = [System.IO.Path]::GetExtension($rel).ToLowerInvariant()
		if ($textExt -contains $ext) {
			if (Test-SameTextIgnoringCr -A $src -B $dst) { $result.EolOnly++; continue }

			$srcLines = Get-TextLineSet -Path $src
			$dstLines = Get-TextLineSet -Path $dst
			$missing = @($srcLines | Where-Object { $dstLines -notcontains $_ })
			if ($missing.Count -eq 0) {
				# 目标只是比源多几行（CK_ 词条之类），保留本地版本。
				$result.LocalExtra += $rel
				continue
			}
			$result.Conflicts += [pscustomobject]@{ Rel = $rel; Kind = '上游更新'; Sample = ($missing | Select-Object -First 3) }
		}
		else {
			$result.Conflicts += [pscustomobject]@{ Rel = $rel; Kind = '二进制不同'; Sample = @() }
		}

		if ($Force) {
			$result.Updated++
			if (-not $DryRun) { Copy-EnsureDir -Source $src -Target $dst }
		}
	}

	return $result
}

function Get-GitText {
	param([string]$Repo, [string[]]$GitArgs)
	$oldPref = $ErrorActionPreference
	$ErrorActionPreference = 'Continue'
	try {
		$out = & git -C $Repo @GitArgs 2>$null
		$code = $LASTEXITCODE
	}
	catch { return '' }
	finally { $ErrorActionPreference = $oldPref }
	if ($code -ne 0 -or $null -eq $out) { return '' }
	return (($out | Out-String).Trim())
}

# ---------------------------------------------------------------- 执行

Write-Host "内置 RJW 同步" -ForegroundColor Cyan
Write-Host ("  源：{0}" -f $srcVersion) -ForegroundColor DarkGray
Write-Host ("  目标：{0}" -f $root) -ForegroundColor DarkGray
if ($DryRun) { Write-Host '  [dry-run] 只报告，不改文件' -ForegroundColor Yellow }

$jobs = @(
	@{ Name = 'Source';    Src = (Join-Path $srcVersion 'Source');    Dst = (Join-Path $root 'Source\Rjw');  Mode = 'mirror' }
	@{ Name = 'Defs';      Src = (Join-Path $srcVersion 'Defs');      Dst = (Join-Path $root 'Defs\Rjw');    Mode = 'mirror' }
	@{ Name = 'Patches';   Src = (Join-Path $srcVersion 'Patches');   Dst = (Join-Path $root 'Patches\Rjw'); Mode = 'mirror' }
	@{ Name = 'Languages'; Src = (Join-Path $srcVersion 'Languages'); Dst = (Join-Path $root 'Languages');   Mode = 'merge' }
	@{ Name = 'Textures';  Src = (Join-Path $srcVersion 'Textures');  Dst = (Join-Path $root 'Textures');    Mode = 'merge' }
	@{ Name = 'Sounds';    Src = (Join-Path $srcVersion 'Sounds');    Dst = (Join-Path $root 'Sounds');      Mode = 'merge' }
)

$results = @()
foreach ($job in $jobs) {
	if (-not (Test-Path -LiteralPath $job.Src)) {
		Write-Host ("[skip] {0}：源目录不存在" -f $job.Name) -ForegroundColor Yellow
		continue
	}
	if ($job.Mode -eq 'mirror') {
		$r = Invoke-Mirror -Name $job.Name -SrcDir $job.Src -DstDir $job.Dst -Exclude $sourceExclude
		Write-Host ("[{0}] {1,-9} 新增 {2,3}  更新 {3,3}  删除 {4,3}  未变 {5,4}  保留 {6}" -f 'mirror', $job.Name, $r.Added, $r.Updated, $r.Deleted, $r.Unchanged, $r.Keep) -ForegroundColor Green
	}
	else {
		$r = Invoke-Merge -Name $job.Name -SrcDir $job.Src -DstDir $job.Dst -Exclude @()
		Write-Host ("[{0}] {1,-9} 新增 {2,3}  覆盖 {3,3}  未变 {4,4}  仅换行 {5,3}  本地扩展 {6,3}  上游冲突 {7,3}" -f 'merge', $job.Name, $r.Added, $r.Updated, $r.Unchanged, $r.EolOnly, @($r.LocalExtra).Count, @($r.Conflicts).Count) -ForegroundColor Green
	}
	$results += $r
}

# 受保护文件：源里被换掉的话，上面已经跳过了；这里再验一次内容，防止有人手改丢了回退。
$protectOk = 0
foreach ($p in $protect) {
	$path = Join-Path (Join-Path $root 'Source\Rjw') $p.Rel
	if (-not (Test-Path -LiteralPath $path)) {
		throw "受保护文件不存在：$path`n（正常构建会因 packageId 找不到自己而静默退化，已中止）"
	}
	$text = [System.IO.File]::ReadAllText($path, [System.Text.Encoding]::UTF8)
	$needle = $p.Match
	if ($text.IndexOf($needle, [System.StringComparison]::Ordinal) -lt 0) {
		throw "受保护文件丢了本地改动（缺少 `"$needle`"）：$path"
	}
	$protectOk++
}
Write-Host ("[protect] {0}/{1} 受保护文件校验通过（packageId 回退 / csproj 输出路径）" -f $protectOk, $protect.Count) -ForegroundColor Green

# 冲突明细
$conflicts = @()
$localExtras = @()
foreach ($r in $results) {
	if ($r.ContainsKey('Conflicts')) { $conflicts += $r.Conflicts }
	if ($r.ContainsKey('LocalExtra')) { $localExtras += ($r.LocalExtra | ForEach-Object { "$($r.Name)/$_" }) }
}
if ($conflicts.Count -gt 0) {
	Write-Host ''
	Write-Host ("[warn] {0} 个文件上游有更新，未覆盖：" -f $conflicts.Count) -ForegroundColor Yellow
	foreach ($c in ($conflicts | Select-Object -First 20)) {
		Write-Host ("  {0}  ({1})" -f $c.Rel, $c.Kind) -ForegroundColor Yellow
		foreach ($s in $c.Sample) { Write-Host ("      + {0}" -f $s) -ForegroundColor DarkGray }
	}
	Write-Host '  处理方式：手工把源里的新行并进 ColonyKit 的对应文件；确认要整份用源覆盖时加 -Force。' -ForegroundColor Yellow
}
if ($localExtras.Count -gt 0) {
	Write-Host ("[info] {0} 个文件保留了 ColonyKit 自己的追加行（正常）：{1}" -f $localExtras.Count, (($localExtras | Select-Object -First 5) -join ', ')) -ForegroundColor DarkGray
}

# ---------------------------------------------------------------- 记录

if (-not $NoRecord -and -not $DryRun) {
	$branch = Get-GitText -Repo $SourceRepo -GitArgs @('rev-parse', '--abbrev-ref', 'HEAD')
	$commit = Get-GitText -Repo $SourceRepo -GitArgs @('rev-parse', '--short', 'HEAD')
	$commitTime = Get-GitText -Repo $SourceRepo -GitArgs @('log', '-1', '--format=%cI')

	$lines = @()
	$lines += '# 内置 RJW 同步记录'
	$lines += ''
	$lines += '> 本文件由 `sync-rjw.ps1` 自动生成，请勿手改。最近一次同步：'
	$lines += ''
	$lines += '| 项 | 值 |'
	$lines += '|---|---|'
	$srcDisplay = $srcVersion
	if ($srcDisplay.StartsWith($workspace, [System.StringComparison]::OrdinalIgnoreCase)) {
		$srcDisplay = $srcDisplay.Substring($workspace.Length).TrimStart('\')
	}
	$srcDisplay = $srcDisplay.Replace('\', '/')
	$lines += ("| 同步时间 | {0} |" -f (Get-Date -Format 'yyyy-MM-dd HH:mm:ss zzz'))
	$lines += ('| 源目录 | `{0}` |' -f $srcDisplay)
	$lines += ("| 分支 | {0} |" -f $(if ($branch) { $branch } else { '（未知）' }))
	$lines += ("| 提交 | {0} |" -f $(if ($commit) { $commit } else { '（未知）' }))
	$lines += ("| 提交时间 | {0} |" -f $(if ($commitTime) { $commitTime } else { '（未知）' }))
	$lines += ("| 受保护文件 | {0}/{1} 校验通过 |" -f $protectOk, $protect.Count)
	foreach ($r in $results) {
		if ($r.Mode -eq '镜像') {
			$lines += ("| {0} | {1} 个文件（镜像；新增 {2} / 更新 {3} / 删除 {4}） |" -f $r.Name, ($r.Added + $r.Updated + $r.Unchanged + $r.Keep), $r.Added, $r.Updated, $r.Deleted)
		}
		else {
			$total = $r.Added + $r.Updated + $r.Unchanged + $r.EolOnly + @($r.LocalExtra).Count + @($r.Conflicts).Count
			$lines += ("| {0} | {1} 个文件（合并；新增 {2} / 仅换行 {3} / 本地扩展 {4} / 上游冲突 {5}） |" -f $r.Name, $total, $r.Added, $r.EolOnly, @($r.LocalExtra).Count, @($r.Conflicts).Count)
		}
	}
	$lines += ''
	if ($conflicts.Count -eq 0) {
		$lines += '冲突（上游有更新、未覆盖）：无。'
	}
	else {
		$lines += ("冲突（上游有更新、未覆盖）：{0} 个，需人工合并。" -f $conflicts.Count)
		$lines += ''
		foreach ($c in $conflicts) { $lines += ("- `{0}`（{1}）" -f $c.Rel, $c.Kind) }
	}
	$lines += ''
	$lines += '同步规则与受保护文件见 `sync-rjw.ps1` 的注释；同步完记得运行 `build-with-rjw.ps1` 重编 RJW.dll。'

	$recordPath = Join-Path $root 'docs\内置RJW同步记录.md'
	[System.IO.File]::WriteAllText($recordPath, (($lines -join "`r`n") + "`r`n"), (New-Object System.Text.UTF8Encoding($false)))
	Write-Host ("[record] {0}" -f $recordPath) -ForegroundColor Cyan
}

Write-Host ''
if ($DryRun) {
	Write-Host '（dry-run，未改动任何文件）' -ForegroundColor Yellow
}
else {
	Write-Host '下一步：.\build-with-rjw.ps1  重新编译内置 RJW' -ForegroundColor Cyan
}

# 本脚本成功路径可能没调用外部程序，显式归零，避免调用方误判。
$global:LASTEXITCODE = 0
