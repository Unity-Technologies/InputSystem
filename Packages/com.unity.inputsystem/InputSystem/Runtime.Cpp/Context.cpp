#include "Context.h"
#include "ArgumentChecks.h"

// these should be the only static values in the whole runtime
// so we will have easier time with implementation transpiling later on
static std::unique_ptr<InputContext> _InputContextPointer;
static InputPALCallbacks _InputPALCallbacks = {};
static InputDeviceDatabaseCallbacks _InputDeviceDatabaseCallbacks = {};

// using pointer so we have easier time in the future when we gonna transpile C++ to C#
InputContext* _GetCtx()
{
    return _InputContextPointer.get();
}

InputPALCallbacks* _GetPALCallbacks()
{
    return &_InputPALCallbacks;
}

InputDeviceDatabaseCallbacks* _GetDeviceDatabaseCallbacks()
{
    return &_InputDeviceDatabaseCallbacks;
}

void InputRuntimeInit(const uint32_t ioFramesCount)
{
    if (_GetCtx() != nullptr)
        return;

    InputAssert(ioFramesCount > 0, "ioFramesCount should be > 0");
    if (ioFramesCount == 0)
        return;

    _InputContextPointer = std::make_unique<InputContext>(ioFramesCount);
    _InputDatabaseInitializeControlStorageSpace();
}

void InputRuntimeDeinit()
{
    if (_GetCtx() == nullptr)
        return;

    delete _InputContextPointer.release();
}

void InputSwapFramebuffer(const InputFramebufferRef framebufferRef)
{
    InputContextGuard _guard;

    if (!ArgumentCheck(framebufferRef))
        return;

    auto ctx = _GetCtx();
    auto& framebufferVisibility = ctx->framebufferVisiblity[framebufferRef.transparent];

    // update visibility of devices and controls, remove pending deletion devices and controls
    if (framebufferVisibility.needsUpdatingVisibilityFlags)
    {
        // TODO make this faster
        framebufferVisibility.visibleDevices.clear();
        framebufferVisibility.visibleDevices.reserve(ctx->devices.size());
        for (auto pair : ctx->devices)
            if (!pair.second.pendingDeletion)
                framebufferVisibility.visibleDevices.insert(pair.first);

        framebufferVisibility.visibleControls.clear();
        framebufferVisibility.visibleControls.reserve(ctx->controls.size());
        for (auto pair : ctx->controls)
            if (!pair.second.pendingDeletion)
                framebufferVisibility.visibleControls.insert(pair.first);

//        // delete devices that are no longer visible to any front buffer
//        for (auto it = ctx->devices.begin(); it != ctx->devices.end();)
//        {
//            if (!it->second.pendingDeletion || std::any_of(ctx->FramebufferRefs().begin(), ctx->FramebufferRefs().end(), [&](const InputFramebufferRef fb) -> bool
//            {
//                return ctx->framebufferVisiblity[fb.transparendId].visibleDevices.find(it->first) != ctx->framebufferVisiblity[fb.transparendId].visibleDevices.end();
//            }))
//                it++;
//            else
//                it = ctx->devices.erase(it);
//        }
//
//        // delete controls that are no longer visible to any front buffer
//        for (auto it = ctx->controls.begin(); it != ctx->controls.end();)
//        {
//            if (!it->second.pendingDeletion || std::any_of(ctx->FramebufferRefs().begin(), ctx->FramebufferRefs().end(), [&](const InputFramebufferRef fb) -> bool
//            {
//              return ctx->framebufferVisiblity[fb.transparendId].visibleControls.find(it->first) != ctx->framebufferVisiblity[fb.transparendId].visibleControls.end();
//            }))
//                it++;
//            else
//            {
//                auto instance = &it->second;
//                // TODO
////                ctx->controlsStoragePerType[instance->typeRef.transparent].
////                it->second.indexInStorage;
//
//                it = ctx->controls.erase(it);
//            }
//        }

        framebufferVisibility.needsUpdatingVisibilityFlags = false;
    }

    // move data to front buffers
    for(auto typeRef: ctx->ControlTypeRefs())
        ctx->controlsStoragePerType[typeRef.transparent].MoveDataToFrontBuffer(framebufferRef);

    // setup back buffers for next frame
    for(auto typeRef: ctx->ControlTypeRefs())
    {
        auto& storage = ctx->controlsStoragePerType[typeRef.transparent];
        _InputDatabaseControlTypeFrameBegin(
            typeRef,
            storage.controlRefs.data(),
            storage.controlState.GetAllElements(framebufferRef.transparent,false),
            reinterpret_cast<InputControlTimestamp*>(storage.latestRecordedTimestamp.GetAllElements(framebufferRef.transparent,false)),
            storage.latestRecordedSample.GetAllElements(framebufferRef.transparent,false),
                storage.controlsCount);
    }
}
