# 自定义 Def 与 DefOf

> 状态：✅ 已实现　适用：Mod Template 0.0.1（RimWorld 1.6.4871）　最后更新：2026-10-25
> 本文件说明自定义 Def 子类怎么写、XML 根元素为什么是类型全名、DefOf 怎么绑定、DefInjected 放哪。

## TOC快速预览

- [简介](#简介)
- [描述](#描述)
- [绘制调用逻辑图](#绘制调用逻辑图)
- [函数清单](#函数清单)
- [函数或功能完成情况](#函数或功能完成情况)
- [涉及文件](#涉及文件)
- [所含功能](#所含功能)
- [相关文档](#相关文档)

## 简介

模板用一个纯数据 Def（`DemoConfigDef`）演示最小数据驱动链路：XML 定义字段 → `DirectXmlLoader` 按类型名反序列化 → `[DefOf]` 绑定成静态字段 → 代码和设置页直接读。它不依赖贴图、不参与渲染，适合当配置载体。

## 描述

- 自定义 Def 必须继承 `Def`；XML 根元素 = **类型全名**（`ModTemplate.DemoConfigDef`），因为游戏用 `GenTypes.GetTypeInAnyAssembly` 按节点名找类型。
- 字段必须是 `public`，给默认值；XML 里没写的字段保持默认值。
- `defName` 在 `Defs/DemoConfig.xml` 里是 `Default`；`ModTemplateDefOf.Default` 字段名必须与之一致。
- `DefOf` 绑定发生在 XML 解析之后、`[StaticConstructorOnStartup]` 之前；在 `Mod` 构造函数里访问会得到 null。
- `DefInjected` 目录名 = 类型全名，键格式 `<defName>.<field>`；模板只翻译 `label` / `description`。
- 不想写自定义类型时，标准 `ThingDef` / `HediffDef` 等同样适用，根元素换成对应类型名即可。

## 绘制调用逻辑图

```text
Defs/DemoConfig.xml
  <ModTemplate.DemoConfigDef>
      <defName>Default</defName>
      <greeting>Hello from ModTemplate</greeting>
        │
        ▼
DirectXmlLoader.DefFromNode(node)
  ├─ 按根元素名解析类型（GenTypes.GetTypeInAnyAssembly）
  ├─ 反射创建 DemoConfigDef
  └─ 逐字段反序列化
        │
        ▼
DefOfHelper.RebindAllDefOfs()
  └─ ModTemplateDefOf.Default = DefDatabase<DemoConfigDef>.GetNamed("Default")
        │
        ▼
Bootstrap / SettingsUI / 其他代码读取
```

## 函数清单

| 类型 / 函数 | 签名 | 作用 |
|---|---|---|
| `DemoConfigDef` | `class DemoConfigDef : Def` | 自定义 Def 子类 |
| `DemoConfigDef.greeting` | `public string greeting` | XML 问候语，自检用 |
| `DemoConfigDef.note` | `public string note` | 可选补丁追加的备注 |
| `DemoConfigDef.logIntervalTicks` | `public int logIntervalTicks` | 演示数值字段 |
| `ModTemplateDefOf.Default` | `public static DemoConfigDef Default` | DefOf 强类型引用 |
| `ModTemplateDefOf` 静态构造 | `DefOfHelper.EnsureInitializedInCtor(...)` | 过早访问时给明确报错 |
| `Bootstrap` 自检 | `DemoConfigDef config = ModTemplateDefOf.Default` | 打印 greeting/note |

## 函数或功能完成情况

| 项目 | 状态 | 说明 |
|---|---|---|
| 自定义 Def 子类 | ✅ | 编译通过，XML 根元素为类型全名 |
| XML 反序列化 | ✅ | quicktest 日志 `greeting="Hello from ModTemplate"` |
| `DefOf` 绑定 | ✅ | `Bootstrap` 安全读取 |
| `DefInjected` 中英翻译 | ✅ | `Languages/{English,ChineseSimplified}/DefInjected/ModTemplate.DemoConfigDef/Default.xml` |
| 可选补丁追加 `note` | ✅ | `Patches/OptionalCompatibility.xml`，Biotech 未装时为空 |

## 涉及文件

| 路径（相对 `sources/mod-template/`） | 作用 |
|---|---|
| `Source/DemoConfigDef.cs` | Def 子类与字段 |
| `Source/ModTemplateDefOf.cs` | `[DefOf]` 绑定 |
| `Defs/DemoConfig.xml` | XML 数据 |
| `Languages/English/DefInjected/ModTemplate.DemoConfigDef/Default.xml` | 英文 Def 字段 |
| `Languages/ChineseSimplified/DefInjected/ModTemplate.DemoConfigDef/Default.xml` | 简中 Def 字段 |
| `Patches/OptionalCompatibility.xml` | 条件追加 `note` |

## 所含功能

- 自定义 Def 子类与字段默认值；
- XML 按类型全名反序列化；
- `DefOf` 强类型引用与提前访问保护；
- DefInjected 字段翻译；
- 可选补丁向 Def 追加字段；
- 零美术资源的数据载体模式。

## 相关文档

- [生命周期与入口.md](生命周期与入口.md)
- [本地化.md](本地化.md)
- [ARCHITECTURE.md](ARCHITECTURE.md) §4.1
- 工作区：[Mod加载机制](../../../docs/Mod加载机制.md)
