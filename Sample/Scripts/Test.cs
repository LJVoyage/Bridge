using System;
using System.Threading.Tasks;
using UnityEngine;
using VoyageForge.Bridge.Runtime;

namespace VoyageForge.Bridge.Sample
{
    public class Test : MonoBehaviour
    {
        private async void Start()
        {
            await TestDefaultEndpoint();
            await TestCustomEndpoint();
        }

        private async Task TestDefaultEndpoint()
        {
            Debug.Log("=== Test 1: 默认端点 (不指定 endpointKey) ===");

            var response = await WebClient.GetAsync<UserDto>("https://jsonplaceholder.typicode.com/users/1");

            if (response != null && response.IsSuccessStatusCode)
            {
                Debug.Log($"[默认端点] 请求成功: id={response.data?.id}, name={response.data?.name}");
            }
            else
            {
                Debug.LogWarning($"[默认端点] 请求失败: {response?.statusCode}");
            }
        }

        private async Task TestCustomEndpoint()
        {
            Debug.Log("=== Test 2: 自定义端点 (endpointKey = \"webapi\") ===");

            var request = new Request
            {
                url = "https://httpbin.org/get",
                method = "GET",
                endpointKey = "webapi"
            };
            var response = await WebClient.SendAsync<HttpBinDto>(request);

            if (response != null && response.IsSuccessStatusCode)
            {
                Debug.Log($"[自定义端点 webapi] 请求成功: url={response.data?.url}");
            }
            else
            {
                Debug.LogWarning($"[自定义端点 webapi] 请求失败: {response?.statusCode}");
            }
        }
    }

    public class UserDto
    {
        public int id { get; set; }
        public string name { get; set; }
    }

    public class HttpBinDto
    {
        public string url { get; set; }
    }
}