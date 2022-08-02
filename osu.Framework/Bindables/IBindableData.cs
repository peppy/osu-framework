// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

#nullable disable
namespace osu.Framework.Bindables
{
    public interface IBindableData<T> : IHasDefaultValue, IHasDescription
    {
        /// <inheritdoc cref="IBindable.GetBoundCopy"/>
        IBindable<T> GetBoundCopy();

        /// <summary>
        /// The current value of this bindable.
        /// </summary>
        T Value { get; }

        /// <summary>
        /// The default value of this bindable. Used when querying <see cref="IHasDefaultValue.IsDefault">IsDefault</see>.
        /// </summary>
        T Default { get; }
    }
}
