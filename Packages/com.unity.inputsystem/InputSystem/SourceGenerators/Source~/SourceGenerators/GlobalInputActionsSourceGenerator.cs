using System;
using System.Collections.Immutable;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;
using System.Reflection;

internal class PathAndContent
{
    public string path;
    public string content;

    public PathAndContent(string path, string content)
    {
        this.path = path;
        this.content = content;
    }
}

[Generator]
public class GlobalInputActionsSourceGenerator : IIncrementalGenerator
{
    const string kTargetAssembly = "Unity.InputSystem";
    const string kActionsFile = "actions.InputSystemActionsAPIGenerator.additionalfile";
    const string kTemplateFile = "TemplateActions.InputSystemActionsAPIGenerator.additionalfile";
    const string kUnityAdditionalFile = "UnityAdditionalFile.txt"; // Optional file, not to be relied upon to exist in IDEs or in future Unity builds.

    public void Initialize(IncrementalGeneratorInitializationContext initContext)
    {
        var assemblyName = initContext.CompilationProvider.Select(static (c, _) => c.AssemblyName);

        var additionalTexts = initContext.AdditionalTextsProvider;
        var pathsAndContents = additionalTexts.Select((text, cancellationToken) => new PathAndContent(text.Path, text.GetText(cancellationToken) !.ToString()));
        var combined = pathsAndContents.Collect().Combine(assemblyName);

        initContext.RegisterSourceOutput(combined, static (spc, combinedPair) =>
        {
            // We only want to inject the new API into one location and not duplicate into every loaded assembly.
            var assemblyName = combinedPair.Right;
            if (assemblyName != kTargetAssembly)
                return;

            Execute(spc, combinedPair.Left);
        });
    }

    static void Execute(SourceProductionContext context, ImmutableArray<PathAndContent> pathsAndContents)
    {
        try
        {
            context.CancellationToken.ThrowIfCancellationRequested();
            var assetInfo = GetAssetContentAndPath(context, pathsAndContents);

            // If the asset file hasn't been created yet, then we fallback to the template (default) asset from the package
            if (assetInfo == null)
                assetInfo = GetDefaultAssetContentAndPath(context, pathsAndContents);

            var asset = ParseInputActionsAsset(context, assetInfo.content, assetInfo.path);

            context.CancellationToken.ThrowIfCancellationRequested();
            var source = BuildActionAssetSource(asset);

            context.CancellationToken.ThrowIfCancellationRequested();
            context.AddSource($"InputSystemProjectActions.g.cs", SourceText.From(source, Encoding.UTF8));

            // Write file of generated source for debugging/inspection.
            var projectPath = GetProjectFilePath(context, pathsAndContents);
            if (!string.IsNullOrEmpty(projectPath))
                File.WriteAllText(Path.Combine(projectPath, "temp//InputSystemActionsAPIGenerator.g.cs"), source);
        }
        catch (Exception exception)
        {
            if (exception is OperationCanceledException)
                throw;
        }
    }

    // The project path is written into the file "Unity.InputSystem.UnityAdditionalFile.txt"
    // However the existence of this file is not guaranteed and the source generator should work
    // even without it. Especially in IDE compilations it will be missing.
    static string GetProjectFilePath(SourceProductionContext context, ImmutableArray<PathAndContent> pathsAndContents)
    {
        try
        {
            var unityFile = FindAdditionalFileContentAndPath(context, pathsAndContents, kUnityAdditionalFile);
            return unityFile.content;
        }
        catch
        {
            // ignored
        }
        return null;
    }

    // The asset is contained in the .additionalfile, which may be located anywhere in the User's Asset directory.
    // For this generator to receive this file, it's name needs to match the case-sensitive pattern:
    // actions.InputSystemActionsAPIGenerator.additionalfile
    // where <InputSystemActionsAPIGenerator> is the name of this source generator's assembly.
    static PathAndContent GetAssetContentAndPath(SourceProductionContext context, ImmutableArray<PathAndContent> pathsAndContents)
    {
        try
        {
            return FindAdditionalFileContentAndPath(context, pathsAndContents, kActionsFile);
        }
        catch
        {
            // ignored
        }
        return null;
    }

