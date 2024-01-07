// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using osu.Framework.Utils;

namespace osu.Framework.Graphics.Transforms
{
    public interface ITransformSequence
    {
        protected static readonly ListPool<Transform> TRANSFORMS_LIST_POOL = new ListPool<Transform>();

        internal void TransformAborted();
        internal void TransformCompleted();
    }
}
