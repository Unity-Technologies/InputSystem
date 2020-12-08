using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine.InputSystem.Utilities;

namespace UnityEngine.InputSystem
{
    public interface IScreenKeyboardFactory
    {
        ScreenKeyboard Create();
    }

    internal static class ScreenKeyboardUtilities
    {
        static List<IScreenKeyboardFactory> m_Factories;

        internal static IReadOnlyList<IScreenKeyboardFactory> Factories
        {
            get
            {
                if (m_Factories != null)
                    return m_Factories;
                m_Factories = new List<IScreenKeyboardFactory>();
                foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
                {
                    foreach (var typeInfo in assembly.DefinedTypes)
                    {
                        if (!typeInfo.IsClass)
                            continue;

                        if (!typeof(IScreenKeyboardFactory).IsAssignableFrom(typeInfo))
                            continue;
                        m_Factories.Add((IScreenKeyboardFactory)Activator.CreateInstance(typeInfo));
                    }
                }

                return m_Factories;
            }
        }

        internal static int GetIndexOf(string factoryTypeName)
        {
            for (int i = 0; i < Factories.Count; i++)
            {
                if (Factories[i].GetType().AssemblyQualifiedName.Equals(factoryTypeName))
                    return i;
            }

            return -1;
        }
    }
}