    // The asset is contained in the .additionalfile, which is a read-only file in the package.
    // For this generator to receive this file, it's name needs to match the case-sensitive pattern:
    // TemplateActions.InputSystemActionsAPIGenerator.additionalfile
    // where <InputSystemActionsAPIGenerator> is the name of this source generator's assembly.
    static PathAndContent GetDefaultAssetContentAndPath(SourceProductionContext context, ImmutableArray<PathAndContent> pathsAndContents)
    {
        try
        {
            return FindAdditionalFileContentAndPath(context, pathsAndContents, kTemplateFile);
        }
        catch
        {
            // This is fatal as we have no asset if even this fallback asset is unavailable.
            context.ReportDiagnostic(Diagnostic.Create(
                new DiagnosticDescriptor("ISGEN001", "", $"InputSystem Source Generator couldn't find template project wide actions asset .additionalfile.",
                    "InputSystemSourceGenerator", DiagnosticSeverity.Error, true), null));

            throw new FileNotFoundException("InputSystem is missing template project wide actions");
        }
    }

    static PathAndContent FindAdditionalFileContentAndPath(
        SourceProductionContext context,
        ImmutableArray<PathAndContent> pathsAndContents,
        string fileToFind)
    {
        foreach (var file in pathsAndContents)
        {
            if (!file.path.EndsWith(fileToFind))
                continue;
            return file;
        }
        throw new FileNotFoundException($"InputSystem Source Generator couldn't find additionalfile: {fileToFind}");
    }

    static InputActionAsset ParseInputActionsAsset(SourceProductionContext context, string jsonAsset, string assetPath)
    {
        try
        {
            InputActionAsset inputActionAssetNullable = JsonSerializer.Deserialize<InputActionAsset>(
                jsonAsset,
                new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true,
                    IncludeFields = true,
                    Converters =
                    {
                        new ExpectedControlTypeConverter(),
                        new JsonStringEnumConverter()
                    }
                });

            return inputActionAssetNullable;
        }
        catch (Exception exception)
        {
            if (exception is OperationCanceledException)
                throw;

            context.ReportDiagnostic(Diagnostic.Create(
                new DiagnosticDescriptor("ISGEN002", "", $"InputSystem couldn't parse project input actions asset file: {assetPath}. " + exception.Message.ToString(),
                    "InputSystemSourceGenerator", DiagnosticSeverity.Error, true), null));

            throw;
        }
    }

    // Code formatter does not like raw literals
    //*begin-nonstandard-formatting*
        static string BuildActionAssetSource(InputActionAsset asset)
        {
            var source =
"""
// <auto-generated/>
using System;
using System.Runtime.CompilerServices;
using UnityEngine;
using UnityEngine.InputSystem;

namespace UnityEngine.InputSystem.TypeSafeAPIInternals
{
    /// <summary>
    /// A wrapper base class for an Input Action
    /// </summary>
    /// <remarks>
    /// This is missing the type component of the _Input<T> class and therefore does not expose a type-safe value property.
    /// It is left to the user to call <see cref="InputAction.ReadValue{T}"/> directly on the underlying action with the correct type.
    /// </remarks>
    public class _Input
    {
        /// <summary>
        /// Enables access to the underlying Input Action for advanced functionality such as rebinding.
        /// </summary>
        public InputAction action => m_Action;

        /// <see cref="InputAction.IsPressed"/>
        public bool isPressed => m_Action.IsPressed();
        /// <see cref="InputAction.WasPressedThisFrame"/>
        public bool wasPressedThisFrame => m_Action.WasPressedThisFrame();
        /// <see cref="InputAction.WasReleasedThisFrame"/>
        public bool wasReleasedThisFrame => m_Action.WasReleasedThisFrame();

        /// <summary>
        /// Construct a wrapper from the given <see cref="InputAction"/>.
        /// </summary>
        /// <param name="action">The action to be wrapped.</param>
        public _Input(InputAction action)
        {
            Debug.Assert(action != null);

            m_Action = action ?? throw new ArgumentNullException(nameof(action));
            m_Action.Enable();
        }

        protected InputAction m_Action;
    } // class _Input

    /// <summary>
    /// A strongly-typed wrapper for an Input Action.
    /// </summary>
    /// <typeparam name="TActionType">The type that will be used in calls to <see cref="InputAction.ReadValue{T}"/></typeparam>
    public class _Input<TActionType> : _Input where TActionType : struct
    {
        /// <summary>
        /// Returns the current value of the Input Action.
        /// </summary>
        public TActionType value
        {
            get
            {
                try
                {
                    // it should be unusual for ReadValue to throw because in most cases instances of this class
                    // will be created through the source generator, and that can catch mismatched control types
                    // at compile time and throw compiler errors, but it will always be possible to dynamically
                    // add incompatible bindings, and the best we can do then is to catch the exceptions
                    // thrown when we try to read from those controls.
                    return m_Action.ReadValue<TActionType>();
                }
                catch (InvalidOperationException ex)
                {
                    Debug.LogWarning(ex.Message);
                }

                return default(TActionType);
            }
        }

        /// <summary>
        /// Construct a strongly-typed wrapper from the given <see cref="InputAction"/>.
        /// </summary>
        /// <param name="action">The action to be wrapped in the typesafe class.</param>
        public _Input(InputAction action) : base(action)
        {
        }

    } // class _Input<T>
} // namespace UnityEngine.InputSystem.TypeSafeAPIInternals

namespace UnityEngine.InputSystem
{
    /// <summary>
    /// A container class to hold the project-wide actions typesafe api.
    /// </summary>
    [CompilerGenerated]
    public static partial class ProjectActions
    {

""";
            var inputActionAsset = asset;
            foreach (var actionMap in inputActionAsset.Maps)
            {
                source +=
$$"""
        public class {{GenerateInputActionMapClassName(actionMap)}}
        {
            public {{GenerateInputActionMapClassName(actionMap)}}()
            {
                {{actionMap.Actions.Render(a =>
                    $"{FormatFieldName(a.Name)} = new {GetInputActionWrapperType(a)}(InputSystem.actions.FindAction(\"{actionMap.Name}/{a.Name}\"));{Environment.NewLine}")}}
            }

            {{GenerateInputActionProperties(actionMap)}}
        }

        public static {{GenerateInputActionMapClassName(actionMap)}} {{FormatFieldName(actionMap.Name)}} { get; }{{Environment.NewLine}}
""";
            } // foreach inputActionAsset.Maps

            source +=
$$"""

        static ProjectActions()
        {
            {{GenerateInstantiateInputActionMaps(inputActionAsset.Maps)}}
        }
    } // class Input
} // namespace UnityEngine.InputSystem
""";
            return source;
        }

        static string GetInputActionWrapperType(InputAction inputAction)
        {
            var controlType = GetTypeFromExpectedType(inputAction.ExpectedControlType, inputAction.Type);
            if (controlType == null)
                return $"UnityEngine.InputSystem.TypeSafeAPIInternals._Input";
            else
                return $"UnityEngine.InputSystem.TypeSafeAPIInternals._Input<{controlType}>";
        }

        static string GenerateInputActionProperties(ActionMap actionMap)
        {
            var source = string.Empty;
            foreach (var action in actionMap.Actions)
            {
                var bindings = actionMap.Bindings.Where(b => b.Action == action.Name).ToList();

                var bindingString = string.Empty;
                for (var i = 0; i < bindings.Count; i++)
                {
                    if (bindings[i].IsComposite)
                    {
                        bindingString += $"/// {bindings[i].Name}{Environment.NewLine}";

                        i++;
                        while (i < bindings.Count && bindings[i].IsPartOfComposite)
                        {
                            bindingString += $"/// {bindings[i].Name}:{SecurityElement.Escape(bindings[i].Path)}{Environment.NewLine}";
                            i++;
                        }
                    }
                    else
                    {
                        bindingString += $"/// {SecurityElement.Escape(bindings[i].Path)}{Environment.NewLine}";
                    }
                }

                // remove the trailing newline
                if (bindings.Count > 0)
                    bindingString = bindingString.TrimEnd('\n', '\r');

                source += $$"""
                    /// <summary>
                    /// This action is currently bound to the following control paths:
                    ///
                    /// <example>
                    /// <code>
                    ///
                    {{bindingString}}
                    ///
                    /// </code>
                    /// </example>
                    /// </summary>

                    """;

                source += $"public {GetInputActionWrapperType(action)} {FormatFieldName(action.Name)}  {{ get; }}{Environment.NewLine}";
            }
            return source;
        }
