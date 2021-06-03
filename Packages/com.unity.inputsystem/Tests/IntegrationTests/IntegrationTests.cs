using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.Scripting;
using UnityEngine.TestTools;
#if UNITY_EDITOR
using UnityEngine.InputSystem.Editor;

#endif

// Disable irrelevant warning about there not being underscores in method names.
#pragma warning disable CA1707

// These tests are the only ones that we put *in* the package. The rest of our tests live in Assets/Tests and run separately
// from our CI and not through upm-ci. This also means that IntegrationTests is the only thing we put on trunk through our
// verified package.
//
// Rationale:
// (1) Our APIVerificationTests have extra package requirements and thus need a custom package manifest.json. This will not
//     work with upm-ci.
// (2) The tests we have in Assets/Tests exercise the input system in isolation. Having these run on trunk in addition to our
//     CI in the input system repo adds little value while adding extra execution time to trunk QV runs. This is unlike
//     the integration tests here which add value to trunk by making sure the input system is intact all the way through
//     to the native input module.
// (3) If we added everything in Assets/Tests to the package, we would add more stuff to user projects that has no value to users.
//
// NOTE: The tests here are necessary to pass the requirement imposed by upm-ci that a package MUST have tests in it.

public class IntegrationTests
{
    [Preserve]
    public static void PreserveMethods()
    {
        // Workaround a bug in com.unity.test-framework.utp-reporter
        // Due Stripping set to to High System.ComponentModel.StringConverter ctor is stripped, making first test Integration_CanSendAndReceiveEvents to fail
        //      MissingMethodException: Constructor on type 'System.ComponentModel.StringConverter' not found.
        //at System.RuntimeType.CreateInstanceImpl(System.Reflection.BindingFlags bindingAttr, System.Reflection.Binder binder, System.Object[] args, System.Globalization.CultureInfo culture, System.Object[] activationAttributes, System.Threading.StackCrawlMark & stackMark)[0x001f0] in < b6074dacdf2142f38da4050b03a225bb >:0
        //at System.Activator.CreateInstance(System.Type type, System.Reflection.BindingFlags bindingAttr, System.Reflection.Binder binder, System.Object[] args, System.Globalization.CultureInfo culture, System.Object[] activationAttributes)[0x00095] in < b6074dacdf2142f38da4050b03a225bb >:0
        //at System.Activator.CreateInstance(System.Type type, System.Reflection.BindingFlags bindingAttr, System.Reflection.Binder binder, System.Object[] args, System.Globalization.CultureInfo culture)[0x00000] in < b6074dacdf2142f38da4050b03a225bb >:0
        //at System.SecurityUtils.SecureCreateInstance(System.Type type, System.Object[] args, System.Boolean allowNonPublic)[0x0003a] in < 43cff77c8e644fb3bd45df5f20310d13 >:0
        //at System.SecurityUtils.SecureCreateInstance(System.Type type)[0x00000] in < 43cff77c8e644fb3bd45df5f20310d13 >:0
        //at System.ComponentModel.ReflectTypeDescriptionProvider.CreateInstance(System.Type objectType, System.Type callingType)[0x0001a] in < 43cff77c8e644fb3bd45df5f20310d13 >:0
        //at System.ComponentModel.ReflectTypeDescriptionProvider.SearchIntrinsicTable(System.Collections.Hashtable table, System.Type callingType)[0x0015d] in < 43cff77c8e644fb3bd45df5f20310d13 >:0
        //at System.ComponentModel.ReflectTypeDescriptionProvider + ReflectedTypeData.GetConverter(System.Object instance)[0x000fc] in < 43cff77c8e644fb3bd45df5f20310d13 >:0
        //at System.ComponentModel.ReflectTypeDescriptionProvider.GetConverter(System.Type type, System.Object instance)[0x00008] in < 43cff77c8e644fb3bd45df5f20310d13 >:0
        //at System.ComponentModel.TypeDescriptor + TypeDescriptionNode + DefaultTypeDescriptor.System.ComponentModel.ICustomTypeDescriptor.GetConverter()[0x00016] in < 43cff77c8e644fb3bd45df5f20310d13 >:0
        //at System.ComponentModel.TypeDescriptor.GetConverter(System.Type type)[0x0000b] in < 43cff77c8e644fb3bd45df5f20310d13 >:0
        //at Newtonsoft.Json.Serialization.JsonTypeReflector.CanTypeDescriptorConvertString(System.Type type, System.ComponentModel.TypeConverter & typeConverter)[0x00000] in < 489f342f5a6b4cd3856aec7b3d5c47e7 >:0
        //at Newtonsoft.Json.Serialization.JsonSerializerInternalWriter.TryConvertToString(System.Object value, System.Type type, System.String & s)[0x00000] in < 489f342f5a6b4cd3856aec7b3d5c47e7 >:0
        //at Newtonsoft.Json.Serialization.JsonSerializerInternalWriter.GetPropertyName(Newtonsoft.Json.JsonWriter writer, System.Object name, Newtonsoft.Json.Serialization.JsonContract contract, System.Boolean & escape)[0x00127] in < 489f342f5a6b4cd3856aec7b3d5c47e7 >:0
        //at Newtonsoft.Json.Serialization.JsonSerializerInternalWriter.SerializeDictionary(Newtonsoft.Json.JsonWriter writer, System.Collections.IDictionary values, Newtonsoft.Json.Serialization.JsonDictionaryContract contract, Newtonsoft.Json.Serialization.JsonProperty member, Newtonsoft.Json.Serialization.JsonContainerContract collectionContract, Newtonsoft.Json.Serialization.JsonProperty containerProperty)[0x000c6] in < 489f342f5a6b4cd3856aec7b3d5c47e7 >:0
        //at Newtonsoft.Json.Serialization.JsonSerializerInternalWriter.SerializeValue(Newtonsoft.Json.JsonWriter writer, System.Object value, Newtonsoft.Json.Serialization.JsonContract valueContract, Newtonsoft.Json.Serialization.JsonProperty member, Newtonsoft.Json.Serialization.JsonContainerContract containerContract, Newtonsoft.Json.Serialization.JsonProperty containerProperty)[0x0013d] in < 489f342f5a6b4cd3856aec7b3d5c47e7 >:0
        //at Newtonsoft.Json.Serialization.JsonSerializerInternalWriter.Serialize(Newtonsoft.Json.JsonWriter jsonWriter, System.Object value, System.Type objectType)[0x00079] in < 489f342f5a6b4cd3856aec7b3d5c47e7 >:0
        //at Newtonsoft.Json.JsonSerializer.SerializeInternal(Newtonsoft.Json.JsonWriter jsonWriter, System.Object value, System.Type objectType)[0x0023a] in < 489f342f5a6b4cd3856aec7b3d5c47e7 >:0
        //at Newtonsoft.Json.JsonSerializer.Serialize(Newtonsoft.Json.JsonWriter jsonWriter, System.Object value, System.Type objectType)[0x00000] in < 489f342f5a6b4cd3856aec7b3d5c47e7 >:0
        //at Newtonsoft.Json.JsonConvert.SerializeObjectInternal(System.Object value, System.Type type, Newtonsoft.Json.JsonSerializer jsonSerializer)[0x00028] in < 489f342f5a6b4cd3856aec7b3d5c47e7 >:0
        //at Newtonsoft.Json.JsonConvert.SerializeObject(System.Object value, System.Type type, Newtonsoft.Json.Formatting formatting, Newtonsoft.Json.JsonSerializerSettings settings)[0x0000e] in < 489f342f5a6b4cd3856aec7b3d5c47e7 >:0
        //at Newtonsoft.Json.JsonConvert.SerializeObject(System.Object value, Newtonsoft.Json.Formatting formatting, Newtonsoft.Json.JsonSerializerSettings settings)[0x00000] in < 489f342f5a6b4cd3856aec7b3d5c47e7 >:0
        //at Newtonsoft.Json.JsonConvert.SerializeObject(System.Object value, Newtonsoft.Json.Formatting formatting)[0x00000] in < 489f342f5a6b4cd3856aec7b3d5c47e7 >:0
        //at Unity.TestProtocol.UnityTestProtocolMessageBuilder.BuildMessage(System.Collections.Specialized.OrderedDictionary fields)[0x00001] in < eea339da6b5e4d4bb255bfef95601890 >:0
        //at Unity.TestProtocol.UnityTestProtocolMessageBuilder.Serialize(Unity.TestProtocol.Message message)[0x0006c] in < eea339da6b5e4d4bb255bfef95601890 >:0
        //at Unity.TestFramework.UTPReporter.TestResultToUtpMessage.Send(NUnit.Framework.Interfaces.IXmlNodeBuilder xmlNodeBuilder)[0x0002d] in F:\Projects\InputSystem\Library\PackageCache\com.unity.test - framework.utp - reporter@0.1.3 - preview.18\Runtime\TestResultsHandler.cs:65
        //at Unity.TestFramework.UTPReporter.TestResultToUtpMessage.TestStarted(NUnit.Framework.Interfaces.ITest testStartedResult)[0x00001] in F:\Projects\InputSystem\Library\PackageCache\com.unity.test - framework.utp - reporter@0.1.3 - preview.18\Runtime\TestResultsHandler.cs:50
        //at UnityEngine.TestRunner.Utils.TestRunCallbackListener +<> c__DisplayClass5_0.< TestStarted > b__0(UnityEngine.TestRunner.ITestRunCallback callback)[0x00000] in F:\Projects\InputSystem\Library\PackageCache\com.unity.test - framework@1.1.14\UnityEngine.TestRunner\Utils\TestRunCallbackListener.cs:55
        //at UnityEngine.TestRunner.Utils.TestRunCallbackListener.InvokeAllCallbacks(System.Action`1[T] invoker)[0x0002d] in F:\Projects\InputSystem\Library\PackageCache\com.unity.test - framework@1.1.14\UnityEngine.TestRunner\Utils\TestRunCallbackListener.cs:38
        var dummy = new System.ComponentModel.StringConverter();
    }

