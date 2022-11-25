#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using HtmlAgilityPack;
using Mono.Cecil;
using NUnit.Framework;
#if HAVE_DOCTOOLS_INSTALLED
using UnityEditor.PackageManager.DocumentationTools.UI;
#endif
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.TestTools;

[TestFixture]
#if !HAVE_DOCTOOLS_INSTALLED
[Ignore("Must install com.unity.package-manager-doctools package to be able to run these tests.")]
#endif
class DocumentationBasedAPIVerficationTests
{
    private string _docsFolder;
    private string _documentationBuilderLogs;

    [OneTimeSetUp]
    public void OneTimeSetup()
    {
        // The dependency on `com.unity.modules.uielements` we have triggers a 404 error in doctools as it
        // tries to retrieve information on the "package" from `packages.unity.com`. As it is a module and not a
        // package, there's no metadata on the server and PacmanUtils.GetVersions() in doctools will log an
        // error to the console. This doesn't impact the rest of the run so just ignore it.
        // This is a workaround. Remove when fixed in doctools.
        LogAssert.ignoreFailingMessages = true;

        // DocumentationBuilder users C:/temp on Windows to avoid deeply nested paths that go
        // beyond the Windows path limit. However, on Yamato agent, C:/temp does not exist.
        // Create it manually here.
#if UNITY_EDITOR_WIN
        Directory.CreateDirectory("C:/temp");
#endif
        var docsPath = Path.GetFullPath(Path.Combine(Application.dataPath, "../Temp/docstest"));
        Directory.CreateDirectory(docsPath);
        var inputSystemPackageInfo = UnityEditor.PackageManager.PackageInfo.FindForAssetPath("Packages/com.unity.inputsystem");

#if HAVE_DOCTOOLS_INSTALLED
        (_documentationBuilderLogs, _docsFolder) = Documentation.Instance.GenerateEx(inputSystemPackageInfo, InputSystem.version.ToString(), docsPath);
        _docsFolder = Path.Combine(docsPath, _docsFolder);
#endif
    }

    [Test]
    [Category("API")]
#if UNITY_EDITOR_OSX
    [Explicit] // Fails due to file system permissions on yamato, but works locally.
#endif
    public void API_DocsDoNotHaveXMLDocErrors()
    {
        var lines = _documentationBuilderLogs.Split('\n');
        Assert.That(lines.Where(l => l.Contains("Badly formed XML")), Is.Empty);
        Assert.That(lines.Where(l => l.Contains("Invalid cref")), Is.Empty);
    }

    [Test]
    [Category("API")]
    [Ignore("Still needs a lot of documentation work to happen")]
#if UNITY_EDITOR_OSX
    [Explicit]     // Fails due to file system permissions on yamato, but works locally.
#endif
    public void API_DoesNotHaveUndocumentedPublicMethods()
    {
        var undocumentedMethods = APIVerificationTests.GetInputSystemPublicMethods().Where(m =>  !IgnoreMethodForDocs(m) && string.IsNullOrEmpty(MethodSummary(m, _docsFolder)));
        Assert.That(undocumentedMethods, Is.Empty, $"Got {undocumentedMethods.Count()} undocumented methods.");
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
        var unresolvedLinks = new List<string>();
        var htmlFileCache = new Dictionary<string, HtmlDocument>();
        foreach (var htmlFile in Directory.EnumerateFiles(Path.Combine(_docsFolder, "manual")))
            CheckHTMLFileLinkConsistency(htmlFile, unresolvedLinks, htmlFileCache);
        Assert.That(unresolvedLinks, Is.Empty);
    }

    [Test]
    [Category("API")]
#if UNITY_EDITOR_OSX
    [Explicit] // Fails due to file system permissions on yamato, but works locally.
#endif
    public void API_DoesNotHaveUndocumentedPublicTypes()
    {
        var undocumentedTypes = APIVerificationTests.GetInputSystemPublicTypes()
            .Where(type => !IgnoreTypeForDocs(type) && string.IsNullOrEmpty(TypeSummary(type, _docsFolder)));
        Assert.That(undocumentedTypes, Is.Empty, $"Got {undocumentedTypes.Count()} undocumented types, the docs are generated in {_docsFolder}");
    }

    [Test]
    [Category("API")]
#if UNITY_EDITOR_OSX
    [Explicit] // Fails due to file system permissions on yamato, but works locally.
#endif
    public void API_MonoBehaviourHelpUrlsAreValid()
    {
        // We exclude abstract MonoBehaviours as these can't show up in the Unity inspector.
        var monoBehaviourTypes = typeof(InputSystem).Assembly.ExportedTypes.Where(t =>
            t.IsPublic &&
            !t.IsAbstract &&
            !APIVerificationTests.IgnoreTypeForDocsByName(t.FullName) &&
            !APIVerificationTests.IgnoreTypeForDocsByNamespace(t.Namespace) &&
            typeof(MonoBehaviour).IsAssignableFrom(t));

        var monoBehaviourTypesHelpUrls = monoBehaviourTypes
            .Where(t => t.GetCustomAttribute<HelpURLAttribute>() != null)
            .Select(t => t.GetCustomAttribute<HelpURLAttribute>().URL);

        // Ensure the links are actually valid.
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
                    var docsFilePath = Path.Combine(_docsFolder, docsFileName);
                    var doc = new HtmlDocument();
                    doc.Load(docsFilePath);

                    // Look up anchor.
                    return doc.DocumentNode.SelectSingleNode($"//*[@id = '{anchorName}']") == null;
                });

        Assert.That(brokenHelpUrls, Is.Empty);
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

    private static bool IgnoreTypeForDocs(TypeDefinition type)
    {
        return APIVerificationTests.IgnoreTypeForDocsByName(type.FullName) ||
            APIVerificationTests.IgnoreTypeForDocsByNamespace(type.Namespace);
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

    private static HtmlDocument LoadHtmlDocument(string htmlFile, Dictionary<string, HtmlDocument> htmlFileCache)
    {
        if (!htmlFileCache.ContainsKey(htmlFile))
        {
            htmlFileCache[htmlFile] = new HtmlDocument();
            htmlFileCache[htmlFile].Load(htmlFile);
        }

        return htmlFileCache[htmlFile];
    }
}
#endif
