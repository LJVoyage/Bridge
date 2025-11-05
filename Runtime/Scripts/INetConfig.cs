using System.Collections.Generic;

namespace LJVoyage.LJVNet.Runtime
{
    public interface INetConfig
    {
        
        NetEnvironment NetEnvironment  { get;  }
        
        
        /// <summary>
        /// 获取当前环境的基础URL。
        /// </summary>
        string BaseUrl { get; }
        
        /// <summary>
        /// 拼接完整 URL，可在此处理路径、参数或签名逻辑
        /// </summary>
        /// <param name="path">请求路径</param>
        /// <param name="query">查询参数</param>
        /// <returns>完整的 URL</returns>
        string BuildFullUrl(string path, Dictionary<string, string> query = null);
    }
}