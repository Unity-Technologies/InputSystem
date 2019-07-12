using System;
using UnityEditor;
using UnityEditor.Presets;
using UnityEngine;

[InitializeOnLoad]
public class ImportInputManagerAsset
{
    static private string m_thisFolder;
    static private UnityEngine.Object m_inputManAsset;
    static private Preset m_inputTestPreset;

    static private bool m_enabledFunction = true;   // In case of no correct folder can be found, cease all function

    // Import the Input Manager asset for Input Test and back up the current one for backup
    [MenuItem("QA Tools/Input Test/Import for Input Test", true, 0)]
    static bool CheckImportInputAssetForTest()
    {
        return !System.IO.File.Exists(m_thisFolder + "default.preset") & m_enabledFunction;
    }

    [MenuItem("QA Tools/Input Test/Import for Input Test", false, 0)]
    static void ImportInputAssetForTest()
    {
        Preset defaultPreset = new Preset(m_inputManAsset);
        AssetDatabase.CreateAsset(defaultPreset, m_thisFolder + "default.preset");
        m_inputTestPreset.ApplyTo(m_inputManAsset);
    }

    // Revert the Input Manager asset to the default one for the project
    [MenuItem("QA Tools/Input Test/Revert to Default", true, 0)]
    static bool CheckRevertToDefaultInputAsset()
    {
        return System.IO.File.Exists(m_thisFolder + "default.preset") & m_enabledFunction;
    }

    [MenuItem("QA Tools/Input Test/Revert to Default", false, 0)]
    static void RevertToDefaultInputAsset()
    {
        try
        {
            Preset defaultPreset = (Preset)AssetDatabase.LoadAssetAtPath(m_thisFolder + "default.preset", typeof(Preset));
            defaultPreset.ApplyTo(m_inputManAsset);
            AssetDatabase.DeleteAsset(m_thisFolder + "default.preset");
        }
        catch (NullReferenceException e)
        {
            Debug.LogWarning(e.HResult);
        }
    }

    static ImportInputManagerAsset()
    {
        // Get the folder's relative path
        string[] thisGUID = AssetDatabase.FindAssets("ImportInputManagerAsset");
        if (thisGUID.Length == 0)
        {
            Debug.LogError("Cannot find Editor folder. Import/reverse input settings will fail.");
            m_enabledFunction = false;
        }
        else
            m_thisFolder = AssetDatabase.GUIDToAssetPath(thisGUID[0]).Replace("ImportInputManagerAsset.cs", "");

        m_inputManAsset = AssetDatabase.LoadAssetAtPath("ProjectSettings/InputManager.asset", typeof(UnityEngine.Object));
        m_inputTestPreset = (Preset)AssetDatabase.LoadAssetAtPath(m_thisFolder + "InputTest.preset", typeof(Preset));
    }
}
