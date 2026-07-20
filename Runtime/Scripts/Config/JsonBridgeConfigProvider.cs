using System.IO;
using UnityEngine;

namespace VoyageForge.Bridge.Runtime
{
    /// <summary>
    /// 从 StreamingAssets 中加载 JSON 格式网络配置的提供器。
    /// 在编辑器模式下，如果配置文件不存在会自动创建默认配置。
    /// </summary>
    public class JsonBridgeConfigProvider : IBridgeConfigProvider
    {
        /// <summary>
        /// StreamingAssets 下的默认配置路径。
        /// </summary>
        public string DefaultRelativePath = Application.streamingAssetsPath + "/VoyageForge/Config/BridgeConfig.json";

        private readonly string _relativePath;
        private IBridgeConfig _cachedConfig;

        /// <summary>
        /// 无参构造，使用默认路径。
        /// </summary>
        public JsonBridgeConfigProvider() : this(null) { }

        /// <summary>
        /// 创建 JSON 配置提供器。
        /// </summary>
        /// <param name="relativePath">
        /// StreamingAssets 下的 JSON 文件相对路径。
        /// 例如 "VoyageForge/Config/BridgeConfig.json" 对应
        /// StreamingAssets/VoyageForge/Config/BridgeConfig.json。
        /// 留空则使用默认路径 <see cref="DefaultRelativePath"/>。
        /// </param>
        public JsonBridgeConfigProvider(string relativePath)
        {
            _relativePath = string.IsNullOrWhiteSpace(relativePath)
                ? DefaultRelativePath
                : relativePath;
        }

        /// <summary>
        /// 从 StreamingAssets 加载 JSON 配置文件并反序列化为 BridgeConfig。
        /// 首次加载后缓存结果，后续调用返回同一实例。
        /// 在编辑器模式下，文件不存在时会自动创建默认配置。
        /// </summary>
        /// <returns>网络配置实例，加载失败时返回 null。</returns>
        public IBridgeConfig LoadConfig()
        {
            if (_cachedConfig != null)
            {
                return _cachedConfig;
            }

            string fullPath = Path.Combine(Application.streamingAssetsPath, _relativePath);

#if UNITY_EDITOR
            if (!File.Exists(fullPath))
            {
                EnsureDirectoryExists(fullPath);
                var defaultConfig = BridgeConfig.CreateDefault();
                File.WriteAllText(fullPath, defaultConfig.ToJson());
                Debug.Log(
                    $"[Bridge] 已在 StreamingAssets 中创建默认 JSON 配置文件: {_relativePath}");
            }
#endif

            if (!File.Exists(fullPath))
            {
                Debug.LogError(
                    $"[Bridge] 未在 StreamingAssets 中找到 JSON 配置文件。期望路径: {fullPath}");
                return null;
            }

            string json = File.ReadAllText(fullPath);

            if (string.IsNullOrWhiteSpace(json))
            {
                Debug.LogError($"[Bridge] JSON 配置文件为空: {fullPath}");
                return null;
            }

            var config = BridgeConfig.FromJson(json);
            _cachedConfig = config;

            return _cachedConfig;
        }

        /// <summary>
        /// 获取当前环境键。
        /// </summary>
        /// <param name="key">未使用，保留参数兼容接口。</param>
        /// <returns>当前环境键，配置缺失时返回保底环境 dev。</returns>
        public string GetEnvironment(string key = null)
        {
            var config = LoadConfig();
            if (config == null)
            {
                Debug.LogWarning("[Bridge] JSON 配置不可用，返回保底环境 dev。");
                return BridgeConfig.ReservedEnvironmentKey;
            }

            return config.EnvironmentKey;
        }

        /// <summary>
        /// 保存配置到 StreamingAssets JSON 文件。
        /// </summary>
        public void SaveConfig(IBridgeConfig config)
        {
            if (config == null)
            {
                Debug.LogError("[Bridge] 无法保存空的配置。");
                return;
            }

            var bridgeConfig = config as BridgeConfig;
            if (bridgeConfig == null)
            {
                Debug.LogError($"[Bridge] JsonBridgeConfigProvider 仅支持保存 BridgeConfig 类型。");
                return;
            }

            string fullPath = Path.Combine(Application.streamingAssetsPath, _relativePath);
            EnsureDirectoryExists(fullPath);
            File.WriteAllText(fullPath, bridgeConfig.ToJson());

            _cachedConfig = null;
        }

        /// <summary>
        /// 清除缓存的配置，下次调用 LoadConfig 时重新从文件加载。
        /// </summary>
        public void ClearCache()
        {
            _cachedConfig = null;
        }

#if UNITY_EDITOR
        /// <summary>
        /// 确保配置文件所在目录存在。
        /// </summary>
        private static void EnsureDirectoryExists(string filePath)
        {
            string directory = Path.GetDirectoryName(filePath);
            if (!string.IsNullOrWhiteSpace(directory) && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }
        }
#else
        private static void EnsureDirectoryExists(string filePath) { }
#endif
    }
}
