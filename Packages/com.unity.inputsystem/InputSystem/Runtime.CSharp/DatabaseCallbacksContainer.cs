using System;
using System.Runtime.InteropServices;

#if true

namespace Unity.InputSystem.Runtime
{
    internal unsafe class DatabaseCallbacksContainer : IDisposable
    {
        public delegate void ControlTypeIngress(
            InputControlTypeRef controlTypeRef,
            InputControlRef controlRef,
            InputControlTypeRef samplesType,
            InputControlTimestamp* timestamps,
            void* samples,
            uint count,
            InputControlRef fromAnotherControl
        );
        public delegate void ControlTypeFrameBegin(
            InputControlTypeRef controlTypeRef,
            InputControlRef* controlRefs,
            void* controlStates,
            InputControlTimestamp* latestRecordedTimestamps,
            void* latestRecordedSamples,
            uint controlCount
        );
        public delegate uint GetDeviceTraits(InputDatabaseDeviceAssignedRef assignedRef, InputDeviceTraitRef* outputTraitAssignedRefs, uint outputCount);
        public delegate uint GetTraitSizeInBytes   (InputDeviceTraitRef traitRef);
        public delegate uint GetTraitControls      (InputDeviceTraitRef traitRef, InputDeviceRef deviceRef, InputControlRef* outputControlRefs, uint outputCount);
        public delegate void ConfigureTraitInstance(InputDeviceTraitRef traitRef, void* traitPointer, InputDeviceRef device);
        public delegate InputDatabaseControlUsageDescr GetControlUsageDescr(InputControlUsage usage);
        public delegate InputDatabaseControlTypeDescr  GetControlTypeDescr(InputControlTypeRef controlTypeRef);
        public delegate InputDatabaseDeviceAssignedRef GetDeviceAssignedRef(InputGuid deviceGuid);
        public delegate InputDeviceTraitRef            GetTraitAssignedRef (InputGuid traitGuid);
        public delegate InputControlUsage              GetControlUsage     (InputGuid controlGuid);
        public delegate InputControlTypeRef            GetControlTypeRef   (InputGuid controlTypeGuid);
        public delegate InputGuid GetDeviceGuid     (InputDatabaseDeviceAssignedRef assignedRef);
        public delegate InputGuid GetTraitGuid      (InputDeviceTraitRef            traitRef);
        public delegate InputGuid GetControlGuid    (InputControlUsage              usage);
        public delegate InputGuid GetControlTypeGuid(InputControlTypeRef            controlTypeRef);
        public delegate uint GetDeviceRefCount();
        public delegate uint GetTraitRefCount();
        public delegate uint GetControlUsageCount();
        public delegate uint GetControlTypeCount();
        public delegate uint GetDeviceName     (InputDatabaseDeviceAssignedRef assignedRef, sbyte* outputBuffer, uint outputBufferCount);
        public delegate uint GetTraitName      (InputDeviceTraitRef traitRef,               sbyte* outputBuffer, uint outputBufferCount);
        public delegate uint GetControlFullName(InputControlUsage usage,                    sbyte* outputBuffer, uint outputBufferCount);
        public delegate uint GetControlTypeName(InputControlTypeRef controlTypeRef,         sbyte* outputBuffer, uint outputBufferCount);

        private bool _disposed = false;

