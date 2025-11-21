//HintName: MyProcessor_Generated.g.cs
using System;
using UnityEngine;
using UnityEngine.InputSystem;

#if UNITY_EDITOR
[UnityEditor.InitializeOnLoad]
#endif
class MyProcessorRegistration 
{
#if UNITY_EDITOR
    static MyProcessorRegistration() { Register(); }
#endif

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSplashScreen)]
    static void Register() => InputSystem.RegisterProcessor(typeof(global::Ns.Outer.MyProcessor));
}
