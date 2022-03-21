using System;
using System.Linq;

namespace Unity.InputSystem.DeviceDatabase.IR
{
    internal class ReflectionDataProvider
    {
        public static string[] FindAllAssembliesInlineDatabaseAttributes()
        {
            return AppDomain
                .CurrentDomain
                .GetAssemblies()
                .SelectMany(x => x.GetCustomAttributes(true))
                .OfType<InputInlineDeviceDatabaseAttribute>()
                .OrderBy(x => x.Priority)
                .Select(x => x.Value)
                .ToArray();
        }
    }
}