using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.Experimental.GraphView;
using UnityEditor.VersionControl;
using UnityEngine.UIElements;
using Task = System.Threading.Tasks.Task;

namespace UnityEngine.InputSystem.Experimental
{
    public class DependencyGraphViewEditorWindow : GraphViewEditorWindow
    {
        private const string ResourcePath = "Packages/com.unity.inputsystem/InputSystem/Experimental/Editor/Resources";

        private class DepenendencyGraphViewNodeModel
        {
            public DepenendencyGraphViewNodeModel(Type type)
            {
                displayName = GetName(type);

                /*var interfaces = type.GetInterfaces();
                for (var i = 0; i < interfaces.Length; ++i)
                {
                    Debug.Log(interfaces[i]);
                }*/
                
                // Identify outputs which maps to IObservableInput<T> implementations.
                // Note that this creates a limitation of a single output per type.
                var outputInterfaces = type.GetInterfaces()
                    .Where(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IObservableInput<>));
                var list = new List<string>();
                foreach (var outputInterface in outputInterfaces)
                    list.Add($"Output <{outputInterface.GenericTypeArguments[0].Name}>");
                outputs = list.ToArray();
                
                // Identify inputs which maps to attributes arguments of constructor.
                var genericArguments = type.GetGenericArguments();
                bool hasNonCompliantGenericArguments = false;
                for (var i = 0; i < genericArguments.Length; ++i)
                {
                    //if (generic)
                    var genericArgument = genericArguments[i];
                    /*if (genericArgument.GetGenericTypeDefinition() != typeof(IObservableInput<>))
                    {
                        hasNonCompliantGenericArguments = true;
                        break;
                    }*/
                    //type.MakeGenericType(ObservableInput<>)
                    var constraints = genericArguments[i].GetGenericParameterConstraints();
                    for (var j = 0; j < constraints.Length; ++j)
                    {
                        var constraint = constraints[j];
                        if (constraint.IsInterface)
                        {
                            
                        }
                    }
                    
                }
                //var genericParameterConstraints = type.GetGenericParameterConstraints();
                
                list.Clear();
                var constructors = type.GetConstructors();
                for (var i = 0; i < constructors.Length; ++i)
                {
                    var parameters = constructors[i].GetParameters();
                    for (var j = 0; j < parameters.Length; ++j)
                    {
                        var parameter = parameters[j];
                        if (!parameter.IsIn)
                            continue;
                        list.Add(parameter.Name);
                    }
                }
                inputs = list.ToArray();

                // TODO Iterate over constructors and select the most specific one and its arguments.
                //type.GetConstructors().Where(c => c.GetParameters())
            }

            private static string GetName(Type type, bool excludeNamespace = true, bool excludeGenerics = true)
            {
                var name = type.ToString();
                if (excludeGenerics)
                {
                    var genericsIndex = name.IndexOf('`');
                    if (genericsIndex >= 0)
                        name = name.Substring(0, genericsIndex);    
                }

                if (excludeNamespace)
                {
                    var namespaceIndex = name.LastIndexOf('.');
                    if (namespaceIndex >= 0)
                        name = name.Substring(namespaceIndex + 1, name.Length - namespaceIndex - 1);    
                }

                return name;
            }
            
            public string[] inputs { get; private set; }
            public string[] outputs { get; private set; }
            
            public string displayName { get; private set; }
        }
        
        private class DependencyGraphView : UnityEditor.Experimental.GraphView.GraphView
        {
            private Type[] m_NodeTypes;
            //private Task<Type[]> m_BuildCaches;
            
            public DependencyGraphView()
            {
                //var assemblies = AppDomain.CurrentDomain.GetAssemblies();
                //m_BuildCaches = Task.Factory.StartNew(() => FindClasses<IDependencyGraphNode>().ToArray());
                m_NodeTypes = FindNodeTypes();
                AddManipulators();
                AddGridBackground();
                AddStyles();
            }

            private Type[] FindNodeTypes()
            {
                return FindClasses<IDependencyGraphNode>().ToArray();
            }

            public override List<Port> GetCompatiblePorts(Port startPort, NodeAdapter nodeAdapter)
            {
                //return base.GetCompatiblePorts(startPort, nodeAdapter);
                
                var compatiblePorts = new List<Port>();
                ports.ForEach((port) =>
                {
                    if (startPort == port)
                        return; // Cannot attach port to itself
                    if (startPort.node == port.node)
                        return; // Cannot connect node to itself
                    if (startPort.direction == port.direction)
                        return; // Cannot connect ports of the same direction
                    // TODO Compare type
                    compatiblePorts.Add(port);
                });
                return compatiblePorts;
            }

