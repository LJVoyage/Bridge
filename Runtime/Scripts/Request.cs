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

        // 默认使用 CancellationToken.None
        public CancellationToken cancellationToken = CancellationToken.None;
    }
}