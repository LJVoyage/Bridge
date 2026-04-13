using System;
using VoyageForge.Bridge.Runtime;
using UnityEditor;
using UnityEngine;

namespace VoyageForge.Bridge.Editor
{
    /// <summary>
    /// Bridge 环境快捷切换菜单。
    /// </summary>
    public static class BridgeEnvironmentMenu
    {
        private const string SettingsMenuPath = "VoyageForge/Bridge/Bridge Config";
        private const string EnvironmentMenuPath = "VoyageForge/Bridge/Bridge Environment/选择当前环境";

        [MenuItem(SettingsMenuPath, false, 100)]
        public static void OpenProjectSettings()
        {
            BridgeProjectSettingsProvider.OpenSettings();
        }

        [MenuItem(EnvironmentMenuPath, false, 101)]
        private static void ShowEnvironmentMenu()
        {
            var config = BridgeProjectSettingsProvider.GetOrCreateConfigAsset();
            if (config == null)
            {
                Debug.LogError("未找到或无法创建 BridgeConfig 配置资源。请先打开 Project Settings 检查配置。");
                return;
            }

            BridgeEnvironmentQuickSwitchWindow.Open(config);
        }

        [MenuItem(EnvironmentMenuPath, true)]
        private static bool ValidateEnvironmentMenu()
        {
            return BridgeProjectSettingsProvider.GetOrCreateConfigAsset() != null;
        }
    }

    /// <summary>
    /// Bridge 环境快速切换窗口。
    /// </summary>
    internal sealed class BridgeEnvironmentQuickSwitchWindow : EditorWindow
    {
        private BridgeConfigAsset config;

        /// <summary>
        /// 打开窗口并绑定配置对象。
        /// </summary>
        public static void Open(BridgeConfigAsset config)
        {
            var window = CreateInstance<BridgeEnvironmentQuickSwitchWindow>();
            window.config = config;
            window.titleContent = new GUIContent("快速切换环境");
            window.minSize = new Vector2(260f, 120f);
            window.maxSize = new Vector2(360f, 480f);
            window.ShowUtility();
        }

        /// <summary>
        /// 绘制窗口内容。
        /// </summary>
        private void OnGUI()
        {
            if (config == null)
            {
                EditorGUILayout.HelpBox("未找到 BridgeConfig 配置资源。", MessageType.Warning);
                if (GUILayout.Button("打开配置"))
                {
                    BridgeProjectSettingsProvider.OpenSettings();
                    Close();
                }

                return;
            }

            EditorGUILayout.Space(8f);
            EditorGUILayout.LabelField("快速切换环境", EditorStyles.boldLabel);
            EditorGUILayout.LabelField("点击下面的环境名称即可立即切换。", EditorStyles.wordWrappedLabel);
            EditorGUILayout.Space(8f);

            if (config.EnvironmentKeys.Count == 0)
            {
                EditorGUILayout.HelpBox("当前没有可用环境，请先到 Project Settings 中配置。", MessageType.Info);
            }
            else
            {
                foreach (string environmentKey in config.EnvironmentKeys)
                {
                    using (new EditorGUI.DisabledScope(string.Equals(config.EnvironmentKey, environmentKey, StringComparison.OrdinalIgnoreCase)))
                    {
                        string buttonText = string.Equals(config.EnvironmentKey, environmentKey, StringComparison.OrdinalIgnoreCase)
                            ? $"当前环境：{environmentKey}"
                            : environmentKey;

                        if (GUILayout.Button(buttonText, GUILayout.Height(28f)))
                        {
                            config.EnvironmentKey = environmentKey;
                            EditorUtility.SetDirty(config);
                            AssetDatabase.SaveAssets();
                            Debug.Log($"Bridge 当前环境已切换为：{environmentKey}");
                            Close();
                        }
                    }
                }
            }

            EditorGUILayout.Space(8f);
            if (GUILayout.Button("打开配置面板"))
            {
                BridgeProjectSettingsProvider.OpenSettings();
                Close();
            }
        }
    }
}
