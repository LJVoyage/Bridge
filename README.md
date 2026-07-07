# Bridge

## 简介
Bridge 是 VoyageForge 的 Unity 通信连接模块，用来统一连接本地与远程服务、客户端与服务端之间的配置与调用入口。

它围绕"环境 + 端点"组织 API 地址，把不同环境下的 Web API、Socket 或其他远程入口收拢到同一套配置体系里，并通过编辑器面板提供可视化维护能力。

## 目录说明

### Runtime
- `Runtime/Scripts/IBridgeConfig.cs` — Bridge 运行时读取配置的访问接口。
- `Runtime/Scripts/IBridgeConfigProvider.cs` — Bridge 配置提供器接口，允许项目按需决定配置的加载方式。
- `Runtime/Scripts/BridgeClient.cs` — Bridge 核心运行时入口，抽象泛型单例基类，提供 **Wait / Callback 双模式**、拦截器链、多端点支持。
- `Runtime/Scripts/Request.cs` — 请求数据模型，支持 url、method、bodyJson、headers、timeout、endpointKey、cancellationToken。
- `Runtime/Scripts/Response.cs` — 响应数据容器，包含 data、statusCode、statusText、headers。
- `Runtime/Scripts/RequestHandle.cs` — Callback 模式句柄，支持 OnComplete / OnError 事件和取消。
- `Runtime/Scripts/Config/BridgeConfigAsset.cs` — Bridge 核心配置资源，负责环境与端点地址管理。

### Editor
- `Editor/Scripts/BridgeSettingsProvider.cs` — Bridge 的 Project Settings 面板实现。
- `Editor/Scripts/BridgeSettings.cs` — 编辑器配置，保存 BridgeConfigAsset 引用到 ProjectSettings。
- `Editor/Scripts/BridgeEnvironmentMenu.cs` — 编辑器菜单与环境快速切换窗口。
- `Editor/Styles/` — UXML 布局与 USS 样式。

### Samples
- `Samples~/Basic/` — 示例项目，包含完整的 WebClient 实现和多端点测试用例。
- 在 Package Manager 中点击 Import 即可导入到 `Assets/Samples/` 目录。

### Tests
- `Tests/Runtime/` — PlayMode 测试，覆盖完整 URL、默认端点、自定义端点三种场景。

## 配置规则

### 环境
- 新建配置时默认提供 `dev` 环境。
- 用户可以自由新增环境。
- `dev` 作为保底环境始终保留，不能删除，始终排在列表第一项。

### 端点
- 每个环境可以维护多条端点配置，通过键值对区分（如 `default`、`webapi`、`socket`）。
- 如果同一主机只是端口不同，推荐直接用不同端点键来区分。

## 快速开始

### 1. 创建 WebClient

继承 `BridgeClient<T>`，指定配置提供器和默认端点键：

```csharp
public class WebClient : BridgeClient<WebClient>
{
    protected override IBridgeConfigProvider ConfigProvider => 
        new ResourcesBridgeConfigProvider("VoyageForge/Config/BridgeConfig");
    protected override string urlKey => "default";
}
```

### 2. 发起请求

**Wait 模式（async/await）**：

```csharp
// 默认端点
var response = await WebClient.GetAsync<UserDto>("api/user");

// 自定义端点
var response = await WebClient.GetAsync<StatusDto>("api/status", "webapi");
```

**Callback 模式**：

```csharp
WebClient.Get<UserDto>("api/user")
    .OnComplete += response => Debug.Log($"成功: {response.data}");
```

### 3. 多端点支持

通过 `Request.endpointKey` 指定端点，或使用便捷方法重载：

```csharp
// 方式一：Request 对象
var request = new Request
{
    url = "api/status",
    method = "GET",
    endpointKey = "webapi"
};
var response = await WebClient.SendAsync<StatusDto>(request);

// 方式二：便捷方法重载
var response = await WebClient.GetAsync<StatusDto>("api/status", "webapi");
```

也可以直接使用完整 URL（以 `http` 开头则跳过端点拼接）：

```csharp
var response = await WebClient.GetAsync<PostDto>("https://jsonplaceholder.typicode.com/posts/1");
```

## API 参考

### 核心请求

