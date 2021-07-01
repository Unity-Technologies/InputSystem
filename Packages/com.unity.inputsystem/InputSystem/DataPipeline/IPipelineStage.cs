namespace UnityEngine.InputSystem.DataPipeline
{
    public interface IPipelineStage
    {
        public void Map(Dataset dataset);
        public void Execute(Dataset dataset);
    }
}