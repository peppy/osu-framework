// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using osu.Framework.Bindables;
using osu.Framework.Caching;
using osu.Framework.Configuration;
using osu.Framework.Input;
using osu.Framework.Platform.Sdl;
using osu.Framework.Platform.Windows.Native;
using osu.Framework.Threading;
using osuTK;
using osuTK.Input;
using SDL2;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Advanced;
using SixLabors.ImageSharp.PixelFormats;
using Image = SixLabors.ImageSharp.Image;

namespace osu.Framework.Platform
{
    /// <summary>
    /// Implementation of <see cref="Window"/> used for desktop platforms, using SDL2.
    /// </summary>
    public class DesktopWindow : Window
    {
        private const int default_icon_size = 256;

        private readonly BindableSize sizeFullscreen = new BindableSize();
        private readonly BindableSize sizeWindowed = new BindableSize();
        private readonly BindableDouble windowPositionX = new BindableDouble();
        private readonly BindableDouble windowPositionY = new BindableDouble();

        public readonly Bindable<ConfineMouseMode> ConfineMouseMode = new Bindable<ConfineMouseMode>();

        /// <summary>
        /// Gets or sets the window's position on the current screen given a relative value between 0 and 1.
        /// The position is calculated with respect to the window's size such that:
        ///   (0, 0) indicates that the window is aligned to the top left of the screen,
        ///   (1, 1) indicates that the window is aligned to the bottom right of the screen, and
        ///   (0.5, 0.5) indicates that the window is centred on the screen.
        /// </summary>
        protected Vector2 RelativePosition
        {
            get
            {
                var displayBounds = CurrentDisplay.Value.Bounds;
                var windowX = Position.Value.X - displayBounds.X;
                var windowY = Position.Value.Y - displayBounds.Y;
                var windowSize = sizeWindowed.Value;

                return new Vector2(
                    displayBounds.Width > windowSize.Width ? (float)windowX / (displayBounds.Width - windowSize.Width) : 0,
                    displayBounds.Height > windowSize.Height ? (float)windowY / (displayBounds.Height - windowSize.Height) : 0);
            }
            set
            {
                if (WindowMode.Value != Configuration.WindowMode.Windowed)
                    return;

                var displayBounds = CurrentDisplay.Value.Bounds;
                var windowSize = sizeWindowed.Value;
                var windowX = (int)Math.Round((displayBounds.Width - windowSize.Width) * value.X);
                var windowY = (int)Math.Round((displayBounds.Height - windowSize.Height) * value.Y);

                Position.Value = new Point(windowX + displayBounds.X, windowY + displayBounds.Y);
            }
        }

        protected override IGraphicsBackend CreateGraphicsBackend() => new Sdl2GraphicsBackend();

        public override void SetupWindow(FrameworkConfigManager config)
        {
            base.SetupWindow(config);

            CurrentDisplay.ValueChanged += evt =>
            {
                windowDisplayIndex.Value = (DisplayIndex)evt.NewValue.Index;
                windowPositionX.Value = 0.5;
                windowPositionY.Value = 0.5;
            };

            config.BindWith(FrameworkSetting.LastDisplayDevice, windowDisplayIndex);
            windowDisplayIndex.BindValueChanged(evt => CurrentDisplay.Value = Displays.ElementAtOrDefault((int)evt.NewValue) ?? PrimaryDisplay, true);

            sizeFullscreen.ValueChanged += evt =>
            {
                if (evt.NewValue.IsEmpty || CurrentDisplay.Value == null)
                    return;

                var mode = CurrentDisplay.Value.FindDisplayMode(evt.NewValue);
                if (mode.Size != System.Drawing.Size.Empty)
                    WindowBackend.CurrentDisplayMode = mode;
            };

            sizeWindowed.ValueChanged += evt =>
            {
                if (evt.NewValue.IsEmpty)
                    return;

                WindowBackend.Size = evt.NewValue;
                Size.Value = evt.NewValue;
            };

            config.BindWith(FrameworkSetting.SizeFullscreen, sizeFullscreen);
            config.BindWith(FrameworkSetting.WindowedSize, sizeWindowed);

            config.BindWith(FrameworkSetting.WindowedPositionX, windowPositionX);
            config.BindWith(FrameworkSetting.WindowedPositionY, windowPositionY);

            RelativePosition = new Vector2((float)windowPositionX.Value, (float)windowPositionY.Value);

            config.BindWith(FrameworkSetting.WindowMode, WindowMode);
            WindowMode.BindValueChanged(evt => UpdateWindowMode(evt.NewValue), true);

            config.BindWith(FrameworkSetting.ConfineMouseMode, ConfineMouseMode);
            ConfineMouseMode.BindValueChanged(confineMouseModeChanged, true);

            Resized += onResized;
            Moved += onMoved;
        }

