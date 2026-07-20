using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;
using VoyageForge.Bridge.Runtime;
using VoyageForge.Depot.Editor.Scripts.Utilities;

namespace VoyageForge.Bridge.Editor
{
    public sealed class BridgeSettingsProvider : SettingsProvider
    {
        private const string SettingsPath = "Project/VoyageForge/Bridge";
        private const string DefaultConfigDirectory = "Assets/Resources/VoyageForge/Config";
        private const string DefaultConfigAssetPath = DefaultConfigDirectory + "/BridgeConfig.asset";
        private const string UxmlPath = "Assets/Bridge/Editor/Scripts/BridgeProjectSettingsView.uxml";

        public BridgeSettingsProvider() : base(SettingsPath, SettingsScope.Project)
        {
            label = "Bridge";
            activateHandler = (_, rootElement) => BuildUi(rootElement);
            keywords = new HashSet<string>(new[] { "Bridge", "网络", "环境", "端点" });
        }

        [SettingsProvider]
        public static SettingsProvider CreateSettingsProvider() => new BridgeSettingsProvider();
        public static void OpenSettings() => SettingsService.OpenProjectSettings(SettingsPath);

        // ============================================================
        // 配置加载 / 保存
        // ============================================================

        public static BridgeConfigAsset GetOrCreateConfigAsset()
        {
            return GetOrCreateConfig() as BridgeConfigAsset ?? GetOrCreateConfigAssetFallback();
        }

        private static IBridgeConfig GetOrCreateConfig()
        {
            string typeName = BridgeSettings.instance.ConfigProviderTypeName;
            if (!string.IsNullOrWhiteSpace(typeName))
            {
                var p = BridgeConfigProviderFactory.CreateProvider(typeName);
                if (p != null) { var c = p.LoadConfig(); if (c != null) return c; }
            }
            return GetOrCreateConfigAssetFallback();
        }

        private static BridgeConfigAsset GetOrCreateConfigAssetFallback()
        {
            var s = BridgeSettings.instance;
            if (s.ConfigAsset != null) return s.ConfigAsset;
            var c = FindBridgeConfigAsset();
            if (c != null) { s.SetConfigAsset(c); return c; }
            EnsureFolderExists("Assets/Resources");
            EnsureFolderExists(DefaultConfigDirectory);
            c = ScriptableObject.CreateInstance<BridgeConfigAsset>();
            AssetDatabase.CreateAsset(c, DefaultConfigAssetPath);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            c = AssetDatabase.LoadAssetAtPath<BridgeConfigAsset>(DefaultConfigAssetPath);
            s.SetConfigAsset(c);
            return c;
        }

        private static void SaveConfig(IBridgeConfig config)
        {
            string typeName = BridgeSettings.instance.ConfigProviderTypeName;
            if (!string.IsNullOrWhiteSpace(typeName))
            {
                var p = BridgeConfigProviderFactory.CreateProvider(typeName);
                if (p != null) { p.SaveConfig(config); return; }
            }
            var a = config as BridgeConfigAsset;
            if (a != null) { EditorUtility.SetDirty(a); AssetDatabase.SaveAssets(); }
        }

        private static bool IsReserved(string k) =>
            string.Equals(k, "dev", StringComparison.OrdinalIgnoreCase);

        // ============================================================
        // UI
        // ============================================================

        private static void BuildUi(VisualElement rootElement)
        {
            rootElement.Clear();

            var vt = UxmlAssetUtility.LoadVisualTreeAsset(UxmlPath);
            if (vt == null) { rootElement.Add(new Label("UXML 加载失败")); return; }
            vt.CloneTree(rootElement);

            var config = GetOrCreateConfig();
            if (config == null) { rootElement.Add(new Label("配置加载失败")); return; }

            BuildProviderPopup(rootElement);
            BuildEnvToolbar(rootElement, config);

            var cc = rootElement.Q<VisualElement>("EnvironmentCardContainer");
            cc?.Clear();
            foreach (string envKey in config.EnvironmentKeys)
                cc?.Add(BuildEnvCard(config, envKey, rootElement));
        }

        // ---- 提供器下拉 ----

