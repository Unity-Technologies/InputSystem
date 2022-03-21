#include "Guid.h"
#include "ArgumentChecks.h"

#include <string.h>

// TODO this is super lame implementation just barely enough to get going
// replace it with a proper library

InputGuid InputGuidFromString(const char* guid)
{
    if (!NullPtrCheck(guid))
        return InputGuidInvalid;

    if (strlen(guid) != 36)
    {
        InputAssert(false, "guid string should be 36 chars long");
        return InputGuidInvalid;
    }

    InputGuid r = {};
    auto ptr = (uint8_t*)&r;

    for(uint32_t i = 0, j = 0; i < 36;)
    {
        if(guid[i] == '-')
        {
            ++i;
            continue;
        }

        if (i + 1 >= 36)
        {
            InputAssertFormatted(false, "invalid guid '%s'", guid);
            return InputGuidInvalid;
        }

        char buf[3] = {guid[i], guid[i + 1], 0};
        ptr[j++] = strtol(buf, nullptr, 16);

        i += 2;
    }

    // d8c9e8d6-9fca-4177-a288-29d4eefd893d
    // d6e8c9d8 ca9f 7741 a288 29d4eefd893d
    return r;
}

void InputGuidToString(const InputGuid guid, char* buffer, const uint32_t bufferSize)
{
    auto ptr = (uint8_t*)&guid;

    if(bufferSize != 37)
        InputAssert(false, "guid output string should be 37 chars long (36 + null)");

    for(uint32_t i = 0, j = 0; i < 36;)
    {
        if (i == 8 || i == 13 || i == 18 || i == 23)
        {
            buffer[i] = '-';
            ++i;
            continue;
        }

        char buf[3];
        snprintf(buf, sizeof(buf), "%02x", ptr[j++]);

        buffer[i] = buf[0];
        buffer[i + 1] = buf[1];
        i += 2;
    }

    buffer[36] = '\0';
}