        public override void CycleMode()
        {
            var currentValue = WindowMode.Value;

            do
            {
                switch (currentValue)
                {
                    case Configuration.WindowMode.Windowed:
                        currentValue = Configuration.WindowMode.Borderless;
                        break;

                    case Configuration.WindowMode.Borderless:
                        currentValue = Configuration.WindowMode.Fullscreen;
                        break;

                    case Configuration.WindowMode.Fullscreen:
                        currentValue = Configuration.WindowMode.Windowed;
                        break;
                }
            } while (!SupportedWindowModes.Contains(currentValue) && currentValue != WindowMode.Value);

            WindowMode.Value = currentValue;
        }

        protected override void UpdateWindowMode(WindowMode mode)
        {
            base.UpdateWindowMode(mode);

            ConfineMouseMode.TriggerChange();
        }

        public virtual void SetIconFromStream(Stream stream)
        {
            using (var ms = new MemoryStream())
            {
                stream.CopyTo(ms);
                ms.Position = 0;

                var imageInfo = Image.Identify(ms);

                if (imageInfo != null)
                    SetIconFromImage(Image.Load<Rgba32>(ms.GetBuffer()));
                else if (IconGroup.TryParse(ms.GetBuffer(), out var iconGroup))
                    SetIconFromGroup(iconGroup);
            }
        }

        internal virtual void SetIconFromImage(Image<Rgba32> iconImage) => WindowBackend.SetIcon(iconImage);

        internal virtual void SetIconFromGroup(IconGroup iconGroup)
        {
            // LoadRawIcon returns raw PNG data if available, which avoids any Windows-specific pinvokes
            var bytes = iconGroup.LoadRawIcon(default_icon_size, default_icon_size);
            if (bytes == null)
                return;

            SetIconFromImage(Image.Load<Rgba32>(bytes));
        }

        private void onResized()
        {
            if (WindowState.Value == Platform.WindowState.Normal)
            {
                sizeWindowed.Value = WindowBackend.Size;
                Size.Value = sizeWindowed.Value;
                updateWindowPositionConfig();
            }
        }

        private void onMoved(Point point)
        {
            if (WindowState.Value == Platform.WindowState.Normal)
                updateWindowPositionConfig();
        }

        private void updateWindowPositionConfig()
        {
            var relativePosition = RelativePosition;
            windowPositionX.Value = relativePosition.X;
            windowPositionY.Value = relativePosition.Y;
        }

        private void confineMouseModeChanged(ValueChangedEvent<ConfineMouseMode> args)
        {
            bool confine = false;

            switch (args.NewValue)
            {
                case Input.ConfineMouseMode.Fullscreen:
                    confine = WindowMode.Value != Configuration.WindowMode.Windowed;
                    break;

                case Input.ConfineMouseMode.Always:
                    confine = true;
                    break;
            }

            if (confine)
                CursorState.Value |= Platform.CursorState.Confined;
            else
                CursorState.Value &= ~Platform.CursorState.Confined;
        }

                private const int default_width = 1366;
        private const int default_height = 768;

        private readonly Scheduler commandScheduler = new Scheduler();
        private readonly Scheduler eventScheduler = new Scheduler();

        private bool mouseInWindow;
        private Point previousPolledPoint = Point.Empty;

        private readonly Dictionary<int, Sdl2ControllerBindings> controllers = new Dictionary<int, Sdl2ControllerBindings>();

        #region Internal Properties

        internal IntPtr SdlWindowHandle { get; private set; } = IntPtr.Zero;

        #endregion

        #region IWindowBackend.Properties

        public override bool Exists { get; protected set; }

        private string title = "";

        public override string Title
        {
            get => SdlWindowHandle == IntPtr.Zero ? title : SDL.SDL_GetWindowTitle(SdlWindowHandle);
            set
            {
                title = value;
                commandScheduler.Add(() => SDL.SDL_SetWindowTitle(SdlWindowHandle, $"{value} (SDL)"));
            }
        }

        private bool visible;

        public override bool Visible
        {
            get => SdlWindowHandle == IntPtr.Zero ? visible : windowFlags.HasFlag(SDL.SDL_WindowFlags.SDL_WINDOW_SHOWN);
            set
            {
                visible = value;
                commandScheduler.Add(() =>
                {
                    if (value)
                        SDL.SDL_ShowWindow(SdlWindowHandle);
                    else
                        SDL.SDL_HideWindow(SdlWindowHandle);
                });
            }
        }

        private Point position = Point.Empty;

        public override Point Position
        {
            get
            {
                if (SdlWindowHandle == IntPtr.Zero)
                    return position;

                SDL.SDL_GetWindowPosition(SdlWindowHandle, out var x, out var y);
                return new Point(x, y);
            }
            set
            {
                position = value;
                commandScheduler.Add(() => SDL.SDL_SetWindowPosition(SdlWindowHandle, value.X, value.Y));
            }
        }

        private Size size = new Size(default_width, default_height);

