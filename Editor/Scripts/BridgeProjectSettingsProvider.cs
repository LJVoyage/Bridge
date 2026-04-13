using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using VoyageForge.Bridge.Runtime;
using VoyageForge.Depot.Editor.Scripts.Utilities;

namespace VoyageForge.Bridge.Editor
{
    /// <summary>
    /// Bridge 项目设置提供器。
    /// 负责在 Project Settings 中展示并维护唯一一份网络配置资源。
    /// </summary>
    public static class BridgeProjectSettingsProvider
    {
        private const string SettingsPath = "Project/Bridge";
        private const string DefaultConfigDirectory = "Assets/Resources/Config";
        private const string DefaultConfigAssetPath = DefaultConfigDirectory + "/BridgeConfig.asset";
        private const string UxmlPath = "Assets/Bridge/Editor/Scripts/BridgeProjectSettingsView.uxml";
        private const string UssPath = "Assets/Bridge/Editor/Styles/BridgeProjectSettings.uss";

        [SettingsProvider]
        public static SettingsProvider CreateSettingsProvider()
        {
            return new SettingsProvider(SettingsPath, SettingsScope.Project)
            {
                label = "Bridge",
                activateHandler = (_, rootElement) => BuildUi(rootElement),
                keywords = new HashSet<string>(new[] { "Bridge", "网络", "环境", "端点", "WebApi", "Socket" })
            };
        }

        /// <summary>
        /// 打开 Bridge 项目设置页。
        /// </summary>
        public static void OpenSettings()
        {
            SettingsService.OpenProjectSettings(SettingsPath);
        }

        /// <summary>
        /// 查找现有配置；若不存在则自动创建默认配置资源。
        /// </summary>
        /// <returns>网络配置资源；创建失败时返回 <c>null</c>。</returns>
        public static BridgeConfigAsset GetOrCreateConfigAsset()
        {
            var config = FindBridgeConfigAsset();
            if (config != null)
            {
                return config;
            }

            EnsureFolderExists("Assets/Resources");
            EnsureFolderExists(DefaultConfigDirectory);

            config = ScriptableObject.CreateInstance<BridgeConfigAsset>();
            AssetDatabase.CreateAsset(config, DefaultConfigAssetPath);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            return AssetDatabase.LoadAssetAtPath<BridgeConfigAsset>(DefaultConfigAssetPath);
        }

        /// <summary>
        /// 构建 Project Settings 主界面。
        /// </summary>
        private static void BuildUi(VisualElement rootElement)
        {
            rootElement.Clear();

            var visualTree = UxmlAssetUtility.LoadVisualTreeAsset(UxmlPath);
            visualTree.CloneTree(rootElement);

            var styleSheet = AssetDatabase.LoadAssetAtPath<StyleSheet>(UssPath);
            if (styleSheet != null)
            {
                rootElement.styleSheets.Add(styleSheet);
            }

            var netConfig = GetOrCreateConfigAsset();
            if (netConfig == null)
            {
                BuildMissingAssetUi(rootElement);
                return;
            }

            var configSerializedObject = new SerializedObject(netConfig);
            BindToolbarSection(rootElement, netConfig, configSerializedObject);
            BindEnvironmentCardSection(rootElement, netConfig, configSerializedObject);
        }

        /// <summary>
        /// 在配置创建失败时显示提示信息。
        /// </summary>
        private static void BuildMissingAssetUi(VisualElement rootElement)
        {
            var banner = rootElement.Q<VisualElement>("MissingAssetMessage");
            if (banner != null)
            {
                banner.style.display = DisplayStyle.Flex;
            }

            var warning = rootElement.Q<TextElement>("MissingAssetText");
            if (warning != null)
            {
                warning.text = "未能创建或加载 BridgeConfig 配置资源，请检查 Assets/Resources/Config 目录的写入状态。";
            }

            var content = rootElement.Q<VisualElement>("ContentRoot");
            if (content != null)
            {
                content.style.display = DisplayStyle.None;
            }
        }

