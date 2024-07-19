using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace UnityEngine.InputSystem.Experimental
{
    // TODO Consider removing this and make it accessible via NodeModel.Types()
    internal sealed class DependencyNodeModel : IEnumerable<NodeModel>
    {
        private static readonly object Lock = new();
        private static List<NodeModel> _nodeModels;
        private static Task<List<NodeModel>> _nodeModelsTask;

        private List<NodeModel> m_NodeModels;

        public DependencyNodeModel()
        {
            // Construct underlying model asynchronously if not already created.
            // Note that since we cache it in a static field its automatically recreated if there is a domain
            // reload which should be the only reason for invalidating the model.
            lock (Lock)
            {
                // Avoid spawning task if already spawned
                if (_nodeModelsTask == null)
                {
                    // Construct task to compute model
                    _nodeModelsTask = new Task<List<NodeModel>>(() =>
                    {
                        var nodeTypes = FindClasses(typeof(IDependencyGraphNode)).ToArray();
                        var models = new List<NodeModel>(nodeTypes.Length);
                        for (var i = 0; i < nodeTypes.Length; ++i)
                            models.Add(new NodeModel(nodeTypes[i]));
                        return models;
                    });
                    
                    // Schedule the task for execution
                    _nodeModelsTask.Start();    
                }    
            }
        }

        public int count => GetList().Count;
        
        public NodeModel this[int index] => GetList()[index];

        public IEnumerator<NodeModel> GetEnumerator()
        {
            return GetList().GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        private static IEnumerable<Type> FindClasses(Type type)
        {
            return AppDomain.CurrentDomain.GetAssemblies()
                .SelectMany(s => s.GetTypes())
                .Where(p =>
                {
                    // Only accept type p if:
                    // 1. Type is assignable from p and its a concrete type, or
                    // 2. Type has been explicitly tagged as an input source.
                    if (type.IsAssignableFrom(p) && !p.IsInterface && !p.IsAbstract)
                        return true;
                    return type.GetCustomAttributes(typeof(InputSourceAttribute), inherit: false).Length > 0;
                });
        }
        
        private List<NodeModel> GetList()
        {
            // Return cached list if available
            if (m_NodeModels != null) 
                return m_NodeModels;
            
            // Extract from concurrent task if needed by waiting for completion
            lock (Lock)
            {
                if (_nodeModels == null)
                {
                    if (!_nodeModelsTask.IsCompleted)
                        _nodeModelsTask.Wait();
                    _nodeModels = _nodeModelsTask.Result;
                    _nodeModelsTask.Dispose();
                    _nodeModelsTask = null;
                }

                m_NodeModels = _nodeModels;
            }
            
            return m_NodeModels;
        }
    }
}