        public override Size Size
        {
            get
            {
                if (SdlWindowHandle == IntPtr.Zero)
                    return size;

                SDL.SDL_GetWindowSize(SdlWindowHandle, out var w, out var h);
                return new Size(w, h);
            }
            set
            {
                size = value;
                commandScheduler.Add(() => SDL.SDL_SetWindowSize(SdlWindowHandle, value.Width, value.Height));
            }
        }

        private readonly Cached<float> cachedScale = new Cached<float>();

        private float scale => validateScale();

        private float validateScale(bool force = false)
        {
            if (SdlWindowHandle == IntPtr.Zero)
                return 1f;

            if (!force && cachedScale.IsValid)
                return cachedScale.Value;

            var w = ClientSize.Width;
            float value = 1f;

            switch (windowFlags.ToWindowState())
            {
                case WindowState.Normal:
                    value = w / (float)Size.Width;
                    break;

                case WindowState.Fullscreen:
                    value = w / (float)windowDisplayMode.w;
                    break;

                case WindowState.FullscreenBorderless:
                    // SDL_GetDesktopDisplayMode gets the native display mode, and is used for *borderless* fullscreen
                    SDL.SDL_GetDesktopDisplayMode(windowDisplayIndex, out var mode);
                    value = w / (float)mode.w;
                    break;

                case WindowState.Maximised:
                case WindowState.Minimised:
                    return 1f;
            }

            cachedScale.Value = value;
            return value;
        }

        private bool cursorVisible = true;

        public override bool CursorVisible
        {
            get => SdlWindowHandle == IntPtr.Zero ? cursorVisible : SDL.SDL_ShowCursor(SDL.SDL_QUERY) == SDL.SDL_ENABLE;
            set
            {
                cursorVisible = value;
                commandScheduler.Add(() => SDL.SDL_ShowCursor(value ? SDL.SDL_ENABLE : SDL.SDL_DISABLE));
            }
        }

        private bool cursorConfined;

        public override bool CursorConfined
        {
            get => SdlWindowHandle == IntPtr.Zero ? cursorConfined : SDL.SDL_GetWindowGrab(SdlWindowHandle) == SDL.SDL_bool.SDL_TRUE;
            set
            {
                cursorConfined = value;
                commandScheduler.Add(() => SDL.SDL_SetWindowGrab(SdlWindowHandle, value ? SDL.SDL_bool.SDL_TRUE : SDL.SDL_bool.SDL_FALSE));
            }
        }

        private WindowState initialWindowState = WindowState.Normal;
        private WindowState lastWindowState;

        public override WindowState WindowState
        {
            get => SdlWindowHandle == IntPtr.Zero ? initialWindowState : windowFlags.ToWindowState();
            set
            {
                if (SdlWindowHandle == IntPtr.Zero)
                {
                    initialWindowState = value;
                    return;
                }

                commandScheduler.Add(() =>
                {
                    switch (value)
                    {
                        case WindowState.Normal:
                            SDL.SDL_SetWindowFullscreen(SdlWindowHandle, (uint)SDL.SDL_bool.SDL_FALSE);
                            break;

                        case WindowState.Fullscreen:
                            // set window display mode again, just in case if it changed from the last time we were fullscreen.
                            var fullscreenMode = closestDisplayMode(currentDisplayMode);
                            SDL.SDL_SetWindowDisplayMode(SdlWindowHandle, ref fullscreenMode);

                            SDL.SDL_SetWindowFullscreen(SdlWindowHandle, (uint)SDL.SDL_WindowFlags.SDL_WINDOW_FULLSCREEN);
                            break;

                        case WindowState.FullscreenBorderless:
                            SDL.SDL_SetWindowFullscreen(SdlWindowHandle, (uint)SDL.SDL_WindowFlags.SDL_WINDOW_FULLSCREEN_DESKTOP);
                            break;

                        case WindowState.Maximised:
                            SDL.SDL_MaximizeWindow(SdlWindowHandle);
                            break;

                        case WindowState.Minimised:
                            SDL.SDL_MinimizeWindow(SdlWindowHandle);
                            break;
                    }
                });
            }
        }

        public override Size ClientSize
        {
            get
            {
                if (SdlWindowHandle == IntPtr.Zero)
                    return Size.Empty;

                SDL.SDL_GL_GetDrawableSize(SdlWindowHandle, out var w, out var h);
                return new Size(w, h);
            }
        }

        public override IEnumerable<Display> Displays => Enumerable.Range(0, SDL.SDL_GetNumVideoDisplays()).Select(displayFromSDL);

        private Display currentDisplay;
        private int lastDisplayIndex = -1;

        public override Display CurrentDisplay
        {
            get => currentDisplay ??= Displays.ElementAtOrDefault(SdlWindowHandle == IntPtr.Zero ? 0 : windowDisplayIndex);
            set
            {
                if (value.Index == windowDisplayIndex)
                    return;

                int x = value.Bounds.Left + value.Bounds.Width / 2 - size.Width / 2;
                int y = value.Bounds.Top + value.Bounds.Height / 2 - size.Height / 2;

                WindowState = WindowState.Normal;
                Position = new Point(x, y);
            }
        }

