using System.Diagnostics;
using System.Runtime.InteropServices;
using Comet.Launcher.Threads;

namespace Comet.Launcher.Managers
{
    internal static class AntiDebuggerManager
    {
        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool IsDebuggerPresent();

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool CheckRemoteDebuggerPresent(IntPtr handle, ref bool checkBool);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool CloseHandle(IntPtr handle);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern IntPtr GetModuleHandle(string lib);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern IntPtr GetProcAddress(IntPtr module, string function);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool WriteProcessMemory(IntPtr procHandle, IntPtr baseAddress, byte[] buffer, uint size,
                                                      int numOfBytes);

        [DllImport("ntdll.dll", SetLastError = true)]
        private static extern IntPtr NtSetInformationThread(IntPtr threadHandle, uint threadInfoClass,
                                                            IntPtr threadInfo, uint threadInfoLength);

        [DllImport("kernel32.dll")]
        private static extern IntPtr OpenThread(uint dwDesiredAccess, bool bInheritHandle, uint dwThreadId);

        public static bool CloseHandleAntiDebug()
        {
            try
            {
                CloseHandle((IntPtr) 0xD99121L);
                return false;
            }
            catch (Exception ex)
            {
                if (ex.Message == "External component has thrown an exception.")
                {
                    return true;
                }
            }

            return false;
        }

        public static bool RemoteDebuggerCheckAntiDebug()
        {
            var remoteDebugCheck = false;
            CheckRemoteDebuggerPresent(Process.GetCurrentProcess().Handle, ref remoteDebugCheck);
            if (remoteDebugCheck || Debugger.IsAttached || Debugger.IsLogging() || IsDebuggerPresent())
                return true;
            return false;
        }

        public static void OnTimer(AntiCheatThread.IllegalActionDelegate action)
        {
            if (RemoteDebuggerCheckAntiDebug() || CloseHandleAntiDebug())
            {
                action?.Invoke();
            }
        }

        public static void AntiDebuggerAttach()
        {
            IntPtr ntdllModule = GetModuleHandle("ntdll.dll");
            IntPtr dbgUiRemoteBreakingAddress = GetProcAddress(ntdllModule, "DbgUiRemoteBreakin");
            IntPtr dbgUiConnectToDbgAddress = GetProcAddress(ntdllModule, "DbgUiConnectToDbg");
            byte[] int3InvalidCode = {0xCC};
            WriteProcessMemory(Process.GetCurrentProcess().Handle, dbgUiRemoteBreakingAddress, int3InvalidCode, 6, 0);
            WriteProcessMemory(Process.GetCurrentProcess().Handle, dbgUiConnectToDbgAddress, int3InvalidCode, 6, 0);
        }

        public static void HideThreadsFromDebugger()
        {
            ProcessThreadCollection procThreads = Process.GetCurrentProcess().Threads;
            foreach (ProcessThread threadsInProc in procThreads)
            {
                IntPtr threadsHandle = OpenThread(0x0020, false, (uint) threadsInProc.Id);
                NtSetInformationThread(threadsHandle, 0x11, IntPtr.Zero, 0);
            }
        }
    }
}