#if UNITY_EDITOR
using System;
using System.Linq;
using System.Collections.Generic;
using System.IO;
using NUnit.Framework;
using Mono.Cecil;
using UnityEditor.PackageManager.DocumentationTools.UI;
using UnityEngine.InputSystem;

class APIVerificationTests
{
    private bool IsValidNameForConstant(string name)
    {
        return char.IsUpper(name[0]);
    }

    private bool TypeHasValidNamespace(TypeReference type)
    {
        // The compiler generates a <Module> type which we want to ignore
        return type.Namespace.StartsWith("UnityEngine.InputSystem") || type.Name == "<Module>";
    }

    // Generally, public API should always expose values as properties, and not as fields.
    // We currently have quite a few exceptions, which are handled here.
    private bool IsTypeWhichCanHavePublicFields(TypeReference type)
    {
        if (type == null)
            return false;

        // This is the base type of all structs
        if (type.FullName == typeof(ValueType).FullName)
            return false;
        if (type.FullName == typeof(Object).FullName)
            return false;

        if (
            // These have fields popuplated by reflection in the Input System
            type.FullName == typeof(UnityEngine.InputSystem.InputProcessor).FullName ||
            type.FullName == typeof(UnityEngine.InputSystem.InputControl).FullName ||
            type.FullName == typeof(UnityEngine.InputSystem.InputBindingComposite).FullName
        )
            return true;

        try
        {
            var resolved = type.Resolve();

            if (resolved == null)
                return false;

            if (
                // Interactions have fields populated by reflection in the Input System
                resolved.Interfaces.Any(i => i.InterfaceType.FullName == typeof(UnityEngine.InputSystem.IInputInteraction).FullName) ||

                // Input state structures use fields for the memory layout and construct Input Controls from the fields.
                resolved.Interfaces.Any(i => i.InterfaceType.FullName == typeof(UnityEngine.InputSystem.IInputStateTypeInfo).FullName) ||

                // These use fields for the explicit memory layout, and have a member for the base type. If we exposed that via a property,
                // base type values could not be written individually.
                resolved.Interfaces.Any(i => i.InterfaceType.FullName == typeof(UnityEngine.InputSystem.LowLevel.IInputDeviceCommandInfo).FullName) ||
                resolved.Interfaces.Any(i => i.InterfaceType.FullName == typeof(UnityEngine.InputSystem.LowLevel.IInputEventTypeInfo).FullName) ||

                // serializable types may depend on the field names to match serialized data (eg. Json)
                resolved.Attributes.HasFlag(TypeAttributes.Serializable)
            )
                return true;

            return IsTypeWhichCanHavePublicFields(resolved.BaseType);
        }
        catch (AssemblyResolutionException)
        {
            return false;
        }
    }

    private IEnumerable<TypeDefinition> GetInputSystemPublicTypes()
    {
        var codeBase = typeof(UnityEngine.InputSystem.InputSystem).Assembly.CodeBase;
        var uri = new UriBuilder(codeBase);
        var path = Uri.UnescapeDataString(uri.Path);
        var asmDef = AssemblyDefinition.ReadAssembly(path);
        return asmDef.MainModule.Types.Where(type => type.IsPublic);
    }

