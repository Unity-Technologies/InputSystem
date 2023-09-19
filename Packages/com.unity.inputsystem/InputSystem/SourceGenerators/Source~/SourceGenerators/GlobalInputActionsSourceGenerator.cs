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

[Generator]
public class GlobalInputActionsSourceGenerator : IIncrementalGenerator
{
    const string kPackagePath = "Packages//";
    const string kLibraryPackagePath = "Library//PackageCache//";
    const string kActionsTemplateAssetPath = "com.unity.inputsystem//InputSystem//Editor//ProjectWideActions//ProjectWideActionsTemplate.inputactions";
    const string kActionsAssetPath = "Assets//InputSystem//actions.InputSystemActionsAPIGenerator.additionalfile";

    public void Initialize(IncrementalGeneratorInitializationContext initContext)
    {
        // Get the additional file paths
        IncrementalValuesProvider<AdditionalText> additionalTexts = initContext.AdditionalTextsProvider;
        IncrementalValuesProvider<string> transformed = additionalTexts.Select(static (text, _) => text.Path);
        IncrementalValueProvider<ImmutableArray<string>> collected = transformed.Collect();

        initContext.RegisterSourceOutput(collected, static (sourceProductionContext, filePaths) =>
        {
            Execute(sourceProductionContext, filePaths);
        });
    }

    static void Execute(SourceProductionContext context, ImmutableArray<string> additionalFilePaths)
    {
        try
        {
            context.CancellationToken.ThrowIfCancellationRequested();

            var projectPath = GetProjectFilePath(context, additionalFilePaths);
            var actionsAssetPath = GetAssetFilePath(projectPath);
            if (string.IsNullOrEmpty(actionsAssetPath))
                return;

            context.CancellationToken.ThrowIfCancellationRequested();
            InputActionAsset asset = ParseInputActionsAsset(context, actionsAssetPath);
            var source = BuildActionAssetSource(asset);

            context.CancellationToken.ThrowIfCancellationRequested();
            context.AddSource($"InputSystemProjectActions.g.cs", SourceText.From(source, Encoding.UTF8));
            File.WriteAllText(Path.Combine(projectPath, "temp//InputSystemActionsAPIGenerator.g.cs"), source);
        }
        catch (Exception exception)
        {
            if (exception is OperationCanceledException)
                throw;
        }
    }

    // First additional file should be "AdditionalFile.txt" and it's contents contains the project path
    static string GetProjectFilePath(SourceProductionContext context, ImmutableArray<string> additionalFilePaths)
    {
        var firstAdditionalFilePath = additionalFilePaths.FirstOrDefault();
        if (firstAdditionalFilePath == null)
        {
            // @TODO: Why additional paths are empty but only for the IDE?
            return "/opt/UnitySrc/InputSystem/project-wide-actions-phase2";
            //return "D://UnitySrc//InputSystem//project-wide-actions-phase2";

            // context.ReportDiagnostic(Diagnostic.Create(
            //     new DiagnosticDescriptor("ISGEN001", "Additional files not specified.", "",
            //         "InputSystemSourceGenerator", DiagnosticSeverity.Error, true,
            //         "No additional files were specified."), null));
            //
            // throw new FileNotFoundException("Missing AdditionalFile.txt");
        }

        return File.ReadAllText(firstAdditionalFilePath);
    }

    static string GetAssetFilePath(string projectPath)
    {
        var actionsAssetPath = Path.Combine(projectPath, kActionsAssetPath);
        if (File.Exists(actionsAssetPath))
            return actionsAssetPath;

        // If the asset file hasn't been created yet, then we use the template (default) asset from the package
        actionsAssetPath = Path.Combine(projectPath, kLibraryPackagePath);
        actionsAssetPath = Path.Combine(actionsAssetPath, kActionsTemplateAssetPath);
        if (File.Exists(actionsAssetPath))
            return actionsAssetPath;

        actionsAssetPath = Path.Combine(projectPath, kPackagePath);
        actionsAssetPath = Path.Combine(actionsAssetPath, kActionsTemplateAssetPath);
        if (File.Exists(actionsAssetPath))
            return actionsAssetPath;

        return null;
    }

    static InputActionAsset ParseInputActionsAsset(SourceProductionContext context, string assetPath)
    {
        try
        {
            using var fileStream = new FileStream(assetPath, FileMode.Open, FileAccess.Read, FileShare.Read);
            InputActionAsset inputActionAssetNullable = JsonSerializer.Deserialize<InputActionAsset>(
                fileStream,
                new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true,
                    IncludeFields = true,
                    Converters =
                    {
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
                new DiagnosticDescriptor("ISGEN002", "", $"Couldn't parse project input actions asset file: {assetPath}. " + exception.Message.ToString(),
                    "InputSystemSourceGenerator",
                    DiagnosticSeverity.Error, true), null));

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

    [CompilerGenerated]
    public static partial class InputActions
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

                    public static {{GenerateInputActionMapClassName(actionMap)}} {{FormatFieldName(actionMap.Name)}};{{Environment.NewLine}}
                    """;
            }

            source +=
                $$"""
                static InputActions()
                {
                    {{GenerateInstantiateInputActionMaps(inputActionAsset.Maps)}}
                }
                """;
            source += " }";

            return source;
        }

        static string GetInputActionWrapperType(InputAction inputAction)
        {
            return $"Input<{GetTypeFromExpectedType(inputAction.ExpectedControlType)}>";
        }

        static string GenerateInputActionProperties(ActionMap actionMap)
        {
            var source = string.Empty;
            foreach (var action in actionMap.Actions)
            {
                var typeFromExpectedType = GetTypeFromExpectedType(action.ExpectedControlType);

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

                source += $"public Input<{typeFromExpectedType}> {FormatFieldName(action.Name)}  {{ get; }}{Environment.NewLine}";
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

    static string GetTypeFromExpectedType(ControlType controlType)
    {
        switch (controlType)
        {
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
    Analog,
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
