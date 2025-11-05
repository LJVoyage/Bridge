using LJVoyage.LJVToolkit.Runtime.Attributes;
using UnityEngine;

namespace LJVoyage.LJVNet.Runtime
{
    /// <summary>
    /// 保存当前加载器的配置文件（单例 ScriptableObject）
    /// </summary>
    //[CreateAssetMenu(fileName = "NetLoaderConfig", menuName = "LJV/Network/Loader Config")]
    public class NetLoaderConfig : ScriptableObject
    {
        [ReadOnly] public string providerTypeName; // 完整类型名
    }
}