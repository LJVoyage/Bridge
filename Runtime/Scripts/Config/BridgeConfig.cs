using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using UnityEngine.Networking;

namespace VoyageForge.Bridge.Runtime
{
    /// <summary>
    /// 基于 JSON 反序列化的 Bridge 网络配置。
    /// 纯 C# POCO 实现，不依赖 ScriptableObject，与 <see cref="BridgeConfigAsset"/> 行为一致。
    /// JSON 文件路径及各环境端点地址由 <see cref="JsonBridgeConfigProvider"/> 管理。
    /// </summary>
    [Serializable]
    public class BridgeConfig : IBridgeConfig
    {
        public const string ReservedEnvironmentKey = "dev";

        // ---- 公共属性：getter 纯数据返回，无副作用，反序列化安全 ----

        [JsonProperty("environmentKey")]
        public string EnvironmentKey
        {
            get => _environmentKey;
            set => _environmentKey = NormalizeKey(value);
        }

        [JsonProperty("environmentKeys")]
        public List<string> EnvironmentKeys
        {
            get => _environmentKeys;
            set => _environmentKeys = value ?? new List<string>();
        }

        [JsonProperty("endpoints")]
        public List<EndpointConfig> Endpoints
        {
            get => _endpoints;
            set => _endpoints = value ?? new List<EndpointConfig>();
        }

        // 私有字段：JsonIgnore 防止 Json.NET 直接序列化，通过属性访问
        [JsonIgnore] private string _environmentKey;
        [JsonIgnore] private List<string> _environmentKeys = new();
        [JsonIgnore] private List<EndpointConfig> _endpoints = new();
        [JsonIgnore] private bool _defaultsEnsured;

        // ============================================================
        // 工厂 / 序列化
        // ============================================================

        /// <summary>
        /// 创建包含默认 dev 环境的 BridgeConfig 实例。
        /// </summary>
        public static BridgeConfig CreateDefault()
        {
            var config = new BridgeConfig
            {
                EnvironmentKey = ReservedEnvironmentKey,
                EnvironmentKeys = new List<string> { ReservedEnvironmentKey },
                Endpoints = new List<EndpointConfig>
                {
                    new EndpointConfig
                    {
                        EnvironmentKey = ReservedEnvironmentKey,
                        EndpointKey = "default",
                        Url = "https://dev-api.example.com"
                    }
                }
            };
            config._defaultsEnsured = true;
            return config;
        }

        /// <summary>
        /// 将配置序列化为 JSON 字符串。写入前强制清理数据。
        /// </summary>
        /// <returns>格式化的 JSON 字符串。</returns>
        public string ToJson()
        {
            // 强制重置，确保写入前数据干净
            _defaultsEnsured = false;
            EnsureDefaults();
            return JsonConvert.SerializeObject(this, Formatting.Indented);
        }

        /// <summary>
        /// 从 JSON 字符串创建 BridgeConfig 实例。
        /// </summary>
        /// <param name="json">JSON 配置文本。</param>
        /// <returns>BridgeConfig 实例。</returns>
        public static BridgeConfig FromJson(string json)
        {
            if (string.IsNullOrWhiteSpace(json))
            {
                throw new ArgumentException("JSON 配置文本不能为空。", nameof(json));
            }

            var config = JsonConvert.DeserializeObject<BridgeConfig>(json);
            if (config == null)
            {
                throw new InvalidOperationException("无法从 JSON 文本反序列化 BridgeConfig。");
            }

            config.EnsureDefaults();
            return config;
        }

        // ============================================================
        // 运行时查询（GetBaseUrl / BuildFullUrl 调用 EnsureDefaults 保底）
        // ============================================================

        /// <summary>
        /// 获取当前环境下指定端点的基础地址。
        /// </summary>
        public string GetBaseUrl(string endpointKey = "default")
        {
            EnsureDefaults();

            var entry = _endpoints.FirstOrDefault(item =>
                item != null &&
                string.Equals(item.EnvironmentKey, _environmentKey, StringComparison.OrdinalIgnoreCase) &&
                string.Equals(item.EndpointKey, endpointKey, StringComparison.OrdinalIgnoreCase));

            if (entry != null && !string.IsNullOrWhiteSpace(entry.Url))
            {
                return entry.Url;
            }

            throw new InvalidOperationException(
                $"未找到环境\"{_environmentKey}\"下端点\"{endpointKey}\"的地址配置。");
        }

        /// <summary>
        /// 构建完整请求地址。
        /// </summary>
        public string BuildFullUrl(string endpointKey, string path, Dictionary<string, string> query = null)
        {
            var baseUri = new Uri(GetBaseUrl(endpointKey));
            var fullUri = new Uri(baseUri, path.TrimStart('/'));
            string url = fullUri.ToString();

            if (query == null || query.Count == 0)
            {
                return url;
            }

            var queryParts = new List<string>();
            foreach (var item in query)
            {
                queryParts.Add($"{item.Key}={UnityWebRequest.EscapeURL(item.Value)}");
            }

            return $"{url}?{string.Join("&", queryParts)}";
        }

        /// <summary>
        /// 设置当前启用的环境键。
        /// </summary>
        public void SetEnvironment(string environmentKey = ReservedEnvironmentKey)
        {
            EnvironmentKey = environmentKey;
        }

        // ============================================================
        // 编辑方法（编辑器通过 IBridgeConfig 接口调用）
        // ============================================================

