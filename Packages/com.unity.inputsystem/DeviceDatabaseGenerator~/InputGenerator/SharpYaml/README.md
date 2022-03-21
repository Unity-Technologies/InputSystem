# SharpYaml [![Build Status](https://github.com/xoofx/SharpYaml/workflows/ci/badge.svg?branch=master)](https://github.com/xoofx/SharpYaml/actions)  [![NuGet](https://img.shields.io/nuget/v/SharpYaml.svg)](https://www.nuget.org/packages/SharpYaml/)

**SharpYaml** is a .NET library that provides a **YAML parser and serialization engine** for .NET objects, **compatible with CoreCLR**.

## Usage

```C#
var serializer = new Serializer();
var text = serializer.Serialize(new { List = new List<int>() { 1, 2, 3 }, Name = "Hello", Value = "World!" });
Console.WriteLine(text);
```   
Output:

	List:
	  - 1
	  - 2
	  - 3
	Name: Hello
	Value: World!

## Features

SharpYaml is a fork of [YamlDotNet](http://www.aaubry.net/yamldotnet.aspx) and is adding the following features:

 - Supports for `.netstandard2.0`
 - Memory allocation and GC pressure improved
  - Completely rewritten serialization/deserialization engine
 - A single interface `IYamlSerializable` for implementing custom serializers, along `IYamlSerializableFactory` to allow dynamic creation of serializers. Registration can be done through `SerializerSettings.RegisterSerializer` and `SerializerSettings.RegisterSerializerFactory`
   - Can inherit from `ScalarSerializerBase` to provide custom serialization to/from a Yaml scalar 
 - Supports for custom collection that contains user properties to serialize along the collection.
 - Supports for Yaml 1.2 schemas 
 - A centralized type system through `ITypeDescriptor` and `IMemberDescriptor`
 - Highly configurable serialization using `SerializerSettings` (see usage)
   - Add supports to register custom attributes on external objects (objects that you can't modify) by using `SerializerSettings.Register(memberInfo, attribute)`
   - Several options and settings: `EmitAlias`, `IndentLess`, `SortKeyForMapping`, `EmitJsonComptible`, `EmitCapacityForList`, `LimitPrimitiveFlowSequence`, `EmitDefaultValues`
   - Add supports for overriding the Yaml style of serialization (block or flow) with `SerializerSettings.DefaultStyle` and `SerializerSettings.DynamicStyleFormat`  
 - Supports for registering an assembly when discovering types to deserialize through `SerializerSettings.RegisterAssembly`
 - Supports a `IObjectSerializerBackend` that allows to hook a global rewriting for all YAML serialization types (scalar, sequence, mapping) when serializing/deserializing to/from a .NET type.
 
## Download

SharpYaml is available on [![NuGet](https://img.shields.io/nuget/v/SharpYaml.svg)](https://www.nuget.org/packages/SharpYaml/)

## License
MIT