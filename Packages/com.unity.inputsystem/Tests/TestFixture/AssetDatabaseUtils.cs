#if UNITY_EDITOR

using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine.UIElements;

namespace UnityEngine.InputSystem
{
    /// Provides convenience functions for creating and managing assets for test purposes.
    public class AssetDatabaseUtils
    {
        private static List<string> s_FilePaths = new List<string>();
        private static List<string> s_DirectoryPaths = new List<string>();
        private const string kAssetPath = "Assets";
        private const string kTestPath = "TestFiles";

        private static T CreateAssetAtPath<T>(string path, string content) where T : UnityEngine.Object
        {
            Debug.Assert(!File.Exists(path));

            T obj;
            try
            {
                CreateDirectories(Path.GetDirectoryName(path));

                File.WriteAllText(path, content);
                AssetDatabase.ImportAsset(path);
                obj = AssetDatabase.LoadAssetAtPath<T>(path);
                if (obj == null)
                    throw new Exception($"Failed to create asset at \"{path}\"");
                s_FilePaths.Add(path);
            }
            catch (Exception)
            {
                AssetDatabase.DeleteAsset(path);
                throw;
            }

            return obj;
        }

        private static string CreateDirectories(string path)
        {
            if (Directory.Exists(path))
                return path;

            var parentFolder = kAssetPath;
            var directories = path.Split('/');
            if (directories[0] != kAssetPath)
                throw new ArgumentException(path);
            for (var i = 1; i < directories.Length; ++i)
            {
                var guid = AssetDatabase.CreateFolder(parentFolder, directories[i]);
                if (guid == string.Empty)
                    throw new Exception("Failed to create path \"" + path + "\"");
                parentFolder = Path.Combine(parentFolder, directories[i]);
            }
            s_DirectoryPaths.Add(path);

            AssetDatabase.Refresh();

            return path;
        }

        public static string CreateDirectory()
        {
            return CreateDirectories(RandomDirectoryPath());
        }

        public static T CreateAsset<T>(string directoryPath, string filename = null, string content = null) where T : UnityEngine.Object
        {
            Debug.Assert(directoryPath == null || directoryPath.Contains(RootPath()));
            Debug.Assert(filename == null || !filename.Contains("/"));

            string path = null;
            if (directoryPath == null)
                directoryPath = RootPath();
            if (filename != null)
            {
                path = RandomAssetFilePath(directoryPath, AssetFileExtensionFromType(typeof(T)));
                if (File.Exists(path))
                    throw new Exception($"File already exists: {path}");
            }

            return CreateAsset<T>(path: path, content: content);
        }

        public static T CreateAsset<T>(string path = null, string content = null) where T : UnityEngine.Object
        {
            if (path == null)
                path = RandomAssetFilePath(RootPath(), AssetFileExtensionFromType(typeof(T)));
            if (content == null)
                content = DefaultContentFromType(typeof(T));
            return CreateAssetAtPath<T>(path, content);
        }

        public static void MoveFileOrDirectory(string source, string destination)
        {
            Debug.Assert(source.Contains(RootPath()));
            Debug.Assert(destination.Contains(RootPath()));

            FileUtil.MoveFileOrDirectory(source, destination);
            AssetDatabase.Refresh();
        }

        public static void Restore()
        {
            var root = RootPath();

            // Delete all files in test folder
            if (!Directory.Exists(root))
                return;

            foreach (var asset in AssetDatabase.FindAssets("", new string[] { root }))
            {
                var path = AssetDatabase.GUIDToAssetPath(asset);
                AssetDatabase.DeleteAsset(path);
            }

            AssetDatabase.DeleteAsset(root);

            //AssetDatabase.DeleteAsset(root);

            /*var stack = new Stack<string>();
            stack.Push(root);

            while (stack.Count != 0)
            {
                var dir = stack.Pop();
                File.Dele
            }

            AssetDatabase.GetSubFolders(path);

            // Delete all files created
            if (s_FilePaths.Count > 0)
            {
                foreach (var path in s_FilePaths)
                {
                    AssetDatabase.DeleteAsset(path);
                }

                s_FilePaths.Clear();
            }*/
        }

        private static string RandomName()
        {
            const double scale = int.MaxValue;
            double r = UnityEngine.Random.value;
            return "Test_" + (int)(Math.Floor(r * scale));
        }

        private static string RandomAssetFilePath(string directoryPath, string extension)
        {
            string path;
            do
            {
                path = Path.Combine(directoryPath, RandomName() + "." + extension);
            }
            while (File.Exists(path));
            return path;
        }

        private static string RootPath()
        {
            return Path.Combine(kAssetPath, kTestPath);
        }

        public static string RandomDirectoryPath()
        {
            string path;
            do
            {
                path = Path.Combine(RootPath(), RandomName());
            }
            while (File.Exists(path));
            return path;
        }

        private static string AssetFileExtensionFromType(Type type)
        {
            if (type == typeof(InputActionAsset))
                return InputActionAsset.Extension;
            return "asset";
        }

        private static string DefaultContentFromType(Type type)
        {
            if (type == typeof(InputActionAsset))
                return "{}";
            return string.Empty;
        }
    }
}

#endif // UNITY_EDITOR
