using System;
using System.Runtime.InteropServices;

namespace WakeUp
{
    public class W32
    {
        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        public static extern IntPtr GetForegroundWindow();

        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        private static extern Int32 GetWindowThreadProcessId(IntPtr hWnd, out Int32 lpdwProcessId);

        public static Int32 GetWindowProcessID(IntPtr hWnd)
        {
            Int32 pid;
            GetWindowThreadProcessId(hWnd, out pid);

            return pid;
        }
    }
}
