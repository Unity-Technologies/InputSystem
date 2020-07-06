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
        public Random Random;

        public void Execute(ref Translation translation, [ReadOnly] ref GameplayInput input)
        {
            if (input.Jump.WasJustPressed)
                translation.Value += Random.NextFloat3(0, 10 * Time.DeltaTime);
            else
                translation.Value += new float3(input.Move.X * 5 * Time.DeltaTime, input.Move.Y * 5 * Time.DeltaTime, 0);
        }
    }

    private Random m_Random;

    protected override void OnCreate()
    {
        m_Random = new Random(1234);
    }

    protected override JobHandle OnUpdate(JobHandle inputDeps)
    {
        // Entities.ForEach API is driving me nuts. Old struct-based API
        // seems so much easier to use. More code but far fewer "how do I cram X into
        // this lambda?" conundrums.

        var job = new MovementJob
        {
            Time = Time,
            Random = m_Random,
        };

        return job.Schedule(this, inputDeps);
    }
}
