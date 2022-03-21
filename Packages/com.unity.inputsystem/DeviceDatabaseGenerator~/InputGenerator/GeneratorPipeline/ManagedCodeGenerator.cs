namespace Unity.InputSystem.DeviceDatabase.IR
{
    internal static class ManagedCodeGenerator
    {

        public static string Generate(IRDeviceDatabase database)
        {
            var template = Scriban.Template.ParseLiquid(@"
// Auto generated. Do not edit.

using System.Runtime.CompilerServices;

using Unity.InputSystem.Runtime;
using static Unity.InputSystem.Runtime.Native;

// TODO kill this, using it just for now while I'm working on control types
using uint8_t = System.Byte;

namespace Unity.InputSystem
{
    public struct InputSourceGeneratorDevelopmentTag { } // meta tag to find in which file generated source is

    // -------------–––-------------–––-------------–––-------------–––-------------–––-------------–––-------------–––-----
    // Enums
    // -------------–––-------------–––-------------–––-------------–––-------------–––-------------–––-------------–––-----

    internal enum kInputControlType
    {
        Invalid = 0,
{%- for x in controlTypes %}
        {{x.name}} = {{x.assignedRef}}, // '{{x.displayName}}'
{%- endfor %}
    };

    internal enum kInputDeviceTrait
    {
        Invalid = 0,
{%- for x in deviceTraits %}
        {{x.name}} = {{x.assignedRef}}, // '{{x.displayName}}'
{%- endfor %}
    };

    internal enum kInputControlUsage
    {
        Invalid = 0,
{%- for x in allFlattenControls %}
        {{x.fullyQualifiedName}} = {{x.assignedRef}}, // '{{x.editorFriendlyFullyQualifiedName}}'
{%- endfor %}
    };

    internal enum kInputDevice
    {
        Invalid = 0,
{%- for x in devices %}
        {{x.name}} = {{x.assignedRef}}, // '{{x.displayName}}'
{%- endfor %}
    }

    // -------------–––-------------–––-------------–––-------------–––-------------–––-------------–––-------------–––-----
    // Control Types
    // -------------–––-------------–––-------------–––-------------–––-------------–––-------------–––-------------–––-----

{% for x in controlTypes %}
    public partial struct Input{{x.name}}Control : IInputControl // {{x.displayName}}
    {
        public static readonly InputControlTypeRef ControlTypeRef = new InputControlTypeRef { transparent = (uint)kInputControlType.{{x.name}} };
        public InputControlTypeRef controlTypeRef => ControlTypeRef;

        internal InputControlRef _controlRef;
        public InputControlRef controlRef => _controlRef;

{% unless x.sampleTypeNameAlias == null %}
        public {{x.sampleTypeNameAlias}} value => GetLatestSample().sample;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public unsafe void Ingress({{x.sampleTypeNameAlias}} sample)
        {
            var timestamp = new InputControlTimestamp {};
            InputGetCurrentTime(&timestamp);
            var sampleTyped = ({{x.sampleTypeName}})sample; 
            {{x.ingressFunctionName}}(ControlTypeRef, controlRef, controlTypeRef, &timestamp, &sampleTyped, 1, InputControlRefInvalid);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public unsafe void Ingress(InputControlTimestamp timestamp, {{x.sampleTypeNameAlias}} sample)
        {
            var sampleTyped = ({{x.sampleTypeName}})sample; 
            {{x.ingressFunctionName}}(ControlTypeRef, controlRef, controlTypeRef, &timestamp, &sampleTyped, 1, InputControlRefInvalid);
        }
{% endunless %}

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public unsafe void Ingress({{x.sampleTypeName}} sample)
        {
            var timestamp = new InputControlTimestamp {};
            InputGetCurrentTime(&timestamp);
            {{x.ingressFunctionName}}(ControlTypeRef, controlRef, controlTypeRef, &timestamp, &sample, 1, InputControlRefInvalid);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public unsafe void Ingress(InputControlTimestamp timestamp, {{x.sampleTypeName}} sample)
        {
            {{x.ingressFunctionName}}(ControlTypeRef, controlRef, controlTypeRef, &timestamp, &sample, 1, InputControlRefInvalid);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public unsafe void Ingress(InputControlTimestamp* timestamps, {{x.sampleTypeName}}* samples, uint count)
        {
            {{x.ingressFunctionName}}(ControlTypeRef, controlRef, controlTypeRef, timestamps, samples, count, InputControlRefInvalid);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public unsafe void IngressFrom<T>(T fromControl, InputControlTimestamp* timestamps, {{x.sampleTypeName}}* samples, uint count) where T: unmanaged, IInputControl
        {
            {{x.ingressFunctionName}}(fromControl.controlTypeRef, controlRef, controlTypeRef, timestamps, samples, count, fromControl.controlRef);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public unsafe {{x.stateTypeName}} GetState()
        {
            return GetState(InputCurrentAPIContext.CurrentFramebuffer);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public unsafe {{x.stateTypeName}} GetState(InputFramebufferRef framebufferRef)
        {
            var v = new InputControlVisitorGenericState {};
            InputGetControlVisitorGenericState(controlRef, framebufferRef, &v);
            return *({{x.stateTypeName}}*)v.controlState;
        }

        public struct LatestSample
        {
            public InputControlTimestamp timestamp;
            public {{x.sampleTypeName}} sample;
        };

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public unsafe LatestSample GetLatestSample()
        {
            return GetLatestSample(InputCurrentAPIContext.CurrentFramebuffer);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public unsafe LatestSample GetLatestSample(InputFramebufferRef framebufferRef)
        {
            var v = new InputControlVisitorGenericState {};
            InputGetControlVisitorGenericState(controlRef, framebufferRef, &v);
            return new LatestSample {
                timestamp = *v.latestRecordedTimestamp,
                sample = *({{x.sampleTypeName}}*)v.latestRecordedSample
            };
        }

        public unsafe struct Recording
        {
            public InputControlTimestamp* timestamps;
            public {{x.sampleTypeName}}* samples;
            public uint count;
        };

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public unsafe Recording GetRecording()
        {
            return GetRecording(InputCurrentAPIContext.CurrentFramebuffer);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public unsafe Recording GetRecording(InputFramebufferRef framebufferRef)
        {
            var v = new InputControlVisitorGenericRecordings {};
            InputGetControlVisitorGenericRecordings(controlRef, framebufferRef, &v);
            return new Recording {
                timestamps = v.allRecordedTimestamps,
                samples = ({{x.sampleTypeName}}*)v.allRecordedSamples,
                count = v.allRecordedCount
            };
        }
    
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static Input{{x.name}}Control Setup(InputControlRef controlRef)
        {
            return new Input{{x.name}}Control { _controlRef = controlRef };
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static Input{{x.name}}Control Setup(InputControlUsage controlUsage, InputDeviceRef deviceRef)
        {
            return new Input{{x.name}}Control { _controlRef = InputControlRef.Setup(controlUsage, deviceRef) };
        }

{% for y in x.virtualControls %}
        public InputDerived{{y.type}}Control {{y.nameStartingWithLowerCase}}{{y.type}} => InputDerived{{y.type}}Control.Setup( new InputControlUsage { transparent = controlRef.usage.transparent + {{y.virtualControlRelativeIndex}} }, controlRef.deviceRef); 
{%- endfor %}
{% for y in x.virtualControlsByType %}
        public enum {{y.type}}s
        {
{%- for z in y.controls %}
            {{z.name}},
{%- endfor %}
        };

        public InputDerived{{y.type}}Control this[{{y.type}}s value]
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                switch(value)
                {
{%- for z in y.controls %}
                    case {{y.type}}s.{{z.name}}: return {{z.nameStartingWithLowerCase}}{{z.type}};
{%- endfor %}
                    default: return default; // TODO fix me
                }
            }
        }
{%- endfor %}
    }

    public partial struct InputDerived{{x.name}}Control : IInputControl // {{x.displayName}}
    {
        public static readonly InputControlTypeRef ControlTypeRef = new InputControlTypeRef { transparent = (uint)kInputControlType.{{x.name}} };
        public InputControlTypeRef controlTypeRef => ControlTypeRef;

        internal InputControlRef _controlRef;
        public InputControlRef controlRef => _controlRef;

{% unless x.sampleTypeNameAlias == null %}
        public {{x.sampleTypeNameAlias}} value => GetLatestSample().sample;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public unsafe void Ingress({{x.sampleTypeNameAlias}} sample)
        {
            var timestamp = new InputControlTimestamp {};
            InputGetCurrentTime(&timestamp);
            var sampleTyped = ({{x.sampleTypeName}})sample; 
            {{x.ingressFunctionName}}(ControlTypeRef, controlRef, controlTypeRef, &timestamp, &sampleTyped, 1, InputControlRefInvalid);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public unsafe void Ingress(InputControlTimestamp timestamp, {{x.sampleTypeNameAlias}} sample)
        {
            var sampleTyped = ({{x.sampleTypeName}})sample; 
            {{x.ingressFunctionName}}(ControlTypeRef, controlRef, controlTypeRef, &timestamp, &sampleTyped, 1, InputControlRefInvalid);
        }
{% endunless %}

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public unsafe void Ingress({{x.sampleTypeName}} sample)
        {
            var timestamp = new InputControlTimestamp {};
            InputGetCurrentTime(&timestamp);
            {{x.ingressFunctionName}}(ControlTypeRef, controlRef, controlTypeRef, &timestamp, &sample, 1, InputControlRefInvalid);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public unsafe void Ingress(InputControlTimestamp timestamp, {{x.sampleTypeName}} sample)
        {
            {{x.ingressFunctionName}}(ControlTypeRef, controlRef, controlTypeRef, &timestamp, &sample, 1, InputControlRefInvalid);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public unsafe void Ingress(InputControlTimestamp* timestamps, {{x.sampleTypeName}}* samples, uint count)
        {
            {{x.ingressFunctionName}}(ControlTypeRef, controlRef, controlTypeRef, timestamps, samples, count, InputControlRefInvalid);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public unsafe void IngressFrom<T>(T fromControl, InputControlTimestamp* timestamps, {{x.sampleTypeName}}* samples, uint count) where T: unmanaged, IInputControl
        {
            {{x.ingressFunctionName}}(fromControl.controlTypeRef, controlRef, controlTypeRef, timestamps, samples, count, fromControl.controlRef);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public unsafe {{x.stateTypeName}} GetState()
        {
            return GetState(InputCurrentAPIContext.CurrentFramebuffer);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public unsafe {{x.stateTypeName}} GetState(InputFramebufferRef framebufferRef)
        {
            var v = new InputControlVisitorGenericState {};
            InputGetControlVisitorGenericState(controlRef, framebufferRef, &v);
            return *({{x.stateTypeName}}*)v.controlState;
        }

        public struct LatestSample
        {
            public InputControlTimestamp timestamp;
            public {{x.sampleTypeName}} sample;
        };

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public unsafe LatestSample GetLatestSample()
        {
            return GetLatestSample(InputCurrentAPIContext.CurrentFramebuffer);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public unsafe LatestSample GetLatestSample(InputFramebufferRef framebufferRef)
        {
            var v = new InputControlVisitorGenericState {};
            InputGetControlVisitorGenericState(controlRef, framebufferRef, &v);
            return new LatestSample {
                timestamp = *v.latestRecordedTimestamp,
                sample = *({{x.sampleTypeName}}*)v.latestRecordedSample
            };
        }

        public unsafe struct Recording
        {
            public InputControlTimestamp* timestamps;
            public {{x.sampleTypeName}}* samples;
            public uint count;
        };

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public unsafe Recording GetRecording()
        {
            return GetRecording(InputCurrentAPIContext.CurrentFramebuffer);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public unsafe Recording GetRecording(InputFramebufferRef framebufferRef)
        {
            var v = new InputControlVisitorGenericRecordings {};
            InputGetControlVisitorGenericRecordings(controlRef, framebufferRef, &v);
            return new Recording {
                timestamps = v.allRecordedTimestamps,
                samples = ({{x.sampleTypeName}}*)v.allRecordedSamples,
                count = v.allRecordedCount
            };
        }
    
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static InputDerived{{x.name}}Control Setup(InputControlRef controlRef)
        {
            return new InputDerived{{x.name}}Control { _controlRef = controlRef };
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static InputDerived{{x.name}}Control Setup(InputControlUsage controlUsage, InputDeviceRef deviceRef)
        {
            return new InputDerived{{x.name}}Control { _controlRef = InputControlRef.Setup(controlUsage, deviceRef) };
        }
    }
{%- endfor %}

    // -------------–––-------------–––-------------–––-------------–––-------------–––-------------–––-------------–––-----
    // Device Traits
    // -------------–––-------------–––-------------–––-------------–––-------------–––-------------–––-------------–––-----
{% for x in deviceTraits %}
    public partial struct Input{{x.name}} : IInputDeviceTrait
    {
        public static readonly InputDeviceTraitRef TraitRef = new InputDeviceTraitRef { transparent = (uint)kInputDeviceTrait.{{x.name}} };
        public InputDeviceTraitRef traitRef => TraitRef;

        internal InputDeviceRef _deviceRef;
        public InputDeviceRef deviceRef => _deviceRef;

{% for y in x.methods %}
{% if y.csharpArgsTypes != """" %}
        public unsafe delegate* unmanaged[Cdecl]<{{ y.csharpArgsTypes }}, void> _{{ y.csharpName }};
{% else %}
        public unsafe delegate* unmanaged[Cdecl]<void> _{{ y.csharpName }};
{% endif %}
{%- endfor %}
{% for y in x.methods %}
        public unsafe void {{y.csharpName}}({{ y.csharpArgsTypesNames }})
        {
            _{{ y.csharpName }}({{ y.csharpArgsNames }});
        }
{%- endfor %}

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Input{{x.name}} Setup(InputDeviceRef deviceRef)
        {
            // TODO assert that devices has the trait
            return new Input{{x.name}} {
                _deviceRef = deviceRef,
{%- for y in x.methods %}
                _{{ y.csharpName }} = null,
{%- endfor %}
            };
        }

{% for y in x.controls %}
        public Input{{y.type}}Control {{y.nameStartingWithLowerCase}}{{y.type}} => Input{{y.type}}Control.Setup(new InputControlUsage { transparent = (int)kInputControlUsage.{{y.fullyQualifiedName}} }, deviceRef);
{%- endfor %}
{% for y in x.controlsByType %}
        public enum {{y.type}}s
        {
{%- for z in y.controls %}
            {{z.name}},
{%- endfor %}
        };

        public Input{{y.type}}Control this[{{y.type}}s value]
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                switch(value)
                {
{%- for z in y.controls %}
                    case {{y.type}}s.{{z.name}}: return {{z.nameStartingWithLowerCase}}{{z.type}};
{%- endfor %}
                    default: return default; // TODO fix me
                }
            }
        }
{% endfor -%}
    }
{% endfor %}

    internal unsafe struct InputDeviceDatabaseGenerated
    {
        #if ENABLE_MONO || ENABLE_IL2CPP
        [AOT.MonoPInvokeCallback(typeof(DatabaseCallbacksContainer.ControlTypeIngress))]
        #endif
        public static void ControlTypeIngress(
            InputControlTypeRef controlTypeRef,
            InputControlRef controlRef,
            InputControlTypeRef samplesType,
            InputControlTimestamp* timestamps,
            void* samples,
            uint count,
            InputControlRef fromAnotherControl
        )
        {
            switch((kInputControlType)controlTypeRef.transparent)
            {
            case kInputControlType.Invalid: break;
{%- for x in controlTypes %}
            case kInputControlType.{{x.name}}: {{x.ingressFunctionName}}(controlTypeRef, controlRef, samplesType, timestamps, samples, count, fromAnotherControl); break;
{%- endfor %}
            default:
                // throw new (false, ""Trying to ingress to unknown type"");
                break;
            }
        }

        #if ENABLE_MONO || ENABLE_IL2CPP
        [AOT.MonoPInvokeCallback(typeof(DatabaseCallbacksContainer.ControlTypeFrameBegin))]
        #endif
        public static void ControlTypeFrameBegin(
            InputControlTypeRef controlTypeRef,
            InputControlRef* controlRefs,
            void* controlStates,
            InputControlTimestamp* latestRecordedTimestamps,
            void* latestRecordedSamples,
            uint controlCount
        )
        {
            // TODO type check pointer conversion!
            switch((kInputControlType)controlTypeRef.transparent)
            {
            case kInputControlType.Invalid: break;
{%- for x in controlTypes %}
            case kInputControlType.{{x.name}}: {{x.frameBeginFunctionName}}(controlTypeRef, controlRefs, ({{x.stateTypeName}}*)controlStates, latestRecordedTimestamps, ({{x.sampleTypeName}}*)latestRecordedSamples, controlCount); break;
{%- endfor %}
            default:
                // throw new (false, ""Trying to frame begin unknown type"");
                break;
            }
        }

        #if ENABLE_MONO || ENABLE_IL2CPP
        [AOT.MonoPInvokeCallback(typeof(DatabaseCallbacksContainer.GetDeviceTraits))]
        #endif
        public static uint GetDeviceTraits(InputDatabaseDeviceAssignedRef assignedRef, InputDeviceTraitRef* o, uint count) 
        {
            switch((kInputDevice)assignedRef._opaque)
            {
{%- for x in devices %}
            case kInputDevice.{{ x.name }}:
                if (o != null && count == {{x.traits | size}})
                {
{%- for yi in 0..(x.traits | size | minus 1) %}
                    o[{{yi}}] = new InputDeviceTraitRef { transparent = (uint)kInputDeviceTrait.{{x.traits[yi]}} };
{%- endfor %}
                }
                else if (o != null)
                {
                    // InputAssert(false, ""Please provide {{x.traits | size}} elements"");
                }
                return {{x.traits | size}};
{%- endfor %}
            default:
                return 0;
            }
        }

        #if ENABLE_MONO || ENABLE_IL2CPP
        [AOT.MonoPInvokeCallback(typeof(DatabaseCallbacksContainer.GetTraitSizeInBytes))]
        #endif
        public static uint GetTraitSizeInBytes(InputDeviceTraitRef traitRef) 
        {
            switch((kInputDeviceTrait)traitRef.transparent)
            {
{%- for x in deviceTraits %}
            case kInputDeviceTrait.{{x.name}}: return (uint)sizeof(Input{{x.name}});
{%- endfor %}
            default: return 0;
            }
        }

        #if ENABLE_MONO || ENABLE_IL2CPP
        [AOT.MonoPInvokeCallback(typeof(DatabaseCallbacksContainer.GetTraitControls))]
        #endif
        public static uint GetTraitControls(InputDeviceTraitRef traitRef, InputDeviceRef deviceRef, InputControlRef* o, uint count)
        {
            switch((kInputDeviceTrait)traitRef.transparent)
            {
{%- for x in deviceTraits %}
            case kInputDeviceTrait.{{x.name}}:
                if (o != null && count == {{x.flattenControls | size}})
                {
{%- unless (x.flattenControls | size) == 0 -%}
{%- for yi in 0..(x.flattenControls | size | minus 1) %}
                    o[{{yi}}] = InputControlRef.Setup( new InputControlUsage { transparent = (uint)kInputControlUsage.{{x.flattenControls[yi].fullyQualifiedName}} }, deviceRef);
{%- endfor %}
{%- endunless -%}
                }
                else if (o != null)
                {
                    // throw new InputAssert(false, ""Please provide {{x.flattenControls | size}} elements"");
                }
                return {{x.flattenControls | size}};
{%- endfor %}
            default: return 0;
            }
        }

        #if ENABLE_MONO || ENABLE_IL2CPP
        [AOT.MonoPInvokeCallback(typeof(DatabaseCallbacksContainer.ConfigureTraitInstance))]
        #endif
        public static void ConfigureTraitInstance(InputDeviceTraitRef traitRef, void* traitPointer, InputDeviceRef deviceRef)
        {
            switch((kInputDeviceTrait)traitRef.transparent)
            {
{%- for x in deviceTraits %}
            case kInputDeviceTrait.{{x.name}}: *((Input{{x.name}}*)traitPointer) = Input{{x.name}}.Setup(deviceRef); break;
{%- endfor %}
            default: break;
            }
        }

        #if ENABLE_MONO || ENABLE_IL2CPP
        [AOT.MonoPInvokeCallback(typeof(DatabaseCallbacksContainer.GetControlUsageDescr))]
        #endif
        public static InputDatabaseControlUsageDescr GetControlUsageDescr(InputControlUsage usage)
        {
            switch((kInputControlUsage)usage.transparent)
            {
{%- for x in allFlattenControls %}
            case kInputControlUsage.{{x.fullyQualifiedName}}: return new InputDatabaseControlUsageDescr {
                controlTypeRef = new InputControlTypeRef { transparent = (uint)kInputControlType.{{x.assignedControlType.name}} },
                defaultRecordingMode = InputControlRecordingMode.{{x.assignedControlType.defaultRecordingMode}},
{%- unless x.parentOfVirtualControlFullyQualifiedName == """" %}
                parentOfVirtualControl = new InputControlUsage { transparent = (uint)kInputControlUsage.{{x.parentOfVirtualControlFullyQualifiedName}} }
{% else %}
                parentOfVirtualControl = InputControlUsageInvalid
{% endunless %}
            };
{%- endfor %}
            default:
                return new InputDatabaseControlUsageDescr {};
            }
        }

        #if ENABLE_MONO || ENABLE_IL2CPP
        [AOT.MonoPInvokeCallback(typeof(DatabaseCallbacksContainer.GetControlTypeDescr))]
        #endif
        public static InputDatabaseControlTypeDescr GetControlTypeDescr(InputControlTypeRef controlTypeRef)
        {
            switch((kInputControlType)controlTypeRef.transparent)
            {
{%- for x in controlTypes %}
            case kInputControlType.{{x.name}}: return new InputDatabaseControlTypeDescr {
                stateSizeInBytes = (uint)sizeof({{x.stateTypeName}}),
                sampleSizeInBytes = (uint)sizeof({{x.sampleTypeName}})
            };
{%- endfor %}
            default:
                return new InputDatabaseControlTypeDescr {};
            }
        }

        // TODO replace guid to id lookups with hashmap
        #if ENABLE_MONO || ENABLE_IL2CPP
        [AOT.MonoPInvokeCallback(typeof(DatabaseCallbacksContainer.GetDeviceAssignedRef))]
        #endif
        public static InputDatabaseDeviceAssignedRef GetDeviceAssignedRef(InputGuid g)
        {
{%- for x in devices %}
            if (g.a == {{x.guidAsPartA}}ul && g.b == {{x.guidAsPartB}}ul) return new InputDatabaseDeviceAssignedRef { _opaque = (uint)kInputDevice.{{x.name}} }; // {{x.guid}}
{%- endfor %}
            return InputDatabaseDeviceAssignedRefInvalid;
        }

        #if ENABLE_MONO || ENABLE_IL2CPP
        [AOT.MonoPInvokeCallback(typeof(DatabaseCallbacksContainer.GetTraitAssignedRef))]
        #endif
        public static InputDeviceTraitRef GetTraitAssignedRef(InputGuid g)
        {
{%- for x in deviceTraits %}
            if (g.a == {{x.guidAsPartA}}ul && g.b == {{x.guidAsPartB}}ul) return new InputDeviceTraitRef { transparent = (uint)kInputDeviceTrait.{{x.name}} }; // {{x.guid}}
{%- endfor %}
            return InputDeviceTraitRefInvalid;
        }

        #if ENABLE_MONO || ENABLE_IL2CPP
        [AOT.MonoPInvokeCallback(typeof(DatabaseCallbacksContainer.GetControlUsage))]
        #endif
        public static InputControlUsage GetControlUsage(InputGuid g)
        {
{%- for x in allFlattenControls %}
            if (g.a == {{x.guidAsPartA}}ul && g.b == {{x.guidAsPartB}}ul) return new InputControlUsage { transparent = (uint)kInputControlUsage.{{x.fullyQualifiedName}} }; // {{x.guid}}
{%- endfor %}
            return InputControlUsageInvalid;
        }

        #if ENABLE_MONO || ENABLE_IL2CPP
        [AOT.MonoPInvokeCallback(typeof(DatabaseCallbacksContainer.GetControlTypeRef))]
        #endif
        public static InputControlTypeRef GetControlTypeRef(InputGuid g)
        {
{%- for x in controlTypes %}
            if (g.a == {{x.guidAsPartA}}ul && g.b == {{x.guidAsPartB}}ul) return new InputControlTypeRef { transparent = (uint)kInputControlType.{{x.name}} }; // {{x.guid}}
{%- endfor %}
            return InputControlTypeRefInvalid;
        }

        #if ENABLE_MONO || ENABLE_IL2CPP
        [AOT.MonoPInvokeCallback(typeof(DatabaseCallbacksContainer.GetDeviceGuid))]
        #endif
        public static InputGuid GetDeviceGuid(InputDatabaseDeviceAssignedRef assignedRef)
        {
            switch((kInputDevice)assignedRef._opaque)
            {
{%- for x in devices %}
            case kInputDevice.{{ x.name }}: return new InputGuid { a = {{x.guidAsPartA}}ul, b = {{x.guidAsPartB}}ul }; // {{x.guid}} '{{x.displayName}}'
{%- endfor %}
            default:
                return InputGuidInvalid;
            }
        }

        #if ENABLE_MONO || ENABLE_IL2CPP
        [AOT.MonoPInvokeCallback(typeof(DatabaseCallbacksContainer.GetTraitGuid))]
        #endif
        public static InputGuid GetTraitGuid(InputDeviceTraitRef traitRef)
        {
            switch((kInputDeviceTrait)traitRef.transparent)
            {
{%- for x in deviceTraits %}
            case kInputDeviceTrait.{{x.name}}: return new InputGuid { a = {{x.guidAsPartA}}ul, b = {{x.guidAsPartB}}ul }; // {{x.guid}}
{%- endfor %}
            default:
                return InputGuidInvalid;
            }
        }

        #if ENABLE_MONO || ENABLE_IL2CPP
        [AOT.MonoPInvokeCallback(typeof(DatabaseCallbacksContainer.GetControlGuid))]
        #endif
        public static InputGuid GetControlGuid(InputControlUsage usage)
        {
            switch((kInputControlUsage)usage.transparent)
            {
{%- for x in allFlattenControls %}
            case kInputControlUsage.{{x.fullyQualifiedName}}: return new InputGuid { a = {{x.guidAsPartA}}ul, b = {{x.guidAsPartB}}ul }; // {{x.guid}}
{%- endfor %}
            default:
                return InputGuidInvalid;
            }
        }

        #if ENABLE_MONO || ENABLE_IL2CPP
        [AOT.MonoPInvokeCallback(typeof(DatabaseCallbacksContainer.GetControlTypeGuid))]
        #endif
        public static InputGuid GetControlTypeGuid(InputControlTypeRef controlTypeRef)
        {
            switch((kInputControlType)controlTypeRef.transparent)
            {
{%- for x in controlTypes %}
            case kInputControlType.{{x.name}}: return new InputGuid { a = {{x.guidAsPartA}}ul, b = {{x.guidAsPartB}}ul }; // {{x.guid}}
{%- endfor %}
            default:
                return InputGuidInvalid;
            }
        }

        #if ENABLE_MONO || ENABLE_IL2CPP
        [AOT.MonoPInvokeCallback(typeof(DatabaseCallbacksContainer.GetDeviceRefCount))]
        #endif
        public static uint GetDeviceRefCount()
        {
            return {{maxDeviceAssignedRef}} + 1; // max value + 1
        }

        #if ENABLE_MONO || ENABLE_IL2CPP
        [AOT.MonoPInvokeCallback(typeof(DatabaseCallbacksContainer.GetTraitRefCount))]
        #endif
        public static uint GetTraitRefCount()
        {
            return {{maxDeviceTraitAssignedRef}} + 1; // max value + 1
        }

        #if ENABLE_MONO || ENABLE_IL2CPP
        [AOT.MonoPInvokeCallback(typeof(DatabaseCallbacksContainer.GetControlUsageCount))]
        #endif
        public static uint GetControlUsageCount()
        {
            return {{maxControlUsageAssignedRef}} + 1; // max value + 1
        }

        #if ENABLE_MONO || ENABLE_IL2CPP
        [AOT.MonoPInvokeCallback(typeof(DatabaseCallbacksContainer.GetControlTypeCount))]
        #endif
        public static uint GetControlTypeCount()
        {
            return {{maxControlTypeAssignedRef}} + 1; // max value + 1
        }

        // TODO this is not burstable, fix it
        private static uint _InputStrToBuf(sbyte* buffer, uint bufferCount, string str)
        {
            byte[] bytes = System.Text.Encoding.UTF8.GetBytes(str);
            uint written = 0;
            for(var i = 0; i < bytes.Length && i < (int)(bufferCount - 1); ++i, ++written)
                buffer[i] = (sbyte)bytes[i];
            buffer[(bytes.Length < (int)(bufferCount - 1)) ? bytes.Length : bufferCount - 1] = 0;
            return written;
        }

        #if ENABLE_MONO || ENABLE_IL2CPP
        [AOT.MonoPInvokeCallback(typeof(DatabaseCallbacksContainer.GetDeviceName))]
        #endif
        public static uint GetDeviceName(InputDatabaseDeviceAssignedRef assignedRef, sbyte* o, uint c)
        {
            switch((kInputDevice)assignedRef._opaque)
            {
{%- for x in devices %}
            case kInputDevice.{{x.name}}: return _InputStrToBuf(o, c, ""{{x.displayName}}"");
{%- endfor %}
            default: return 0;
            }
        }

        #if ENABLE_MONO || ENABLE_IL2CPP
        [AOT.MonoPInvokeCallback(typeof(DatabaseCallbacksContainer.GetTraitName))]
        #endif
        public static uint GetTraitName(InputDeviceTraitRef traitRef, sbyte* o, uint c)
        {
            switch((kInputDeviceTrait)traitRef.transparent)
            {
{%- for x in deviceTraits %}
            case kInputDeviceTrait.{{x.name}}: return _InputStrToBuf(o, c, ""{{x.displayName}}"");
{%- endfor %}
            default: return 0;
            }
        }

        #if ENABLE_MONO || ENABLE_IL2CPP
        [AOT.MonoPInvokeCallback(typeof(DatabaseCallbacksContainer.GetControlFullName))]
        #endif
        public static uint GetControlFullName(InputControlUsage usage, sbyte* o, uint c)
        {
            switch((kInputControlUsage)usage.transparent)
            {
{%- for x in allFlattenControls %}
            case kInputControlUsage.{{x.fullyQualifiedName}}: return _InputStrToBuf(o, c, ""{{x.editorFriendlyFullyQualifiedName}}"");
{%- endfor %}
            default: return 0;
            }
        }

        #if ENABLE_MONO || ENABLE_IL2CPP
        [AOT.MonoPInvokeCallback(typeof(DatabaseCallbacksContainer.GetControlTypeName))]
        #endif
        public static uint GetControlTypeName(InputControlTypeRef controlTypeRef, sbyte* o, uint c)
        {
            switch((kInputControlType)controlTypeRef.transparent)
            {
{%- for x in controlTypes %}
            case kInputControlType.{{x.name}}: return _InputStrToBuf(o, c, ""{{x.displayName}}"");
{%- endfor %}
            default: return 0;
            }
        }
    }
}
");
        
            
            return template.Render(database, member => member.Name);
        }  
    }
}