        /// <summary>
        /// 添加环境。已存在则忽略。
        /// </summary>
        public bool AddEnvironment(string key)
        {
            string normalized = NormalizeKey(key);
            if (string.IsNullOrWhiteSpace(normalized) || ContainsEnvironment(normalized))
                return false;

            _environmentKeys.Add(normalized);
            EnsureReservedEnvironment();
            if (string.IsNullOrWhiteSpace(_environmentKey))
                _environmentKey = normalized;

            return true;
        }

        /// <summary>
        /// 删除环境及该环境下所有端点。dev 环境不可删除。
        /// </summary>
        public bool RemoveEnvironment(string key)
        {
            string normalized = NormalizeKey(key);
            if (string.IsNullOrWhiteSpace(normalized)) return false;
            if (string.Equals(normalized, ReservedEnvironmentKey, StringComparison.OrdinalIgnoreCase))
                return false;

            _environmentKeys.RemoveAll(e => string.Equals(e, normalized, StringComparison.OrdinalIgnoreCase));
            _endpoints.RemoveAll(e => e != null &&
                                      string.Equals(e.EnvironmentKey, normalized, StringComparison.OrdinalIgnoreCase));

            if (string.Equals(_environmentKey, normalized, StringComparison.OrdinalIgnoreCase))
                _environmentKey = ReservedEnvironmentKey;

            EnsureReservedEnvironment();
            return true;
        }

        /// <summary>
        /// 在指定环境下添加一条默认端点配置。
        /// </summary>
        public void AddEndpoint(string environmentKey)
        {
            string normalized = NormalizeKey(environmentKey);
            if (string.IsNullOrWhiteSpace(normalized)) return;

            if (!ContainsEnvironment(normalized))
                _environmentKeys.Add(normalized);

            _endpoints.Add(new EndpointConfig
            {
                EnvironmentKey = normalized,
                EndpointKey = "default",
                Url = string.Empty
            });
        }

        /// <summary>
        /// 删除指定环境下的第 localIndex 条端点。
        /// </summary>
        public void RemoveEndpoint(string environmentKey, int localIndex)
        {
            int n = 0;
            for (int i = 0; i < _endpoints.Count; i++)
            {
                var e = _endpoints[i];
                if (e != null && string.Equals(e.EnvironmentKey, environmentKey, StringComparison.OrdinalIgnoreCase))
                {
                    if (n == localIndex) { _endpoints.RemoveAt(i); return; }
                    n++;
                }
            }
        }

        // BridgeConfig 的 EnvironmentKeys (List<string>) 和 Endpoints (List<EndpointConfig>)
        // 已直接匹配 IBridgeConfig 接口，无需显式实现。

        // ============================================================
        // 内部数据清理（EnsureDefaults → CleanupData → EnsureReserved）
        // ============================================================

        /// <summary>
        /// 确保保底数据完整：去重、规范化、补齐 dev 环境。
        /// 仅在 FromJson / ToJson / GetBaseUrl 中显式调用，getter 中不触发。
        /// </summary>
        private void EnsureDefaults()
        {
            if (_defaultsEnsured) return;

            _environmentKeys ??= new List<string>();
            _endpoints ??= new List<EndpointConfig>();

            // 清理空值
            CleanupData();

            // 确保 dev 环境存在且位于首位
            EnsureReservedEnvironment();

            // 环境键规范化
            _environmentKey = NormalizeKey(_environmentKey);
            if (string.IsNullOrWhiteSpace(_environmentKey) ||
                !ContainsEnvironment(_environmentKey))
            {
                _environmentKey = ReservedEnvironmentKey;
            }

            _defaultsEnsured = true;
        }

        private void CleanupData()
        {
            // 清理环境列表
            var cleanedKeys = new List<string>();
            foreach (string key in _environmentKeys)
            {
                string normalizedKey = NormalizeKey(key);
                if (string.IsNullOrWhiteSpace(normalizedKey))
                {
                    continue;
                }

                if (cleanedKeys.Any(item =>
                        string.Equals(item, normalizedKey, StringComparison.OrdinalIgnoreCase)))
                {
                    continue;
                }

                cleanedKeys.Add(normalizedKey);
            }

            _environmentKeys = cleanedKeys;

            // 清理端点列表
            for (int index = _endpoints.Count - 1; index >= 0; index--)
            {
                var entry = _endpoints[index];
                if (entry == null)
                {
                    _endpoints.RemoveAt(index);
                    continue;
                }

                entry.EnvironmentKey = NormalizeKey(entry.EnvironmentKey);
                entry.EndpointKey = string.IsNullOrWhiteSpace(entry.EndpointKey)
                    ? "default"
                    : entry.EndpointKey.Trim();
                entry.Url = entry.Url?.Trim() ?? string.Empty;

                if (string.IsNullOrWhiteSpace(entry.EnvironmentKey))
                {
                    _endpoints.RemoveAt(index);
                }
            }
        }

        private void EnsureReservedEnvironment()
        {
            if (!ContainsEnvironment(ReservedEnvironmentKey))
            {
                _environmentKeys.Insert(0, ReservedEnvironmentKey);
                return;
            }

            // 确保 dev 位于首位
            int reservedIndex = _environmentKeys.FindIndex(item =>
                string.Equals(item, ReservedEnvironmentKey, StringComparison.OrdinalIgnoreCase));

            if (reservedIndex > 0)
            {
                _environmentKeys.RemoveAt(reservedIndex);
                _environmentKeys.Insert(0, ReservedEnvironmentKey);
            }
        }

        private bool ContainsEnvironment(string key)
        {
            return _environmentKeys.Any(item =>
                string.Equals(item, key, StringComparison.OrdinalIgnoreCase));
        }

        private static string NormalizeKey(string key)
        {
            return string.IsNullOrWhiteSpace(key) ? string.Empty : key.Trim();
        }
    }
}
