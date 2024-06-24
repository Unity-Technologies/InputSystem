using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Experimental.GraphView;
using UnityEngine.UIElements;

namespace UnityEngine.InputSystem.Experimental
{
    public class DependencyGraphViewEditorWindow : GraphViewEditorWindow
    {
        private const string ResourcePath = "Packages/com.unity.inputsystem/InputSystem/Experimental/Editor/Resources";
        
        private class GraphView : UnityEditor.Experimental.GraphView.GraphView
        {
            private DependencyGraphViewEditorWindow m_EditorWindow;
            private SearchWindow m_SearchWindow;
            private readonly DependencyNodeModel m_Model;
            //private readonly Type[] m_NodeTypes;
            //private readonly NodeModel[] m_NodeModels;
            //private Task<Type[]> m_BuildCaches;
            
            public GraphView(DependencyGraphViewEditorWindow editorWindow)
            {
                //var assemblies = AppDomain.CurrentDomain.GetAssemblies();
                //m_BuildCaches = Task.Factory.StartNew(() => FindClasses<IDependencyGraphNode>().ToArray());
                m_Model = new DependencyNodeModel();
                m_EditorWindow = editorWindow;
                AddManipulators();
                AddSearchWindow();
                AddGridBackground();
                AddStyles();
            }

            private void AddSearchWindow()
            {
                if (m_SearchWindow != null)
                    return;

                m_SearchWindow = ScriptableObject.CreateInstance<SearchWindow>();
                m_SearchWindow.Initialize(this);

                nodeCreationRequest = (context) =>
                    UnityEditor.Experimental.GraphView.SearchWindow.Open(new SearchWindowContext(GetLocalMousePosition(context.screenMousePosition)), m_SearchWindow);
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

            internal DependencyGraphViewNode CreateNode(Vector2 position, NodeModel model)
            {
                // TODO Also consider Activator.CreateInstance
                // TODO We might use attributes instead of require ChildCount and GetChild(int index)
                var node = new DependencyGraphViewNode(model);
                node.SetPosition(new Rect(position, Vector2.zero));
                node.Draw();
                return node;
            }

            internal Group CreateGroup(Vector2 position, string title = "Group")
            {
                var group = new Group()
                {
                    title = title,
                };
                group.SetPosition(new Rect(position, Vector2.zero));
                return group;
            }

            private void AddManipulators()
            {
                SetupZoom(ContentZoomer.DefaultMinScale, ContentZoomer.DefaultMaxScale);
                this.AddManipulator(new ContentDragger());
                this.AddManipulator(new SelectionDragger()); // Note: Needs to be added before RectangleSelector
                this.AddManipulator(new RectangleSelector());
                this.AddManipulator(new ContextualMenuManipulator(CreateContextualMenu));
                this.AddManipulator(new ContextualMenuManipulator(CreateGroupContextualMenu));
            }

            private void CreateContextualMenu(ContextualMenuPopulateEvent menuEvent)
            {
                foreach (var node in m_Model)
                {
                    menuEvent.menu.AppendAction(actionName: $"Create {node.displayName}", action: (actionEvent) =>
                    {
                        AddElement(CreateNode(GetLocalMousePosition(actionEvent.eventInfo.localMousePosition), node));
                    });
                }
            }

            private void CreateGroupContextualMenu(ContextualMenuPopulateEvent menuEvent)
            {
                menuEvent.menu.AppendAction(actionName: $"Create Group", action: (actionEvent) =>
                {
                    AddElement(CreateGroup(GetLocalMousePosition(actionEvent.eventInfo.localMousePosition)));
                });
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

            internal Vector2 GetLocalMousePosition(Vector2 mousePosition, bool isSearchWindow = false)
            {
                var worldMousePositiion = mousePosition;
                if (isSearchWindow)
                {
                    worldMousePositiion -= m_EditorWindow.position.position;
                }
                var localMousePosition = contentViewContainer.WorldToLocal(worldMousePositiion);
                return localMousePosition;
            }
        }

        private class DependencyGraphViewNode : Node
        {
            private NodeModel m_Model;

            public DependencyGraphViewNode(NodeModel model)
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
            var graphView = new GraphView(this);
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
            var wnd = GetWindow<DependencyGraphViewEditorWindow>();
            wnd.titleContent = new GUIContent("Input Dependency Graph");
            wnd.minSize = new Vector2(450, 200);
            wnd.maxSize = new Vector2(1920, 720);
        }

        public void CreateGUI()
        {
            
        }
        
        private class SearchWindow : ScriptableObject, ISearchWindowProvider
        {
            private GraphView m_GraphView;
            private Texture2D m_IndentationIcon;

            private enum Choice
            {
                Invalid = 0,
                Group
            }
            
            public void Initialize(GraphView graphView)
            {
                m_GraphView = graphView;
                
                // Texture for indentation hack similar to shader graph
                m_IndentationIcon = new Texture2D(1, 1);
                m_IndentationIcon.SetPixel(0,0, Color.clear);
                m_IndentationIcon.Apply();
            }
        
            public List<SearchTreeEntry> CreateSearchTree(SearchWindowContext context)
            {
                var model = new DependencyNodeModel();
                var searchListEntry = new List<SearchTreeEntry>();
                searchListEntry.Add(new SearchTreeGroupEntry(new GUIContent("Create")));
                searchListEntry.Add(new SearchTreeGroupEntry(new GUIContent("Input Node"), 1));
                for (var i = 0; i < model.count; ++i)
                {
                    searchListEntry.Add(new SearchTreeEntry(new GUIContent(model[i].displayName, m_IndentationIcon))
                    {
                        level = 2,
                        userData = model[i]
                    });
                }
                searchListEntry.Add(new SearchTreeGroupEntry(new GUIContent("Input Node Group"), 1));
                searchListEntry.Add(new SearchTreeEntry(new GUIContent("Single Group", m_IndentationIcon))
                {
                    level = 2,
                    userData = Choice.Group
                });
                return searchListEntry;
            }

            public bool OnSelectEntry(SearchTreeEntry SearchTreeEntry, SearchWindowContext context)
            {
                var localMousePosition = m_GraphView.GetLocalMousePosition(context.screenMousePosition, isSearchWindow: true);
                var data = SearchTreeEntry.userData;
                if (data is NodeModel)
                {
                    m_GraphView.AddElement(m_GraphView.CreateNode(localMousePosition, (NodeModel)data));
                    return true; // close
                }

                if (data is Choice)
                {
                    m_GraphView.AddElement(m_GraphView.CreateGroup(localMousePosition));
                }

                return false; // remain open
            }
        }
    }

    internal static class DependencyGraphUtilities
    {
        
    }

    public static class ObservableInputEditorExtensions
    {
        /// <summary>
        /// Shows the given observable input as a graph.
        /// </summary>
        /// <param name="observableInput"></param>
        /// <typeparam name="T"></typeparam>
        public static void Show<T>(this IObservableInput<T> observableInput) where T : struct
        {
            DependencyGraphViewEditorWindow.Open();
        }
    }
}