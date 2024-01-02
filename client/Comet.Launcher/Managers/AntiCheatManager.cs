using System.Diagnostics;
using System.Runtime.InteropServices;
using Comet.Launcher.Threads;

namespace Comet.Launcher.Managers
{
    internal static class AntiCheatManager
    {
        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern IntPtr GetModuleHandle(string lib);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern IntPtr GetProcAddress(IntPtr module, string function);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool WriteProcessMemory(IntPtr procHandle, IntPtr baseAddress, byte[] buffer, uint size,
                                                      int numOfBytes);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool ReadProcessMemory(IntPtr procHandle, IntPtr baseAddress, byte[] buffer, uint size,
                                                     int numOfBytes);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern IntPtr GetForegroundWindow(IntPtr windowHandle);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool VirtualProtect(IntPtr address, uint size, uint newProtect, uint oldProtect);

        [DllImport("ntdll.dll", SetLastError = true)]
        private static extern IntPtr NtSetInformationThread(IntPtr threadHandle, uint threadInfoClass,
                                                            IntPtr threadInfo, uint threadInfoLength);

        [DllImport("kernel32.dll")]
        private static extern IntPtr OpenThread(uint dwDesiredAccess, bool bInheritHandle, uint dwThreadId);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern IntPtr GetCurrentThread();

        public static void AntiUnHookerNormal(AntiCheatThread.IllegalActionDelegate action)
        {
            IntPtr kernelModule = GetModuleHandle("kernelbase.dll");
            IntPtr loadLibraryWAddress = GetProcAddress(kernelModule, "LoadLibraryW");
            IntPtr loadLibraryAAddress = GetProcAddress(kernelModule, "LoadLibraryA");
            var loadLibraryWCode = new byte[1];
            var loadLibraryACode = new byte[1];
            Marshal.Copy(loadLibraryWAddress, loadLibraryWCode, 0, 1);
            Marshal.Copy(loadLibraryAAddress, loadLibraryACode, 0, 1);
            if (loadLibraryWCode[0] == 0xCC == false || loadLibraryACode[0] == 0xCC == false)
            {
                action?.Invoke();
            }
        }

        public static void AntiUnHookerAggressive(AntiCheatThread.IllegalActionDelegate action)
        {
            IntPtr kernelModule = GetModuleHandle("kernelbase.dll");
            IntPtr ntdllModule = GetModuleHandle("ntdll.dll");
            IntPtr loadLibraryWAddress = GetProcAddress(kernelModule, "LoadLibraryW");
            IntPtr loadLibraryAAddress = GetProcAddress(kernelModule, "LoadLibraryA");
            IntPtr loadLibraryExAAddress = GetProcAddress(kernelModule, "LoadLibraryExA");
            IntPtr loadLibraryExWAddress = GetProcAddress(kernelModule, "LoadLibraryExW");
            IntPtr ldrLoadDllAddress = GetProcAddress(ntdllModule, "LdrLoadDll");
            var loadLibraryWCode = new byte[1];
            var loadLibraryACode = new byte[1];
            var loadLibraryExACode = new byte[1];
            var loadLibraryExWCode = new byte[1];
            var ldrLoadDllCode = new byte[1];
            Marshal.Copy(loadLibraryWAddress, loadLibraryWCode, 0, 1);
            Marshal.Copy(loadLibraryAAddress, loadLibraryACode, 0, 1);
            Marshal.Copy(loadLibraryExAAddress, loadLibraryExACode, 0, 1);
            Marshal.Copy(loadLibraryExWAddress, loadLibraryExWCode, 0, 1);
            Marshal.Copy(ldrLoadDllAddress, ldrLoadDllCode, 0, 1);
            if (loadLibraryACode[0] == 0xCC == false || loadLibraryWCode[0] == 0xCC == false ||
                loadLibraryExACode[0] == 0xCC == false || loadLibraryExWCode[0] == 0xCC == false ||
                ldrLoadDllCode[0] == 0xCC == false)
            {
                action?.Invoke();
            }
        }

        public static void LockDownLibraryLoadingNormal()
        {
            IntPtr kernelModule = GetModuleHandle("kernelbase.dll");
            IntPtr loadLibraryWAddress = GetProcAddress(kernelModule, "LoadLibraryW");
            IntPtr loadLibraryAAddress = GetProcAddress(kernelModule, "LoadLibraryA");
            byte[] int3InvalidCode = {0xCC};
            WriteProcessMemory(Process.GetCurrentProcess().Handle, loadLibraryWAddress, int3InvalidCode, 6, 0);
            WriteProcessMemory(Process.GetCurrentProcess().Handle, loadLibraryAAddress, int3InvalidCode, 6, 0);
        }

        public static void LockDownLibraryLoadingAggressive()
        {
            IntPtr kernelModule = GetModuleHandle("kernelbase.dll");
            IntPtr ntdllModule = GetModuleHandle("ntdll.dll");
            IntPtr loadLibraryWAddress = GetProcAddress(kernelModule, "LoadLibraryW");
            IntPtr loadLibraryAAddress = GetProcAddress(kernelModule, "LoadLibraryA");
            IntPtr loadLibraryExAAddress = GetProcAddress(kernelModule, "LoadLibraryExA");
            IntPtr loadLibraryExWAddress = GetProcAddress(kernelModule, "LoadLibraryExW");
            IntPtr ldrLoadDllAddress = GetProcAddress(ntdllModule, "LdrLoadDll");
            byte[] int3InvalidCode = {0xCC};
            WriteProcessMemory(Process.GetCurrentProcess().Handle, loadLibraryWAddress, int3InvalidCode, 6, 0);
            WriteProcessMemory(Process.GetCurrentProcess().Handle, loadLibraryAAddress, int3InvalidCode, 6, 0);
            WriteProcessMemory(Process.GetCurrentProcess().Handle, loadLibraryExAAddress, int3InvalidCode, 6, 0);
            WriteProcessMemory(Process.GetCurrentProcess().Handle, loadLibraryExWAddress, int3InvalidCode, 6, 0);
            WriteProcessMemory(Process.GetCurrentProcess().Handle, ldrLoadDllAddress, int3InvalidCode, 6, 0);
        }

        [Flags]
        public enum Protections
        {
            PAGE_NOACCESS = 0x01,
            PAGE_READONLY = 0x02,
            PAGE_READWRITE = 0x04,
            PAGE_WRITECOPY = 0x08,
            PAGE_GUARD = 0x100,
            PAGE_NOCACHE = 0x200,
            PAGE_WRITECOMBINE = 0x400,
            PAGE_TARGETS_INVALID = 0x40000000
        }

        public static void ChangeMemoryPageAccess(IntPtr address, uint size, uint newProtection)
        {
            uint oldProtect = 0;
            VirtualProtect(address, size, newProtection, oldProtect);
        }

        public static void InjectCodeToFunction(byte[] assemblyCode, string libraryOfFunction, string function)
        {
            IntPtr libraryModule = GetModuleHandle(libraryOfFunction);
            IntPtr functionAddress = GetProcAddress(libraryModule, function);
            WriteProcessMemory(Process.GetCurrentProcess().Handle, functionAddress, assemblyCode, 6, 0);
        }
    }
}