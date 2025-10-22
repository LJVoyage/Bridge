using System.Linq;
using LJVoyage.LJVNet.Runtime;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace LJVoyage.Network.Editor
{
    public class NetConfigEditor : EditorWindow
    {
        private NetConfig _config;

        [SerializeField] private VisualTreeAsset _visualTree;
        [SerializeField] private StyleSheet _styleSheet;


        [MenuItem("LJV/Network/Net Config")]
        public static void ShowWindow()
        {
            var wnd = GetWindow<NetConfigEditor>();
            wnd.titleContent = new GUIContent("LJV Net Config");
        }

        private void OnEnable()
        {
            VisualElement root = _visualTree.Instantiate();

            rootVisualElement.Add(root);

            root.styleSheets.Add(_styleSheet);

            // 在项目中查找所有匹配类型的资源
            string[] guids = AssetDatabase.FindAssets($"t:{nameof(NetConfig)}");

            if (guids == null || guids.Length == 0)
            {
                Debug.LogWarning($"未找到 {nameof(NetConfig)} 配置文件。");

                return;
            }

            // 默认取第一个结果（也可以在编辑器里显示列表让用户选择）
            string path = AssetDatabase.GUIDToAssetPath(guids.First());

            _config = AssetDatabase.LoadAssetAtPath<NetConfig>(path);

            if (_config == null)
            {
                Debug.LogError("NetConfig not found in Resources folder.");
                return;
            }

            // 绑定 UI
            BindUI(root);
        }

        private void BindUI(VisualElement root)
        {
            root.Q<EnumField>("envField").Init(_config.currentEnvironment);
            root.Q<EnumField>("envField").RegisterValueChangedCallback(evt =>
            {
                _config.currentEnvironment = (NetEnvironment)evt.newValue;
            });

            root.Q<Toggle>("useABToggle").value = _config.useAssetBundle;
            root.Q<Toggle>("useABToggle")
                .RegisterValueChangedCallback(evt => { _config.useAssetBundle = evt.newValue; });

            root.Q<TextField>("devBaseUrl").value = _config.devBaseUrl;

            root.Q<TextField>("testBaseUrl").value = _config.testBaseUrl;

            root.Q<TextField>("prodBaseUrl").value = _config.prodBaseUrl;

            root.Q<IntegerField>("timeoutField").value = _config.timeoutSeconds;
            root.Q<TextField>("abPath").value = _config.abPath;
            root.Q<TextField>("abAssetName").value = _config.abAssetName;

            // 保存按钮
            root.Q<Button>("saveButton").clicked += () =>
            {
                SaveChanges(root);
                EditorUtility.SetDirty(_config);
                AssetDatabase.SaveAssets();
                Debug.Log("✅ NetConfig saved successfully!");
            };
        }

        private void SaveChanges(VisualElement root)
        {
            _config.devBaseUrl = root.Q<TextField>("devBaseUrl").value;
            _config.testBaseUrl = root.Q<TextField>("testBaseUrl").value;
            _config.prodBaseUrl = root.Q<TextField>("prodBaseUrl").value;

            _config.timeoutSeconds = root.Q<IntegerField>("timeoutField").value;
            _config.abPath = root.Q<TextField>("abPath").value;
            _config.abAssetName = root.Q<TextField>("abAssetName").value;
        }
    }
}