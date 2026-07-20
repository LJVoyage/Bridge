using System;
using UnityEngine;

namespace VoyageForge.Bridge.Runtime
{
    /// <summary>
    /// Bridge 配置提供器工厂。
    /// 通过反射根据类型名创建 IBridgeConfigProvider 实例。
    /// </summary>
    public static class BridgeConfigProviderFactory
    {
        /// <summary>
        /// 根据类型名反射创建提供器实例。
        /// 使用无参构造，路径由提供器自身默认值决定。
        /// </summary>
        /// <param name="typeName">程序集限定类型名。</param>
        public static IBridgeConfigProvider CreateProvider(string typeName)
        {
            if (string.IsNullOrWhiteSpace(typeName))
            {
                return null;
            }

            Type type = Type.GetType(typeName);
            if (type == null)
            {
                foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
                {
                    type = assembly.GetType(typeName);
                    if (type != null) break;
                }
            }

            if (type == null)
            {
                Debug.LogError($"[Bridge] 无法找到提供器类型: {typeName}");
                return null;
            }

            if (!typeof(IBridgeConfigProvider).IsAssignableFrom(type))
            {
                Debug.LogError($"[Bridge] 类型 {typeName} 未实现 IBridgeConfigProvider。");
                return null;
            }

            try
            {
                return (IBridgeConfigProvider)Activator.CreateInstance(type);
            }
            catch (Exception ex)
            {
                Debug.LogError($"[Bridge] 创建提供器实例失败 ({typeName}): {ex.Message}");
                return null;
            }
        }
    }
}
