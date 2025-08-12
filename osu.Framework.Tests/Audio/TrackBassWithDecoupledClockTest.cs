// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;
using osu.Framework.Audio.Track;
using osu.Framework.Timing;

namespace osu.Framework.Tests.Audio
{
    [TestFixture]
    public class TrackBassWithDecoupledClockTest
    {
        private BassTestComponents bass = null!;
        private TrackBass track = null!;

        [Test]
        public void TestSomething()
        {
            var stopwatch = new StopwatchClock(true);

            bass = new BassTestComponents();
            track = bass.GetTrack();

            Task.Run(async () =>
            {
                while (true)
                {
                    bass.Update();
                    await Task.Delay(1).ConfigureAwait(false);
                }
            });

            const double track_start_time = -1000;
            const double elapsed_per_frame = 1;
            const double sync_lenience = 5;

            var decoupling = new DecouplingFramedClock(track);
            decoupling.Seek(track_start_time);
            decoupling.Start();

            double startInRealTime = stopwatch.CurrentTime;

            while (decoupling.CurrentTime < 1000)
            {
                decoupling.ProcessFrame();

                double realtime = stopwatch.CurrentTime - startInRealTime + track_start_time;

                if (Math.Abs(realtime - decoupling.CurrentTime) >= sync_lenience)
                    Assert.Fail($"Decoupled clock desynchronised from real time. decoupled:{decoupling.CurrentTime:0.000}, realtime:{realtime:0.000}");

                Thread.Sleep((int)elapsed_per_frame);
            }
        }
    }
}
