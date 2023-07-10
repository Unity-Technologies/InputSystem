//using System;
//using UnityEngine;
//using UnityEngine.InputSystem;
//using UnityEngine.InputSystem.HighLevel;
//using Input = UnityEngine.InputSystem.HighLevel.Input;

// public static class InputActions
// {
//  public class _UIInputActionMap
//  {
//      public _UIInputActionMap()
//      {
//          navigate = new Input<UnityEngine.Vector2>(Input.globalActions.FindAction("UI/Navigate"));
//          submit = new Input<Single>(Input.globalActions.FindAction("UI/Submit"));
//          cancel = new Input<Single>(Input.globalActions.FindAction("UI/Cancel"));
//          point = new Input<UnityEngine.Vector2>(Input.globalActions.FindAction("UI/Point"));
//          click = new Input<Single>(Input.globalActions.FindAction("UI/Click"));
//          scrollWheel = new Input<UnityEngine.Vector2>(Input.globalActions.FindAction("UI/ScrollWheel"));
//          middleClick = new Input<Single>(Input.globalActions.FindAction("UI/MiddleClick"));
//          rightClick = new Input<Single>(Input.globalActions.FindAction("UI/RightClick"));
//          trackedDevicePosition = new Input<UnityEngine.Vector3>(Input.globalActions.FindAction("UI/TrackedDevicePosition"));
//          trackedDeviceOrientation = new Input<UnityEngine.Quaternion>(Input.globalActions.FindAction("UI/TrackedDeviceOrientation"));
//
//      }
//
//      public Input<UnityEngine.Vector2> navigate { get; }
//      public Input<Single> submit { get; }
//      public Input<Single> cancel { get; }
//      public Input<UnityEngine.Vector2> point { get; }
//      public Input<Single> click { get; }
//      public Input<UnityEngine.Vector2> scrollWheel { get; }
//      public Input<Single> middleClick { get; }
//      public Input<Single> rightClick { get; }
//      public Input<UnityEngine.Vector3> trackedDevicePosition { get; }
//      public Input<UnityEngine.Quaternion> trackedDeviceOrientation { get; }
//
//  }
//  public class _FPSInputActionMap
//  {
//      public _FPSInputActionMap()
//      {
//          move = new Input<UnityEngine.Vector2>(Input.globalActions.FindAction("FPS/Move"));
//          look = new Input<UnityEngine.Vector2>(Input.globalActions.FindAction("FPS/Look"));
//          fire = new Input<Single>(Input.globalActions.FindAction("FPS/Fire"));
//          jump = new Input<Single>(Input.globalActions.FindAction("FPS/Jump"));
//          interact = new Input<Single>(Input.globalActions.FindAction("FPS/Interact"));
//          nextWeapon = new Input<Single>(Input.globalActions.FindAction("FPS/NextWeapon"));
//          previousWeapon = new Input<Single>(Input.globalActions.FindAction("FPS/PreviousWeapon"));
//          sprint = new Input<Single>(Input.globalActions.FindAction("FPS/Sprint"));
//          melee = new Input<Single>(Input.globalActions.FindAction("FPS/Melee"));
//
//      }
//
//      public Input<UnityEngine.Vector2> move { get; }
//      public Input<UnityEngine.Vector2> look { get; }
//      public Input<Single> fire { get; }
//      public Input<Single> jump { get; }
//      public Input<Single> interact { get; }
//      public Input<Single> nextWeapon { get; }
//      public Input<Single> previousWeapon { get; }
//      public Input<Single> sprint { get; }
//      public Input<Single> melee { get; }
//
//  }
//  public class _DrivingInputActionMap
//  {
//      public _DrivingInputActionMap()
//      {
//          accelerate = new Input<Single>(Input.globalActions.FindAction("Driving/Accelerate"));
//          brake = new Input<Single>(Input.globalActions.FindAction("Driving/Brake"));
//          clutch = new Input<Single>(Input.globalActions.FindAction("Driving/Clutch"));
//          steer = new Input<Single>(Input.globalActions.FindAction("Driving/Steer"));
//          horn = new Input<Single>(Input.globalActions.FindAction("Driving/Horn"));
//          handbrake = new Input<Single>(Input.globalActions.FindAction("Driving/Handbrake"));
//          shiftUp = new Input<Single>(Input.globalActions.FindAction("Driving/ShiftUp"));
//          shiftDown = new Input<Single>(Input.globalActions.FindAction("Driving/ShiftDown"));
//          headlights = new Input<Single>(Input.globalActions.FindAction("Driving/Headlights"));
//          wipers = new Input<Single>(Input.globalActions.FindAction("Driving/Wipers"));
//          ignition = new Input<Single>(Input.globalActions.FindAction("Driving/Ignition"));
//          pitLimiter = new Input<Single>(Input.globalActions.FindAction("Driving/PitLimiter"));
//          dRS = new Input<Single>(Input.globalActions.FindAction("Driving/DRS"));
//          toggleSteeringAssist = new Input<Single>(Input.globalActions.FindAction("Driving/ToggleSteeringAssist"));
//          toggleBrakingAssist = new Input<Single>(Input.globalActions.FindAction("Driving/ToggleBrakingAssist"));
//          toggleDamage = new Input<Single>(Input.globalActions.FindAction("Driving/ToggleDamage"));
//          toggleBrakeAssist = new Input<Single>(Input.globalActions.FindAction("Driving/ToggleBrakeAssist"));
//          toggleTractionControl = new Input<Single>(Input.globalActions.FindAction("Driving/ToggleTractionControl"));
//          togglePitLaneAssist = new Input<Single>(Input.globalActions.FindAction("Driving/TogglePitLaneAssist"));
//          freeLook = new Input<UnityEngine.Vector2>(Input.globalActions.FindAction("Driving/FreeLook"));
//          lookLeft = new Input<Single>(Input.globalActions.FindAction("Driving/LookLeft"));
//          lookRight = new Input<Single>(Input.globalActions.FindAction("Driving/LookRight"));
//          lookUp = new Input<Single>(Input.globalActions.FindAction("Driving/LookUp"));
//          lookDown = new Input<Single>(Input.globalActions.FindAction("Driving/LookDown"));
//          nextCamera = new Input<Single>(Input.globalActions.FindAction("Driving/NextCamera"));
//          previousCamera = new Input<Single>(Input.globalActions.FindAction("Driving/PreviousCamera"));
//
//      }
//
//      public Input<Single> accelerate { get; }
//      public Input<Single> brake { get; }
//      public Input<Single> clutch { get; }
//      public Input<Single> steer { get; }
//      public Input<Single> horn { get; }
//      public Input<Single> handbrake { get; }
//      public Input<Single> shiftUp { get; }
//      public Input<Single> shiftDown { get; }
//      public Input<Single> headlights { get; }
//      public Input<Single> wipers { get; }
//      public Input<Single> ignition { get; }
//      public Input<Single> pitLimiter { get; }
//      public Input<Single> dRS { get; }
//      public Input<Single> toggleSteeringAssist { get; }
//      public Input<Single> toggleBrakingAssist { get; }
//      public Input<Single> toggleDamage { get; }
//      public Input<Single> toggleBrakeAssist { get; }
//      public Input<Single> toggleTractionControl { get; }
//      public Input<Single> togglePitLaneAssist { get; }
//      public Input<UnityEngine.Vector2> freeLook { get; }
//      public Input<Single> lookLeft { get; }
//      public Input<Single> lookRight { get; }
//      public Input<Single> lookUp { get; }
//      public Input<Single> lookDown { get; }
//      public Input<Single> nextCamera { get; }
//      public Input<Single> previousCamera { get; }
//
//  }
//  public class _2DPlatformerInputActionMap
//  {
//      public _2DPlatformerInputActionMap()
//      {
//          left = new Input<Single>(Input.globalActions.FindAction("2DPlatformer/Left"));
//          right = new Input<Single>(Input.globalActions.FindAction("2DPlatformer/Right"));
//          jump = new Input<Single>(Input.globalActions.FindAction("2DPlatformer/Jump"));
//          sprint = new Input<Single>(Input.globalActions.FindAction("2DPlatformer/Sprint"));
//          walk = new Input<Single>(Input.globalActions.FindAction("2DPlatformer/Walk"));
//          fire = new Input<Single>(Input.globalActions.FindAction("2DPlatformer/Fire"));
//
//      }
//
//      public Input<Single> left { get; }
//      public Input<Single> right { get; }
//      public Input<Single> jump { get; }
//      public Input<Single> sprint { get; }
//      public Input<Single> walk { get; }
//      public Input<Single> fire { get; }
//
//  }
//  public class _ThirdPersonInputActionMap
//  {
//      public _ThirdPersonInputActionMap()
//      {
//          move = new Input<UnityEngine.Vector2>(Input.globalActions.FindAction("ThirdPerson/Move"));
//          look = new Input<UnityEngine.Vector2>(Input.globalActions.FindAction("ThirdPerson/Look"));
//          fire = new Input<Single>(Input.globalActions.FindAction("ThirdPerson/Fire"));
//          jump = new Input<Single>(Input.globalActions.FindAction("ThirdPerson/Jump"));
//          interact = new Input<Single>(Input.globalActions.FindAction("ThirdPerson/Interact"));
//          nextWeapon = new Input<Single>(Input.globalActions.FindAction("ThirdPerson/NextWeapon"));
//          previousWeapon = new Input<Single>(Input.globalActions.FindAction("ThirdPerson/PreviousWeapon"));
//          sprint = new Input<Single>(Input.globalActions.FindAction("ThirdPerson/Sprint"));
//          melee = new Input<Single>(Input.globalActions.FindAction("ThirdPerson/Melee"));
//          walk = new Input<Single>(Input.globalActions.FindAction("ThirdPerson/Walk"));
//
//      }
//
//      public Input<UnityEngine.Vector2> move { get; }
//      public Input<UnityEngine.Vector2> look { get; }
//      public Input<Single> fire { get; }
//      public Input<Single> jump { get; }
//      public Input<Single> interact { get; }
//      public Input<Single> nextWeapon { get; }
//      public Input<Single> previousWeapon { get; }
//      public Input<Single> sprint { get; }
//      public Input<Single> melee { get; }
//      public Input<Single> walk { get; }
//
//  }
//  public class _RTSInputActionMap
//  {
//      public _RTSInputActionMap()
//      {
//          command = new Input<Single>(Input.globalActions.FindAction("RTS/Command"));
//          assignSelectedToGroup = new Input<Single>(Input.globalActions.FindAction("RTS/AssignSelectedToGroup"));
//          addSelectedToGroup = new Input<Single>(Input.globalActions.FindAction("RTS/AddSelectedToGroup"));
//          removeSelectedFromSelection = new Input<Single>(Input.globalActions.FindAction("RTS/RemoveSelectedFromSelection"));
//          selectGroup = new Input<Single>(Input.globalActions.FindAction("RTS/SelectGroup"));
//          centerSelectedGroup = new Input<Single>(Input.globalActions.FindAction("RTS/CenterSelectedGroup"));
//          queueOrders = new Input<Single>(Input.globalActions.FindAction("RTS/QueueOrders"));
//          panCamera = new Input<UnityEngine.Vector2>(Input.globalActions.FindAction("RTS/PanCamera"));
//          zoomCamera = new Input<Single>(Input.globalActions.FindAction("RTS/ZoomCamera"));
//          move = new Input<Single>(Input.globalActions.FindAction("RTS/Move"));
//          attack = new Input<Single>(Input.globalActions.FindAction("RTS/Attack"));
//          patrol = new Input<Single>(Input.globalActions.FindAction("RTS/Patrol"));
//          cancel = new Input<Single>(Input.globalActions.FindAction("RTS/Cancel"));
//
//      }
//
//      public Input<Single> command { get; }
//      public Input<Single> assignSelectedToGroup { get; }
//      public Input<Single> addSelectedToGroup { get; }
//      public Input<Single> removeSelectedFromSelection { get; }
//      public Input<Single> selectGroup { get; }
//      public Input<Single> centerSelectedGroup { get; }
//      public Input<Single> queueOrders { get; }
//      public Input<UnityEngine.Vector2> panCamera { get; }
//      public Input<Single> zoomCamera { get; }
//      public Input<Single> move { get; }
//      public Input<Single> attack { get; }
//      public Input<Single> patrol { get; }
//      public Input<Single> cancel { get; }
//
//  }
//  public class _FlightSimInputActionMap
//  {
//      public _FlightSimInputActionMap()
//      {
//          throttleUp = new Input<Single>(Input.globalActions.FindAction("FlightSim/ThrottleUp"));
//          throttleDown = new Input<Single>(Input.globalActions.FindAction("FlightSim/ThrottleDown"));
//          parkingBrake = new Input<Single>(Input.globalActions.FindAction("FlightSim/ParkingBrake"));
//          landingGear = new Input<Single>(Input.globalActions.FindAction("FlightSim/LandingGear"));
//          increaseFlaps = new Input<Single>(Input.globalActions.FindAction("FlightSim/IncreaseFlaps"));
//          decreaseFlaps = new Input<Single>(Input.globalActions.FindAction("FlightSim/DecreaseFlaps"));
//          ailerons = new Input<UnityEngine.Vector2>(Input.globalActions.FindAction("FlightSim/Ailerons"));
//          freeLook = new Input<UnityEngine.Vector2>(Input.globalActions.FindAction("FlightSim/FreeLook"));
//          resetView = new Input<Single>(Input.globalActions.FindAction("FlightSim/ResetView"));
//          rudderLeft = new Input<Single>(Input.globalActions.FindAction("FlightSim/RudderLeft"));
//          rudderRight = new Input<Single>(Input.globalActions.FindAction("FlightSim/RudderRight"));
//          brakes = new Input<Single>(Input.globalActions.FindAction("FlightSim/Brakes"));
//          autopilot = new Input<Single>(Input.globalActions.FindAction("FlightSim/Autopilot"));
//
//      }
//
//      public Input<Single> throttleUp { get; }
//      public Input<Single> throttleDown { get; }
//      public Input<Single> parkingBrake { get; }
//      public Input<Single> landingGear { get; }
//      public Input<Single> increaseFlaps { get; }
//      public Input<Single> decreaseFlaps { get; }
//      public Input<UnityEngine.Vector2> ailerons { get; }
//      public Input<UnityEngine.Vector2> freeLook { get; }
//      public Input<Single> resetView { get; }
//      public Input<Single> rudderLeft { get; }
//      public Input<Single> rudderRight { get; }
//      public Input<Single> brakes { get; }
//      public Input<Single> autopilot { get; }
//
//  }
//  public static _UIInputActionMap uI;
//  public static _FPSInputActionMap fPS;
//  public static _DrivingInputActionMap driving;
//  public static _2DPlatformerInputActionMap _2DPlatformer;
//  public static _ThirdPersonInputActionMap thirdPerson;
//  public static _RTSInputActionMap rTS;
//  public static _FlightSimInputActionMap flightSim;
//  static InputActions()
//  {
//      uI = new _UIInputActionMap();
//      fPS = new _FPSInputActionMap();
//      driving = new _DrivingInputActionMap();
//      _2DPlatformer = new _2DPlatformerInputActionMap();
//      thirdPerson = new _ThirdPersonInputActionMap();
//      rTS = new _RTSInputActionMap();
//      flightSim = new _FlightSimInputActionMap();
//
//  }
// }
