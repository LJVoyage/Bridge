using System.Collections.Generic;
using System.Threading;

namespace VoyageForge.Bridge.Runtime
{
    public class Request
    {
        public string url;
        public string method;
        public string bodyJson;
        public Dictionary<string, string> headers;
        public int timeoutSeconds = 30;

        /// <summary>
        /// 指定使用的端点 key，若为 null 则使用 BridgeClient 子类的默认端点。
        /// </summary>
        public string endpointKey;

        // 默认使用 CancellationToken.None
        public CancellationToken cancellationToken = CancellationToken.None;
    }
}