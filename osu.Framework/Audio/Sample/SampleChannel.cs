// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using osu.Framework.Statistics;
using System;
using osu.Framework.Audio.Track;

namespace osu.Framework.Audio.Sample
{
    public abstract class SampleChannel : AdjustableAudioComponent, ISampleChannel
    {
        protected bool WasStarted;

        /// <summary>
        /// Whether each invocation of <see cref="Play"/> results in a new (potentially concurrent) playback.
        /// </summary>
        internal bool ConcurrentPlayback;

        protected Sample Sample { get; set; }

        private readonly Action<SampleChannel> onPlay;

        protected SampleChannel(Sample sample, Action<SampleChannel> onPlay)
        {
            Sample = sample ?? throw new ArgumentNullException(nameof(sample));
            this.onPlay = onPlay;
        }

        public virtual void Play(bool restart = true)
        {
            if (ConcurrentPlayback)
            {
                if (Looping)
                    throw new InvalidOperationException($"Cannot play a layered sample playback if {nameof(Looping)} is enabled.");

                if (!restart)
                    throw new ArgumentException("Cannot resume playback of a layered sample playback.", nameof(restart));
            }

            if (IsDisposed)
                throw new ObjectDisposedException(ToString(), "Can not play disposed samples.");

            onPlay(this);
            WasStarted = true;
        }

        public virtual void Stop()
        {
            if (ConcurrentPlayback)
                throw new InvalidOperationException($"Cannot call {nameof(Stop)} on a layered sample playback.");

            if (IsDisposed)
                throw new ObjectDisposedException(ToString(), "Can not stop disposed samples.");
        }

        protected override void Dispose(bool disposing)
        {
            if (!IsDisposed && !ConcurrentPlayback)
                Stop();

            base.Dispose(disposing);
        }

        protected override void UpdateState()
        {
            FrameStatistics.Increment(StatisticsCounterType.SChannels);
            base.UpdateState();
        }

        public abstract bool Playing { get; }

        public virtual bool Played => WasStarted && !Playing;

        public double Length => Sample.Length;

        public override bool IsAlive => base.IsAlive && !Played;

        public virtual ChannelAmplitudes CurrentAmplitudes { get; } = ChannelAmplitudes.Empty;
    }
}
