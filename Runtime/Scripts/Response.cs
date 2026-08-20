using System;
using System.Collections.Generic;
using System.Net;

namespace VoyageForge.Bridge.Runtime
{
    /// <summary>
    /// 响应数据容器。
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class Response<T>
    {
        public T data;
        public HttpStatusCode statusCode; // 直接使用枚举
        public string statusText;
        public Dictionary<string, string> headers;
        
        public string RawText; // 新增原始响应文本

        /// <summary>
        /// 简单判断是否成功
        /// </summary>
        public bool IsSuccessStatusCode => (int)statusCode >= 200 && (int)statusCode < 300;
    }
}