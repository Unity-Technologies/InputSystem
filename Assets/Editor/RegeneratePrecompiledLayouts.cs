using System;
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Editor;

namespace Editor
{
    internal static class RegeneratePrecompiledLayouts
    {
        private static void GeneratePrecompiledLayout(string path, string layoutName)
        {
            var filePath = Path.Combine(path, "Fast" + layoutName + ".cs");
            if (!File.Exists(filePath))
            {
                Debug.Log($"Cannot generate precompiled layout for: '{filePath}', only existing files may be updated.");
                return;
            }
            var code = InputLayoutCodeGenerator.GenerateCodeFileForDeviceLayout(layoutName, filePath, prefix: "Fast");
            var existingContent = File.ReadAllText(filePath);
            if (string.Compare(existingContent, code, StringComparison.InvariantCulture) == 0)
            {
                Debug.Log($"Skipped updating precompiled layout: '{filePath}'. No difference.");
                return;
            }

            try
            {
                File.WriteAllText(filePath, code);
            }
            catch (Exception e)
            {
                Debug.LogException(e);
                return;
            }

            Debug.Log($"Updated precompiled layout: '{filePath}'.");
        }

        private static void GeneratePrecompiledLayouts(string path)
        {
            GeneratePrecompiledLayout(path, nameof(Keyboard));
            GeneratePrecompiledLayout(path, nameof(Mouse));
            GeneratePrecompiledLayout(path, nameof(Touchscreen));
        }

        [MenuItem("QA Tools/Regenerate Precompiled Layouts", priority = 20)]
        private static void GeneratePrecompiledLayouts()
        {
            GeneratePrecompiledLayouts("Packages/com.unity.inputsystem/InputSystem/Devices/Precompiled");
        }
    }
}
