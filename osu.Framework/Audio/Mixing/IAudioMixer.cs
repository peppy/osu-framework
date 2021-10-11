// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

#nullable enable

using System.Collections.Generic;
using ManagedBass;

namespace osu.Framework.Audio.Mixing
{
    /// <summary>
    /// An audio mixer which one or more <see cref="IAudioChannel"/>s can be routed into.
    /// Supports DSP effects independent of other <see cref="IAudioMixer"/>s.
    /// </summary>
    public interface IAudioMixer
    {
        /// <summary>
        /// The effects currently applied to the mix.
        /// </summary>
        IEnumerable<IEffectParameter> Effects { get; }

        /// <summary>
        /// Add a new effect to this mixer.
        /// </summary>
        /// <param name="effect">The effect to add.</param>
        void AddEffect(IEffectParameter effect);

        /// <summary>
        /// Add multiple new effects to this mixer.
        /// </summary>
        /// <param name="effects">The effects to add.</param>
        void AddEffects(IEnumerable<IEffectParameter> effects);

        /// <summary>
        /// Remove an effect from this mixer.
        /// </summary>
        /// <param name="effect">The effect to remove.</param>
        bool RemoveEffect(IEffectParameter effect);

        /// <summary>
        /// Update an already-applied effect's parameters.
        /// </summary>
        /// <param name="effect">The new parameters.</param>
        void UpdateEffect(IEffectParameter effect);

        /// <summary>
        /// Adds a channel to the mix.
        /// </summary>
        /// <param name="channel">The channel to add.</param>
        void Add(IAudioChannel channel);

        /// <summary>
        /// Removes a channel from the mix.
        /// </summary>
        /// <param name="channel">The channel to remove.</param>
        void Remove(IAudioChannel channel);
    }
}