    private IEnumerable<FieldDefinition> GetInputSystemPublicFields() => GetInputSystemPublicTypes().SelectMany(t => t.Resolve().Fields).Where(f => f.IsPublic);
    private IEnumerable<MethodDefinition> GetInputSystemPublicMethods() => GetInputSystemPublicTypes().SelectMany(t => t.Resolve().Methods).Where(m => m.IsPublic);

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
        var disallowedPublicFields = GetInputSystemPublicFields().Where(field => !field.HasConstant && !(field.IsInitOnly && field.IsStatic) && !IsTypeWhichCanHavePublicFields(field.DeclaringType));
        Assert.That(disallowedPublicFields, Is.Empty);
    }

    string DocsForType(TypeDefinition type, string docsFolder)
    {
        var typeName = type.ToString().Replace('`', '-');
        var docsPath = $"{docsFolder}/api/{typeName}.html";
        if (!File.Exists(docsPath))
            return null;
        return File.ReadAllText(docsPath);
    }

    string TypeSummary(TypeDefinition type, string docsFolder)
    {
        var docs = DocsForType(type, docsFolder);
        if (docs == null)
            return null;
        var summaryKey = "<div class=\"markdown level0 summary\">";
        var endKey = "</div>";
        var summaryIndex = docs.IndexOf(summaryKey);
        var endIndex = docs.IndexOf(endKey, summaryIndex);
        if (summaryIndex != -1 && endIndex != -1)
            return docs.Substring(summaryIndex + summaryKey.Length, endIndex - (summaryIndex + summaryKey.Length));
        return null;
    }

    string MethodSummary(MethodDefinition method, string docsFolder)
    {
        var docs = DocsForType(method.DeclaringType, docsFolder);
        if (docs == null)
            return null;
        var methodName = method.Name;
        if (method.IsGetter || method.IsSetter || method.IsAddOn)
            methodName = methodName.Substring(4);
        if (method.IsRemoveOn)
            methodName = methodName.Substring(7);
        if (method.IsConstructor)
            methodName = "#ctor";

        var methodKey = $"data-uid=\"{method.DeclaringType}.{methodName}";
        var nextEntryKey = "<a id=";
        var summaryKey = "<div class=\"markdown level1 summary\">";
        var endKey = "</div>";
        var methodIndex = docs.IndexOf(methodKey);
        if (methodIndex == -1)
        {
            Console.WriteLine($"Could not find {methodKey}");
            return null;
        }

        var summaryIndex = docs.IndexOf(summaryKey, methodIndex);
        var endIndex = docs.IndexOf(endKey, summaryIndex);
        var nextEntryIndex = docs.IndexOf(nextEntryKey, methodIndex);
        if (summaryIndex != -1 && endIndex != -1 && (summaryIndex < nextEntryIndex || nextEntryIndex == -1))
        {
            Console.WriteLine($"summary for {method}:{docs.Substring(summaryIndex + summaryKey.Length, endIndex - (summaryIndex + summaryKey.Length))}");

            return docs.Substring(summaryIndex + summaryKey.Length, endIndex - (summaryIndex + summaryKey.Length));
        }

        return null;
    }

    bool IgnoreTypeForDocs(TypeDefinition type)
    {
        return
            // Currently, the package documentation system is broken as it will not generate docs for any code contained
            // in #ifdef blocks. Since the input system has a lot of platform specific code, that means that all this code
            // is currently without docs. I'm talking to the package docs team to find a fix for this. Until then, we need
            // to ignore any public API inside ifdefs for docs checks.
            type.FullName == typeof(UnityEngine.InputSystem.UI.TrackedDeviceRaycaster).FullName ||
            type.FullName == typeof(UnityEngine.InputSystem.WebGL.WebGLGamepad).FullName ||
            type.FullName == typeof(UnityEngine.InputSystem.WebGL.WebGLJoystick).FullName ||
            type.FullName == typeof(UnityEngine.InputSystem.Switch.NPad).FullName ||
            type.FullName == typeof(UnityEngine.InputSystem.Switch.SwitchProControllerHID).FullName ||
            type.FullName == typeof(UnityEngine.InputSystem.XInput.XboxOneGamepad).FullName ||
#if UNITY_EDITOR_OSX
            type.FullName == typeof(UnityEngine.InputSystem.XInput.XboxGamepadMacOS).FullName ||
            type.FullName == typeof(UnityEngine.InputSystem.XInput.XboxOneGampadMacOSWireless).FullName ||
#endif
#if UNITY_EDITOR_WIN
            type.FullName == typeof(UnityEngine.InputSystem.XInput.XInputControllerWindows).FullName ||
#endif
            type.FullName == typeof(UnityEngine.InputSystem.Steam.ISteamControllerAPI).FullName ||
            type.FullName == typeof(UnityEngine.InputSystem.Steam.SteamController).FullName ||
            type.FullName == typeof(UnityEngine.InputSystem.Steam.SteamDigitalActionData).FullName ||
            type.FullName == typeof(UnityEngine.InputSystem.Steam.SteamAnalogActionData).FullName ||
            type.FullName == typeof(UnityEngine.InputSystem.Steam.SteamHandle<>).FullName ||
            type.FullName == typeof(UnityEngine.InputSystem.Steam.Editor.SteamIGAConverter).FullName ||
            type.FullName == typeof(UnityEngine.InputSystem.PS4.PS4TouchControl).FullName ||
            type.FullName == typeof(UnityEngine.InputSystem.PS4.DualShockGamepadPS4).FullName ||
            type.FullName == typeof(UnityEngine.InputSystem.PS4.MoveControllerPS4).FullName ||
            type.FullName == typeof(UnityEngine.InputSystem.PS4.LowLevel.PS4Touch).FullName ||
            type.FullName == typeof(UnityEngine.InputSystem.iOS.iOSGameController).FullName ||
            type.FullName == typeof(UnityEngine.InputSystem.DualShock.DualShock3GamepadHID).FullName ||
            type.FullName == typeof(UnityEngine.InputSystem.DualShock.DualShock4GamepadHID).FullName ||
            type.FullName == typeof(UnityEngine.InputSystem.Android.AndroidAccelerometer).FullName ||
            type.FullName == typeof(UnityEngine.InputSystem.Android.AndroidGamepad).FullName ||
            type.FullName == typeof(UnityEngine.InputSystem.Android.AndroidGyroscope).FullName ||
            type.FullName == typeof(UnityEngine.InputSystem.Android.AndroidJoystick).FullName ||
            type.FullName == typeof(UnityEngine.InputSystem.Android.AndroidProximity).FullName ||
            type.FullName == typeof(UnityEngine.InputSystem.Android.AndroidAmbientTemperature).FullName ||
            type.FullName == typeof(UnityEngine.InputSystem.Android.AndroidGravitySensor).FullName ||
            type.FullName == typeof(UnityEngine.InputSystem.Android.AndroidLightSensor).FullName ||
            type.FullName == typeof(UnityEngine.InputSystem.Android.AndroidPressureSensor).FullName ||
            type.FullName == typeof(UnityEngine.InputSystem.Android.AndroidMagneticFieldSensor).FullName ||
            type.FullName == typeof(UnityEngine.InputSystem.Android.AndroidLinearAccelerationSensor).FullName ||
            type.FullName == typeof(UnityEngine.InputSystem.Android.AndroidRelativeHumidity).FullName ||
            type.FullName == typeof(UnityEngine.InputSystem.Android.AndroidRotationVector).FullName ||
            type.FullName == typeof(UnityEngine.InputSystem.Android.AndroidStepCounter).FullName ||
            ////REVIEW: why are the ones in the .Editor namespace being filtered out by the docs generator?
            type.FullName == typeof(UnityEngine.InputSystem.Editor.InputActionCodeGenerator).FullName ||
            type.FullName == typeof(UnityEngine.InputSystem.Editor.InputControlPathEditor).FullName ||
            type.FullName == typeof(UnityEngine.InputSystem.Editor.InputControlPicker).FullName ||
            type.FullName == typeof(UnityEngine.InputSystem.Editor.InputControlPickerState).FullName ||
            type.FullName == typeof(UnityEngine.InputSystem.Editor.InputEditorUserSettings).FullName ||
            type.FullName == typeof(UnityEngine.InputSystem.Editor.InputParameterEditor).FullName ||
            type.FullName == typeof(UnityEngine.InputSystem.Editor.InputParameterEditor<>).FullName ||
            type.FullName == typeof(UnityEngine.InputSystem.Processors.EditorWindowSpaceProcessor).FullName ||
            // All our XR stuff completely lacks docs. Get XR team to fix this.
            type.Namespace.StartsWith("UnityEngine.InputSystem.XR") ||
            false;
    }

    bool IgnoreMethodForDocs(MethodDefinition method)
    {
        if (IgnoreTypeForDocs(method.DeclaringType))
            return true;

        // Default constructors may be implicit in which case they don't need docs.
        if (method.IsConstructor && !method.HasParameters)
            return true;

        // delegate members are implicit and don't need docs.
        if (method.DeclaringType.Name.EndsWith("Delegate"))
            return true;

        return false;
    }

    string GenerateDocsDirectory()
    {
        var docsFolder = "Temp/docstest";
        Directory.CreateDirectory(docsFolder);
        Documentation.Instance.Generate("com.unity.inputsystem", InputSystem.version.ToString(), docsFolder);
        return docsFolder;
    }

    [Test]
    [Category("API")]
#if UNITY_EDITOR_OSX
    [Explicit] // Fails due to file system permissions on yamato, but works locally.
#endif
    public void API_DoesNotHaveUndocumentedPublicTypes()
    {
        var docsFolder = GenerateDocsDirectory();
        var undocumentedTypes = GetInputSystemPublicTypes().Where(type => !IgnoreTypeForDocs(type) && string.IsNullOrEmpty(TypeSummary(type, docsFolder)));
        Assert.That(undocumentedTypes, Is.Empty, $"Got {undocumentedTypes.Count()} undocumented types.");
    }

    [Test]
    [Category("API")]
    [Ignore("Still needs a lot of documentation work to happen")]
    public void API_DoesNotHaveUndocumentedPublicMethods()
    {
        var docsFolder = GenerateDocsDirectory();
        var undocumentedMethods = GetInputSystemPublicMethods().Where(m =>  !IgnoreMethodForDocs(m) && string.IsNullOrEmpty(MethodSummary(m, docsFolder)));
        Assert.That(undocumentedMethods, Is.Empty, $"Got {undocumentedMethods.Count()} undocumented methods.");
    }
}
#endif
