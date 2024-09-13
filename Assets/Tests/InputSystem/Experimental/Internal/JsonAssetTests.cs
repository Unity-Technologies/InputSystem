using System;
using NUnit.Framework;
using UnityEditor;
using UnityEngine.InputSystem.Experimental;
using UnityEngine.InputSystem.Experimental.JSON;

namespace Tests.InputSystem.Experimental.Editor.Generator
{
    [Category("Experimental")]
    public class JsonAssetTests
    {
        /*
         "Gamepad.leftStick"
        
        [ 
            "Gamepad.leftStick", 
            { 
                "type": "Composite",
                "negativeX": "Keyboard.A",
                "positiveX": "Keyboard.D",
                "negativeY": "Keyboard.S",
                "positiveY": "Keyboard.W"
            } 
        ]
        
        // TODO Add feature to enumerator to get notification on begin object and end object
        // TODO We basically want to be able to reason about object as a whole, likely this means reading all children into list at end of object instantiate object.
        // TODO How could we instantiate Composite<ObservableInput<bool>, ObservableInput<bool>, ObservableInput<bool>, ObservableInput<bool>> efficiently?
        
        */

        public static class InputSourceFactory
        {
            
        }
        
        [Test]
        public void PoC()
        {
            const string json = "\"Gamepad.leftStick\"";
            var parser = new JsonUtility.JsonContext(json);
            JsonUtility.JsonNode node = default;
            foreach (var item in parser)
            {
                switch (item.type)
                {
                    case JsonUtility.JsonType.String:
                        node = item;
                        var name = item.name;
                        // TODO Call into factory to construct based on name, factory should be registered automatically based off attributes
                        var types = TypeCache.GetTypesWithAttribute<InputSourceAttribute>();
                        UnityEngine.Debug.Log(types);
                        break;
                    case JsonUtility.JsonType.Array:
                        if (node.type == JsonUtility.JsonType.String)
                            throw new Exception("Unrecognized file format");
                        break;
                    case JsonUtility.JsonType.Number:
                    case JsonUtility.JsonType.Value:
                    case JsonUtility.JsonType.Invalid:
                    default:
                        throw new Exception("Unrecognized file format");
                }
            }
        }
    }
}