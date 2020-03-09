﻿// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Threading;
using osu.Framework.Statistics;
using osu.Framework.Timing;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using osu.Framework.Bindables;
using osu.Framework.Development;

namespace osu.Framework.Threading
{
    public class GameThread
    {
        internal const double DEFAULT_ACTIVE_HZ = 1000;
        internal const double DEFAULT_INACTIVE_HZ = 60;

        internal PerformanceMonitor Monitor { get; }
        public ThrottledFrameClock Clock { get; }
        public Thread Thread { get; private set; }
        public Scheduler Scheduler { get; }

        /// <summary>
        /// Attach a handler to delegate responsibility for per-frame exceptions.
        /// While attached, all exceptions will be caught and forwarded. Thread execution will continue indefinitely.
        /// </summary>
        public EventHandler<UnhandledExceptionEventArgs> UnhandledException;

        internal Action<Thread> ThreadChanged;

        protected Action OnNewFrame;

        /// <summary>
        /// Whether the game is active (in the foreground).
        /// </summary>
        public readonly IBindable<bool> IsActive = new Bindable<bool>(true);

        private double activeHz = DEFAULT_ACTIVE_HZ;

        public double ActiveHz
        {
            get => activeHz;
            set
            {
                activeHz = value;
                updateMaximumHz();
            }
        }

        private double inactiveHz = DEFAULT_INACTIVE_HZ;

        public double InactiveHz
        {
            get => inactiveHz;
            set
            {
                inactiveHz = value;
                updateMaximumHz();
            }
        }

        public static string PrefixedThreadNameFor(string name) => $"{nameof(GameThread)}.{name}";

        public virtual bool Running => Thread?.IsAlive == true;

        public virtual bool IsCurrent => true;

        private readonly ManualResetEvent initializedEvent = new ManualResetEvent(false);

        internal void Initialize(bool withThrottling)
        {
            MakeCurrent();

            OnInitialize();

            Clock.Throttling = withThrottling;

            Monitor.Thread = Thread.CurrentThread;

            updateCulture();

            initializedEvent.Set();
        }

        protected virtual void OnInitialize() { }

        internal virtual IEnumerable<StatisticsCounterType> StatisticsCounters => Array.Empty<StatisticsCounterType>();

        public readonly string Name;

        internal GameThread(Action onNewFrame = null, string name = "unknown", bool monitorPerformance = true)
        {
            OnNewFrame = onNewFrame;

            Name = name;
            Clock = new ThrottledFrameClock();
            if (monitorPerformance)
                Monitor = new PerformanceMonitor(this, StatisticsCounters);
            Scheduler = new GameThreadScheduler(this);

            IsActive.BindValueChanged(_ => updateMaximumHz(), true);
        }

        protected virtual Thread CreateRunningThread()
        {
            var thread = new Thread(runWork) { IsBackground = true };
            thread.Start();
            return thread;
        }

        public void WaitUntilInitialized()
        {
            initializedEvent.WaitOne();
        }

        private void updateMaximumHz() => Scheduler.Add(() => Clock.MaximumUpdateHz = IsActive.Value ? activeHz : inactiveHz);

        private void runWork()
        {
            try
            {
                Initialize(true);

                while (!Exited && !Paused)
                {
                    ProcessFrame();
                }
            }
            finally
            {
                Cleanup();
            }
        }

        /// <summary>
        /// Run when thread transitions into an active/processing state.
        /// </summary>
        internal virtual void MakeCurrent()
        {
            ThreadSafety.ResetAllForCurrentThread();
        }

        internal void ProcessFrame()
        {
            try
            {
                if (Exited)
                    return;

                MakeCurrent();

                Monitor?.NewFrame();

                using (Monitor?.BeginCollecting(PerformanceCollectionType.Scheduler))
                    Scheduler.Update();

                using (Monitor?.BeginCollecting(PerformanceCollectionType.Work))
                    OnNewFrame?.Invoke();

                using (Monitor?.BeginCollecting(PerformanceCollectionType.Sleep))
                    Clock.ProcessFrame();

                Monitor?.EndFrame();
            }
            catch (Exception e)
            {
                if (UnhandledException != null && !ThreadSafety.IsInputThread)
                    // the handler schedules back to the input thread, so don't run it if we are already on the input thread
                    UnhandledException.Invoke(this, new UnhandledExceptionEventArgs(e, false));
                else
                    throw;
            }
        }

        public bool Exited { get; private set; }

        private CultureInfo culture;

        public CultureInfo CurrentCulture
        {
            get => culture;
            set
            {
                culture = value;

                updateCulture();
            }
        }

        private void updateCulture()
        {
            if (Thread == null || culture == null) return;

            Thread.CurrentCulture = culture;
            Thread.CurrentUICulture = culture;
        }

        protected bool Paused;

        public void Pause()
        {
            if (Thread != null)
            {
                Paused = true;
                while (Running)
                    Thread.Sleep(1);
            }

            Cleanup();
        }

        protected virtual void Cleanup()
        {
            Thread = null;
            ThreadChanged?.Invoke(null);
        }

        public void Exit()
        {
            // if already on the correct thread, perform exit inline
            // can't rely on scheduler since ThreadSafety gets reset to non-current.
            if (Thread.CurrentThread == Thread)
                PerformExit();
            else
                Scheduler.Add(PerformExit);
        }

        public void Start()
        {
            Debug.Assert(Thread == null);

            Paused = false;

            Thread = CreateRunningThread();
            if (Thread.Name == null)
                Thread.Name = PrefixedThreadNameFor(Name);
            ThreadChanged?.Invoke(Thread);
        }

        protected virtual void PerformExit()
        {
            Monitor?.Dispose();
            initializedEvent?.Dispose();
            Exited = true;
        }

        public class GameThreadScheduler : Scheduler
        {
            public GameThreadScheduler(GameThread thread)
                : base(() => thread.IsCurrent, thread.Clock)
            {
            }
        }
    }
}
