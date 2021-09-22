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
            if (nativeThread != null)
                return nativeThread;

            return nativeThread = base.CreateThread();
        }

        protected override void RunWork()
        {
            while (State.Value != GameThreadState.Exited)
            {
                if (State.Value == GameThreadState.Starting)
                {
                    Initialize(true);

                    while (Running)
                        RunSingleFrame();
                }

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
