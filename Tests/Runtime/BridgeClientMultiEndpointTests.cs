using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Cysharp.Threading.Tasks;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using VoyageForge.Bridge.Runtime;

namespace VoyageForge.Bridge.Tests.Runtime
{
    // ---------- 测试配置 ----------
    internal class TestBridgeConfig : IBridgeConfig
    {
        public string EnvironmentKey { get; set; } = "test";
        public List<string> EnvironmentKeys { get; } = new List<string> { "test" };
        public List<EndpointConfig> Endpoints { get; } = new List<EndpointConfig>
        {
            new EndpointConfig { EnvironmentKey = "test", EndpointKey = "default", Url = "https://jsonplaceholder.typicode.com" },
            new EndpointConfig { EnvironmentKey = "test", EndpointKey = "webapi", Url = "https://httpbin.org" }
        };

        public string GetBaseUrl(string endpointKey)
        {
            if (string.IsNullOrEmpty(endpointKey)) endpointKey = "default";
            var match = Endpoints.FirstOrDefault(e => e.EndpointKey == endpointKey);
            return match?.Url ?? Endpoints.First(e => e.EndpointKey == "default").Url;
        }

        public string BuildFullUrl(string endpointKey, string path, Dictionary<string, string> query = null)
        {
            var baseUrl = GetBaseUrl(endpointKey).TrimEnd('/');
            var url = baseUrl + "/" + path.TrimStart('/');
            if (query != null && query.Count > 0)
                url += "?" + string.Join("&", query.Select(kv => $"{kv.Key}={kv.Value}"));
            return url;
        }

        public void SetEnvironment(string environmentKey = null)
        {
            if (!string.IsNullOrWhiteSpace(environmentKey))
                EnvironmentKey = environmentKey;
        }
    }

    internal class TestBridgeConfigProvider : IBridgeConfigProvider
    {
        private readonly TestBridgeConfig _config = new();
        public IBridgeConfig LoadConfig() => _config;
        public void SaveConfig(IBridgeConfig config) { }
        public string GetEnvironment(string key = null) => _config.EnvironmentKey;
    }

    internal class TestBridgeClient : BridgeClient<TestBridgeClient>
    {
        protected override IBridgeConfigProvider ConfigProvider => new TestBridgeConfigProvider();
        protected override string urlKey => "default";
    }

    // ---------- 测试类 ----------
    public class BridgeClientMultiEndpointTests
    {
        [UnityTest]
        public IEnumerator TestFullUrl()
        {
            var task = TestBridgeClient.GetAsync<string>("https://jsonplaceholder.typicode.com/posts/1");
            var awaiter = task.GetAwaiter();
            while (!awaiter.IsCompleted) yield return null;

            var response = awaiter.GetResult();
            Assert.IsNotNull(response);
            Assert.IsTrue(response.IsSuccessStatusCode, $"StatusCode: {response.statusCode}");
            Assert.IsNotNull(response.data);
            // 兼容 JSON 格式化（可能带空格）
            Assert.IsTrue(response.data.Contains("\"id\": 1") || response.data.Contains("\"id\":1"), $"Response missing 'id:1': {response.data}");
        }

        [UnityTest]
        public IEnumerator TestDefaultEndpoint()
        {
            var task = TestBridgeClient.GetAsync<string>("/users/1");
            var awaiter = task.GetAwaiter();
            while (!awaiter.IsCompleted) yield return null;

            var response = awaiter.GetResult();
            Assert.IsNotNull(response);
            Assert.IsTrue(response.IsSuccessStatusCode);
            Assert.IsNotNull(response.data);
            Assert.IsTrue(response.data.Contains("\"id\": 1") || response.data.Contains("\"id\":1"));
        }

        [UnityTest]
        public IEnumerator TestCustomEndpoint()
        {
            var task = TestBridgeClient.GetAsync<string>("/get", "webapi");
            var awaiter = task.GetAwaiter();
            while (!awaiter.IsCompleted) yield return null;

            var response = awaiter.GetResult();
            Assert.IsNotNull(response);
            Assert.IsTrue(response.IsSuccessStatusCode);
            Assert.IsNotNull(response.data);
            // 检查包含 "url" 字段和 "httpbin.org"
            Assert.IsTrue(response.data.Contains("\"url\"") && response.data.Contains("httpbin.org"), $"Response missing url or host: {response.data}");
        }

        [UnityTest]
        public IEnumerator TestTimeout_ShouldFailFast()
        {
            var request = new Request
            {
                url = "https://httpbin.org/delay/5",
                method = "GET",
                timeoutSeconds = 2
            };
            var task = TestBridgeClient.SendAsync<string>(request);
            var awaiter = task.GetAwaiter();
            float start = Time.time;
            while (!awaiter.IsCompleted) yield return null;
            float elapsed = Time.time - start;

            Assert.IsTrue(elapsed < 3f, $"Timeout took {elapsed}s, expected <3s");

            var response = awaiter.GetResult();
            Assert.IsNotNull(response);
            Assert.IsFalse(response.IsSuccessStatusCode, "Should fail due to timeout");
            Assert.IsNull(response.data);
        }

        [UnityTest]
        public IEnumerator TestLongRunningRequest_ShouldSucceed()
        {
            var request = new Request
            {
                url = "https://httpbin.org/delay/3",
                method = "GET",
                timeoutSeconds = 30
            };
            var task = TestBridgeClient.SendAsync<string>(request);
            var awaiter = task.GetAwaiter();
            float start = Time.time;
            while (!awaiter.IsCompleted) yield return null;
            float elapsed = Time.time - start;

            // 允许网络波动，放宽到 2~12 秒
            Assert.IsTrue(elapsed >= 2f && elapsed < 12f, $"Request took {elapsed}s, expected ~3s");

            var response = awaiter.GetResult();
            Assert.IsNotNull(response);
            Assert.IsTrue(response.IsSuccessStatusCode, $"StatusCode: {response.statusCode}, StatusText: {response.statusText}");
            Assert.IsNotNull(response.data);

            // 验证响应是包含 'url' 字段的 JSON（httpbin /delay 返回的就是 /get 的响应）
            Assert.IsTrue(response.data.Contains("\"url\"") && response.data.Contains("httpbin.org"), $"Response missing url or host: {response.data}");
        }
    }
}