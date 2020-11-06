using System;

namespace UnityEditor.PackageManager.DocumentationTools.UI
{
    internal static class GlobalSettings
    {
        public static bool Validate
        {
            get
            {
                if (EditorPrefs.HasKey("DocTool.Validate"))
                    return EditorPrefs.GetBool("DocTool.Validate");
                return true;
            }
            set
            {
                EditorPrefs.SetBool("DocTool.Validate", value);
            }
        }
        public static bool ServeAfterGeneration
        {
            get
            {
                if (EditorPrefs.HasKey("DocTool.ServeAfterGeneration"))
                    return EditorPrefs.GetBool("DocTool.ServeAfterGeneration");
                return true;
            }
            set
            {
                EditorPrefs.SetBool("DocTool.ServeAfterGeneration", value);
            }
        }
        private static string _destinationPath;
        public static string DestinationPath
        {
            get
            {
                if (_destinationPath == null)
                {
                    var destinationPathVariable = Environment.GetEnvironmentVariable("DOCTOOLS_DESTINATION");
                    if (!string.IsNullOrEmpty(destinationPathVariable))
                        _destinationPath = destinationPathVariable;
                    else if (EditorPrefs.HasKey("DocTool.DestinationPath"))
                        _destinationPath = EditorPrefs.GetString("DocTool.DestinationPath");
                    else return "Enter path...";
                }

                return _destinationPath;
            }
            set
            {
                EditorPrefs.SetString("DocTool.DestinationPath", value);
                _destinationPath = value;
            }
        }
        public static bool DoScriptRef 
        {
            get
            {
                if (EditorPrefs.HasKey("DocTool.DoScriptRef"))
                    return EditorPrefs.GetBool("DocTool.DoScriptRef");
                return true;
            }
            set
            {
                EditorPrefs.SetBool("DocTool.DoScriptRef", value);
            }
        }

        //Needs to be called on a thread, so can't use a pref
        public static bool Debug { get; set; } = false;
        //These are not UI options, so they don't have "prefs"
        public static PackageInfo PackageInformation { get; set; }
        public static string LinkToUnityVersion { get; set; } = "Current";
        public static float Progress { get; set; } = 0;
    }
}
