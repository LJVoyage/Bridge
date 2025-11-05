using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;

namespace LJVoyage.LJVNet.Runtime
{
    // ReSharper disable once IdentifierTypo
    public class LJVNetClient
    {
        /// <summary>
        /// 配置文件路径
        /// </summary>
        public const string LOADER_CONFIG_PATH = "LJVNet/Config/NetLoaderConfig";

        // 拦截器
        public static Action<UnityWebRequest> OnRequest; // 请求前

        public static Func<string, string> OnResponse; // 响应后

        public static Action<UnityWebRequest> OnError; // 错误时

        private static INetConfig _config;

        private static NetLoaderConfig _loaderConfig;

        public static void Init()
        {
            if (_loaderConfig == null)
            {
                _loaderConfig = Resources.Load<NetLoaderConfig>(LOADER_CONFIG_PATH);
                if (_loaderConfig == null)
                    throw new Exception($"未找到配置文件 {LOADER_CONFIG_PATH}");

                Debug.Log($"加载配置 {_loaderConfig}");
            }

            if (_config == null)
            {
                Type type = null;

                foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
                {
                    type = assembly.GetType(_loaderConfig.providerTypeName);
                    if (type != null)
                        break;
                }

                if (type == null)
                    throw new Exception($"未找到类型 {_loaderConfig.providerTypeName}");


                _config = ((INetConfigProvider)Activator.CreateInstance(type)).LoadConfig();

                Debug.Log($"加载配置 {_config}");
            }
        }

        public static async UniTask<string> TestAsync()
        {
            using var request = UnityWebRequest.Get("http://google.co.jp");

            await request.SendWebRequest();

            if (request.result != UnityWebRequest.Result.Success)
            {
                OnError?.Invoke(request);
            }

            return request.downloadHandler.text;
        }
    }
}