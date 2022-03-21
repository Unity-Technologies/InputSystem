#pragma once

#include "Control.Button.h"
#include "Control.AxisOneWay.h"
#include "Control.AxisTwoWay.h"
#include "Control.DeltaAxisTwoWay.h"
#include "Control.Stick.h"
#include "Control.DeltaVector2D.h"
#include "Control.Position2D.h"

// TODO remove this
static inline void _TodoIngress(const InputControlTypeRef controlTypeRef, const InputControlRef controlRef, const InputControlTypeRef samplesType, const InputControlTimestamp* timestamps, const void* samples, const uint32_t count, const InputControlRef fromAnotherControl){}
static inline void _TodoFrameBegin(const InputControlTypeRef controlTypeRef, const InputControlRef* controlRefs, uint8_t* controlStates, InputControlTimestamp* timestamps, uint8_t* samples, const uint32_t controlCount){}

