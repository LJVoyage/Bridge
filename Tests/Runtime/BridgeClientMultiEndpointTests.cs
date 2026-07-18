using System.Collections;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using VoyageForge.Bridge.Runtime;

namespace VoyageForge.Bridge.Tests.Runtime
{
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

        public string BuildFullUrl(string endpointKey, string path, System.Collections.Generic.Dictionary<string, string> query = null)
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

        public void SaveConfig(IBridgeConfig config)
        {
            // 测试提供器为内存实现，无需持久化
        }

        public string GetEnvironment(string key = null) => _config.EnvironmentKey;
    }

    internal class TestBridgeClient : BridgeClient<TestBridgeClient>
    {
        protected override IBridgeConfigProvider ConfigProvider => new TestBridgeConfigProvider();
        protected override string urlKey => "default";
    }

    public class BridgeClientMultiEndpointTests
    {
        [UnityTest]
        public IEnumerator TestFullUrl()
        {
            var request = new Request
            {
                url = "https://jsonplaceholder.typicode.com/posts/1",
                method = "GET"
            };

            var task = TestBridgeClient.SendAsync<PostDto>(request);
            
            yield return new WaitUntil(() => task.IsCompleted);

            var response = task.Result;
            Assert.IsNotNull(response, "Response should not be null");
            Assert.IsTrue(response.IsSuccessStatusCode, $"Expected success, got {response.statusCode}");
            Assert.IsNotNull(response.data, "Data should not be null");
            Assert.AreEqual(1, response.data.id);
        }

        [UnityTest]
        public IEnumerator TestDefaultEndpoint()
        {
            var request = new Request
            {
                url = "/users/1",
                method = "GET"
            };

            var task = TestBridgeClient.SendAsync<UserDto>(request);
            yield return new WaitUntil(() => task.IsCompleted);

            var response = task.Result;
            Assert.IsNotNull(response, "Response should not be null");
            Assert.IsTrue(response.IsSuccessStatusCode, $"Expected success, got {response.statusCode}");
            Assert.IsNotNull(response.data, "Data should not be null");
            Assert.AreEqual(1, response.data.id);
        }

        [UnityTest]
        public IEnumerator TestCustomEndpoint()
        {
            var request = new Request
            {
                url = "/get",
                method = "GET",
                endpointKey = "webapi"
            };

            var task = TestBridgeClient.SendAsync<HttpBinDto>(request);
            yield return new WaitUntil(() => task.IsCompleted);

            var response = task.Result;
            Assert.IsNotNull(response, "Response should not be null");
            Assert.IsTrue(response.IsSuccessStatusCode, $"Expected success, got {response.statusCode}");
            Assert.IsNotNull(response.data, "Data should not be null");
            Assert.IsTrue(response.data.url.Contains("httpbin.org"));
        }

        [UnityTest]
        public IEnumerator TestCustomEndpointWithGetAsync()
        {
            var task = TestBridgeClient.GetAsync<HttpBinDto>("/get", "webapi");
            yield return new WaitUntil(() => task.IsCompleted);

            var response = task.Result;
            Assert.IsNotNull(response, "Response should not be null");
            Assert.IsTrue(response.IsSuccessStatusCode, $"Expected success, got {response.statusCode}");
            Assert.IsNotNull(response.data);
        }
    }

    public class UserDto
    {
        public int id { get; set; }
        public string name { get; set; }
    }

    public class PostDto
    {
        public int id { get; set; }
        public string title { get; set; }
    }

    public class HttpBinDto
    {
        public string url { get; set; }
    }
}