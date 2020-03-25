// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Threading;
using NUnit.Framework;
using osu.Framework.Timing;

namespace osu.Framework.Tests.Clocks
{
    [TestFixture]
    public class DecoupledFramedClockTest
    {
        private TestClockWithRange source;
        private TestDecoupledClock decoupled;

        [SetUp]
        public void SetUp()
        {
            source = new TestClockWithRange();

            decoupled = new TestDecoupledClock();
            decoupled.ChangeSource(source);
        }

        #region Start/stop by decoupleable

        /// <summary>
        /// Tests that the source clock starts when the coupled clock starts.
        /// </summary>
        [Test]
        public void TestSourceStartedByCoupled()
        {
            decoupled.Start();

            Assert.IsTrue(source.IsRunning, "Source should be running.");
        }

        /// <summary>
        /// Tests that the source clock stops when the coupled clock stops.
        /// </summary>
        [Test]
        public void TestSourceStoppedByCoupled()
        {
            decoupled.Start();
            decoupled.Stop();

            Assert.IsFalse(source.IsRunning, "Source should not be running.");
        }

        /// <summary>
        /// Tests that the source clock starts when the decoupled clock starts.
        /// </summary>
        [Test]
        public void TestSourceStartedByDecoupled()
        {
            decoupled.ClampRangeToSource = false;
            decoupled.Start();

            Assert.IsTrue(source.IsRunning, "Source should be running.");
        }

        /// <summary>
        /// Tests that the source clock stops when the decoupled clock stops.
        /// </summary>
        [Test]
        public void TestSourceStoppedByDecoupled()
        {
            decoupled.Start();

            decoupled.ClampRangeToSource = false;
            decoupled.Stop();

            Assert.IsFalse(source.IsRunning, "Source should not be running.");
        }

        #endregion

        #region Start/stop by source

        /// <summary>
        /// Tests that the coupled clock starts when the source clock starts.
        /// </summary>
        [Test]
        public void TestCoupledStartedBySourceClock()
        {
            source.Start();
            decoupled.ProcessFrame();

            Assert.IsTrue(decoupled.IsRunning, "Coupled should be running.");
        }

        /// <summary>
        /// Tests that the coupled clock stops when the source clock stops.
        /// </summary>
        [Test]
        public void TestCoupledStoppedBySourceClock()
        {
            decoupled.Start();

            source.Stop();
            decoupled.ProcessFrame();

            Assert.IsFalse(decoupled.IsRunning, "Coupled should not be running.");
        }

        /// <summary>
        /// Tests that the decoupled clock doesn't start when the source clock starts.
        /// </summary>
        [Test]
        public void TestDecoupledNotStartedBySourceClock()
        {
            decoupled.ClampRangeToSource = false;

            source.Start();
            decoupled.ProcessFrame();

            Assert.IsFalse(decoupled.IsRunning, "Decoupled should not be running.");
        }

        /// <summary>
        /// Tests that the decoupled clock doesn't stop when the source clock stops.
        /// </summary>
        [Test]
        public void TestDecoupledNotStoppedBySourceClock()
        {
            decoupled.Start();
            decoupled.ClampRangeToSource = false;

            source.Stop();
            decoupled.ProcessFrame();

            Assert.IsTrue(decoupled.IsRunning, "Decoupled should be running.");
        }

        #endregion

        #region Offset start

        /// <summary>
        /// Tests that the coupled clock seeks to the correct position when the source clock starts.
        /// </summary>
        [Test]
        public void TestCoupledStartBySourceWithSourceOffset()
        {
            source.Seek(1000);

            source.Start();
            decoupled.ProcessFrame();

            Assert.AreEqual(source.CurrentTime, decoupled.CurrentTime, "Coupled time should match source time.");
        }

