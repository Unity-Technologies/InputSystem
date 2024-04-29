using System;
using UnityEditor;
using UnityEngine.InputSystem.Utilities;
using UnityEngine.Serialization;

#if UNITY_EDITOR

namespace UnityEngine.InputSystem.Editor
{
    public static class InputBindingPresets
    {
        private static InputBindingAsset Create(string name)
        {
            var temp = ScriptableObject.CreateInstance<InputBindingAsset>();
            temp.name = name;
            return temp;
        }

        private static void CreateAssetWithContent(string name, InputBindingAsset blueprint)
        {
            ProjectWindowUtil.CreateAssetWithContent(
                filename: BindingImporter.GetFileName(name),
                content: blueprint.ToJson(), 
                icon: BindingImporter.GetIcon());
            Object.DestroyImmediate(blueprint); // TODO Better have internal object exposed
        }
        
        private static InputBindingAsset Jump()
        {
            var asset = Create("Jump");
            asset.AddBinding(new InputBinding("<Keyboard>/space"));
            return asset;
        }
        
        [MenuItem("Assets/Create/Input Binding/Jump")]
        public static void CreateBindingAssetJumpPreset()
        {
            CreateAssetWithContent("Jump", Jump());
        }
    }

    [Serializable]
    internal struct InputBindingSet
    {
        [SerializeField] public InputBinding[] bindings;
    }

    public interface IBind
    {
        bool Bind(InputAction action, Type typeConstraint);
    }

    public class ProgrammaticBind : MonoBehaviour, IBind
    {
        public bool Bind(InputAction action, Type typeConstraint)
        {
            throw new NotImplementedException("MonoBehavior Bind");
        }
    }
    
    /// <summary>
    /// The built-in asset representation of a binding asset.
    /// </summary>
    public class InputBindingAsset : ScriptableObject, IBind
    {
        [SerializeField] private InputBindingSet data;
        
        // TODO Recreate editor from InputActionDrawer to start with
        
        // TODO Consider implementing an mutable and readonly interface here, e.g. IBind<T> where we can extract binding of type T.
        // TODO In theory this may be a container containing anything implementing such interface or an indirection
        //      allowing resolving a binding for type T.

        public bool Bind(InputAction action, Type typeConstraint)
        {
            // Not possible to bind an action if there are no binding candidates
            if (data.bindings == null || data.bindings.Length == 0)
                return false;
            
            // TODO Perform filtering based on applicable bindings
            
            // Apply bindings
            foreach (var binding in data.bindings)
                action.AddBinding(data.bindings[0]);
            
            return false;
        }
        
        public void AddBinding(InputBinding binding)
        {
            ArrayHelpers.Append(ref data.bindings, binding);
        }
        
        [Serializable]
        private struct ReadFileJson
        {
            public string name;
            public InputActionMap.BindingJson[] bindings;
        }

        [Serializable]
        private struct WriteFileJson
        {
            public string name;
            public InputActionMap.BindingJson[] bindings;

            private static InputActionMap.BindingJson[] ToBindingJson(InputBinding[] bindings)
            {
                if (bindings == null)
                    return null;
                var result = new InputActionMap.BindingJson[bindings.Length];
                for (var i = 0; i < bindings.Length; ++i)
                    result[i] = InputActionMap.BindingJson.FromBinding(ref bindings[i]);
                return result;
            }
            
            public static WriteFileJson FromInputBindingAsset(InputBindingAsset asset)
            {
                return new WriteFileJson
                {
                    name = asset.name,
                    bindings = ToBindingJson(asset.data.bindings)
                };
            }
        }

        public void LoadFromJson(string json)
        {
            if (string.IsNullOrEmpty(json))
                throw new ArgumentNullException(nameof(json));

            // Parse
            var parsedJson = JsonUtility.FromJson<ReadFileJson>(json);
            InputBinding[] bindings = null; 
            if (parsedJson.bindings != null)
            {
                bindings = new InputBinding[parsedJson.bindings.Length];
                for (var i = 0; i < data.bindings.Length; ++i)
                {
                    var jsonBinding = parsedJson.bindings[i];
                    var binding = jsonBinding.ToBinding();
                    data.bindings[i] = binding;
                }    
            }
            
            // Finally assign
            this.name = parsedJson.name;
            this.data.bindings = bindings;
        }
        
        public string ToJson()
        {
            // Generate JSON representation
            InputActionMap.BindingJson[] bindings = null;
            if (data.bindings != null)
            {
                bindings = new InputActionMap.BindingJson[data.bindings.Length];
                for (var i = 0; i < data.bindings.Length; ++i)
                    bindings[i] = InputActionMap.BindingJson.FromBinding(ref data.bindings[i]);
            }
            
            // Convert to JSON context
            return JsonUtility.ToJson(new WriteFileJson
            {
                name = this.name,
                bindings = bindings,
            }, true);
        }
    }
}

#endif