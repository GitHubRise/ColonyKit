#Requires -Version 5.1
<#
.SYNOPSIS
	把一个已展开的 ColonyKit Mod 目录里的内置 RJW 内容摘掉（-NoRjw 出包 / 部署时共用）。

.DESCRIPTION
	RJW 在这份 Mod 里不只是多一个 DLL，摘掉时要动五处，少一处启动日志就会刷红：

	1. Assemblies\RJW.dll / RJW.pdb
	2. Defs\Rjw、Patches\Rjw（RJW 的 Def 与补丁）
	3. Defs\Robotics\RJWCompat.xml —— 根元素是 <rjw.HediffDef_SexPart>，
	   没 RJW.dll 会报「找不到类型」。
	4. Defs\Robotics\MkVBody.xml 里的 MechGenitals / MechChest / MechAnus 三个部位
	   （这三个 BodyPartDef 定义在 RJW 的 Def 里），以及 Languages 下指向 RJW 的 def 的词条
	   （DefInjected 里 13 个 rjw.* 目录 + 混在 HediffDef\ThingDef 等目录里的 RJW 条目）。
	5. About.xml 的 incompatibleWith 里的 rim.job.world（不内置了就不该再禁止外部 RJW）。

	代码侧不用改：ColonyKit.dll 不引用 RJW 程序集（RjwCompat 用 Type.GetType 探测），
	相关分支在 RJW 缺席时自动失效。

.PARAMETER Path
	要处理的 Mod 目录（已展开的运行期目录结构，含 About\About.xml）。

.PARAMETER SourceRoot
	仓库根目录，用来读「RJW 一共定义了哪些 defName」。必须指向还带着 Defs\Rjw 的源码树。

.EXAMPLE
	. .\tools\strip-rjw.ps1
	Remove-RjwContent -Path 'D:\stage\ColonyKit'
#>

function Get-DefNameSet {
	param([string]$File)
	$set = New-Object 'System.Collections.Generic.HashSet[string]'
	foreach ($m in [regex]::Matches([IO.File]::ReadAllText($File), '<defName>([^<]+)</defName>')) {
		[void]$set.Add($m.Groups[1].Value.Trim())
	}
	,$set
}

