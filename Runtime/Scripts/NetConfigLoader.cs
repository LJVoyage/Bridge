using UnityEngine;

namespace LJVoyage.LJVNet.Runtime
{
    public static class NetConfigLoader
    {
        private static NetConfig _config;

        public static NetConfig Config
        {
            get
            {
                if (_config == null)
                    Load();
                return _config;
            }
        }

        public static void Load()
        {
            _config = Resources.Load<NetConfig>("NetConfig");
            if (_config == null)
            {
                Debug.LogError("[NetConfigLoader] NetConfig not found in Resources!");
                return;
            }

            if (_config.useAssetBundle)
            {
                LoadFromAssetBundle();
            }
        }

        private static void LoadFromAssetBundle()
        {
            var ab = AssetBundle.LoadFromFile(_config.abPath);
            if (ab == null)
            {
                Debug.LogError("[NetConfigLoader] Failed to load AB at " + _config.abPath);
                return;
            }

            var abConfig = ab.LoadAsset<NetConfig>(_config.abAssetName);
            if (abConfig != null)
                _config = abConfig;

            ab.Unload(false);
        }

        public static string GetBaseUrl()
        {
            switch (Config.currentEnvironment)
            {
                case NetEnvironment.Development:
                    return Config.devBaseUrl;
                case NetEnvironment.Testing:
                    return Config.testBaseUrl;
                case NetEnvironment.Production:
                    return Config.prodBaseUrl;
            }
            return "";
        }
    }
}