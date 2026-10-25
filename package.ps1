#Requires -Version 5.1
<#
.SYNOPSIS
	打包可发布的 Mod zip（只含运行期目录）。

.DESCRIPTION
	1. 可选先跑 verify.ps1
	2. 构建（除非 -SkipBuild）
	3. 把 About / Assemblies / Defs / Patches / Languages / Translations / Textures 复制到临时目录
	4. 检查：About/About.xml 在第一层、没有 Source/obj/bin、程序集只有 .dll
	5. 压缩到 dist\ColonyKit-<modVersion>.zip

.EXAMPLE
	.\package.ps1
	.\package.ps1 -SkipBuild
#>
[CmdletBinding()]
param(
	[switch]$SkipBuild,
	[switch]$SkipVerify
)

$ErrorActionPreference = 'Stop'

$root  = $PSScriptRoot
$dist  = Join-Path $root 'dist'

# 先校验再构建：结构问题（About 位置、XML 语法）在这里就能拦下，不用等打完包才发现。
if (-not $SkipVerify) {
	& (Join-Path $root 'verify.ps1')
	if ($LASTEXITCODE -ne 0) { throw '静态校验失败，已中止打包。' }
}

if (-not $SkipBuild) {
	& (Join-Path $root 'build.ps1')
	if ($LASTEXITCODE -ne 0) { throw '构建失败，已中止打包。' }
}

$dll = Join-Path $root 'Assemblies\ColonyKit.dll'
if (-not (Test-Path $dll)) { throw "缺少产物：$dll（先运行 build.ps1）" }

[xml]$about = Get-Content (Join-Path $root 'About\About.xml') -Encoding utf8
$version = $about.ModMetaData.modVersion
if ([string]::IsNullOrWhiteSpace($version)) { $version = '0.0.0' }

$stage = Join-Path $env:TEMP ("ColonyKit-package-" + [guid]::NewGuid().ToString('N'))
New-Item -ItemType Directory -Path $stage | Out-Null

try {
	# 只复制运行期目录（见 docs/开发Mod须知 §2.4）；Source/obj/bin 一律不进包。
	foreach ($name in 'About', 'Assemblies', 'Defs', 'Patches', 'Languages', 'Translations', 'Textures', 'Sounds', 'Blueprints') {
		$src = Join-Path $root $name
		if (Test-Path $src) {
			Copy-Item $src (Join-Path $stage $name) -Recurse -Force
		}
	}

	# 布局检查
	if (-not (Test-Path (Join-Path $stage 'About\About.xml'))) {
		throw '打包内容缺少 About\About.xml（位置不对，会变成一个空 Mod）'
	}
	foreach ($forbidden in 'Source', 'obj', 'bin') {
		$hits = Get-ChildItem $stage -Recurse -Directory -Filter $forbidden -ErrorAction SilentlyContinue
		if ($hits) { throw "打包内容里出现 $forbidden/：$($hits.FullName -join ', ')" }
	}
	$nativeOrExtra = Get-ChildItem (Join-Path $stage 'Assemblies') -Recurse -File |
		Where-Object { $_.Extension -ne '.dll' -and $_.Extension -ne '.pdb' }
	if ($nativeOrExtra) {
		throw "Assemblies/ 里有非程序集文件：$($nativeOrExtra.Name -join ', ')"
	}

	New-Item -ItemType Directory -Force -Path $dist | Out-Null
	$zipPath = Join-Path $dist ("ColonyKit-{0}.zip" -f $version)
	if (Test-Path $zipPath) { Remove-Item $zipPath -Force }

	Add-Type -AssemblyName System.IO.Compression
	Add-Type -AssemblyName System.IO.Compression.FileSystem
	# 手写 entry 名，保证 zip 里用 '/'（CreateFromDirectory 在 Windows 上会写 '\'）
	$archive = [System.IO.Compression.ZipFile]::Open(
		$zipPath,
		[System.IO.Compression.ZipArchiveMode]::Create)
	try {
		Get-ChildItem $stage -Recurse -File | ForEach-Object {
			$entryName = $_.FullName.Substring($stage.Length + 1).Replace('\', '/')
			[void][System.IO.Compression.ZipFileExtensions]::CreateEntryFromFile(
				$archive,
				$_.FullName,
				$entryName,
				[System.IO.Compression.CompressionLevel]::Optimal)
		}
	}
	finally {
		$archive.Dispose()
	}

	$zip = [System.IO.Compression.ZipFile]::OpenRead($zipPath)
	try {
		$tops = $zip.Entries |
			ForEach-Object { ($_.FullName -split '[\\/]')[0] } |
			Where-Object { $_ } |
			Sort-Object -Unique
		$count = $zip.Entries.Count
	}
	finally {
		$zip.Dispose()
	}

	$size = (Get-Item $zipPath).Length
	Write-Host ("[ok] {0}  {1:N0} 个文件  {2:N0} bytes" -f $zipPath, $count, $size) -ForegroundColor Green
	Write-Host ("[ok] 第一层：{0}" -f ($tops -join ', ')) -ForegroundColor Green
}
finally {
	if (Test-Path $stage) { Remove-Item $stage -Recurse -Force }
}