//*end-nonstandard-formatting*

    static string GenerateInstantiateInputActionMaps(ActionMap[] actionMaps)
    {
        var str = "";
        foreach (var actionMap in actionMaps)
        {
            str += $"{FormatFieldName(actionMap.Name)} = new {GenerateInputActionMapClassName(actionMap)}();{Environment.NewLine}";
        }

        return str;
    }

    static string GenerateInputActionMapClassName(ActionMap actionMap)
    {
        // TODO: More robust class name generation. Replace incompatible characters
        var actionMapClassName = actionMap.Name.Replace(" ", "");

        return FormatClassName(actionMapClassName) + "InputActionMap";
    }

    static string FormatClassName(string str)
    {
        return "_" + char.ToUpper(str[0]) + str.Substring(1);
    }

    static string FormatFieldName(string str)
    {
        if (str.Length <= 3)
            return (char.IsDigit(str[0]) ? "_" : "") + str.ToUpper();

        return (char.IsDigit(str[0]) ? "_" : "") + char.ToLower(str[0]) + str.Substring(1);
    }

    static string GetTypeFromExpectedType(ControlType controlType, ActionType actionType)
    {
        if (actionType == ActionType.Button)
            return nameof(Single);

        switch (controlType)
        {
            case ControlType.None:
            case ControlType.Any:
                return null;

            case ControlType.Analog:
            case ControlType.Axis:
            case ControlType.Button:
            case ControlType.Delta:
            case ControlType.DiscreteButton:
            case ControlType.Key:
                return nameof(Single);

            case ControlType.Digital:
            case ControlType.Integer:
                return nameof(Int32);

            case ControlType.Double:
                return nameof(Double);

            case ControlType.Bone:
                return "UnityEngine.InputSystem.XR.Bone";

            case ControlType.Dpad:
            case ControlType.Vector2:
            case ControlType.Stick:
                return "UnityEngine.Vector2";

            case ControlType.Vector3:
                return "UnityEngine.Vector3";

            case ControlType.Eyes:
                return "UnityEngine.InputSystem.XR.Eyes";

            case ControlType.Pose:
                return "UnityEngine.InputSystem.XR.PoseState";

            case ControlType.Quaternion:
                return "UnityEngine.Quaternion";

            case ControlType.Touch:
                return "UnityEngine.InputSystem.LowLevel.TouchState";

            default:
                return null;
        }
    }

    // This is to workaround that Unity's JsonUtility does not handle optional fields properly.
    // Essentially it populates optional fields as empty strings, rather than omitting them during serialization.
    // These cannot then be properly deserialized into Enums here without some customisation.
    public class ExpectedControlTypeConverter : JsonConverterFactory
    {
        public override bool CanConvert(Type typeToConvert)
        {
            return typeToConvert == typeof(ControlType);
        }

        public override JsonConverter CreateConverter(
            Type type,
            JsonSerializerOptions options)
        {
            JsonConverter converter = (JsonConverter)Activator.CreateInstance(
                typeof(ExpectedControlTypeConverterInner),
                BindingFlags.Instance | BindingFlags.Public,
                binder: null,
                args: new object[] { options },
                culture: null)!;

            return converter;
        }

        private class ExpectedControlTypeConverterInner : JsonConverter<ControlType>
        {
            private readonly JsonConverter<ControlType> m_ValueConverter;
            private readonly Type m_ValueType;

            public ExpectedControlTypeConverterInner(JsonSerializerOptions options)
            {
                // For performance, use the existing converter.
                m_ValueConverter = (JsonConverter<ControlType>)new JsonStringEnumConverter().CreateConverter(typeof(ControlType), options);
                    //(JsonConverter<ControlType>)options.GetConverter(typeof(ControlType));
                m_ValueType = typeof(ControlType);
            }

            public override ControlType Read(
                ref Utf8JsonReader reader,
                Type typeToConvert,
                JsonSerializerOptions options)
            {
                var controlType = ControlType.None; // Default value. Used for empty strings or missing values.
                try
                {
                    controlType = m_ValueConverter.Read(ref reader, m_ValueType, options);
                }
                catch { }

                return controlType;
            }

            public override void Write(
                Utf8JsonWriter writer,
                ControlType controlType,
                JsonSerializerOptions options) => throw new NotImplementedException();
        }
    }
}

