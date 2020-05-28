// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using osu.Framework;
using osu.Framework.Graphics;
using osuTK;
using osuTK.Graphics;
using osu.Framework.Graphics.Shapes;
using osu.Framework.Allocation;
using osu.Framework.Bindables;

namespace SampleGame
{
    public class SampleGameGame : Game
    {
        private Box box;

        //private readonly BindableList<object> groups = new BindableList<object>();
        // ^ no crash

        private readonly IBindableList<object> groups = new BindableList<object>();
        // ^ crash


        [BackgroundDependencyLoader]
        private void load()
        {
            Add(box = new Box
            {
                Anchor = Anchor.Centre,
                Origin = Anchor.Centre,
                Size = new Vector2(150, 150),
                Colour = Color4.Tomato
            });
        }

        protected override void LoadComplete()
        {
            base.LoadComplete();

            groups.GetBoundCopy();

            groups.GetBoundCopy();
            // ^ crash only on second call
        }

        protected override void Update()
        {
            base.Update();

            box.Rotation += (float)Time.Elapsed / 10;
        }
    }
}