        /// <summary>
        /// 绑定顶部环境工具栏。
        /// </summary>
        private static void BindToolbarSection(VisualElement rootElement, BridgeConfigAsset config,
            SerializedObject serializedObject)
        {
            var currentEnvironmentContainer = rootElement.Q<VisualElement>("CurrentEnvironmentContainer");
            currentEnvironmentContainer.Clear();

            var environments = config.EnvironmentKeys.ToList();
            if (environments.Count == 0)
            {
                environments.Add(config.EnvironmentKey);
            }

            int selectedIndex = Mathf.Max(0, environments.FindIndex(item => item == config.EnvironmentKey));
            var popupField = new PopupField<string>("当前环境", environments, selectedIndex);
            popupField.AddToClassList("stretch-field");
            popupField.RegisterValueChangedCallback(evt =>
            {
                serializedObject.Update();
                serializedObject.FindProperty("environmentKey").stringValue = evt.newValue;
                Save(serializedObject);
                BuildUi(rootElement);
            });
            currentEnvironmentContainer.Add(popupField);

            var environmentNameField = rootElement.Q<TextField>("NewEnvironmentField");
            var addEnvironmentButton = rootElement.Q<Button>("AddEnvironmentButton");
            addEnvironmentButton.clicked += () =>
            {
                string environmentKey = environmentNameField.value?.Trim();
                if (!config.AddEnvironment(environmentKey))
                {
                    return;
                }

                SaveAsset(config);
                environmentNameField.value = string.Empty;
                BuildUi(rootElement);
            };
        }

        /// <summary>
        /// 绑定环境卡片区域。
        /// </summary>
        private static void BindEnvironmentCardSection(VisualElement rootElement, BridgeConfigAsset config,
            SerializedObject serializedObject)
        {
            var cardContainer = rootElement.Q<VisualElement>("EnvironmentCardContainer");
            cardContainer.Clear();

            if (config.EnvironmentKeys.Count == 0)
            {
                var emptyMessage = new TextElement { text = "当前还没有环境，请先新增环境。" };
                emptyMessage.AddToClassList("warning-banner-text");
                cardContainer.Add(emptyMessage);
                return;
            }

            foreach (string environmentKey in config.EnvironmentKeys)
            {
                cardContainer.Add(CreateEnvironmentCard(rootElement, config, serializedObject, environmentKey));
            }
        }

        /// <summary>
        /// 创建单个环境卡片。
        /// </summary>
        private static VisualElement CreateEnvironmentCard(
            VisualElement rootElement,
            BridgeConfigAsset config,
            SerializedObject serializedObject,
            string environmentKey)
        {
            var card = new VisualElement();
            card.AddToClassList("environment-card");

            var header = new VisualElement();
            header.AddToClassList("environment-card-header");

            var titleGroup = new VisualElement();
            titleGroup.AddToClassList("environment-title-group");

            var title = new Label(environmentKey);
            title.AddToClassList("environment-title");

            var subtitle = new TextElement { text = "同一个环境下可以维护多组端点键值对，例如 default、webapi、socket。" };
            subtitle.AddToClassList("environment-subtitle");

            titleGroup.Add(title);
            titleGroup.Add(subtitle);

            var actionGroup = new VisualElement();
            actionGroup.AddToClassList("environment-action-group");

            var addEndpointButton = new Button(() =>
            {
                config.AddEndpoint(environmentKey);
                SaveAsset(config);
                BuildUi(rootElement);
            })
            {
                text = "新增链接"
            };
            addEndpointButton.AddToClassList("primary-button");

            var removeEnvironmentButton = new Button(() =>
            {
                config.RemoveEnvironment(environmentKey);
                SaveAsset(config);
                BuildUi(rootElement);
            })
            {
                text = "删除环境"
            };
            removeEnvironmentButton.AddToClassList("secondary-button");

            bool isReservedEnvironment = string.Equals(environmentKey, BridgeConfigAsset.ReservedEnvironmentKey,
                StringComparison.OrdinalIgnoreCase);
            if (isReservedEnvironment)
            {
                removeEnvironmentButton.text = "保留环境";
                removeEnvironmentButton.tooltip = "测试环境作为保底环境会始终保留，不能删除。";
                removeEnvironmentButton.SetEnabled(false);
            }

            actionGroup.Add(addEndpointButton);
            actionGroup.Add(removeEnvironmentButton);

            header.Add(titleGroup);
            header.Add(actionGroup);
            card.Add(header);

            var entryIndexes = GetEntryIndexesByEnvironment(serializedObject, environmentKey);
            if (entryIndexes.Count == 0)
            {
                var emptyMessage = new TextElement { text = "这个环境下还没有链接配置。" };
                emptyMessage.AddToClassList("info-banner-text");
                card.Add(emptyMessage);
                return card;
            }

            foreach (int entryIndex in entryIndexes)
            {
                card.Add(CreateEndpointRow(rootElement, serializedObject, entryIndex));
            }

            return card;
        }

