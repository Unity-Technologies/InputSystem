#include <InputRuntime.h>

//#include <Input.Device.Catalog.h>
//#include <Input.Control.Catalog.h>
//#include <Input.Context.h>
//#include <Input.PAL.Baselib.h>
//#include <Input.PAL.Windows.h>
//#include <Input.PAL.SDL.h>
//#include <Input.Tests.h>
//
//#include <Include/Baselib.h>
//#include <Include/C/Baselib_Thread.h>
//#include <Include/C/Baselib_Memory.h>

#include <windows.h>

#include "_BuiltInDeviceDatabase.h"

#include <vector>

class WndClass
{
public:
    static const wchar_t* ClassName;

    static bool shouldRun;

    void Register()
    {
        WNDCLASSEXW wc = {};
        wc.cbSize = sizeof(WNDCLASSEXW);
        wc.style = CS_HREDRAW | CS_VREDRAW | CS_OWNDC;
        wc.lpfnWndProc = WndProc;
        wc.hInstance = GetModuleHandle(NULL);
        wc.hIcon = NULL;
        wc.hCursor = LoadCursor(NULL, IDC_ARROW);
        wc.lpszClassName = ClassName;
        if (!RegisterClassExW(&wc))
        {
        }
    }

    void Unregister()
    {
        if (!UnregisterClassW(ClassName, GetModuleHandle(NULL)))
        {
        }
    }

    static LRESULT CALLBACK WndProc(HWND hWnd, UINT message, WPARAM wParam, LPARAM lParam)
    {
        LRESULT inputEarlyOutResult;
//        if(Input_PAL_Windows_WndProcCallback(hWnd, message, wParam, lParam, &inputEarlyOutResult))
//            return inputEarlyOutResult;

        //printf("hello?\n");
        //fflush(stdout);
        switch (message)
        {
        case WM_PAINT:
        {
            PAINTSTRUCT ps;
            HDC hdc = BeginPaint(hWnd, &ps);
            FillRect(hdc, &ps.rcPaint, (HBRUSH)COLOR_BACKGROUND);
            EndPaint(hWnd, &ps);
            ValidateRect(hWnd, NULL);
            return 0;
        }
        case WM_SIZING:
            return TRUE;
        case WM_CLOSE:
            shouldRun = false;
            return 0;
        case WM_QUIT:
            shouldRun = false;
            return 0;
        case WM_SYSKEYUP:
        case WM_KEYUP:
            if (wParam == VK_ESCAPE)
            {
                shouldRun = false;
                return 0;
            }
            break;
        default:
            break;
        }
        return DefWindowProcW(hWnd, message, wParam, lParam);
    }
};

const wchar_t* WndClass::ClassName = L"Unity.Input.Standalone";
bool WndClass::shouldRun = true;

class Window
{
public:
    void Create()
    {
        DWORD dwStyle = WS_OVERLAPPEDWINDOW;
        RECT rc = { 0, 0, 1024, 768 };
        AdjustWindowRect(&rc, dwStyle, FALSE);

        hWnd = CreateWindowW(
            WndClass::ClassName,
            L"Hello",
            dwStyle,
            CW_USEDEFAULT,
            CW_USEDEFAULT,
            rc.right - rc.left,
            rc.bottom - rc.top,
            NULL,
            NULL,
            GetModuleHandle(NULL),
            NULL
        );

        if (!hWnd)
        {
        }

        ShowWindow(hWnd, SW_SHOW);
        SetForegroundWindow(hWnd);
        SetFocus(hWnd);
    }

    void Destroy()
    {
        if (!DestroyWindow(hWnd))
        {
        }

        hWnd = nullptr;
    }

public:
    HWND hWnd;
};


int main()
{
    InputSetPALCallbacks(
        {
            [](const char* msg)
            {
              printf("%s\n", msg);
              fflush(stdout);
            },
            []()
            {
              //__debugbreak();
            }
        }
    );

    InputRuntimeRunNativeTests();

//    InputKeyboard asd;
//    asd[InputKeyboard::Buttons::Space];

//    Input_PAL_Baselib_SetCallbacks();
//
//    Input_Tests_RunAll();


//    Input_RuntimeContext ctx = {};
//    Input_SetRuntimeContext_ForNative(&ctx);
//
    InputRuntimeInit(1);

    const auto deviceRef = InputInstantiateDevice(InputGuidFromString("8d37e884-458e-4b1d-805f-95425987e9d1"), {});

    AsTrait<InputKeyboard>(deviceRef)[InputKeyboard::Buttons::Space];

//
//    Input_PAL_SDL_Looper();

//    Input_Device_PersistentId persistentId = {};
//    Input_Device_RuntimeRegistration_PlatformData platformData = {};
//
//    std::vector<Input_Control_Usage> usages;
//    usages.push_back({(uint32_t)Input_Control_UsagePage_BuiltIn::GenericButtons, (uint32_t)Input_Control_Usage_GenericButton::Button1});
//    usages.push_back({(uint32_t)Input_Control_UsagePage_BuiltIn::GenericButtons, (uint32_t)Input_Control_Usage_GenericButton::Button2});
//    usages.push_back({(uint32_t)Input_Control_UsagePage_BuiltIn::GenericButtons, (uint32_t)Input_Control_Usage_GenericButton::Button3});
//
//    Input_Device_SessionId deviceSessionId = Input_RegisterDevice(
//        &persistentId,
//        "test device",
//        usages.data(),
//        (uint32_t)usages.size(),
//        platformData
//    );
//
//    std::vector<Input_Control_Button_Sample> samples;
//    samples.push_back(true);
//    samples.push_back(false);
//    samples.push_back(false);
//    Input_Ingress(usages[0], deviceSessionId, samples.data(), (uint32_t)samples.size());
//
//    const bool* t = (bool*)Input_GetLatestSample(usages[0], deviceSessionId);
//    printf("hello %u\n", *t);
//
//    Input_RemoveDevice(deviceSessionId);

    InputRuntimeDeinit();

//    //std::cout << "Hello, World!" << std::endl;
//
//    Input_Device_Catalog devctlg = {};
//    Input_Device_Catalog_Init(&devctlg);
//
//    Input_Device_PersistentId t = {};
//    Input_Device_SessionId s = Input_Device_Catalog_RegisterDevice(
//        &devctlg,
//        &t,
//        nullptr, // ???
//        0,
//        0
//    );

//    WndClass wndClass = {};
//    wndClass.Register();
//
//    Window window = {};
//    window.Create();
//
//    while (WndClass::shouldRun)
//    {
//        MSG msg;
//        while (PeekMessage(&msg, window.hWnd, 0, 0, PM_REMOVE))
//        {
//            TranslateMessage(&msg);
//            DispatchMessage(&msg);
//        }
//    }
//
//    window.Destroy();
//
//    wndClass.Unregister();

    return 0;
}
