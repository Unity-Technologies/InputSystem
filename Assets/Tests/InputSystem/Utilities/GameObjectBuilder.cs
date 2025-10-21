using System;
using UnityEngine;

internal static class GameObjectBuilder
{
    public static (GameObject, T1) MakeGameObject<T1>(
        string name = "",
        Action<T1> init = null)
        where T1 : Component
    {
        var go = new GameObject(name);
        var c1 = go.AddComponent<T1>();
        init?.Invoke(c1);
        return (go, c1);
    }

    public static (GameObject, T1, T2) MakeGameObject<T1, T2>(
        string name = "",
        Action<T1, T2> init = null)
        where T1 : Component
        where T2 : Component
    {
        var go = new GameObject(name);
        var c1 = go.AddComponent<T1>();
        var c2 = go.AddComponent<T2>();
        init?.Invoke(c1, c2);
        return (go, c1, c2);
    }

    public static (GameObject, T1, T2, T3) MakeGameObject<T1, T2, T3>(string name = "",
        Action<T1, T2, T3> init = null)
        where T1 : Component
        where T2 : Component
        where T3 : Component
    {
        var go = new GameObject(name);
        var c1 = go.AddComponent<T1>();
        var c2 = go.AddComponent<T2>();
        var c3 = go.AddComponent<T3>();
        init?.Invoke(c1, c2, c3);
        return (go, c1, c2, c3);
    }

    public static (GameObject, T1, T2, T3, T4) MakeGameObject<T1, T2, T3, T4>(string name = "",
        Action<T1, T2, T3, T4> init = null)
        where T1 : Component
        where T2 : Component
        where T3 : Component
        where T4 : Component
    {
        var go = new GameObject(name);
        var c1 = go.AddComponent<T1>();
        var c2 = go.AddComponent<T2>();
        var c3 = go.AddComponent<T3>();
        var c4 = go.AddComponent<T4>();
        init?.Invoke(c1, c2, c3, c4);
        return (go, c1, c2, c3, c4);
    }

    public static (GameObject, T1, T2, T3, T4, T5) MakeGameObject<T1, T2, T3, T4, T5>(string name = "",
        Action<T1, T2, T3, T4> init = null)
        where T1 : Component
        where T2 : Component
        where T3 : Component
        where T4 : Component
        where T5 : Component
    {
        var go = new GameObject(name);
        var c1 = go.AddComponent<T1>();
        var c2 = go.AddComponent<T2>();
        var c3 = go.AddComponent<T3>();
        var c4 = go.AddComponent<T4>();
        var c5 = go.AddComponent<T5>();

        init?.Invoke(c1, c2, c3, c4);

        return (go, c1, c2, c3, c4, c5);
    }
}
