using osu.Framework.Platform.Windows.Native;

namespace osu.Framework.Platform.Windows
{
    /// <summary>
    /// A windows specific mouse input handler which overrides the SDL2 implementation of raw input.
    /// This is done to better handle quirks of some devices.
    /// </summary>
    internal unsafe class WindowsRawInputMouseHandler : WndProcInputHandler
    {
        public override bool Initialize(GameHost host)
        {
            if (!base.Initialize(host))
                return false;

            SDL2DesktopWindow desktopWindow = (SDL2DesktopWindow)host.Window;

            try
            {
                RawInputDevice r = new RawInputDevice
                {
                    UsagePage = HIDUsagePage.Generic,
                    Usage = HIDUsage.Mouse,
                    Flags = RawInputDeviceFlags.None,
                    WindowHandle = desktopWindow.WindowHandle
                };

                if (!WindowsRawInput.RegisterRawInputDevices(new[] { r }, 1, sizeof(RawInputDevice)))
                {
                    return false;
                }
            }
            catch { }

            return true;
        }

        public override bool IsActive => Enabled.Value;
        public override int Priority => 0;
    }
}
