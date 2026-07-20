using System;
using Newtonsoft.Json;
using UnityEngine;

namespace VoyageForge.Bridge.Runtime
{
    /// <summary>
    /// 单条端点配置。一个环境可配置多条端点（如 default、webapi、socket）。
    /// 同时支持 Unity ScriptableObject 序列化和 Newtonsoft.JSON 序列化。
    /// </summary>
    [Serializable]
    public class EndpointConfig
    {
        [SerializeField] private string environmentKey;
        [SerializeField] private string endpointKey;
        [SerializeField] private string url;

        /// <summary>
        /// 获取或设置所属环境键。
        /// </summary>
        [JsonProperty("environmentKey")]
        public string EnvironmentKey
        {
            get => environmentKey;
            set => environmentKey = value;
        }

        /// <summary>
        /// 获取或设置端点键。
        /// </summary>
        [JsonProperty("endpointKey")]
        public string EndpointKey
        {
            get => endpointKey ?? "default";
            set => endpointKey = value;
        }

        /// <summary>
        /// 获取或设置端点地址。
        /// </summary>
        [JsonProperty("url")]
        public string Url
        {
            get => url ?? string.Empty;
            set => url = value;
        }
    }
}
