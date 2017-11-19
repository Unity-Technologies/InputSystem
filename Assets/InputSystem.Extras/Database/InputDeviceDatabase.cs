// Hmmm... can the device asset stuff *all* be driven from the template? Or at least also contain the template?

// Each device is defined as an asset which has its own inspector
//   Can reference template
//   Can reference arbitrary many assets of arbitrary types (textures, models, sounds, etc)
//      Each one has an associated tag
//      Maybe just make templates part of this?
//   Points to 'XXXSupport' class which is the C# "plugin" for the device
// Plugins do not have to reference the database extra
//   User can just as well use them without

// InputDatabase asset that references the devices
//    Can collect them from the project by scanning
// Custom inspector allows customizing each devices (e.g. overriding settings coming from the device plugin)
// Generates a C# MonoBehaviour that references all the C# plugins and all the assets
// User puts that MB somewhere where it gets included in the build and it will automatically take care
//    of making sure that all the device data is there at runtime

namespace ISX.Database
{
    public class InputDeviceDatabase
    {
    }
}
