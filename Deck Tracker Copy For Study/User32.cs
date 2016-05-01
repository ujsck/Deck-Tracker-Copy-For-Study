﻿using System;
using System.Runtime.InteropServices;

namespace Deck_Tracker_Copy_For_Study
{
    internal class User32
    {
        private const int WsExTransparent = 0x00000020;
        private const int GwlExstyle = (-20);

        [DllImport("user32.dll")]
        public static extern IntPtr GetWindowRect(IntPtr hWnd, ref Rect rect);

        //https://msdn.microsoft.com/zh-cn/library/7b93s42f(v=vs.110).aspx
        [DllImport("user32.dll", CharSet = CharSet.Unicode)]
        public static extern IntPtr FindWindow(String lpClassName, String lpWindowName);

        [DllImport("user32.dll")]
        public static extern IntPtr GetForegroundWindow();

        [DllImport("user32.dll")]
        private static extern int GetWindowLong(IntPtr hwnd, int index);

        [DllImport("user32.dll")]
        private static extern int SetWindowLong(IntPtr hwnd, int index, int newStyle);

        // ?
        public static void SetWindowExTransparent(IntPtr hwnd)
        {
            int extendedStyle = GetWindowLong(hwnd, GwlExstyle);
            SetWindowLong(hwnd, GwlExstyle, extendedStyle | WsExTransparent);
        }

        public static bool IsForegroudWindow(String lpWindowName)
        {
            return GetForegroundWindow() == FindWindow(null, lpWindowName);
        }

        //https://msdn.microsoft.com/zh-cn/library/system.runtime.interopservices.layoutkind(v=vs.110).aspx
        [StructLayout(LayoutKind.Sequential)]
        public struct Rect
        {
            public int left;
            public int top;
            public int right;
            public int bottom;
        }
    }
}
