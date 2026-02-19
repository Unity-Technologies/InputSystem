#if defined(__APPLE__)
#include <TargetConditionals.h>
#if TARGET_OS_IOS

#include <CoreMotion/CoreMotion.h>

#define ENABLE_STEP_COUNTER_LOGGING 1
#if ENABLE_STEP_COUNTER_LOGGING
#define STEP_COUNTER_LOG(...) NSLog(@"StepCounter - %@", [NSString stringWithFormat: __VA_ARGS__])
#else
#define STEP_COUNTER_LOG(...) {}
#endif

class iOSStepCounterWrapper
{
public:
    typedef void (*OnDataReceived) (int deviceId, int numberOfSteps);

    struct iOSStepCounterCallbacks
    {
        OnDataReceived dataReceived;
    };


    iOSStepCounterWrapper()
    {
        m_Pedometer = nullptr;
        m_Device = -1;
    }

    void Enable(int deviceId, iOSStepCounterCallbacks* callbacks)
    {
        if (IsEnabled())
        {
            STEP_COUNTER_LOG(@"Was already enabled?");
            if (m_Device != deviceId)
                STEP_COUNTER_LOG(@"Enabling with different device id? Expected %d, was %d. Are you creating more than one iOSStepCounter", m_Device, deviceId);
        }
        m_Pedometer = [[CMPedometer alloc]init];
        m_Callbacks = *callbacks;
        m_Device = deviceId;
        [m_Pedometer startPedometerUpdatesFromDate: [NSDate date] withHandler:^(CMPedometerData * _Nullable pedometerData, NSError * _Nullable error)
        {
            // Note: We need to call our callback on the same thread the Unity scripting is operating
            dispatch_async(dispatch_get_main_queue(), ^{
                if (error != nil)
                {
                    STEP_COUNTER_LOG(@"startPedometerUpdatesFromDate threw an error '%@', was it authorized?", [error localizedDescription]);
                    if (m_Device != -1)
                        Disable(m_Device);
                    return;
                }
                // Guard against situation where device was disabled, any event which are received after that, should be ignored.
                if (m_Device == -1)
                    return;
                m_Callbacks.dataReceived(m_Device, [pedometerData.numberOfSteps intValue]);
            });
        }];
    }

    bool Disable(int deviceId)
    {
        if (m_Pedometer == nullptr)
            return false;
        if (m_Device != deviceId)
            STEP_COUNTER_LOG(@"Disabling with wrong device id, expected %d, was %d", m_Device, deviceId);
        [m_Pedometer stopPedometerUpdates];
        m_Pedometer = nullptr;
        m_Device = -1;
        return true;
    }

    bool IsEnabled() const
    {
        return m_Pedometer != nullptr;
    }

private:
    CMPedometer* m_Pedometer;
    iOSStepCounterCallbacks m_Callbacks;
    int m_Device;
};

static iOSStepCounterWrapper s_Wrapper;
static const int kResultSuccess = 1;
static const int kResultFailure = -1;

extern "C" int _iOSStepCounterIsAvailable()
{
    return [CMPedometer isStepCountingAvailable] ? 1 : 0;
}

extern "C" int _iOSStepCounterGetAuthorizationStatus()
{
    if (@available(iOS 11.0, *))
    {
        return (int)[CMPedometer authorizationStatus];
    }
    return 0;
}

extern "C" int _iOSStepCounterEnable(int deviceId, iOSStepCounterWrapper::iOSStepCounterCallbacks* callbacks, int sizeOfCallbacks)
{
    if (sizeof(iOSStepCounterWrapper::iOSStepCounterCallbacks) != sizeOfCallbacks)
    {
        STEP_COUNTER_LOG(@"iOSStepCounterCallbacks size mismatch, expected %lu was %d", sizeof(iOSStepCounterWrapper::iOSStepCounterCallbacks), sizeOfCallbacks);
        return kResultFailure;
    }

    if (_iOSStepCounterIsAvailable() == 0)
    {
        STEP_COUNTER_LOG(@"Step counting is not available");
        return kResultFailure;
    }

    NSString* motionUsage = @"NSMotionUsageDescription";

    if ([[NSBundle mainBundle] objectForInfoDictionaryKey: motionUsage] == nil)
    {
        STEP_COUNTER_LOG(@"%@ is missing in Info.plist, please enable Motion Usage in Input Settings", motionUsage);
        return kResultFailure;
    }

    if (@available(iOS 11.0, *))
    {
        if ([CMPedometer authorizationStatus] == CMAuthorizationStatusRestricted)
        {
            STEP_COUNTER_LOG(@"Step Counter was restricted.");
            return kResultFailure;
        }

        if ([CMPedometer authorizationStatus] == CMAuthorizationStatusDenied)
        {
            STEP_COUNTER_LOG(@"Step Counter was denied. Enable Motion & Fitness under app settings.");
            return kResultFailure;
        }
        // Do nothing for Authorized and NotDetermined
    }

    // Note: After installation this function will prompt a dialog asking about Motion & Fitness authorization
    //       If user denies the prompt, there will be an error in startPedometerUpdatesFromDate callback
    //       The dialog only appears once, not sure how to trigger it again, besides reinstalling app
    s_Wrapper.Enable(deviceId, callbacks);

    return kResultSuccess;
}

extern "C" int _iOSStepCounterDisable(int deviceId)
{
    return s_Wrapper.Disable(deviceId) ? kResultSuccess : kResultFailure;
}

extern "C" int _iOSStepCounterIsEnabled(int deviceId)
{
    return s_Wrapper.IsEnabled() ? 1 : 0;
}

#endif
#endif