| 方法 | 说明 |
|------|------|
| `SendAsync<R>(Request)` | 核心异步请求，自动拼接 baseUrl、合并 headers、执行拦截器链 |
| `Send<R>(Request)` | Callback 模式，返回 `RequestHandle<R>` |

### Wait 模式（返回 `Task<Response<R>>`）

| 方法 | 说明 |
|------|------|
| `GetAsync<R>(url, headers?, timeout?, ct?)` | GET 请求 |
| `GetAsync<R>(url, endpointKey, headers?, timeout?, ct?)` | GET 请求 + 指定端点 |
| `PostAsync<R>(url, bodyJson, headers?, timeout?, ct?)` | POST 请求 |
| `PostAsync<R>(url, bodyJson, endpointKey, headers?, timeout?, ct?)` | POST + 指定端点 |
| `PutAsync<R>(url, bodyJson, headers?, timeout?, ct?)` | PUT 请求 |
| `PutAsync<R>(url, bodyJson, endpointKey, headers?, timeout?, ct?)` | PUT + 指定端点 |
| `DeleteAsync<R>(url, headers?, timeout?, ct?)` | DELETE 请求 |
| `DeleteAsync<R>(url, endpointKey, headers?, timeout?, ct?)` | DELETE + 指定端点 |

### Callback 模式（返回 `RequestHandle<R>`）

| 方法 | 说明 |
|------|------|
| `Get<R>(url, headers?, timeout?, ct?)` | GET 请求 |
| `Get<R>(url, endpointKey, headers?, timeout?, ct?)` | GET + 指定端点 |
| `Post<R>(url, bodyJson, headers?, timeout?, ct?)` | POST 请求 |
| `Post<R>(url, bodyJson, endpointKey, headers?, timeout?, ct?)` | POST + 指定端点 |
| `Put<R>(url, bodyJson, headers?, timeout?, ct?)` | PUT 请求 |
| `Put<R>(url, bodyJson, endpointKey, headers?, timeout?, ct?)` | PUT + 指定端点 |
| `Delete<R>(url, headers?, timeout?, ct?)` | DELETE 请求 |
| `Delete<R>(url, endpointKey, headers?, timeout?, ct?)` | DELETE + 指定端点 |

### 拦截器

```csharp
// 请求拦截器：修改请求参数
WebClient.UseRequestInterceptor(request =>
{
    request.headers["Authorization"] = "Bearer xxx";
    return request;
});

// 响应拦截器：预处理响应数据
WebClient.UseResponseInterceptor(response =>
{
    if (!response.IsSuccessStatusCode)
        Debug.LogError($"请求失败: {response.statusCode}");
    return response;
});

// 移除拦截器
WebClient.RemoveRequestInterceptor(interceptor);
WebClient.RemoveResponseInterceptor(interceptor);
```

### 全局 Headers

```csharp
WebClient.DefaultHeaders["Authorization"] = "Bearer xxx";
WebClient.DefaultHeaders["X-App-Version"] = Application.version;
```

### 环境切换

```csharp
// 运行时切换环境
WebClient.Instance.SetEnvironmentKey("prod");
```

### 请求配置

```csharp
var request = new Request
{
    url = "api/user",
    method = "GET",
    bodyJson = "{...}",
    headers = new Dictionary<string, string> { { "X-Custom", "value" } },
    timeoutSeconds = 30,
    endpointKey = "webapi",
    cancellationToken = token
};
```

## 设计说明
- 使用字符串环境键与端点键管理远程服务入口，避免把地址结构写死在代码里。
- 配置面板基于 UI Toolkit、UXML、USS 实现，适合在 Unity 编辑器内集中维护。
- 配置资源会优先全项目搜索；若不存在，则自动创建到 `Assets/Resources/VoyageForge/Config/BridgeConfig.asset`。
- 无论用户如何新增或删除环境，`dev` 都会被强制保留并保持在第一位。
- 相对路径自动拼接当前端点的 baseUrl；完整 URL（`http` 开头）直接请求，不拼接。
- 如果项目后续扩展新的服务类型，优先新增端点键，而不是继续堆叠专用字段。

## 维护建议
- 如果需要增加新的配置来源，实现 `IBridgeConfigProvider` 即可接入。
- 建议项目中只保留一份主配置资源，避免搜索到多份配置时产生歧义。
- 通过 `BridgeClient<T>` 泛型单例模式，不同服务可继承各自独立的客户端实例。