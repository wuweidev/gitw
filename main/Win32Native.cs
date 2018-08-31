using System;
using System.Runtime.InteropServices;

namespace gitw
{
    public static class Win32Native
    {
        public const int WM_CONTEXTMENU = 0x007B;

        public const int EM_SETCUEBANNER = 0x1501;
        public const int EM_GETCUEBANNER = 0x1502;

        public const int LVM_FIRST = 0x1000;
        public const int LVM_GETHEADER = (LVM_FIRST + 31);

        [DllImport("user32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        public static extern Int32 SendMessage(IntPtr hWnd, int msg, int wParam, [MarshalAs(UnmanagedType.LPWStr)] string lParam);

        [DllImport("user32.dll", EntryPoint = "SendMessage")]
        public static extern IntPtr SendMessage(IntPtr hwnd, int wMsg, long wParam, long lParam);

        [DllImport("user32.dll")]
        public static extern bool GetWindowRect(IntPtr hwnd, out RECT lpRect);

        [DllImport("uxtheme.dll", ExactSpelling = true, CharSet = CharSet.Unicode)]
        public static extern int SetWindowTheme(IntPtr hwnd, string pszSubAppName, string pszSubIdList);

        [Serializable, StructLayout(LayoutKind.Sequential)]
        public struct RECT
        {
            public int Left;
            public int Top;
            public int Right;
            public int Bottom;
        }
    }
}
