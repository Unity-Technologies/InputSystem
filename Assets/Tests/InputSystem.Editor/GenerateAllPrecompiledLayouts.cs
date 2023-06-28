using System;
using System.IO;
using UnityEngine;
using UnityEngine.InputSystem.Editor;

namespace UnityEditor.InputSystem
{
    public static class GenerateAllPrecompiledLayouts
    {
        [MenuItem("Assets/Generate precompiled layouts for all devices")]
        static void GenerateLayouts()
        {
            const string folder = "Assets/PrecompiledLayouts";
            Directory.CreateDirectory(folder);
            
            var count = 0;
            foreach (var layout in UnityEngine.InputSystem.InputSystem.ListLayouts())
            {
                try
                {
                    var loadedLayout = UnityEngine.InputSystem.InputSystem.LoadLayout(layout);
                    if (loadedLayout.isControlLayout)
                        continue;

                    var fileName = $"{folder}/{layout}_{count++}.cs";
                    var code = InputLayoutCodeGenerator.GenerateCodeFileForDeviceLayout(layout, fileName, prefix: "Fast");
                    File.WriteAllText(fileName, code);
                }
                catch (Exception e)
                {
                    Debug.LogException(e);
                }
            }
            
            AssetDatabase.Refresh();
        }
    }
}
