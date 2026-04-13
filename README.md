# Bridge

## 简介
Bridge 是 VoyageForge 的 Unity 通信连接模块，用来统一连接本地与远程服务、客户端与服务端之间的配置与调用入口。

它围绕“环境 + 端点”组织 API 地址，把不同环境下的 Web API、Socket 或其他远程入口收拢到同一套配置体系里，并通过编辑器面板提供可视化维护能力。

## 目录说明
- `Runtime/Scripts/IBridgeConfig.cs`
  Bridge 运行时读取配置的访问接口。
- `Runtime/Scripts/IBridgeConfigProvider.cs`
  Bridge 配置提供器接口，允许项目按需决定配置的加载方式。
- `Runtime/Scripts/BridgeClient.cs`
  Bridge 的运行时入口，负责注册提供器、初始化配置并提供基础请求扩展示例。
- `Runtime/Scripts/ResourcesBridgeConfigProvider.cs`
  默认的 `Resources` 方案，会在所有 Resources 目录中搜索第一份 `BridgeConfigAsset`。
- `Runtime/Scripts/Config/BridgeConfigAsset.cs`
  Bridge 的核心配置资源，负责环境与端点地址管理。
- `Editor/Scripts/BridgeProjectSettingsProvider.cs`
  Project Settings 面板实现，支持自动搜索或创建主配置资源。
- `Editor/Scripts/BridgeEnvironmentMenu.cs`
  编辑器菜单与环境快速切换窗口。
- `Editor/Scripts/BridgeProjectSettingsView.uxml`
  Project Settings 面板的 UXML 布局。
- `Editor/Styles/BridgeProjectSettings.uss`
  Project Settings 面板的 USS 样式。

## 配置规则
### 环境
- 新建配置时默认提供 `开发`、`测试`、`生产` 三个环境。
- 用户可以自由新增环境。
- `测试` 作为保底环境会始终保留，不能删除。
- `测试` 会始终排在环境列表第一项，方便快速切换和兜底排查。

### 端点
- 每个环境可以维护多条端点配置。
- 端点通过键值对区分，例如 `default`、`webapi`、`socket`。
- 如果同一主机只是端口不同，推荐直接用不同端点键来区分。

## 使用方式
### 1. 打开配置面板
通过菜单打开：
- `VoyageForge/Bridge/Bridge Config`

### 2. 快速切换环境
通过菜单打开：
- `VoyageForge/Bridge/Bridge Environment/选择当前环境`

### 3. 运行时注册配置提供器
在调用 `BridgeClient.Init()` 之前，先注册配置来源：
- `BridgeClient.SetConfigProvider(new YourConfigProvider())`
- `BridgeClient.SetConfigProvider<YourConfigProvider>()`
- 如果你已经自己拿到了配置对象，也可以直接调用 `BridgeClient.SetConfig(yourConfig)`

### 4. 可选使用默认的 Resources 提供器
如果你希望继续使用 `Resources` 方案，可以直接注册：
- `BridgeClient.SetConfigProvider<ResourcesBridgeConfigProvider>()`

## 设计说明
- 使用字符串环境键与端点键管理远程服务入口，避免把地址结构写死在代码里。
- 配置面板基于 UI Toolkit、UXML、USS 实现，适合在 Unity 编辑器内集中维护。
- 配置资源会优先全项目搜索；若不存在，则自动创建到 `Assets/Resources/Config/BridgeConfig.asset`。
- 无论用户如何新增或删除环境，`测试` 都会被强制保留并保持在第一位。
- 如果项目后续扩展新的服务类型，优先新增端点键，而不是继续堆叠专用字段。

## 维护建议
- 如果需要增加新的配置来源，实现 `IBridgeConfigProvider` 即可接入。
- 建议项目中只保留一份主配置资源，避免搜索到多份配置时产生歧义。
