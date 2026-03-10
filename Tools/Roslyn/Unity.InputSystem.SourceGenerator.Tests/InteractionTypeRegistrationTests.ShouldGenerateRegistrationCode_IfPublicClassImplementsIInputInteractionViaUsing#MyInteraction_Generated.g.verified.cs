//HintName: MyInteraction_Generated.g.cs
using System;
using UnityEngine;
using UnityEngine.InputSystem;

#if UNITY_EDITOR
[UnityEditor.InitializeOnLoad]
#endif
class MyInteractionRegistration 
{
#if UNITY_EDITOR
    static MyInteractionRegistration() { Register(); }
#endif

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSplashScreen)]
    static void Register() => InputSystem.RegisterInteraction(typeof(global::MyInteraction));
}