        public DatabaseCallbacksContainer()
        {
            ControlTypeIngress     _controlTypeIngress     = InputDeviceDatabaseGenerated.ControlTypeIngress;
            ControlTypeFrameBegin  _controlTypeFrameBegin  = InputDeviceDatabaseGenerated.ControlTypeFrameBegin;
            GetDeviceTraits        _getDeviceTraits        = InputDeviceDatabaseGenerated.GetDeviceTraits;
            GetTraitSizeInBytes    _getTraitSizeInBytes    = InputDeviceDatabaseGenerated.GetTraitSizeInBytes;
            GetTraitControls       _getTraitControls       = InputDeviceDatabaseGenerated.GetTraitControls;
            ConfigureTraitInstance _configureTraitInstance = InputDeviceDatabaseGenerated.ConfigureTraitInstance;
            GetControlUsageDescr   _getControlUsageDescr   = InputDeviceDatabaseGenerated.GetControlUsageDescr;
            GetControlTypeDescr    _getControlTypeDescr    = InputDeviceDatabaseGenerated.GetControlTypeDescr;
            GetDeviceAssignedRef   _getDeviceAssignedRef   = InputDeviceDatabaseGenerated.GetDeviceAssignedRef;
            GetTraitAssignedRef    _getTraitAssignedRef    = InputDeviceDatabaseGenerated.GetTraitAssignedRef;
            GetControlUsage        _getControlUsage        = InputDeviceDatabaseGenerated.GetControlUsage;
            GetControlTypeRef      _getControlTypeRef      = InputDeviceDatabaseGenerated.GetControlTypeRef;
            GetDeviceGuid          _getDeviceGuid          = InputDeviceDatabaseGenerated.GetDeviceGuid;
            GetTraitGuid           _getTraitGuid           = InputDeviceDatabaseGenerated.GetTraitGuid;
            GetControlGuid         _getControlGuid         = InputDeviceDatabaseGenerated.GetControlGuid;
            GetControlTypeGuid     _getControlTypeGuid     = InputDeviceDatabaseGenerated.GetControlTypeGuid;
            GetDeviceRefCount      _getDeviceRefCount      = InputDeviceDatabaseGenerated.GetDeviceRefCount;
            GetTraitRefCount       _getTraitRefCount       = InputDeviceDatabaseGenerated.GetTraitRefCount;
            GetControlUsageCount   _getControlUsageCount   = InputDeviceDatabaseGenerated.GetControlUsageCount;
            GetControlTypeCount    _getControlTypeCount    = InputDeviceDatabaseGenerated.GetControlTypeCount;
            GetDeviceName          _getDeviceName          = InputDeviceDatabaseGenerated.GetDeviceName;
            GetTraitName           _getTraitName           = InputDeviceDatabaseGenerated.GetTraitName;
            GetControlFullName     _getControlFullName     = InputDeviceDatabaseGenerated.GetControlFullName;
            GetControlTypeName     _getControlTypeName     = InputDeviceDatabaseGenerated.GetControlTypeName;

            Native.InputDatabaseSetCallbacks(new InputDeviceDatabaseCallbacks
            {
                ControlTypeIngress     = (delegate* unmanaged[Cdecl]<InputControlTypeRef, InputControlRef, InputControlTypeRef, InputControlTimestamp*, void*, uint, InputControlRef, void >) Marshal.GetFunctionPointerForDelegate(_controlTypeIngress),
                ControlTypeFrameBegin  = (delegate* unmanaged[Cdecl]<InputControlTypeRef, InputControlRef*, void*, InputControlTimestamp*, void*, uint, void                               >) Marshal.GetFunctionPointerForDelegate(_controlTypeFrameBegin),
                GetDeviceTraits        = (delegate* unmanaged[Cdecl]<InputDatabaseDeviceAssignedRef, InputDeviceTraitRef*, uint, uint                                                      >) Marshal.GetFunctionPointerForDelegate(_getDeviceTraits),
                GetTraitSizeInBytes    = (delegate* unmanaged[Cdecl]<InputDeviceTraitRef, uint                                                                                             >) Marshal.GetFunctionPointerForDelegate(_getTraitSizeInBytes),
                GetTraitControls       = (delegate* unmanaged[Cdecl]<InputDeviceTraitRef, InputDeviceRef, InputControlRef*, uint, uint                                                     >) Marshal.GetFunctionPointerForDelegate(_getTraitControls),
                ConfigureTraitInstance = (delegate* unmanaged[Cdecl]<InputDeviceTraitRef, void*, InputDeviceRef, void                                                                      >) Marshal.GetFunctionPointerForDelegate(_configureTraitInstance),
                GetControlUsageDescr   = (delegate* unmanaged[Cdecl]<InputControlUsage, InputDatabaseControlUsageDescr                                                                     >) Marshal.GetFunctionPointerForDelegate(_getControlUsageDescr),
                GetControlTypeDescr    = (delegate* unmanaged[Cdecl]<InputControlTypeRef, InputDatabaseControlTypeDescr                                                                    >) Marshal.GetFunctionPointerForDelegate(_getControlTypeDescr),
                GetDeviceAssignedRef   = (delegate* unmanaged[Cdecl]<InputGuid, InputDatabaseDeviceAssignedRef                                                                             >) Marshal.GetFunctionPointerForDelegate(_getDeviceAssignedRef),
                GetTraitAssignedRef    = (delegate* unmanaged[Cdecl]<InputGuid, InputDeviceTraitRef                                                                                        >) Marshal.GetFunctionPointerForDelegate(_getTraitAssignedRef),
                GetControlUsage        = (delegate* unmanaged[Cdecl]<InputGuid, InputControlUsage                                                                                          >) Marshal.GetFunctionPointerForDelegate(_getControlUsage),
                GetControlTypeRef      = (delegate* unmanaged[Cdecl]<InputGuid, InputControlTypeRef                                                                                        >) Marshal.GetFunctionPointerForDelegate(_getControlTypeRef),
                GetDeviceGuid          = (delegate* unmanaged[Cdecl]<InputDatabaseDeviceAssignedRef, InputGuid                                                                             >) Marshal.GetFunctionPointerForDelegate(_getDeviceGuid),
                GetTraitGuid           = (delegate* unmanaged[Cdecl]<InputDeviceTraitRef, InputGuid                                                                                        >) Marshal.GetFunctionPointerForDelegate(_getTraitGuid),
                GetControlGuid         = (delegate* unmanaged[Cdecl]<InputControlUsage, InputGuid                                                                                          >) Marshal.GetFunctionPointerForDelegate(_getControlGuid),
                GetControlTypeGuid     = (delegate* unmanaged[Cdecl]<InputControlTypeRef, InputGuid                                                                                        >) Marshal.GetFunctionPointerForDelegate(_getControlTypeGuid),
                GetDeviceRefCount      = (delegate* unmanaged[Cdecl]<uint                                                                                                                  >) Marshal.GetFunctionPointerForDelegate(_getDeviceRefCount),
                GetTraitRefCount       = (delegate* unmanaged[Cdecl]<uint                                                                                                                  >) Marshal.GetFunctionPointerForDelegate(_getTraitRefCount),
                GetControlUsageCount   = (delegate* unmanaged[Cdecl]<uint                                                                                                                  >) Marshal.GetFunctionPointerForDelegate(_getControlUsageCount),
                GetControlTypeCount    = (delegate* unmanaged[Cdecl]<uint                                                                                                                  >) Marshal.GetFunctionPointerForDelegate(_getControlTypeCount),
                GetDeviceName          = (delegate* unmanaged[Cdecl]<InputDatabaseDeviceAssignedRef, sbyte*, uint, uint                                                                    >) Marshal.GetFunctionPointerForDelegate(_getDeviceName),
                GetTraitName           = (delegate* unmanaged[Cdecl]<InputDeviceTraitRef, sbyte*, uint, uint                                                                               >) Marshal.GetFunctionPointerForDelegate(_getTraitName),
                GetControlFullName     = (delegate* unmanaged[Cdecl]<InputControlUsage, sbyte*, uint, uint                                                                                 >) Marshal.GetFunctionPointerForDelegate(_getControlFullName),
                GetControlTypeName     = (delegate* unmanaged[Cdecl]<InputControlTypeRef, sbyte*, uint, uint                                                                               >) Marshal.GetFunctionPointerForDelegate(_getControlTypeName)
            });
        }

        public void Dispose()
        {
            Dispose(iAmBeingCalledFromDisposeAndNotFinalize: true);
            GC.SuppressFinalize(this);
        }

        ~DatabaseCallbacksContainer()
        {
            Dispose(iAmBeingCalledFromDisposeAndNotFinalize: false);
        }

        protected void Dispose(bool iAmBeingCalledFromDisposeAndNotFinalize)
        {
            if (_disposed)
                return;
            Native.InputDatabaseSetCallbacks(new InputDeviceDatabaseCallbacks { });
            _disposed = true;
        }
    }
}

#endif