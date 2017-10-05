//allow multiple binding sets in a JSON file

using System;

namespace ISX
{
    public class InputBindingSet
    {
        public static InputBindingSet[] FromJson()
        {
            throw new NotImplementedException();
        }
        
        [Serializable]
        private struct BindingJson
        {
            public string action;
            public string binding;
            public string modifier;
        }

        [Serializable]
        private struct BindingSetJson
        {
            public string name;
            public BindingJson[] bindings;
        }
    }
}
