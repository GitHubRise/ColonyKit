#Requires -Version 5.1
<#
.SYNOPSIS
	静态检查 Mod 包结构、XML 合法性、packageId 与补丁 xpath。

.DESCRIPTION
	不启动游戏，只做能在磁盘上验证的检查：
	  1. About/About.xml 存在、packageId 合规、supportedVersions 含 1.6
	  2. Defs / Patches / Languages 下所有 XML 可解析
	  3. Defs 根元素是 <Defs>，Patches 根元素是 <Patch> 且子节点是 <Operation>
	  4. 补丁里的 <xpath> 能在本地 Defs 合并文档里命中（命中不了只警告，可能指向原版 Def）
	  5. Assemblies 存在且只有 .dll
  6. AutoScrollView 里的 Listing 保持单列（否则内容超出视口时换列，滚动条永远不出现）

.EXAMPLE
	.\verify.ps1
#>
[CmdletBinding()]
param()

$ErrorActionPreference = 'Stop'

$root      = $PSScriptRoot
$aboutPath = Join-Path $root 'About\About.xml'
$problems  = New-Object System.Collections.Generic.List[string]
$warnings  = New-Object System.Collections.Generic.List[string]

if (-not (Test-Path $aboutPath)) {
	$problems.Add("缺少 $aboutPath")
}
else {
	[xml]$about = Get-Content $aboutPath -Encoding utf8
	$meta = $about.ModMetaData
	if ([string]::IsNullOrWhiteSpace($meta.packageId)) {
		$problems.Add('About.xml 缺少 packageId')
	}
	elseif ($meta.packageId -notmatch '^(?=.{1,60}$)(?!\.)(?=.*?[.])(?!.*([.])\1+)[a-zA-Z0-9.]{1,}[a-zA-Z0-9]{1}$') {
		$problems.Add("packageId 不合规：$($meta.packageId)")
	}

	$versions = @($meta.supportedVersions.li)
	if ($versions -notcontains '1.6') {
		$problems.Add("supportedVersions 里没有 1.6（当前：$($versions -join ', ')）")
	}
}

# 2. XML 可解析 + 3. 根元素
$defs = New-Object System.Xml.XmlDocument
$defsRoot = $defs.CreateElement('Defs')
[void]$defs.AppendChild($defsRoot)

$defsDir = Join-Path $root 'Defs'
if (Test-Path $defsDir) {
	Get-ChildItem $defsDir -Recurse -Filter '*.xml' | ForEach-Object {
		try {
			$doc = New-Object System.Xml.XmlDocument
			$doc.Load($_.FullName)
			if ($doc.DocumentElement.Name -ne 'Defs') {
				$problems.Add("$($_.FullName)：根元素是 <$($doc.DocumentElement.Name)>，应为 <Defs>")
				return
			}
			foreach ($node in $doc.DocumentElement.ChildNodes) {
				if ($node.NodeType -eq 'Element') {
					[void]$defsRoot.AppendChild($defs.ImportNode($node, $true))
				}
			}
		}
		catch {
			$problems.Add("$($_.FullName)：XML 解析失败 - $($_.Exception.Message)")
		}
	}
}
else {
	$warnings.Add('没有 Defs/ 目录')
}

$patchesDir = Join-Path $root 'Patches'
if (Test-Path $patchesDir) {
	Get-ChildItem $patchesDir -Recurse -Filter '*.xml' | ForEach-Object {
		try {
			$doc = New-Object System.Xml.XmlDocument
			$doc.Load($_.FullName)
			if ($doc.DocumentElement.Name -ne 'Patch') {
				$problems.Add("$($_.FullName)：根元素是 <$($doc.DocumentElement.Name)>，应为 <Patch>")
				return
			}
			foreach ($op in $doc.DocumentElement.ChildNodes) {
				if ($op.NodeType -ne 'Element') { continue }
				if ($op.Name -ne 'Operation') {
					$problems.Add("$($_.FullName)：<Patch> 下出现 <$($op.Name)>，应为 <Operation>")
					continue
				}
				$class = $op.GetAttribute('Class')
				if ([string]::IsNullOrWhiteSpace($class)) {
					$problems.Add("$($_.FullName)：<Operation> 缺少 Class 属性")
				}
			}

			# 4. 收集所有 xpath，尝试在本地 Defs 里命中
			$nodes = $doc.SelectNodes('//xpath')
			foreach ($xp in $nodes) {
				$expr = $xp.InnerText.Trim()
				if ([string]::IsNullOrWhiteSpace($expr)) { continue }
				try {
					$hit = $defs.SelectSingleNode($expr)
					if ($null -eq $hit) {
						# 补丁也可能改原版/其他 Mod 的 Def，本地 Defs 找不到属正常，
						# 所以只警告；要严格校验，需要把目标 Mod 的 XML 也合进来。
						$warnings.Add("补丁 xpath 未命中本地 Defs（可能指向原版/其他 Mod）：$expr")
					}
				}
				catch {
					$problems.Add("补丁 xpath 非法：$expr - $($_.Exception.Message)")
				}
			}
		}
		catch {
			$problems.Add("$($_.FullName)：XML 解析失败 - $($_.Exception.Message)")
		}
	}
}

# 5. 程序集
$assembliesDir = Join-Path $root 'Assemblies'
if (-not (Test-Path $assembliesDir)) {
	$warnings.Add('没有 Assemblies/（纯 XML Mod 可以忽略）')
}
else {
	$dlls = Get-ChildItem $assembliesDir -Recurse -Filter '*.dll'
	if ($dlls.Count -eq 0) { $problems.Add('Assemblies/ 存在但没有 .dll') }
	$other = Get-ChildItem $assembliesDir -Recurse -File | Where-Object { $_.Extension -ne '.dll' -and $_.Extension -ne '.pdb' -and $_.Extension -ne '.xml' }
	if ($other) {
		$warnings.Add("Assemblies/ 里有非程序集文件：$($other.Name -join ', ')")
	}
}

# 6. 设置页滚动视图：Listing 默认会在内容超过 rect 高度时换列，换列后高度测不出来，
#    滚动条永远不出现、后面的参数也点不到；AutoScrollView 必须显式单列。
$autoScrollPath = Join-Path $root 'Source\UI\AutoScrollView.cs'
if (Test-Path $autoScrollPath) {
	$autoScrollText = Get-Content $autoScrollPath -Raw -Encoding utf8
	if ($autoScrollText -notmatch 'maxOneColumn\s*=\s*true') {
		$problems.Add('Source/UI/AutoScrollView.cs 缺少 maxOneColumn = true：内容超过视口时 Listing 会换列，滚动条不会出现')
	}
}

foreach ($w in $warnings) { Write-Host "[warn] $w" -ForegroundColor Yellow }
foreach ($p in $problems) { Write-Host "[fail] $p" -ForegroundColor Red }

if ($problems.Count -gt 0) {
	Write-Host ("校验失败：{0} 个问题，{1} 个警告" -f $problems.Count, $warnings.Count) -ForegroundColor Red
	exit 1
}

Write-Host ("校验通过：0 个问题，{0} 个警告" -f $warnings.Count) -ForegroundColor Green
exit 0