public static class LinqExtensions
{
    public static string Render<T>(this IEnumerable<T> collection, Func<T, string> renderFunc)
    {
        var sb = new StringBuilder();
        foreach (var item in collection)
        {
            sb.Append(renderFunc(item));
        }

        return sb.ToString();
    }
}

public struct InputActionAsset
{
    public string Name;
    public ActionMap[] Maps;
    public ControlScheme[] ControlSchemes;
}

public struct ActionMap
{
    public string Id;
    public string Name;
    public InputAction[] Actions;
    public Binding[] Bindings;
}

public class InputAction
{
    public string Id;
    public ActionType Type;
    public string Name;
    public ControlType ExpectedControlType;
    public string Processors;
    public string Interactions;
}

public class Binding
{
    public string Id;
    public string Name;
    public string Path;
    public string Interactions;
    public string Groups;
    public string Action;
    public bool IsComposite;
    public bool IsPartOfComposite;
}

public class ControlScheme
{
}

public enum ActionType
{
    Button,
    Value,
    Passthrough
}

public enum ControlType
{
    None,

    Analog,
    Any,
    Axis,
    Bone,
    Button,
    Delta,
    Digital,
    DiscreteButton,
    Double,
    Dpad,
    Eyes,
    Integer,
    Key,
    Pose,
    Quaternion,
    Stick,
    Touch,
    Vector2,
    Vector3
}
