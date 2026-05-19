using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Networking;

namespace VoyageForge.Bridge.Runtime
{
    /// <summary>
    /// 单条端点配置。
    /// 一个环境可以配置多条端点，例如 default、webapi、socket。
    /// </summary>
    [Serializable]
    public class EndpointConfig
    {
        /// <summary>
        /// 所属环境键。
        /// </summary>
        [SerializeField] private string environmentKey;

        /// <summary>
        /// 端点键。
        /// </summary>
        [SerializeField] private string endpointKey;

        /// <summary>
        /// 端点地址。
        /// </summary>
        [SerializeField] private string url;

        [SerializeField]
        private string test;

        /// <summary>
        /// 获取或设置所属环境键。
        /// </summary>
        public string EnvironmentKey
        {
            get => environmentKey;
            set => environmentKey = value;
        }

        /// <summary>
        /// 获取或设置端点键。
        /// </summary>
        public string EndpointKey
        {
            get => endpointKey;
            set => endpointKey = value;
        }

        /// <summary>
        /// 获取或设置端点地址。
        /// </summary>
        public string Url
        {
            get => url;
            set => url = value;
        }
    }

    /// <summary>
    /// Bridge 网络配置资源。
    /// 使用字符串环境键管理多环境、多端点地址。
    /// </summary>
    [CreateAssetMenu(fileName = "BridgeConfig", menuName = "VoyageForge/Bridge/Config")]
    public class BridgeConfigAsset : ScriptableObject, IBridgeConfig
    {
        /// <summary>
        /// 默认保底环境键。
        /// dev 环境始终保留，避免所有环境被误删后无法快速恢复。
        /// </summary>
        public const string ReservedEnvironmentKey = "dev";

        /// <summary>
        /// 旧版保底环境键，用于把历史配置迁移到 dev。
        /// </summary>
        private const string LegacyReservedEnvironmentKey = "测试";

        /// <summary>
        /// 新建配置时自动创建的默认环境列表。
        /// </summary>
        private static readonly string[] DefaultEnvironmentKeys = { ReservedEnvironmentKey };

        [SerializeField] private string environmentKey;
        [SerializeField] private List<string> environmentKeys = new();
        [SerializeField] private List<EndpointConfig> endpointEntries = new();
        [SerializeField] private bool defaultsInitialized;

        /// <summary>
        /// 获取或设置当前启用的环境键。
        /// 设置时会自动补入环境列表。
        /// </summary>
        public string EnvironmentKey
        {
            get
            {
                EnsureConfigData();
                return environmentKey;
            }
            set
            {
                EnsureConfigData();
                environmentKey = NormalizeKey(value);
                AddEnvironmentIfMissing(environmentKey);
            }
        }

        /// <summary>
        /// 获取全部环境键列表。
        /// </summary>
        public IReadOnlyList<string> EnvironmentKeys
        {
            get
            {
                EnsureConfigData();
                return environmentKeys;
            }
        }

        /// <summary>
        /// 获取全部端点配置。
        /// </summary>
        public IReadOnlyList<EndpointConfig> EndpointEntries
        {
            get
            {
                EnsureConfigData();
                return endpointEntries;
            }
        }

        /// <summary>
        /// 添加环境。
        /// </summary>
        public bool AddEnvironment(string key)
        {
            EnsureConfigData();
            string normalizedKey = NormalizeKey(key);
            if (string.IsNullOrWhiteSpace(normalizedKey) || ContainsEnvironment(normalizedKey))
            {
                return false;
            }

            environmentKeys.Add(normalizedKey);
            EnsureReservedEnvironmentFirst();
            if (string.IsNullOrWhiteSpace(environmentKey))
            {
                environmentKey = normalizedKey;
            }

            return true;
        }

