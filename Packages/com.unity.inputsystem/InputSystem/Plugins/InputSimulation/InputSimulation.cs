using System;
using UnityEngine.InputSystem.Utilities;

//step1: need to be able to feed events from onAfterUpdate
//step2: need to have event processing pass on new events fed from onAfterUpdate
//step3: get InputSimulation working on top of this

//need to be able to simulate mouse from both touch and gamepad

//need to figure out what to do about simulation that feeds into other simulation

namespace UnityEngine.InputSystem.Plugins.InputSimulation
{
    /// <summary>
    /// Simulates input one or more devices from input on one or more other devices.
    /// </summary>
    /// <remarks>
    /// This system can be used for mouse or touch simulation, for example.
    ///
    /// Simulation works by transforming data from a set of source bindings to a data
    /// fed into a set of target bindings. The system has a set of built-in value
    /// transformations but additional value transformation and processing can be
    /// attached to the mappings.
    ///
    /// Input simulation is run after each update and output from simulation is
    /// consumed in the same update.
    /// </remarks>
    public class InputSimulation
    {
        public struct SimulatedInput
        {
            public ReadOnlyArray<InputBinding> inputs => throw new NotImplementedException();

            public ReadOnlyArray<InputBinding> outputs => throw new NotImplementedException();
        }
    }
}