        private DisplayMode currentDisplayMode;

        public override DisplayMode CurrentDisplayMode
        {
            get => SdlWindowHandle == IntPtr.Zero ? currentDisplayMode : displayModeFromSDL(windowDisplayMode, windowDisplayIndex, 0);
            set
            {
                currentDisplayMode = value;

                commandScheduler.Add(() =>
                {
                    var closest = closestDisplayMode(value);
                    var wasFullscreen = windowFlags.ToWindowState() == WindowState.Fullscreen;
                    if (wasFullscreen)
                        SDL.SDL_SetWindowFullscreen(SdlWindowHandle, (uint)SDL.SDL_bool.SDL_FALSE);

                    SDL.SDL_SetWindowDisplayMode(SdlWindowHandle, ref closest);

                    if (wasFullscreen)
                        SDL.SDL_SetWindowFullscreen(SdlWindowHandle, (uint)SDL.SDL_WindowFlags.SDL_WINDOW_FULLSCREEN);

                    cachedScale.Invalidate();
                });
            }
        }

        public override IntPtr WindowHandle
        {
            get
            {
                if (SdlWindowHandle == IntPtr.Zero)
                    return IntPtr.Zero;

                var wmInfo = windowWmInfo;

                // Window handle is selected per subsystem as defined at:
                // https://wiki.libsdl.org/SDL_SysWMinfo
                switch (wmInfo.subsystem)
                {
                    case SDL.SDL_SYSWM_TYPE.SDL_SYSWM_WINDOWS:
                        return wmInfo.info.win.window;

                    case SDL.SDL_SYSWM_TYPE.SDL_SYSWM_X11:
                        return wmInfo.info.x11.window;

                    case SDL.SDL_SYSWM_TYPE.SDL_SYSWM_DIRECTFB:
                        return wmInfo.info.dfb.window;

                    case SDL.SDL_SYSWM_TYPE.SDL_SYSWM_COCOA:
                        return wmInfo.info.cocoa.window;

                    case SDL.SDL_SYSWM_TYPE.SDL_SYSWM_UIKIT:
                        return wmInfo.info.uikit.window;

                    case SDL.SDL_SYSWM_TYPE.SDL_SYSWM_WAYLAND:
                        return wmInfo.info.wl.shell_surface;

                    case SDL.SDL_SYSWM_TYPE.SDL_SYSWM_ANDROID:
                        return wmInfo.info.android.window;

                    default:
                        return IntPtr.Zero;
                }
            }
        }

        #endregion

        #region Convenience Functions

        private SDL.SDL_SysWMinfo windowWmInfo
        {
            get
            {
                if (SdlWindowHandle == IntPtr.Zero)
                    return default;

                var wmInfo = new SDL.SDL_SysWMinfo();
                SDL.SDL_GetWindowWMInfo(SdlWindowHandle, ref wmInfo);
                return wmInfo;
            }
        }

        private int windowDisplayIndex => SdlWindowHandle == IntPtr.Zero ? 0 : SDL.SDL_GetWindowDisplayIndex(SdlWindowHandle);

        private Rectangle windowDisplayBounds
        {
            get
            {
                SDL.SDL_GetDisplayBounds(windowDisplayIndex, out var rect);
                return new Rectangle(rect.x, rect.y, rect.w, rect.h);
            }
        }

        private SDL.SDL_WindowFlags windowFlags => SdlWindowHandle == IntPtr.Zero ? 0 : (SDL.SDL_WindowFlags)SDL.SDL_GetWindowFlags(SdlWindowHandle);

        private SDL.SDL_DisplayMode windowDisplayMode
        {
            get
            {
                if (SdlWindowHandle == IntPtr.Zero)
                    return default;

                // SDL_GetWindowDisplayMode gets the resolution currently assigned to the window for *exclusive* fullscreen
                if (SDL.SDL_GetWindowDisplayMode(SdlWindowHandle, out var mode) >= 0)
                    return mode;

                // SDL_GetWindowDisplayMode can fail if the window was shown fullscreen on a different (especially larger) window before.
                // if that happens, fall back to closest mode for the current display.
                return closestDisplayMode(CurrentDisplayMode);
            }
        }

        private SDL.SDL_DisplayMode closestDisplayMode(DisplayMode mode)
        {
            var targetMode = new SDL.SDL_DisplayMode { w = mode.Size.Width, h = mode.Size.Height, refresh_rate = mode.RefreshRate };
            SDL.SDL_GetClosestDisplayMode(windowDisplayIndex, ref targetMode, out var closest);
            return closest;
        }

