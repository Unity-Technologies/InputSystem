//HintName: MyBindingComposite_Generated.g.cs
using System;
using UnityEngine;
using UnityEngine.InputSystem;

#if UNITY_EDITOR
[UnityEditor.InitializeOnLoad]
#endif
class MyBindingCompositeRegistration 
{
#if UNITY_EDITOR
    static MyBindingCompositeRegistration() { Register(); }
#endif

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSplashScreen)]
    static void Register() => InputSystem.RegisterBindingComposite(typeof(global::Ns.Outer.MyBindingComposite), null);
}
