namespace UnityEngine.InputSystem.DataPipeline
{
    public interface IStepFunction
    {
        // if 0 then value is opaque
        public int dimensionsCount { get; }

        // this is silly workaround to avoid managed arrays
        // ideally would make a fixed array here
        // TODO figure out something better
        public int valuesXProperty { get; }
        public int valuesYProperty { get; }
        public int valuesZProperty { get; }

        public int opaqueValuesProperty { get; }
        public int opaqueValuesStrideProperty { get; }
    };
    
    // TODO we don't _really_ need strongly typed step function structs
    // but they help to leverage static typing to ensure that we're doing the right thing

    public struct StepFunction1D : IStepFunction
    {
        public int valuesX;

        public int dimensionsCount => 1;
        public int valuesXProperty => valuesX;
        public int valuesYProperty => 0;
        public int valuesZProperty => 0;
        public int opaqueValuesProperty => 0;
        public int opaqueValuesStrideProperty => 0;
    };

    public struct StepFunction2D : IStepFunction
    {
        public int valuesX;
        public int valuesY;

        public int dimensionsCount => 2;
        public int valuesXProperty => valuesX;
        public int valuesYProperty => valuesY;
        public int valuesZProperty => 0;
        public int opaqueValuesProperty => 0;
        public int opaqueValuesStrideProperty => 0;
    };

    public struct StepFunction3D : IStepFunction
    {
        public int valuesX;
        public int valuesY;
        public int valuesZ;

        public int dimensionsCount => 3;
        public int valuesXProperty => valuesX;
        public int valuesYProperty => valuesY;
        public int valuesZProperty => valuesZ;
        public int opaqueValuesProperty => 0;
        public int opaqueValuesStrideProperty => 0;
    };

    public struct StepFunctionOpaque : IStepFunction
    {
        public int opaqueValues;
        public int opaqueValueStride;

        public int dimensionsCount => 0;
        public int valuesXProperty => 0;
        public int valuesYProperty => 0;
        public int valuesZProperty => 0;
        public int opaqueValuesProperty => opaqueValues;
        public int opaqueValuesStrideProperty => opaqueValueStride;
    };

    public struct StepFunctionQuaternion : IStepFunction
    {
        public int opaqueValues;

        public int dimensionsCount => 0;
        public int valuesXProperty => 0;
        public int valuesYProperty => 0;
        public int valuesZProperty => 0;
        public int opaqueValuesProperty => opaqueValues;
        public unsafe int opaqueValuesStrideProperty => sizeof(Quaternion);
    };

    public struct StepFunctionInt : IStepFunction
    {
        public int opaqueValues;

        public int dimensionsCount => 0;
        public int valuesXProperty => 0;
        public int valuesYProperty => 0;
        public int valuesZProperty => 0;
        public int opaqueValuesProperty => opaqueValues;
        public int opaqueValuesStrideProperty => sizeof(int);
    };

}