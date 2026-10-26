#Requires -Version 5.1
<#
.SYNOPSIS
	生成带 install.bat 的便携 zip，拷到其它电脑解压即可安装。

.DESCRIPTION
	1. 调用 package.ps1（verify + build + dist\ColonyKit-<version>.zip）
	2. 把 Mod 包解压到 staging\ColonyKit
	3. 复制 installer\install.bat 与 installer\使用说明.txt 到 staging 根
	4. 压缩为 dist\ColonyKit-<version>-portable.zip
	5. 校验第一层是 ColonyKit / install.bat / 使用说明.txt，且 Mod 根有 About

.EXAMPLE
	.\make-portable.ps1
	.\make-portable.ps1 -SkipBuild -SkipVerify
#>
[CmdletBinding()]
param(
	[switch]$SkipBuild,
	[switch]$SkipVerify
)

$ErrorActionPreference = 'Stop'

$root      = $PSScriptRoot
$dist      = Join-Path $root 'dist'
$installer = Join-Path $root 'installer'

$packageArgs = @{}
if ($SkipBuild)  { $packageArgs.SkipBuild  = $true }
if ($SkipVerify) { $packageArgs.SkipVerify = $true }
& (Join-Path $root 'package.ps1') @packageArgs
if ($LASTEXITCODE -ne 0) { throw 'package.ps1 失败，已中止便携包生成。' }

[xml]$about = Get-Content (Join-Path $root 'About\About.xml') -Encoding utf8
$version = $about.ModMetaData.modVersion
if ([string]::IsNullOrWhiteSpace($version)) { $version = '0.0.0' }

$modZip = Join-Path $dist ("ColonyKit-{0}.zip" -f $version)
if (-not (Test-Path $modZip)) { throw "缺少 Mod 包：$modZip" }

$bat  = Join-Path $installer 'install.bat'
$read = Join-Path $installer '使用说明.txt'
foreach ($file in $bat, $read) {
	if (-not (Test-Path $file)) { throw "缺少安装器文件：$file" }
}

$stage  = Join-Path $env:TEMP ("ColonyKit-portable-" + [guid]::NewGuid().ToString('N'))
$modDir = Join-Path $stage 'ColonyKit'
New-Item -ItemType Directory -Path $modDir -Force | Out-Null

try {
	Expand-Archive -Path $modZip -DestinationPath $modDir -Force
	Copy-Item $bat  $stage -Force
	Copy-Item $read $stage -Force

	foreach ($required in 'ColonyKit\About\About.xml', 'ColonyKit\Assemblies\ColonyKit.dll', 'install.bat', '使用说明.txt') {
		if (-not (Test-Path (Join-Path $stage $required))) {
			throw "便携包缺少：$required"
		}
	}

	New-Item -ItemType Directory -Force -Path $dist | Out-Null
	$out = Join-Path $dist ("ColonyKit-{0}-portable.zip" -f $version)
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
