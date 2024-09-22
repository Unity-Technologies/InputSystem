// TODO This should be complete code generated from preset class

using UnityEditor;
namespace UnityEngine.InputSystem.Experimental.Editor
{
    internal static class PresetMenu
    {
        public const string Extension = ".asset";
        public const string AssetPath = "Assets/";
        private const string CreateMenu = "Assets/Create/Input Binding Presets/";
        private const string Category = "First Person Shooter";
        public const string AssetsMenu = CreateMenu + Category + "/";
    }
    
    internal static class FirstPersonShooterPresetMenu
    {
        private const string kSpace = " ";
        
        private const string kBool = "(bool)";
        private const string kEvent = "(Event)";
        private const string kAxis2D = "(Axis2D)";
        
        // TODO These constants should be generated based off the actual type of the associated preset
        private const string kMove = "Move";
        private const string kFiring = "Firing";
        private const string kJump = "Jump";
        private const string kLook = "Look";
        
        // Support creating an empty binding asset
        [MenuItem(PresetMenu.AssetsMenu + kMove + " " + kAxis2D)]
        public static void CreateMove() 
        {
            // TODO Needs some work: FirstPersonShooter.Move().CreateAsset(GetPath(kMove));
            Combine.Composite(Devices.Keyboard.A, 
                Devices.Keyboard.D, 
                Devices.Keyboard.S, 
                Devices.Keyboard.W).CreateAsset(GetPath(kMove));
        }
        
        [MenuItem(PresetMenu.AssetsMenu + kFiring + kSpace + kAxis2D)] 
        public static void CreateFire() => 
            Devices.Keyboard.LeftCtrl.CreateAsset(GetPath(kFiring));
        
        [MenuItem(PresetMenu.AssetsMenu + kJump + kSpace + kEvent)] 
        public static void CreateJump() => 
            Devices.Keyboard.Space.Pressed().CreateAsset(GetPath(kJump));

        [MenuItem(PresetMenu.AssetsMenu + kLook + kSpace + kAxis2D)] 
        public static void CreateLook() => 
            Devices.Gamepad.RightStick.CreateAsset(GetPath(kLook));
        
        private static string GetPath(string name) => PresetMenu.AssetPath + name + PresetMenu.Extension;
    }

    public static class InputBindingAssetDatabaseExtensions
    {
        public static void CreateAsset<T>(this IObservableInput<T> source, string path) where T : struct
        {
            var asset = ScriptableInputBinding.Create(source);
            EditorUtility.SetDirty(asset);
            //EditorUtility.SetDirty(asset);
            AssetDatabase.CreateAsset(asset, path);
            AssetDatabase.SaveAssets();
        }
    }
}