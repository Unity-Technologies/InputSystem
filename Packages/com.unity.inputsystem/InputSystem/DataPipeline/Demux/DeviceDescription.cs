using UnityEngine.InputSystem.Utilities;

namespace UnityEngine.InputSystem.DataPipeline.Demux
{
    public struct DeviceDescription
    {
        public int deviceId;
        public FourCC stateEventFourCC;

        public DemuxerType demuxerType;

        public int dataFieldsOffset;
        public int dataFieldsCount;
        
        public int stateOffsetInULongs;
        public int stateSizeInULongs;

        public int validStateIndex;

        public int valueStepFunctionOffset;
        // TODO opaque values
    }
}