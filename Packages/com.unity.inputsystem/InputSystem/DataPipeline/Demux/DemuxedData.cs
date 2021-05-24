namespace UnityEngine.InputSystem.DataPipeline.Demux
{
    internal struct DemuxedData
    {
        public int valueStepfunctionIndex;
        // TODO opaque values
        public ulong timestamp;
        public float value;
    }
}