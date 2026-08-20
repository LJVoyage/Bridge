using System;
using System.Collections.Generic;
using System.Net;
using System.Text;
using System.Threading;
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

        public IBridgeConfig Config
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

        public string GetBaseUrl(string key) => Config.GetBaseUrl(key);
        public void SetEnvironmentKey(string environmentKey = "")
        {
            if (string.IsNullOrEmpty(environmentKey))
                Config.SetEnvironment();
            else
                Config.SetEnvironment(environmentKey);
        }

        public static Dictionary<string, string> DefaultHeaders { get; set; } = new();

        private static readonly List<Func<Request, Request>> requestInterceptors = new();
        private static readonly List<Func<Response<string>, Response<string>>> responseInterceptors = new();

        public static void UseRequestInterceptor(Func<Request, Request> interceptor) => requestInterceptors.Add(interceptor);
        public static void RemoveRequestInterceptor(Func<Request, Request> interceptor) => requestInterceptors.Remove(interceptor);
        public static void UseResponseInterceptor(Func<Response<string>, Response<string>> interceptor) => responseInterceptors.Add(interceptor);
        public static void RemoveResponseInterceptor(Func<Response<string>, Response<string>> interceptor) => responseInterceptors.Remove(interceptor);

        // ======================== 核心发送方法 ========================
        public static async UniTask<Response<R>> SendAsync<R>(Request request) where R : class
        {
            // 1. BaseURL
            if (!request.url.StartsWith("http"))
                request.url = Instance.Config.GetBaseUrl(request.endpointKey).TrimEnd('/') + "/" + request.url.TrimStart('/');

            // 2. Headers
            request.headers ??= new Dictionary<string, string>();
            foreach (var kv in DefaultHeaders)
                if (!request.headers.ContainsKey(kv.Key))
                    request.headers[kv.Key] = kv.Value;

            // 3. 拦截器
            foreach (var interceptor in requestInterceptors)
                request = interceptor.Invoke(request);

            // 4. 创建 UnityWebRequest
            using var uwr = new UnityWebRequest(request.url, request.method);
            uwr.downloadHandler = new DownloadHandlerBuffer();

            if (request.timeoutSeconds > 0)
                uwr.timeout = request.timeoutSeconds;

            if (!string.IsNullOrEmpty(request.bodyJson))
            {
                byte[] bodyRaw = Encoding.UTF8.GetBytes(request.bodyJson);
                uwr.uploadHandler = new UploadHandlerRaw(bodyRaw);
                uwr.SetRequestHeader("Content-Type", "application/json");
            }

            if (request.headers != null)
            {
                foreach (var kv in request.headers)
                    uwr.SetRequestHeader(kv.Key, kv.Value);
            }

            // 5. 发送并捕获所有异常（包括超时、取消、网络错误）
            try
            {
                await uwr.SendWebRequest().ToUniTask(cancellationToken: request.cancellationToken);
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[BridgeClient] 请求异常: {ex.Message} (URL: {request.url})");
                var errorResponse = new Response<R>
                {
                    statusCode = 0,
                    statusText = ex.GetType().Name,
                    headers = new Dictionary<string, string>(),
                    data = null
                };
                if (ex is UnityWebRequestException webEx && webEx.UnityWebRequest != null)
                {
                    errorResponse.statusCode = (HttpStatusCode)webEx.UnityWebRequest.responseCode;
                    errorResponse.statusText = webEx.UnityWebRequest.result.ToString();
                }
                Instance.OnError?.Invoke(ex.Message);
                return errorResponse;
            }

            // 6. 成功处理
            var response = new Response<R>
            {
                statusCode = (HttpStatusCode)uwr.responseCode,
                statusText = uwr.result.ToString(),
                headers = new Dictionary<string, string>()
            };

            if (uwr.result != UnityWebRequest.Result.Success)
            {
                Instance.OnError?.Invoke(uwr.error);
                return response;
            }

            string responseText = uwr.downloadHandler.text;
            response.RawText = responseText; // 新增
            R data = default;

            if (typeof(R) == typeof(string))
            {
                data = (R)(object)responseText;
            }
            else
            {
                try
                {
                    data = JsonConvert.DeserializeObject<R>(responseText);
                }
                catch (Exception ex)
                {
                    Instance.OnError?.Invoke($"JSON 反序列化失败: {ex.Message}\n响应文本: {responseText}");
                }
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

        // ======================== 快捷方法 ========================
        public static UniTask<Response<R>> GetAsync<R>(string url, Dictionary<string, string> headers = null,
            int timeoutSeconds = 30, CancellationToken cancellationToken = default) where R : class
        {
            var req = BuildRequest(url, "GET", null, headers, timeoutSeconds, cancellationToken);
            return SendAsync<R>(req);
        }

        public static UniTask<Response<R>> GetAsync<R>(string url, string endpointKey,
            Dictionary<string, string> headers = null, int timeoutSeconds = 30,
            CancellationToken cancellationToken = default) where R : class
        {
            var req = BuildRequest(url, "GET", null, headers, timeoutSeconds, cancellationToken, endpointKey);
            return SendAsync<R>(req);
        }

        public static UniTask<Response<R>> PostAsync<R>(string url, string bodyJson,
            Dictionary<string, string> headers = null, int timeoutSeconds = 30,
            CancellationToken cancellationToken = default) where R : class
        {
            var req = BuildRequest(url, "POST", bodyJson, headers, timeoutSeconds, cancellationToken);
            return SendAsync<R>(req);
        }

        public static UniTask<Response<R>> PostAsync<R>(string url, string bodyJson, string endpointKey,
            Dictionary<string, string> headers = null, int timeoutSeconds = 30,
            CancellationToken cancellationToken = default) where R : class
        {
            var req = BuildRequest(url, "POST", bodyJson, headers, timeoutSeconds, cancellationToken, endpointKey);
            return SendAsync<R>(req);
        }

        public static UniTask<Response<R>> PutAsync<R>(string url, string bodyJson,
            Dictionary<string, string> headers = null, int timeoutSeconds = 30,
            CancellationToken cancellationToken = default) where R : class
        {
            var req = BuildRequest(url, "PUT", bodyJson, headers, timeoutSeconds, cancellationToken);
            return SendAsync<R>(req);
        }

        public static UniTask<Response<R>> PutAsync<R>(string url, string bodyJson, string endpointKey,
            Dictionary<string, string> headers = null, int timeoutSeconds = 30,
            CancellationToken cancellationToken = default) where R : class
        {
            var req = BuildRequest(url, "PUT", bodyJson, headers, timeoutSeconds, cancellationToken, endpointKey);
            return SendAsync<R>(req);
        }

        public static UniTask<Response<R>> DeleteAsync<R>(string url, Dictionary<string, string> headers = null,
            int timeoutSeconds = 30, CancellationToken cancellationToken = default) where R : class
        {
            var req = BuildRequest(url, "DELETE", null, headers, timeoutSeconds, cancellationToken);
            return SendAsync<R>(req);
        }

        public static UniTask<Response<R>> DeleteAsync<R>(string url, string endpointKey,
            Dictionary<string, string> headers = null, int timeoutSeconds = 30,
            CancellationToken cancellationToken = default) where R : class
        {
            var req = BuildRequest(url, "DELETE", null, headers, timeoutSeconds, cancellationToken, endpointKey);
            return SendAsync<R>(req);
        }

        private static Request BuildRequest(string url, string method, string bodyJson,
            Dictionary<string, string> headers, int timeoutSeconds, CancellationToken cancellationToken,
            string endpointKey = null)
        {
            return new Request
            {
                url = url,
                method = method,
                bodyJson = bodyJson,
                headers = headers,
                timeoutSeconds = timeoutSeconds,
                endpointKey = endpointKey ?? Instance.Config.EnvironmentKey,
                cancellationToken = cancellationToken == default ? CancellationToken.None : cancellationToken
            };
        }
    }
}