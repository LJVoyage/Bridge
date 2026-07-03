using VoyageForge.Bridge.Runtime;

namespace VoyageForge.Bridge.Sample
{
    /// <summary>
    /// 
    /// </summary>
    public class WebClient : BridgeClient<WebClient>
    {
        protected override IBridgeConfigProvider ConfigProvider => _resourcesBridgeConfigProvider;

        /// <summary>
        /// 从resources 中 加载 配置文件
        /// </summary>
        private readonly ResourcesBridgeConfigProvider _resourcesBridgeConfigProvider = new();
        
        protected override string urlKey => "default";
        
    }
}