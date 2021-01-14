// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Threading;
using ManagedBass;
using NUnit.Framework;
using osu.Framework.Audio.Sample;
using osu.Framework.Development;
using osu.Framework.IO.Stores;
using osu.Framework.Threading;

namespace osu.Framework.Tests.Audio
{
    [TestFixture]
    public class SampleBassTest
    {
        private DllResourceStore resources;

        private SampleBass sample;

        private SampleChannelBass channel;

        [SetUp]
        public void Setup()
        {
            // Initialize bass with no audio to make sure the test remains consistent even if there is no audio device.
            Bass.Init(0);

            resources = new DllResourceStore(typeof(TrackBassTest).Assembly);

            sample = new SampleBass(resources.Get("Resources.Tracks.sample-track.mp3"));

            channel = new SampleChannelBass(sample, channel => { });
            updateSample();
        }

        [TearDown]
        public void Teardown()
        {
            Bass.Free();
        }

        [Test]
        public void TestStart()
        {
            channel.Play();
            updateSample();

            Thread.Sleep(50);

            updateSample();

            Assert.IsTrue(channel.Playing);
        }

        [Test]
        public void TestStop()
        {
            channel.Play();
            updateSample();

            channel.Stop();
            updateSample();

            Assert.IsFalse(channel.Playing);
        }

        [Test]
        public void TestStopBeforeLoadFinished()
        {
            channel.Play();
            channel.Stop();

            updateSample();

            Assert.IsFalse(channel.Playing);
        }

        [Test]
        public void TestStandardPlaybackConcurrency()
        {
            channel.Play();
            updateSample();
            channel.Play();
            updateSample();
            channel.Play();
            updateSample();

            Assert.AreEqual(1, Bass.SampleGetChannels(sample.SampleId).Length);
        }

        [Test]
        public void TestLayeredPlaybackConcurrency()
        {
            channel.PlayStopsPreviousPlayback = false;

            channel.Play();
            updateSample();
            channel.Play();
            updateSample();
            channel.Play();
            updateSample();

            Assert.AreEqual(Sample.DEFAULT_CONCURRENCY, Bass.SampleGetChannels(sample.SampleId).Length);
        }

        private void updateSample() => runOnAudioThread(() =>
        {
            sample.Update();
            channel.Update();
        });

        /// <summary>
        /// Certain actions are invoked on the audio thread.
        /// Here we simulate this process on a correctly named thread to avoid endless blocking.
        /// </summary>
        /// <param name="action">The action to perform.</param>
        private void runOnAudioThread(Action action)
        {
            var resetEvent = new ManualResetEvent(false);

            new Thread(() =>
            {
                ThreadSafety.IsAudioThread = true;

                action();

                resetEvent.Set();
            })
            {
                Name = GameThread.PrefixedThreadNameFor("Audio")
            }.Start();

            if (!resetEvent.WaitOne(TimeSpan.FromSeconds(10)))
                throw new TimeoutException();
        }
    }
}
