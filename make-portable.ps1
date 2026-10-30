#Requires -Version 5.1
<#
.SYNOPSIS
	生成带 install.bat 的便携 zip，拷到其它电脑解压即可安装。

.DESCRIPTION
	1. 调用 package.ps1（verify + build + dist\ColonyKit-<version>.zip）
	2. 把 Mod 包解压到 staging\ColonyKit
	3. 把本地 Harmony Mod（含 1.6 DLL 与 LICENSE）复制到 staging\Harmony
	4. 复制 installer\install.bat / install.ps1 与 installer\使用说明.txt 到 staging 根
	5. 压缩为 dist\ColonyKit-<version>-portable.zip
	6. 校验第一层是 ColonyKit / Harmony / install.bat / install.ps1 / 使用说明.txt

.EXAMPLE
	.\make-portable.ps1
	.\make-portable.ps1 -SkipBuild -SkipVerify
	.\make-portable.ps1 -NoRjw
	.\make-portable.ps1 -HarmonyPath 'D:\SteamLibrary\steamapps\workshop\content\294100\2009463077'

.PARAMETER NoRjw
	转发给 package.ps1，产出去掉内置 RJW 的便携包（dist\ColonyKit-<version>-portable-noRjw.zip）。
#>
[CmdletBinding()]
param(
	[switch]$SkipBuild,
	[switch]$SkipVerify,
	[switch]$NoRjw,
	[string]$HarmonyPath = ''
)

$ErrorActionPreference = 'Stop'

$root      = $PSScriptRoot
$dist      = Join-Path $root 'dist'
$installer = Join-Path $root 'installer'

$packageArgs = @{}
if ($SkipBuild)  { $packageArgs.SkipBuild  = $true }
if ($SkipVerify) { $packageArgs.SkipVerify = $true }
if ($NoRjw)      { $packageArgs.NoRjw      = $true }
$suffix = if ($NoRjw) { '-noRjw' } else { '' }
# package.ps1 可能一次外部程序都没调，$LASTEXITCODE 会是 $null 或调用方的残留值。
$global:LASTEXITCODE = 0
& (Join-Path $root 'package.ps1') @packageArgs
if ($LASTEXITCODE -ne 0) { throw 'package.ps1 失败，已中止便携包生成。' }

[xml]$about = Get-Content (Join-Path $root 'About\About.xml') -Encoding utf8
$version = $about.ModMetaData.modVersion
if ([string]::IsNullOrWhiteSpace($version)) { $version = '0.0.0' }

# 便携包内置 Harmony：优先用 -HarmonyPath，否则在常见 Steam 创意工坊目录里找。
function Resolve-HarmonyPath {
	param([string]$Path)
	$candidates = New-Object System.Collections.Generic.List[string]
	if (-not [string]::IsNullOrWhiteSpace($Path)) { $candidates.Add($Path) }
	$candidates.Add('E:\SteamLibrary\steamapps\workshop\content\294100\2009463077')
	$candidates.Add('D:\SteamLibrary\steamapps\workshop\content\294100\2009463077')
	$candidates.Add('C:\SteamLibrary\steamapps\workshop\content\294100\2009463077')
	$pf86 = [Environment]::GetFolderPath('ProgramFilesX86')
	if ($pf86) {
		$candidates.Add((Join-Path $pf86 'Steam\steamapps\workshop\content\294100\2009463077'))
	}

	foreach ($c in $candidates) {
		if (-not (Test-Path -LiteralPath (Join-Path $c 'About\About.xml'))) { continue }
		if (-not (Test-Path -LiteralPath (Join-Path $c '1.6\Assemblies\0Harmony.dll'))) { continue }
		if (-not (Test-Path -LiteralPath (Join-Path $c 'LICENSE'))) { continue }
		if (Select-String -LiteralPath (Join-Path $c 'About\About.xml') -Pattern 'brrainz\.harmony' -Quiet -ErrorAction SilentlyContinue) {
			return $c
		}
	}
	throw '找不到 Harmony 创意工坊目录（需含 About、1.6\Assemblies\0Harmony.dll、LICENSE）。请用 -HarmonyPath 指定。'
}

$harmony = Resolve-HarmonyPath $HarmonyPath
Write-Host ("[harmony] {0}" -f $harmony) -ForegroundColor Cyan

$modZip = Join-Path $dist ("ColonyKit-{0}{1}.zip" -f $version, $suffix)
if (-not (Test-Path $modZip)) { throw "缺少 Mod 包：$modZip" }

$bat  = Join-Path $installer 'install.bat'
$ps1  = Join-Path $installer 'install.ps1'
$read = Join-Path $installer '使用说明.txt'
foreach ($file in $bat, $ps1, $read) {
	if (-not (Test-Path $file)) { throw "缺少安装器文件：$file" }
}

$stage  = Join-Path $env:TEMP ("ColonyKit-portable-" + [guid]::NewGuid().ToString('N'))
$modDir = Join-Path $stage 'ColonyKit'
New-Item -ItemType Directory -Path $modDir -Force | Out-Null

try {
	Expand-Archive -Path $modZip -DestinationPath $modDir -Force
	Copy-Item -LiteralPath $harmony -Destination (Join-Path $stage 'Harmony') -Recurse -Force
	Copy-Item $bat  $stage -Force
	Copy-Item $ps1  $stage -Force
	Copy-Item $read $stage -Force

	foreach ($required in 'ColonyKit\About\About.xml', 'ColonyKit\Assemblies\ColonyKit.dll', 'Harmony\About\About.xml', 'Harmony\1.6\Assemblies\0Harmony.dll', 'Harmony\LICENSE', 'install.bat', 'install.ps1', '使用说明.txt') {
		if (-not (Test-Path (Join-Path $stage $required))) {
			throw "便携包缺少：$required"
		}
	}

	New-Item -ItemType Directory -Force -Path $dist | Out-Null
	$out = Join-Path $dist ("ColonyKit-{0}-portable{1}.zip" -f $version, $suffix)
	if (Test-Path $out) { Remove-Item $out -Force }

	Add-Type -AssemblyName System.IO.Compression
	Add-Type -AssemblyName System.IO.Compression.FileSystem
	$archive = [System.IO.Compression.ZipFile]::Open(
		$out,
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

	$zip = [System.IO.Compression.ZipFile]::OpenRead($out)
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

	$size = (Get-Item $out).Length
	Write-Host ("[ok] {0}  {1:N0} 个文件  {2:N0} bytes" -f $out, $count, $size) -ForegroundColor Green
	Write-Host ("[ok] 第一层：{0}" -f ($tops -join ', ')) -ForegroundColor Green
}
finally {
	if (Test-Path $stage) { Remove-Item $stage -Recurse -Force }
}

# 同 build.ps1：本脚本的成功路径不会留下外部程序退出码，显式归零，避免调用方误判。
$global:LASTEXITCODE = 0
