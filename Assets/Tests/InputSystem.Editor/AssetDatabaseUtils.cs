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
        private const string kAssetPath = "Assets";
        private const string kTestPath = "TestFiles";
        private const string kMetaExtension = ".meta";

        // Perform an operation equivalent to a file delete operation outside of Unity Editor.
        // Note that meta file is also removed to avoid generating warnings about non-clean delete.
        public static void ExternalDeleteFileOrDirectory(string path)
        {
            FileUtil.DeleteFileOrDirectory(path);
            FileUtil.DeleteFileOrDirectory(path + kMetaExtension);
        }

        // Perform an operation equivalent to a file move operation outside of Unity Editor.
        // Note that meta file is also moved to avoid generating warnings about non-clean move.
        public static void ExternalMoveFileOrDirectory(string source, string dest)
        {
            FileUtil.MoveFileOrDirectory(source, dest);
            FileUtil.MoveFileOrDirectory(source + kMetaExtension, dest + kMetaExtension);
        }

        // Create an asset at the given path containing the given text content.
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
            }
            catch (Exception)
            {
                AssetDatabase.DeleteAsset(path);
                throw;
            }

            return obj;
        }

        // Creates all directories (including intermediate) defined in path.
        private static string CreateDirectories(string path)
        {
            if (Directory.Exists(path))
                return path;

            var parentFolder = kAssetPath;
            path = path.Replace("\\", "/"); // Make sure we only get '/' separators.
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

            AssetDatabase.Refresh();

            return path;
        }

        // Creates a random test directory within asset folder that is automatically removed after test run.
        public static string CreateDirectory()
        {
            return CreateDirectories(RandomDirectoryPath());
        }

        // Creates an asset in the given directory path with an explicit or random file name containing the
        // given content or the default content based on type.
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

        // Creates an asset at the given path containing the specified content.
        // If path is null, a unique random file name is assigned, if content is null the default content based
        // on type (extension) is used.
        public static T CreateAsset<T>(string path = null, string content = null) where T : UnityEngine.Object
        {
            if (path == null)
                path = RandomAssetFilePath(RootPath(), AssetFileExtensionFromType(typeof(T)));
            if (content == null)
                content = DefaultContentFromType(typeof(T));
            return CreateAssetAtPath<T>(path, content);
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
