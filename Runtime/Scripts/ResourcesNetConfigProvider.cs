using System.Linq;
using UnityEngine;

namespace LJVoyage.LJVNet.Runtime
{
    /// <summary>
    /// 从 Resources 中搜索网络配置的默认提供器。
    /// 该实现只是一个可选默认方案，使用方也可以自行实现 INetConfigProvider。
    /// </summary>
    public class ResourcesNetConfigProvider : INetConfigProvider
    {
        /// <summary>
        /// 从所有 Resources 目录中搜索第一份网络配置资源。
        /// </summary>
        /// <returns>网络配置实例。</returns>
        public INetConfig LoadConfig()
        {
            var configs = Resources.LoadAll<LJVNetConfigAsset>(string.Empty);
            if (configs == null || configs.Length == 0)
            {
                return null;
            }

            if (configs.Length > 1)
            {
                Debug.LogWarning("检测到多份 LJVNetConfig 配置资源，将使用搜索到的第一份配置。请只保留一份主配置资源。");
            }

            return configs.First();
        }

        /// <summary>
        /// 获取当前环境键。
        /// 当配置缺失时返回保底环境“开发”。
        /// </summary>
        /// <returns>当前环境键。</returns>
        public string GetEnvironment()
        {
            var config = LoadConfig();
            if (config == null)
            {
                Debug.LogError("未在 Resources 目录中搜索到 LJVNetConfig 配置资源。");
                return "开发";
            }

            return config.EnvironmentKey;
        }
    }
}
