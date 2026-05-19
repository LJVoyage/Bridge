using System;
using System.Collections.Generic;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;
using VoyageForge.Depot.Runtime.Utilities;

namespace VoyageForge.Bridge.Runtime
{
    public class BridgeClient : Singleton<BridgeClient>
    {
        private IBridgeConfigProvider _configProvider;
        private IBridgeConfig _config;

        public const string UrlKey = "BridgeWebApi";
        public event Action<string> OnError;

        public void Init(IBridgeConfigProvider provider = null)
        {
            if (_configProvider != null) return;

            provider ??= new ResourcesBridgeConfigProvider();

            _configProvider = provider;
            _config = _configProvider.LoadConfig();
            _config.SetEnvironment();
        }

        public string GetBaseUrl(string key = UrlKey)
        {
            return _config.GetBaseUrl(key);
        }

        public void SetEnvironmentKey(string environmentKey = "")
        {
            if (string.IsNullOrEmpty(environmentKey))
                _config.SetEnvironment();
            else
                _config.SetEnvironment(environmentKey);
        }

        // 全局默认 headers
        public static Dictionary<string, string> DefaultHeaders { get; set; } = new();

        // 拦截器链
        private static readonly List<Func<Request, Request>> requestInterceptors = new();
        private static readonly List<Func<Response<string>, Response<string>>> responseInterceptors = new();

        /// <summary>
        /// 添加请求拦截器
        /// </summary>
        /// <param name="interceptor"></param>
        public static void UseRequestInterceptor(Func<Request, Request> interceptor)
        {
            requestInterceptors.Add(interceptor);
        }

        /// <summary>
        /// 添加响应拦截器
        /// </summary>
        /// <param name="interceptor"></param>
        public static void UseResponseInterceptor(Func<Response<string>, Response<string>> interceptor)
        {
            responseInterceptors.Add(interceptor);
        }

      

        #region 核心请求

        public static async Task<Response<T>> SendAsync<T>(Request request)
        {
            // baseURL
            if (!string.IsNullOrEmpty(Instance._config.GetBaseUrl(UrlKey)) && !request.url.StartsWith("http"))
                request.url = Instance._config.GetBaseUrl(UrlKey).TrimEnd('/') + "/" + request.url.TrimStart('/');

            // 合并全局 headers
            request.headers ??= new Dictionary<string, string>();
            foreach (var kv in DefaultHeaders)
                if (!request.headers.ContainsKey(kv.Key))
                    request.headers[kv.Key] = kv.Value;

            // 执行请求拦截器链
            foreach (var interceptor in requestInterceptors)
                request = interceptor.Invoke(request);

            using var uwr = new UnityWebRequest(request.url, request.method);
            uwr.downloadHandler = new DownloadHandlerBuffer();

            // body
            if (!string.IsNullOrEmpty(request.bodyJson))
            {
                byte[] bodyRaw = Encoding.UTF8.GetBytes(request.bodyJson);
                uwr.uploadHandler = new UploadHandlerRaw(bodyRaw);
                uwr.SetRequestHeader("Content-Type", "application/json");
            }

            // headers
            if (request.headers != null)
            {
                foreach (var kv in request.headers)
                    uwr.SetRequestHeader(kv.Key, kv.Value);
            }

            var op = uwr.SendWebRequest();
            float elapsed = 0f;
            float timeout = request.timeoutSeconds;

            while (!op.isDone)
            {
                if (request.cancellationToken.IsCancellationRequested)
                {
                    uwr.Abort();
                    Debug.LogWarning("[BridgeClient] 请求被取消: " + request.url);
                    return null;
                }

                if (timeout > 0)
                {
                    elapsed += Time.deltaTime;
                    if (elapsed > timeout)
                    {
                        uwr.Abort();
                        Debug.LogWarning("[BridgeClient] 请求超时: " + request.url);
                        return null;
                    }
                }

                await Task.Yield();
            }

            var response = new Response<T>
            {
              
                statusCode = (HttpStatusCode)uwr.responseCode,
                statusText = uwr.result.ToString(),
                headers = new Dictionary<string, string>()
            };
            
            response.statusCode = (HttpStatusCode)uwr.responseCode;
          
            
            if (uwr.result != UnityWebRequest.Result.Success)
            {
                Instance.OnError?.Invoke(uwr.error);
                return response;
            }

            string responseText = uwr.downloadHandler.text;
            T data = default;
            try
            {
                data = JsonUtility.FromJson<T>(responseText);
            }
            catch
            {
                Instance.OnError?.Invoke(uwr.error);
            }
            
            response.data = data;
           

            var rawResponse = new Response<string>
            {
                data = responseText,
                statusCode = response.statusCode,
                statusText = response.statusText,
                headers = response.headers
            };

            foreach (var interceptor in responseInterceptors)
                rawResponse = interceptor.Invoke(rawResponse);

            return response;
        }

        #endregion

        #region 快捷方法（统一使用 CancellationToken.None 默认值）

        public RequestHandle<T> Get<T>(string url, Dictionary<string, string> headers = null,
            int timeoutSeconds = 30, CancellationToken cancellationToken = default)
        {
            var req = new Request
            {
                url = url,
                method = "GET",
                headers = headers,
                timeoutSeconds = timeoutSeconds,
                cancellationToken = cancellationToken == default ? CancellationToken.None : cancellationToken
            };
            return new RequestHandle<T>(SendAsync<T>(req));
        }

        public RequestHandle<T> Post<T>(string url, string bodyJson,
            Dictionary<string, string> headers = null, int timeoutSeconds = 30,
            CancellationToken cancellationToken = default)
        {
            var req = new Request
            {
                url = url,
                method = "POST",
                bodyJson = bodyJson,
                headers = headers,
                timeoutSeconds = timeoutSeconds,
                cancellationToken = cancellationToken == default ? CancellationToken.None : cancellationToken
            };
            return new RequestHandle<T>(SendAsync<T>(req));
        }

        public RequestHandle<T> Put<T>(string url, string bodyJson,
            Dictionary<string, string> headers = null, int timeoutSeconds = 30,
            CancellationToken cancellationToken = default)
        {
            var req = new Request
            {
                url = url,
                method = "PUT",
                bodyJson = bodyJson,
                headers = headers,
                timeoutSeconds = timeoutSeconds,
                cancellationToken = cancellationToken == default ? CancellationToken.None : cancellationToken
            };
            return new RequestHandle<T>(SendAsync<T>(req));
        }

        public RequestHandle<T> Delete<T>(string url, Dictionary<string, string> headers = null,
            int timeoutSeconds = 30, CancellationToken cancellationToken = default)
        {
            var req = new Request
            {
                url = url,
                method = "DELETE",
                headers = headers,
                timeoutSeconds = timeoutSeconds,
                cancellationToken = cancellationToken == default ? CancellationToken.None : cancellationToken
            };
            return new RequestHandle<T>(SendAsync<T>(req));
        }

        #endregion
    }
}