using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.PackageManager.DocumentationTools.UI;

[CustomEditor(typeof(PackageSetAsset))]
public class PackageSetAssetEditor : Editor
{
    private SerializedProperty packageSet;
    private SerializedProperty targetPath;
    private void OnEnable()
    {
        packageSet = serializedObject.FindProperty("SpaceDelimitedPackageNames");
        targetPath = serializedObject.FindProperty("DestinationPath");
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();
        EditorGUILayout.PropertyField(property: packageSet, includeChildren: true);
        EditorGUILayout.PropertyField(targetPath, true);
        serializedObject.ApplyModifiedProperties();

        if(GUILayout.Button("Add these packages to project"))
        {
            Batch.AddPackagesFromString(packageSet.stringValue);
        }
        if(GUILayout.Button("Generate these docs"))
        {
            GlobalSettings.DestinationPath = targetPath.stringValue;
            GlobalSettings.Progress = 0;
            Batch.GenerateDocsFromString(packageSet.stringValue);
        }
        //if(GUILayout.Button("Remove these packages from project"))
        //{
        //    Debug.LogWarning("Remove function not implemented yet.");
        //    //Batch.RemovePackagesFromString(packageSet.stringValue);
        //}

    }

}
