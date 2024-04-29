using System;

namespace UnityEngine.InputSystem
{
    [Serializable]
    public sealed class TypedInputAction<T> : InputAction, ISerializationCallbackReceiver 
    {
        [SerializeReference] private Action<InputAction> preset; // Non-ideal, would ideally only be set on construction
        
        public TypedInputAction(Action<InputAction> preset)
            //: base(name: null) // Ideally no name
        {
            this.preset = preset;
        }
        
        public Action<InputAction> bindingPreset => preset;
        
        public void ApplyPresetIfNotAlreadyBound()
        {
            // Early return in case the action has already been bound in edit-mode
            if (bindings.Count > 0)
                return; 
        
            // Apply preset
            preset(this);
        }

        public void OnBeforeSerialize()
        {
            
        }

        public void OnAfterDeserialize()
        {
            
        }
    }
}