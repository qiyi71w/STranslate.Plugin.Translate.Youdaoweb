<div align="center">
  <a href="https://github.com/qiyi71w/STranslate.Plugin.Translate.Youdaoweb">
    <img src="./icon.png" alt="有道网页翻译" width="128" height="128" />
  </a>

  <h1>有道网页翻译</h1>

  <p>
    有道网页端翻译插件 for <a href="https://github.com/STranslate/STranslate">STranslate</a>
  </p>

  <p>
    <img alt="License" src="https://img.shields.io/github/license/qiyi71w/STranslate.Plugin.Translate.Youdaoweb?style=flat-square" />
    <img alt="Release" src="https://img.shields.io/github/v/release/qiyi71w/STranslate.Plugin.Translate.Youdaoweb?style=flat-square" />
    <img alt="Downloads" src="https://img.shields.io/github/downloads/qiyi71w/STranslate.Plugin.Translate.Youdaoweb/total?style=flat-square" />
    <img alt=".NET" src="https://img.shields.io/badge/.NET-10.0-512bd4?style=flat-square" />
    <img alt="WPF" src="https://img.shields.io/badge/WPF-Plugin-blue?style=flat-square" />
  </p>
</div>

`STranslate.Plugin.Translate.YoudaoWeb` 是一个面向 [STranslate](https://github.com/STranslate/STranslate) 的第三方翻译插件，调用有道网页端翻译接口完成文本翻译，不依赖有道智云官方付费 OpenAPI，也不需要配置 AppKey / AppSecret。

## 概览

- 面向 `STranslate.Plugin` SDK 1.0.10
- 目标框架：`net10.0-windows`
- 支持构建后自动打包为 `.spkg`
- 当前仅实现文本翻译
- 设置页无需配置，只展示说明与免责声明

## 功能特性

- 无需 API Key，开箱即用
- 通过 STranslate 标准翻译插件接口接入
- 使用 `System.Text.Json` 解析响应
- 使用 `MD5` 与 `AES-128-CBC` 完成网页端协议签名与解密
- 翻译失败时返回错误信息，不向 UI 层抛出未处理异常

## 支持语言

支持以下语言映射：

- 自动检测（仅源语言）
- 简体中文
- 繁体中文
- 英语
- 日语
- 韩语
- 法语
- 西班牙语
- 葡萄牙语（葡萄牙 / 巴西共用 `pt`）
- 意大利语
- 德语
- 俄语
- 阿拉伯语
- 泰语
- 荷兰语
- 印度尼西亚语
- 越南语

> [!NOTE]
> 目标语言不支持 `Auto`。如果在 STranslate 中把目标语言设为自动，插件会返回 `UnsupportedTargetLang`。

## 工作原理

本插件调用的是有道网页端的普通翻译链路，而不是 AI 对话或有道智云官方 OpenAPI。当前实现流程如下：

1. 请求 `https://dict.youdao.com/webtranslate/key` 获取动态 `secretKey`、`aesKey`、`aesIv`
2. 请求 `https://dict.youdao.com/webtranslate` 提交翻译表单
3. 对返回的加密字符串做 URL-safe Base64 还原
4. 使用 `AES-128-CBC + PKCS7` 解密响应
5. 从 `translateResult` 中提取 `tgt` 并拼接为最终译文

## 项目结构

```text
.
├─ Main.cs
├─ plugin.json
├─ icon.png
└─ STranslate.Plugin.Translate.YoudaoWeb.csproj
```

- `Main.cs`：插件入口、语言映射、网页端请求、解密与结果解析
- `plugin.json`：STranslate 插件元数据
- `icon.png`：插件图标
- `STranslate.Plugin.Translate.YoudaoWeb.csproj`：项目与自动打包配置

## 环境要求

开始前请确认本机具备：

- Windows
- .NET 10 SDK
- 能访问 NuGet 以还原 `STranslate.Plugin` 包

> [!IMPORTANT]
> 仅安装 .NET Runtime 不够，构建本项目需要 .NET SDK。

## 构建

在仓库根目录执行：

```powershell
dotnet build .\STranslate.Plugin.Translate.YoudaoWeb.csproj -c Release
```

构建成功后，主要产物位于：

- `bin\Release\STranslate.Plugin.Translate.YoudaoWeb.dll`
- `bin\Release\plugin.json`
- `bin\Release\icon.png`

## 打包为 `.spkg`

项目已启用：

```xml
<EnableAutoPackage>true</EnableAutoPackage>
```

因此执行 `Release` 构建后会自动生成插件包：

```text
bin\Release\plugins\STranslate.Plugin.Translate.YoudaoWeb.spkg
```

`.spkg` 包内根目录应包含：

- `STranslate.Plugin.Translate.YoudaoWeb.dll`
- `plugin.json`
- `icon.png`

## 安装到 STranslate

你可以使用任一方式安装：

1. 在 STranslate 插件管理器中导入 `bin\Release\plugins\STranslate.Plugin.Translate.YoudaoWeb.spkg`
2. 手动解压 `.spkg`，将包含 `plugin.json` 的整个目录复制到 STranslate 插件目录

安装后，在插件列表中启用 `有道网页翻译` 即可。

## 实现说明

- 这是针对 STranslate SDK 的独立插件实现
- 当前只实现文本翻译，不包含 OCR、TTS、词典或复杂设置页
- 代码侧不复用 STranslate 内置的官方有道 OpenAPI 插件 `PluginID`
- 调用的是网页端普通翻译接口，不是显式的 AI 翻译接口

## 免责声明

> [!WARNING]
> 本插件调用的是有道网页端接口，属于非公开接入方式，接口行为、参数、签名、加密方式、返回结构、可用性和频率限制都可能随时变化。项目维护者不保证该接口持续可用，也不保证翻译结果、服务稳定性或兼容性。

> [!WARNING]
> 本插件为非官方第三方实现，与有道及其关联方不存在授权、认可或合作关系；“有道”相关名称、商标及服务权利归其权利人所有。使用者应自行确认其使用方式符合相关法律法规、平台规则及服务条款；如权利人明确禁止、要求停止，或你无法确认使用合法性，请立即停止使用。

> [!NOTE]
> 本仓库提供的是一个面向 STranslate 的技术实现示例，不构成任何法律、合规或商业使用建议。
