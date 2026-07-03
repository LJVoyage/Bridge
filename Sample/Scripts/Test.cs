using System;
using UnityEngine;

namespace VoyageForge.Bridge.Sample
{
    public class Test : MonoBehaviour
    {
        private void Awake()
        {
            UnityEngine.Debug.Log("Awake");
        }

        private void Start()
        {
            WebClient.Get<MyDto>("api/user");
            
            UnityEngine.Debug.Log("Start");
        }
    }
    
    public class MyDto
    {
        public int Id { get; set; }
        public string Name { get; set; }
    }
}