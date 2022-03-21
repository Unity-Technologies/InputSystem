using System.IO;

namespace Unity.InputSystem.DeviceDatabase.IR
{
    public class GeneratorBuilder
    {
        private IRPipeline _pipeline = new IRPipeline();
        private IRDeviceDatabase _db = new IRDeviceDatabase { };

        public GeneratorBuilder AddYAMLString(string yamlString)
        {
            var db = YAMLDataProvider.ParseYamlString(yamlString);
            _pipeline.Ingress(db);
            return this;
        }

        public GeneratorBuilder AddViaReflection()
        {
            foreach (var yamlString in ReflectionDataProvider.FindAllAssembliesInlineDatabaseAttributes())
                AddYAMLString(yamlString);
            return this;
        }

        public GeneratorBuilder Build()
        {
            _db = _pipeline.Build();
            return this;
        }

        public string GenerateNativeSourceCode()
        {
            return NativeCodeGenerator.Generate(_db);
        }
        
        public string GenerateManagedSourceCode()
        {
            return ManagedCodeGenerator.Generate(_db);
        }
    }
}