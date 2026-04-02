using System.Linq;
#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using UnityEngine.InputSystem;
using UnityEditor.PackageManager;
using UnityEditor.PackageManager.Requests;

internal class DownloadableSample : ScriptableObject
{
    public string url;
    public string[] packageDeps;
}

[CustomEditor(typeof(DownloadableSample))]
internal class DownloadableSampleInspector : Editor
{
    private ListRequest list;
    private AddRequest add;

    public void OnEnable()
    {
        list = Client.List();
    }

    bool HasPackage(string id)
    {
        if (id.Contains('@'))
            return list.Result.Any(x => x.packageId == id);
        else
            return list.Result.Any(x => x.packageId.Split('@')[0] == id);
    }

    public override void OnInspectorGUI()
    {
        GUILayout.Label("Downloadable Sample", EditorStyles.boldLabel);

        var sample = (DownloadableSample)target;
        EditorGUILayout.HelpBox($"The {target.name} sample is stored outside of the input system package, because it contains custom project settings and is too big to be distributed as part of the sample. Instead, you can download it as a .unitypackage file which you can import into the project. Click the button below to download this sample", MessageType.Info);
        if (GUILayout.Button("Download Sample"))
        {
            var url = sample.url.Replace("%VERSION%", InputSystem.version.ToString());
            Application.OpenURL(url);
        }

        GUILayout.Space(10);

        GUILayout.Label("Package Dependencies", EditorStyles.boldLabel);

        // We are adding a new package, wait for the operation to finish and then relist.
        if (add != null)
        {
            if (add.IsCompleted)
            {
                add = null;
                list = Client.List();
            }
        }

        if (add != null || !list.IsCompleted)
            // Keep refreshing while we are waiting for Packman to resolve our request.
            Repaint();
        else
        {
            if (!sample.packageDeps.All(x => HasPackage(x)))
                EditorGUILayout.HelpBox($"The {target.name} sample requires the following packages to be installed in your Project. Please install all the required packages before downloading the sample!", MessageType.Warning);
        }


        foreach (var req in sample.packageDeps)
        {
            Rect rect = EditorGUILayout.GetControlRect(true, 20);

            GUI.Label(rect, new GUIContent(req), EditorStyles.label);
            rect.width -= 200;
            rect.x += 200;
            if (add != null || !list.IsCompleted)
            {
                using (new EditorGUI.DisabledScope(true))
                {
                    GUI.Label(rect, "checking…", EditorStyles.label);
                }
            }
            else if (HasPackage(req))
            {
                GUI.Label(rect, $"OK \u2713", EditorStyles.boldLabel);
            }
            else
            {
                GUI.Label(rect, "Missing \u2717", EditorStyles.label);
                rect.x += rect.width - 80;
                rect.width = 80;
                if (GUI.Button(rect, "Install"))
                    add = Client.Add(req);
            }
        }
    }
}

#endif
