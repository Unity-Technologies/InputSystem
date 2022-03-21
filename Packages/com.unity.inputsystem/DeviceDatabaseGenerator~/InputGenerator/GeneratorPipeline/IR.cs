using System;
using System.Linq;

namespace Unity.InputSystem.DeviceDatabase.IR
{
    public struct IRControlInstance
    {
        public Guid guid;
        public string name;
        public string displayName;
        public string doc;
        public string type;
        
        // --------------------- below are variables assigned by the IR pipeline
        
        // offset of what assignedRef of virtual control instance must be in relation to parent assignedRef
        // TODO maybe it makes sense to make internal if we gonna code generate control types?
        public int virtualControlRelativeIndex;

        // as in format "Trait_ControlNameControlType{_VirtualControlNameVirtualControlType}"
        public string fullyQualifiedName;

        // as in format "Trait/ControlNameControlType{/VirtualControlNameVirtualControlType}"
        public string editorFriendlyFullyQualifiedName;

        // database build time id, not stable between builds
        public int assignedRef;

        // value of IRControlType.assignedRef for this controls' type
        public int typeRef;

        // if this is virtual control, contains value of parent control fully qualified name
        // otherwise empty string ("", not null, because Liquid wants empty strings)
        public string parentOfVirtualControlFullyQualifiedName;
        
        public string guidAsPartA => guid.ToA();
        public string guidAsPartB => guid.ToB();
        public object databaseInstance;

        public string nameStartingWithLowerCase => name.FirstCharToLowerCase();

        public IRControlType assignedControlType
        {
            get
            {
                var typeRefCopy = typeRef;
                return ((IRDeviceDatabase)databaseInstance).controlTypes.First(x => x.assignedRef == typeRefCopy);
            }
        }
    }
    
    public struct IRControlsInstancesGroupedByType
    {
        public string type;
        public IRControlInstance[] controls;
    }
    
    public enum IRInputControlRecordingMode
    {
        NotDeserialized = 0, // special one, so we can set a default later on 
        Disabled,
        LatestOnly,
        AllMerged,
        AllAsIs,
    }

    public struct IRControlType
    {
        public Guid guid;
        public string name;
        public string displayName;
        public string doc;
        public bool nativeVisible;
        public string sampleTypeName;
        public string sampleTypeNameAlias; // for samples we want strongly defined types, but for ergonomics we want basic types e.g. button == bool
        public string stateTypeName;
        public string ingressFunctionName;
        public string frameBeginFunctionName;
        // TODO add default!!!
        public IRInputControlRecordingMode defaultRecordingMode;
        public IRControlInstance[] virtualControls;

        // --------------------- below are variables assigned by the IR pipeline

        public int assignedRef; // database build time id, not stable between builds

        public IRControlsInstancesGroupedByType[] virtualControlsByType =>
            virtualControls
                .GroupBy(x => x.type)
                .Select(x => new IRControlsInstancesGroupedByType{type = x.Key, controls = x.ToArray()})
                .ToArray();
        public string guidAsPartA => guid.ToA();
        public string guidAsPartB => guid.ToB();
    }

    public struct IRDeviceTraitMethodArgument
    {
        public string name;
        public string type;

        // --------------------- below are variables assigned by the IR pipeline

        public string cppType => type; // TODO
        public string csharpType => type; // TODO
    }

    public struct IRDeviceTraitMethod
    {
        public string name;
        public string doc;

        public enum Implementation
        {
            ManagedOrNative,
            ManagedOnly,
        }

        public Implementation implementation;
        public IRDeviceTraitMethodArgument[] args;

        // --------------------- below are variables assigned by the IR pipeline

        public string cppName => name.FirstCharToLowerCase();
        public string cppArgs => string.Join(", ", args.Select(x => $"{x.cppType} {x.name}"));

        public string csharpName => name;
        public string csharpArgsNames => string.Join(", ", args.Select(x => $"{x.name}"));
        public string csharpArgsTypes => string.Join(", ", args.Select(x => $"{x.csharpType}"));
        public string csharpArgsTypesNames => string.Join(", ", args.Select(x => $"{x.csharpType} {x.name}"));
    }

    public struct IRDeviceTrait
    {
        public Guid guid;
        public string name;
        public string displayName;
        public string doc;
        public bool nativeVisible;
        public bool controlsAreOptional;
        public IRControlInstance[] controls;
        public IRDeviceTraitMethod[] methods;

        // --------------------- below are variables assigned by the IR pipeline

        //internal int sizeInBytes; // do we need this? or we gonna sizeof the type in the code?
        public int assignedRef;
        public IRControlInstance[] flattenControls;
        
        public IRControlsInstancesGroupedByType[] controlsByType =>
            controls
                .GroupBy(x => x.type)
                .Select(x => new IRControlsInstancesGroupedByType{type = x.Key, controls = x.ToArray()})
                .ToArray();

        public string guidAsPartA => guid.ToA();
        public string guidAsPartB => guid.ToB();
    }

    // internal struct IRDeviceExtraProcessors
    // {
    //     
    // }

    public struct IRDevice
    {
        public Guid guid;
        public string name;
        public string displayName;
        public string doc;
        public string[] traits;

        // for remapping later on
        // public int usbVid;
        // public int usbPid;
        
        // --------------------- below are variables assigned by the IR pipeline

        public int assignedRef;
        public string guidAsPartA => guid.ToA();
        public string guidAsPartB => guid.ToB();
    }

    public struct IRDeviceDatabase
    {
        public IRControlType[] controlTypes;
        public IRDeviceTrait[] deviceTraits;
        public IRDevice[] devices;

        // --------------------- below are variables assigned by the IR pipeline

        public IRControlInstance[] allFlattenControls =>
            deviceTraits
                .SelectMany(x => x.flattenControls)
                .ToArray();

        public int maxDeviceAssignedRef => devices.Select(x => x.assignedRef).Max();
        public int maxDeviceTraitAssignedRef => deviceTraits.Select(x => x.assignedRef).Max();
        public int maxControlUsageAssignedRef => allFlattenControls.Select(x => x.assignedRef).Max();
        public int maxControlTypeAssignedRef => controlTypes.Select(x => x.assignedRef).Max();
    }
    
    public static class IRHelperExtensions
    {
        public static string FirstCharToLowerCase(this string str)
        {
            if (string.IsNullOrEmpty(str) || char.IsLower(str[0]))
                return str;

            return char.ToLower(str[0]) + str.Substring(1);
        }

        public static unsafe string ToA(this Guid guid)
        {
            ulong r = 0;
            var ptr = (byte*) &r;
            var bytes = guid.ToByteArray();
            // TODO guids have first 8 bytes stored as uint ushort ushort and we need to do big-little endian conversion
            // instead we should massage native code to follow this
            ptr[0] = bytes[3];
            ptr[1] = bytes[2];
            ptr[2] = bytes[1];
            ptr[3] = bytes[0];
            ptr[4] = bytes[5];
            ptr[5] = bytes[4];
            ptr[6] = bytes[7];
            ptr[7] = bytes[6];
            return $"0x{r:x16}";
        }

        public static unsafe string ToB(this Guid guid)
        {
            ulong r = 0;
            var ptr = (byte*) &r;
            var bytes = guid.ToByteArray();
            for (var i = 0; i < 8; ++i)
                ptr[i] = bytes[i + 8];
            return $"0x{r:x16}";
        }
    }
}