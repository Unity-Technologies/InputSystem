using System;
using System.IO;
using System.Net;
using NUnit.Framework;
using UnityEditor;
using UnityEngine.InputSystem.Experimental;
using UnityEngine.InputSystem.Experimental.Devices;

namespace Tests.InputSystem.Experimental.Bindings
{
    [Category("Experimental")]
    public class InputBindingTests
    {
        private Context m_Context;

        [SetUp]
        public void SetUp()
        {
            m_Context = new Context();
        }

        [TearDown]
        public void TearDown()
        {
            m_Context.Dispose();
        }
        
        private void TestBindingAsset<T>(WrappedScriptableInputBinding<T> asset, string path) 
            where T : struct
        {
            IDisposable subscription = null;
            try
            {
                // Create the asset in asset database
                AssetDatabase.CreateAsset(asset, path);
                
                // TODO Why isn't this exposing problems with serializing this?
                // Make sure it loads and deserializes as specific type
                var actual = AssetDatabase.LoadAssetAtPath<WrappedScriptableInputBinding<T>>(path);
                Assert.That(actual, Is.Not.Null);
                Assert.That(actual.value, Is.EqualTo(asset.value));
                
                // Make sure it loads and deserializes as base type
                var actualGeneric = AssetDatabase.LoadAssetAtPath<ScriptableInputBinding<T>>(path);
                Assert.That(actualGeneric, Is.Not.Null);

                // Make sure that we can subscribe to binding (this verifies underlying hierarchy)
                var observer = new ListObserver<T>();
                subscription = actualGeneric.Subscribe(m_Context, observer);
                Assert.That(subscription, Is.Not.Null);
            }
            catch (Exception)
            {
                subscription?.Dispose();
                AssetDatabase.DeleteAsset(path);
                throw;
            }
        }

        private void TestBindingAsset<T>(WrappedScriptableInputBinding<T> asset) 
            where T : struct
        {
            var path = "Assets/" + TestContext.CurrentContext.Test.Name + ".asset";
            TestBindingAsset(asset, path);
        }
        
        // TODO Convert to parameterized tests
        
        [Test]
        public void BooleanInputBinding_KeyboardKey_SupportsSerialization() => TestBindingAsset(
            ScriptableInputBinding.Create(Keyboard.W));
        
        [Test]
        public void BooleanInputBinding_GamepadButtonSouth_SupportsSerialization() => TestBindingAsset(
            ScriptableInputBinding.Create(Gamepad.ButtonSouth));
        
        [Test]
        public void InputEventInputBinding_SupportsSerialization() => TestBindingAsset(
            ScriptableInputBinding.Create(Gamepad.ButtonSouth.Pressed()));
        
        [Test] 
        public void Vector2InputBinding_GamepadLeftStick_SupportsSerialization() => TestBindingAsset(
            ScriptableInputBinding.Create(Gamepad.leftStick));
        
        [Test] 
        public void Vector2InputBinding_KeyboardComposite_SupportsSerialization() => TestBindingAsset(
            ScriptableInputBinding.Create(Combine.Composite(Keyboard.A, Keyboard.D, Keyboard.S, Keyboard.W)));
    }
}