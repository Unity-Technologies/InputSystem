## Select an appropriate Input Processing Mode

The Input System **Update Mode** controls when queued input events are processed.

You can find and change the Update Mode by going to **Project Settings** \> **Input System Package** \> **Input Settings** \> **Update Mode.**

The choice of Update Mode that best suits your project is related to whether you're using Update or FixedUpdate to respond to input events. You should choose this based on the specifics of the game you're making. You can read more about Update and FixedUpdate in [Time and Framerate Management](https://docs.unity3d.com/Manual/TimeFrameManagement.html).

In most cases the update mode should remain set to the default of **Process Events in Dynamic Update**, even if you're using code in FixedUpdate to apply physics forces based on input. Refer to the section [Optimizing for fixed-timestep scenarios](\#optimize-for-fixed-timestep-scenarios-(task)) for more information. If you use this update mode, the input events are processed in lock-step with the fixed physics system time steps, but this can result in undesirable lag due to the way events are grouped and processed, as explained in the following sections.

