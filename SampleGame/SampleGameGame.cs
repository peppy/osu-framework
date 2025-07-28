// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using osu.Framework;
using osu.Framework.Graphics;
using osuTK;
using osuTK.Graphics;
using osu.Framework.Graphics.Shapes;
using osu.Framework.Allocation;
using osu.Framework.Audio.Track;
using osu.Framework.Graphics.Sprites;
using osu.Framework.Input.Events;

namespace SampleGame
{
    public partial class SampleGameGame : Game
    {
        private Box box = null!;
        private TrackBass bassTrack;
        private SpriteText text;

        [BackgroundDependencyLoader]
        private void load()
        {
            bassTrack = (TrackBass)Dependencies.Get<ITrackStore>().Get("test.ogg");
            bassTrack.Start();

            Add(box = new Box
            {
                Anchor = Anchor.Centre,
                Origin = Anchor.Centre,
                Size = new Vector2(150, 150),
                Colour = Color4.Tomato
            });

            Add(text = new SpriteText()
            {
                Anchor = Anchor.Centre,
                Origin = Anchor.Centre,
            });
        }

        protected override bool OnKeyDown(KeyDownEvent e)
        {
            bassTrack.Stop();
            bassTrack.Seek(200);
            bassTrack.Start();

            return base.OnKeyDown(e);
        }

        protected override void Update()
        {
            base.Update();
            box.Rotation += (float)Time.Elapsed / 10;

            if (bassTrack.CurrentTime < 100)
                text.Text = bassTrack.CurrentTime.ToString();
        }
    }
}
