// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using ManagedBass;
using ManagedBass.Mix;
using osu.Framework.Allocation;
using osu.Framework.Audio;
using osu.Framework.Audio.Track;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Audio;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Shapes;
using osu.Framework.Graphics.Sprites;
using osuTK;

namespace osu.Framework.Tests.Visual.Audio
{
    public class TestSceneBassMix2 : FrameworkTestScene
    {
        private int mixerHandle;
        private int trackHandle;
        private int sfxHandle;
        private int sfx2Handle;

        private int reverbHandle;
        private int compressorHandle;

        // private const int num_mix_channels = 8;
        // private ChannelStrip2[] channelStrips = new ChannelStrip2[num_mix_channels];

        private AudioManager audio;
        // private AudioMixer mixer;

        private TrackBass bassTrack;
        private ITrackStore tracks;
        private DrawableSample sample;

        [BackgroundDependencyLoader]
        private void load(ITrackStore tracks, AudioManager audio)
        {
            this.tracks = tracks;
            this.audio = audio;

            Child = new Mixer(audio.Mixer);
        }

        protected override void LoadComplete()
        {
            base.LoadComplete();

            AddStep("idle", () =>
            {
                // do nothing
            });
            AddStep("load", () =>
            {
                // test = audio.Samples.Get("loud");
                bassTrack = (TrackBass)tracks.Get("sample-track.mp3");
                // mixer.AddChannel(bassTrack.ActiveStream);
            });
            AddStep("play", () =>
            {
                // test?.Play();
                bassTrack?.Start();
            });
            AddStep("stop", () =>
            {
                bassTrack?.Stop();
            });
            // AddStep("start track", () =>
            // {
            //     Bass.ChannelSetPosition(trackHandle, 0);
            //     BassMix.ChannelFlags(trackHandle, BassFlags.Default, BassFlags.MixerPause);
            // });
            // AddStep("stop track", () =>
            // {
            //     BassMix.ChannelFlags(trackHandle, BassFlags.MixerPause, BassFlags.MixerPause);
            // });
            //
            // AddStep("Reverb on", () =>
            // {
            //     // reverbHandle = Bass.ChannelSetFX(mixerHandle, EffectType.Freeverb, 100);
            //     reverbHandle = Bass.ChannelSetFX(sfxHandle, EffectType.Freeverb, 100);
            //     Bass.FXSetParameters(reverbHandle, new ReverbParameters
            //     {
            //         fDryMix = 1f,
            //         fWetMix = 0.1f,
            //     });
            //     Logger.Log($"[BASSDLL] ChannelSetFX: {Bass.LastError}");
            // });
            // AddStep("Reverb off", () =>
            // {
            //     Bass.ChannelRemoveFX(sfxHandle, reverbHandle);
            //     Logger.Log($"[BASSDLL] ChannelSetFX: {Bass.LastError}");
            // });
            //
            // AddStep("Compressor on", () =>
            // {
            //     compressorHandle = Bass.ChannelSetFX(mixerHandle, EffectType.Compressor, 1);
            //     Bass.FXSetParameters(compressorHandle, new CompressorParameters
            //     {
            //         fAttack = 5,
            //         fRelease = 100,
            //         fThreshold = -6,
            //         fGain = 0,
            //         // fRatio = 4,
            //     });
            //     Logger.Log($"[BASSDLL] ChannelSetFX: {Bass.LastError}");
            // });
            // AddStep("Compressor off", () =>
            // {
            //     Bass.ChannelRemoveFX(mixerHandle, compressorHandle);
            //     Logger.Log($"[BASSDLL] ChannelSetFX: {Bass.LastError}");
            // });
            //
            // AddStep("Load SFX1", () =>
            // {
            //
            // });
            AddStep("Play SFX1", () =>
            {
                sample = new DrawableSample(audio.Samples.Get("long.mp3"));
                sample?.Play();
                // Bass.ChannelSetPosition(sample, 0);
                // BassMix.ChannelFlags(sfxHandle, BassFlags.Default, BassFlags.MixerPause);
            });
            //
            // AddStep("Play SFX2", () =>
            // {
            //     Bass.ChannelSetPosition(sfx2Handle, 0);
            //     BassMix.ChannelFlags(sfx2Handle, BassFlags.Default, BassFlags.MixerPause);
            // });

            AddStep("Reset Peaks", () =>
            {

                // foreach (var strip in channelStrips)
                // {
                //     strip.Reset();
                // }
            });
        }

        // protected override void Dispose(bool isDisposing)
        // {
        //     base.Dispose(isDisposing);
        //
        //     Bass.StreamFree(trackHandle);
        // }

        // protected override void Update()
        // {
        //     base.Update();
        //
        //     if (mixer.MixChannels.Count < 1)
        //         return;
        //
        //     for (int i = 0; i < num_mix_channels; i++)
        //     {
        //         if (i >= mixer.MixChannels.Count)
        //             break;
        //
        //         channelStrips[i].Handle = mixer.MixChannels[i];
        //     }
        // }