        private static Display displayFromSDL(int displayIndex)
        {
            var displayModes = Enumerable.Range(0, SDL.SDL_GetNumDisplayModes(displayIndex))
                                         .Select(modeIndex =>
                                         {
                                             SDL.SDL_GetDisplayMode(displayIndex, modeIndex, out var mode);
                                             return displayModeFromSDL(mode, displayIndex, modeIndex);
                                         })
                                         .ToArray();

            SDL.SDL_GetDisplayBounds(displayIndex, out var rect);
            return new Display(displayIndex, SDL.SDL_GetDisplayName(displayIndex), new Rectangle(rect.x, rect.y, rect.w, rect.h), displayModes);
        }

        private static DisplayMode displayModeFromSDL(SDL.SDL_DisplayMode mode, int displayIndex, int modeIndex)
        {
            SDL.SDL_PixelFormatEnumToMasks(mode.format, out var bpp, out _, out _, out _, out _);
            return new DisplayMode(SDL.SDL_GetPixelFormatName(mode.format), new Size(mode.w, mode.h), bpp, mode.refresh_rate, modeIndex, displayIndex);
        }

        private void enqueueJoystickAxisInput(JoystickAxisSource axisSource, short axisValue)
        {
            // SDL reports axis values in the range short.MinValue to short.MaxValue, so we scale and clamp it to the range of -1f to 1f
            var clamped = Math.Clamp((float)axisValue / short.MaxValue, -1f, 1f);
            eventScheduler.Add(() => OnJoystickAxisChanged(new JoystickAxis(axisSource, clamped)));
        }

        private void enqueueJoystickButtonInput(JoystickButton button, bool isPressed)
        {
            if (isPressed)
                eventScheduler.Add(() => OnJoystickButtonDown(button));
            else
                eventScheduler.Add(() => OnJoystickButtonUp(button));
        }

        #endregion

        public Sdl2WindowBackend()
        {
            SDL.SDL_Init(SDL.SDL_INIT_VIDEO | SDL.SDL_INIT_GAMECONTROLLER);
        }

        #region IWindowBackend.Methods

        public override void Create()
        {
            SDL.SDL_WindowFlags flags = SDL.SDL_WindowFlags.SDL_WINDOW_OPENGL |
                                        SDL.SDL_WindowFlags.SDL_WINDOW_RESIZABLE |
                                        SDL.SDL_WindowFlags.SDL_WINDOW_ALLOW_HIGHDPI |
                                        SDL.SDL_WindowFlags.SDL_WINDOW_HIDDEN | // shown after first swap to avoid white flash on startup (windows)
                                        WindowState.ToFlags();

            SDL.SDL_SetHint(SDL.SDL_HINT_WINDOWS_NO_CLOSE_ON_ALT_F4, "1");

            SdlWindowHandle = SDL.SDL_CreateWindow($"{title} (SDL)", Position.X, Position.Y, Size.Width, Size.Height, flags);

            cachedScale.Invalidate();
            Exists = true;
        }

        public override void Run()
        {
            while (Exists)
            {
                commandScheduler.Update();

                if (!Exists)
                    break;

                processEvents();

                if (!mouseInWindow)
                    pollMouse();

                eventScheduler.Update();

                OnUpdate();
            }

            OnClosed();

            if (SdlWindowHandle != IntPtr.Zero)
                SDL.SDL_DestroyWindow(SdlWindowHandle);

            SDL.SDL_Quit();
        }

        public override void Close() => commandScheduler.Add(() => Exists = false);

        public override void RequestClose() => ScheduleEvent(OnCloseRequested);

        public override unsafe void SetIcon(Image<Rgba32> image)
        {
            var data = image.GetPixelSpan().ToArray();
            var imageSize = image.Size();

            commandScheduler.Add(() =>
            {
                IntPtr surface;
                fixed (Rgba32* ptr = data)
                    surface = SDL.SDL_CreateRGBSurfaceFrom(new IntPtr(ptr), imageSize.Width, imageSize.Height, 32, imageSize.Width * 4, 0xff, 0xff00, 0xff0000, 0xff000000);

                SDL.SDL_SetWindowIcon(SdlWindowHandle, surface);
                SDL.SDL_FreeSurface(surface);
            });
        }

        private void pollMouse()
        {
            SDL.SDL_GetGlobalMouseState(out var x, out var y);
            if (previousPolledPoint.X == x && previousPolledPoint.Y == y)
                return;

            previousPolledPoint = new Point(x, y);

            var pos = windowFlags.ToWindowState() == WindowState.Normal ? Position : windowDisplayBounds.Location;
            var rx = x - pos.X;
            var ry = y - pos.Y;

            ScheduleEvent(() => OnMouseMove(new Vector2(rx * scale, ry * scale)));
        }

        #endregion

        #region SDL Event Handling

        /// <summary>
        /// Adds an <see cref="Action"/> to the <see cref="Scheduler"/> expected to handle event callbacks.
        /// </summary>
        /// <param name="action">The <see cref="Action"/> to execute.</param>
        protected void ScheduleEvent(Action action) => eventScheduler.Add(action);

