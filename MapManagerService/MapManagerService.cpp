#include <windows.h>
#include <tchar.h>
#include <string>
#include <fstream>
#include <sstream>

// Имя службы
#define SERVICE_NAME _T("OsuMapManagerService")

// Состояние службы
SERVICE_STATUS ServiceStatus;
SERVICE_STATUS_HANDLE ServiceStatusHandle;
HANDLE StopEvent = nullptr;

// Путь к ключам реестра
const std::wstring RegistryPath = L"SOFTWARE\\OsuMapManagerService";
const std::wstring LaunchOnBootKey = L"LaunchOnBoot";
const std::wstring StopOnOsuKey = L"StopOnOsu";
const std::wstring MainAppPathKey = L"MainAppPath";

// Логирование
void WriteToLog(const std::wstring& message) {
    try {
        // Определяем путь для логирования
        wchar_t exePath[MAX_PATH];
        GetModuleFileName(nullptr, exePath, MAX_PATH);
        std::wstring logFilePath = std::wstring(exePath);
        size_t pos = logFilePath.find_last_of(L"\\/");
        if (pos != std::wstring::npos) {
            logFilePath = logFilePath.substr(0, pos) + L"\\ServiceLogs.txt";
        }

        // Открываем файл логов и записываем сообщение
        std::wofstream logFile(logFilePath, std::ios::app);
        if (logFile.is_open()) {
            SYSTEMTIME time;
            GetLocalTime(&time);

            logFile << L"[" << time.wYear << L"-" << time.wMonth << L"-" << time.wDay << L" "
                << time.wHour << L":" << time.wMinute << L":" << time.wSecond << L"] "
                << message << std::endl;
            logFile.close();
        }
    }
    catch (...) {
        // Игнорируем ошибки логирования
    }
}

// Чтение строки из реестра
std::wstring ReadRegistryString(const std::wstring& key) {
    HKEY hKey;
    wchar_t buffer[MAX_PATH] = {};
    DWORD bufferSize = sizeof(buffer);

    if (RegOpenKeyEx(HKEY_LOCAL_MACHINE, RegistryPath.c_str(), 0, KEY_READ, &hKey) == ERROR_SUCCESS) {
        RegQueryValueEx(hKey, key.c_str(), nullptr, nullptr, reinterpret_cast<LPBYTE>(&buffer), &bufferSize);
        RegCloseKey(hKey);
    }
    return std::wstring(buffer);
}

// Чтение значения DWORD из реестра
bool ReadRegistryBool(const std::wstring& key) {
    HKEY hKey;
    DWORD value = 0;
    DWORD valueSize = sizeof(value);

    if (RegOpenKeyEx(HKEY_LOCAL_MACHINE, RegistryPath.c_str(), 0, KEY_READ, &hKey) == ERROR_SUCCESS) {
        RegQueryValueEx(hKey, key.c_str(), nullptr, nullptr, reinterpret_cast<LPBYTE>(&value), &valueSize);
        RegCloseKey(hKey);
    }
    return value != 0;
}

// Создание ключей реестра, если их нет
void EnsureRegistryKeysExist() {
    HKEY hKey;
    LONG result = RegCreateKeyEx(HKEY_LOCAL_MACHINE, RegistryPath.c_str(), 0, nullptr,
        REG_OPTION_NON_VOLATILE, KEY_ALL_ACCESS, nullptr, &hKey, nullptr);
    if (result == ERROR_SUCCESS) {
        DWORD launchOnBoot = 1; // Включено по умолчанию
        if (RegQueryValueEx(hKey, LaunchOnBootKey.c_str(), nullptr, nullptr, nullptr, nullptr) != ERROR_SUCCESS) {
            RegSetValueEx(hKey, LaunchOnBootKey.c_str(), 0, REG_DWORD, reinterpret_cast<BYTE*>(&launchOnBoot), sizeof(DWORD));
            WriteToLog(L"Добавлен ключ реестра: LaunchOnBoot.");
        }

        DWORD stopOnOsu = 1; // Включено по умолчанию
        if (RegQueryValueEx(hKey, StopOnOsuKey.c_str(), nullptr, nullptr, nullptr, nullptr) != ERROR_SUCCESS) {
            RegSetValueEx(hKey, StopOnOsuKey.c_str(), 0, REG_DWORD, reinterpret_cast<BYTE*>(&stopOnOsu), sizeof(DWORD));
            WriteToLog(L"Добавлен ключ реестра: StopOnOsu.");
        }

        wchar_t exePath[MAX_PATH];
        GetModuleFileName(nullptr, exePath, MAX_PATH);
        std::wstring mainAppPath = std::wstring(exePath);
        if (RegQueryValueEx(hKey, MainAppPathKey.c_str(), nullptr, nullptr, nullptr, nullptr) != ERROR_SUCCESS) {
            RegSetValueEx(hKey, MainAppPathKey.c_str(), 0, REG_SZ,
                reinterpret_cast<const BYTE*>(mainAppPath.c_str()),
                static_cast<DWORD>((mainAppPath.size() + 1) * sizeof(wchar_t)));
            WriteToLog(L"Добавлен ключ реестра: MainAppPath.");
        }

        RegCloseKey(hKey);
    }
    else {
        WriteToLog(L"Ошибка создания ключей реестра.");
    }
}

