package com.unity.inputsystem;

import java.text.*;
import android.util.*;

public final class AndroidInputSystem
{
	interface IInputSystemCallbacks
	{
	    int AddDevice(String deviceClass);
		
        void RemoveDevice(int deviceId);

		void QueueScreenKeyboardEvent(int deviceId, int state, float occludingAreaPositionX, float occludingAreaPositionY, float occludingAreaSizeX, float occludingAreaSizeY, String text);
	}
	
	private IInputSystemCallbacks m_Callbacks;
	private static AndroidInputSystem ms_Instance;

	private AndroidInputSystem(IInputSystemCallbacks callbacks)
    {
		m_Callbacks = callbacks;

		int deviceId = m_Callbacks.AddDevice("AndroidScreenKeyboard");
		Log.v("Unity", MessageFormat.format("Device {0} deviceId added", deviceId));
	}

	public static IInputSystemCallbacks getCallbacks()
	{
		return ms_Instance.m_Callbacks;
	}

	public static void initialize(IInputSystemCallbacks callbacks) throws Exception
	{
		if (ms_Instance != null)
			throw new Exception("Only one AndroidInputSystem instance can exist");

		ms_Instance = new AndroidInputSystem(callbacks);
	}

	public static void shutdown()
	{
		ms_Instance = null;
	}
}