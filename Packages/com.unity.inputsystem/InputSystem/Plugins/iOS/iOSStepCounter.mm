#include "iOSStepCounter.h"
#include "UnityAppController.h"
#include "UnityForwardDecls.h"
#include <string>
#include <CoreMotion/CoreMotion.h>


typedef void (*OnDataReceived) (int deviceId, int numberOfSteps);

struct iOSStepCounterCallbacks
{
    OnDataReceived dataReceived;
};

static const int kResultSuccess = 1;
static const int kResultFailure = -1;
CMPedometer* m_Pedometer = nullptr;
iOSStepCounterCallbacks m_Callbacks;
int m_Device = -1;
extern "C" int _iOSStepCounterEnable(int deviceId, iOSStepCounterCallbacks* callbacks, int sizeOfCallbacks)
{
    if (sizeof(iOSStepCounterCallbacks) != sizeOfCallbacks)
    {
        NSLog(@"iOSStepCounterCallbacks size mismatch, expected %lu was %d", sizeof(iOSStepCounterCallbacks), sizeOfCallbacks);
        return kResultFailure;
    }
    
    if (![CMPedometer isStepCountingAvailable])
    {
        NSLog(@"Step counting is not available");
        return kResultFailure;
    }
    
    // TODO: on ios 11
    /*
    if ([CMPedometer authorizationStatus] != CMAuthorizationStatusAuthorized)
    {
        NSLog(@"Step counting was not authorized");
        return kResultFailure;
    }
     */

    m_Pedometer = [[CMPedometer alloc]init];
    m_Callbacks = *callbacks;
    m_Device = deviceId;
    [m_Pedometer startPedometerUpdatesFromDate:[NSDate date] withHandler:^(CMPedometerData * _Nullable pedometerData, NSError * _Nullable error)
    {
        // Note: We need to call our call on the same thread the Unity scripting is operating
        dispatch_async(dispatch_get_main_queue(), ^{
            m_Callbacks.dataReceived(m_Device, [pedometerData.numberOfSteps intValue]);
        });
    }];

    return kResultSuccess;
}

extern "C" int _iOSStepCounterDisable(int deviceId)
{
    if (m_Pedometer == nullptr)
        return kResultSuccess;
    [m_Pedometer stopPedometerUpdates];
    m_Pedometer = nullptr;
    m_Device = -1;
    return kResultSuccess;
}

extern "C" int _iOSStepCounterIsEnabled(int deviceId)
{
    return m_Pedometer != nullptr;
}
