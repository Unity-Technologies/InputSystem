// namespace InputSystem.Experimental
// {
//     public interface IIoService
//     {
//         public bool EnableInterface(ulong type);
//         public bool DisableInterface(ulong type);
//         public void Update();
//     }
//     
//     public class DummyService : IIoService
//     {
//         public void Initialize()
//         {
//             
//         }
//
//         public void Destroy()
//         {
//             
//         }
//         
//         public bool EnableInterface(ulong type)
//         {
//             switch (type)
//             {
//                 case BuiltinTypes.StandardGamepad:
//                     return true;
//                 default:
//                     break; 
//             }
//             
//             return true;
//         }
//
//         public bool DisableInterface(ulong type)
//         {
//             switch (type)
//             {
//                 case BuiltinTypes.StandardGamepad:
//                     return true;
//                 default:
//                     break;
//             }
//         }
//
//         public void Update()
//         {
//             return;
//         }
//     }
// }