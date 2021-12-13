﻿#include "pch.h"

#include <common/utils/ProcessWaiter.h>
#include <common/utils/window.h>
#include <common/utils/UnhandledExceptionHandler_x64.h>

#include <common/utils/logger_helper.h>

#include <AlwaysOnTop.h>
#include <trace.h>

// Non-localizable
const std::wstring moduleName = L"AlwaysOnTop";
const std::wstring internalPath = L"";
const std::wstring instanceMutexName = L"Local\\PowerToys_AlwaysOnTop_InstanceMutex";

int WINAPI wWinMain(_In_ HINSTANCE hInstance, _In_opt_ HINSTANCE hPrevInstance, _In_ PWSTR lpCmdLine, _In_ int nCmdShow)
{
    winrt::init_apartment();
    LoggerHelpers::init_logger(moduleName, internalPath, LogSettings::alwaysOnTopLoggerName);
    InitUnhandledExceptionHandler_x64();    

    auto mutex = CreateMutex(nullptr, true, instanceMutexName.c_str());
    if (mutex == nullptr)
    {
        Logger::error(L"Failed to create mutex. {}", get_last_error_or_default(GetLastError()));
    }

    if (GetLastError() == ERROR_ALREADY_EXISTS)
    {
        Logger::warn(L"AlwaysOnTop instance is already running");
        return 0;
    }

    std::wstring pid = std::wstring(lpCmdLine);
    if (!pid.empty())
    {
        auto mainThreadId = GetCurrentThreadId();
        ProcessWaiter::OnProcessTerminate(pid, [mainThreadId](int err) {
            if (err != ERROR_SUCCESS)
            {
                Logger::error(L"Failed to wait for parent process exit. {}", get_last_error_or_default(err));
            }
            else
            {
                Logger::trace(L"PowerToys runner exited.");
            }

            Logger::trace(L"Exiting AlwaysOnTop");
            PostThreadMessage(mainThreadId, WM_QUIT, 0, 0);
        });
    }

    Trace::RegisterProvider();

    AlwaysOnTop app;

    run_message_loop();

    Trace::UnregisterProvider();
    
    return 0;
}
