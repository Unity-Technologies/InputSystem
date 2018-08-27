#if (UNITY_STANDALONE || UNITY_EDITOR) && UNITY_ENABLE_STEAM_CONTROLLER_SUPPORT
using System;
#if UNITY_EDITOR
using UnityEngine.Experimental.Input.Plugins.Steam.Editor;
#endif

namespace UnityEngine.Experimental.Input.Plugins.Steam
{
    /// <summary>
    /// Base class for controllers made available through the Steam controller API.
    /// </summary>
    /// <remarks>
    /// Unlike other controllers, the Steam controller is somewhat of an amorphous input
    /// device which gains specific shape only in combination with an "In-Game Action File"
    /// in VDF format installed alongside the application. These files specify the actions
    /// that are supported by the application and are internally bound to specific controls
    /// on a controller inside the Steam runtime. The bindings are set up in the Steam client
    /// through its own binding UI.
    ///
    /// Note that as the Steam controller API supports PS4 and Xbox controllers as well,
    /// the actual hardware device behind a SteamController instance may not be a
    /// Steam Controller. The <see cref="controllerType"/> property can be used what kind
    /// of controller the Steam runtime is talking to internally.
    ///
    /// This class is abstract. Specific Steam controller interfaces can either be implemented
    /// manually based on this class or generated automatically from Steam IGA files using
    /// <see cref="SteamIGAConverter"/>. This can be done in the editor by right-clicking
    /// a .VDF file containing the actions and then selecting "Steam >> Generate Unity Input Device...".
    /// The result is a newly generated device layout that will automatically register itself
    /// with the input system and will represent a Steam controller with the specific action
    /// sets and actions found in the .VDF file. The Steam handles for sets and actions will
    /// automatically be exposed from the device and controls will be set up that correspond
    /// to each action defined in the .VDF file.
    ///
    /// Devices based on SteamController can be used in one of two ways.
    ///
    /// The first method is by manually managing active action sets on a controller. This is done by
    /// calling the various APIs (such as <see cref="ActivateActionSet"/>) that correspond
    /// to the methods in the <see cref="ISteamControllerAPI">Steam controller API</see>. The
    /// controller handle is implicit in this case and corresponds to the <see cref="handle"/>
    /// of the controller the methods are called on.
    ///
    /// The second method is by using
    ///
    ///
    ///
    ///
    ///
    /// By default, Steam controllers will automatically activate action set layers in
    /// response to action maps being enabled and disabled. The correlation between Unity
    /// actions and action maps and Steam actions and action sets happens entirely by name.
    /// E.g. a Unity action map called "gameplay" will be looked up as a Steam action set
    /// using the same name "gameplay".
    /// </remarks>
    public abstract class SteamController : InputDevice
    {
        public const string kSteamInterface = "Steam";

        /// <summary>
        /// Handle in the <see cref="ISteamControllerAPI">Steam API</see> for the controller.
        /// </summary>
        public SteamHandle<SteamController> handle { get; internal set; }

        public SteamControllerType controllerType
        {
            get { throw new NotImplementedException(); }
        }

        /// <summary>
        /// Determine whether the controller automatically activates and deactivates action set
        /// layers in response to <see cref="InputActionMap">input action map</see> being enabled
        /// and disabled.
        /// </summary>
        /// <remarks>
        /// This is on by default.
        ///
        /// When on, if an <see cref="InputActionMap">action map</see> has bindings to a SteamController
        /// and is enabled or disabled, the SteamController will automatically enable or disable
        /// the correspondingly named Steam action set as an action set layer.
        /// </remarks>
        public bool autoActivateSets { get; set; }

        public void ActivateActionSet(SteamHandle<InputActionMap> actionSet)
        {
            SteamSupport.GetAPIAndRequireItToBeSet().ActivateActionSet(handle, actionSet);
        }

        public SteamHandle<InputActionMap> GetCurrentActionSet()
        {
            return SteamSupport.GetAPIAndRequireItToBeSet().GetCurrentActionSet(handle);
        }

        public void ActivateActionSetLayer(SteamHandle<InputActionMap> actionSet)
        {
            throw new NotImplementedException();
        }

        public void DeactivateActionSetLayer(SteamHandle<InputActionMap> actionSet)
        {
            throw new NotImplementedException();
        }

        public void DeactivateAllActionSetLayers()
        {
            throw new NotImplementedException();
        }

        protected abstract void ResolveActions(ISteamControllerAPI api);

        protected abstract void Update(ISteamControllerAPI api);

        // These methods avoid having an 'internal' modifier that every override needs to carry along.
        internal void InvokeResolveActions()
        {
            ResolveActions(SteamSupport.GetAPIAndRequireItToBeSet());
        }

        internal void InvokeUpdate()
        {
            Update(SteamSupport.GetAPIAndRequireItToBeSet());
        }
    }
}

#endif // (UNITY_STANDALONE || UNITY_EDITOR) && UNITY_ENABLE_STEAM_CONTROLLER_SUPPORT
