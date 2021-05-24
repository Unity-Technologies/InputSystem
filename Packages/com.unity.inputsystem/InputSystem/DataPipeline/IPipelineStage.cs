namespace UnityEngine.InputSystem.DataPipeline
{
    public interface IPipelineStage
    {
        public void Map(DatasetProxy datasetProxy);
        public void Execute(DatasetProxy datasetProxy);
    }
}