        /// <summary>
        /// Tests that the coupled clock seeks the source clock to its time when it starts.
        /// </summary>
        [Test]
        public void TestCoupledStartWithSouceOffset()
        {
            source.Seek(1000);
            decoupled.Start();

            Assert.AreEqual(0, source.CurrentTime);
            Assert.AreEqual(source.CurrentTime, decoupled.CurrentTime, "Coupled time should match source time.");
        }

        [Test]
        public void TestFromNegativeCoupledMode()
        {
            decoupled.ClampRangeToSource = true;
            decoupled.Seek(-1000);

            decoupled.ProcessFrame();

            Assert.AreEqual(0, source.CurrentTime);
            Assert.AreEqual(0, decoupled.CurrentTime);
        }

        /// <summary>
        /// Tests that the decoupled clocks starts the source as a result of being able to handle the current time.
        /// </summary>
        [Test]
        public void TestDecoupledStartsSourceIfAllowable()
        {
            decoupled.ClampRangeToSource = false;
            decoupled.CustomAllowableErrorMilliseconds = 1000;
            decoupled.Seek(-50);
            decoupled.ProcessFrame();
            decoupled.Start();

            // Delay a bit to make sure the clock crosses the 0 boundary
            Thread.Sleep(100);
            decoupled.ProcessFrame();

            Assert.That(source.IsRunning, Is.True);
        }

        /// <summary>
        /// Tests that during forward playback the decoupled clock always moves in the forwards direction after starting the source clock.
        /// For this test, the source clock is started when the decoupled time crosses the 0ms-boundary.
        /// </summary>
        [Test]
        public void TestForwardPlaybackDecoupledTimeDoesNotRewindAfterSourceStarts()
        {
            decoupled.ClampRangeToSource = false;
            decoupled.CustomAllowableErrorMilliseconds = 1000;
            decoupled.Seek(-50);
            decoupled.ProcessFrame();
            decoupled.Start();

            // Delay a bit to make sure the clock crosses the 0ms boundary
            Thread.Sleep(100);
            decoupled.ProcessFrame();

            // Make sure that time doesn't rewind. Note that the source clock does not move by itself,
            double last = decoupled.CurrentTime;
            decoupled.ProcessFrame();
            Assert.That(decoupled.CurrentTime, Is.GreaterThanOrEqualTo(last));
        }

        /// <summary>
        /// Tests that during backwards playback the decoupled clock always moves in the backwards direction after starting the source clock.
        /// For this test, the source clock is started when the decoupled time crosses the 1000ms-boundary.
        /// </summary>
        [Test]
        public void TestBackwardPlaybackDecoupledTimeDoesNotRewindAfterSourceStarts()
        {
            source.MaxTime = 1000;
            decoupled.ClampRangeToSource = false;
            decoupled.CustomAllowableErrorMilliseconds = 1000;
            decoupled.Rate = -1;

            // Bring the source clock into a good state by seeking to a valid time
            decoupled.Seek(1000);
            decoupled.Start();
            decoupled.ProcessFrame();
            decoupled.Stop();

            decoupled.Seek(1050);
            decoupled.ProcessFrame();
            decoupled.Start();

            // Delay a bit to make sure the clock crosses the 1000ms boundary
            Thread.Sleep(100);
            decoupled.ProcessFrame();

            // Make sure that time doesn't rewind
            double last = decoupled.CurrentTime;
            decoupled.ProcessFrame();
            Assert.That(decoupled.CurrentTime, Is.LessThanOrEqualTo(last));
        }

        /// <summary>
        /// Tests that the decoupled clock seeks the source clock to its time when it starts.
        /// </summary>
        [Test]
        public void TestDecoupledStartWithSourceOffset()
        {
            decoupled.ClampRangeToSource = false;

            source.Seek(1000);
            decoupled.Start();

            Assert.AreEqual(0, source.CurrentTime);
            Assert.AreEqual(source.CurrentTime, decoupled.CurrentTime, "Deoupled time should match source time.");
        }

        #endregion

        #region Seeking

