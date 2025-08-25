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
            var stopwatch = new StopwatchClock();

            bass = new BassTestComponents();
            track = bass.GetTrack();

            Task.Run(async () =>
            {
                while (true)
                {
                    bass.Update();
                    //await Task.Delay(1).ConfigureAwait(false);
                }
            });

            const double track_start_time = -1000;
            const double elapsed_per_frame = 2;
            const double sync_lenience = 5;

            var decoupling = new DecouplingFramedClock(track);
            var interpolating = new InterpolatingFramedClock(decoupling);

            decoupling.Seek(track_start_time);
            stopwatch.Seek(track_start_time);

            stopwatch.Start();
            decoupling.Start();

            while (decoupling.CurrentTime < 1000)
            {
                interpolating.ProcessFrame();

                if (stopwatch.CurrentTime > -50)
                {
                    Console.WriteLine($"realtime: {stopwatch.CurrentTime:N2}");
                    Console.WriteLine($"track   : {track.CurrentTime:N2}");
                    Console.WriteLine();
                    Console.WriteLine($"decouple: {decoupling.CurrentTime:N2}");
                    Console.WriteLine($"drift   : {Math.Abs(decoupling.CurrentTime - stopwatch.CurrentTime):N2}");
                    Console.WriteLine();
                    Console.WriteLine($"interpol: {interpolating.CurrentTime:N2}");
                    Console.WriteLine($"realtime drift   : {Math.Abs(interpolating.CurrentTime - stopwatch.CurrentTime):N2}");
                    Console.WriteLine($"decouple drift      : {Math.Abs(interpolating.CurrentTime - decoupling.CurrentTime):N2}");
                    Console.WriteLine();
                    Console.WriteLine("-------------");
                    Console.WriteLine();
                }

                // if (Math.Abs(stopwatch.CurrentTime - decoupling.CurrentTime) >= sync_lenience)
                //     Assert.Fail($"Decoupled clock desynchronised from real time. decoupled:{decoupling.CurrentTime:0.000}, realtime:{stopwatch.CurrentTime:0.000}");

                Thread.Sleep((int)elapsed_per_frame);
            }
        }
    }
}
