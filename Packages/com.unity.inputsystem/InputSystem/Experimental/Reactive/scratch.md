For rebinding we want to find all source stream bindings and allow changing them.
Each change would imply replacing a subscription.

For input context, e.g. "grab keyboard", we need a way to set context. This could be something like.

InputSystem.contextIsGrabbed

Layout system could be avoided for standard models. Its better to perform normalization at producer side for standard
models. Native or platform specific exposure is better handled through separate interface where the generic standard
interface may be omitted. These can be mapped to separate interfaces.