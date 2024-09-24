using System.Reflection;
using UnityEditor;
using UnityEditor.AssetImporters;
using UnityEditor.Compilation;

namespace UnityEngine.InputSystem.Experimental
{
    [ScriptedImporter(version: 1, ext: Extension, AllowCaching = true)]
    public class InputBindingImporter : ScriptedImporter
    {
        public const string Extension = "inputbinding";
        
        public override void OnImportAsset(AssetImportContext ctx)
        {
            // TODO With this approach we basically need to construct a scriptable object (and define a type for the target value type) based 
            //      on the content of the file.
            
            // TODO We could generate code from this place if needed to support type?
            var path = ctx.assetPath;
            
            // TODO Parse JSON and construct objects

            // Gamepad.leftStick        // TODO Contains punctuation so defer to ObservableInput<bool>
            
            // Effectively could be replaced by a lookup table for edit-mode
            var types = TypeCache.GetTypesWithAttribute(typeof(InputSourceAttribute));
            for (var i = 0; i < types.Count; ++i)
            {
                var fullName = types[i].FullName;
                if (fullName == null || !fullName.Equals("UnityEngine.InputSystem.Experimental.Devices.Gamepad"))
                    continue;
                var field = types[i].GetField("ButtonSouth", BindingFlags.Static | BindingFlags.Public);
                var value = field.GetValue(null);
                //var properties = types[i].GetProperties(BindingFlags.Static);
                //for (var j = 0; j < type[i].)
                //var result = types[i].GetProperty("ButtonSouth", BindingFlags.Static);
                //var value = result.GetValue(null, null);
                Debug.Log("X");
            }
            
            //var proxyType = typeof(UnityEngine.InputSystem.Experimental.Devices.Gamepad);
            //proxyType.GetProperty()
            
            //typeof("UnityEngine.InputSystem.")
            
            //var obj = ScriptableObject.CreateInstance<Vector2ScriptableInputBinding>();
            var obj = ScriptableObject.CreateInstance<Vector2InputBinding>();
            var guid = GUID.Generate().ToString();
            ctx.AddObjectToAsset(guid, obj); // TODO DO we need to keep GUID consistent?
            ctx.SetMainObject(obj);
        }
        
        // Support creating an empty binding asset
        [MenuItem("Assets/Create/Input/Input Binding")]
        public static void CreateInputAsset()
        {
            ProjectWindowUtil.CreateAssetWithContent(
                filename: "New Input Binding." + Extension,
                content: "{}", 
                icon: null); // TODO We should set appropriate icon, otherwise no icon is displayed during renaming (prior to file getting imported).
        }
    }
}