    [Test]
    [Category("Integration")]
    public void Integration_CanSendAndReceiveEvents()
    {
        var keyboard = InputSystem.AddDevice<Keyboard>();

        try
        {
            InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.A));
            InputSystem.Update();

            Assert.That(keyboard.aKey.isPressed, Is.True);
        }
        finally
        {
            InputSystem.RemoveDevice(keyboard);
        }
    }

#if UNITY_EDITOR

    [Test]
    [Category("Integration")]
    public void Integration_CanChangeInputBackendPlayerSettingInEditor()
    {
        // Save current player settings so we can restore them.
        var oldEnabled = EditorPlayerSettingHelpers.oldSystemBackendsEnabled;
        var newEnabled = EditorPlayerSettingHelpers.newSystemBackendsEnabled;

        // Enable new and disable old.
        EditorPlayerSettingHelpers.newSystemBackendsEnabled = true;
        EditorPlayerSettingHelpers.oldSystemBackendsEnabled = false;
        Assert.That(EditorPlayerSettingHelpers.newSystemBackendsEnabled, Is.True);
        Assert.That(EditorPlayerSettingHelpers.oldSystemBackendsEnabled, Is.False);

        // Enable old and disable new.
        EditorPlayerSettingHelpers.newSystemBackendsEnabled = false;
        EditorPlayerSettingHelpers.oldSystemBackendsEnabled = true;
        Assert.That(EditorPlayerSettingHelpers.newSystemBackendsEnabled, Is.False);
        Assert.That(EditorPlayerSettingHelpers.oldSystemBackendsEnabled, Is.True);

        // Enable both.
        EditorPlayerSettingHelpers.newSystemBackendsEnabled = true;
        EditorPlayerSettingHelpers.oldSystemBackendsEnabled = true;
        Assert.That(EditorPlayerSettingHelpers.newSystemBackendsEnabled, Is.True);
        Assert.That(EditorPlayerSettingHelpers.oldSystemBackendsEnabled, Is.True);

        // Restore previous settings.
        EditorPlayerSettingHelpers.oldSystemBackendsEnabled = oldEnabled;
        EditorPlayerSettingHelpers.newSystemBackendsEnabled = newEnabled;
    }

#endif

#if UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN

    [UnityTest]
    [Category("Integration")]
    [Ignore("Unstable due to 1252825")]
    public IEnumerator WindowsInput_RemoteDesktopMouseMovements_AreDetected()
    {
        var mouse = InputSystem.GetDevice<Mouse>();
        var currentPosition = mouse.position.ReadValue();

        yield return new WaitForSeconds(0.1f);
        Assert.AreEqual(currentPosition, mouse.position.ReadValue(), "Expected mouse position to not change when no input was sent. Please do not move the mouse during this test.");

        WinUserInput.SendRDPMouseMoveEvent(10, 10);
        yield return new WaitForSeconds(0.1f);

        Assert.AreNotEqual(currentPosition, mouse.position.ReadValue(), "Expected mouse position to have moved when sending RDP/absolute values.");
        currentPosition = mouse.position.ReadValue();

        WinUserInput.SendRDPMouseMoveEvent(100, 100);
        yield return new WaitForSeconds(0.1f);
        Assert.AreNotEqual(currentPosition, mouse.position.ReadValue(), "Expected mouse position to have moved when sending RDP/absolute values.");
    }

    [UnityTest]
    [Category("Integration")]
    [Ignore("Unstable due to 1252825")]
    public IEnumerator WindowsInput_MouseMovements_AreDetected()
    {
        var mouse = InputSystem.GetDevice<Mouse>();
        var currentPosition = mouse.position.ReadValue();

        yield return new WaitForSeconds(0.1f);
        Assert.AreEqual(currentPosition, mouse.position.ReadValue(), "Expected mouse position to not change when no input was sent. Please do not move the mouse during this test.");

        WinUserInput.SendMouseMoveEvent(10, 10);
        yield return new WaitForSeconds(0.1f);

        Assert.AreNotEqual(currentPosition, mouse.position.ReadValue(), "Expected mouse position to have moved when sending relative values.");
        currentPosition = mouse.position.ReadValue();

        WinUserInput.SendMouseMoveEvent(100, 100);
        yield return new WaitForSeconds(0.1f);
        Assert.AreNotEqual(currentPosition, mouse.position.ReadValue(), "Expected mouse position to have moved when sending relative values.");
    }

    #endif // UNITY_2019_3_OR_NEWER && (UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN)
}
