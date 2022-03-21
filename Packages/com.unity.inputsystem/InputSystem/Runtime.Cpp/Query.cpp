#include "Query.h"
#include "Context.h"

// TODO does this actually force execute the query?
InputQueryRef InputRegisterQuery(
    const InputFramebufferRef framebufferRef,
    const InputQueryDescr queryDescr
)
{
    return {};
}

void InputRemoveQuery(
    const InputQueryRef queryRef
)
{

}

// return nullptr is none, pointer is valid until next buffer swap or ForceSyncControlInFrontbufferWithBackbuffer
const InputControlsLinkedList* InputGetQueryResult(
    const InputQueryRef queryRef
)
{
    return nullptr;
}
