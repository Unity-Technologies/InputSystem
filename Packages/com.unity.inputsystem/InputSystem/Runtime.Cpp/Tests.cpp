#include "InputRuntime.h"

#ifdef INPUT_BUILD_NATIVETESTS

#include <ostream>

std::ostream& operator<<(std::ostream& out, InputControlTimestamp const&t) {
    out << t.timestamp << ":" << t.timeline;
    return out;
}

std::ostream& operator<<(std::ostream& out, InputButtonControlSample const&t) {
    out << (t.IsPressed() ? "pressed": "released");
    return out;
}

#include <catch_amalgamated.hpp>

void InputRuntimeRunNativeTests()
{
    Catch::Session().run();
}

TEST_CASE("Can Init, Deinit and swap framebuffers", "[InputNative.Basics]" )
{
    uint32_t ioFramesCount = GENERATE(1, 10);

    InputRuntimeInit(ioFramesCount);

    for(uint32_t i = 0; i < ioFramesCount; ++i)
        InputSwapFramebuffer(InputFramebufferRef {i});

    // do it twice in a row
    for(uint32_t i = 0; i < ioFramesCount; ++i)
        InputSwapFramebuffer(InputFramebufferRef {i});

    InputRuntimeDeinit();
}

class WithPlayerFramebuffer {
public:
    const InputFramebufferRef playerFramebuffer = {0};

    WithPlayerFramebuffer()
    {
        InputRuntimeInit(1);
    }

    ~WithPlayerFramebuffer()
    {
        InputRuntimeDeinit();
    }
};

class WithPlayerAndEditorFramebuffers {
public:
    const InputFramebufferRef playerFramebuffer = {0};
    const InputFramebufferRef editorFramebuffer = {1};

    WithPlayerAndEditorFramebuffers()
    {
        InputRuntimeInit(2);
    }

    ~WithPlayerAndEditorFramebuffers()
    {
        InputRuntimeDeinit();
    }
};


TEST_CASE_METHOD(WithPlayerFramebuffer, "Basic keyboard", "[InputNative.Basics]" )
{
    const auto deviceRef = InputInstantiateDevice(InputGuidFromString("8d37e884-458e-4b1d-805f-95425987e9d1"), {});
    CHECK(deviceRef != InputDeviceRefInvalid);

    const auto keyboard = AsTrait<InputKeyboard>(deviceRef);
    const auto spaceButton = keyboard.SpaceButton();

    spaceButton.Ingress({}, {1});
    CHECK(spaceButton.GetLatestSample(playerFramebuffer).sample.IsPressed() == false);
    CHECK(spaceButton.GetState(playerFramebuffer).wasPressedThisIOFrame == false);
    CHECK(spaceButton.GetState(playerFramebuffer).wasReleasedThisIOFrame == false);
//    CHECK(spaceButton.GetLatestSample(editorFramebuffer).sample.IsPressed() == false);
//    CHECK(spaceButton.WasPressedThisIOFrame(editorFramebuffer) == false);
//    CHECK(spaceButton.WasReleasedThisIOFrame(editorFramebuffer) == false);

    InputSwapFramebuffer(playerFramebuffer);
    CHECK(spaceButton.GetLatestSample(playerFramebuffer).sample.IsPressed() == true);
    CHECK(spaceButton.GetState(playerFramebuffer).wasPressedThisIOFrame == true);
    CHECK(spaceButton.GetState(playerFramebuffer).wasReleasedThisIOFrame == false);
//    CHECK(spaceButton.GetLatestSample(editorFramebuffer).sample.IsPressed() == false);
//    CHECK(spaceButton.WasPressedThisIOFrame(editorFramebuffer) == false);
//    CHECK(spaceButton.WasReleasedThisIOFrame(editorFramebuffer) == false);

    CHECK(spaceButton[InputButtonControlRef::AxisOneWays::As].GetLatestSample(playerFramebuffer).sample.value == 1.0f);

    InputSwapFramebuffer(playerFramebuffer);
    CHECK(spaceButton.GetLatestSample(playerFramebuffer).sample.IsPressed() == true);
    CHECK(spaceButton.GetState(playerFramebuffer).wasPressedThisIOFrame == false);
    CHECK(spaceButton.GetState(playerFramebuffer).wasReleasedThisIOFrame == false);

    spaceButton.Ingress({}, {0});
    CHECK(spaceButton.GetLatestSample(playerFramebuffer).sample.IsPressed() == true);
    CHECK(spaceButton.GetState(playerFramebuffer).wasPressedThisIOFrame == false);
    CHECK(spaceButton.GetState(playerFramebuffer).wasReleasedThisIOFrame == false);

    InputSwapFramebuffer(playerFramebuffer);
    CHECK(spaceButton.GetLatestSample(playerFramebuffer).sample.IsPressed() == false);
    CHECK(spaceButton.GetState(playerFramebuffer).wasPressedThisIOFrame == false);
    CHECK(spaceButton.GetState(playerFramebuffer).wasReleasedThisIOFrame == true);

    InputSwapFramebuffer(playerFramebuffer);
    CHECK(spaceButton.GetLatestSample(playerFramebuffer).sample.IsPressed() == false);
    CHECK(spaceButton.GetState(playerFramebuffer).wasPressedThisIOFrame == false);
    CHECK(spaceButton.GetState(playerFramebuffer).wasReleasedThisIOFrame == false);

    spaceButton[InputButtonControlRef::AxisOneWays::As].Ingress({}, {1.0f});
    InputSwapFramebuffer(playerFramebuffer);
    CHECK(spaceButton.GetLatestSample(playerFramebuffer).sample.IsPressed() == true);
    CHECK(spaceButton.GetState(playerFramebuffer).wasPressedThisIOFrame == true);
    CHECK(spaceButton.GetState(playerFramebuffer).wasReleasedThisIOFrame == false);

    {
        constexpr uint32_t count = 10;
        std::vector<InputControlTimestamp> timestamps(count);
        std::vector<InputButtonControlSample> samples(count);
        for(uint32_t i = 0; i < count; ++i)
        {
            // 0 1 2 3 4 5 6 7 8 9
            timestamps[i].timestamp = i;
            // 0 1 1 0 1 1 0 1 1 0
            samples[i].value = i % 3 ? 1 : 0;
        }

        spaceButton.Ingress(timestamps.data(), samples.data(), count);
        InputSwapFramebuffer(playerFramebuffer);

        auto recording = spaceButton.GetRecording(playerFramebuffer);
        std::vector<InputControlTimestamp> recordingTimestamps(recording.timestamps, recording.timestamps + recording.count);
        std::vector<InputButtonControlSample> recordingSamples(recording.samples, recording.samples + recording.count);

        // 0 1 3 4 6 7 9
        std::vector<InputControlTimestamp> expectedTimestamps({ {0}, {1}, {3}, {4}, {6}, {7}, {9} });
        // 0 1 0 1 0 1 0
        std::vector<InputButtonControlSample> expectedSamples({ {0}, {1}, {0}, {1}, {0}, {1}, {0} });

        CHECK_THAT(recordingTimestamps, Catch::Matchers::Equals(expectedTimestamps));
        CHECK_THAT(recordingSamples, Catch::Matchers::Equals(expectedSamples));

    }

#if 0
    {
    const uint32_t count = 1000;
    std::vector<InputControlTimestamp> timestamps(count);
    std::vector<InputButtonControlSample> samplesDifferent(count);
    std::vector<InputButtonControlSample> samplesSame(count);
    for(uint32_t i = 0; i < count; ++i)
    {
        timestamps[i].timestamp = i;
        samplesDifferent[i].value = i % 2 ? 1 : 0;
        samplesSame[i].value = 1;
    }

    BENCHMARK("Ingress 1000 different button states and swap")
    {
        spaceButton.Ingress(timestamps.data(), samplesDifferent.data(), count);
        InputSwapFramebuffer(playerFramebuffer);
    };

    BENCHMARK("Ingress 1000 same button states and swap")
    {
        spaceButton.Ingress(timestamps.data(), samplesSame.data(), count);
        InputSwapFramebuffer(playerFramebuffer);
    };

    BENCHMARK("Ingress 1 button state and swap")
    {
        spaceButton.Ingress(timestamps.data(), samplesSame.data(), 1);
        InputSwapFramebuffer(playerFramebuffer);
    };

    BENCHMARK("Ingress 1000 button states in many calls and swap")
    {
        for(uint32_t i = 0; i < count; ++i)
            spaceButton.Ingress({i}, (i % 2 ? InputButtonControlSamplePressed : InputButtonControlSampleReleased));
        InputSwapFramebuffer(playerFramebuffer);
    };
    }
#endif

    InputRemoveDevice(deviceRef);
}

TEST_CASE_METHOD(WithPlayerFramebuffer, "Basic mouse", "[InputNative.Basics]" )
{
    const auto recordingMode = GENERATE(InputControlRecordingMode::Disabled, InputControlRecordingMode::LatestOnly, InputControlRecordingMode::AllMerged, InputControlRecordingMode::AllAsIs);

    const auto deviceRef = InputInstantiateDevice(InputGuidFromString("b642521e-7c4b-45d0-b3b7-6084e786aa22"), {});
    CHECK(deviceRef != InputDeviceRefInvalid);

    auto mouse = AsTrait<InputMouse>(deviceRef);
    auto scroll = mouse.ScrollDeltaVector2D();
    auto upButton = scroll.UpButton();
    auto downButton = scroll.DownButton();

    InputSetRecordingMode(scroll.controlRef, recordingMode);

    constexpr uint32_t count = 10;
    std::vector<InputControlTimestamp> timestamps(count);
    std::vector<InputDeltaVector2DControlSample> samples(count);
    for(uint32_t i = 0; i < count; ++i)
    {
        //  0  1  2  3  4  5  6  7  8  9
        timestamps[i].timestamp = i;
        // -1  1  1 -1  1  1 -1  1  1 -1
        samples[i].y = i % 3 ? 1 : -1;
    }

    scroll.Ingress(timestamps.data(), samples.data(), count);

    InputSwapFramebuffer(playerFramebuffer);

    const auto state = scroll.GetState(playerFramebuffer);
    const auto latestSample = scroll.GetLatestSample(playerFramebuffer);
    const auto recording = scroll.GetRecording(playerFramebuffer);

    switch(recordingMode)
    {
    case InputControlRecordingMode::Disabled:
        CHECK(latestSample.timestamp.timestamp == 0);
        CHECK(latestSample.sample.y == 0.0f);
        CHECK(upButton.GetState(playerFramebuffer).wasPressedThisIOFrame == false);
        CHECK(upButton.GetState(playerFramebuffer).wasReleasedThisIOFrame == false);
        CHECK(downButton.GetState(playerFramebuffer).wasPressedThisIOFrame == false);
        CHECK(downButton.GetState(playerFramebuffer).wasReleasedThisIOFrame == false);
        break;
    case InputControlRecordingMode::LatestOnly:
        CHECK(latestSample.timestamp.timestamp == 9);
        CHECK(latestSample.sample.y == 2.0f);
        CHECK(upButton.GetState(playerFramebuffer).wasPressedThisIOFrame == true);
        CHECK(upButton.GetState(playerFramebuffer).wasReleasedThisIOFrame == true);
        CHECK(downButton.GetState(playerFramebuffer).wasPressedThisIOFrame == true);
        CHECK(downButton.GetState(playerFramebuffer).wasReleasedThisIOFrame == true);
        break;
    case InputControlRecordingMode::AllMerged:
        CHECK(latestSample.timestamp.timestamp == 9);
        CHECK(latestSample.sample.y == 2.0f);
        CHECK(upButton.GetState(playerFramebuffer).wasPressedThisIOFrame == true);
        CHECK(upButton.GetState(playerFramebuffer).wasReleasedThisIOFrame == true);
        CHECK(downButton.GetState(playerFramebuffer).wasPressedThisIOFrame == true);
        CHECK(downButton.GetState(playerFramebuffer).wasReleasedThisIOFrame == true);
        break;
    case InputControlRecordingMode::AllAsIs:
        CHECK(latestSample.timestamp.timestamp == 9);
        CHECK(latestSample.sample.y == -1.0f);
        CHECK(upButton.GetState(playerFramebuffer).wasPressedThisIOFrame == true);
        CHECK(upButton.GetState(playerFramebuffer).wasReleasedThisIOFrame == true);
        CHECK(downButton.GetState(playerFramebuffer).wasPressedThisIOFrame == true);
        CHECK(downButton.GetState(playerFramebuffer).wasReleasedThisIOFrame == true);
        break;
    }

    InputSwapFramebuffer(playerFramebuffer);
    CHECK(scroll.GetLatestSample(playerFramebuffer).sample.y == 0.0f);

    if (recordingMode != InputControlRecordingMode::Disabled)
    {
        upButton.Ingress({}, InputButtonControlSamplePressed);
        upButton.Ingress({}, InputButtonControlSamplePressed);
        InputSwapFramebuffer(playerFramebuffer);

        const float expectedValue = (recordingMode != InputControlRecordingMode::AllAsIs ? 2.0f : 1.0f);
        CHECK(scroll.GetLatestSample(playerFramebuffer).sample.y == expectedValue);
    }

    InputRemoveDevice(deviceRef);
}

TEST_CASE("Can parse guid", "[InputNative.Basics]" )
{
    const char* guidString = "d8c9e8d6-9fca-4177-a288-29d4eefd893d";
    auto guid = InputGuidFromString(guidString);

    CHECK(guid.a == 0x7741ca9fd6e8c9d8ull);
    CHECK(guid.b == 0x3d89fdeed42988a2ull);

    char buffer[37] = {};
    InputGuidToString(guid, buffer, sizeof(buffer));
    CHECK_THAT(buffer, Catch::Matchers::Equals(guidString));
}

#endif