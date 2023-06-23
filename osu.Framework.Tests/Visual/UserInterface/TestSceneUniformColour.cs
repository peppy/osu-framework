// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.UserInterface;
using osu.Framework.Utils;
using osuTK;
using osuTK.Graphics;

namespace osu.Framework.Tests.Visual.UserInterface
{
    public partial class TestSceneUniformColour : FrameworkTestScene
    {
        private readonly Container content;

        public TestSceneUniformColour()
        {
            const int grid_size = 20;

            Add(content = new Container
            {
                Anchor = Anchor.Centre,
                Origin = Anchor.Centre,
                RelativeSizeAxes = Axes.Both
            });

            for (int i = 0; i < grid_size; i++)
            {
                for (int j = 0; j < grid_size; j++)
                {
                    content.Add(new CircularProgress
                    {
                        InnerRadius = RNG.NextSingle() * 0.5f + 0.5f,
                        Current = { Value = RNG.NextSingle() * 0.5f + 0.5f },
                        RelativeSizeAxes = Axes.Both,
                        Size = new Vector2(1f / grid_size),
                        RelativePositionAxes = Axes.Both,
                        Position = new Vector2((float)i / grid_size, (float)j / grid_size)
                    });
                }
            }
        }

        protected override void Update()
        {
            base.Update();

            var col = Interpolation.ValueAt(fract(Time.Current / 1000), Color4.Red, Color4.DarkRed, 0f, 1f);
            content.Colour = col;
            content.Rotation = fract(Time.Current / 10000) * 360;
            content.Position = new Vector2((float)Math.Sin(Time.Current / 500) * 100f);
        }

        private static float fract(double value) => (float)(value - Math.Truncate(value));
    }
}
