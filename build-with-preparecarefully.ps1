#Requires -Version 5.1
<#
.SYNOPSIS
	构建 Colony Kit 并重编内置「精心准备」（Source\PrepareCarefully → Assemblies\EdBPrepareCarefully.dll），可选部署。

.DESCRIPTION
	和 build.ps1 的区别只有一个：把内置精心准备的子工程也一起编。
	- 改了 Source\PrepareCarefully 下的代码、或刚跑过 sync-preparecarefully.ps1 → 用本脚本。
	- 只改 ColonyKit 自己的代码 → 用 build.ps1（快，不重编精心准备）。

	本脚本是 build.ps1 -WithPrepareCarefully 的入口，参数与行为完全一致；
	产物是 Assemblies\ColonyKit.dll + Assemblies\EdBPrepareCarefully.dll，-Deploy 时一起部署。

.PARAMETER Deploy
	构建成功后复制运行期目录到游戏 Mods\ColonyKit，并校验 DLL 的 SHA256。

.PARAMETER Clean
	构建前删除 obj/bin 与两个子工程的旧产物（RJW 保留，除非同时 -WithRjw）。

.PARAMETER NoRebuild
	跳过 -t:Rebuild（日常增量构建用）。

.EXAMPLE
	.\build-with-preparecarefully.ps1
	.\build-with-preparecarefully.ps1 -Deploy
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
	-WithPrepareCarefully `
	-Deploy:$Deploy `
	-Clean:$Clean `
	-NoRebuild:$NoRebuild `
	-Configuration $Configuration `
	-ModsPath $ModsPath

if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }
