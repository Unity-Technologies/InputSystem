using Unity.Collections;
using Unity.Core;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Transforms;

[UpdateAfter(typeof(GameplayInputUpdate))]
public class PlayerMovementSystem : JobComponentSystem
{
    private struct MovementJob : IJobForEach<Translation, GameplayInput>
    {
        public TimeData Time;

        public void Execute(ref Translation translation, [ReadOnly] ref GameplayInput input)
        {
            translation.Value += new float3(input.Move.X * 5 * Time.DeltaTime, input.Move.Y * 5 * Time.DeltaTime, 0);
        }
    }

    protected override JobHandle OnUpdate(JobHandle inputDeps)
    {
        // Entities.ForEach API is driving me fucking nuts. Old struct-based API
        // is so much easier to use. More code but far fewer "how do I cram X into
        // the lambda shit?" conundrums.

        var job = new MovementJob
        {
            Time = Time,
        };

        return job.Schedule(this, inputDeps);
    }
}
