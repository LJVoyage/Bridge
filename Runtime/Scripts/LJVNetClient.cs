using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

namespace LJVoyage.LJVNet.Runtime
{
    // ReSharper disable once IdentifierTypo
    public partial class LJVNet
    {
        // 拦截器
        public static Action<UnityWebRequest> OnRequest; // 请求前

        public static Func<string, string> OnResponse; // 响应后

        public static Action<Exception> OnError; // 错误时

        private static NetConfig _config;

        private static NetProxy _proxy;

        public static void Init(INetConfigProvider configProvider)
        {
            _config = configProvider.LoadConfig();

            _proxy = NetProxy.Instance;
        }

        // 🌐 对外接口：Get
        public static void Get<T>(string path, Action<T> onSuccess, Action<Exception> onFail = null,
            Dictionary<string, string> query = null)
        {
            _proxy.RunCoroutine(GetRoutine(path, onSuccess, onFail, query));
        }

        // 🌐 对外接口：Post
        public static void Post<T>(string path, object body, Action<T> onSuccess, Action<Exception> onFail = null)
        {
            _proxy.RunCoroutine(PostRoutine(path, body, onSuccess, onFail));
        }

        // 实际的协程逻辑
        private static IEnumerator GetRoutine<T>(string path, Action<T> onSuccess, Action<Exception> onFail,
            Dictionary<string, string> query)
        {
            string url = BuildUrl(path, query);
            using var req = UnityWebRequest.Get(url);
            yield return Send(req, onSuccess, onFail);
        }

        private static IEnumerator PostRoutine<T>(string path, object body, Action<T> onSuccess,
            Action<Exception> onFail)
        {
            string url = NetConfigLoader.GetBaseUrl() + path;
            string json = body != null ? JsonUtility.ToJson(body) : "{}";

            using var req = new UnityWebRequest(url, "POST");
            req.uploadHandler = new UploadHandlerRaw(Encoding.UTF8.GetBytes(json));
            req.downloadHandler = new DownloadHandlerBuffer();
            req.SetRequestHeader("Content-Type", "application/json");

            yield return Send(req, onSuccess, onFail);
        }

        // 统一请求发送逻辑
        private static IEnumerator Send<T>(UnityWebRequest req, Action<T> onSuccess, Action<Exception> onFail)
        {
            var cfg = NetConfigLoader.Config;

            // 请求拦截器
            OnRequest?.Invoke(req);

            req.timeout = cfg.timeoutSeconds;
            
            yield return req.SendWebRequest();

#if UNITY_2020_3_OR_NEWER
            bool hasError = req.result == UnityWebRequest.Result.ConnectionError ||
                            req.result == UnityWebRequest.Result.ProtocolError;
#else
            bool hasError = req.isHttpError || req.isNetworkError;
#endif

            if (hasError)
            {
                var ex = new Exception(req.error);
                OnError?.Invoke(ex);
                onFail?.Invoke(ex);
                yield break;
            }

            string resText = req.downloadHandler.text;


            if (OnResponse != null)
                resText = OnResponse.Invoke(resText);

            try
            {
                T result = JsonUtility.FromJson<T>(resText);
                onSuccess?.Invoke(result);
            }
            catch (Exception ex)
            {
                OnError?.Invoke(ex);
                onFail?.Invoke(ex);
            }
        }

        private static string BuildUrl(string path, Dictionary<string, string> query)
        {
            var baseUri = new Uri(_config.GetBaseUrl());

            var fullUri = new Uri(baseUri, path.TrimStart('/'));

            var url = fullUri.ToString();

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