// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Collections.Generic;
using System.Diagnostics;
using ManagedBass;
using osu.Framework.Allocation;
using osu.Framework.Audio;
using osu.Framework.Audio.Mixing;
using osu.Framework.Graphics.Containers;

namespace osu.Framework.Graphics.Audio
{
    public class DrawableAudioMixer : AudioContainer, IAudioMixer
    {
        private AudioMixer mixer;

        [BackgroundDependencyLoader]
        private void load(AudioManager audio)
        {
            mixer = audio.CreateAudioMixer();
        }

        public void Add(IAudioChannel channel)
        {
            if (LoadState < LoadState.Ready)
                Schedule(() => mixer.Add(channel));
            else
            {
                Debug.Assert(mixer != null);
                mixer.Add(channel);
            }
        }

        public void Remove(IAudioChannel channel)
        {
            if (LoadState < LoadState.Ready)
                Schedule(() => mixer.Remove(channel));
            else
            {
                Debug.Assert(mixer != null);
                mixer.Remove(channel);
            }
        }

        protected override void Dispose(bool isDisposing)
        {
            base.Dispose(isDisposing);
            mixer?.Dispose();
        }

        IEnumerable<IEffectParameter> IAudioMixer.Effects => ((IAudioMixer)mixer).Effects;
        public void AddEffect(IEffectParameter effect) => mixer.AddEffect(effect);
        public void AddEffects(IEnumerable<IEffectParameter> effects) => mixer.AddEffects(effects);

        public bool RemoveEffect(IEffectParameter effect) => mixer.RemoveEffect(effect);
        public void UpdateEffect(IEffectParameter effect) => mixer.UpdateEffect(effect);
    }
}
