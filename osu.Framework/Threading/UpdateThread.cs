// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using osu.Framework.Statistics;
using System;
using System.Collections.Generic;
using System.Threading;
using osu.Framework.Development;
using osu.Framework.Platform;

namespace osu.Framework.Threading
{
    public class UpdateThread : GameThread
    {
        private readonly DrawThread drawThread;

        private Thread nativeThread;

        public UpdateThread(Action onNewFrame, DrawThread drawThread)
            : base(onNewFrame, "Update")
        {
            this.drawThread = drawThread;
        }

        protected sealed override void OnInitialize()
        {
            if (ThreadSafety.ExecutionMode != ExecutionMode.SingleThread)
            {
                //this was added due to the dependency on GLWrapper.MaxTextureSize begin initialised.
                drawThread?.WaitUntilInitialized();
            }
        }

        protected override Thread CreateThread()
        {
            // To better support components which rely on remaining on the same managed thread for the lifetime of the application,
            // avoid recycling the underlying thread on Pause operations or execution mode switches.
            if (nativeThread != null)
                return nativeThread;

            return nativeThread = base.CreateThread();
        }

        protected override void RunWork()
        {
            while (State.Value != GameThreadState.Exited)
            {
                if (State.Value == GameThreadState.Starting)
                    base.RunWork();

                // as long as the target state is not exited, sleep waiting for a potential resume of execution.
                Thread.Sleep(10);
            }
        }

        public override bool IsCurrent => ThreadSafety.IsUpdateThread;

        internal sealed override void MakeCurrent()
        {
            base.MakeCurrent();

            ThreadSafety.IsUpdateThread = true;
        }

        internal override IEnumerable<StatisticsCounterType> StatisticsCounters => new[]
        {
            StatisticsCounterType.Invalidations,
            StatisticsCounterType.Refreshes,
            StatisticsCounterType.DrawNodeCtor,
            StatisticsCounterType.DrawNodeAppl,
            StatisticsCounterType.ScheduleInvk,
            StatisticsCounterType.InputQueue,
            StatisticsCounterType.PositionalIQ,
            StatisticsCounterType.CCL
        };
    }
}
