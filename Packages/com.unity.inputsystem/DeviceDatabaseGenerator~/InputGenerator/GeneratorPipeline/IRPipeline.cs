using System;
using System.Collections.Generic;
using System.ComponentModel.Design.Serialization;
using System.Diagnostics;
using System.Linq;

namespace Unity.InputSystem.DeviceDatabase.IR
{
    internal class IRPipeline
    {
        private IList<IRDeviceDatabase> _databases = new List<IRDeviceDatabase>();

        public void Ingress(IRDeviceDatabase database)
        {
            _databases.Add(database);
        }

        public IRDeviceDatabase Build()
        {
            if (_databases.Count == 0)
                return new IRDeviceDatabase();

            var db = MergeAll(_databases);
            SetDefaults(ref db);
            var controlTypeNameToControlTypeIndex = GenerateTypeRefs(ref db);
            FlattenControls(ref db, controlTypeNameToControlTypeIndex);
            AssignTypeRefs(ref db, controlTypeNameToControlTypeIndex);
            GenerateControlRefs(ref db);
            GenerateTraitsRefs(ref db);
            GenerateDeviceRefs(ref db);

            return db;
        }

        // merge db2 into db1, overriding whatever is there
        private static IRDeviceDatabase MergeTwo(IRDeviceDatabase db1, IRDeviceDatabase db2)
        {
            // Don't sort values in arrays, try to preserve the order in which control types, traits, etc are coming from
            // this will be relevant so we don't suddenly break the mapping for native code unless we surely need to (adding an extra control to a keyboard)

            if (db2.controlTypes != null)
                foreach (var controlType in db2.controlTypes)
                {
                    if (db1.controlTypes != null && db1.controlTypes.Any(x => x.name == controlType.name))
                        throw new NotImplementedException("TODO merge of control types");

                    db1.controlTypes = db1.controlTypes != null ? db1.controlTypes.Append(controlType).ToArray() : new []{controlType};
                }
            
            if (db2.deviceTraits != null)
                foreach (var deviceTrait in db2.deviceTraits)
                {
                    if (db1.deviceTraits != null && db1.deviceTraits.Any(x => x.name == deviceTrait.name))
                        throw new NotImplementedException("TODO merge of device traits");

                    db1.deviceTraits = db1.deviceTraits != null ? db1.deviceTraits.Append(deviceTrait).ToArray() : new []{deviceTrait};
                }
            
            if (db2.devices != null)
                foreach (var device in db2.devices)
                {
                    if (db1.devices != null && db1.devices.Any(x => x.name == device.name))
                        throw new NotImplementedException("TODO merge of devices");

                    db1.devices = db1.devices != null ? db1.devices.Append(device).ToArray() : new []{device};
                }

            return db1;
        }

        private static IRDeviceDatabase MergeAll(IEnumerable<IRDeviceDatabase> databases)
        {
            var databasesArray = databases.ToArray();

            var db = databasesArray[0];
            for (var i = 1; i < databasesArray.Length; ++i)
                db = MergeTwo(db, databasesArray[i]);

            return db;
        }

        private static void SetDefaults(ref IRDeviceDatabase database)
        {
            for(var i = 0; i < database.controlTypes.Length; ++i)
            {
                ref var controlType = ref database.controlTypes[i];

                //controlType.sampleTypeNameAlias ??= "";

                if (controlType.defaultRecordingMode == IRInputControlRecordingMode.NotDeserialized)
                    controlType.defaultRecordingMode = IRInputControlRecordingMode.LatestOnly;
                
                // there are some issues with null arrays in the scriban, so set empty array instead
                controlType.virtualControls ??= Array.Empty<IRControlInstance>();
            }

            for(var i = 0; i < database.deviceTraits.Length; ++i)
            {
                ref var deviceTrait = ref database.deviceTraits[i];

                deviceTrait.controls ??= Array.Empty<IRControlInstance>();
                deviceTrait.methods ??= Array.Empty<IRDeviceTraitMethod>();

                for (var j = 0; j < deviceTrait.methods.Length; ++j)
                {
                    ref var method = ref deviceTrait.methods[j]; 
                    method.args ??= Array.Empty<IRDeviceTraitMethodArgument>();
                }
            }
        }

