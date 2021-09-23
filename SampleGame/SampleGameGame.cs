// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Threading;
using osu.Framework;
using osu.Framework.Graphics;
using osuTK;
using osuTK.Graphics;
using osu.Framework.Graphics.Shapes;
using osu.Framework.Allocation;
using osu.Framework.Graphics.Sprites;
using osu.Framework.Bindables;
using osu.Framework.Configuration;
using osu.Framework.Platform;

namespace SampleGame
{
    public class SampleGameGame : Game
    {
        private Box box;
        private SpriteText thread;
        private Bindable<ExecutionMode> executingMode;

        [Resolved]
        private FrameworkConfigManager frameworkConfigManager { get; set; }

        [BackgroundDependencyLoader]
        private void load()
        {
            Children = new Drawable[]
            {
                box = new Box
                {
                    Anchor = Anchor.Centre,
                    Origin = Anchor.Centre,
                    Size = new Vector2(150, 150),
                    Colour = Color4.Tomato
                },
                thread = new SpriteText()
            };

            executingMode = frameworkConfigManager.GetBindable<ExecutionMode>(FrameworkSetting.ExecutionMode);
        }

        protected override void Update()
        {
            base.Update();
            box.Rotation += (float)Time.Elapsed / 10;

            thread.Text = $"Thread ID: {Thread.CurrentThread.ManagedThreadId}";

            executingMode.Value = executingMode.Value == ExecutionMode.MultiThreaded ? ExecutionMode.SingleThread : ExecutionMode.MultiThreaded;
        }
    }
}
