using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
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
        
        struct Port
        {
            public string Name;
            public string TypeName;
            public string GenericTypeName;
            public string ArgName;
            public Syntax.Field Field;
            public Syntax.TypeReference Type;
            
            public Port(Type type, string name, string genericTypeName, string argName)
            {
                this.Name = name;
                this.TypeName = SourceUtils.GetTypeName(type);
                this.GenericTypeName = genericTypeName;
                this.ArgName = argName;
                Field = default;
                Type = default;
            }
        }

        private static Port[] GetPorts(MethodInfo info)
        {
            var parameterInfos = info.GetParameters();
            var length = parameterInfos.Length;
            var count = length - 1;
            var ports = new Port[count];
            for (int i = 1; i < length; ++i)
            {
                ref var p = ref parameterInfos[i];
                ports[i-1] = new Port(p.ParameterType, count == 1 ? "source" : $"source{i}", 
                    count == 1 ? "TSource" : $"TSource{i}", 
                    p.Name);
            }
            return ports;
    }
        
        private static string GenerateOperation(in Settings settings, MethodInfo info, Type inputType, Type outputType, bool stateless)
        {
            var inputTypeName = SourceUtils.GetTypeName(inputType);
            var outputTypeName = SourceUtils.GetTypeName(outputType);
            
            /*var sb = new StringBuilder();
            sb.Append(Replace(kSourceHeader, settings, inputTypeName, outputTypeName));
            sb.Append(Replace(kSourceBeginNamespace, settings, inputTypeName, outputTypeName));
            sb.Append(Replace(kSourceTemplate2, settings, inputTypeName, outputTypeName));
            if (stateless)
                sb.Append(Replace(kSourceStatelessObserverTemplate, settings, inputTypeName, outputTypeName));
            else
                sb.Append(Replace(kSourceStatefulObserverTemplate, settings, inputTypeName, outputTypeName));
            sb.Append(Replace(kSourceExtensionMethods, settings, inputTypeName, outputTypeName));
            sb.Append(Replace(kSourceEndNamespace, settings, inputTypeName, outputTypeName));*/

            if (true)
            {
                var proxyName = info.Name;
                var nodeName = $"{info.Name}Node";
                
                // TODO Fix, namespaces should be automatic
                var ctx = new SourceContext();
                ctx.root.Using("System");
                ctx.root.Using("System.Diagnostics.CodeAnalysis");
                ctx.root.Using("UnityEngine.InputSystem.Experimental");
                ctx.root.Using("UnityEngine.InputSystem.Experimental.Internal"); // TODO Fix, should not access internals
                
                var ns = ctx.root.Namespace(info.DeclaringType!.Namespace);
                
                var proxy = ns.DeclareStruct(name: info.Name, Syntax.Visibility.Public);
                proxy.isPartial = true;
                proxy.ImplementInterface(typeof(IObservableInputNode<>).MakeGenericType(outputType));
                proxy.DeclareAttribute(nameof(InputSourceAttribute));
                
                //var inputs = new List<Port>();
                var typeArgumentList = new StringBuilder();
                var argList = new StringBuilder();
                
                // Input #1
                var inputs = GetPorts(info);
                for (var i = 0; i < inputs.Length; ++i)
                {
                    var genericArg = proxy.TypeArgument(inputs[i].GenericTypeName);
                    genericArg.AddStructConstraint();
                    genericArg.AddConstraint(typeof(IObservableInputNode<>).MakeGenericType(inputType));
                    
                    var field = proxy.DeclareField(genericArg, name: inputs[i].Name);
                    field.visibility = Syntax.Visibility.Private;

                    if (typeArgumentList.Length != 0)
                    {
                        typeArgumentList.Append(", ");
                        argList.Append(", ");
                    }
                    typeArgumentList.Append(inputs[i].GenericTypeName);
                    argList.Append(inputs[i].Name);
                    
                    inputs[i].Field = field;
                    inputs[i].Type = genericArg;
                }
                
                var explicitType = $"{proxyName}<{typeArgumentList}>";
                var observerType = typeof(IObserver<>).MakeGenericType(outputType);
                
                // Constructor
                var ctor = proxy.DefineMethod(settings.nodeName, Syntax.Visibility.Public);
                ctor.isConstructor = true;
                
                // Forwarding IObservable subscribe
                var proxySubscribe = proxy.DefineMethod("Subscribe", visibility: Syntax.Visibility.Public);
                proxySubscribe.returnType = typeof(IDisposable);
                proxySubscribe.Parameter("observer", observerType).NotNull(); // TODO NotNull
                proxySubscribe.Statement("return Subscribe(Context.instance, observer)");
                //proxy.Snippet($"public IDisposable Subscribe([NotNull] IObserver<{outputTypeName}> observer) => Subscribe(Context.instance, observer);");
                
                // IObservableInput subscribe
                proxy.Snippet("");
                proxy.Snippet("public IDisposable Subscribe<TObserver>([NotNull] Context context, [NotNull] TObserver observer)");
                proxy.Snippet($"    where TObserver : IObserver<{outputType}>");
                proxy.Snippet("{");
                proxy.Snippet("    // TODO Implement node sharing (multi-cast)");
                proxy.Snippet($"    var impl = ObjectPool<{nodeName}>.shared.Rent();");
                proxy.Snippet("     impl.Initialize(");
                for (var i = 0; i < inputs.Length; ++i)
                {
                    proxy.Snippet($"         {inputs[i].Name}.Subscribe(context, impl)");
                }
                proxy.Snippet("     );");
                proxy.Snippet("    impl.AddObserver(observer);");
                proxy.Snippet($"    return new Subscription<{outputTypeName}>(impl, observer);");
                proxy.Snippet("}");
                proxy.Snippet("");
                
                // IDependencyGraphNode
                proxy.Snippet("#region IDependencyGraphNode");
                proxy.Snippet($"public bool Equals(IDependencyGraphNode other) => other is {explicitType} node && Equals(node);");
                proxy.Snippet($"public bool Equals({explicitType} other) => {inputs[0].Name}.Equals(other.{inputs[0].Name});"); // TODO FIX
                // TODO Need return statement: var property = proxy.DefineProperty(nameof(IDependencyGraphNode.displayName), typeof(string));
                proxy.Snippet($"public string displayName => \"{info.Name}\";");
                proxy.Snippet($"public int childCount => {inputs.Length};");
                /*var getChild = proxy.DefineMethod(nameof(IDependencyGraphNode.GetChild), Syntax.Visibility.Public);
                getChild.returnType = typeof(IDependencyGraphNode);
                getChild.Parameter("index", typeof(int));
                getChild.Statement("switch(index)"); // TODO Cannot be statement, should be block
                getChild.Statement("{");
                for (var i = 0; i < inputs.Count; ++i)
                    getChild.Statement($"        case {i}: return {inputs[i].Name};");
                getChild.Statement("    default: throw new ArgumentOutOfRangeException(nameof(index));");
                getChild.Statement("}");*/
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
                
                var node = ns.DeclareClass(nodeName);
                node.isSealed = true;
                node.visibility = Syntax.Visibility.Internal;
                node.ImplementInterface(typeof(ObserverBase<>).MakeGenericType(outputType)); // TODO Not technically correct
                node.ImplementInterface(typeof(IUnsubscribe<>).MakeGenericType(outputType));
                node.ImplementInterface(typeof(IObserver<>).MakeGenericType(inputType));
                
                var initialize = node.DefineMethod(name: "Initialize", visibility: Syntax.Visibility.Public);
                var unsubscribe = node.DefineMethod(name: "Unsubscribe", visibility: Syntax.Visibility.Public);
                
                unsubscribe.Parameter("observer", observerType);
                unsubscribe.Statement("if (!RemoveObserver(observer)) return");
                
                var onNext = node.DefineMethod("OnNext", Syntax.Visibility.Public);
                onNext.Statement($"{settings.declaringType}.{settings.methodName}(this, value)"); // TODO Use parameter list
                
                for (var i = 0; i < inputs.Length; ++i)
                {
                    var argName = $"{inputs[i].Name}Subscription";
                    var fieldName = $"m_{inputs[i].Name}Subscription";
                    var fieldRef = $"this.{fieldName}";
                    var subscriptionField = node.DeclareField(typeof(IDisposable), fieldName);
                    subscriptionField.visibility = Syntax.Visibility.Private;
                    
                    ctor.Parameter(inputs[i].Name, inputs[i].Type);
                    ctor.Statement($"this.{inputs[i].Name} = {inputs[i].Name}"); // TODO AssignmentStatement
                    
                    initialize.Parameter(argName, typeof(IDisposable)); // TODO NotNull
                    initialize.Statement($"{fieldRef} = {argName}"); // TODO AssignmentStatement
                    unsubscribe.Statement($"{fieldRef}.Dispose()");
                    unsubscribe.Statement($"{fieldRef} = null");
                    
                    onNext.Parameter("value", inputType); // TODO Take type from element
                }
                
                var extensions = ns.DeclareClass(info.Name + "Extensions");
                extensions.visibility = Syntax.Visibility.Public;
                extensions.isPartial = true;
                extensions.isStatic = true;

                // TODO This calls for dynamic solutions depending on number of arguments.
                var ext = extensions.DefineMethod(proxyName, Syntax.Visibility.Public);
                ext.isExtensionMethod = true;
                ext.isStatic = true;
                for (var i = 0; i < inputs.Length; ++i)
                {
                    var source = ext.TypeArgument(inputs[i].GenericTypeName);
                    source.AddStructConstraint();
                    source.AddConstraint(typeof(IObservableInputNode<>).MakeGenericType(inputType));
                    ext.Parameter(inputs[i].Name, source);
                }

                ext.Statement($"return new {explicitType}({argList})"); // TODO Need full argument list
                ext.returnType = new Syntax.TypeArgument(ctx, explicitType); // TODO Fix syntax
                
                return ctx.ToSource();
            }
            
            //return sb.ToString();
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
            foreach (var operationMethod in operationMethods)
            {
                if (operationMethod.IsAbstract) 
                    continue;
                var stateless = operationMethod.IsStatic;
                var generic = operationMethod.DeclaringType!.IsGenericType; // TODO If declaring type is generic we need to parameterize it as such

                var folder = Path.Combine(Resources.PackagePath, "Experimental/Reactive/");
                var path = folder + operationMethod.Name + ".g.cs";
                var settings = new Settings(operationMethod.DeclaringType!.Namespace,
                    operationMethod.Name,
                    operationMethod.DeclaringType,
                    operationMethod.Name);

                var parameters = operationMethod.GetParameters();
                //if (parameters.Length != 2)
                //    continue; // TODO Generate errors
                var inputCount = parameters.Length - 1;
                
                // TODO Make a list of inputs, need to find a way to express whether we want inputs by index.
                
                SourceUtils.Generate(path, () => GenerateOperation(settings, operationMethod, typeof(bool), typeof(bool), stateless));
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