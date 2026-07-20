using System.Linq;
using UnityEngine;
using VoyageForge.Bridge.Runtime;

namespace VoyageForge.Bridge.Sample
{
    /// <summary>
    /// 从 Resources 中搜索网络配置的默认提供器。
    /// 该实现只是一个可选默认方案，使用方也可以自行实现 IBridgeConfigProvider。
    /// </summary>
    public class ResourcesBridgeConfigProvider : IBridgeConfigProvider
    {
        /// <summary>
        /// Resources 下的默认配置路径（不含扩展名）。
        /// </summary>
        public const string DefaultResourcesPath = "VoyageForge/Config/BridgeConfig";

        /// <summary>
        /// 配置资源路径。
        /// </summary>
        private readonly string _path;

        /// <summary>
        /// 无参构造，使用默认路径。
        /// </summary>
        public ResourcesBridgeConfigProvider() : this(DefaultResourcesPath) { }

        /// <summary>
        /// 创建 Resources 配置提供器。
        /// </summary>
        /// <param name="path">Resources 目录下的配置路径（不含扩展名）。</param>
        public ResourcesBridgeConfigProvider(string path)
        {
            _path = path;
        }


        /// <summary>
        /// 从所有 Resources 目录中搜索第一份网络配置资源。
        /// </summary>
        /// <returns>网络配置实例。</returns>
        public IBridgeConfig LoadConfig()
        {
            var configs = Resources.LoadAll<BridgeConfigAsset>(_path);

            
            if (configs == null || configs.Length == 0)
            {
                Debug.LogError("未在 Resources 目录中搜索到 BridgeConfig 配置资源。");
                return null;
            }

            if (configs.Length > 1)
            {
                Debug.LogWarning("检测到多份 BridgeConfig 配置资源，将使用搜索到的第一份配置。请只保留一份主配置资源。");
            }

            return configs.First();
        }


        /// <summary>
        /// 保存配置到 Resources 中的 ScriptableObject 资源。
        /// </summary>
        /// <param name="config">网络配置实例。</param>
        public void SaveConfig(IBridgeConfig config)
        {
#if UNITY_EDITOR
            var asset = config as BridgeConfigAsset;
            if (asset == null)
            {
                Debug.LogError($"[Bridge] ResourcesBridgeConfigProvider 仅支持保存 BridgeConfigAsset 类型。");
                return;
            }

            UnityEditor.EditorUtility.SetDirty(asset);
            UnityEditor.AssetDatabase.SaveAssets();
#endif
        }

        /// <summary>
        /// 获取当前环境键。
        /// 当配置缺失时返回保底环境 dev。
        /// </summary>
        /// <returns>当前环境键。</returns>
        public string GetEnvironment(string key = null)
        {
            var config = LoadConfig();
            if (config == null)
            {
                throw new System.Exception("未在 Resources 目录中搜索到 BridgeConfig 配置资源。");
                // return BridgeConfigAsset.ReservedEnvironmentKey;
            }

            return config.EnvironmentKey;
        }
    }
}