        private static void BuildProviderPopup(VisualElement rootElement)
        {
            var cr = rootElement.Q<VisualElement>("ContentRoot");
            if (cr == null) return;

            var settings = BridgeSettings.instance;
            var types = TypeCache.GetTypesDerivedFrom<IBridgeConfigProvider>()
                .Where(t => !t.IsAbstract && !t.IsInterface).OrderBy(t => t.FullName).ToList();

            var section = new VisualElement();
            section.AddToClassList("section-card");
            section.Add(new Label("配置提供器") { name = "section-title" });

            var choices = new List<string> { "(未选择 — ScriptableObject)" };
            choices.AddRange(types.Select(t => t.FullName));

            string cur = settings.ConfigProviderTypeName;
            int sel = 0;
            if (!string.IsNullOrWhiteSpace(cur))
            { int f = choices.FindIndex(x => string.Equals(x, cur, StringComparison.Ordinal)); if (f >= 0) sel = f; }

            var dd = new PopupField<string>("提供器类型", choices, sel);
            dd.AddToClassList("stretch-field");
            section.Add(dd);

            var hint = new TextElement();
            hint.AddToClassList("environment-subtitle");
            section.Add(hint);
            void Rf() { hint.text = dd.index == 0 ? "数据源: BridgeConfigAsset (ScriptableObject)" : $"数据源: {choices[dd.index]}"; }
            Rf();

            dd.RegisterValueChangedCallback(evt =>
            {
                settings.SetConfigProviderType(dd.index > 0 ? evt.newValue : null);
                Rf();
                BuildUi(rootElement);
            });

            cr.Insert(0, section);
        }

        // ---- 环境工具栏 ----

        private static void BuildEnvToolbar(VisualElement rootElement, IBridgeConfig config)
        {
            var container = rootElement.Q<VisualElement>("CurrentEnvironmentContainer");
            if (container == null) return;
            container.Clear();

            var envs = config.EnvironmentKeys.ToList();
            if (envs.Count == 0) envs.Add(config.EnvironmentKey);

            int idx = Mathf.Max(0, envs.FindIndex(e => e == config.EnvironmentKey));
            var popup = new PopupField<string>("当前环境", envs, idx);
            popup.AddToClassList("stretch-field");
            popup.RegisterValueChangedCallback(evt =>
            {
                config.SetEnvironment(evt.newValue);
                SaveConfig(config);
                BuildUi(rootElement);
            });
            container.Add(popup);

            var input = rootElement.Q<TextField>("NewEnvironmentField");
            var btn = rootElement.Q<Button>("AddEnvironmentButton");
            if (btn != null)
            {
                btn.clicked += () =>
                {
                    string key = input?.value?.Trim();
                    if (string.IsNullOrWhiteSpace(key)) return;
                    if (!config.EnvironmentKeys.Any(e => string.Equals(e, key, StringComparison.OrdinalIgnoreCase)))
                    {
                        config.EnvironmentKeys.Add(key);
                        if (string.IsNullOrWhiteSpace(config.EnvironmentKey))
                            config.EnvironmentKey = key;
                        SaveConfig(config);
                    }
                    if (input != null) input.value = string.Empty;
                    BuildUi(rootElement);
                };
            }
        }

        // ---- 环境卡片 ----

