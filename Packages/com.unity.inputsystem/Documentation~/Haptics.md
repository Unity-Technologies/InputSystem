    ////WIP

# Haptics Support

## Pausing, Resuming, and Stopping Haptics

In can be desirable to globally pause or stop haptics in certain situation. For example, if the player enters the in-game menu, it can make sense to pauses haptics while the player is in the menu and then resume haptics effects once the player resumes the game.

```CSharp
// Pause haptics globally.
InputSystem.PauseHaptics();

// Resume haptics globally.
InputSystem.ResumeHaptics();

// Stop haptics globally.
InputSystem.ResetHaptics();
```

The difference between `PauseHaptics` and `ResetHaptics` is that the latter will reset haptics playback state on each device to its initial state whereas `PauseHaptics` will preserve playback state in memory and only stop playback on the hardware.

## Rumble
