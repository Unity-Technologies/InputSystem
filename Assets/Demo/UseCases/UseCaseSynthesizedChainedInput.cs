namespace UseCases
{
    public class UseCaseSynthesizedChainedInput
    {
        // TODO Setup a use case where e.g. pointer is controlled by touch or e.g. gamepad to see how to reason about this. Maybe in FPS scenario. 
        // TODO This use case example could also serve as custom device, e.g.
        //
        // var device = context.CreateDevice();
        // var pointer = device.AddInterface<Pointer>();
        // var light = device.AddInterface<Light>();
        //
        // context.Offer(pointer.usage, new Pointer.State());
        // context.Offer(light.usage, new Light.State());
        //
        // Gamepad.leftStick.Subscribe(v => { pointer.x += v.x * Time.deltaTime; pointer.y += v.y * Time.deltaTime; } ); // TODO Key to use source event ID here
        //
        // IQ: G G G
        // OQ: G P G P G P
    }
}