// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.

// See the LICENCE file in the repository root for full licence text.

using System;
using System.Diagnostics;
using System.Threading;

namespace osu.Framework.Timing
{
    /// <summary>
    /// A FrameClock which will limit the number of frames processed by adding Thread.Sleep calls on each ProcessFrame.
    /// </summary>
    public class ThrottledFrameClock : FramedClock
    {
        /// <summary>
        /// The target number of updates per second. Only used when <see cref="Throttling"/> is <c>true</c>.
        /// </summary>
        /// <remarks>
        /// A value of 0 is treated the same as "unlimited" or <see cref="double.MaxValue"/>.
        /// </remarks>
        public double MaximumUpdateHz = 1000.0;

        /// <summary>
        /// Whether throttling should be enabled. Defaults to <c>true</c>.
        /// </summary>
        public bool Throttling = true;

        private double nextFrameTime;

        /// <summary>
        /// The time spent in a Thread.Sleep state during the last frame.
        /// </summary>
        public double TimeSlept { get; private set; }

        public override void ProcessFrame()
        {
            Debug.Assert(MaximumUpdateHz >= 0);

            base.ProcessFrame();

            if (Source.IsRunning && Throttling)
                throttle();
            else
                TimeSlept = 0;

            Debug.Assert(TimeSlept <= ElapsedFrameTime);
        }

        private void throttle()
        {
            double timeToSpare = nextFrameTime - CurrentTime;

            if (timeToSpare > 0)
                alignTo(nextFrameTime);
            else
            {
                // yield, not sure if we still want this.
                Thread.Sleep(0);
            }

            double timeAfterSleep = SourceTime;

            TimeSlept = timeAfterSleep - CurrentTime;
            CurrentTime = timeAfterSleep;

            if (timeToSpare < 1000)
            {
                // Edge case for if we are running far behind (ie. the thread got suspended)
                nextFrameTime = CurrentTime + 1000 / MaximumUpdateHz;
            }
            else
                nextFrameTime += 1000 / MaximumUpdateHz;
        }

        private void alignTo(double targetMilliseconds)
        {
            double remaining() => targetMilliseconds - SourceTime;

            while (remaining() > 8) Thread.Sleep(TimeSpan.FromMilliseconds(4));
            while (remaining() > 4) Thread.Yield();
            while (remaining() > 1) Thread.Sleep(0);
            while (remaining() > 0) Thread.SpinWait(32);
        }
    }
}
