using UnityEditor;
using UnityEngine;
using VoyageForge.Bridge.Runtime;

namespace VoyageForge.Bridge.Editor
{
    /// <summary>
    /// Bridge 编辑器配置。
    /// 在 ProjectSettings 中保存配置资产引用和提供器选择。
    /// </summary>
    [FilePath("ProjectSettings/BridgeSettings.asset", FilePathAttribute.Location.ProjectFolder)]
    public sealed class BridgeSettings : ScriptableSingleton<BridgeSettings>
    {
        [SerializeField] private BridgeConfigAsset configAsset;
        [SerializeField] private string configProviderTypeName;

        public BridgeConfigAsset ConfigAsset => configAsset;

        /// <summary>
        /// 选中的 IBridgeConfigProvider 类型名，空表示未选择。
        /// </summary>
        public string ConfigProviderTypeName => configProviderTypeName;

        public void SetConfigAsset(BridgeConfigAsset asset)
        {
            configAsset = asset;
            SaveSettings();
        }

        public void SetConfigProviderType(string typeName)
        {
            configProviderTypeName = typeName;
            SaveSettings();
        }

        public void SaveSettings()
        {
            Save(true);
        }
    }
}