        private static VisualElement BuildEnvCard(IBridgeConfig config, string envKey, VisualElement rootElement)
        {
            var card = new VisualElement();
            card.AddToClassList("environment-card");

            var header = new VisualElement();
            header.AddToClassList("environment-card-header");
            var tg = new VisualElement();
            tg.AddToClassList("environment-title-group");
            tg.Add(new Label(envKey));
            header.Add(tg);

            var actions = new VisualElement();
            actions.AddToClassList("environment-action-group");

            var addEp = new Button(() =>
            {
                config.Endpoints.Add(new EndpointConfig
                {
                    EnvironmentKey = envKey,
                    EndpointKey = "default",
                    Url = string.Empty
                });
                SaveConfig(config);
                BuildUi(rootElement);
            })
            { text = "新增链接" };
            addEp.AddToClassList("primary-button");
            actions.Add(addEp);

            bool reserved = IsReserved(envKey);
            var delEnv = new Button(() =>
            {
                if (!reserved)
                {
                    config.EnvironmentKeys.Remove(envKey);
                    config.Endpoints.RemoveAll(e => e != null &&
                        string.Equals(e.EnvironmentKey, envKey, StringComparison.OrdinalIgnoreCase));
                    if (string.Equals(config.EnvironmentKey, envKey, StringComparison.OrdinalIgnoreCase))
                        config.SetEnvironment();
                    SaveConfig(config);
                }
                BuildUi(rootElement);
            })
            { text = reserved ? "保留环境" : "删除环境" };
            delEnv.AddToClassList("secondary-button");
            if (reserved) { delEnv.tooltip = "dev 环境始终保留。"; delEnv.SetEnabled(false); }
            actions.Add(delEnv);

            header.Add(actions);
            card.Add(header);

            var eps = config.Endpoints
                .Where(e => e != null && string.Equals(e.EnvironmentKey, envKey, StringComparison.OrdinalIgnoreCase))
                .ToList();
            if (eps.Count == 0)
            {
                var empty = new TextElement { text = "暂无链接配置。" };
                empty.AddToClassList("info-banner-text");
                card.Add(empty);
            }
            else
            {
                int globalBase = config.Endpoints.FindIndex(e =>
                    e != null && string.Equals(e.EnvironmentKey, envKey, StringComparison.OrdinalIgnoreCase));
                for (int i = 0; i < eps.Count; i++)
                    card.Add(BuildEpRow(config, envKey, i, eps[i], rootElement));
            }

            return card;
        }

        // ---- 端点行 ----

        private static VisualElement BuildEpRow(IBridgeConfig config, string envKey,
            int localIdx, EndpointConfig entry, VisualElement rootElement)
        {
            var row = new VisualElement();
            row.AddToClassList("endpoint-row");

            var kf = new TextField("键") { value = entry.EndpointKey ?? "default" };
            kf.AddToClassList("endpoint-field");
            kf.RegisterValueChangedCallback(evt =>
            {
                entry.EndpointKey = string.IsNullOrWhiteSpace(evt.newValue) ? "default" : evt.newValue.Trim();
                SaveConfig(config);
            });
            row.Add(kf);

            var uf = new TextField("地址") { value = entry.Url ?? "" };
            uf.AddToClassList("endpoint-field");
            uf.RegisterValueChangedCallback(evt =>
            {
                entry.Url = evt.newValue?.Trim() ?? "";
                SaveConfig(config);
            });
            row.Add(uf);

            var del = new Button(() =>
            {
                // 通过 global index 删除，因为 localIdx 可能在编辑期间失效
                int globalIdx = -1;
                int n = 0;
                for (int i = 0; i < config.Endpoints.Count; i++)
                {
                    var e = config.Endpoints[i];
                    if (e != null && string.Equals(e.EnvironmentKey, envKey, StringComparison.OrdinalIgnoreCase))
                    {
                        if (n == localIdx) { globalIdx = i; break; }
                        n++;
                    }
                }
                if (globalIdx >= 0) config.Endpoints.RemoveAt(globalIdx);
                SaveConfig(config);
                BuildUi(rootElement);
            })
            { text = "删除链接" };
            del.AddToClassList("danger-button");
            row.Add(del);

            return row;
        }

        // ============================================================
        // 辅助
        // ============================================================

        private static BridgeConfigAsset FindBridgeConfigAsset()
        {
            string[] guids = AssetDatabase.FindAssets($"t:{nameof(BridgeConfigAsset)}");
            if (guids == null || guids.Length == 0) return null;
            return AssetDatabase.LoadAssetAtPath<BridgeConfigAsset>(AssetDatabase.GUIDToAssetPath(guids[0]));
        }

        private static void EnsureFolderExists(string folderPath)
        {
            if (AssetDatabase.IsValidFolder(folderPath)) return;
            string p = Path.GetDirectoryName(folderPath)?.Replace("\\", "/");
            string n = Path.GetFileName(folderPath);
            if (!string.IsNullOrWhiteSpace(p) && !AssetDatabase.IsValidFolder(p)) EnsureFolderExists(p);
            AssetDatabase.CreateFolder(p, n);
        }
    }
}
