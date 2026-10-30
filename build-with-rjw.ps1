#Requires -Version 5.1
<#
.SYNOPSIS
	构建 Colony Kit 并重编内置 RJW（Source\Rjw → Assemblies\RJW.dll），可选部署。

.DESCRIPTION
	和 build.ps1 的区别只有一个：把 RJW 子工程也一起编。
	- 改了 Source\Rjw 下的代码、或刚跑过 sync-rjw.ps1 同步了 RJW 源码/资源 → 用本脚本。
	- 只改 ColonyKit 自己的代码 → 用 build.ps1（快，不重编 RJW）。

	本脚本是 build.ps1 -WithRjw 的入口，参数与行为完全一致；
	产物仍是 Assemblies\ColonyKit.dll + Assemblies\RJW.dll，-Deploy 时一起部署。

.PARAMETER Deploy
	构建成功后复制运行期目录到游戏 Mods\ColonyKit，并校验两个 DLL 的 SHA256。

.PARAMETER Clean
	构建前删除 Source\obj、Source\bin、Source\Rjw\obj、Source\Rjw\bin 与整个 Assemblies（RJW 也重编）。

.PARAMETER NoRebuild
	跳过 -t:Rebuild（日常增量构建用；发布/CI 不要用）。

.EXAMPLE
	.\build-with-rjw.ps1
	.\build-with-rjw.ps1 -Clean
	.\build-with-rjw.ps1 -Deploy
#>
[CmdletBinding()]
param(
	[switch]$Deploy,
	[switch]$Clean,
	[switch]$NoRebuild,
	[ValidateSet('Debug', 'Release')]
	[string]$Configuration = 'Release',
	[string]$ModsPath = 'E:\SteamLibrary\steamapps\common\RimWorld\Mods'
)

$ErrorActionPreference = 'Stop'

& (Join-Path $PSScriptRoot 'build.ps1') `
	-WithRjw `
	-Deploy:$Deploy `
	-Clean:$Clean `
	-NoRebuild:$NoRebuild `
	-Configuration $Configuration `
	-ModsPath $ModsPath

if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }
