#if UNITY_EDITOR
using System;
using System.Linq;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using NUnit.Framework;
using Mono.Cecil;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using HtmlAgilityPack;
using UnityEngine.InputSystem.DualShock;
using UnityEngine.InputSystem.Editor;
using UnityEngine;
using UnityEngine.InputSystem.iOS.LowLevel;
using UnityEngine.InputSystem.Utilities;
using UnityEngine.TestTools;
using Object = System.Object;
using TypeAttributes = Mono.Cecil.TypeAttributes;
using PropertyAttribute = NUnit.Framework.PropertyAttribute;

class APIVerificationTests
{
    private bool IsValidNameForConstant(string name)
    {
        return char.IsUpper(name[0]);
    }

    private static bool TypeHasValidNamespace(TypeReference type)
    {
        // The XR stuff is putting some things in Unity.XR and UnityEngine.XR. While we still have
        // these in the input system itself, accept that namespace. Remove it when
        // the XR layouts are removed.
        if (type.Namespace.StartsWith("Unity.XR") || type.Namespace.StartsWith("UnityEngine.XR"))
            return true;

        // The compiler generates a <Module> type which we want to ignore
        return type.Namespace.StartsWith("UnityEngine.InputSystem") || type.Name == "<Module>";
    }

    // Generally, public API should always expose values as properties, and not as fields.
    // We currently have quite a few exceptions, which are handled here.
    private static bool IsTypeWhichCanHavePublicFields(TypeReference type)
    {
        if (type == null)
            return false;

        // This is the base type of all structs
        if (type.FullName == typeof(ValueType).FullName)
            return false;
        if (type.FullName == typeof(Object).FullName)
            return false;

        if (
            // These have fields populated by reflection in the Input System
            type.FullName == typeof(InputProcessor).FullName ||
            type.FullName == typeof(InputControl).FullName ||
            type.FullName == typeof(InputBindingComposite).FullName
        )
            return true;

        try
        {
            var resolved = type.Resolve();

            if (resolved == null)
                return false;

            if (
                // Interactions have fields populated by reflection in the Input System
                resolved.Interfaces.Any(i => i.InterfaceType.FullName == typeof(IInputInteraction).FullName) ||

                // Input state structures use fields for the memory layout and construct Input Controls from the fields.
                resolved.Interfaces.Any(i => i.InterfaceType.FullName == typeof(IInputStateTypeInfo).FullName) ||

                // These use fields for the explicit memory layout, and have a member for the base type. If we exposed that via a property,
                // base type values could not be written individually.
                resolved.Interfaces.Any(i => i.InterfaceType.FullName == typeof(IInputDeviceCommandInfo).FullName) ||
                resolved.Interfaces.Any(i => i.InterfaceType.FullName == typeof(IInputEventTypeInfo).FullName) ||

                // serializable types may depend on the field names to match serialized data (eg. Json)
                resolved.Attributes.HasFlag(TypeAttributes.Serializable) ||

                // These types need to use fields because they are returned as ref readonly from InputAction.value and we
                // want to avoid defensive copies being created for every property access. Also, we can't use the types
                // Bone and Eyes here because they don't exist on some platforms
                resolved.Name == "Bone" || resolved.Name == "Eyes"
            )
                return true;

            return IsTypeWhichCanHavePublicFields(resolved.BaseType);
        }
        catch (AssemblyResolutionException)
        {
            return false;
        }
    }

    internal static IEnumerable<TypeDefinition> GetInputSystemPublicTypes()
    {
        var codeBase = typeof(InputSystem).Assembly.CodeBase;
        var uri = new UriBuilder(codeBase);
        var path = Uri.UnescapeDataString(uri.Path);
        var asmDef = AssemblyDefinition.ReadAssembly(path);
        return asmDef.MainModule.Types.Where(type => type.IsPublic);
    }

    internal static IEnumerable<FieldDefinition> GetInputSystemPublicFields() => GetInputSystemPublicTypes().SelectMany(t => t.Resolve().Fields).Where(f => f.IsPublic);
    internal static IEnumerable<MethodDefinition> GetInputSystemPublicMethods() => GetInputSystemPublicTypes().SelectMany(t => t.Resolve().Methods).Where(m => m.IsPublic);

    [Test]
    [Category("API")]
    public void API_ConstantsAreAppropriatelyNamed()
    {
        var incorrectlyNamedConstants = GetInputSystemPublicFields().Where(field => field.HasConstant && !IsValidNameForConstant(field.Name));
        Assert.That(incorrectlyNamedConstants, Is.Empty);
    }

    [Test]
    [Category("API")]
    public void API_StaticReadonlyFieldsAreAppropriatelyNamed()
    {
        var incorrectlyNamedConstants = GetInputSystemPublicFields().Where(field => field.IsInitOnly && field.IsStatic && !IsValidNameForConstant(field.Name));
        Assert.That(incorrectlyNamedConstants, Is.Empty);
    }

    [Test]
    [Category("API")]
    public void API_EnumValuesAreAppropriatelyNamed()
    {
        var incorrectlyNamedConstants = GetInputSystemPublicTypes().Where(t => t.IsEnum).SelectMany(t => t.Fields).Where(f => f.IsStatic && !IsValidNameForConstant(f.Name));
        Assert.That(incorrectlyNamedConstants, Is.Empty);
    }

    [Test]
    [Category("API")]
    public void API_TypesHaveAnAppropriateNamespace()
    {
        var incorrectlyNamespacedTypes = GetInputSystemPublicTypes().Where(t => !TypeHasValidNamespace(t));
        Assert.That(incorrectlyNamespacedTypes, Is.Empty);
    }

    [Test]
    [Category("API")]
    public void API_FieldsAreNotIntPtr()
    {
        var intptrFields = GetInputSystemPublicFields().Where(f => f.FieldType.Name == "IntPtr");
        Assert.That(intptrFields, Is.Empty);
    }

    [Test]
    [Category("API")]
    public void API_MethodReturnTypesAreNotIntPtr()
    {
        var intptrMethods = GetInputSystemPublicMethods().Where(m => m.ReturnType.FullName == "System.IntPtr");
        Assert.That(intptrMethods, Is.Empty);
    }

    [Test]
    [Category("API")]
    public void API_MethodParameterTypesAreNotIntPtr()
    {
        // Ignore IntPtr parameters on delegate constructors. These are generated by the compiler and not within our control
        var intptrMethods = GetInputSystemPublicMethods().Where(m => m.DeclaringType.BaseType?.FullName != "System.MulticastDelegate" && m.Parameters.Any(p => p.ParameterType.FullName == "System.IntPtr"));
        Assert.That(intptrMethods, Is.Empty);
    }

    [Test]
    [Category("API")]
    public void API_DoesNotHaveDisallowedPublicFields()
    {
        var disallowedPublicFields = GetInputSystemPublicFields().Where(field => !field.HasConstant && !(field.IsInitOnly && field.IsStatic) && !IsTypeWhichCanHavePublicFields(field.DeclaringType) && !field.IsSpecialName);
        Assert.That(disallowedPublicFields, Is.Empty);
    }

    internal static bool IgnoreTypeForDocsByName(string fullName)
    {
        return
            fullName == typeof(UnityEngine.InputSystem.UI.TrackedDeviceRaycaster).FullName ||
            fullName == typeof(UnityEngine.InputSystem.Switch.SwitchProControllerHID).FullName ||
#if UNITY_EDITOR_OSX
            fullName == typeof(UnityEngine.InputSystem.XInput.XboxGamepadMacOS).FullName ||
            fullName == typeof(UnityEngine.InputSystem.XInput.XboxOneGampadMacOSWireless).FullName ||
            fullName == typeof(UnityEngine.InputSystem.XInput.XboxGamepadMacOSWireless).FullName ||
#endif
#if UNITY_EDITOR_WIN
            fullName == typeof(UnityEngine.InputSystem.XInput.XInputControllerWindows).FullName ||
#endif
#if UNITY_ENABLE_STEAM_CONTROLLER_SUPPORT
            fullName == typeof(UnityEngine.InputSystem.Steam.ISteamControllerAPI).FullName ||
            fullName == typeof(UnityEngine.InputSystem.Steam.SteamController).FullName ||
            fullName == typeof(UnityEngine.InputSystem.Steam.SteamDigitalActionData).FullName ||
            fullName == typeof(UnityEngine.InputSystem.Steam.SteamAnalogActionData).FullName ||
            fullName == typeof(UnityEngine.InputSystem.Steam.SteamHandle<>).FullName ||
            fullName == typeof(UnityEngine.InputSystem.Steam.Editor.SteamIGAConverter).FullName ||
#endif
            fullName == typeof(UnityEngine.InputSystem.DualShock.DualShock3GamepadHID).FullName ||
            fullName == typeof(UnityEngine.InputSystem.DualShock.DualShock4GamepadHID).FullName ||
            fullName == typeof(UnityEngine.InputSystem.Editor.InputActionCodeGenerator).FullName;
    }

    internal static bool IgnoreTypeForDocsByNamespace(string @namespace)
    {
        return
            // All our XR stuff completely lacks docs. Get XR team to fix this.
            @namespace.StartsWith("UnityEngine.InputSystem.XR") ||
            @namespace.StartsWith("UnityEngine.XR") ||
            @namespace.StartsWith("Unity.XR");
    }

    [Test]
    [Category("API")]
    [TestCase("Keyboard", "Devices/Precompiled/FastKeyboard.cs")]
    [TestCase("Mouse", "Devices/Precompiled/FastMouse.cs")]
    [TestCase("Touchscreen", "Devices/Precompiled/FastTouchscreen.cs")]
    public void API_PrecompiledLayoutsAreUpToDate(string layoutName, string filePath)
    {
        var fullPath = "Packages/com.unity.inputsystem/InputSystem/" + filePath;
        var existingCode = File.ReadAllText(fullPath);

        // May be a git checkout with CRLF auto-conversion on. Strip all '\r' characters.
        existingCode = existingCode.Replace("\r", "");

        // We need to pass it the existing file path to ensure that we respect modifications made to #defines and access modifiers.
        var generatedCode = InputLayoutCodeGenerator.GenerateCodeFileForDeviceLayout(layoutName, fullPath, prefix: "Fast");

        Assert.That(existingCode, Is.EqualTo(generatedCode));
    }

    [Test]
    [Category("API")]
#if UNITY_EDITOR_OSX
    [Explicit] // Fails due to file system permissions on yamato, but works locally.
#endif
    #if UNITY_STANDALONE_LINUX || UNITY_EDITOR_LINUX
    [Ignore("Disabled to make test suite pass on Linux")]
    #endif
    public void API_MonoBehavioursHaveHelpUrls()
    {
        // We exclude abstract MonoBehaviours as these can't show up in the Unity inspector.
        var monoBehaviourTypes = typeof(InputSystem).Assembly.ExportedTypes.Where(t =>
            t.IsPublic && !t.IsAbstract && !IgnoreTypeForDocsByName(t.FullName) && !IgnoreTypeForDocsByNamespace(t.Namespace) &&
            typeof(MonoBehaviour).IsAssignableFrom(t));
        var monoBehaviourTypesHelpUrls =
            monoBehaviourTypes.Where(t => t.GetCustomAttribute<HelpURLAttribute>() != null)
                .Select(t => t.GetCustomAttribute<HelpURLAttribute>().URL);
        var monoBehaviourTypesWithoutHelpUrls =
            monoBehaviourTypes.Where(t => t.GetCustomAttribute<HelpURLAttribute>() == null);

        Assert.That(monoBehaviourTypesWithoutHelpUrls, Is.Empty);
        Assert.That(monoBehaviourTypesHelpUrls, Has.All.StartWith(InputSystem.kDocUrl));
    }

    private const string kAPIDirectory = "Tools/API";

    ////FIXME: The .api-based checks are temporary and don't account for platform-specific APIs. Nuke these tests as soon
    ////       as we can switch back to API validation performed by the Package Validation Suite (as soon as Adriano's fix
    ////       for the access modifier false positive has landed).

    // The .api files are platform-specific so we can only compare on the platform
    // they were built on.
    #if UNITY_EDITOR_WIN

    // We disable "API Verification" tests running as part of the validation suite as they give us
    // false positives (specifically, for setters having changes accessibility from private to protected).
    // Instead, we run our own check here which, instead of comparing to the previous artifact on the
    // package repo (like the validation suite does), we keep a checked-in XML file with the public API
    // that we compare against. This also makes it much easier to run this test locally (rather than
    // having to install and run the package validation suite manually).
    [Test]
    [Category("API")]
    // This is our whitelist for changes to existing APIs that we are fine with. Each exclusion
    // starts with the version number of the API that was changed and then each line lists the API
    // that is whitelisted for a change.
    //
    // NOTE: ATM we do not actually check for the right context of these definitions.
    //
    // The following properties have setters that changed from being private to being protected.
    // This is not a breaking change as no existing code will fail to compile.
    [Property("Exclusions", @"1.0.0
        public Controls.ButtonControl buttonEast { get; }
        public Controls.ButtonControl buttonNorth { get; }
        public Controls.ButtonControl buttonSouth { get; }
        public Controls.ButtonControl buttonWest { get; }
        public Controls.DpadControl dpad { get; }
        public Controls.ButtonControl leftShoulder { get; }
        public Controls.StickControl leftStick { get; }
        public Controls.ButtonControl leftStickButton { get; }
        public Controls.ButtonControl leftTrigger { get; }
        public Controls.ButtonControl rightShoulder { get; }
        public Controls.StickControl rightStick { get; }
        public Controls.ButtonControl rightStickButton { get; }
        public Controls.ButtonControl rightTrigger { get; }
        public Controls.ButtonControl selectButton { get; }
        public Controls.ButtonControl startButton { get; }
        public Controls.Vector2Control hatswitch { get; }
        public Controls.StickControl stick { get; }
        public Controls.ButtonControl trigger { get; }
        public Controls.AxisControl twist { get; }
        public Controls.ButtonControl altKey { get; }
        public Controls.AnyKeyControl anyKey { get; }
        public Controls.ButtonControl ctrlKey { get; }
        public Controls.ButtonControl imeSelected { get; }
        public Controls.ButtonControl shiftKey { get; }
        public Controls.ButtonControl backButton { get; }
        public Controls.IntegerControl clickCount { get; }
        public static Mouse current { get; }
        public Controls.ButtonControl forwardButton { get; }
        public Controls.ButtonControl leftButton { get; }
        public Controls.ButtonControl middleButton { get; }
        public Controls.ButtonControl rightButton { get; }
        public Controls.Vector2Control scroll { get; }
        public Controls.ButtonControl eraser { get; }
        public Controls.ButtonControl firstBarrelButton { get; }
        public Controls.ButtonControl fourthBarrelButton { get; }
        public Controls.ButtonControl inRange { get; }
        public Controls.ButtonControl secondBarrelButton { get; }
        public Controls.ButtonControl thirdBarrelButton { get; }
        public Controls.Vector2Control tilt { get; }
        public Controls.ButtonControl tip { get; }
        public Controls.AxisControl twist { get; }
        public Controls.Vector2Control delta { get; }
        public Controls.Vector2Control position { get; }
        public Controls.ButtonControl press { get; }
        public Controls.AxisControl pressure { get; }
        public Controls.Vector2Control radius { get; }
        public Controls.Vector2Control delta { get; }
        public Controls.ButtonControl indirectTouch { get; }
        public Controls.TouchPhaseControl phase { get; }
        public Controls.Vector2Control position { get; }
        public Controls.TouchPressControl press { get; }
        public Controls.AxisControl pressure { get; }
        public Controls.Vector2Control radius { get; }
        public Controls.Vector2Control startPosition { get; }
        public Controls.DoubleControl startTime { get; }
        public Controls.ButtonControl tap { get; }
        public Controls.IntegerControl tapCount { get; }
        public Controls.IntegerControl touchId { get; }
        public Controls.ButtonControl leftTriggerButton { get; }
        public Controls.ButtonControl playStationButton { get; }
        public Controls.ButtonControl rightTriggerButton { get; }
        public Controls.TouchControl primaryTouch { get; }
        public ButtonControl down { get; }
        public ButtonControl left { get; }
        public ButtonControl right { get; }
        public ButtonControl up { get; }
        public Controls.AxisControl x { get; }
        public Controls.AxisControl y { get; }
        public Controls.AxisControl z { get; }
        public UnityEngine.InputSystem.Controls.ButtonControl L1 { get; }
        public UnityEngine.InputSystem.Controls.ButtonControl L2 { get; }
        public UnityEngine.InputSystem.Controls.ButtonControl L3 { get; }
        public UnityEngine.InputSystem.Controls.ButtonControl optionsButton { get; }
        public UnityEngine.InputSystem.Controls.ButtonControl R1 { get; }
        public UnityEngine.InputSystem.Controls.ButtonControl R2 { get; }
        public UnityEngine.InputSystem.Controls.ButtonControl R3 { get; }
        public UnityEngine.InputSystem.Controls.ButtonControl shareButton { get; }
        public UnityEngine.InputSystem.Controls.ButtonControl touchpadButton { get; }
        public UnityEngine.InputSystem.Utilities.ReadOnlyArray<Controls.TouchControl> touches { get; }
        public System.Collections.Generic.IEnumerator<TValue> GetEnumerator();
        public Vector2Control delta { get; }
        public ButtonControl indirectTouch { get; }
        public TouchPhaseControl phase { get; }
        public Vector2Control position { get; }
        public TouchPressControl press { get; }
        public AxisControl pressure { get; }
        public Vector2Control radius { get; }
        public Vector2Control startPosition { get; }
        public DoubleControl startTime { get; }
        public ButtonControl tap { get; }
        public IntegerControl tapCount { get; }
        public IntegerControl touchId { get; }
        public UnityEngine.InputSystem.Controls.ButtonControl app { get; }
        public UnityEngine.InputSystem.Controls.Vector3Control deviceAcceleration { get; }
        public UnityEngine.InputSystem.Controls.Vector3Control deviceVelocity { get; }
        public UnityEngine.InputSystem.Controls.ButtonControl home { get; }
        public UnityEngine.InputSystem.Controls.ButtonControl recentered { get; }
        public UnityEngine.InputSystem.Controls.ButtonControl recentering { get; }
        public UnityEngine.InputSystem.Controls.Vector2Control touchpad { get; }
        public UnityEngine.InputSystem.Controls.ButtonControl touchpadClicked { get; }
        public UnityEngine.InputSystem.Controls.ButtonControl touchpadTouched { get; }
        public UnityEngine.InputSystem.Controls.ButtonControl volumeDown { get; }
        public UnityEngine.InputSystem.Controls.ButtonControl volumeUp { get; }
        public UnityEngine.InputSystem.Controls.ButtonControl back { get; }
        public UnityEngine.InputSystem.Controls.Vector3Control deviceAngularAcceleration { get; }
        public UnityEngine.InputSystem.Controls.Vector3Control deviceAngularVelocity { get; }
        public UnityEngine.InputSystem.Controls.AxisControl trigger { get; }
        public UnityEngine.InputSystem.Controls.ButtonControl triggerPressed { get; }
        public UnityEngine.InputSystem.Controls.Vector3Control centerEyeAcceleration { get; }
        public UnityEngine.InputSystem.Controls.Vector3Control centerEyeAngularAcceleration { get; }
        public UnityEngine.InputSystem.Controls.Vector3Control centerEyeAngularVelocity { get; }
        public UnityEngine.InputSystem.Controls.Vector3Control leftEyeAcceleration { get; }
        public UnityEngine.InputSystem.Controls.Vector3Control leftEyeAngularAcceleration { get; }
        public UnityEngine.InputSystem.Controls.Vector3Control leftEyeAngularVelocity { get; }
        public UnityEngine.InputSystem.Controls.Vector3Control rightEyeAcceleration { get; }
        public UnityEngine.InputSystem.Controls.Vector3Control rightEyeAngularAcceleration { get; }
        public UnityEngine.InputSystem.Controls.Vector3Control rightEyeAngularVelocity { get; }
        public UnityEngine.InputSystem.Controls.ButtonControl userPresence { get; }
        public UnityEngine.InputSystem.Controls.ButtonControl start { get; }
        public UnityEngine.InputSystem.Controls.AxisControl grip { get; }
        public UnityEngine.InputSystem.Controls.ButtonControl gripPressed { get; }
        public UnityEngine.InputSystem.Controls.ButtonControl primaryButton { get; }
        public UnityEngine.InputSystem.Controls.ButtonControl primaryTouched { get; }
        public UnityEngine.InputSystem.Controls.ButtonControl secondaryButton { get; }
        public UnityEngine.InputSystem.Controls.ButtonControl secondaryTouched { get; }
        public UnityEngine.InputSystem.Controls.Vector2Control thumbstick { get; }
        public UnityEngine.InputSystem.Controls.ButtonControl thumbstickClicked { get; }
        public UnityEngine.InputSystem.Controls.ButtonControl thumbstickTouched { get; }
        public UnityEngine.InputSystem.Controls.AxisControl triggerTouched { get; }
        public UnityEngine.InputSystem.Controls.ButtonControl isTracked { get; }
        public UnityEngine.InputSystem.Controls.IntegerControl trackingState { get; }
        public UnityEngine.InputSystem.Controls.ButtonControl primary { get; }
        public UnityEngine.InputSystem.Controls.ButtonControl trackpadPressed { get; }
        public UnityEngine.InputSystem.Controls.Vector2Control joystick { get; }
        public UnityEngine.InputSystem.Controls.ButtonControl menu { get; }
        public UnityEngine.InputSystem.Controls.ButtonControl touchpadClick { get; }
        public UnityEngine.InputSystem.Controls.ButtonControl touchpadTouch { get; }
        public UnityEngine.InputSystem.Controls.Vector3Control centerEyeVelocity { get; }
        public UnityEngine.InputSystem.Controls.Vector3Control leftEyeVelocity { get; }
        public UnityEngine.InputSystem.Controls.Vector3Control rightEyeVelocity { get; }
        public UnityEngine.InputSystem.Controls.Vector2Control trackpad { get; }
        public UnityEngine.InputSystem.Controls.ButtonControl trackpadTouched { get; }
        public Controls.Vector3Control acceleration { get; }
        public Controls.AxisControl ambientTemperature { get; }
        public Controls.QuaternionControl attitude { get; }
        public Controls.Vector3Control gravity { get; }
        public Controls.Vector3Control angularVelocity { get; }
        public Controls.AxisControl relativeHumidity { get; }
        public Controls.AxisControl lightLevel { get; }
        public Controls.Vector3Control magneticField { get; }
        public Controls.AxisControl atmosphericPressure { get; }
        public Controls.AxisControl distance { get; }
        public Controls.IntegerControl stepCounter { get; }
        public Controls.Vector3Control devicePosition { get; }
        public Controls.QuaternionControl deviceRotation { get; }
        public Controls.ButtonControl isTracked { get; }
        public Controls.IntegerControl trackingState { get; }
        public AxisControl w { get; }
        public AxisControl x { get; }
        public AxisControl y { get; }
        public AxisControl z { get; }
        public UnityEngine.InputSystem.Controls.ButtonControl leftTriggerButton { get; }
        public UnityEngine.InputSystem.Controls.ButtonControl playStationButton { get; }
        public UnityEngine.InputSystem.Controls.ButtonControl rightTriggerButton { get; }
        public UnityEngine.InputSystem.Controls.ButtonControl view { get; }
        public UnityEngine.InputSystem.Controls.IntegerControl parentBoneIndex { get; }
        public UnityEngine.InputSystem.Controls.Vector3Control position { get; }
        public UnityEngine.InputSystem.Controls.QuaternionControl rotation { get; }
        public UnityEngine.InputSystem.Controls.Vector3Control fixationPoint { get; }
        public UnityEngine.InputSystem.Controls.AxisControl leftEyeOpenAmount { get; }
        public UnityEngine.InputSystem.Controls.Vector3Control leftEyePosition { get; }
        public UnityEngine.InputSystem.Controls.QuaternionControl leftEyeRotation { get; }
        public UnityEngine.InputSystem.Controls.AxisControl rightEyeOpenAmount { get; }
        public UnityEngine.InputSystem.Controls.Vector3Control rightEyePosition { get; }
        public UnityEngine.InputSystem.Controls.QuaternionControl rightEyeRotation { get; }
        public UnityEngine.InputSystem.Controls.Vector3Control centerEyePosition { get; }
        public UnityEngine.InputSystem.Controls.QuaternionControl centerEyeRotation { get; }
        public UnityEngine.InputSystem.Controls.ButtonControl airTap { get; }
        public UnityEngine.InputSystem.Controls.Vector3Control sourceLossMitigationDirection { get; }
        public UnityEngine.InputSystem.Controls.AxisControl sourceLossRisk { get; }
        public UnityEngine.InputSystem.Controls.AxisControl batteryLevel { get; }
        public UnityEngine.InputSystem.Controls.ButtonControl joystickClicked { get; }
        public UnityEngine.InputSystem.Controls.Vector3Control pointerPosition { get; }
        public UnityEngine.InputSystem.Controls.QuaternionControl pointerRotation { get; }
    ")]
    // InputActionAsset and InputActionMap changed from IInputActionCollection to IInputActionCollection2 with
    // the latter just being based on the former.
    [Property("Exclusions", @"1.0.0
        public class InputActionAsset : ScriptableObject, IInputActionCollection, System.Collections.Generic.IEnumerable<InputAction>, System.Collections.IEnumerable
        public sealed class InputActionMap : IInputActionCollection, ISerializationCallbackReceiver, System.Collections.Generic.IEnumerable<InputAction>, System.Collections.IEnumerable, System.ICloneable, System.IDisposable
    ")]
    // FindAction is now defined at the IInputActionCollection2 level and thus no longer introduced separately
    // by InputActionMap and InputActionAsset.
    [Property("Exclusions", @"1.0.0
        public InputAction FindAction(string actionNameOrId, bool throwIfNotFound = false);
        public InputAction FindAction(string nameOrId, bool throwIfNotFound = false);
    ")]
    // RemoveAllBindingOverrides(InputActionMap) is now RemoveAllBindingOverrides (IInputActionCollection2).
    [Property("Exclusions", @"1.0.0
        public static void RemoveAllBindingOverrides(this InputActionMap actionMap);
    ")]
    // These methods have gained an extra (optional) parameter.
    [Property("Exclusions", @"1.0.0
        public InputTestFixture.ActionConstraint Canceled(UnityEngine.InputSystem.InputAction action, UnityEngine.InputSystem.InputControl control = default(UnityEngine.InputSystem.InputControl), System.Nullable<double> time = default(System.Nullable<double>), System.Nullable<double> duration = default(System.Nullable<double>));
        public InputTestFixture.ActionConstraint Performed(UnityEngine.InputSystem.InputAction action, UnityEngine.InputSystem.InputControl control = default(UnityEngine.InputSystem.InputControl), System.Nullable<double> time = default(System.Nullable<double>), System.Nullable<double> duration = default(System.Nullable<double>));
        public InputTestFixture.ActionConstraint Started(UnityEngine.InputSystem.InputAction action, UnityEngine.InputSystem.InputControl control = default(UnityEngine.InputSystem.InputControl), System.Nullable<double> time = default(System.Nullable<double>));
        public static InputActionSetupExtensions.BindingSyntax AddBinding(this InputActionMap actionMap, string path, string interactions = default(string), string groups = default(string), string action = default(string));
        public InputActionSetupExtensions.CompositeSyntax With(string name, string binding, string groups = default(string));
        public static void DisableDevice(InputDevice device);
        public InputEventBuffer(Unity.Collections.NativeArray<byte> buffer, int eventCount, int sizeInBytes = -1) {}
        public void AppendEvent(InputEvent* eventPtr, int capacityIncrementInBytes = 2048);
        public InputEvent* AllocateEvent(int sizeInBytes, int capacityIncrementInBytes = 2048);
    ")]
    // TrackedPose Driver changes
    [Property("Exclusions", @"1.0.0
         public class TrackedPoseDriver : MonoBehaviour
    ")]
    // These methods have been superseded and have an Obsolete warning on them.
    [Property("Exclusions", @"1.0.0
        public static bool TryResetDevice(InputDevice device);
    ")]
    // Enum value that was never functional.
    [Property("Exclusions", @"1.0.0
        public const InputDeviceChange Destroyed = 8;
    ")]
    // InputSystem.onEvent has become a property with the Action replaced by the InputEventListener type.
    [Property("Exclusions", @"1.0.0
        public static event System.Action<LowLevel.InputEventPtr, InputDevice> onEvent;
    ")]
    // Mouse and Touchscreen implement internal IEventMerger interface
    [Property("Exclusions", @"1.0.0
        public class Touchscreen : Pointer, LowLevel.IInputStateCallbackReceiver
    ")]
    [ScopedExclusionProperty("1.0.0", "UnityEngine.InputSystem.Editor", "public sealed class InputControlPathEditor : System.IDisposable", "public void OnGUI(Rect rect);")]
    // InputEventTrace.Resize() has a new parameter with a default value.
    [ScopedExclusionProperty("1.0.0", "UnityEngine.InputSystem.LowLevel", "public sealed class InputEventTrace : System.Collections.Generic.IEnumerable<InputEventPtr>, System.Collections.IEnumerable, System.IDisposable", "public bool Resize(long newBufferSize);")]
    // filterNoiseOnCurrent is Obsolete since 1.3.0
    [Property("Exclusions", @"1.0.0
        public bool filterNoiseOnCurrent { get; set; }
    ")]
    // SwitchProControllerHID inherited from IInputStateCallbackReceiver and IEventPreProcessor, both are internal interfaces
    [Property("Exclusions", @"1.0.0
        public class SwitchProControllerHID : Gamepad
    ")]
    // AddChangeMonitor has a new, optional groupIndex argument.
    [Property("Exclusions", @"1.0.0
        public static void AddChangeMonitor(InputControl control, IInputStateChangeMonitor monitor, long monitorIndex = -1);
    ")]
    // DualShock4GamepadHID from IEventPreProcessor, which is an internal interface
    [Property("Exclusions", @"1.0.0
        public class DualShock4GamepadHID : DualShockGamepad
    ")]
    // InputDeviceChange.Destroyed was deprecated and not active anymore
    [Property("Exclusions", @"1.0.0
        Destroyed = 8,
    ")]
    // TestFixture added some arguments over the time
    [Property("Exclusions", @"1.0.0
        public void BeginTouch(int touchId, Vector2 position, bool queueEventOnly = false, Touchscreen screen = default(Touchscreen), double time = -1d, double timeOffset = 0d);
        public InputTestFixture.ActionConstraint Canceled(InputAction action, InputControl control = default(InputControl), System.Nullable<double> time = default(System.Nullable<double>), System.Nullable<double> duration = default(System.Nullable<double>));
        public void EndTouch(int touchId, Vector2 position, Vector2 delta = default(Vector2), bool queueEventOnly = false, Touchscreen screen = default(Touchscreen), double time = -1d, double timeOffset = 0d);
        public InputTestFixture.ActionConstraint Performed(InputAction action, InputControl control = default(InputControl), System.Nullable<double> time = default(System.Nullable<double>), System.Nullable<double> duration = default(System.Nullable<double>));
        public InputTestFixture.ActionConstraint Started(InputAction action, InputControl control = default(InputControl), System.Nullable<double> time = default(System.Nullable<double>));
    ")]
    // Api scraper seems to be unstable with fields with default values, sometimes "= 0;" appears (locally) and sometimes (on CI) doesn't.
    [Property("Exclusions", @"1.0.0
        public int negative = 0;
        public int positive = 0;
        public AxisComposite.WhichSideWins whichSideWins = AxisComposite.WhichSideWins.Neither;
        public int down = 0;
        public int left = 0;
        public int right = 0;
        public int up = 0;
    ")]
    public void API_MinorVersionsHaveNoBreakingChanges()
    {
        var currentVersion = CoreTests.PackageJson.ReadVersion();
        var apiVersions = Directory.GetDirectories(kAPIDirectory)
            .Select(p => new Version(Path.GetFileName(p)))
            .ToList();
        apiVersions.Sort();

        Assert.That(apiVersions, Has.Count.GreaterThanOrEqualTo(1), "Did not find a checked in .api version in " + kAPIDirectory);

        var lastReleasedVersion = apiVersions[apiVersions.Count - 1];
        Assert.That(currentVersion, Is.Not.EqualTo(lastReleasedVersion), "Must bump package version when making changes.");

        var exclusions =
            TestContext.CurrentContext.Test.Properties["Exclusions"].OfType<string>()
                .Where(t => t.StartsWith(lastReleasedVersion.ToString())).SelectMany(t => t.Split(new[] { "\n", "\r\n", "\r" },
                    StringSplitOptions.None)).ToArray();

        var scopedExclusions = TestContext.CurrentContext.Test.Properties[ScopedExclusionPropertyAttribute.ScopedExclusions].OfType<ScopedExclusion>()
            .Where(s => s.Version == lastReleasedVersion.ToString())
            .ToArray();


        if (currentVersion.Major == lastReleasedVersion.Major)
        {
            Unity.Coding.Editor.ApiScraping.ApiScraping.Scrape();

            var currentApiFiles = Directory.GetFiles("Packages/com.unity.inputsystem", "*.api", SearchOption.AllDirectories);
            var lastPublicApiFiles = Directory.GetFiles(Path.Combine(kAPIDirectory, lastReleasedVersion.ToString()), "*.api");

            Assert.That(lastPublicApiFiles.Where(p => !currentApiFiles.Any(x => Path.GetFileName(x) == Path.GetFileName(p))),
                Is.Empty,
                "Any API file existing for the last published release must also exist for the current one.");

            var missingLines = lastPublicApiFiles.SelectMany(p => MissingLines(Path.GetFileName(p), currentApiFiles, lastPublicApiFiles, exclusions, scopedExclusions))
                .ToList();
            Debug.Log(string.Join("\n", missingLines));
            Assert.That(missingLines, Is.Empty);
        }
    }

    private static IEnumerable<string> MissingLines(string apiFile, string[] currentApiFiles, string[] lastPublicApiFiles, string[] exclusions,
        ScopedExclusion[] scopedExclusions)
    {
        var oldApiFile = lastPublicApiFiles.First(p => Path.GetFileName(p) == apiFile);
        var newApiFile = currentApiFiles.First(p => Path.GetFileName(p) == apiFile);

        var oldApiContents = File.ReadAllLines(oldApiFile).Select(FilterIgnoredChanges).ToArray();
        var newApiContents = File.ReadAllLines(newApiFile).Select(FilterIgnoredChanges).ToArray();

        var scopeStack = new List<string>();
        for (var i = 0; i < oldApiContents.Length; i++)
        {
            var line = oldApiContents[i];
            if (line.Trim().StartsWith("{"))
            {
                scopeStack.Add(oldApiContents[i - 1]);
            }
            else if (line.Trim().StartsWith("}"))
            {
                scopeStack.RemoveAt(scopeStack.Count - 1);
            }

            if (!newApiContents.Contains(line) && !exclusions.Any(x => x.Trim() == line.Trim()) && !scopedExclusions.Any(s => s.IsMatch(scopeStack, line)))
                yield return line;
        }
    }

    private static string FilterIgnoredChanges(string line)
    {
        if (line.Length == 0)
            return line;

        var pos = 0;
        while (true)
        {
            // Skip whitespace.
            while (pos < line.Length && char.IsWhiteSpace(line[pos]))
                ++pos;

            if (pos < line.Length && line[pos] != '[')
                return line;

            var startPos = pos;
            ++pos;
            while (pos < line.Length + 1 && !(line[pos] == ']' && line[pos + 1] == ' '))
                ++pos;
            ++pos;

            var length = pos - startPos - 2;
            var attribute = line.Substring(startPos + 1, length);
            if (!attribute.StartsWith("System.Obsolete"))
            {
                line = line.Substring(0, startPos) + line.Substring(pos + 1); // Snip space after ']'.
                pos -= length + 2;
            }
        }
    }

    /// <summary>
    /// Use a scoped exclusion property to exclude members of a type from API verification when the member's names are not
    /// unique in the entire project and you don't want to exclude the unrelated members. This type will scope the exlusion
    /// to just a particular namespace and type.
    /// </summary>
    internal readonly struct ScopedExclusion
    {
        public ScopedExclusion(string version, string ns, string type, params string[] members)
        {
            Version = version;
            Namespace = ns;
            Type = type;
            Members = members;
        }

        public string Version { get; }
        public string Namespace { get; }
        public string Type { get; }
        public string[] Members { get; }

        public bool IsMatch(List<string> scopeStack, string member)
        {
            var namespaceScope = string.Empty;
            var typeScope = string.Empty;

            for (var i = scopeStack.Count - 1; i >= 0; i--)
            {
                if (scopeStack[i].StartsWith("namespace"))
                    namespaceScope = scopeStack[i].Substring(scopeStack[i].IndexOf(' ') + 1);
                else
                    typeScope = scopeStack[i].Trim();
            }

            return namespaceScope == Namespace && typeScope == Type && Members.Contains(member.Trim());
        }
    }

    [AttributeUsage(AttributeTargets.Method, AllowMultiple = true)]
    public class ScopedExclusionPropertyAttribute : PropertyAttribute
    {
        public const string ScopedExclusions = "ScopedExclusions";

        public ScopedExclusionPropertyAttribute(string version, string ns, string type, params string[] method)
        {
            Properties.Add(ScopedExclusions, new ScopedExclusion(version, ns, type, method));
        }
    }

#endif // UNITY_EDITOR_WIN

    [Test]
    [Category("API")]
#if UNITY_EDITOR_LINUX
    [Ignore("Disabled to make test suite pass on Linux")]
#endif
    public void API_DocumentationManualDoesNotHaveMissingOrUnusedImages()
    {
        const string docsPath = "Packages/com.unity.inputsystem/Documentation~/";
        const string imagesPath = "Packages/com.unity.inputsystem/Documentation~/images/";
        var regex = new Regex("\\(.*images\\/(?<filename>[^\\)]*)", RegexOptions.IgnoreCase);

        // Add files here if you want to ignore them being unreferenced.
        var unreferencedIgnoreList = new[] { "InputArchitectureLowLevel.sdxml", "InputArchitectureHighLevel.sdxml", "InteractionsDiagram.sdxml" };

        var missingImages = false;
        var unusedImages = false;
        var messages = new StringBuilder();

        // Record all the files in the images directory.
        var foundImageFiles = Directory.GetFiles(imagesPath);
        var imageFiles = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        foreach (var img in foundImageFiles)
        {
            // Ignore hidden files such as those OSX creates
            if (new FileInfo(img).Attributes.HasFlag(FileAttributes.Hidden))
                continue;

            var name = img.Replace(imagesPath, string.Empty);

            if (unreferencedIgnoreList.Contains(name))
                continue;

            imageFiles[name] = 0;
        }

        // Iterate through all the md doc pages and count the image
        // references and record missing images.
        var docsPages = new List<string>(Directory.GetFiles(docsPath, "*.md"));

        // Add the changelog.
        docsPages.Add("Packages/com.unity.inputsystem/CHANGELOG.md");

        var missingImagesList = new List<string>();
        foreach (var page in docsPages)
        {
            missingImagesList.Clear();
            var contents = File.ReadAllText(page);
            var regexMatches = regex.Matches(contents);

            foreach (Match match in regexMatches)
            {
                var name = match.Groups["filename"].Value;
                if (imageFiles.ContainsKey(name))
                {
                    imageFiles[name]++;
                }
                else
                {
                    missingImagesList.Add(name);
                }
            }

            if (missingImagesList.Count > 0)
            {
                if (!missingImages)
                    messages.AppendLine("Docs contain referenced image files that do not exist:");

                missingImages = true;
                messages.AppendLine("  " + page);
                foreach (var img in missingImagesList)
                    messages.AppendLine($"    {img}");
            }
        }

        foreach (var img in imageFiles.Where(img => img.Value == 0))
        {
            if (!unusedImages)
                messages.AppendLine("Images directory contains image files that are not referenced in any docs. Consider removing them:");

            unusedImages = true;
            messages.AppendLine($"  {img.Key}");
        }

        if (unusedImages || missingImages)
        {
            Assert.Fail(messages.ToString());
        }
    }

    [Test]
    [Category("API")]
    public void API_DefaultInputActionsClassIsUpToDate()
    {
        const string assetFile = "Packages/com.unity.inputsystem/InputSystem/Plugins/PlayerInput/DefaultInputActions.inputactions";
        Assert.That(File.Exists(assetFile), Is.True);

        var actions = new DefaultInputActions();
        var jsonFromActions = actions.asset.ToJson();
        var jsonFromFile = File.ReadAllText(assetFile);

        Assert.That(jsonFromActions.WithAllWhitespaceStripped(), Is.EqualTo(jsonFromFile.WithAllWhitespaceStripped()));
    }
}
#endif