// Запуск основного приложения
void LaunchMainApp(const std::wstring& appPath) {
    STARTUPINFO si = { sizeof(STARTUPINFO) };
    PROCESS_INFORMATION pi;

    if (!CreateProcess(appPath.c_str(), nullptr, nullptr, nullptr, FALSE, 0, nullptr, nullptr, &si, &pi)) {
        WriteToLog(L"Ошибка запуска основного приложения.");
    }
    else {
        WriteToLog(L"Основное приложение запущено.");
        CloseHandle(pi.hProcess);
        CloseHandle(pi.hThread);
    }
}

// Остановка основного приложения
void StopMainApp() {
    WriteToLog(L"Попытка остановки основного приложения.");
    system("taskkill /F /IM MapManager.exe");
}

// Основной поток службы
void ServiceWorkerThread() {
    WriteToLog(L"Служба начала выполнение.");
    EnsureRegistryKeysExist();

    bool isMainAppRunning = false;

    while (WaitForSingleObject(StopEvent, 1000) == WAIT_TIMEOUT) {
        bool launchOnBoot = ReadRegistryBool(LaunchOnBootKey);
        bool stopOnOsu = ReadRegistryBool(StopOnOsuKey);
        std::wstring mainAppPath = ReadRegistryString(MainAppPathKey);

        if (launchOnBoot && !isMainAppRunning && !mainAppPath.empty()) {
            LaunchMainApp(mainAppPath);
            isMainAppRunning = true;
        }

        if (stopOnOsu) {
            HWND osuWindow = FindWindow(nullptr, L"osu!");
            if (osuWindow && isMainAppRunning) {
                StopMainApp();
                isMainAppRunning = false;
            }
        }
    }

    WriteToLog(L"Служба завершила выполнение.");
}

// Обработчик управления службой
void WINAPI ServiceControlHandler(DWORD controlCode) {
    switch (controlCode) {
    case SERVICE_CONTROL_STOP:
        ServiceStatus.dwCurrentState = SERVICE_STOP_PENDING;
        SetServiceStatus(ServiceStatusHandle, &ServiceStatus);

        SetEvent(StopEvent);

        ServiceStatus.dwCurrentState = SERVICE_STOPPED;
        SetServiceStatus(ServiceStatusHandle, &ServiceStatus);
        break;

    default:
        SetServiceStatus(ServiceStatusHandle, &ServiceStatus);
        break;
    }
}

// Точка входа службы
void WINAPI ServiceMain(DWORD argc, LPTSTR* argv) {
    ServiceStatusHandle = RegisterServiceCtrlHandler(SERVICE_NAME, ServiceControlHandler);
    if (!ServiceStatusHandle) {
        return;
    }

    ServiceStatus.dwServiceType = SERVICE_WIN32_OWN_PROCESS;
    ServiceStatus.dwCurrentState = SERVICE_START_PENDING;
    ServiceStatus.dwControlsAccepted = SERVICE_ACCEPT_STOP;

    SetServiceStatus(ServiceStatusHandle, &ServiceStatus);

    StopEvent = CreateEvent(nullptr, TRUE, FALSE, nullptr);
    if (!StopEvent) {
        ServiceStatus.dwCurrentState = SERVICE_STOPPED;
        SetServiceStatus(ServiceStatusHandle, &ServiceStatus);
        return;
    }

    ServiceStatus.dwCurrentState = SERVICE_RUNNING;
    SetServiceStatus(ServiceStatusHandle, &ServiceStatus);

    ServiceWorkerThread();
}

// Установка службы
void InstallService() {
    SC_HANDLE hSCManager = OpenSCManager(nullptr, nullptr, SC_MANAGER_CREATE_SERVICE);
    if (!hSCManager) {
        WriteToLog(L"Не удалось открыть SCManager.");
        return;
    }

    wchar_t exePath[MAX_PATH];
    GetModuleFileName(nullptr, exePath, MAX_PATH);

    SC_HANDLE hService = CreateService(
        hSCManager,
        SERVICE_NAME,
        SERVICE_NAME,
        SERVICE_ALL_ACCESS,
        SERVICE_WIN32_OWN_PROCESS,
        SERVICE_AUTO_START,
        SERVICE_ERROR_NORMAL,
        exePath,
        nullptr,
        nullptr,
        nullptr,
        nullptr,
        nullptr);

    if (!hService) {
        WriteToLog(L"Не удалось создать службу.");
    }
    else {
        WriteToLog(L"Служба успешно создана.");
        CloseServiceHandle(hService);
    }

    CloseServiceHandle(hSCManager);
}

// Основная функция
int _tmain(int argc, TCHAR* argv[]) {
    if (argc > 1 && _tcscmp(argv[1], _T("install")) == 0) {
        InstallService();
        return 0;
    }

    SERVICE_TABLE_ENTRY ServiceTable[] = {
        { const_cast<LPWSTR>(SERVICE_NAME), ServiceMain },
        { nullptr, nullptr }
    };

    if (!StartServiceCtrlDispatcher(ServiceTable)) {
        WriteToLog(L"Ошибка запуска диспетчера службы.");
    }

    return 0;
}
