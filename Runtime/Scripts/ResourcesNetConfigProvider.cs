using UnityEngine;

namespace LJVoyage.LJVNet.Runtime
{
    public class ResourcesNetConfigProvider : INetConfigProvider
    {
        public INetConfig LoadConfig()
        {
            return Resources.Load<NetConfig>("NetworkConfig");
        }

        public NetEnvironment GetEnvironment()
        {
            var config = LoadConfig();

            if (config == null)
            {
                Debug.LogError("NetworkConfig not found in Resources folder.");
                return NetEnvironment.Development;
            }

            return config.NetEnvironment;
        }
    }
}