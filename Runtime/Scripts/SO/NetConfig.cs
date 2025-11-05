using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.Serialization;

namespace LJVoyage.LJVNet.Runtime
{
    [CreateAssetMenu(fileName = "NetConfig", menuName = "LJV/Net/Config")]
    public class NetConfig : ScriptableObject, INetConfig
    {
        [SerializeField] private NetEnvironment _netEnvironment = NetEnvironment.Development;

        public NetEnvironment  NetEnvironment
        {
            
                get => _netEnvironment;
                set => _netEnvironment = value;
        }
        

        [Header("Development")][SerializeField] private string devBaseUrl = "http://localhost:8080";

        [Header("Testing")] [SerializeField] private string testBaseUrl = "http://test.server.com";

        [Header("Production")] [SerializeField] private string prodBaseUrl = "https://api.server.com";
        
        public string BaseUrl
        {
            get
            {
                return _netEnvironment switch
                {
                    NetEnvironment.Development => devBaseUrl,
                    NetEnvironment.Testing => testBaseUrl,
                    NetEnvironment.Production => prodBaseUrl,
                    _ => throw new ArgumentOutOfRangeException(nameof(_netEnvironment), _netEnvironment, null)
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