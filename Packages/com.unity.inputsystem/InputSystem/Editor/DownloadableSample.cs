#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using UnityEngine.InputSystem;

internal class DownloadableSample : ScriptableObject
{
    public string url;
}

[CustomEditor(typeof(DownloadableSample))]
internal class DownloadableSampleInspector : Editor
{
    public override void OnInspectorGUI()
    {
        EditorGUILayout.HelpBox($"The {target.name} sample is stored outside of the input system package, because it contains custom project settings and is too big to be distributed as part of the sample. Instead, you can download it as a .unitypackage file which you can import into the project. Click the button below to download this sample", MessageType.Info);
        if (GUILayout.Button("Download Sample"))
        {
            var url = ((DownloadableSample)target).url.Replace("%VERSION%", InputSystem.version.ToString());
            Application.OpenURL(url);
        }
    }
}

#endif
