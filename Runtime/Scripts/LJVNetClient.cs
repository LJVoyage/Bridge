using System;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;

namespace LJVoyage.LJVNet.Runtime
{
    /// <summary>
    /// LJVNet 客户端入口。
    /// 当前负责初始化配置与提供简单的网络请求扩展示例。
    /// </summary>
    public static class LJVNetClient
    {
        /// <summary>
        /// 请求发送前回调。
        /// </summary>
        public static Action<UnityWebRequest> OnRequest;

        /// <summary>
        /// 响应成功后的文本处理回调。
        /// </summary>
        public static Func<string, string> OnResponse;

        /// <summary>
        /// 请求失败回调。
        /// </summary>
        public static Action<UnityWebRequest> OnError;

        private static INetConfigProvider configProvider;
        private static INetConfig config;

        /// <summary>
        /// 手动设置配置提供器实例。
        /// 使用方可以通过该方法决定配置从哪里加载。
        /// </summary>
        /// <param name="provider">配置提供器实例。</param>
        public static void SetConfigProvider(INetConfigProvider provider)
        {
            configProvider = provider ?? throw new ArgumentNullException(nameof(provider));
            config = null;
        }

        /// <summary>
        /// 使用泛型方式创建并设置配置提供器。
        /// </summary>
        /// <typeparam name="TProvider">提供器类型，必须带无参构造函数。</typeparam>
        public static void SetConfigProvider<TProvider>() where TProvider : INetConfigProvider, new()
        {
            SetConfigProvider(new TProvider());
        }

        /// <summary>
        /// 直接设置配置对象。
        /// 当外部已经自行完成配置加载时，可以跳过提供器。
        /// </summary>
        /// <param name="netConfig">网络配置对象。</param>
        public static void SetConfig(INetConfig netConfig)
        {
            config = netConfig ?? throw new ArgumentNullException(nameof(netConfig));
        }

        /// <summary>
        /// 获取当前已加载的网络配置。
        /// </summary>
        public static INetConfig Config
        {
            get
            {
                Init();
                return config;
            }
        }

        /// <summary>
        /// 初始化网络配置。
        /// 若未直接设置配置对象，则要求使用方先注册配置提供器。
        /// </summary>
        public static void Init()
        {
            if (config != null)
            {
                return;
            }

            if (configProvider == null)
            {
                throw new InvalidOperationException("LJVNet 尚未设置配置提供器，请先调用 SetConfigProvider 或 SetConfig。");
            }

            config = configProvider.LoadConfig();
            if (config == null)
            {
                throw new InvalidOperationException($"配置提供器 {configProvider.GetType().FullName} 未返回有效的网络配置。");
            }

            Debug.Log($"已加载 LJVNet 网络配置：{config}");
        }

        /// <summary>
        /// 发送一个测试请求。
        /// 该方法主要用于验证基础网络链路是否可用。
        /// </summary>
        /// <returns>响应文本。</returns>
        public static async UniTask<string> TestAsync()
        {
            using var request = UnityWebRequest.Get("http://google.co.jp");
            OnRequest?.Invoke(request);

            await request.SendWebRequest();

            if (request.result != UnityWebRequest.Result.Success)
            {
                OnError?.Invoke(request);
                return request.error;
            }

            string responseText = request.downloadHandler.text;
            return OnResponse != null ? OnResponse.Invoke(responseText) : responseText;
        }
    }
}
