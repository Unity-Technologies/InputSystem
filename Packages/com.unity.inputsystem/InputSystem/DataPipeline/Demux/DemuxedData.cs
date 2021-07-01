namespace UnityEngine.InputSystem.DataPipeline.Demux
{
    public struct DemuxedData
    {
        public int valueStepfunctionIndex;
        // TODO opaque values
        public ulong timestamp;
        public float value;
    }
}