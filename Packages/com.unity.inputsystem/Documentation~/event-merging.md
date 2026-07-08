---
uid: input-system-event-merging
---

# Event merging

Input system uses event mering to reduce amount of events required to be processed.
This greatly improves performance when working with high refresh rate devices like 8000 Hz mice, touchscreens and others.

For example let's take a stream of 7 mouse events coming in the same update:

```

Mouse       Mouse       Mouse       Mouse       Mouse       Mouse       Mouse
Event no1   Event no2   Event no3   Event no4   Event no5   Event no6   Event no7
Time 1      Time 2      Time 3      Time 4      Time 5      Time 6      Time 7
Pos(10,20)  Pos(12,21)  Pos(13,23)  Pos(14,24)  Pos(16,25)  Pos(17,27)  Pos(18,28)
Delta(1,1)  Delta(2,1)  Delta(1,2)  Delta(1,1)  Delta(2,1)  Delta(1,2)  Delta(1,1)
BtnLeft(0)  BtnLeft(0)  BtnLeft(0)  BtnLeft(1)  BtnLeft(1)  BtnLeft(1)  BtnLeft(1)
```

To reduce workload we can skip events that are not encoding button state changes:

```
                        Mouse       Mouse                               Mouse
                        Time 3      Time 4                              Time 7
                        Event no3   Event no4                           Event no7
                        Pos(13,23)  Pos(14,24)                          Pos(18,28)
                        Delta(3,3)  Delta(1,1)                          Delta(4,4)
                        BtnLeft(0)  BtnLeft(1)                          BtnLeft(1)
```

In that case we combine no1, no2, no3 together into no3 and accumulate the delta,
then we keep no4 because it stores the transition from button unpressed to button pressed,
and it's important to keep the exact timestamp of such transition.
Later we combine no5, no6, no7 together into no7 because it is the last event in the update.

Currently this approach is implemented for:

- `FastMouse`, combines events unless `buttons` or `clickCount` differ in `MouseState`.
- `Touchscreen`, combines events unless `touchId`, `phaseId` or `flags` differ in `TouchState`.

You can disable merging of events by:

```
InputSystem.settings.disableRedundantEventsMerging = true;
```
