#Requires -Version 5.1
<#
.SYNOPSIS
	从模板生成一个新的 Mod 工程（复制 → 重命名 → 编译）。

.DESCRIPTION
	模板里的占位符：
	  ColonyKit        → 新 Mod 的程序集名 / 命名空间 / 文件名
	  local.colonykit  → 新 packageId
	  Colony Kit       → 新的显示名

	复制完成后会尝试用工作区的 .tools/dotnet 编译一次，确认改名没有破坏引用。

.PARAMETER Name
	新 Mod 的标识名（字母开头，只含字母数字），用作文件夹名、程序集名、命名空间。

.PARAMETER PackageId
	新的 packageId，必须符合游戏规则（小写、含点、≤60 字符）。

.PARAMETER DisplayName
	Mod 列表里显示的名字，默认与 Name 相同。

.PARAMETER Destination
	目标目录，默认 sources\<Name>。

.EXAMPLE
	.\new-mod.ps1 -Name MyMod -PackageId local.mymod -DisplayName "My Mod"
#>
[CmdletBinding()]
param(
	[Parameter(Mandatory = $true)]
	[string]$Name,

	[Parameter(Mandatory = $true)]
	[string]$PackageId,

	[string]$DisplayName,
	[string]$Destination
)

$ErrorActionPreference = 'Stop'

if ($Name -notmatch '^[A-Za-z][A-Za-z0-9]*$') {
	throw "Name 只能以字母开头、且只含字母数字：$Name"
}
if ($PackageId -notmatch '^(?=.{1,60}$)(?!\.)(?=.*?[.])(?!.*([.])\1+)[a-zA-Z0-9.]{1,}[a-zA-Z0-9]{1}$') {
	throw "packageId 不合规（需要含点、只含字母数字点、≤60 字符）：$PackageId"
}
if ([string]::IsNullOrWhiteSpace($DisplayName)) { $DisplayName = $Name }

$template = $PSScriptRoot
if ([string]::IsNullOrWhiteSpace($Destination)) {
	$Destination = Join-Path (Split-Path $template -Parent) $Name
}
$Destination = [IO.Path]::GetFullPath($Destination)

if (Test-Path $Destination) {
	throw "目标目录已存在：$Destination"
}

$skip = @('.git', 'Assemblies', 'dist', 'build-error.log')
New-Item -ItemType Directory -Path $Destination | Out-Null

try {
	Get-ChildItem -Force $template | Where-Object { $skip -notcontains $_.Name } | ForEach-Object {
		Copy-Item $_.FullName (Join-Path $Destination $_.Name) -Recurse -Force
	}
	foreach ($junk in 'Source\obj', 'Source\bin') {
		$path = Join-Path $Destination $junk
		if (Test-Path $path) { Remove-Item $path -Recurse -Force }
	}

	$map = [ordered]@{
		'local.colonykit' = $PackageId
		'Colony Kit'      = $DisplayName
		'ColonyKit'       = $Name
	}

	$extensions = '.cs', '.csproj', '.xml', '.ps1', '.md', '.txt', '.gitignore'
	Get-ChildItem $Destination -Recurse -File -Force |
		Where-Object { $extensions -contains $_.Extension -or $_.Name -eq '.gitignore' } |
		ForEach-Object {
			# PowerShell 5.1 会把无 BOM 的 .ps1 当 ANSI 读，中文会变乱码甚至语法错误。
			# 按原文件是否带 BOM 决定写入编码，避免生成出来的脚本反而跑不起来。
			$bytes = [IO.File]::ReadAllBytes($_.FullName)
			$hasBom = $bytes.Length -ge 3 -and $bytes[0] -eq 0xEF -and $bytes[1] -eq 0xBB -and $bytes[2] -eq 0xBF
			$text = [IO.File]::ReadAllText($_.FullName, [Text.UTF8Encoding]::new($false))
			foreach ($key in $map.Keys) {
				$text = $text.Replace($key, $map[$key])
			}
			[IO.File]::WriteAllText($_.FullName, $text, [Text.UTF8Encoding]::new($hasBom))
		}

	# 先重命名目录（路径长的优先，避免父目录改名后子项路径失效），再重命名文件
	Get-ChildItem $Destination -Recurse -Directory |
		Where-Object { $_.Name -like '*ColonyKit*' } |
		Sort-Object { $_.FullName.Length } -Descending |
		ForEach-Object {
			Rename-Item $_.FullName $_.Name.Replace('ColonyKit', $Name)
		}
	Get-ChildItem $Destination -Recurse -File |
		Where-Object { $_.Name -like '*ColonyKit*' } |
		ForEach-Object {
			Rename-Item $_.FullName $_.Name.Replace('ColonyKit', $Name)
		}

	Write-Host "[ok] 已生成：$Destination" -ForegroundColor Green
	Write-Host "     packageId: $PackageId"
	Write-Host "     显示名   : $DisplayName"

	$workspace = Split-Path (Split-Path $Destination -Parent) -Parent
	$dotnet = Join-Path $workspace '.tools\dotnet\dotnet.exe'
	$nuget = Join-Path $workspace '.tools\nuget'
	if (Test-Path $dotnet) {
		$project = Join-Path $Destination ("Source\{0}.csproj" -f $Name)
		Write-Host "[build] $project" -ForegroundColor Cyan
		& $dotnet restore $project --packages $nuget 2>&1 | Out-String | Write-Verbose
		& $dotnet build $project -c Release --no-restore 2>&1 | Out-String | Write-Verbose
		if ($LASTEXITCODE -ne 0) { throw '新工程的首次编译失败，请检查上面的输出。' }
		Write-Host "[ok] 编译通过：Assemblies\$Name.dll" -ForegroundColor Green
	}
	else {
		Write-Host '[warn] 没找到 .tools/dotnet，跳过编译。' -ForegroundColor Yellow
	}

	Write-Host ''
	Write-Host '下一步：'
	Write-Host "  1. 改 About/About.xml 的 description、作者"
	Write-Host "  2. .\build.ps1 -Deploy 部署到游戏，进开发者模式验证"
	Write-Host "  3. 删掉演示用的 DemoConfig / DemoGameComponent / Patch_Pawn_SpawnSetup"
}
catch {
	if (Test-Path $Destination) { Remove-Item $Destination -Recurse -Force }
	throw
}
