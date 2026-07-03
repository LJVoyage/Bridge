using System;
using System.Collections.Generic;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Cysharp.Threading.Tasks;
using Newtonsoft.Json;
using UnityEngine;
using UnityEngine.Networking;
using VoyageForge.Depot.Runtime.Utilities;

namespace VoyageForge.Bridge.Runtime
{
    public abstract class BridgeClient<T> : Singleton<T> where T : BridgeClient<T>, new()
    {
        protected abstract IBridgeConfigProvider ConfigProvider { get; }

        private IBridgeConfig Config
        {
            get
            {
                _config ??= ConfigProvider.LoadConfig();
                return _config;
            }
        }


        private IBridgeConfig _config;

        public static string UrlKey => Instance.urlKey;

        protected virtual string urlKey => "BridgeWebApi";

        public event Action<string> OnError;


        public string GetBaseUrl(string key)
        {
            return Config.GetBaseUrl(key);
        }

        public void SetEnvironmentKey(string environmentKey = "")
        {
            if (string.IsNullOrEmpty(environmentKey))
                Config.SetEnvironment();
            else
                Config.SetEnvironment(environmentKey);
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
        /// 移除请求拦截器
        /// </summary>
        /// <param name="interceptor"></param>
        public static void RemoveRequestInterceptor(Func<Request, Request> interceptor)
        {
            requestInterceptors.Remove(interceptor);
        }

        /// <summary>
        /// 添加响应拦截器
        /// </summary>
        /// <param name="interceptor"></param>
        public static void UseResponseInterceptor(Func<Response<string>, Response<string>> interceptor)
        {
            responseInterceptors.Add(interceptor);
        }

        /// <summary>
        /// 移除响应拦截器
        /// </summary>
        /// <param name="interceptor"></param>
        public static void RemoveResponseInterceptor(Func<Response<string>, Response<string>> interceptor)
        {
            responseInterceptors.Remove(interceptor);
        }


        #region 核心请求

        public static async Task<Response<R>> SendAsync<R>(Request request)
        {
            // baseURL
            if (!string.IsNullOrEmpty(Instance.Config.GetBaseUrl(UrlKey)) && !request.url.StartsWith("http"))
                request.url = Instance.Config.GetBaseUrl(UrlKey).TrimEnd('/') + "/" + request.url.TrimStart('/');

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

            var response = new Response<R>
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

            R data = default;

            try
            {
                data = JsonConvert.DeserializeObject<R>(responseText);
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

        #region 回调形式

        public static RequestHandle<R> Send<R>(Request request)
        {
            return new RequestHandle<R>(SendAsync<R>(request));
        }

        public RequestHandle<R> Get<R>(string url, Dictionary<string, string> headers = null,
            int timeoutSeconds = 30, CancellationToken cancellationToken = default)
        {
            var req = BuildRequest(url, "GET", null, headers, timeoutSeconds, cancellationToken);
            return Send<R>(req);
        }

        public RequestHandle<R> Post<R>(string url, string bodyJson,
            Dictionary<string, string> headers = null, int timeoutSeconds = 30,
            CancellationToken cancellationToken = default)
        {
            var req = BuildRequest(url, "POST", bodyJson, headers, timeoutSeconds, cancellationToken);
            return Send<R>(req);
        }

        public RequestHandle<R> Put<R>(string url, string bodyJson,
            Dictionary<string, string> headers = null, int timeoutSeconds = 30,
            CancellationToken cancellationToken = default)
        {
            var req = BuildRequest(url, "PUT", bodyJson, headers, timeoutSeconds, cancellationToken);
            return Send<R>(req);
        }

        public RequestHandle<R> Delete<R>(string url, Dictionary<string, string> headers = null,
            int timeoutSeconds = 30, CancellationToken cancellationToken = default)
        {
            var req = BuildRequest(url, "DELETE", null, headers, timeoutSeconds, cancellationToken);
            return Send<R>(req);
        }

        #endregion

        #region Wait 形式

        public static Task<Response<R>> GetAsync<R>(string url, Dictionary<string, string> headers = null,
            int timeoutSeconds = 30, CancellationToken cancellationToken = default)
        {
            var req = BuildRequest(url, "GET", null, headers, timeoutSeconds, cancellationToken);
            return SendAsync<R>(req);
        }

        public static Task<Response<R>> PostAsync<R>(string url, string bodyJson,
            Dictionary<string, string> headers = null, int timeoutSeconds = 30,
            CancellationToken cancellationToken = default)
        {
            var req = BuildRequest(url, "POST", bodyJson, headers, timeoutSeconds, cancellationToken);
            return SendAsync<R>(req);
        }

        public static Task<Response<R>> PutAsync<R>(string url, string bodyJson,
            Dictionary<string, string> headers = null, int timeoutSeconds = 30,
            CancellationToken cancellationToken = default)
        {
            var req = BuildRequest(url, "PUT", bodyJson, headers, timeoutSeconds, cancellationToken);
            return SendAsync<R>(req);
        }

        public static Task<Response<R>> DeleteAsync<R>(string url, Dictionary<string, string> headers = null,
            int timeoutSeconds = 30, CancellationToken cancellationToken = default)
        {
            var req = BuildRequest(url, "DELETE", null, headers, timeoutSeconds, cancellationToken);
            return SendAsync<R>(req);
        }

        #endregion

        private static Request BuildRequest(string url, string method, string bodyJson,
            Dictionary<string, string> headers, int timeoutSeconds, CancellationToken cancellationToken)
        {
            return new Request
            {
                url = url,
                method = method,
                bodyJson = bodyJson,
                headers = headers,
                timeoutSeconds = timeoutSeconds,
                cancellationToken = cancellationToken == default ? CancellationToken.None : cancellationToken
            };
        }
    }
}