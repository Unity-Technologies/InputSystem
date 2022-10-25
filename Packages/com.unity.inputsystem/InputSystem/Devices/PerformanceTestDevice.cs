using System.Runtime.InteropServices;
using UnityEngine.InputSystem.Controls;
using UnityEngine.InputSystem.Layouts;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.InputSystem.Utilities;
using UnityEngine.InputSystem.XR;

namespace UnityEngine.InputSystem
{
    [StructLayout(LayoutKind.Explicit, Size = PoseState.kSizeInBytes * kPoseCount)]
    public unsafe struct PerformanceTestDeviceState : IInputStateTypeInfo
    {
        internal const int kPoseCount = 50;

        public static FourCC Format => new FourCC('P', 'T', 'D', 'S');

[InputControl(name = "pose0", layout = "Pose", bit = PoseState.kSizeInBytes * 0)]
[InputControl(name = "pose1", layout = "Pose", bit = PoseState.kSizeInBytes * 1)]
[InputControl(name = "pose2", layout = "Pose", bit = PoseState.kSizeInBytes * 2)]
[InputControl(name = "pose3", layout = "Pose", bit = PoseState.kSizeInBytes * 3)]
[InputControl(name = "pose4", layout = "Pose", bit = PoseState.kSizeInBytes * 4)]
[InputControl(name = "pose5", layout = "Pose", bit = PoseState.kSizeInBytes * 5)]
[InputControl(name = "pose6", layout = "Pose", bit = PoseState.kSizeInBytes * 6)]
[InputControl(name = "pose7", layout = "Pose", bit = PoseState.kSizeInBytes * 7)]
[InputControl(name = "pose8", layout = "Pose", bit = PoseState.kSizeInBytes * 8)]
[InputControl(name = "pose9", layout = "Pose", bit = PoseState.kSizeInBytes * 9)]
[InputControl(name = "pose10", layout = "Pose", bit = PoseState.kSizeInBytes * 10)]
[InputControl(name = "pose11", layout = "Pose", bit = PoseState.kSizeInBytes * 11)]
[InputControl(name = "pose12", layout = "Pose", bit = PoseState.kSizeInBytes * 12)]
[InputControl(name = "pose13", layout = "Pose", bit = PoseState.kSizeInBytes * 13)]
[InputControl(name = "pose14", layout = "Pose", bit = PoseState.kSizeInBytes * 14)]
[InputControl(name = "pose15", layout = "Pose", bit = PoseState.kSizeInBytes * 15)]
[InputControl(name = "pose16", layout = "Pose", bit = PoseState.kSizeInBytes * 16)]
[InputControl(name = "pose17", layout = "Pose", bit = PoseState.kSizeInBytes * 17)]
[InputControl(name = "pose18", layout = "Pose", bit = PoseState.kSizeInBytes * 18)]
[InputControl(name = "pose19", layout = "Pose", bit = PoseState.kSizeInBytes * 19)]
[InputControl(name = "pose20", layout = "Pose", bit = PoseState.kSizeInBytes * 20)]
[InputControl(name = "pose21", layout = "Pose", bit = PoseState.kSizeInBytes * 21)]
[InputControl(name = "pose22", layout = "Pose", bit = PoseState.kSizeInBytes * 22)]
[InputControl(name = "pose23", layout = "Pose", bit = PoseState.kSizeInBytes * 23)]
[InputControl(name = "pose24", layout = "Pose", bit = PoseState.kSizeInBytes * 24)]
[InputControl(name = "pose25", layout = "Pose", bit = PoseState.kSizeInBytes * 25)]
[InputControl(name = "pose26", layout = "Pose", bit = PoseState.kSizeInBytes * 26)]
[InputControl(name = "pose27", layout = "Pose", bit = PoseState.kSizeInBytes * 27)]
[InputControl(name = "pose28", layout = "Pose", bit = PoseState.kSizeInBytes * 28)]
[InputControl(name = "pose29", layout = "Pose", bit = PoseState.kSizeInBytes * 29)]
[InputControl(name = "pose30", layout = "Pose", bit = PoseState.kSizeInBytes * 30)]
[InputControl(name = "pose31", layout = "Pose", bit = PoseState.kSizeInBytes * 31)]
[InputControl(name = "pose32", layout = "Pose", bit = PoseState.kSizeInBytes * 32)]
[InputControl(name = "pose33", layout = "Pose", bit = PoseState.kSizeInBytes * 33)]
[InputControl(name = "pose34", layout = "Pose", bit = PoseState.kSizeInBytes * 34)]
[InputControl(name = "pose35", layout = "Pose", bit = PoseState.kSizeInBytes * 35)]
[InputControl(name = "pose36", layout = "Pose", bit = PoseState.kSizeInBytes * 36)]
[InputControl(name = "pose37", layout = "Pose", bit = PoseState.kSizeInBytes * 37)]
[InputControl(name = "pose38", layout = "Pose", bit = PoseState.kSizeInBytes * 38)]
[InputControl(name = "pose39", layout = "Pose", bit = PoseState.kSizeInBytes * 39)]
[InputControl(name = "pose40", layout = "Pose", bit = PoseState.kSizeInBytes * 40)]
[InputControl(name = "pose41", layout = "Pose", bit = PoseState.kSizeInBytes * 41)]
[InputControl(name = "pose42", layout = "Pose", bit = PoseState.kSizeInBytes * 42)]
[InputControl(name = "pose43", layout = "Pose", bit = PoseState.kSizeInBytes * 43)]
[InputControl(name = "pose44", layout = "Pose", bit = PoseState.kSizeInBytes * 44)]
[InputControl(name = "pose45", layout = "Pose", bit = PoseState.kSizeInBytes * 45)]
[InputControl(name = "pose46", layout = "Pose", bit = PoseState.kSizeInBytes * 46)]
[InputControl(name = "pose47", layout = "Pose", bit = PoseState.kSizeInBytes * 47)]
[InputControl(name = "pose48", layout = "Pose", bit = PoseState.kSizeInBytes * 48)]
[InputControl(name = "pose49", layout = "Pose", bit = PoseState.kSizeInBytes * 49)]

        [FieldOffset(0)]
        public fixed byte poses[PoseState.kSizeInBytes * kPoseCount];

        public FourCC format => Format;
    }
    
    [InputControlLayout(stateType = typeof(PerformanceTestDeviceState))]
    public class PerformanceTestDevice : InputDevice
    {
        public PoseControl[] poses { get; protected set; }

        public new static PerformanceTestDevice current { get; private set; }

        public override void MakeCurrent()
        {
            base.MakeCurrent();
            current = this;
        }

        protected override void FinishSetup()
        {
            poses = new PoseControl[PerformanceTestDeviceState.kPoseCount];
            for(var i = 0; i < PerformanceTestDeviceState.kPoseCount; ++i)
                poses[i] = GetChildControl<PoseControl>($"pose{i}");
            base.FinishSetup();
        }
    }
}
