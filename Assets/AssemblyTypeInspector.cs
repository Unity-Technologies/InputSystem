#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;

public static class AssemblyTypeInspector
{
    [MenuItem("Tools/Save Assembly Types to File")]
    private static void SaveAssemblyTypesToFile()
    {
        const string assemblyName = "Unity.InputSystem";

        var assembly = AppDomain.CurrentDomain
            .GetAssemblies()
            .FirstOrDefault(a => a.GetName().Name == assemblyName);

        if (assembly == null)
        {
            Debug.LogError($"Assembly '{assemblyName}' not found.");
            return;
        }

        var types = assembly.GetTypes()
            .OrderBy(t => t.FullName)
            .Select(t => t.FullName)
            .ToArray();

        string path = "Assets/Editor/UnityInputSystemTypes.txt";
        File.WriteAllLines(path, types);

        Debug.Log($"Saved {types.Length} types to {path}");
        AssetDatabase.Refresh();
    }
}
#endif