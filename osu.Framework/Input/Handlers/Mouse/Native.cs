using System;
using System.Runtime.InteropServices;

namespace osu.Framework.Input.Handlers.Mouse
{
    internal static class Native
    {
        [DllImport(@"kernel32.dll", CharSet = CharSet.Ansi, ExactSpelling = true, SetLastError = true)]
        internal static extern IntPtr GetProcAddress(IntPtr hModule, string procName);

        [DllImport(@"kernel32.dll", CharSet = CharSet.Auto)]
        public static extern IntPtr GetModuleHandle(string lpModuleName);

        [DllImport(@"kernel32.dll", SetLastError = true)]
        public static extern bool AllocConsole();

        [DllImport("user32.dll")]
        public static extern bool RegisterTouchWindow(IntPtr hWnd, int flags);

        [DllImport(@"Kernel32.dll")]
        internal static extern ushort GlobalAddAtom(string lpString);

        [DllImport(@"Kernel32.dll")]
        internal static extern ushort GlobalDeleteAtom(ushort nAtom);

        [DllImport(@"user32.dll")]
        internal static extern int SetProp(IntPtr hWnd, string lpString, int hData);

        [DllImport(@"user32.dll")]
        internal static extern int RemoveProp(IntPtr hWnd, string lpString);

        [DllImport("user32.dll")]
        public static extern int GetSystemMetrics(int nIndex);

        [DllImport("user32.dll")]
        public static extern bool RegisterRawInputDevices(
            [MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 1)]
            RawInputDevice[] pRawInputDevices,
            int uiNumDevices,
            int cbSize);

