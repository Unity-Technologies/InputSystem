using System;
using System.Collections.Generic;
using System.Linq;

namespace UnityEngine.InputSystem.Experimental
{
    internal class NodeModel
    {
        private string TypeToString(Type type)
        {
            if (type.IsPrimitive)
            {
                if (type == typeof(bool))
                    return "bool";
                if (type == typeof(short))
                    return "short";
                if (type == typeof(ushort))
                    return "ushort";
                if (type == typeof(int))
                    return "int";
                if (type == typeof(uint))
                    return "uint";
                if (type == typeof(long))
                    return "long";
                if (type == typeof(ulong))
                    return "ulong";
                if (type == typeof(float))
                    return "float";
                if (type == typeof(double))
                    return "double";
                if (type == typeof(byte))
                    return "byte";
            }

            return type.ToString();
        }
        
        private Type IsObservableInputType(Type type)
        {
            if (type.IsGenericParameter)
            {
                var constraints = type.GetGenericParameterConstraints();
                var index = IndexOfObservableInputConstraint(constraints);
                if (index >= 0)
                {
                    var args = constraints[index].GenericTypeArguments;
                    return args[0];
                }
            }

            // If not generic, we need to check that concrete type implements IObservableInput<T> and extract T.
            // E.g. type is typically MySource : IObservableInput<int>.
            // TODO However it may be class MySource : IObservableInput<int>, IObservableInput<float>, i.e. implementing
            // multiple interfaces. In this case we cannot treat it as having a single type.
            var interfaces = type.GetInterfaces();
            for (var i = 0; i < interfaces.Length; ++i)
            {
                var x = interfaces[i];
                if (x.IsGenericType && x.GetGenericTypeDefinition() == typeof(IObservableInput<>))
                    return x.GenericTypeArguments[0];
            }

            return null;
        }
        
        private static int IndexOfObservableInputConstraint(Type[] constraints)
        {
            // First, walk through generic type constraints to see if this is an observable input.
            int index = -1;
            for (var j = 0; j < constraints.Length; ++j)
            {
                var constraint = constraints[j];
                if (constraint.IsInterface && constraint.IsGenericType &&
                    constraint.GetGenericTypeDefinition() == typeof(IObservableInput<>))
                {
                    index = j;
                    break;
                }
            }
            return index;
        }

        private static bool MapObservableInputConstraint(Type[] constraints, out Type concreteType, out Type valueType)
        {
            // If it is an IObservableInput constraint on it, attempt to create a concrete type out of it.
            var index = IndexOfObservableInputConstraint(constraints);
            if (index >= 0)
            {
                // ObservableInput only have a single type argument
                var interfaceTypes = constraints[index].GenericTypeArguments;
                if (interfaceTypes.Length == 1)
                {
                    valueType = interfaceTypes[0];
                    concreteType = typeof(ObservableInput<>).MakeGenericType(valueType);
                        
                    // TODO Should only evaluate for constructor arguments? Or should we use this only to attempt makeing a concerete type
                    // We have determined that the current generic argument is a constraint on an input type. Make sure our concrete type fulfills all other constraints
                    for (var k = 0; k < constraints.Length; ++k)
                    {
                        if (k == index)
                            continue; // Avoid checking against self again (MakeGenericType would have failed)
                        if (constraints[index].IsInterface && constraints[index].IsAssignableFrom(concreteType))
                            continue;
                        return false;
                    }

                    return true;
                }
            }

            concreteType = null;
            valueType = null;

            return false;
        }
        
        public NodeModel(Type type)
        {
            displayName = GetNameFromType(type);

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
            list.Clear();
            
            // Identify inputs which maps to attributes arguments of constructor. Note that type may be
            // generic in which case we need to attempt to derive a applicable concrete type to decide
            // if this is a bindable input.
            // (This may be simplified if attributes are used or if concrete overload is provided).
            if (type.IsGenericType)
            {
                var genericArguments = type.GetGenericArguments();
                var concreteArguments = new Type[genericArguments.Length];
                var substitutionSuccessful = true;
                for (var i = 0; i < genericArguments.Length; ++i)
                {
                    // Remap e.g. TSource where TSource : IObservableInput<bool> to IObservableInput<bool>
                    var constraints = genericArguments[i].GetGenericParameterConstraints();
                    if (MapObservableInputConstraint(constraints, out Type concreteType, out Type valueType))
                    {
                        concreteArguments[i] = concreteType;
                    }
                    else
                    {
                        substitutionSuccessful = false;
                        break;
                    }
                }

                if (substitutionSuccessful)
                {
                    var substitutedType = type.MakeGenericType(concreteArguments);
                    var constructors = substitutedType.GetConstructors();
                    if (constructors.Length == 1)
                    {
                        
                    }
                }
            }
            
            var types = AppDomain.CurrentDomain.GetAssemblies()
                .SelectMany(s => s.GetTypes())
                .Where(p => type.IsAssignableFrom(p) && !p.IsInterface && !p.IsAbstract);
            var methods = types.SelectMany(t => t.GetMethods())
                .Where(m => m.GetCustomAttributes(typeof(InputNodeFactoryAttribute), false).Length > 0);

            // TODO Use this when creating nodes
            var sources = AppDomain.CurrentDomain.GetAssemblies()
                .SelectMany(s => s.GetTypes())
                .Where(p => type.GetCustomAttributes(typeof(InputSourceAttribute), inherit: false).Length > 0);

            // Generate inputs from public constructor if available
            var typeConstructors = type.GetConstructors();
            if (typeConstructors.Length == 1)
            {
                var parameters = typeConstructors[0].GetParameters();
                for (var i = 0; i < parameters.Length; ++i)
                {
                    var p = parameters[i];
                    var t = IsObservableInputType(p.ParameterType);
                    if (t != null)
                        list.Add($"{parameters[i].Name} <i>({TypeToString(t)})</i>");
                    else
                        list.Add(parameters[i].Name);
                }
            }
            
            /*var constructors = type.GetConstructors();
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
            }*/
            inputs = list.ToArray();

            // TODO A type that only has outputs, e.g. ObservableInput<T> is generic and only provide output. This should not be listed.
            // TODO We should find [InputSource] nodes, e.g. Gamepad.
            
            // TODO Iterate over constructors and select the most specific one and its arguments.
            //type.GetConstructors().Where(c => c.GetParameters())
        }

        private static string GetNameFromType(Type type, bool excludeNamespace = true, bool excludeGenerics = true)
        {
            var name = type.ToString();
            
            // Exclude generics from type display name
            if (excludeGenerics)
            {
                var genericsIndex = name.IndexOf('`');
                if (genericsIndex >= 0)
                    name = name.Substring(0, genericsIndex);    
            }

            // Optionally exclude namespace prefix from display name
            if (excludeNamespace)
            {
                var namespaceIndex = name.LastIndexOf('.');
                if (namespaceIndex >= 0)
                    name = name.Substring(namespaceIndex + 1, name.Length - namespaceIndex - 1);    
            }

            return name;
        }
        
        /// <summary>
        /// Returns a list of inputs associated with this node.
        /// </summary>
        public string[] inputs { get; private set; }
        
        /// <summary>
        /// Returns a list of outputs associated with this node.
        /// </summary>
        public string[] outputs { get; private set; }
        
        /// <summary>
        /// Returns the display name of this node.
        /// </summary>
        public string displayName { get; private set; }
    }
}