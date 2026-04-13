# LJVNet

## 简介
LJVNet 是一个用于 Unity 项目的轻量网络配置模块。
它提供了基于字符串环境键的多环境、多端点配置能力，并将配置入口整合到了 Project Settings 中，方便在编辑器里维护 WebApi、Socket 等不同地址。

## 目录说明
- `Runtime/Scripts/INetConfig.cs`
  网络配置访问接口。
- `Runtime/Scripts/INetConfigProvider.cs`
  网络配置提供器接口，使用方可以自行实现并决定配置加载方式。
- `Runtime/Scripts/LJVNetClient.cs`
  LJVNet 运行时入口，负责注册提供器、初始化配置与示例请求方法。
- `Runtime/Scripts/ResourcesNetConfigProvider.cs`
  可选的默认实现，会在所有 Resources 目录中搜索第一份 `LJVNetConfigAsset`。
- `Runtime/Scripts/Config/LJVNetConfigAsset.cs`
  网络配置资源，负责环境与端点地址管理。
- `Editor/Scripts/LJVNetProjectSettingsProvider.cs`
  Project Settings 面板实现，支持自动搜索或创建主配置资源。
- `Editor/Scripts/LJVNetEnvironmentMenu.cs`
  编辑器菜单与环境快速切换窗口。
- `Editor/Scripts/LJVNetProjectSettingsView.uxml`
  Project Settings 面板的 UXML 布局。
- `Editor/Styles/LJVNetProjectSettings.uss`
  Project Settings 面板的 USS 样式。

## 配置规则
### 环境
- 新建配置时默认提供 `开发`、`测试`、`生产` 三个环境。
- 用户可以自由新增环境。
- `测试` 作为保底环境会始终保留，不能删除。
- `测试` 会始终排在环境列表第一项，方便快速切换和兜底排查。

### 端点
- 每个环境可以维护多条端点配置。
- 端点通过键值对区分，例如：
  - `default`
  - `webapi`
  - `socket`
- 如果同一主机只是端口不同，推荐直接用不同端点键来区分。

## 使用方式
### 1. 打开配置面板
通过菜单打开：
- `LJV/Network/Net Config`

### 2. 快速切换环境
通过菜单打开：
- `LJV/Network/Net Environment/选择当前环境`

### 3. 运行时注册配置提供器
在调用 `LJVNetClient.Init()` 之前，先注册配置来源：
- `LJVNetClient.SetConfigProvider(new YourConfigProvider())`
- 或 `LJVNetClient.SetConfigProvider<YourConfigProvider>()`
- 如果你已经自己拿到了配置对象，也可以直接调用 `LJVNetClient.SetConfig(yourConfig)`

### 4. 可选使用默认的 Resources 提供器
如果你希望继续使用 `Resources` 方案，可以直接注册：
- `LJVNetClient.SetConfigProvider<ResourcesNetConfigProvider>()`

## 设计说明
- 不再使用固定枚举环境。
- 不再使用写死的 `WebApiUrl`、`SocketUrl` 属性。
- 不再使用独立的 `LJVNetProviderConfigAsset` 资源。
- 所有环境和端点都以字符串键值对方式管理，便于扩展。
- 配置面板使用 UI Toolkit、UXML、USS 实现。
- 配置资源会优先全项目搜索；若不存在，则自动创建到 `Assets/Resources/Config/LJVNetConfig.asset`。
- 无论用户如何新增或删除环境，`测试` 都会被强制保留并保持在第一位。
- `Runtime/Scripts/SO` 已重命名为 `Runtime/Scripts/Config`，相关资源类型也同步改名。

## 维护建议
- 如果后续新增新的端点类型，优先直接增加端点键，而不是新增专用字段。
- 如果需要增加新的配置来源，实现 `INetConfigProvider` 即可接入。
- 建议项目中只保留一份主配置资源，避免搜索到多份配置时产生歧义。
