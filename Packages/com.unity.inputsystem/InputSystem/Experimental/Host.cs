using System;
using System.Collections.Generic;
using UnityEngine.InputSystem.Experimental;
using UnityEngine.InputSystem.Experimental.Interfaces;

namespace InputSystem.Experimental
{
    public static class BuiltinTypes
    {
        public const ushort StandardGamepad = 0x0001;

        public const ushort CustomTypeRangeBegin = 0x7fff;
    }
    
    public class Host
    {
        private static Host _instance;

        unsafe struct RegisteredType
        {
            public ushort type;
            public Func<WrappedStream, object> factory;
        }
        
        private Dictionary<Type, RegisteredType> _types;
        private ushort _typeId;
        
        public static Host instance
        {
            get { return _instance ??= new Host(); }
        }
        
        public ushort RegisterType(Type type, Func<WrappedStream, object> factory)
        {
            // Handle built-in types
            if (type is StandardGamepad)
                return BuiltinTypes.StandardGamepad;
            
            // Register custom type
            if (_types.TryGetValue(type, out RegisteredType value))
                return value.type;
            RegisteredType registeredType;
            registeredType.type = ++_typeId;
            registeredType.factory = factory;
            while (!_types.TryAdd(type, registeredType))
            {
                registeredType.type = ++_typeId;
            }
            return _typeId;
        }

        public ushort GetType(Type type)
        {
            return _types.TryGetValue(type, out RegisteredType value) ? value.type : (ushort)0;
        }
    }
}