        /// <summary>
        /// 删除环境，并同步删除该环境下的全部端点。
        /// </summary>
        public bool RemoveEnvironment(string key)
        {
            EnsureConfigData();
            string normalizedKey = NormalizeKey(key);
            if (string.IsNullOrWhiteSpace(normalizedKey))
            {
                return false;
            }

            if (string.Equals(normalizedKey, ReservedEnvironmentKey, StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            bool removed = false;
            for (int index = environmentKeys.Count - 1; index >= 0; index--)
            {
                if (string.Equals(environmentKeys[index], normalizedKey, StringComparison.OrdinalIgnoreCase))
                {
                    environmentKeys.RemoveAt(index);
                    removed = true;
                }
            }

            for (int index = endpointEntries.Count - 1; index >= 0; index--)
            {
                var entry = endpointEntries[index];
                if (entry != null &&
                    string.Equals(entry.EnvironmentKey, normalizedKey, StringComparison.OrdinalIgnoreCase))
                {
                    endpointEntries.RemoveAt(index);
                }
            }

            if (string.Equals(environmentKey, normalizedKey, StringComparison.OrdinalIgnoreCase))
            {
                environmentKey = ReservedEnvironmentKey;
            }

            EnsureReservedEnvironmentFirst();
            return removed;
        }

        /// <summary>
        /// 在指定环境下添加一条默认端点配置。
        /// </summary>
        public void AddEndpoint(string key)
        {
            EnsureConfigData();
            string normalizedKey = NormalizeKey(key);
            if (string.IsNullOrWhiteSpace(normalizedKey))
            {
                return;
            }

            AddEnvironmentIfMissing(normalizedKey);
            endpointEntries.Add(new EndpointConfig
            {
                EnvironmentKey = normalizedKey,
                EndpointKey = "default",
                Url = string.Empty
            });
        }

        /// <summary>
        /// 获取当前环境下指定端点的基础地址。
        /// </summary>
        public string GetBaseUrl(string endpointKey = "default")
        {
            EnsureConfigData();

            var entry = endpointEntries.FirstOrDefault(item =>
                item != null &&
                string.Equals(item.EnvironmentKey, environmentKey, StringComparison.OrdinalIgnoreCase) &&
                string.Equals(item.EndpointKey, endpointKey, StringComparison.OrdinalIgnoreCase));

            if (entry != null && !string.IsNullOrWhiteSpace(entry.Url))
            {
                return entry.Url;
            }

            throw new InvalidOperationException($"未找到环境“{environmentKey}”下端点“{endpointKey}”的地址配置。");
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

        public void SetEnvironment(string environmentKey)
        {
            EnvironmentKey =  environmentKey;
        }

        private void OnEnable()
        {
            EnsureConfigData();
        }

        private void OnValidate()
        {
            EnsureConfigData();
        }

        /// <summary>
        /// 初始化并清理配置数据。
        /// 新建资源时默认创建 dev 环境，
        /// 之后仅强制保留 dev 环境并让它始终位于第一项。
        /// </summary>
        private void EnsureConfigData()
        {
            environmentKeys ??= new List<string>();
            endpointEntries ??= new List<EndpointConfig>();
            bool useDefaultEnvironment = string.IsNullOrWhiteSpace(environmentKey) ||
                                         string.Equals(environmentKey, LegacyReservedEnvironmentKey,
                                             StringComparison.OrdinalIgnoreCase);

            if (!defaultsInitialized)
            {
                foreach (string defaultKey in DefaultEnvironmentKeys)
                {
                    if (!ContainsEnvironment(defaultKey))
                    {
                        environmentKeys.Add(defaultKey);
                    }
                }

                defaultsInitialized = true;
            }

            CleanupEnvironmentKeys();
            CleanupEndpointEntries();
            AddEnvironmentIfMissing(ReservedEnvironmentKey);
            EnsureReservedEnvironmentFirst();

            if (!string.IsNullOrWhiteSpace(environmentKey))
            {
                AddEnvironmentIfMissing(environmentKey);
                EnsureReservedEnvironmentFirst();
            }

            environmentKey = NormalizeKey(environmentKey);
            if (useDefaultEnvironment || string.IsNullOrWhiteSpace(environmentKey) ||
                !ContainsEnvironment(environmentKey))
            {
                environmentKey = ReservedEnvironmentKey;
            }
        }

        /// <summary>
        /// 清理环境列表中的空值与重复值。
        /// </summary>
        private void CleanupEnvironmentKeys()
        {
            var cleanedKeys = new List<string>();
            foreach (string key in environmentKeys)
            {
                string normalizedKey = NormalizeKey(key);
                if (string.IsNullOrWhiteSpace(normalizedKey))
                {
                    continue;
                }

                if (cleanedKeys.Any(item => string.Equals(item, normalizedKey, StringComparison.OrdinalIgnoreCase)))
                {
                    continue;
                }

                cleanedKeys.Add(normalizedKey);
            }

            environmentKeys = cleanedKeys;
        }

        /// <summary>
        /// 清理端点列表中的空值与无效值，并自动补齐环境。
        /// </summary>
        private void CleanupEndpointEntries()
        {
            for (int index = endpointEntries.Count - 1; index >= 0; index--)
            {
                var entry = endpointEntries[index];
                if (entry == null)
                {
                    endpointEntries.RemoveAt(index);
                    continue;
                }

                entry.EnvironmentKey = NormalizeKey(entry.EnvironmentKey);
                entry.EndpointKey = string.IsNullOrWhiteSpace(entry.EndpointKey) ? "default" : entry.EndpointKey.Trim();
                entry.Url = entry.Url?.Trim() ?? string.Empty;

                if (string.IsNullOrWhiteSpace(entry.EnvironmentKey))
                {
                    endpointEntries.RemoveAt(index);
                    continue;
                }

                AddEnvironmentIfMissing(entry.EnvironmentKey);
            }
        }

        /// <summary>
        /// 在环境不存在时自动补入环境列表。
        /// </summary>
        private void AddEnvironmentIfMissing(string key)
        {
            string normalizedKey = NormalizeKey(key);
            if (string.IsNullOrWhiteSpace(normalizedKey))
            {
                return;
            }

            if (!ContainsEnvironment(normalizedKey))
            {
                environmentKeys.Add(normalizedKey);
            }
        }

        /// <summary>
        /// 确保 dev 保底环境始终位于环境列表第一项。
        /// </summary>
        private void EnsureReservedEnvironmentFirst()
        {
            int reservedIndex = environmentKeys.FindIndex(item =>
                string.Equals(item, ReservedEnvironmentKey, StringComparison.OrdinalIgnoreCase));

            if (reservedIndex < 0)
            {
                return;
            }

            environmentKeys.RemoveAt(reservedIndex);
            environmentKeys.Insert(0, ReservedEnvironmentKey);
        }

        /// <summary>
        /// 判断环境列表中是否已存在指定环境。
        /// </summary>
        private bool ContainsEnvironment(string key)
        {
            return environmentKeys.Any(item => string.Equals(item, key, StringComparison.OrdinalIgnoreCase));
        }

        /// <summary>
        /// 规范化环境键，去除空白字符。
        /// </summary>
        private static string NormalizeKey(string key)
        {
            return string.IsNullOrWhiteSpace(key) ? string.Empty : key.Trim();
        }
    }
}