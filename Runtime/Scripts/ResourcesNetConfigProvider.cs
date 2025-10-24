using UnityEngine;

namespace LJVoyage.LJVNet.Runtime
{
    public class ResourcesNetConfigProvider : INetConfigProvider
    {
        public NetConfig LoadConfig()
        {
            return Resources.Load<NetConfig>("LJVNet/Config/NetworkConfig");
        }
    }
}