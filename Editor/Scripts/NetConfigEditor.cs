using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using LJVoyage.LJVNet.Runtime;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace LJVoyage.Network.Editor
{
    public class NetConfigEditor : EditorWindow
    {
        private static NetLoaderConfig _config;

        public static NetLoaderConfig Config
        {
            get
            {
                if (_config == null)
                {
                    _config = Resources.Load<NetLoaderConfig>(LJVNetClient.LOADER_CONFIG_PATH);
                }

                return _config;
            }
        }

        /// <summary>
        /// 查找所有实现了 INetConfigProvider 接口的类型。
        /// </summary>
        public static List<Type> FindAllNetConfigProviders()
        {
            var result = new List<Type>();

            // 获取当前所有已加载的程序集
            var assemblies = AppDomain.CurrentDomain.GetAssemblies();

            foreach (var assembly in assemblies)
            {
                try
                {
                    var types = assembly.GetTypes()
                        .Where(t =>
                                typeof(INetConfigProvider).IsAssignableFrom(t) && // 继承接口
                                !t.IsInterface && // 排除接口本身
                                !t.IsAbstract && // 排除抽象类
                                t.GetConstructor(Type.EmptyTypes) != null // 必须有无参构造函数
                        );

                    result.AddRange(types);
                }
                catch (ReflectionTypeLoadException e)
                {
                    // 某些程序集可能会抛异常（例如动态生成的程序集）
                    result.AddRange(e.Types.Where(t =>
                        t != null &&
                        typeof(INetConfigProvider).IsAssignableFrom(t) &&
                        !t.IsInterface && !t.IsAbstract &&
                        t.GetConstructor(Type.EmptyTypes) != null
                    ));
                }
            }

            return result.Distinct().ToList();
        }

        private static INetConfigProvider _provider;

        private static INetConfigProvider Provider
        {
            get
            {
                if (_provider == null)
                {
                    Type type = null;

                    foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
                    {
                        type = assembly.GetType(Config.providerTypeName);
                        if (type != null)
                            break;
                    }

                    if (type == null)
                        throw new Exception($"未找到类型 {Config.providerTypeName}");

                    _provider = (INetConfigProvider)Activator.CreateInstance(type);
                }

                return _provider;
            }
        }


        /// <summary>
        /// 菜单根路径
        /// </summary>
        private const string MENU_ROOT = "LJV/Network/Net Environment/";

        [SerializeField] private VisualTreeAsset _visualTree;

        [SerializeField] private StyleSheet _styleSheet;


        [MenuItem("LJV/Network/Net Config", false, 100)]
        public static void ShowWindow()
        {
            var wnd = GetWindow<NetConfigEditor>();

            wnd.titleContent = new GUIContent("LJV Net Config");
        }


        [MenuItem(MENU_ROOT + "Development")]
        private static void SetDevelopment() => SetEnvironment(NetEnvironment.Development);

        [MenuItem(MENU_ROOT + "Testing")]
        private static void SetTesting() => SetEnvironment(NetEnvironment.Testing);

        [MenuItem(MENU_ROOT + "Production")]
        private static void SetProduction() => SetEnvironment(NetEnvironment.Production);
        
        private static bool ValidateLoaderConfig()
        {
            var loaderConfig = Config;
            if (loaderConfig == null)
                return false;

            // 检查加载器配置是否存在指定的提供程序类型
            if (string.IsNullOrEmpty(loaderConfig.providerTypeName))
                return false;

            return true;
        }

        private static void SetEnvironment(NetEnvironment environment)
        {
            // 更新配置文件中的环境设置
            string[] guids = AssetDatabase.FindAssets($"t:{nameof(NetConfig)}");
            if (guids != null && guids.Length > 0)
            {
                string path = AssetDatabase.GUIDToAssetPath(guids.First());
                var config = AssetDatabase.LoadAssetAtPath<NetConfig>(path);
                if (config != null)
                {
                    config.NetEnvironment = environment;
                    EditorUtility.SetDirty(config);
                    AssetDatabase.SaveAssets();
                }
            }

            // 更新菜单勾选状态
            UpdateMenuValidation();

            Debug.Log($"Net Environment set to: {environment}");
        }


        [MenuItem(MENU_ROOT + "Development", true)]
        private static bool ValidateDevelopment()
        {
            // 检查加载器配置是否有效
            if (!ValidateLoaderConfig())
                return false;

            var currentEnv = Provider.GetEnvironment();
            Menu.SetChecked(MENU_ROOT + "Development", currentEnv == NetEnvironment.Development);
            return true;
        }

        [MenuItem(MENU_ROOT + "Testing", true)]
        private static bool ValidateTesting()
        {
            // 检查加载器配置是否有效
            if (!ValidateLoaderConfig())
                return false;

            var currentEnv = Provider.GetEnvironment();
            Menu.SetChecked(MENU_ROOT + "Testing", currentEnv == NetEnvironment.Testing);
            return true;
        }

        [MenuItem(MENU_ROOT + "Production", true)]
        private static bool ValidateProduction()
        {
            // 检查加载器配置是否有效
            if (!ValidateLoaderConfig())
                return false;

            var currentEnv = Provider.GetEnvironment();
            Menu.SetChecked(MENU_ROOT + "Production", currentEnv == NetEnvironment.Production);
            return true;
        }


        private static void UpdateMenuValidation()
        {
            // 检查加载器配置是否有效
            if (!ValidateLoaderConfig())
                return;

            // 强制刷新菜单验证状态
            Menu.SetChecked(MENU_ROOT + "Development", false);
            Menu.SetChecked(MENU_ROOT + "Testing", false);
            Menu.SetChecked(MENU_ROOT + "Production", false);

            var currentEnv = Provider.GetEnvironment();

            Menu.SetChecked(MENU_ROOT + "Development", currentEnv == NetEnvironment.Development);
            Menu.SetChecked(MENU_ROOT + "Testing", currentEnv == NetEnvironment.Testing);
            Menu.SetChecked(MENU_ROOT + "Production", currentEnv == NetEnvironment.Production);
        }

        [InitializeOnLoadMethod]
        private static void InitializeMenu()
        {
            // 在编辑器加载时初始化菜单状态
            EditorApplication.delayCall += UpdateMenuValidation;
        }

        private void OnEnable()
        {
            _ = Config;

            // 创建 UI
            CreateUI();
        }

        private void CreateUI()
        {
            if (_visualTree == null)
            {
                Debug.LogError("VisualTreeAsset is not assigned.");
                return;
            }

            // 清空现有的 UI
            rootVisualElement.Clear();

            // 实例化 UI
            VisualElement root = _visualTree.Instantiate();

            rootVisualElement.Add(root);

            if (_styleSheet != null)
            {
                root.styleSheets.Add(_styleSheet);
            }

            // 绑定 UI
            BindUI(root);
        }

        private void BindUI(VisualElement root)
        {
            // 加载器下拉列表
            var loaderField = root.Q<DropdownField>("Loader");

            if (loaderField != null)
            {
                var list = FindAllNetConfigProviders().Select(t => t.FullName).ToList();

                loaderField.choices = list;

                var index = list.FindIndex((t) => t == Config.providerTypeName);

                loaderField.index = index >= 0 ? index : 0;

                loaderField.RegisterValueChangedCallback((evt) =>
                {
                    _config.providerTypeName = evt.newValue.ToString();

                    EditorUtility.SetDirty(_config);

                    AssetDatabase.SaveAssets();
                });
            }

            // 保存按钮
            var saveButton = root.Q<Button>("saveButton");

            if (saveButton != null)
            {
                saveButton.clicked += () =>
                {
                    EditorUtility.SetDirty(_config);
                    AssetDatabase.SaveAssets();

                    // 更新菜单状态
                    UpdateMenuValidation();
                };
            }
        }
    }
}