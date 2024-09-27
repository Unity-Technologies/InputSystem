using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using UnityEngine;
using UnityEngine.InputSystem.Experimental;
using UnityEngine.InputSystem.Experimental.Generator;

// TODO Remove partial struct definition.

namespace UnityEditor.InputSystem.Experimental.Generator
{
    public class NodeGenerator
    {
        private const string kSourceHeader =
            @"// WARNING: This is an auto-generated file. Any manual edits will be lost.
using System;
using System.Diagnostics.CodeAnalysis;
using UnityEngine.InputSystem.Experimental;
using UnityEngine.InputSystem.Experimental.Internal; // TODO ArrayPoolExtensions could be incorporated to avoid

";

        private const string kSourceBeginNamespace = @"namespace UnityEngine.InputSystem.Experimental
{
";
        
        private const string kSourceTemplate = @"    /// <summary>
    /// TemplateSummaryDoc
    /// </summary>
    [InputSource, Serializable]
    public partial struct TemplateName<TSource> : IObservableInputNode<TOut>
        where TSource : IObservableInputNode<TIn>
    {
        [SerializeField] private TSource source;

        public TemplateName([InputPort] TSource source)
        {
            this.source = source;
        }
    } 

    public partial struct TemplateName<TSource> : IObservableInputNode<TOut>
        where TSource : IObservableInputNode<TIn>
    {
        // TODO Return Subscription instead of IDispoable when its a struct. Hence why we do not want subscription as a struct since it easily gets boxed.

        #region IObservable<TOut>

        public IDisposable Subscribe([NotNull] IObserver<TOut> observer) => 
            Subscribe(Context.instance, observer);

        #endregion

        #region IObservableInput<TOut>

        public IDisposable Subscribe<TObserver>([NotNull] Context context, [NotNull] TObserver observer)
            where TObserver : IObserver<TOut>
        {
            // TODO Implement node sharing (multi-cast)

            // Construct node instance and register underlying subscriptions
            var impl = ObjectPool<TemplateNodeName>.shared.Rent();
            impl.Initialize( source.Subscribe(context, impl) ); 

            // Register observer with node and return subscription
            impl.AddObserver(observer);
            return new Subscription<TOut>(impl, observer);
        }

        #endregion

        #region IDependencyGraphNode

        public bool Equals(IDependencyGraphNode other) => other is TemplateName<TSource> node && Equals(node);
        public bool Equals(TemplateName<TSource> other) => source.Equals(other.source);    
        public string displayName => ""TemplateName""; 
        public int childCount => 1; 
        public IDependencyGraphNode GetChild(int index) 
        {
            switch (index)
            {
                case 0:  return source;
                default: throw new ArgumentOutOfRangeException(nameof(index));
            }
        }

        #endregion
    }
";
        
        // TODO Might make sense to switch to explicit source generation
        
        private const string kSourceTemplate2 = @"    /// <summary>
    /// TemplateSummaryDoc
    /// </summary>
    [InputSource, Serializable]
    public partial struct TemplateName<TSource> : IObservableInputNode<TOut>
        where TSource : IObservableInputNode<TIn>
    {
        [SerializeField] private TSource source;

        public TemplateName([InputPort] TSource source)
        {
            this.source = source;
        }

        // TODO Return Subscription instead of IDispoable when its a struct. Hence why we do not want subscription as a struct since it easily gets boxed.

        #region IObservable<TOut>

        public IDisposable Subscribe([NotNull] IObserver<TOut> observer) => 
            Subscribe(Context.instance, observer);

        #endregion

        #region IObservableInput<TOut>

        public IDisposable Subscribe<TObserver>([NotNull] Context context, [NotNull] TObserver observer)
            where TObserver : IObserver<TOut>
        {
            // TODO Implement node sharing (multi-cast)

            // Construct node instance and register underlying subscriptions
            var impl = ObjectPool<TemplateNodeName>.shared.Rent();
            impl.Initialize( source.Subscribe(context, impl) ); 

            // Register observer with node and return subscription
            impl.AddObserver(observer);
            return new Subscription<TOut>(impl, observer);
        }

        #endregion

        #region IDependencyGraphNode

        public bool Equals(IDependencyGraphNode other) => other is TemplateName<TSource> node && Equals(node);
        public bool Equals(TemplateName<TSource> other) => source.Equals(other.source);    
        public string displayName => ""TemplateName""; 
        public int childCount => 1; 
        public IDependencyGraphNode GetChild(int index) 
        {
            switch (index)
            {
                case 0:  return source;
                default: throw new ArgumentOutOfRangeException(nameof(index));
            }
        }

        #endregion
    }
";        

        private const string kSourceEndNamespace = @"}
";

        private const string kSourceObserverTemplate = @"    internal sealed class TemplateNameObserver : ObserverBase<TOut>, IObserver<TIn>, IUnsubscribe<TOut>
    {
        private TIn m_PreviousValue;
        private IDisposable m_SourceSubscription;

        public void Initialize([NotNull] IDisposable sourceSubscription)
        {
            m_SourceSubscription = sourceSubscription;
        }

        public void Unsubscribe([NotNull] IObserver<TOut> observer)
        {
            if (!RemoveObserver(observer)) 
                return;

            m_SourceSubscription.Dispose();
            m_SourceSubscription = null;
        }

        public void OnNext(TIn value)
        {
            if (m_PreviousValue == value) 
                return;
            if (value) // TODO Let class be converted to Step and take a IComparable<T> type, then we can use for both Press and Relase
                ForwardOnNext(new TOut()); // TODO This needs to be tentative, should indirection between data observer an events or we need another stage, so its either a separate method or parameter
            m_PreviousValue = value;
        }        
    }
";
        
        private const string kSourceStatelessObserverTemplate = @"
    internal sealed class TemplateNameObserver : ObserverBase<TOut>, IObserver<TIn>, IUnsubscribe<TOut>
    {
        private IDisposable m_SourceSubscription;

        public void Initialize([NotNull] IDisposable sourceSubscription)
        {
            m_SourceSubscription = sourceSubscription;
        }

        public void Unsubscribe([NotNull] IObserver<TOut> observer)
        {
            if (!RemoveObserver(observer)) 
                return;

            m_SourceSubscription.Dispose();
            m_SourceSubscription = null;
        }

        public void OnNext(TIn value)
        {
            TOperationDeclaringType.TOperation(this, value);
        }        
    }
";
        
        private const string kSourceStatefulObserverTemplate = @"
    internal sealed class TemplateNameObserver : ObserverBase<TOut>, IObserver<TIn>, IUnsubscribe<TOut>
    {
        private TOperationDeclaringType m_Operation;
        private IDisposable m_SourceSubscription;

        public void Initialize([NotNull] IDisposable sourceSubscription)
        {
            m_SourceSubscription = sourceSubscription;
        }

        public void Unsubscribe([NotNull] IObserver<TOut> observer)
        {
            if (!RemoveObserver(observer)) 
                return;

            m_SourceSubscription.Dispose();
            m_SourceSubscription = null;
        }

        public void OnNext(TIn value)
        {
            m_Operation.TOperation(this, value);
        }        
    }
";

        private const string kSourceExtensionMethods = @"   /// <summary>
    /// Fluent API extension methods for <see cref=""TemplateFullName""/>.
    /// </summary>
    public static partial class TemplateNameExtensions
    {
        /// <summary>
        /// Returns a new observable representing the TemplateName operation applied to <paramref name=""source""/>.
        /// </summary>
        /// <param name=""source"">The source observable.</param>
        /// <typeparam name=""TSource"">The source observable type.</typeparam>
        /// <returns>A new observable representing the TemplateName operation applied to <paramref name=""source""/>.</returns>
        /// <exception cref=""System.ArgumentNullException"">if <paramref name=""source""/> is <c>null</c>.</exception>
        public static TemplateName<TSource> TemplateName<TSource>(this TSource source)
            where TSource : IObservableInputNode<TIn>
        {
            return new TemplateName<TSource>(source); // TODO If we switch to class this should be cached
        }
    }
";
        
        // Generator settings
        private sealed class Settings
        {
            public Settings(string @namespace, string name, Type declaringType, string methodName)
            {
                this.@namespace = @namespace;
                this.name = name;
                this.declaringType = declaringType;
                this.fullNodeName = $"{@namespace}.{name}{{TSource}}";
                this.extensionsName = $"{name}Extensions";
                this.methodName = methodName;
            }
            
            public string @namespace { get; }
            public string name { get; }
            public Type declaringType { get; }

            public string nodeName => name;
            public string fullNodeName { get; }
            public string extensionsName { get; }
            public string methodName { get; }
        }
        
        // Replaces template placeholders with parameterized values.
        private static string Replace(string value, Settings settings, string inputType, string outputType)
        {
            return value
                .Replace("TemplateName", settings.nodeName, StringComparison.Ordinal)
                .Replace("TemplateFullName", settings.fullNodeName, StringComparison.Ordinal)
                .Replace("TemplateNodeName", settings.nodeName + "Observer", StringComparison.Ordinal)
                .Replace("TIn", inputType, StringComparison.Ordinal)
                .Replace("TOut", outputType, StringComparison.Ordinal)
                .Replace("TOperationDeclaringType", settings.declaringType.FullName, StringComparison.Ordinal)
                .Replace("TOperation", settings.methodName, StringComparison.Ordinal);
        }
        
        private static string Generate2(in Settings settings)
        {
            var inputType = "bool";                                 // Derive via reflection of marked argument
            var outputType = "InputEvent";                          // Derive via implemented interfaces via reflection

            var sb = new StringBuilder();
            sb.Append(kSourceHeader);
            sb.Append(Replace(kSourceBeginNamespace, settings, inputType, outputType));
            sb.Append(Replace(kSourceTemplate, settings, inputType, outputType));
            sb.Append(Replace(kSourceObserverTemplate, settings, inputType, outputType));
            sb.Append(Replace(kSourceExtensionMethods, settings, inputType, outputType));
            sb.Append(Replace(kSourceEndNamespace, settings, inputType, outputType));
            return sb.ToString();
        }
        
        readonly struct Port
        {
            public readonly string Name;
            public readonly string GenericTypeName;
            public readonly Type ParameterType;
            
            public Port(Type type, string name, string genericTypeName)
            {
                Name = name;
                ParameterType = type;
                GenericTypeName = genericTypeName;
            }
        }

        private static Port[] GetPorts(MethodInfo info, out string typeArguments, out string argumentList)
        {
            var parameterInfos = info.GetParameters();
            var length = parameterInfos.Length;
            var count = length - 1;
            var ports = new Port[count];
            for (var i = 1; i < length; ++i)
            {
                ports[i-1] = new Port(type: parameterInfos[i].ParameterType, 
                    name: count == 1 ? "source" : $"source{i}", 
                    genericTypeName: count == 1 ? "TSource" : $"TSource{i}");
            }
            
            var temp = new StringBuilder();
            temp.Append(ports[0].GenericTypeName);
            for (var i = 1; i < count; ++i)
                temp.Append(", ").Append(ports[i].GenericTypeName);
            typeArguments = temp.ToString();

            temp.Clear();
            temp.Append(ports[0].Name);
            for (var i = 1; i < count; ++i)
                temp.Append(", ").Append(ports[i].Name);
            argumentList = temp.ToString();

            return ports;
        }
        
        private static string GenerateOperation(MethodInfo info)
        {
            var outputType = typeof(bool); // TODO FIX
            
            var methodName = info.Name;
            var proxyName = methodName;
            var nodeName = $"{methodName}Node";
            var outputTypeName = SourceUtils.GetTypeName(outputType);
            
            // Source file context
            var ctx = new SourceContext();
            ctx.root.Using("System");
            ctx.root.Using("System.Diagnostics.CodeAnalysis");
            ctx.root.Using("UnityEngine.InputSystem.Experimental");
            ctx.root.Using("UnityEngine.InputSystem.Experimental.Internal"); // TODO Fix, should not access internals
            
            // Root namespace
            var @namespace = ctx.root.Namespace(info.DeclaringType!.Namespace);
            
            // Proxy struct
            var proxy = @namespace.DeclareStruct(name: info.Name, Syntax.Visibility.Public);
            proxy.isPartial = true;
            proxy.ImplementInterface(typeof(IObservableInputNode<>).MakeGenericType(outputType));
            proxy.DeclareAttribute(typeof(SerializableAttribute));
            proxy.DeclareAttribute(typeof(InputSourceAttribute));
            var proxyConstructor = proxy.DefineMethod(proxyName, Syntax.Visibility.Public);
            proxyConstructor.isConstructor = true;
            
            // Extension method class
            var extensions = @namespace.DeclareClass(info.Name + "Extensions");
            extensions.visibility = Syntax.Visibility.Public;
            extensions.isPartial = true;
            extensions.isStatic = true;
            
            // Node class
            var node = @namespace.DeclareClass(nodeName);
            node.isSealed = true;
            node.visibility = Syntax.Visibility.Internal;
            node.ImplementInterface(typeof(ObserverBase<>).MakeGenericType(outputType)); // TODO Not technically correct
            node.ImplementInterface(typeof(IUnsubscribe<>).MakeGenericType(outputType));
            
            var nodeInitializeMethod = node.DefineMethod(name: "Initialize", visibility: Syntax.Visibility.Public);
            
            var nodeObserverType = typeof(IObserver<>).MakeGenericType(outputType);
            var nodeUnsubscribeMethod = node.DefineMethod(name: "Unsubscribe", visibility: Syntax.Visibility.Public);
            nodeUnsubscribeMethod.Parameter("observer", nodeObserverType);
            nodeUnsubscribeMethod.Statement("if (!RemoveObserver(observer)) return");
            
            var nodeOnNextMethod = node.DefineMethod("OnNext", Syntax.Visibility.Public);
            nodeOnNextMethod.Statement($"{info.DeclaringType}.{methodName}(this, value)"); // TODO Use parameter list
            
            // Inputs
            var inputs = GetPorts(info, out var typeArgumentList, out var argList);
            for (var i = 0; i < inputs.Length; ++i)
            {
                var input = inputs[i];
                
                // Node needs to be an observer of all input types
                node.ImplementInterface(typeof(IObserver<>).MakeGenericType(input.ParameterType));
                
                // Each input is represented by a generic type in struct to avoid boxing and indirection
                var genericArg = proxy.TypeArgument(input.GenericTypeName);
                genericArg.AddStructConstraint();
                genericArg.AddConstraint(typeof(IObservableInputNode<>).MakeGenericType(input.ParameterType));
                
                // Each input is backed by a serializable field in proxy struct
                var field = proxy.DeclareField(genericArg, name: input.Name);
                field.visibility = Syntax.Visibility.Private;
                field.DeclareAttribute(typeof(SerializeField));
                
                // Node holds a subscription field associated with the input
                var argName = $"{input.Name}Subscription";
                var fieldName = $"m_{argName}";
                var subscriptionField = node.DeclareField(typeof(IDisposable), fieldName);
                subscriptionField.visibility = Syntax.Visibility.Private;
                
                // Add input parameter to proxy struct constructor
                proxyConstructor.Parameter(input.Name, genericArg);
                proxyConstructor.Statement($"this.{input.Name} = {input.Name}");
                
                // Add node input subscription to initialization method and assign it to field
                nodeInitializeMethod.Parameter(argName, typeof(IDisposable)).NotNull();
                nodeInitializeMethod.Statement($"{fieldName} = {argName}");
                
                // Add unsubscription logic for input to node
                nodeUnsubscribeMethod.Statement($"{fieldName}.Dispose()");
                nodeUnsubscribeMethod.Statement($"{fieldName} = null");
                
                // Add input parameter to OnNext method of node
                nodeOnNextMethod.Parameter("value", input.ParameterType);
            }
            
            // Forwarding IObservable subscribe
            var proxySubscribe = proxy.DefineMethod("Subscribe", visibility: Syntax.Visibility.Public);
            proxySubscribe.returnType = typeof(IDisposable);
            proxySubscribe.Parameter("observer", nodeObserverType).NotNull();
            proxySubscribe.Statement("return Subscribe(Context.instance, observer)");
            
            // IObservableInput subscribe
            proxy.Snippet(" ");
            proxy.Snippet("public IDisposable Subscribe<TObserver>([NotNull] Context context, [NotNull] TObserver observer)");
            proxy.Snippet($"    where TObserver : IObserver<{outputType}>");
            proxy.Snippet("{");
            proxy.Snippet("    if (context == null) throw new ArgumentNullException(nameof(context));");
            proxy.Snippet("    if (observer == null) throw new ArgumentNullException(nameof(observer));");
            proxy.Snippet("    // TODO Implement node sharing (multi-cast)");
            proxy.Snippet($"    var impl = ObjectPool<{nodeName}>.shared.Rent();");
            proxy.Snippet("    impl.Initialize(");
            for (var i = 0; i < inputs.Length; ++i)
                proxy.Snippet($"         {inputs[i].Name}.Subscribe(context, impl)");
            proxy.Snippet("    );");
            proxy.Snippet("    impl.AddObserver(observer);");
            proxy.Snippet($"    return new Subscription<{outputTypeName}>(impl, observer);");
            proxy.Snippet("}");
            proxy.Snippet(" ");
            
            // IDependencyGraphNode
            var explicitType = $"{proxyName}<{typeArgumentList}>";
            proxy.Snippet("#region IDependencyGraphNode");
            proxy.Snippet($"public bool Equals(IDependencyGraphNode other) => other is {explicitType} node && Equals(node);");
            if (inputs.Length == 1)
            {
                proxy.Snippet($"public bool Equals({explicitType} other) => {inputs[0].Name}.Equals(other.{inputs[0].Name});");
            }
            else
            {
                proxy.Snippet($"public bool Equals({explicitType} other)");
                proxy.Snippet("{");
                var n = inputs.Length - 1;
                proxy.Snippet($"return {inputs[0].Name}.Equals(other.{inputs[0].Name}) &&");
                for (var i = 1; i < n; ++i)
                    proxy.Snippet($"    {inputs[i].Name}.Equals(other.{inputs[i].Name}) &&");
                proxy.Snippet($"    {inputs[n].Name}.Equals(other.{inputs[n].Name});");
                proxy.Snippet("}");
                proxy.Snippet(" ");
            }
            proxy.Snippet($"public string displayName => \"{info.Name}\";");
            proxy.Snippet($"public int childCount => {inputs.Length};");
            proxy.Snippet("public IDependencyGraphNode GetChild(int index)");
            proxy.Snippet("{");
            proxy.Snippet("    switch(index)");
            proxy.Snippet("    {");
            for (var i = 0; i < inputs.Length; ++i)
                proxy.Snippet($"        case {i}: return {inputs[i].Name};");
            proxy.Snippet("        default: throw new ArgumentOutOfRangeException(nameof(index));");
            proxy.Snippet("    }");
            proxy.Snippet("}");
            proxy.Snippet("#endregion");
            
            // Extension method
            var extensionMethod = extensions.DefineMethod(proxyName, Syntax.Visibility.Public);
            extensionMethod.isExtensionMethod = true;
            extensionMethod.isStatic = true;
            extensionMethod.returnType = new Syntax.TypeArgument(ctx, explicitType); // TODO Fix syntax
            extensionMethod.Statement($"return new {explicitType}({argList})");
            for (var i = 0; i < inputs.Length; ++i)
            {
                var source = extensionMethod.TypeArgument(inputs[i].GenericTypeName);
                source.AddStructConstraint();
                source.AddConstraint(typeof(IObservableInputNode<>).MakeGenericType(inputs[i].ParameterType));
                extensionMethod.Parameter(inputs[i].Name, source);
            }
            
            return ctx.ToSource();
        }
        
        static IEnumerable<Type> GetTypesWithAttribute(Assembly assembly, Type attributeType) {
            foreach(Type type in assembly.GetTypes()) {
                if (type.GetCustomAttributes(attributeType, true).Length > 0) {
                    yield return type;
                }
            }
        }
        
        [MenuItem("Debug/Generate Nodes")]
        public static void Generate()
        {
            GenerateInputNode();
            GenerateOperations();
        }

        private static void GenerateOperations()
        {
            var operationMethods = TypeCache.GetMethodsWithAttribute(typeof(InputOperationAttribute));
            foreach (var operationMethod in operationMethods) // TODO Consider parallel for
            {
                if (operationMethod.IsAbstract) 
                    continue;
                
                var folder = Path.Combine(Resources.PackagePath, "Experimental/Reactive/");
                var path = folder + operationMethod.Name + ".g.cs";
                SourceUtils.Generate(path, () => GenerateOperation(operationMethod));
            }
        }
        
        private static void GenerateInputNode()
        {
            var inputNodeTypes = GetTypesWithAttribute(Assembly.GetAssembly(typeof(InputNodeAttribute)), typeof(InputNodeAttribute)).FirstOrDefault();

            var attribute = inputNodeTypes.GetCustomAttributes<InputNodeAttribute>().First();

            //type.GetGenericArguments();
            var onNextMethodName = "OnNext";
            var methods = inputNodeTypes.GetMethods();
            for (var i = 0; i < methods.Length; ++i)
            {
                var method = methods[i];
                if (method.Name.Equals("OnNext"))
                {
                    if (method.ReturnType.Name != "Void")
                    {
                        throw new Exception(
                            $"Invalid return type for method \"{onNextMethodName}\" must be 'void' but was '{method.ReturnType}'.");    
                    }
                    
                    var parameters = method.GetParameters();
                    
                    var genericArguments = method.GetGenericArguments();
                    for (var j = 0; j < genericArguments.Length; ++j)
                    {
                        var genericArgument = genericArguments[j];
                        var name = genericArgument.Name;
                    }
                }
            }

            var settings = new Settings(inputNodeTypes.Namespace, attribute.name, inputNodeTypes, string.Empty);
            var path = "Packages/com.unity.inputsystem/InputSystem/Experimental/Reactive/Generated.cs";
            SourceUtils.Generate(path, () => Generate2(settings));
        }
    }
}