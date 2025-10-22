using System;
using UnityEngine;

namespace LJVoyage.LJVNet.Runtime
{
    [CreateAssetMenu(fileName = "NetConfig", menuName = "LJV/Net/Config")]
    public class NetConfig : ScriptableObject
    {
        public NetEnvironment currentEnvironment = NetEnvironment.Development;

        [Header("Development")]
        public string devBaseUrl = "http://localhost:8080";

        [Header("Testing")]
        public string testBaseUrl = "http://test.server.com";

        [Header("Production")]
        public string prodBaseUrl = "https://api.server.com";

        [Header("General")]
        public int timeoutSeconds = 10;
        public bool useAssetBundle = false;
        public string abPath;
        public string abAssetName = "NetConfig";

       
        
        
    }
}