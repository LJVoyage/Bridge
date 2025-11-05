namespace LJVoyage.LJVNet.Runtime
{
    /// <summary>
    /// 网络配置提供器接口
    /// </summary>
    public interface INetConfigProvider
    {
        INetConfig LoadConfig();
        
        NetEnvironment GetEnvironment();
        
    }
}