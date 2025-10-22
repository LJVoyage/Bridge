using System.Collections;
using LJVoyage.LJVToolkit.Runtime.Utilities;
using UnityEngine;

namespace LJVoyage.LJVNet.Runtime
{
    public class NetProxy : MonoSingleton<NetProxy>
    {
        /// <summary>
        /// 启动一个网络请求协程
        /// </summary>
        public void RunCoroutine(IEnumerator routine)
        {
            StartCoroutine(routine);
        }

        /// <summary>
        /// 取消协程
        /// </summary>
        public void StopCoroutineSafe(Coroutine c)
        {
            if (c != null)
                StopCoroutine(c);
        }
    }
}