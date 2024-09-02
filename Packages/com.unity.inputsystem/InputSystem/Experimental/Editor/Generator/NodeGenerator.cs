using System;
using System.Diagnostics;
using System.IO;
using System.Text;
using UnityEditor;

namespace UnityEngine.InputSystem.Experimental.Generator
{
    public class NodeGenerator
    {
        private const string kHeader =
            @"// WARNING: This is an auto-generated file. Any manual edits will be lost.
using System;
using System.Buffers;
using System.Diagnostics.CodeAnalysis;

using UnityEngine.InputSystem.Experimental.Internal; // TODO ArrayPoolExtensions could be incorporated

namespace UnityEngine.InputSystem.Experimental
{
    /// <summary>
    /// TemplateSummaryDoc
    /// </summary>
    [InputSource]
    public readonly partial struct TemplateName<TSource> : IObservableInputNode<TOut>
        where TSource : IObservableInputNode<TIn>
    {
        private readonly TSource m_Source;

        public TemplateName([InputPort] TSource source)
        {
            m_Source = source;
        }
    } 

    public readonly partial struct TemplateName<TSource> : IObservableInputNode<TOut>
        where TSource : IObservableInputNode<TIn>
    {
        // TODO Return Subscription instead of IDispoable when its a struct. Hence why we do not want subscription as a struct since it easily gets boxed.

        #region IObservable<TOut>

        public IDisposable Subscribe([NotNull] IObserver<InputEvent> observer) => 
            Subscribe(Context.instance, observer);

        #endregion

        #region IObservableInput<TOut>

        public IDisposable Subscribe<TObserver>([NotNull] Context context, [NotNull] TObserver observer)
            where TObserver : IObserver<TOut>
        {
            // TODO Implement node sharing (multi-cast)

            // Construct node instance and register underlying subscriptions
            var impl = ObjectPool<TemplateNodeName>.shared.Rent();
            impl.Initialize( m_Source.Subscribe(context, impl) ); 

            // Register observer with node and return subscription
            impl.AddObserver(observer);
            return new Subscription<InputEvent>(impl, observer);
        }

        #endregion

        #region IDependencyGraphNode

        public bool Equals(IDependencyGraphNode other) => other is TemplateName<TSource> node && Equals(node);
        public bool Equals(TemplateName<TSource> other) => m_Source.Equals(other.m_Source);    
        public string displayName => ""TemplateName""; 
        public int childCount => 1; 
        public IDependencyGraphNode GetChild(int index) 
        {
            switch (index)
            {
                case 0:  return m_Source;
                default: throw new ArgumentOutOfRangeException(nameof(index));
            }
        }

        #endregion
    }

    internal sealed class TemplateNameObserver : ObserverBase<TOut>, IObserver<TIn>, IUnsubscribe<TOut>
    {
        private TIn m_PreviousValue;
        private IDisposable m_SourceSubscription;

        public void Initialize([NotNull] IDisposable sourceSubscription)
        {
            m_SourceSubscription = sourceSubscription;
        }

        public void Unsubscribe([NotNull] IObserver<InputEvent> observer)
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

    /// <summary>
    /// Fluent API extension methods for <see cref=""TemplateFullName""/>.
    /// </summary>
    public static class TemplateNameExtensions
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
}";

        private static string Replace(string value, string nodeName, string fullNodeName, string inputType, string outputType)
        {
            return value
                .Replace("TemplateName", nodeName, StringComparison.Ordinal)
                .Replace("TemplateFullName", fullNodeName, StringComparison.Ordinal)
                .Replace("TemplateNodeName", nodeName + "Observer", StringComparison.Ordinal)
                .Replace("TIn", inputType, StringComparison.Ordinal)
                .Replace("TOut", outputType, StringComparison.Ordinal);
        }
        
        private static string Generate2(string filePath, string @namespace)
        {
            var nodeName = "Process";                               // Derive via reflection
            var inputType = "bool";                                 // Derive via reflection of marked argument
            var outputType = "InputEvent";                          // Derive via implemented interfaces via reflection
            var extensionsName = nodeName + "Extensions";     // Fixed default
            var fullNodeName = $"{@namespace}.{nodeName}{{TSource}}"; // Fully qualified name

            var sb = new StringBuilder();
            sb.Append(Replace(kHeader, nodeName, fullNodeName, inputType, outputType));
            return sb.ToString();
        }
        
        [MenuItem("Debug/Generate")]
        public static void Generate()
        {
            var stopwatch = Stopwatch.StartNew();
            
            var packageNamespace = "UnityEngine.InputSystem.Experimental";
            var filePath = $"Packages/com.unity.inputsystem/InputSystem/Experimental/Reactive/Generated.cs";

            try
            {
                //Generate(destinationFilePath, packageNamespace);
                var newContent = Generate2(filePath, packageNamespace);
                var currentContent = File.Exists(filePath) ? File.ReadAllText(filePath) : null;
                if (!newContent.Equals(currentContent))
                {
                    File.WriteAllText(filePath, newContent);
                    
                    stopwatch.Stop();
                    var elapsed = stopwatch.Elapsed.TotalSeconds;
                    LogFileMessage(LogType.Log, filePath, $"successfully generated in {elapsed} seconds.");
                }
                else
                {
                    LogFileMessage(LogType.Log, filePath, "already up to date");
                }
            }
            catch (Exception e)
            {
                LogFileMessage(LogType.Error, filePath, "could not be generated due to an unexpected exception");
                Debug.LogException(e);
            }
            
            stopwatch.Stop();
        }

        private static string Generate(string filePath, string @namespace)
        {
            var nodeName = "Process";
            var extensionsName = nodeName + "Extensions";
            
            var context = new SourceContext();
            var ns = context.root.Namespace(@namespace);
            
            var typeArg = new Syntax.TypeArgument(context, "T");
            typeArg.AddConstraint("struct");
            
            var proxy = ns.DeclareStruct(nodeName);
            proxy.visibility = Syntax.Visibility.Public;
            proxy.AddTypeArgument(typeArg);
            proxy.ImplementInterface("UnityEngine.InputSystem.Experimental.IObservableInputNode<T>");

            var p = proxy.DefineProperty("displayName", new Syntax.TypeReference(typeof(string)));
            p.visibility = Syntax.Visibility.Public;
            //proxy.Snippet($"public string displayName => \"{nodeName}\"");
            //proxy.Snippet("public int childCount => 1");
            /*proxy.Snippet()
            proxy.Snippet(@"public string displayName { get; }
        public int childCount { get; }
        public IDependencyGraphNode GetChild(int index)
        {
            throw new NotImplementedException();
        }")*/
            
            var extension = ns.DeclareClass(extensionsName);
            extension.visibility = Syntax.Visibility.Public;
            extension.isStatic = true;

            return context.ToSource();
        }

        private static void LogFileMessage(LogType logType, string path, string message)
        {
            Log(logType, $"\"{path}\" {message}");
        }

        private static void Log(LogType logType, string message)
        {
            Debug.unityLogger.Log(logType, $"{nameof(NodeGenerator)}: {message}.");
        }
    }
}