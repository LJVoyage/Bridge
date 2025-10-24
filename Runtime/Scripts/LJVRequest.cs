using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

namespace LJVoyage.LJVNet.Runtime
{
   public class LJVRequest
    {
        private readonly string _method;
        private readonly string _path;
        private readonly object _body;
        private readonly Dictionary<string, string> _query = new();
        private Action<UnityWebRequest> _onBefore;
        private Action<string> _onRawResponse;
        private Action<Exception> _onError;
        private Delegate _onSuccess;

        public LJVRequest(string method, string path, object body = null)
        {
            _method = method;
            _path = path;
            _body = body;
        }

        // 链式配置
        public LJVRequest Query(string key, string value)
        {
            _query[key] = value;
            return this;
        }

        public LJVRequest OnBefore(Action<UnityWebRequest> callback)
        {
            _onBefore = callback;
            return this;
        }

        public LJVRequest OnResponse(Action<string> callback)
        {
            _onRawResponse = callback;
            return this;
        }

        public LJVRequest OnSuccess<T>(Action<T> callback)
        {
            _onSuccess = callback;
            return this;
        }

        public LJVRequest OnError(Action<Exception> callback)
        {
            _onError = callback;
            return this;
        }

        // 🔹 最终发送请求
        public void Send()
        {
            NetProxy.Instance.RunCoroutine(SendRoutine());
        }

        private IEnumerator SendRoutine()
        {
            string url = BuildUrl(_path, _query);
            using var req = new UnityWebRequest(url, _method);
            
            
            
            if (_method == UnityWebRequest.kHttpVerbPOST && _body != null)
            {
                string json = JsonUtility.ToJson(_body);
                req.uploadHandler = new UploadHandlerRaw(Encoding.UTF8.GetBytes(json));
                req.SetRequestHeader("Content-Type", "application/json");
            }

            req.downloadHandler = new DownloadHandlerBuffer();

            _onBefore?.Invoke(req);

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
                _onError?.Invoke(ex);
                yield break;
            }

            string resText = req.downloadHandler.text;
            _onRawResponse?.Invoke(resText);

            try
            {
                if (_onSuccess != null)
                {
                    Type t = _onSuccess.Method.GetParameters()[0].ParameterType;
                    object result = JsonUtility.FromJson(resText, t);
                    _onSuccess.DynamicInvoke(result);
                }
            }
            catch (Exception ex)
            {
                _onError?.Invoke(ex);
            }
        }

        private string BuildUrl(string path, Dictionary<string, string> query)
        {
            var baseUri = new Uri(NetConfigLoader.GetBaseUrl());
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