        private static void FlattenControls(ref IRDeviceDatabase database,
            IReadOnlyDictionary<string, int> controlTypeNameToControlTypeIndex)
        {
            for (var i = 0; i < database.deviceTraits.Length; ++i)
            {
                ref var trait = ref database.deviceTraits[i];
                
                var flattenControls = new List<IRControlInstance>();
                
                for(var j = 0; j < trait.controls.Length; ++j)
                {
                    var control = trait.controls[j];
                    control.fullyQualifiedName = $"{trait.name}_{control.name}{control.type}";
                    control.editorFriendlyFullyQualifiedName = control.fullyQualifiedName.Replace('_', '/');
                    control.databaseInstance = database;
                    control.parentOfVirtualControlFullyQualifiedName = "";
                    trait.controls[j] = control; // set it back also

                    flattenControls.Add(control);
                    
                    if (!controlTypeNameToControlTypeIndex.TryGetValue(control.type, out var typeIndex))
                        throw new ArgumentException($"Control type '{control.type}' is not found (in '{control.name}')");
                
                    var controlType = database.controlTypes[typeIndex];
                    if (controlType.virtualControls == null)
                        continue;

                    foreach (var t2 in controlType.virtualControls)
                    {
                        var virtualControl = t2;
                        virtualControl.fullyQualifiedName = $"{control.fullyQualifiedName}_{virtualControl.name}{virtualControl.type}";
                        virtualControl.editorFriendlyFullyQualifiedName = virtualControl.fullyQualifiedName.Replace('_', '/');
                        virtualControl.parentOfVirtualControlFullyQualifiedName = control.fullyQualifiedName;
                        virtualControl.databaseInstance = database;
                        flattenControls.Add(virtualControl);
                    }
                }

                trait.flattenControls = flattenControls.ToArray();
            }
        }

        private static IReadOnlyDictionary<string, int> GenerateTypeRefs(ref IRDeviceDatabase database)
        {
            var controlTypeNameToControlTypeIndex = new Dictionary<string, int>();

            // assign reference numbers
            for (var i = 0; i < database.controlTypes.Length; ++i)
            {
                ref var controlType = ref database.controlTypes[i];
                controlType.assignedRef = i + 1;
                controlTypeNameToControlTypeIndex.Add(controlType.name, i);
            }

            // assign type refs to virtual controls inside the control types
            for (var i = 0; i < database.controlTypes.Length; ++i)
            {
                ref var controlType = ref database.controlTypes[i];
                if (controlType.virtualControls == null)
                    continue;

                for (var j = 0; j < controlType.virtualControls.Length; ++j)
                {
                    ref var virtualControl = ref controlType.virtualControls[j];

                    if (!controlTypeNameToControlTypeIndex.TryGetValue(virtualControl.type, out var typeIndex))
                        throw new ArgumentException(
                            $"Virtual control type '{virtualControl.type}' is not found (in '{virtualControl.name}')");

                    virtualControl.typeRef = database.controlTypes[typeIndex].assignedRef;
                }
            }

            return controlTypeNameToControlTypeIndex;
        }

        private static void AssignTypeRefs(ref IRDeviceDatabase database,
            IReadOnlyDictionary<string, int> controlTypeNameToControlTypeIndex)
        {
            for (var i = 0; i < database.deviceTraits.Length; ++i)
            {
                ref var trait = ref database.deviceTraits[i];

                for (var j = 0; j < trait.controls.Length; ++j)
                {
                    ref var controlInstance = ref trait.controls[j];

                    if (!controlTypeNameToControlTypeIndex.TryGetValue(controlInstance.type, out var typeIndex))
                        throw new ArgumentException(
                            $"Virtual control type '{controlInstance.type}' is not found (in '{controlInstance.name}')");

                    controlInstance.typeRef = database.controlTypes[typeIndex].assignedRef;
                }

                for (var j = 0; j < trait.flattenControls.Length; ++j)
                {
                    ref var controlInstance = ref trait.flattenControls[j];

                    if (!controlTypeNameToControlTypeIndex.TryGetValue(controlInstance.type, out var typeIndex))
                        throw new ArgumentException(
                            $"Virtual control type '{controlInstance.type}' is not found (in '{controlInstance.name}')");

                    controlInstance.typeRef = database.controlTypes[typeIndex].assignedRef;
                }
            }
        }

        private static void GenerateControlRefs(ref IRDeviceDatabase database)
        {
            var counter = 0;

            for (var i = 0; i < database.deviceTraits.Length; ++i)
            {
                ref var trait = ref database.deviceTraits[i];

                for (var j = 0; j < trait.flattenControls.Length; ++j)
                {
                    ref var controlInstance = ref trait.flattenControls[j];
                    controlInstance.assignedRef = ++counter;

                    if (!string.IsNullOrEmpty(controlInstance.parentOfVirtualControlFullyQualifiedName))
                    {
                        var parentQualifiedName = controlInstance.parentOfVirtualControlFullyQualifiedName;
                        var parentControl = database.deviceTraits
                            .SelectMany(x => x.flattenControls)
                            .First(x => x.fullyQualifiedName == parentQualifiedName);
                        Debug.Assert(controlInstance.assignedRef == parentControl.assignedRef + controlInstance.virtualControlRelativeIndex);
                    }
                }
            }
        }
        
        private static void GenerateTraitsRefs(ref IRDeviceDatabase database)
        {
            for (var i = 0; i < database.deviceTraits.Length; ++i)
            {
                ref var trait = ref database.deviceTraits[i];
                trait.assignedRef = i + 1;
            }
        }

        private static void GenerateDeviceRefs(ref IRDeviceDatabase database)
        {
            for (var i = 0; i < database.devices.Length; ++i)
            {
                ref var device = ref database.devices[i];
                device.assignedRef = i + 1;
            }
        }
    }
}