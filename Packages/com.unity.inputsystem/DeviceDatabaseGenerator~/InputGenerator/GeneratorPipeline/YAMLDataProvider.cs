#if true
using System.IO;
using SharpYaml.Serialization;

namespace Unity.InputSystem.DeviceDatabase.IR
{
    internal static class YAMLDataProvider
    {
        public static IRDeviceDatabase ParseYamlString(string yamlString)
        {
            return new Serializer().Deserialize<IRDeviceDatabase>(yamlString);
        }
    }
}
#endif
