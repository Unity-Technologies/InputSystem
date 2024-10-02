# Predefined Processors

The Input System package comes with a set of useful Processors you can use.

### Clamp

|__Name__|[`Clamp`](../api/UnityEngine.InputSystem.Processors.ClampProcessor.html)|
|---|---|
|__Operand Type__|`float`|
|__Parameters__|`float min`<br>`float max`|

Clamps input values to the [`min`..`max`] range.

### Invert

|__Name__|[`Invert`](../api/UnityEngine.InputSystem.Processors.InvertProcessor.html)|
|---|---|
|__Operand Type__|`float`|

Inverts the values from a Control (that is, multiplies the values by -1).

### Invert Vector 2

|__Name__|[`InvertVector2`](../api/UnityEngine.InputSystem.Processors.InvertVector2Processor.html)|
|---|---|
|__Operand Type__|`Vector2`|
|__Parameters__|`bool invertX`<br>`bool invertY`|

Inverts the values from a Control (that is, multiplies the values by -1). Inverts the x axis of the vector if `invertX` is true, and the y axis if `invertY` is true.

### Invert Vector 3

|__Name__|[`Invert Vector 3`](../api/UnityEngine.InputSystem.Processors.InvertVector3Processor.html)|
|---|---|
|__Operand Type__|`Vector3`|
|__Parameters__|`bool invertX`<br>`bool invertY`<br>`bool invertZ`|

Inverts the values from a Control (that is, multiplies the values by -1). Inverts the x axis of the vector if `invertX` is true, the y axis if `invertY` is true, and the z axis if `invertZ` is true.

### Normalize

|__Name__|[`Normalize`](../api/UnityEngine.InputSystem.Processors.NormalizeProcessor.html)|
|---|---|
|__Operand Type__|`float`|
|__Parameters__|`float min`<br>`float max`<br>`float zero`|

Normalizes input values in the range [`min`..`max`] to unsigned normalized form [0..1] if `min` is >= `zero`, and to signed normalized form [-1..1] if `min` < `zero`.

### Normalize Vector 2

|__Name__|[`NormalizeVector2`](../api/UnityEngine.InputSystem.Processors.NormalizeVector2Processor.html)|
|---|---|
|__Operand Type__|`Vector2`|

Normalizes input vectors to be of unit length (1). This is the same as calling `Vector2.normalized`.

### Normalize Vector 3

|__Name__|[`NormalizeVector3`](../api/UnityEngine.InputSystem.Processors.NormalizeVector3Processor.html)|
|---|---|
|__Operand Type__|`Vector3`|

Normalizes input vectors to be of unit length (1). This is the same as calling `Vector3.normalized`.

### Scale

|__Name__|[`Scale`](../api/UnityEngine.InputSystem.Processors.ScaleProcessor.html)|
|---|---|
|__Operand Type__|`float`|
|__Parameters__|`float factor`|

Multiplies all input values by `factor`.

### Scale Vector 2

|__Name__|[`ScaleVector2`](../api/UnityEngine.InputSystem.Processors.ScaleVector2Processor.html)|
|---|---|
|__Operand Type__|`Vector2`|
|__Parameters__|`float x`<br>`float y`|

Multiplies all input values by `x` along the X axis and by `y` along the Y axis.

### Scale Vector 3

|__Name__|[`ScaleVector3`](../api/UnityEngine.InputSystem.Processors.ScaleVector3Processor.html)|
|---|---|
|__Operand Type__|`Vector3`|
|__Parameters__|`float x`<br>`float y`<br>`float x`|

Multiplies all input values by `x` along the X axis, by `y` along the Y axis, and by `z` along the Z axis.

### Axis deadzone

|__Name__|[`AxisDeadzone`](../api/UnityEngine.InputSystem.Processors.AxisDeadzoneProcessor.html)|
|---|---|
|__Operand Type__|`float`|
|__Parameters__|`float min`<br>`float max`|

An axis deadzone Processor scales the values of a Control so that any value with an absolute value smaller than `min` is 0, and any value with an absolute value larger than `max` is 1 or -1. Many Controls don't have a precise resting point (that is, they don't always report exactly 0 when the Control is in the center). Using the `min` value on a deadzone Processor avoids unintentional input from such Controls. Also, some Controls don't consistently report their maximum values when moving the axis all the way. Using the `max` value on a deadzone Processor ensures that you always get the maximum value in such cases.

### Stick deadzone

|__Name__|[`StickDeadzone`](../api/UnityEngine.InputSystem.Processors.StickDeadzoneProcessor.html)|
|---|---|
|__Operand Type__|`Vector2`|
|__Parameters__|`float min`<br>`float max`|

A stick deadzone Processor scales the values of a Vector2 Control, such as a stick, so that any input vector with a magnitude smaller than `min` results in (0,0), and any input vector with a magnitude greater than `max` is normalized to length 1. Many Controls don't have a precise resting point (that is, they don't always report exactly 0,0 when the Control is in the center). Using the `min` value on a deadzone Processor avoids unintentional input from such Controls. Also, some Controls don't consistently report their maximum values when moving the axis all the way. Using the `max` value on a deadzone Processor ensures that you always get the maximum value in such cases.