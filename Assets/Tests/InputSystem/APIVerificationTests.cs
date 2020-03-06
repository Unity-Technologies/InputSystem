#if UNITY_EDITOR
using System;
using System.Linq;
using System.Collections.Generic;
using System.IO;
using NUnit.Framework;
using Mono.Cecil;
using UnityEditor.PackageManager.DocumentationTools.UI;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using HtmlAgilityPack;

class APIVerificationTests
{
    private bool IsValidNameForConstant(string name)
    {
        return char.IsUpper(name[0]);
    }

    private bool TypeHasValidNamespace(TypeReference type)
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
        var codeBase = typeof(InputSystem).Assembly.CodeBase;
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
    [TestCase(typeof(InputControl))]
    [TestCase(typeof(IInputInteraction))]
    [TestCase(typeof(InputBindingComposite))]
    [TestCase(typeof(InputProcessor))]
    public void API_TypesCreatedByReflectionHavePreserveAttribute(Type type)
    {
        var types = type.Assembly.GetTypes().Where(t => type.IsAssignableFrom(t)).Concat(typeof(APIVerificationTests).Assembly.GetTypes().Where(t => type.IsAssignableFrom(t)));
        Assert.That(types, Is.Not.Empty);
        var typesWithoutPreserveAttribute = types.Where(t => !t.CustomAttributes.Any(a => a.AttributeType.Name.Contains("PreserveAttribute")));
        Assert.That(typesWithoutPreserveAttribute, Is.Empty);
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

        // For generic methods, tag ``N onto name.
        if (method.HasGenericParameters)
            methodKey = $"{methodKey}``{method.GenericParameters.Count}";

        // For non-get/set/add/remove methods, we need to take arguments into account
        // to be able to differentiate overloads.
        if (!method.IsGetter && !method.IsSetter && !method.IsAddOn && !method.IsRemoveOn)
        {
            string ConvertTypeName(string typeName)
            {
                // DocFX, at least with our current setup, seems to not be able
                // handle any type references that aren't in the current package
                // or in system libraries. Meaning that a method references anything in
                // UnityEngine.dll, for example, whatever the reference is, it will
                // get truncated to just the type name. So, "UnityEngine.EventSystems.PointerEventData",
                // for example, comes out as just "PointerEventData".

                if (!typeName.StartsWith("UnityEngine.InputSystem.") &&
                    (typeName.StartsWith("UnityEngine.") ||
                     typeName.StartsWith("UnityEditor.") ||
                     typeName.StartsWith("Unity.") ||
                     typeName.StartsWith("System.Linq")))
                {
                    var lastDot = typeName.LastIndexOf('.');
                    return typeName.Substring(lastDot + 1);
                }

                // Nested types in Cecil use '/', in docs we use '.'.
                return typeName.Replace('/', '.');
            }

            string TypeToString(TypeReference type)
            {
                var isByReference = type.IsByReference;
                var isArray = false;

                // If it's a Type& reference, switch to the referenced type.
                if (type is ByReferenceType)
                {
                    isByReference = true;
                    type = type.GetElementType();
                }

                // If it's a Type[] reference, switch to referenced type.
                if (type is ArrayType)
                {
                    isArray = true;
                    type = type.GetElementType();
                }

                // Parameters on generic methods and types are referenced by position,
                // not by name.
                string typeName;
                if (type is GenericParameter genericParameter)
                {
                    ////FIXME: This doesn't work properly. The docs use `` on parameters
                    ////       *coming* from methods and ` on parameters *coming* from types.
                    ////       However, Cecil's GenericParameter is also used for generic
                    ////       *arguments* and there, GenericParameter.Type simply indicates what
                    ////       generic thing the argument is applied to, *not* where it is defined.
                    ////       So something like "Foo<TControl>(InputControlList<TControl>)" will
                    ////       give us GenericParameterType.Type on the use of TControl.
                    ////       I found no way to tell the two apart. I.e. to tell whether the
                    ////       *definition* is coming from a method or type.

                    // Method parameters are referenced with ``, type parameters
                    // with `.
                    var prefix = genericParameter.Type == GenericParameterType.Method
                        ? "``"
                        : "`";
                    typeName = $"{prefix}{genericParameter.Position}";
                }
                else if (type.IsGenericInstance)
                {
                    // Cecil uses `N<...> notation, docs use {...} notation.
                    var genericInstanceType = (GenericInstanceType)type;

                    // Extract name of generic type. Snip off `N suffix.
                    typeName = ConvertTypeName(type.GetElementType().FullName);
                    var indexOfBacktick = typeName.IndexOf('`');
                    if (indexOfBacktick != -1)
                        typeName = typeName.Substring(0, indexOfBacktick);

                    typeName += "{";
                    typeName += string.Join(",", genericInstanceType.GenericArguments.Select(TypeToString));
                    typeName += "}";
                }
                else if (type.HasGenericParameters)
                {
                    // Same deal as IsGenericInstance.

                    typeName = ConvertTypeName(type.FullName);
                    var indexOfBacktick = typeName.IndexOf('`');
                    if (indexOfBacktick != -1)
                        typeName = typeName.Substring(0, indexOfBacktick);

                    typeName += "{";
                    typeName += string.Join(",", type.GenericParameters.Select(TypeToString));
                    typeName += "}";
                }
                else
                {
                    typeName = ConvertTypeName(type.FullName);
                }

                if (isArray)
                    typeName += "[]";

                // Cecil uss &, docs use @ for 'ref' parameters.
                if (isByReference)
                    typeName += "@";

                return typeName;
            }

            var parameters = string.Join(",", method.Parameters.Select(p => TypeToString(p.ParameterType)));
            if (!string.IsNullOrEmpty(parameters))
                methodKey = $"{methodKey}({parameters})";
        }

        const string nextEntryKey = "<a id=";
        const string summaryKey = "<div class=\"markdown level1 summary\">";
        const string endKey = "</div>";

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
            return docs.Substring(summaryIndex + summaryKey.Length, endIndex - (summaryIndex + summaryKey.Length));

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
            type.FullName == typeof(UnityEngine.InputSystem.Switch.SwitchProControllerHID).FullName ||
#if UNITY_EDITOR_OSX
            type.FullName == typeof(UnityEngine.InputSystem.XInput.XboxGamepadMacOS).FullName ||
            type.FullName == typeof(UnityEngine.InputSystem.XInput.XboxOneGampadMacOSWireless).FullName ||
#endif
#if UNITY_EDITOR_WIN
            type.FullName == typeof(UnityEngine.InputSystem.XInput.XInputControllerWindows).FullName ||
#endif
#if UNITY_ENABLE_STEAM_CONTROLLER_SUPPORT
            type.FullName == typeof(UnityEngine.InputSystem.Steam.ISteamControllerAPI).FullName ||
            type.FullName == typeof(UnityEngine.InputSystem.Steam.SteamController).FullName ||
            type.FullName == typeof(UnityEngine.InputSystem.Steam.SteamDigitalActionData).FullName ||
            type.FullName == typeof(UnityEngine.InputSystem.Steam.SteamAnalogActionData).FullName ||
            type.FullName == typeof(UnityEngine.InputSystem.Steam.SteamHandle<>).FullName ||
            type.FullName == typeof(UnityEngine.InputSystem.Steam.Editor.SteamIGAConverter).FullName ||
#endif
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
            type.Namespace.StartsWith("UnityEngine.XR") ||
            type.Namespace.StartsWith("Unity.XR") ||
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
        const string docsFolder = "Temp/docstest";
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

    HtmlDocument LoadHtmlDocument(string htmlFile, Dictionary<string, HtmlDocument> htmlFileCache)
    {
        if (!htmlFileCache.ContainsKey(htmlFile))
        {
            htmlFileCache[htmlFile] = new HtmlDocument();
            htmlFileCache[htmlFile].Load(htmlFile);
        }

        return htmlFileCache[htmlFile];
    }

    void CheckHTMLFileLinkConsistency(string htmlFile, List<string> unresolvedLinks, Dictionary<string, HtmlDocument> htmlFileCache)
    {
        var dir = Path.GetDirectoryName(htmlFile);
        var doc = LoadHtmlDocument(htmlFile, htmlFileCache);
        var hrefList = doc.DocumentNode.SelectNodes("//a")
            .Select(p => p.GetAttributeValue("href", null))
            .ToList();
        foreach (var _link in hrefList)
        {
            var link = _link;
            if (string.IsNullOrEmpty(link))
                continue;

            // ignore external links for now
            if (link.StartsWith("http://"))
                continue;

            if (link.StartsWith("https://"))
                continue;

            if (link == "#top")
                continue;

            if (link.StartsWith("#"))
                link = Path.GetFileName(htmlFile) + link;

            var split = link.Split('#');
            var linkedFile = split[0];
            var tag = split.Length > 1 ? split[1] : null;

            if (!File.Exists(Path.Combine(dir, linkedFile)))
            {
                unresolvedLinks.Add($"{link} in {htmlFile} (File Not Found)");
                continue;
            }

            if (!string.IsNullOrEmpty(tag))
            {
                var linkedDoc = LoadHtmlDocument(Path.Combine(dir, linkedFile), htmlFileCache);
                var idNode = linkedDoc.DocumentNode.SelectSingleNode($"//*[@id = '{tag}']");
                if (idNode == null)
                    unresolvedLinks.Add($"{link} in {htmlFile} (Tag Not Found)");
            }
        }
    }

    ////TODO: add verification of *online* links to this; probably prone to instability and maybe they shouldn't fail tests but would
    ////      be great to have some way of diagnosing links that have gone stale
    [Test]
    [Category("API")]
#if UNITY_EDITOR_OSX
    [Explicit] // Fails due to file system permissions on yamato, but works locally.
#endif
    public void API_DocumentationManualDoesNotHaveMissingInternalLinks()
    {
        var docsFolder = GenerateDocsDirectory();
        var unresolvedLinks = new List<string>();
        var htmlFileCache = new Dictionary<string, HtmlDocument>();
        foreach (var htmlFile in Directory.EnumerateFiles(Path.Combine(docsFolder, "manual")))
            CheckHTMLFileLinkConsistency(htmlFile, unresolvedLinks, htmlFileCache);
        Assert.That(unresolvedLinks, Is.Empty);
    }
}
#endif