function Remove-RjwContent {
	[CmdletBinding()]
	param(
		[Parameter(Mandatory)][string]$Path,
		[string]$SourceRoot = (Split-Path $PSScriptRoot -Parent)
	)

	if (-not (Test-Path -LiteralPath (Join-Path $Path 'About\About.xml'))) {
		throw "不是 Mod 目录（缺 About\About.xml）：$Path"
	}

	# RJW 定义了哪些 defName：源码树里还在，目标目录可能已经删过，所以从源码读。
	$rjwNames = New-Object 'System.Collections.Generic.HashSet[string]'
	$rjwDefs = Join-Path $SourceRoot 'Defs\Rjw'
	if (Test-Path -LiteralPath $rjwDefs) {
		Get-ChildItem $rjwDefs -Recurse -File -Filter *.xml | ForEach-Object {
			[void]$rjwNames.UnionWith((Get-DefNameSet $_.FullName))
		}
	}
	$rjwCompat = Join-Path $SourceRoot 'Defs\Robotics\RJWCompat.xml'
	if (Test-Path -LiteralPath $rjwCompat) {
		[void]$rjwNames.UnionWith((Get-DefNameSet $rjwCompat))
	}
	if ($rjwNames.Count -eq 0) {
		Write-Host '[rjw] 源码树里找不到 Defs\Rjw，按「没有 RJW 内容」继续' -ForegroundColor Yellow
	}

	# 1. 程序集
	$removedFiles = 0
	foreach ($leaf in 'RJW.dll', 'RJW.pdb') {
		$p = Join-Path $Path "Assemblies\$leaf"
		if (Test-Path -LiteralPath $p) { Remove-Item -LiteralPath $p -Force; $removedFiles++ }
	}

	# 2. RJW 的 Def / Patches
	foreach ($dir in 'Defs\Rjw', 'Patches\Rjw') {
		$p = Join-Path $Path $dir
		if (Test-Path -LiteralPath $p) {
			$removedFiles += (Get-ChildItem $p -Recurse -File).Count
			Remove-Item -LiteralPath $p -Recurse -Force
		}
	}

	# 3. 依赖 RJW 类型的 Def 文件
	$compat = Join-Path $Path 'Defs\Robotics\RJWCompat.xml'
	if (Test-Path -LiteralPath $compat) { Remove-Item -LiteralPath $compat -Force; $removedFiles++ }

	# 3b. RJW 自己的图标/贴图（含 .psd 源文件），留在包里只是白占体积
	$texRoot = Join-Path $Path 'Textures'
	if (Test-Path -LiteralPath $texRoot) {
		Get-ChildItem $texRoot -Recurse -File |
			Where-Object { $_.Name -match '^rjw' } |
			ForEach-Object { Remove-Item -LiteralPath $_.FullName -Force; $removedFiles++ }
	}

	# 4a. Mk-V 身上三个 RJW 部位
	$bodyFile = Join-Path $Path 'Defs\Robotics\MkVBody.xml'
	$bodyParts = @('MechGenitals', 'MechChest', 'MechAnus')
	$removedParts = 0
	if (Test-Path -LiteralPath $bodyFile) {
		$doc = New-Object System.Xml.XmlDocument
		$doc.Load($bodyFile)
		foreach ($li in @($doc.SelectNodes('//parts/li'))) {
			$defNode = $li.SelectSingleNode('def')
			if ($defNode -and ($bodyParts -contains $defNode.InnerText.Trim())) {
				[void]$li.ParentNode.RemoveChild($li)
				$removedParts++
			}
		}
		$doc.Save($bodyFile)
	}

	# 4b. Languages：14 个 rjw.* 类型目录整目录删；其余目录里指向 RJW def 的词条逐条删。
	$removedLangFiles = 0
	$removedEntries = 0
	$langRoot = Join-Path $Path 'Languages'
	if (Test-Path -LiteralPath $langRoot) {
		Get-ChildItem $langRoot -Recurse -Directory |
			Where-Object { $_.Parent -and $_.Parent.Name -eq 'DefInjected' -and $_.Name -like 'rjw.*' } |
			ForEach-Object {
				$removedLangFiles += (Get-ChildItem $_.FullName -Recurse -File).Count
				Remove-Item -LiteralPath $_.FullName -Recurse -Force
			}

		$defInjectedFiles = Get-ChildItem $langRoot -Recurse -File -Filter *.xml |
			Where-Object { $_.FullName -match '\\DefInjected\\' }
		$skipped = 0
		foreach ($file in $defInjectedFiles) {
			# 个别词条文件本身就不是合法 XML（开闭标签大小写不一致），RimWorld 也读不动；
			# 这种文件原样留着并记一笔，不让整个出包流程挂掉。
			try {
				$doc = New-Object System.Xml.XmlDocument
				$doc.Load($file.FullName)
			}
			catch {
				# 解析不了就退回正则：条目全都指向 RJW 的 def 就整份删掉；
				# 混着别的 def 才原样保留（这种文件 RimWorld 也读不动，但至少不误删）。
				$text = [IO.File]::ReadAllText($file.FullName)
				$names = @([regex]::Matches($text, '<\s*([A-Za-z0-9_]+)\.[A-Za-z0-9_.]+') |
					ForEach-Object { $_.Groups[1].Value } | Sort-Object -Unique)
				if ($names.Count -gt 0 -and @($names | Where-Object { -not $rjwNames.Contains($_) }).Count -eq 0) {
					Remove-Item -LiteralPath $file.FullName -Force
					$removedLangFiles++
					continue
				}
				$skipped++
				Write-Host ("[rjw] 跳过无法解析的词条文件：{0}" -f $file.FullName.Substring($langRoot.Length + 1)) -ForegroundColor Yellow
				continue
			}
			$root = $doc.DocumentElement
			if (-not $root) { continue }
			$hit = 0
			foreach ($node in @($root.ChildNodes)) {
				if ($node.NodeType -ne [System.Xml.XmlNodeType]::Element) { continue }
				# 词条形如 <DefName.fieldName>，defName 是第一个点之前的部分。
				$name = $node.Name
				$dot = $name.IndexOf('.')
				$defName = if ($dot -gt 0) { $name.Substring(0, $dot) } else { $name }
				if ($rjwNames.Contains($defName)) {
					[void]$root.RemoveChild($node)
					$hit++
				}
			}
			if ($hit -eq 0) { continue }
			$removedEntries += $hit
			if ($root.ChildNodes.Count -eq 0) {
				Remove-Item -LiteralPath $file.FullName -Force
				$removedLangFiles++
			}
			else {
				$doc.Save($file.FullName)
			}
		}
		if ($skipped -gt 0) {
			Write-Host ("[rjw] 有 {0} 个词条文件不是合法 XML，已原样保留（RimWorld 也读不动它们）" -f $skipped) -ForegroundColor Yellow
		}

		# RJW 自己的词条文件（Keyed\RJWMainTab.xml、BackstoryDef\rjw_*.xml …）：
		# 里面有的条目指向早已不存在的 def，按 def 名过滤抓不住，直接按文件名清掉。
		Get-ChildItem $langRoot -Recurse -File -Filter *.xml |
			Where-Object { $_.Name -match '^rjw' } |
			ForEach-Object { Remove-Item -LiteralPath $_.FullName -Force; $removedLangFiles++ }

		# 清掉被删空的目录（DefInjected 的类型目录、语言目录）
		do {
			$empty = Get-ChildItem $langRoot -Recurse -Directory |
				Where-Object { (Get-ChildItem $_.FullName -Force | Measure-Object).Count -eq 0 }
			foreach ($d in $empty) { Remove-Item -LiteralPath $d.FullName -Recurse -Force }
		} while ($empty)
	}

	# 5. About：不再禁止外部 RJW
	$aboutFile = Join-Path $Path 'About\About.xml'
	$aboutDoc = New-Object System.Xml.XmlDocument
	$aboutDoc.Load($aboutFile)
	$droppedIncompat = 0
	foreach ($li in @($aboutDoc.SelectNodes('//incompatibleWith/li'))) {
		if ($li.InnerText.Trim() -eq 'rim.job.world') {
			[void]$li.ParentNode.RemoveChild($li)
			$droppedIncompat++
		}
	}
	if ($droppedIncompat -gt 0) { $aboutDoc.Save($aboutFile) }

	Write-Host ("[rjw] 摘除完成：程序集/Def 文件 {0} 个、Mk-V 部位 {1} 段、语言文件 {2} 个、词条 {3} 条、incompatibleWith {4} 条" -f `
			$removedFiles, $removedParts, $removedLangFiles, $removedEntries, $droppedIncompat) -ForegroundColor Cyan
}