        // private double calculateReplayGain(byte[] data)
        // {
        //     int replayGainProcessingStream = Bass.CreateStream(data, 0, data.Length, BassFlags.Decode);
        //     TrackGain trackGain = new TrackGain(44100, 16);
        //
        //     const int buf_len = 1024;
        //     short[] buf = new short[buf_len];
        //
        //     List<int> leftSamples = new List<int>();
        //     List<int> rightSamples = new List<int>();
        //
        //     while (true)
        //     {
        //         int length = Bass.ChannelGetData(replayGainProcessingStream, buf, buf_len * sizeof(short));
        //         if (length == -1) break;
        //
        //         for (int a = 0; a < length / sizeof(short); a += 2)
        //         {
        //             leftSamples.Add(buf[a]);
        //             rightSamples.Add(buf[a + 1]);
        //         }
        //     }
        //
        //     trackGain.AnalyzeSamples(leftSamples.ToArray(), rightSamples.ToArray());
        //
        //     double gain = trackGain.GetGain();
        //     double peak = trackGain.GetPeak();
        //
        //     Logger.Log($"REPLAYGAIN GAIN: {gain}");
        //     Logger.Log($"REPLAYGAIN PEAK: {peak}");
        //
        //     Bass.StreamFree(replayGainProcessingStream);
        //
        //     return gain;
        // }

        public class Mixer : CompositeDrawable
        {
            private readonly AudioMixer mixer;

            // private readonly BindableList<ChannelStrip2> channelStrips = new BindableList<ChannelStrip2>();
            // private ChannelStrip2[] channelStrips;

            private const int num_mix_channels = 8;
            private readonly ChannelStrip2[] channelStrips = new ChannelStrip2[num_mix_channels];

            public Mixer(AudioMixer mixer)
            {
                this.mixer = mixer;
            }

            [BackgroundDependencyLoader]
            private void load()
            {
                for (int i = 0; i < num_mix_channels; i++)
                {
                    channelStrips[i] = new ChannelStrip2
                    {
                        IsMixerChannel = (i < num_mix_channels - 1),
                        Width = 1f / num_mix_channels
                    };
                }

                RelativeSizeAxes = Axes.Both;
                Size = new Vector2(1.0f);
                InternalChild = new FillFlowContainer
                {
                    RelativeSizeAxes = Axes.Both,
                    Size = new Vector2(1.0f),
                    Children = channelStrips
                };
            }

            protected override void Update()
            {
                base.Update();

                if (mixer.MixChannels.Count < 1)
                    return;

                for (int i = 0; i < num_mix_channels; i++)
                {
                    if (i >= mixer.MixChannels.Count)
                        break;

                    channelStrips[i].Handle = mixer.MixChannels[i];
                }
            }
        }

        public class ChannelStrip2 : CompositeDrawable
        {
            public int Handle { get; set; }
            public int BuffSize = 30;
            public bool IsMixerChannel { get; set; } = true;

            private float maxPeak = float.MinValue;
            private float peak = float.MinValue;
            private float gain;
            private Box volBarL;
            private Box volBarR;
            private SpriteText peakText;
            private SpriteText maxPeakText;

            public ChannelStrip2(int handle = 0)
            {
                Handle = handle;

                RelativeSizeAxes = Axes.Both;
                InternalChildren = new Drawable[]
                {
                    volBarL = new Box
                    {
                        RelativeSizeAxes = Axes.Both,
                        Origin = Anchor.BottomLeft,
                        Anchor = Anchor.BottomLeft,
                        Colour = Colour4.Green,
                        Height = 1f,
                        Width = 0.5f,
                    },
                    volBarR = new Box
                    {
                        RelativeSizeAxes = Axes.Both,
                        Origin = Anchor.BottomRight,
                        Anchor = Anchor.BottomRight,
                        Colour = Colour4.Green,
                        Height = 1f,
                        Width = 0.5f,
                    },
                    new FillFlowContainer
                    {
                        AutoSizeAxes = Axes.Both,
                        Direction = FillDirection.Vertical,
                        Children = new[]
                        {
                            peakText = new SpriteText
                            {
                                Text = "N/A",
                            },
                            maxPeakText = new SpriteText
                            {
                                Text = "N/A",
                            }
                        }
                    }
                };
            }

            protected override void Update()
            {
                base.Update();

                if (Handle == 0)
                {
                    volBarL.Height = 0;
                    volBarR.Height = 0;
                    peakText.Text = "N/A";
                    maxPeakText.Text = "N/A";

                    return;
                }

                float[] levels = new float[2];

                if (IsMixerChannel)
                    BassMix.ChannelGetLevel(Handle, levels, 1 / (float)BuffSize, LevelRetrievalFlags.Stereo);
                else
                    Bass.ChannelGetLevel(Handle, levels, 1 / (float)BuffSize, LevelRetrievalFlags.Stereo);

                peak = (levels[0] + levels[1]) / 2f;
                maxPeak = Math.Max(peak, maxPeak);

                volBarL.TransformTo(nameof(Drawable.Height), levels[0], BuffSize * 4);
                volBarR.TransformTo(nameof(Drawable.Height), levels[1], BuffSize * 4);
                peakText.Text = $"{BassUtils.LevelToDb(peak):F}dB";
                maxPeakText.Text = $"{BassUtils.LevelToDb(maxPeak):F}dB";
            }

            public void Reset()
            {
                peak = float.MinValue;
                maxPeak = float.MinValue;
            }
        }
    }
}
