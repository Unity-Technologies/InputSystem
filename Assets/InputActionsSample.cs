//using System;
//using UnityEngine;
//using UnityEngine.InputSystem.HighLevel;
//using Input = UnityEngine.InputSystem.HighLevel.Input;

// namespace Assets
// {
//  public static class InputActions
//  {
//      public class Driving
//      {
//          public Driving()
//          {
//              accelerate = new Input<Single>(Input.globalActions.FindAction("driving/accelerate"));
//          }
//
//          public Input<float> accelerate { get; }
//      }
//
//      public class FlightSim
//      {
//          public FlightSim()
//          {
//              ailerons = new Input<Vector2>(Input.globalActions.FindAction("flightsim/ailerons"));
//          }
//
//          /// <summary>
//          /// This action is currently bound to:
//          /// <example>
//          /// <code>
//          ///     2D Vector
//          ///         Up: Left Stick/Up [Gamepad]
//          ///         Down: S [Keyboard]
//          ///         Left: A [Keyboard]
//          ///         Right: D [Keyboard]
//          ///     Left Stick [Gamepad]
//          /// </code>
//          /// </example>
//          /// </summary>
//          public Input<Vector2> ailerons;
//      }
//
//      public static Driving driving;
//      public static FlightSim flightSim;
//
//      static InputActions()
//      {
//          driving = new Driving();
//
//          driving.accelerate.action.Enable();
//      }
//  }
//
// }