        private void processEvents()
        {
            while (SDL.SDL_PollEvent(out var evt) > 0)
            {
                switch (evt.type)
                {
                    case SDL.SDL_EventType.SDL_QUIT:
                    case SDL.SDL_EventType.SDL_APP_TERMINATING:
                        handleQuitEvent(evt.quit);
                        break;

                    case SDL.SDL_EventType.SDL_WINDOWEVENT:
                        handleWindowEvent(evt.window);
                        break;

                    case SDL.SDL_EventType.SDL_KEYDOWN:
                    case SDL.SDL_EventType.SDL_KEYUP:
                        handleKeyboardEvent(evt.key);
                        break;

                    case SDL.SDL_EventType.SDL_TEXTEDITING:
                        handleTextEditingEvent(evt.edit);
                        break;

                    case SDL.SDL_EventType.SDL_TEXTINPUT:
                        handleTextInputEvent(evt.text);
                        break;

                    case SDL.SDL_EventType.SDL_MOUSEMOTION:
                        handleMouseMotionEvent(evt.motion);
                        break;

                    case SDL.SDL_EventType.SDL_MOUSEBUTTONDOWN:
                    case SDL.SDL_EventType.SDL_MOUSEBUTTONUP:
                        handleMouseButtonEvent(evt.button);
                        break;

                    case SDL.SDL_EventType.SDL_MOUSEWHEEL:
                        handleMouseWheelEvent(evt.wheel);
                        break;

                    case SDL.SDL_EventType.SDL_JOYAXISMOTION:
                        handleJoyAxisEvent(evt.jaxis);
                        break;

                    case SDL.SDL_EventType.SDL_JOYBALLMOTION:
                        handleJoyBallEvent(evt.jball);
                        break;

                    case SDL.SDL_EventType.SDL_JOYHATMOTION:
                        handleJoyHatEvent(evt.jhat);
                        break;

                    case SDL.SDL_EventType.SDL_JOYBUTTONDOWN:
                    case SDL.SDL_EventType.SDL_JOYBUTTONUP:
                        handleJoyButtonEvent(evt.jbutton);
                        break;

                    case SDL.SDL_EventType.SDL_JOYDEVICEADDED:
                    case SDL.SDL_EventType.SDL_JOYDEVICEREMOVED:
                        handleJoyDeviceEvent(evt.jdevice);
                        break;

                    case SDL.SDL_EventType.SDL_CONTROLLERAXISMOTION:
                        handleControllerAxisEvent(evt.caxis);
                        break;

                    case SDL.SDL_EventType.SDL_CONTROLLERBUTTONDOWN:
                    case SDL.SDL_EventType.SDL_CONTROLLERBUTTONUP:
                        handleControllerButtonEvent(evt.cbutton);
                        break;

                    case SDL.SDL_EventType.SDL_CONTROLLERDEVICEADDED:
                    case SDL.SDL_EventType.SDL_CONTROLLERDEVICEREMOVED:
                    case SDL.SDL_EventType.SDL_CONTROLLERDEVICEREMAPPED:
                        handleControllerDeviceEvent(evt.cdevice);
                        break;

                    case SDL.SDL_EventType.SDL_FINGERDOWN:
                    case SDL.SDL_EventType.SDL_FINGERUP:
                    case SDL.SDL_EventType.SDL_FINGERMOTION:
                        handleTouchFingerEvent(evt.tfinger);
                        break;

                    case SDL.SDL_EventType.SDL_DROPFILE:
                    case SDL.SDL_EventType.SDL_DROPTEXT:
                    case SDL.SDL_EventType.SDL_DROPBEGIN:
                    case SDL.SDL_EventType.SDL_DROPCOMPLETE:
                        handleDropEvent(evt.drop);
                        break;
                }
            }
        }

        private void handleQuitEvent(SDL.SDL_QuitEvent evtQuit) => RequestClose();

        private void handleDropEvent(SDL.SDL_DropEvent evtDrop)
        {
            switch (evtDrop.type)
            {
                case SDL.SDL_EventType.SDL_DROPFILE:
                    var str = SDL.UTF8_ToManaged(evtDrop.file, true);
                    if (str != null)
                        ScheduleEvent(() => OnDragDrop(str));

                    break;
            }
        }

        private void handleTouchFingerEvent(SDL.SDL_TouchFingerEvent evtTfinger)
        {
        }

