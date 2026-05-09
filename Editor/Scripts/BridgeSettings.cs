using UnityEditor;
using UnityEngine;
using VoyageForge.Bridge.Runtime;

namespace VoyageForge.Bridge.Editor
{
    /// <summary>
    /// Bridge 编辑器配置。
    /// 用于在 Project Settings 中保存当前项目使用的 Bridge 配置 SO 引用。
    /// </summary>
    [FilePath("ProjectSettings/BridgeSettings.asset", FilePathAttribute.Location.ProjectFolder)]
    public sealed class BridgeSettings : ScriptableSingleton<BridgeSettings>
    {
        /// <summary>
        /// 当前项目使用的 Bridge 配置资产。
        /// </summary>
        [SerializeField] private BridgeConfigAsset configAsset;

        /// <summary>
        /// 当前项目使用的 Bridge 配置资产。
        /// </summary>
        public BridgeConfigAsset ConfigAsset => configAsset;

        /// <summary>
        /// 设置当前项目使用的 Bridge 配置资产。
        /// </summary>
        /// <param name="asset">Bridge 配置资产。</param>
        public void SetConfigAsset(BridgeConfigAsset asset)
        {
            configAsset = asset;
            SaveSettings();
        }

        /// <summary>
        /// 保存 Bridge 编辑器配置到 ProjectSettings。
        /// </summary>
        public void SaveSettings()
        {
            Save(true);
        }
    }
}
