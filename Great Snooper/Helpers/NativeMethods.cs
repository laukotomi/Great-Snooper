namespace GreatSnooper.Helpers
{
    using System;
    using System.Runtime.InteropServices;
    using System.Text;

    internal enum GetWindow_Cmd : uint
    {
        GW_HWNDFIRST = 0,
        GW_HWNDLAST = 1,
        GW_HWNDNEXT = 2,
        GW_HWNDPREV = 3,
        GW_OWNER = 4,
        GW_CHILD = 5,
        GW_ENABLEDPOPUP = 6
    }

    internal enum ShowWindowCommands : int
    {
        Hide = 0,
        Normal = 1,
        Minimized = 2,
        Maximized = 3,
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct FLASHWINFO
    {
        internal uint cbSize; // The size of the structure in bytes.
        internal IntPtr hwnd; // A Handle to the Window to be Flashed. The window can be either opened or minimized.
        internal uint dwFlags; // The Flash Status.
        internal uint uCount; // number of times to flash the window
        internal uint dwTimeout; // The rate at which the Window is to be flashed, in milliseconds. If Zero, the function uses the default cursor blink rate.
    }

    [Serializable]
    [StructLayout(LayoutKind.Sequential)]
    internal struct WINDOWPLACEMENT
    {
        internal int length;
        internal int flags;
        internal ShowWindowCommands showCmd;
        internal System.Drawing.Point ptMinPosition;
        internal System.Drawing.Point ptMaxPosition;
        internal System.Drawing.Rectangle rcNormalPosition;
    }

    internal static class NativeMethods
    {
        private delegate bool EnumWindowsProc(IntPtr hWnd, IntPtr lParam);

        internal static void DebugWindowTitles()
        {
            IntPtr found = IntPtr.Zero;

            EnumWindows(
                delegate(IntPtr wnd, IntPtr param)
                {
                    System.Diagnostics.Debug.WriteLine(GetWindowText(wnd));
                    return true;
                },
                IntPtr.Zero);
        }

        [DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
        internal static extern IntPtr FindWindow(string lpClassName, string lpWindowName);

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool FlashWindowEx(ref FLASHWINFO pwfi);

        [DllImport("user32.dll")]
        internal static extern IntPtr GetForegroundWindow();

        internal static WINDOWPLACEMENT GetPlacement(IntPtr hwnd)
        {
            WINDOWPLACEMENT placement = new WINDOWPLACEMENT();
            placement.length = Marshal.SizeOf(placement);
            GetWindowPlacement(hwnd, ref placement);
            return placement;
        }

        [DllImport("user32.dll", SetLastError = true)]
        internal static extern IntPtr GetWindow(IntPtr hWnd, GetWindow_Cmd uCmd);

        [DllImport("user32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool GetWindowPlacement(
            IntPtr hWnd, ref WINDOWPLACEMENT lpwndpl);

        internal static string GetWindowText(IntPtr hWnd)
        {
            int size = GetWindowTextLength(hWnd);
            if (size++ > 0)
            {
                var builder = new StringBuilder(size);
                GetWindowText(hWnd, builder, builder.Capacity);
                return builder.ToString();
            }

            return string.Empty;
        }

        [DllImport("user32.dll")]
        internal static extern int GetWindowThreadProcessId(IntPtr handle, out int processId);

        [return: MarshalAs(UnmanagedType.Bool)]
        [DllImport("user32.dll", SetLastError = true)]
        internal static extern bool IsWindowVisible(IntPtr hWnd);

        [DllImport("winmm.dll")]
        internal static extern int waveOutSetVolume(IntPtr hwo, uint dwVolume);

        [DllImport("user32.dll")]
        private static extern bool EnumWindows(EnumWindowsProc enumProc, IntPtr lParam);

        [DllImport("user32.dll", CharSet = CharSet.Unicode)]
        private static extern int GetWindowText(IntPtr hWnd, StringBuilder strText, int maxCount);

        [DllImport("user32.dll", CharSet = CharSet.Unicode)]
        private static extern int GetWindowTextLength(IntPtr hWnd);
    }
}