        private void handleControllerDeviceEvent(SDL.SDL_ControllerDeviceEvent evtCdevice)
        {
            switch (evtCdevice.type)
            {
                case SDL.SDL_EventType.SDL_CONTROLLERDEVICEADDED:
                    var controller = SDL.SDL_GameControllerOpen(evtCdevice.which);
                    var joystick = SDL.SDL_GameControllerGetJoystick(controller);
                    var instanceID = SDL.SDL_JoystickGetDeviceInstanceID(evtCdevice.which);
                    controllers[instanceID] = new Sdl2ControllerBindings(joystick, controller);
                    break;

                case SDL.SDL_EventType.SDL_CONTROLLERDEVICEREMOVED:
                    SDL.SDL_GameControllerClose(controllers[evtCdevice.which].ControllerHandle);
                    controllers.Remove(evtCdevice.which);
                    break;

                case SDL.SDL_EventType.SDL_CONTROLLERDEVICEREMAPPED:
                    if (controllers.TryGetValue(evtCdevice.which, out var state))
                        state.PopulateBindings();

                    break;
            }
        }

        private void handleControllerButtonEvent(SDL.SDL_ControllerButtonEvent evtCbutton)
        {
            var button = ((SDL.SDL_GameControllerButton)evtCbutton.button).ToJoystickButton();

            switch (evtCbutton.type)
            {
                case SDL.SDL_EventType.SDL_CONTROLLERBUTTONDOWN:
                    enqueueJoystickButtonInput(button, true);
                    break;

                case SDL.SDL_EventType.SDL_CONTROLLERBUTTONUP:
                    enqueueJoystickButtonInput(button, false);
                    break;
            }
        }

        private void handleControllerAxisEvent(SDL.SDL_ControllerAxisEvent evtCaxis) =>
            enqueueJoystickAxisInput(((SDL.SDL_GameControllerAxis)evtCaxis.axis).ToJoystickAxisSource(), evtCaxis.axisValue);

        private void handleJoyDeviceEvent(SDL.SDL_JoyDeviceEvent evtJdevice)
        {
            switch (evtJdevice.type)
            {
                case SDL.SDL_EventType.SDL_JOYDEVICEADDED:
                    var instanceID = SDL.SDL_JoystickGetDeviceInstanceID(evtJdevice.which);

                    // if the joystick is already opened, ignore it
                    if (controllers.ContainsKey(instanceID))
                        break;

                    var joystick = SDL.SDL_JoystickOpen(evtJdevice.which);
                    controllers[instanceID] = new Sdl2ControllerBindings(joystick, IntPtr.Zero);
                    break;

                case SDL.SDL_EventType.SDL_JOYDEVICEREMOVED:
                    // if the joystick is already closed, ignore it
                    if (!controllers.ContainsKey(evtJdevice.which))
                        break;

                    SDL.SDL_JoystickClose(controllers[evtJdevice.which].JoystickHandle);
                    controllers.Remove(evtJdevice.which);
                    break;
            }
        }

        private void handleJoyButtonEvent(SDL.SDL_JoyButtonEvent evtJbutton)
        {
            // if this button exists in the controller bindings, skip it
            if (controllers.TryGetValue(evtJbutton.which, out var state) && state.GetButtonForIndex(evtJbutton.button) != SDL.SDL_GameControllerButton.SDL_CONTROLLER_BUTTON_INVALID)
                return;

            var button = JoystickButton.FirstButton + evtJbutton.button;

            switch (evtJbutton.type)
            {
                case SDL.SDL_EventType.SDL_JOYBUTTONDOWN:
                    enqueueJoystickButtonInput(button, true);
                    break;

                case SDL.SDL_EventType.SDL_JOYBUTTONUP:
                    enqueueJoystickButtonInput(button, false);
                    break;
            }
        }

        private void handleJoyHatEvent(SDL.SDL_JoyHatEvent evtJhat)
        {
        }

        private void handleJoyBallEvent(SDL.SDL_JoyBallEvent evtJball)
        {
        }

        private void handleJoyAxisEvent(SDL.SDL_JoyAxisEvent evtJaxis)
        {
            // if this axis exists in the controller bindings, skip it
            if (controllers.TryGetValue(evtJaxis.which, out var state) && state.GetAxisForIndex(evtJaxis.axis) != SDL.SDL_GameControllerAxis.SDL_CONTROLLER_AXIS_INVALID)
                return;

            enqueueJoystickAxisInput(JoystickAxisSource.Axis1 + evtJaxis.axis, evtJaxis.axisValue);
        }

        private void handleMouseWheelEvent(SDL.SDL_MouseWheelEvent evtWheel) =>
            ScheduleEvent(() => OnMouseWheel(new Vector2(evtWheel.x, evtWheel.y), false));

        private void handleMouseButtonEvent(SDL.SDL_MouseButtonEvent evtButton)
        {
            MouseButton button = mouseButtonFromEvent(evtButton.button);

            switch (evtButton.type)
            {
                case SDL.SDL_EventType.SDL_MOUSEBUTTONDOWN:
                    ScheduleEvent(() => OnMouseDown(button));
                    break;

                case SDL.SDL_EventType.SDL_MOUSEBUTTONUP:
                    ScheduleEvent(() => OnMouseUp(button));
                    break;
            }
        }

