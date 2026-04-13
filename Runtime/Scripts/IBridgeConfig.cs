using System.Collections.Generic;

namespace VoyageForge.Bridge.Runtime
{
    /// <summary>
    /// Bridge 网络配置访问接口。
    /// 运行时通过该接口读取当前环境和端点地址。
    /// </summary>
    public interface IBridgeConfig
    {
        /// <summary>
        /// 获取当前启用的环境键。
        /// </summary>
        string EnvironmentKey { get; }

        /// <summary>
        /// 获取当前环境下指定端点的基础地址。
        /// </summary>
        /// <param name="endpointKey">端点键，默认使用 <c>default</c>。</param>
        /// <returns>端点基础地址。</returns>
        string GetBaseUrl(string endpointKey = "default");

        /// <summary>
        /// 根据端点键、路径和查询参数构建完整请求地址。
        /// </summary>
        /// <param name="endpointKey">端点键。</param>
        /// <param name="path">请求路径。</param>
        /// <param name="query">查询参数。</param>
        /// <returns>完整请求地址。</returns>
        string BuildFullUrl(string endpointKey, string path, Dictionary<string, string> query = null);
    }
}