        /// <summary>
        /// 创建单条端点编辑行。
        /// </summary>
        private static VisualElement CreateEndpointRow(VisualElement rootElement, SerializedObject serializedObject,
            int index)
        {
            var endpointEntriesProperty = serializedObject.FindProperty("endpointEntries");
            var entryProperty = endpointEntriesProperty.GetArrayElementAtIndex(index);

            var row = new VisualElement();
            row.AddToClassList("endpoint-row");

            var endpointKeyField = new TextField("键")
            {
                value = entryProperty.FindPropertyRelative("endpointKey").stringValue
            };
            endpointKeyField.AddToClassList("endpoint-field");
            endpointKeyField.RegisterValueChangedCallback(evt =>
            {
                serializedObject.Update();
                var target = serializedObject.FindProperty("endpointEntries").GetArrayElementAtIndex(index);
                target.FindPropertyRelative("endpointKey").stringValue =
                    string.IsNullOrWhiteSpace(evt.newValue) ? "default" : evt.newValue.Trim();
                Save(serializedObject);
            });

            var endpointUrlField = new TextField("地址")
            {
                value = entryProperty.FindPropertyRelative("url").stringValue
            };
            endpointUrlField.AddToClassList("endpoint-field");
            endpointUrlField.RegisterValueChangedCallback(evt =>
            {
                serializedObject.Update();
                var target = serializedObject.FindProperty("endpointEntries").GetArrayElementAtIndex(index);
                target.FindPropertyRelative("url").stringValue = evt.newValue?.Trim() ?? string.Empty;
                Save(serializedObject);
            });

            var removeEndpointButton = new Button(() =>
            {
                serializedObject.Update();
                serializedObject.FindProperty("endpointEntries").DeleteArrayElementAtIndex(index);
                Save(serializedObject);
                BuildUi(rootElement);
            })
            {
                text = "删除链接"
            };
            removeEndpointButton.AddToClassList("danger-button");

            row.Add(endpointKeyField);
            row.Add(endpointUrlField);
            row.Add(removeEndpointButton);
            return row;
        }

        /// <summary>
        /// 获取指定环境下的全部端点索引。
        /// </summary>
        private static List<int> GetEntryIndexesByEnvironment(SerializedObject serializedObject, string environmentKey)
        {
            var result = new List<int>();
            var endpointEntriesProperty = serializedObject.FindProperty("endpointEntries");
            for (int index = 0; index < endpointEntriesProperty.arraySize; index++)
            {
                var entryProperty = endpointEntriesProperty.GetArrayElementAtIndex(index);
                string value = entryProperty.FindPropertyRelative("environmentKey").stringValue;
                if (string.Equals(value, environmentKey, StringComparison.OrdinalIgnoreCase))
                {
                    result.Add(index);
                }
            }

            return result;
        }

        /// <summary>
        /// 全项目搜索现有配置资源。
        /// </summary>
        private static BridgeConfigAsset FindBridgeConfigAsset()
        {
            string[] guids = AssetDatabase.FindAssets($"t:{nameof(BridgeConfigAsset)}");
            if (guids == null || guids.Length == 0)
            {
                return null;
            }

            string assetPath = AssetDatabase.GUIDToAssetPath(guids[0]);
            return AssetDatabase.LoadAssetAtPath<BridgeConfigAsset>(assetPath);
        }

        /// <summary>
        /// 确保目录存在，不存在时自动创建。
        /// </summary>
        private static void EnsureFolderExists(string folderPath)
        {
            if (AssetDatabase.IsValidFolder(folderPath))
            {
                return;
            }

            string parentPath = Path.GetDirectoryName(folderPath)?.Replace("\\", "/");
            string folderName = Path.GetFileName(folderPath);
            if (!string.IsNullOrWhiteSpace(parentPath) && !AssetDatabase.IsValidFolder(parentPath))
            {
                EnsureFolderExists(parentPath);
            }

            AssetDatabase.CreateFolder(parentPath, folderName);
        }

        /// <summary>
        /// 保存序列化对象。
        /// </summary>
        private static void Save(SerializedObject serializedObject)
        {
            serializedObject.ApplyModifiedProperties();
            if (serializedObject.targetObject != null)
            {
                EditorUtility.SetDirty(serializedObject.targetObject);
            }

            AssetDatabase.SaveAssets();
        }

        /// <summary>
        /// 保存单个资源对象。
        /// </summary>
        private static void SaveAsset(UnityEngine.Object target)
        {
            if (target == null)
            {
                return;
            }

            EditorUtility.SetDirty(target);
            AssetDatabase.SaveAssets();
        }
    }
}
