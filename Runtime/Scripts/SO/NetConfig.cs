using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

namespace LJVoyage.LJVNet.Runtime
{
    [CreateAssetMenu(fileName = "NetConfig", menuName = "LJV/Net/Config")]
    public class NetConfig : ScriptableObject, INetConfig
    {
        public NetEnvironment currentEnvironment = NetEnvironment.Development;

        [Header("Development")] public string devBaseUrl = "http://localhost:8080";

        [Header("Testing")] public string testBaseUrl = "http://test.server.com";

        [Header("Production")] public string prodBaseUrl = "https://api.server.com";

        [Header("General")] public int timeoutSeconds = 10;

        public bool useAssetBundle = false;

        public string abPath;

        public string abAssetName = "NetConfig";



        /// <summary>
        /// 获取当前环境的基础URL。
        /// </summary>
        /// <returns></returns>
        /// <exception cref="ArgumentOutOfRangeException"></exception>
        public string GetBaseUrl()
        {
            return currentEnvironment switch
            {
                NetEnvironment.Development => devBaseUrl,
                NetEnvironment.Testing => testBaseUrl,
                NetEnvironment.Production => prodBaseUrl,
                _ => throw new ArgumentOutOfRangeException(nameof(currentEnvironment), currentEnvironment, null)
            };
        }
        
        public NetEnvironment Environment
        {
            get => currentEnvironment;
        }

        public string BaseUrl
        {
            get
            {
                return currentEnvironment switch
                {
                    NetEnvironment.Development => devBaseUrl,
                    NetEnvironment.Testing => testBaseUrl,
                    NetEnvironment.Production => prodBaseUrl,
                    _ => throw new ArgumentOutOfRangeException(nameof(currentEnvironment), currentEnvironment, null)
                };
            }
        }

        public string BuildFullUrl(string path, Dictionary<string, string> query = null)
        {
            var baseUri = new Uri(BaseUrl);

            var fullUri = new Uri(baseUri, path.TrimStart('/'));

            string url = fullUri.ToString();

            if (query != null && query.Count > 0)
            {
                List<string> list = new();
                foreach (var kv in query)
                    list.Add($"{kv.Key}={UnityWebRequest.EscapeURL(kv.Value)}");

                url += "?" + string.Join("&", list);
            }

            return url;
        }
    }
}