        private void handleMouseMotionEvent(SDL.SDL_MouseMotionEvent evtMotion) =>
            ScheduleEvent(() => OnMouseMove(new Vector2(evtMotion.x * scale, evtMotion.y * scale)));

        private unsafe void handleTextInputEvent(SDL.SDL_TextInputEvent evtText)
        {
            var ptr = new IntPtr(evtText.text);
            if (ptr == IntPtr.Zero)
                return;

            string text = Marshal.PtrToStringUTF8(ptr) ?? "";

            foreach (char c in text)
                ScheduleEvent(() => OnKeyTyped(c));
        }

        private void handleTextEditingEvent(SDL.SDL_TextEditingEvent evtEdit)
        {
        }

        private void handleKeyboardEvent(SDL.SDL_KeyboardEvent evtKey)
        {
            Key key = evtKey.keysym.ToKey();

            if (key == Key.Unknown || key == Key.CapsLock)
                return;

            switch (evtKey.type)
            {
                case SDL.SDL_EventType.SDL_KEYDOWN:
                    ScheduleEvent(() => OnKeyDown(key));
                    break;

                case SDL.SDL_EventType.SDL_KEYUP:
                    ScheduleEvent(() => OnKeyUp(key));
                    break;
            }
        }

        private void handleWindowEvent(SDL.SDL_WindowEvent evtWindow)
        {
            var currentState = windowFlags.ToWindowState();
            var displayIndex = windowDisplayIndex;

            if (lastWindowState != currentState)
            {
                lastWindowState = currentState;
                cachedScale.Invalidate();
                ScheduleEvent(() => OnWindowStateChanged(currentState));
            }

            if (lastDisplayIndex != displayIndex)
            {
                lastDisplayIndex = displayIndex;
                currentDisplay = null;
                cachedScale.Invalidate();
                ScheduleEvent(() => OnDisplayChanged(Displays.ElementAtOrDefault(displayIndex) ?? PrimaryDisplay));
            }

            switch (evtWindow.windowEvent)
            {
                case SDL.SDL_WindowEventID.SDL_WINDOWEVENT_SHOWN:
                    ScheduleEvent(OnShown);
                    break;

                case SDL.SDL_WindowEventID.SDL_WINDOWEVENT_HIDDEN:
                    ScheduleEvent(OnHidden);
                    break;

                case SDL.SDL_WindowEventID.SDL_WINDOWEVENT_MOVED:
                    var eventPos = new Point(evtWindow.data1, evtWindow.data2);

                    if (currentState == WindowState.Normal && !eventPos.Equals(position))
                    {
                        position = eventPos;
                        cachedScale.Invalidate();
                        ScheduleEvent(() => OnMoved(eventPos));
                    }

                    break;

                case SDL.SDL_WindowEventID.SDL_WINDOWEVENT_SIZE_CHANGED:
                    var eventSize = new Size(evtWindow.data1, evtWindow.data2);

                    if (currentState == WindowState.Normal && !eventSize.Equals(size))
                    {
                        size = eventSize;
                        cachedScale.Invalidate();
                        ScheduleEvent(() => OnResized(eventSize));
                    }

                    break;

                case SDL.SDL_WindowEventID.SDL_WINDOWEVENT_MINIMIZED:
                case SDL.SDL_WindowEventID.SDL_WINDOWEVENT_MAXIMIZED:
                case SDL.SDL_WindowEventID.SDL_WINDOWEVENT_RESTORED:
                    break;

                case SDL.SDL_WindowEventID.SDL_WINDOWEVENT_ENTER:
                    mouseInWindow = true;
                    ScheduleEvent(OnMouseEntered);
                    break;

                case SDL.SDL_WindowEventID.SDL_WINDOWEVENT_LEAVE:
                    mouseInWindow = false;
                    ScheduleEvent(OnMouseLeft);
                    break;

                case SDL.SDL_WindowEventID.SDL_WINDOWEVENT_FOCUS_GAINED:
                    ScheduleEvent(OnFocusGained);
                    break;

                case SDL.SDL_WindowEventID.SDL_WINDOWEVENT_FOCUS_LOST:
                    ScheduleEvent(OnFocusLost);
                    break;

                case SDL.SDL_WindowEventID.SDL_WINDOWEVENT_CLOSE:
                    break;
            }
        }

        private MouseButton mouseButtonFromEvent(byte button)
        {
            switch ((uint)button)
            {
                default:
                case SDL.SDL_BUTTON_LEFT:
                    return MouseButton.Left;

                case SDL.SDL_BUTTON_RIGHT:
                    return MouseButton.Right;

                case SDL.SDL_BUTTON_MIDDLE:
                    return MouseButton.Middle;

                case SDL.SDL_BUTTON_X1:
                    return MouseButton.Button1;

                case SDL.SDL_BUTTON_X2:
                    return MouseButton.Button2;
            }
        }

        #endregion
    }
}
