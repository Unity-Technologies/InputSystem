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
#if HAVE_DOCTOOLS_INSTALLED
using UnityEditor.PackageManager.DocumentationTools.UI;
#endif
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using HtmlAgilityPack;
using UnityEditor;
using UnityEngine.InputSystem.DualShock;
using UnityEngine.InputSystem.Editor;
using UnityEngine;
using UnityEngine.InputSystem.iOS.LowLevel;
using UnityEngine.InputSystem.Utilities;
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

    private static IEnumerable<TypeDefinition> GetInputSystemPublicTypes()
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
        var typesWithoutPreserveAttribute =
            types.Where(t => !t.CustomAttributes.Any(a => a.AttributeType.Name.Contains("PreserveAttribute")))
                .Where(t => !IgnoreTypeWithoutPreserveAttribute(t));
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
    #if !HAVE_DOCTOOLS_INSTALLED
    [Ignore("Must install com.unity.package-manager-doctools package to be able to run this test")]
    #endif
    public void API_DoesNotHaveDisallowedPublicFields()
    {
        #if HAVE_DOCTOOLS_INSTALLED
        var disallowedPublicFields = GetInputSystemPublicFields().Where(field => !field.HasConstant && !(field.IsInitOnly && field.IsStatic) && !IsTypeWhichCanHavePublicFields(field.DeclaringType));
        Assert.That(disallowedPublicFields, Is.Empty);
        #endif
    }

    private static string DocsForType(TypeDefinition type, string docsFolder)
    {
        var typeName = type.ToString().Replace('`', '-');
        var docsPath = $"{docsFolder}/api/{typeName}.html";
        if (!File.Exists(docsPath))
            return null;
        return File.ReadAllText(docsPath);
    }

    private static string TypeSummary(TypeDefinition type, string docsFolder)
    {
        var docs = DocsForType(type, docsFolder);
        if (docs == null)
            return null;
        const string summaryKey = "<div class=\"markdown level0 summary\">";
        const string endKey = "</div>";
        var summaryIndex = docs.IndexOf(summaryKey);
        var endIndex = docs.IndexOf(endKey, summaryIndex);
        if (summaryIndex != -1 && endIndex != -1)
            return docs.Substring(summaryIndex + summaryKey.Length, endIndex - (summaryIndex + summaryKey.Length));
        return null;
    }

    private static string MethodSummary(MethodDefinition method, string docsFolder)
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

    private static bool IgnoreTypeForDocsByName(string fullName)
    {
        return
            fullName == typeof(UnityEngine.InputSystem.UI.TrackedDeviceRaycaster).FullName ||
            fullName == typeof(UnityEngine.InputSystem.Switch.SwitchProControllerHID).FullName ||
#if UNITY_EDITOR_OSX
            fullName == typeof(UnityEngine.InputSystem.XInput.XboxGamepadMacOS).FullName ||
            fullName == typeof(UnityEngine.InputSystem.XInput.XboxOneGampadMacOSWireless).FullName ||
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

    private static bool IgnoreTypeForDocsByNamespace(string @namespace)
    {
        return
            // All our XR stuff completely lacks docs. Get XR team to fix this.
            @namespace.StartsWith("UnityEngine.InputSystem.XR") ||
            @namespace.StartsWith("UnityEngine.XR") ||
            @namespace.StartsWith("Unity.XR");
    }

    private static bool IgnoreTypeForDocs(TypeDefinition type)
    {
        return IgnoreTypeForDocsByName(type.FullName) || IgnoreTypeForDocsByNamespace(type.Namespace);
    }

    private static bool IgnoreMethodForDocs(MethodDefinition method)
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

    private bool IgnoreTypeWithoutPreserveAttribute(Type type)
    {
        // Precompiled layouts are not created through reflection and thus don't need [Preserve].
        if (type == typeof(FastKeyboard)
            || type == typeof(FastMouse)
            || type == typeof(FastTouchscreen)
            || type == typeof(FastDualShock4GamepadHID)
#if UNITY_EDITOR || UNITY_IOS || UNITY_TVOS
            // iOS Step Counter is created from C# code
            || type == typeof(iOSStepCounter)
#endif
        )
            return true;

        return false;
    }

    #if HAVE_DOCTOOLS_INSTALLED
    ////TODO: move this to a fixture setup so that it runs *once* for all API checks in a test run
    private static string GenerateDocsDirectory(out string log)
    {
        // DocumentationBuilder users C:/temp on Windows to avoid deeply nested paths that go
        // beyond the Windows path limit. However, on Yamato agent, C:/temp does not exist.
        // Create it manually here.
        #if UNITY_EDITOR_WIN
        Directory.CreateDirectory("C:/temp");
        #endif
        var docsFolder = Path.GetFullPath(Path.Combine(Application.dataPath, "../Temp/docstest"));
        Directory.CreateDirectory(docsFolder);
        var inputSystemPackageInfo = UnityEditor.PackageManager.PackageInfo.FindForAssetPath("Packages/com.unity.inputsystem");
        var(buildLog, folderName) = Documentation.Instance.GenerateEx(inputSystemPackageInfo, InputSystem.version.ToString(), docsFolder);
        log = buildLog;
        return Path.Combine(docsFolder, folderName);
    }

    #endif

    [Test]
    [Category("API")]
    #if UNITY_EDITOR_OSX
    [Explicit] // Fails due to file system permissions on yamato, but works locally.
    #endif
    #if !HAVE_DOCTOOLS_INSTALLED
    [Ignore("Must install com.unity.package-manager-doctools package to be able to run this test")]
    #endif
    public void API_DoesNotHaveUndocumentedPublicTypes()
    {
        #if HAVE_DOCTOOLS_INSTALLED
        var docsFolder = GenerateDocsDirectory(out _);
        var undocumentedTypes = GetInputSystemPublicTypes().Where(type => !IgnoreTypeForDocs(type) && string.IsNullOrEmpty(TypeSummary(type, docsFolder)));
        Assert.That(undocumentedTypes, Is.Empty, $"Got {undocumentedTypes.Count()} undocumented types, the docs are generated in {docsFolder}");
        #endif
    }

    [Test]
    [Category("API")]
    #if UNITY_EDITOR_OSX
    [Explicit] // Fails due to file system permissions on yamato, but works locally.
    #endif
    #if !HAVE_DOCTOOLS_INSTALLED
    [Ignore("Must install com.unity.package-manager-doctools package to be able to run this test")]
    #endif
    public void API_DocsDoNotHaveXMLDocErrors()
    {
        #if HAVE_DOCTOOLS_INSTALLED
        GenerateDocsDirectory(out var log);
        var lines = log.Split('\n');
        Assert.That(lines.Where(l => l.Contains("Badly formed XML")), Is.Empty);
        Assert.That(lines.Where(l => l.Contains("Invalid cref")), Is.Empty);
        #endif
    }

    [Test]
    [Category("API")]
    [Ignore("Still needs a lot of documentation work to happen")]
    #if !HAVE_DOCTOOLS_INSTALLED
    //[Ignore("Must install com.unity.package-manager-doctools package to be able to run this test")]
    #endif
    public void API_DoesNotHaveUndocumentedPublicMethods()
    {
        #if HAVE_DOCTOOLS_INSTALLED
        var docsFolder = GenerateDocsDirectory(out _);
        var undocumentedMethods = GetInputSystemPublicMethods().Where(m =>  !IgnoreMethodForDocs(m) && string.IsNullOrEmpty(MethodSummary(m, docsFolder)));
        Assert.That(undocumentedMethods, Is.Empty, $"Got {undocumentedMethods.Count()} undocumented methods.");
        #endif
    }

    private static HtmlDocument LoadHtmlDocument(string htmlFile, Dictionary<string, HtmlDocument> htmlFileCache)
    {
        if (!htmlFileCache.ContainsKey(htmlFile))
        {
            htmlFileCache[htmlFile] = new HtmlDocument();
            htmlFileCache[htmlFile].Load(htmlFile);
        }

        return htmlFileCache[htmlFile];
    }

    private static void CheckHTMLFileLinkConsistency(string htmlFile, List<string> unresolvedLinks, Dictionary<string, HtmlDocument> htmlFileCache)
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

    [Test]
    [Category("API")]
    [TestCase("Keyboard", "Devices/Precompiled/FastKeyboard.cs")]
    [TestCase("Mouse", "Devices/Precompiled/FastMouse.cs")]
    [TestCase("Touchscreen", "Devices/Precompiled/FastTouchscreen.cs")]
    [TestCase("DualShock4GamepadHID", "Plugins/DualShock/FastDualShock4GamepadHID.cs")]
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

        #if HAVE_DOCTOOLS_INSTALLED
        // Ensure the links are actually valid.
        var docsFolder = GenerateDocsDirectory(out _);
        var brokenHelpUrls =
            monoBehaviourTypesHelpUrls.Where(
                s =>
                {
                    // Parse file path and anchor.
                    var path = s.Substring(InputSystem.kDocUrl.Length);
                    if (path.StartsWith("/"))
                        path = path.Substring(1);
                    var docsFileName = path.Substring(0, path.IndexOf('#'));
                    var anchorName = path.Substring(path.IndexOf('#') + 1);

                    // Load doc.
                    var docsFilePath = Path.Combine(docsFolder, docsFileName);
                    var doc = new HtmlDocument();
                    doc.Load(docsFilePath);

                    // Look up anchor.
                    return doc.DocumentNode.SelectSingleNode($"//*[@id = '{anchorName}']") == null;
                });

        Assert.That(brokenHelpUrls, Is.Empty);
        #endif
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
        public UnityEngine.InputSystem.Controls.ButtonControl buttonEast { get; }
        public UnityEngine.InputSystem.Controls.ButtonControl buttonNorth { get; }
        public UnityEngine.InputSystem.Controls.ButtonControl buttonSouth { get; }
        public UnityEngine.InputSystem.Controls.ButtonControl buttonWest { get; }
        public UnityEngine.InputSystem.Controls.DpadControl dpad { get; }
        public UnityEngine.InputSystem.Controls.ButtonControl leftShoulder { get; }
        public UnityEngine.InputSystem.Controls.StickControl leftStick { get; }
        public UnityEngine.InputSystem.Controls.ButtonControl leftStickButton { get; }
        public UnityEngine.InputSystem.Controls.ButtonControl leftTrigger { get; }
        public UnityEngine.InputSystem.Controls.ButtonControl rightShoulder { get; }
        public UnityEngine.InputSystem.Controls.StickControl rightStick { get; }
        public UnityEngine.InputSystem.Controls.ButtonControl rightStickButton { get; }
        public UnityEngine.InputSystem.Controls.ButtonControl rightTrigger { get; }
        public UnityEngine.InputSystem.Controls.ButtonControl selectButton { get; }
        public UnityEngine.InputSystem.Controls.ButtonControl startButton { get; }
        public UnityEngine.InputSystem.Controls.Vector2Control hatswitch { get; }
        public UnityEngine.InputSystem.Controls.StickControl stick { get; }
        public UnityEngine.InputSystem.Controls.ButtonControl trigger { get; }
        public UnityEngine.InputSystem.Controls.AxisControl twist { get; }
        public UnityEngine.InputSystem.Controls.ButtonControl altKey { get; }
        public UnityEngine.InputSystem.Controls.AnyKeyControl anyKey { get; }
        public UnityEngine.InputSystem.Controls.ButtonControl ctrlKey { get; }
        public UnityEngine.InputSystem.Controls.ButtonControl imeSelected { get; }
        public UnityEngine.InputSystem.Controls.ButtonControl shiftKey { get; }
        public UnityEngine.InputSystem.Controls.ButtonControl backButton { get; }
        public UnityEngine.InputSystem.Controls.IntegerControl clickCount { get; }
        public static UnityEngine.InputSystem.Mouse current { get; }
        public UnityEngine.InputSystem.Controls.ButtonControl forwardButton { get; }
        public UnityEngine.InputSystem.Controls.ButtonControl leftButton { get; }
        public UnityEngine.InputSystem.Controls.ButtonControl middleButton { get; }
        public UnityEngine.InputSystem.Controls.ButtonControl rightButton { get; }
        public UnityEngine.InputSystem.Controls.Vector2Control scroll { get; }
        public UnityEngine.InputSystem.Controls.ButtonControl eraser { get; }
        public UnityEngine.InputSystem.Controls.ButtonControl firstBarrelButton { get; }
        public UnityEngine.InputSystem.Controls.ButtonControl fourthBarrelButton { get; }
        public UnityEngine.InputSystem.Controls.ButtonControl inRange { get; }
        public UnityEngine.InputSystem.Controls.ButtonControl secondBarrelButton { get; }
        public UnityEngine.InputSystem.Controls.ButtonControl thirdBarrelButton { get; }
        public UnityEngine.InputSystem.Controls.Vector2Control tilt { get; }
        public UnityEngine.InputSystem.Controls.ButtonControl tip { get; }
        public UnityEngine.InputSystem.Controls.AxisControl twist { get; }
        public UnityEngine.InputSystem.Controls.Vector2Control delta { get; }
        public UnityEngine.InputSystem.Controls.Vector2Control position { get; }
        public UnityEngine.InputSystem.Controls.ButtonControl press { get; }
        public UnityEngine.InputSystem.Controls.AxisControl pressure { get; }
        public UnityEngine.InputSystem.Controls.Vector2Control radius { get; }
        public UnityEngine.InputSystem.Controls.Vector2Control delta { get; }
        public UnityEngine.InputSystem.Controls.ButtonControl indirectTouch { get; }
        public UnityEngine.InputSystem.Controls.TouchPhaseControl phase { get; }
        public UnityEngine.InputSystem.Controls.Vector2Control position { get; }
        public UnityEngine.InputSystem.Controls.TouchPressControl press { get; }
        public UnityEngine.InputSystem.Controls.AxisControl pressure { get; }
        public UnityEngine.InputSystem.Controls.Vector2Control radius { get; }
        public UnityEngine.InputSystem.Controls.Vector2Control startPosition { get; }
        public UnityEngine.InputSystem.Controls.DoubleControl startTime { get; }
        public UnityEngine.InputSystem.Controls.ButtonControl tap { get; }
        public UnityEngine.InputSystem.Controls.IntegerControl tapCount { get; }
        public UnityEngine.InputSystem.Controls.IntegerControl touchId { get; }
        public UnityEngine.InputSystem.Controls.ButtonControl leftTriggerButton { get; }
        public UnityEngine.InputSystem.Controls.ButtonControl playStationButton { get; }
        public UnityEngine.InputSystem.Controls.ButtonControl rightTriggerButton { get; }
        public UnityEngine.InputSystem.Controls.TouchControl primaryTouch { get; }
        public UnityEngine.InputSystem.Controls.ButtonControl down { get; }
        public UnityEngine.InputSystem.Controls.ButtonControl left { get; }
        public UnityEngine.InputSystem.Controls.ButtonControl right { get; }
        public UnityEngine.InputSystem.Controls.ButtonControl up { get; }
        public UnityEngine.InputSystem.Controls.AxisControl x { get; }
        public UnityEngine.InputSystem.Controls.AxisControl y { get; }
        public UnityEngine.InputSystem.Controls.AxisControl z { get; }
        public UnityEngine.InputSystem.Controls.ButtonControl L1 { get; }
        public UnityEngine.InputSystem.Controls.ButtonControl L2 { get; }
        public UnityEngine.InputSystem.Controls.ButtonControl L3 { get; }
        public UnityEngine.InputSystem.Controls.ButtonControl optionsButton { get; }
        public UnityEngine.InputSystem.Controls.ButtonControl R1 { get; }
        public UnityEngine.InputSystem.Controls.ButtonControl R2 { get; }
        public UnityEngine.InputSystem.Controls.ButtonControl R3 { get; }
        public UnityEngine.InputSystem.Controls.ButtonControl shareButton { get; }
        public UnityEngine.InputSystem.Controls.ButtonControl touchpadButton { get; }
        public UnityEngine.InputSystem.Utilities.ReadOnlyArray<UnityEngine.InputSystem.Controls.TouchControl> touches { get; }
        public virtual System.Collections.Generic.IEnumerator<TValue> GetEnumerator();
    ")]
    // InputActionAsset and InputActionMap changed from IInputActionCollection to IInputActionCollection2 with
    // the latter just being based on the former.
    [Property("Exclusions", @"1.0.0
        public class InputActionAsset : UnityEngine.ScriptableObject, System.Collections.Generic.IEnumerable<UnityEngine.InputSystem.InputAction>, System.Collections.IEnumerable, UnityEngine.InputSystem.IInputActionCollection
        public sealed class InputActionMap : System.Collections.Generic.IEnumerable<UnityEngine.InputSystem.InputAction>, System.Collections.IEnumerable, System.ICloneable, System.IDisposable, UnityEngine.InputSystem.IInputActionCollection, UnityEngine.ISerializationCallbackReceiver
    ")]
    // FindAction is now defined at the IInputActionCollection2 level and thus no longer introduced separately
    // by InputActionMap and InputActionAsset.
    [Property("Exclusions", @"1.0.0
        public UnityEngine.InputSystem.InputAction FindAction(string actionNameOrId, bool throwIfNotFound = False);
        public UnityEngine.InputSystem.InputAction FindAction(string nameOrId, bool throwIfNotFound = False);
    ")]
    // RemoveAllBindingOverrides(InputActionMap) is now RemoveAllBindingOverrides (IInputActionCollection2).
    [Property("Exclusions", @"1.0.0
        public static void RemoveAllBindingOverrides(UnityEngine.InputSystem.InputActionMap actionMap);
    ")]
    // These methods have gained an extra (optional) parameter.
    [Property("Exclusions", @"1.0.0
        public UnityEngine.InputSystem.InputTestFixture.ActionConstraint Canceled(UnityEngine.InputSystem.InputAction action, UnityEngine.InputSystem.InputControl control = default(UnityEngine.InputSystem.InputControl), System.Nullable<double> time = default(System.Nullable<double>), System.Nullable<double> duration = default(System.Nullable<double>));
        public UnityEngine.InputSystem.InputTestFixture.ActionConstraint Performed(UnityEngine.InputSystem.InputAction action, UnityEngine.InputSystem.InputControl control = default(UnityEngine.InputSystem.InputControl), System.Nullable<double> time = default(System.Nullable<double>), System.Nullable<double> duration = default(System.Nullable<double>));
        public UnityEngine.InputSystem.InputTestFixture.ActionConstraint Started(UnityEngine.InputSystem.InputAction action, UnityEngine.InputSystem.InputControl control = default(UnityEngine.InputSystem.InputControl), System.Nullable<double> time = default(System.Nullable<double>));
        public static UnityEngine.InputSystem.InputActionSetupExtensions.BindingSyntax AddBinding(UnityEngine.InputSystem.InputActionMap actionMap, string path, string interactions = default(string), string groups = default(string), string action = default(string));
        public UnityEngine.InputSystem.InputActionSetupExtensions.CompositeSyntax With(string name, string binding, string groups = default(string));
        public static void DisableDevice(UnityEngine.InputSystem.InputDevice device);
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

        if (currentVersion.Major == lastReleasedVersion.Major)
        {
            Unity.Coding.Editor.ApiScraping.ApiScraping.Scrape();

            var currentApiFiles = Directory.GetFiles("Packages/com.unity.inputsystem", "*.api", SearchOption.AllDirectories);
            var lastPublicApiFiles = Directory.GetFiles(Path.Combine(kAPIDirectory, lastReleasedVersion.ToString()), "*.api");

            Assert.That(lastPublicApiFiles.Where(p => !currentApiFiles.Any(x => Path.GetFileName(x) == Path.GetFileName(p))),
                Is.Empty,
                "Any API file existing for the last published release must also exist for the current one.");

            var missingLines = lastPublicApiFiles.SelectMany(p => MissingLines(Path.GetFileName(p), currentApiFiles, lastPublicApiFiles, exclusions))
                .ToList();
            Assert.That(missingLines, Is.Empty);
        }
    }

    private static IEnumerable<string> MissingLines(string apiFile, string[] currentApiFiles, string[] lastPublicApiFiles, string[] exclusions)
    {
        var oldApiFile = lastPublicApiFiles.First(p => Path.GetFileName(p) == apiFile);
        var newApiFile = currentApiFiles.First(p => Path.GetFileName(p) == apiFile);

        var oldApiContents = File.ReadAllLines(oldApiFile).Select(FilterIgnoredChanges).ToArray();
        var newApiContents = File.ReadAllLines(newApiFile).Select(FilterIgnoredChanges).ToArray();

        foreach (var line in oldApiContents)
        {
            if (!newApiContents.Contains(line) && !exclusions.Any(x => x.Trim() == line.Trim()))
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

    #endif // UNITY_EDITOR_WIN

    ////TODO: add verification of *online* links to this; probably prone to instability and maybe they shouldn't fail tests but would
    ////      be great to have some way of diagnosing links that have gone stale
    [Test]
    [Category("API")]
    #if UNITY_EDITOR_OSX
    [Explicit] // Fails due to file system permissions on yamato, but works locally.
    #endif
    #if !HAVE_DOCTOOLS_INSTALLED
    [Ignore("Must install com.unity.package-manager-doctools package to be able to run this test")]
    #endif
    public void API_DocumentationManualDoesNotHaveMissingInternalLinks()
    {
        #if HAVE_DOCTOOLS_INSTALLED
        var docsFolder = GenerateDocsDirectory(out _);
        var unresolvedLinks = new List<string>();
        var htmlFileCache = new Dictionary<string, HtmlDocument>();
        foreach (var htmlFile in Directory.EnumerateFiles(Path.Combine(docsFolder, "manual")))
            CheckHTMLFileLinkConsistency(htmlFile, unresolvedLinks, htmlFileCache);
        Assert.That(unresolvedLinks, Is.Empty);
        #endif
    }

    [Test]
    [Category("API")]
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