            // TODO Instead let this create the models we need
            private static IEnumerable<Type> FindClasses<T>()
            {
                var type = typeof(T);
                var types = AppDomain.CurrentDomain.GetAssemblies()
                    .SelectMany(s => s.GetTypes())
                    .Where(p => type.IsAssignableFrom(p) && !p.IsInterface && !p.IsAbstract);
                return types;
            }

            private DependencyGraphViewNode CreateNode(DepenendencyGraphViewNodeModel model, Vector2 position)
            {
                // TODO Also consider Activator.CreateInstance
                // TODO We might use attributes instead of require ChildCount and GetChild(int index)
                var node = new DependencyGraphViewNode(model);
                node.SetPosition(new Rect(position, Vector2.zero));
                node.Draw();
                return node;
            }

            private void AddManipulators()
            {
                SetupZoom(ContentZoomer.DefaultMinScale, ContentZoomer.DefaultMaxScale);
                this.AddManipulator(new ContentDragger());
                this.AddManipulator(new SelectionDragger()); // Note: Needs to be added before RectangleSelector
                this.AddManipulator(new RectangleSelector());
                this.AddManipulator(CreateContextMenuManipulator());
            }

            private IManipulator CreateContextMenuManipulator()
            {
                var manipulator = new ContextualMenuManipulator((menuEvent) =>
                {
                    foreach (var type in m_NodeTypes)
                    {
                        var model = new DepenendencyGraphViewNodeModel(type);
                        menuEvent.menu.AppendAction(actionName: $"Create {model.displayName}", action: (actionEvent) =>
                        {
                            AddElement(CreateNode(model, actionEvent.eventInfo.localMousePosition));
                        });
                    }
                });
                return manipulator;
            }

            private void AddGridBackground()
            {
                // Add grid background and make sure its sized appropriately (defaults to zero) and inserted
                // at the back.
                var gridBackground = new GridBackground();
                gridBackground.StretchToParentSize();
                Insert(0, gridBackground);
            }

            private void AddStyles()
            {
                var styleSheet = (StyleSheet)EditorGUIUtility.Load(ResourcePath + "/DependencyGraphView.uss");
                styleSheets.Add(styleSheet);
            }
        }

        private class DependencyGraphViewNode : Node
        {
            private DepenendencyGraphViewNodeModel m_Model;

            public DependencyGraphViewNode(DepenendencyGraphViewNodeModel model)
            {
                m_Model = model;
                base.title = model.displayName;
            }
            
            public void Draw()
            {
                //var label = new Label() { text = "What" };
                
                //titleContainer.Insert(0, label);

                // Create input(s) 
                for (var i = 0; i < m_Model.inputs.Length; ++i)
                {
                    var port = InstantiatePort(Orientation.Horizontal, Direction.Input, Port.Capacity.Single, 
                        typeof(bool));
                    port.portName = m_Model.inputs[i];
                    inputContainer.Add(port);    
                }
                
                // Create output(s)
                for (var i = 0; i < m_Model.outputs.Length; ++i)
                {
                    var outputPort = InstantiatePort(Orientation.Horizontal, Direction.Output, Port.Capacity.Single,
                        typeof(bool));
                    outputPort.portName = m_Model.outputs[i];
                    outputContainer.Add(outputPort);    
                }
                
                RefreshExpandedState();
            }
        }

        private void OnEnable()
        {
            AddGraphView();
            AddStyles();
        }

        private void AddGraphView()
        {
            var graphView = new DependencyGraphView();
            graphView.StretchToParentSize();
            
            rootVisualElement.Add(graphView);
        }

        private void AddStyles()
        {
            var styleSheet = (StyleSheet)EditorGUIUtility.Load(ResourcePath + "/DependencyGraphVariables.uss");
            rootVisualElement.styleSheets.Add(styleSheet);
        }
        
        [MenuItem("Debug/Show")]
        public static void Open()
        {
            // This method is called when the user selects the menu item in the Editor.
            var wnd = GetWindow<DependencyGraphViewEditorWindow>();
            wnd.titleContent = new GUIContent("Input Dependency Graph");

            // Limit size of the window.
            wnd.minSize = new Vector2(450, 200);
            wnd.maxSize = new Vector2(1920, 720);
        }

        public void CreateGUI()
        {
            
        }
    }

    public static class DependenyGraphExtensions
    {
        public static void Show<T>(this IObservableInput<T> observableInput) where T : struct
        {
            DependencyGraphViewEditorWindow.Open();
        }
    }
}