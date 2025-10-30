using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.InputSystem;

internal static class DumpInputActionReferences
{
    private static void DumpReferences(StringBuilder sb, string prefix, InputActionReference[] references)
    {
        sb.Append(prefix + ":\n");
        foreach (var reference in references)
        {
            var s = reference.action != null ? "Yes" : "No";
            sb.Append($"- {reference.name} (Resolved: {s}, Asset: {reference.asset})\n");
        }
    }

    private static void DumpReferences()
    {
        var sb = new StringBuilder();
        DumpReferences(sb, "Loaded objects", Object.FindObjectsByType<InputActionReference>(
            FindObjectsInactive.Include, FindObjectsSortMode.InstanceID));
        DumpReferences(sb, "All objects:", Resources.FindObjectsOfTypeAll<InputActionReference>());
        Debug.Log(sb.ToString());
    }

    [UnityEditor.MenuItem("QA Tools/Dump Input Action References to Console", false, 100)]
    private static void Dump()
    {
        DumpReferences();
    }
}
