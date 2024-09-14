// TODO This should be code generated from preset class

using UnityEditor;

namespace UnityEngine.InputSystem.Experimental.Editor
{
    public static class FirstPersonShooterPresetMenu
    {
        private const string kExtension = ".asset";
        private const string kAssetPath = "Assets/";
        private const string kCategory = "Presets - First Person Shooter";
        private const string kMenu = "Assets/Create/Input/" + kCategory + "/";
        
        // Support creating an empty binding asset
        [MenuItem(kMenu + "Move")]
        public static void CreateMove()
        {
            // TODO We obviously want an overload ToObject() or ToAsset() or ToScriptableObject();
            var asset = InputBinding.Create(Devices.Gamepad.leftStick);
            AssetDatabase.CreateAsset(asset, kAssetPath + "Move" + kExtension);
            //ProjectWindowUtil.CreateAsset(asset, kAssetPath + "Move" + kExtension);
        }
    }
}