        /// <summary>
        /// Tests that the source clock is seeked when the coupled clock is seeked.
        /// </summary>
        [Test]
        public void TestSourceSeekedByCoupledSeek()
        {
            decoupled.Seek(1000);

            Assert.AreEqual(source.CurrentTime, source.CurrentTime, "Source time should match coupled time.");
        }

        /// <summary>
        /// Tests that the coupled clock is seeked when the source clock is seeked.
        /// </summary>
        [Test]
        public void TestCoupledSeekedBySourceSeek()
        {
            decoupled.Start();

            source.Seek(1000);
            decoupled.ProcessFrame();

            Assert.AreEqual(source.CurrentTime, decoupled.CurrentTime, "Coupled time should match source time.");
        }

        /// <summary>
        /// Tests that the source clock is seeked when the decoupled clock is seeked.
        /// </summary>
        [Test]
        public void TestSourceSeekedByDecoupledSeek()
        {
            decoupled.ClampRangeToSource = false;
            decoupled.Seek(1000);

            Assert.AreEqual(decoupled.CurrentTime, source.CurrentTime, "Source time should match coupled time.");
        }

        /// <summary>
        /// Tests that the coupled clock is not seeked while stopped and the source clock is seeked.
        /// </summary>
        [Test]
        public void TestDecoupledNotSeekedBySourceSeekWhenStopped()
        {
            decoupled.ClampRangeToSource = false;

            source.Seek(1000);
            decoupled.ProcessFrame();

            Assert.AreEqual(0, decoupled.CurrentTime);
            Assert.AreNotEqual(source.CurrentTime, decoupled.CurrentTime, "Coupled time should not match source time.");
        }

        /// <summary>
        /// Tests that seeking a decoupled clock negatively does not cause it to seek to the positive source time.
        /// </summary>
        [Test]
        public void TestDecoupledNotSeekedPositivelyByFailedNegativeSeek()
        {
            decoupled.ClampRangeToSource = false;
            decoupled.Start();

            decoupled.Seek(-5000);

            Assert.That(source.IsRunning, Is.False);
            Assert.That(decoupled.IsRunning, Is.True);
            Assert.That(decoupled.CurrentTime, Is.LessThan(0));
        }

        #endregion

        /// <summary>
        /// Tests that the state of the decouplable clock is preserved when it is stopped after processing a frame.
        /// </summary>
        [Test]
        public void TestStoppingAfterProcessingFramePreservesState()
        {
            decoupled.Start();
            source.CurrentTime = 1000;

            decoupled.ProcessFrame();
            decoupled.Stop();

            Assert.AreEqual(source.CurrentTime, decoupled.CurrentTime, decoupled.AllowableErrorMilliseconds, "Decoupled should match source time.");
        }

        /// <summary>
        /// Tests that the state of the decouplable clock is preserved when it is stopped after having being started by the source clock.
        /// </summary>
        [Test]
        public void TestStoppingAfterStartingBySourcePreservesState()
        {
            source.Start();
            source.CurrentTime = 1000;

            decoupled.ProcessFrame();
            decoupled.Stop();

            Assert.AreEqual(source.CurrentTime, decoupled.CurrentTime, decoupled.AllowableErrorMilliseconds, "Decoupled should match source time.");
        }

        private class TestDecoupledClock : DecoupledFramedClock
        {
            public double? CustomAllowableErrorMilliseconds { get; set; }

            public override double AllowableErrorMilliseconds => CustomAllowableErrorMilliseconds ?? base.AllowableErrorMilliseconds;
        }

        private class TestClockWithRange : TestClock
        {
            public double MinTime { get; set; } = 0;
            public double MaxTime { get; set; } = double.PositiveInfinity;

            public override bool Seek(double position)
            {
                if (Math.Clamp(position, MinTime, MaxTime) != position)
                    return false;

                return base.Seek(position);
            }
        }
    }
}
