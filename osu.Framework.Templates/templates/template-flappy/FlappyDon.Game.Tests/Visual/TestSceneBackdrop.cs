// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using osu.Framework.Allocation;
using osu.Framework.Testing;

namespace FlappyDon.Game.Tests.Visual
{
    /// <summary>
    /// A test scene for testing the alignment
    /// and placement of the sprites that make up the backdrop
    /// </summary>
    public class TestSceneBackdrop : TestScene
    {
        [BackgroundDependencyLoader]
        private void load()
        {
            Add(new Backdrop(() => new BackdropSprite()));
        }
    }
}
