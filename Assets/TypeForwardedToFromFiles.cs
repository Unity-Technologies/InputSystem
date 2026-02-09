#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

public static class TypeForwardedToFromFiles
{
    [MenuItem("Tools/Generate TypeForwardedTo From TXT Files")]
    private static void GenerateTypeForwardedTo()
    {
        // 1️⃣ Path to your old Runtime types TXT
        string oldRuntimeFile = "Assets/Editor/OldTypes.txt";

        // 2️⃣ Path to your new Runtime types TXT
        string newRuntimeFile = "Assets/Editor/NewTypes.txt";

        if (!File.Exists(oldRuntimeFile) || !File.Exists(newRuntimeFile))
        {
            Debug.LogError("One or both TXT files do not exist. Check the paths.");
            return;
        }

        // 3️⃣ Read all lines from both files
        HashSet<string> oldRuntimeTypes = new HashSet<string>(
            File.ReadAllLines(oldRuntimeFile)
                .Select(s => s.Trim())
                .Where(s => !string.IsNullOrEmpty(s))
        );

        HashSet<string> newRuntimeTypes = new HashSet<string>(
            File.ReadAllLines(newRuntimeFile)
                .Select(s => s.Trim())
                .Where(s => !string.IsNullOrEmpty(s))
        );

        // 4️⃣ Find types that were in old Runtime but missing in new Runtime
        var movedTypes = oldRuntimeTypes
            .Where(t => !newRuntimeTypes.Contains(t))
            .OrderBy(t => t)
            .ToList();

        Debug.Log($"Old Runtime types: {oldRuntimeTypes.Count}");
        Debug.Log($"New Runtime types: {newRuntimeTypes.Count}");
        Debug.Log($"Types moved to Editor: {movedTypes.Count}");

        if (movedTypes.Count == 0)
        {
            Debug.Log("No types were removed from new Runtime.");
            return;
        }

        // 5️⃣ Generate the .cs file content
        string csFilePath = "Assets/Editor/TypeForwardedTo_MovedTypes.cs";
        using (StreamWriter writer = new StreamWriter(csFilePath))
        {
            writer.WriteLine("// Auto-generated TypeForwardedTo lines");
            writer.WriteLine("// Paste this into your Runtime assembly");
            writer.WriteLine("using System.Runtime.CompilerServices;");
            writer.WriteLine();

            foreach (var type in movedTypes)
            {
                writer.WriteLine($"[assembly: TypeForwardedTo(typeof({type}))]");
            }
        }

        AssetDatabase.Refresh();
        Debug.Log($"TypeForwardedTo file generated at: {csFilePath}");
    }
}
#endif