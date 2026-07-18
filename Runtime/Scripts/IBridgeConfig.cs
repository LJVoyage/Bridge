using System.Collections.Generic;

namespace VoyageForge.Bridge.Runtime
{
    /// <summary>
    /// Bridge 网络配置访问接口。
    /// 提供运行时查询和编辑器可视化编辑所需的全部数据与方法。
    /// </summary>
    public interface IBridgeConfig
    {
        const string DefaultEnvironmentKey = "dev";

        /// <summary>
        /// 获取或设置当前启用的环境键。设置时自动写入环境列表。
        /// </summary>
        string EnvironmentKey { get; set; }

        /// <summary>
        /// 可读写环境键列表。编辑器可直接增删。
        /// </summary>
        List<string> EnvironmentKeys { get; }

        /// <summary>
        /// 可读写端点列表。编辑器可直接增删改。
        /// </summary>
        List<EndpointConfig> Endpoints { get; }

        /// <summary>
        /// 获取当前环境下指定端点的基础地址。
        /// </summary>
        string GetBaseUrl(string endpointKey = "default");

        /// <summary>
        /// 根据端点键、路径和查询参数构建完整请求地址。
        /// </summary>
        string BuildFullUrl(string endpointKey, string path, Dictionary<string, string> query = null);

        /// <summary>
        /// 设置当前启用的环境键。
        /// </summary>
        void SetEnvironment(string environmentKey = DefaultEnvironmentKey);
    }
}
