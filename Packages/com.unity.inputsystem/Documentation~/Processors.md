---
uid: input-system-processors
---
# Processors

An Input Processor takes a value and returns a processed result for it. The received value and result value must be of the same type. For example, you can use a [clamp](#clamp) Processor to clamp values from a control to a certain range.

>__Note__: To convert received input values into different types, see [composite Bindings](ActionBindings.md#composite-bindings).

* [Using Processors](#using-processors)
    * [Processors on Bindings](#processors-on-bindings)
    * [Processors on Actions](#processors-on-actions)
    * [Processors on Controls](#processors-on-controls)
* [Predefined Processors](#predefined-processors)
    * [Clamp](#clamp)
    * [Invert](#invert)
    * [Invert Vector 2](#invert-vector-2)
    * [Invert Vector 3](#invert-vector-3)
    * [Normalize](#normalize)
    * [Normalize Vector 2](#normalize-vector-2)
    * [Normalize Vector 3](#normalize-vector-3)
    * [Scale](#scale)
    * [Scale Vector 2](#scale-vector-2)
    * [Scale Vector 3](#scale-vector-3)
    * [Axis deadzone](#axis-deadzone)
    * [Stick deadzone](#stick-deadzone)
* [Writing custom Processors](#writing-custom-processors)



