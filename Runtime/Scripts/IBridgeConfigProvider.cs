namespace VoyageForge.Bridge.Runtime
{
    /// <summary>
    /// Bridge 网络配置提供器接口。
    /// 用于按约定加载配置资源并返回当前环境。
    /// </summary>
    public interface IBridgeConfigProvider
    {
        /// <summary>
        /// 加载网络配置对象。
        /// </summary>
        /// <returns>网络配置实例。</returns>
        IBridgeConfig LoadConfig();

        /// <summary>
        /// 获取当前环境键。
        /// </summary>
        /// <returns>当前环境键。</returns>
        string GetEnvironment();
    }
}