        [DllImport("user32.dll")]
        public static extern bool GetTouchInputInfo(
            IntPtr hTouchInput,
            int uiNumDevices,
            [MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 1), Out]
            RawTouchInput[] pRawTouchInputs,
            int cbSize);

        [DllImport("user32.dll")]
        public static extern bool CloseTouchInputHandle(IntPtr hTouchInput);

        [DllImport("user32.dll")]
        public static extern int GetRawInputData(IntPtr hRawInput, RawInputCommand uiCommand, out RawInput pData, ref int pcbSize, int cbSizeHeader);

        [DllImport("user32.dll")]
        public static extern bool GetPointerInfo(int pointerID, out RawPointerInput type);

        // SetWindowLongPtr does not exist on x86 platforms (it's a macro that resolves to SetWindowLong).
        // We need to detect if we are on x86 or x64 at runtime and call the correct function
        // (SetWindowLongPtr on x64 or SetWindowLong on x86). Fun!
        internal static IntPtr SetWindowLong(IntPtr handle, GetWindowLongOffsets item, IntPtr newValue)
        {
            // SetWindowPos defines its error condition as an IntPtr.Zero retval and a non-0 GetLastError.
            // We need to SetLastError(0) to ensure we are not detecting on older error condition (from another function).

            IntPtr retval = IntPtr.Zero;

            if (IntPtr.Size == 4)
                retval = new IntPtr(SetWindowLongInternal(handle, item, newValue.ToInt32()));
            else
                retval = SetWindowLongPtrInternal(handle, item, newValue);

            return retval;
        }

        [DllImport("user32.dll", SetLastError = true, EntryPoint = "SetWindowLong")]
        static extern int SetWindowLongInternal(IntPtr hWnd, GetWindowLongOffsets nIndex, int dwNewLong);

        [DllImport("user32.dll", SetLastError = true, EntryPoint = "SetWindowLongPtr")]
        static extern IntPtr SetWindowLongPtrInternal(IntPtr hWnd, GetWindowLongOffsets nIndex, IntPtr dwNewLong);

        [DllImport("user32.dll")]
        public static extern long GetWindowLong(IntPtr hWnd, GetWindowLongOffsets nIndex);

        [DllImport("user32.dll")]
        public static extern int SetWindowPos(IntPtr hWnd, IntPtr hWndPos, int X, int Y, int cx, int cy, int flags);

        [DllImport("kernel32.dll")]
        public static extern uint SetThreadExecutionState(ExecutionState state);

        [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Auto)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool GetDiskFreeSpaceEx(string lpDirectoryName, out ulong lpFreeBytesAvailable, out ulong lpTotalNumberOfBytes, out ulong lpTotalNumberOfFreeBytes);

        [Flags]
        internal enum ExecutionState : uint
        {
            AwaymodeRequired = 0x00000040,
            Continuous = 0x80000000,
            DisplayRequired = 0x00000002,
            SystemRequired = 0x00000001,
            UserPresent = 0x00000004,
        }

        [Flags]
        internal enum WindowStyle : long
        {
            Overlapped = 0x00000000,
            Popup = 0x80000000,
            Child = 0x40000000,
            Minimize = 0x20000000,
            Visible = 0x10000000,
            Disabled = 0x08000000,
            ClipSiblings = 0x04000000,
            ClipChildren = 0x02000000,
            Maximize = 0x01000000,
            Caption = 0x00C00000, // Border | DialogFrame
            Border = 0x00800000,
            DialogFrame = 0x00400000,
            VScroll = 0x00200000,
            HScreen = 0x00100000,
            SystemMenu = 0x00080000,
            ThickFrame = 0x00040000,
            Group = 0x00020000,
            TabStop = 0x00010000,

            MinimizeBox = 0x00020000,
            MaximizeBox = 0x00010000,

            Tiled = Overlapped,
            Iconic = Minimize,
            SizeBox = ThickFrame,
            TiledWindow = OverlappedWindow,

            // Common window styles:
            OverlappedWindow = Overlapped | Caption | SystemMenu | ThickFrame | MinimizeBox | MaximizeBox,
            PopupWindow = Popup | Border | SystemMenu,
            ChildWindow = Child
        }

        [Flags]
        internal enum ExtendedWindowStyle : long
        {
            DialogModalFrame = 0x00000001,
            NoParentNotify = 0x00000004,
            Topmost = 0x00000008,
            AcceptFiles = 0x00000010,
            Transparent = 0x00000020,

            // #if(WINVER >= 0x0400)
            MdiChild = 0x00000040,
            ToolWindow = 0x00000080,
            WindowEdge = 0x00000100,
            ClientEdge = 0x00000200,
            ContextHelp = 0x00000400,
            // #endif

            // #if(WINVER >= 0x0400)
            Right = 0x00001000,
            Left = 0x00000000,
            RightToLeftReading = 0x00002000,
            LeftToRightReading = 0x00000000,
            LeftScrollbar = 0x00004000,
            RightScrollbar = 0x00000000,

            ControlParent = 0x00010000,
            StaticEdge = 0x00020000,
            ApplicationWindow = 0x00040000,

            OverlappedWindow = WindowEdge | ClientEdge,
            PaletteWindow = WindowEdge | ToolWindow | Topmost,
            // #endif

            // #if(_WIN32_WINNT >= 0x0500)
            Layered = 0x00080000,
            // #endif

            // #if(WINVER >= 0x0500)
            NoInheritLayout = 0x00100000, // Disable inheritence of mirroring by children
            RightToLeftLayout = 0x00400000, // Right to left mirroring
            // #endif /* WINVER >= 0x0500 */

            // #if(_WIN32_WINNT >= 0x0501)
            Composited = 0x02000000,
            // #endif /* _WIN32_WINNT >= 0x0501 */

            // #if(_WIN32_WINNT >= 0x0500)
            NoActivate = 0x08000000
            // #endif /* _WIN32_WINNT >= 0x0500 */
        }

        internal enum GetWindowLongOffsets : int
        {
            WNDPROC = (-4),
            HINSTANCE = (-6),
            HWNDPARENT = (-8),
            STYLE = (-16),
            EXSTYLE = (-20),
            USERDATA = (-21),
            ID = (-12),
        }

        internal const int SM_XVIRTUALSCREEN = 76;
        internal const int SM_YVIRTUALSCREEN = 77;

        internal const int SM_CXVIRTUALSCREEN = 78;
        internal const int SM_CYVIRTUALSCREEN = 79;

        internal const int WM_NCPOINTERUPDATE = 0x0241;
        internal const int WM_NCPOINTERDOWN = 0x0242;
        internal const int WM_NCPOINTERUP = 0x0243;
        internal const int WM_POINTERUPDATE = 0x0245;
        internal const int WM_POINTERDOWN = 0x0246;
        internal const int WM_POINTERUP = 0x0247;
        internal const int WM_POINTERENTER = 0x0249;
        internal const int WM_POINTERLEAVE = 0x024A;
        internal const int WM_POINTERACTIVATE = 0x024B;
        internal const int WM_POINTERCAPTURECHANGED = 0x024C;
        internal const int WM_POINTERWHEEL = 0x024E;
        internal const int WM_POINTERHWHEEL = 0x024F;

        internal const int WM_INPUT = 0x00FF;
        internal const int WM_TOUCH = 0x0240;

        internal const int TWF_FINETOUCH = 0x00000001;
        internal const int TWF_WANTPALM = 0x00000002;

        internal const int TABLET_DISABLE_PRESSANDHOLD = 0x00000001;
        internal const int TABLET_DISABLE_PENTAPFEEDBACK = 0x00000008;
        internal const int TABLET_DISABLE_PENBARRELFEEDBACK = 0x00000010;
        internal const int TABLET_DISABLE_TOUCHUIFORCEON = 0x00000100;
        internal const int TABLET_DISABLE_TOUCHUIFORCEOFF = 0x00000200;
        internal const int TABLET_DISABLE_TOUCHSWITCH = 0x00008000;
        internal const int TABLET_DISABLE_FLICKS = 0x00010000;
        internal const int TABLET_ENABLE_FLICKSONCONTEXT = 0x00020000;
        internal const int TABLET_ENABLE_FLICKLEARNINGMODE = 0x00040000;
        internal const int TABLET_DISABLE_SMOOTHSCROLLING = 0x00080000;
        internal const int TABLET_DISABLE_FLICKFALLBACKKEYS = 0x00100000;
        internal const int TABLET_ENABLE_MULTITOUCHDATA = 0x01000000;
    }
}
