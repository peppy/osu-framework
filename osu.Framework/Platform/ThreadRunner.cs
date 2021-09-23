// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Threading;
using osu.Framework.Development;
using osu.Framework.Extensions.IEnumerableExtensions;
using osu.Framework.Logging;
using osu.Framework.Threading;

namespace osu.Framework.Platform
{
    /// <summary>
    /// Runs a game host in a specific threading mode.
    /// </summary>
    public class ThreadRunner
    {
        private GameThread updateThread;

        private readonly GameThread loopThread;

        private readonly List<GameThread> threads = new List<GameThread>();

        public IReadOnlyCollection<GameThread> Threads
        {
            get
            {
                lock (threads)
                    return threads.ToArray();
            }
        }

        private double maximumUpdateHz = GameThread.DEFAULT_ACTIVE_HZ;

        public double MaximumUpdateHz
        {
            set
            {
                maximumUpdateHz = value;
                updateMainThreadRates();
            }
        }

        private double maximumInactiveHz = GameThread.DEFAULT_INACTIVE_HZ;

        public double MaximumInactiveHz
        {
            set
            {
                maximumInactiveHz = value;
                updateMainThreadRates();
            }
        }

        public ThreadRunner()
        {
            loopThread = new GameThread(RunMainLoop, "ThreadRunner", false);
        }

        private readonly object startStopLock = new object();

        /// <summary>
        /// Add a new non-main thread. In single-threaded execution, threads will be executed in the order they are added.
        /// </summary>
        public void AddThread(GameThread thread)
        {
            lock (threads)
            {
                updateThread ??= thread;

                if (!threads.Contains(thread))
                    threads.Add(thread);
            }
        }

        /// <summary>
        /// Remove a non-main thread.
        /// </summary>
        public void RemoveThread(GameThread thread)
        {
            lock (threads)
                threads.Remove(thread);
        }

        private ExecutionMode? activeExecutionMode;

        private bool suspended;

        public ExecutionMode ExecutionMode { private get; set; } = ExecutionMode.MultiThreaded;

        public virtual void RunMainLoop()
        {
            if (suspended)
            {
                Thread.Sleep(10);
                return;
            }

            // propagate any requested change in execution mode at a safe point in frame execution
            ensureCorrectExecutionMode();

            Debug.Assert(activeExecutionMode != null);

            switch (activeExecutionMode.Value)
            {
                case ExecutionMode.SingleThread:
                {
                    lock (threads)
                    {
                        foreach (var t in threads)
                            t.RunSingleFrame();
                    }

                    break;
                }

                case ExecutionMode.MultiThreaded:
                    // still need to run the main/input thread on the window loop
                    updateThread.RunSingleFrame();
                    break;
            }
        }

        public void Start()
        {
            suspended = false;
            if (!loopThread.Running)
                loopThread.Start();
        }

        public void Suspend()
        {
            lock (startStopLock)
            {
                pauseAllThreads();

                activeExecutionMode = null;
                suspended = true;
            }
        }

        public void Stop()
        {
            const int thread_join_timeout = 30000;

            Threads.ForEach(t => t.Exit());
            Threads.Where(t => t.Running).ForEach(t =>
            {
                var thread = t.Thread;

                if (thread == null)
                {
                    // has already been cleaned up (or never started)
                    return;
                }

                if (!thread.Join(thread_join_timeout))
                    Logger.Log($"Thread {t.Name} failed to exit in allocated time ({thread_join_timeout}ms).", LoggingTarget.Runtime, LogLevel.Important);
            });

            // as the input thread isn't actually handled by a thread, the above join does not necessarily mean it has been completed to an exiting state.
            updateThread.WaitForState(GameThreadState.Exited);

            ThreadSafety.ResetAllForCurrentThread();

            loopThread.Exit();
        }

        private void ensureCorrectExecutionMode()
        {
            // locking is required as this method may be called from two different threads.
            lock (startStopLock)
            {
                if (ExecutionMode == activeExecutionMode)
                    return;

                activeExecutionMode = ThreadSafety.ExecutionMode = ExecutionMode;
                Logger.Log($"Execution mode changed to {activeExecutionMode}");
            }

            pauseAllThreads();

            switch (activeExecutionMode)
            {
                case ExecutionMode.MultiThreaded:
                {
                    // switch to multi-threaded
                    foreach (var t in Threads)
                    {
                        if (t == updateThread)
                            t.Initialize(true);
                        else
                            t.Start();
                    }

                    break;
                }

                case ExecutionMode.SingleThread:
                {
                    // switch to single-threaded.
                    foreach (var t in Threads)
                    {
                        // only throttle for the main thread
                        t.Initialize(withThrottling: t == updateThread);
                    }

                    // this is usually done in the execution loop, but required here for the initial game startup,
                    // which would otherwise leave values in an incorrect state.
                    ThreadSafety.ResetAllForCurrentThread();
                    break;
                }
            }

            updateMainThreadRates();
        }

        private void pauseAllThreads()
        {
            // shut down threads in reverse to ensure audio stops last (other threads may be waiting on a queued event otherwise)
            foreach (var t in Threads.Reverse())
                t.Pause();
        }

        private void updateMainThreadRates()
        {
            loopThread.ActiveHz = updateThread.ActiveHz = maximumUpdateHz;
            loopThread.InactiveHz = updateThread.InactiveHz = maximumInactiveHz;
        }

        /// <summary>
        /// Sets the current culture of all threads to the supplied <paramref name="culture"/>.
        /// </summary>
        public void SetCulture(CultureInfo culture)
        {
            // for single-threaded mode, switch the current (assumed to be main) thread's culture, since it's actually the one that's running the frames.
            Thread.CurrentThread.CurrentCulture = culture;

            // for multi-threaded mode, schedule the culture change on all threads.
            // note that if the threads haven't been created yet (e.g. if the game started single-threaded), this will only store the culture in GameThread.CurrentCulture.
            // in that case, the stored value will be set on the actual threads after the next Start() call.
            foreach (var t in Threads)
            {
                t.Scheduler.Add(() => t.CurrentCulture = culture);
            }
        }
    }
}
