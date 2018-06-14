// Copyright (c) 2007-2018 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using NUnit.Framework;
using osu.Framework.Graphics.Textures;

namespace osu.Framework.Tests.IO
{
    [TestFixture]
    public class TestImageLoading
    {
        private static IEnumerable<Stream> getStreams()
        {
            foreach(var f in Directory.GetFiles("/Users/dean/Projects/osu/osu-resources/osu.Game.Resources/Fonts", "*.png"))
                yield return File.OpenRead(f);
        }

        [Test]
        public void TestSystemDrawing()
        {
            Stopwatch s = new Stopwatch();

            s.Start();

            foreach(var f in getStreams())
            {
                var tex = RawTexture.FromStream(f);
                    Console.WriteLine($"read {tex.Pixels.Length} bytes");
            }
            Console.WriteLine($"took {s.ElapsedMilliseconds}");
        }

        [Test]
        public void TestImageSharp()
        {
            Stopwatch s = new Stopwatch();

            s.Start();
            foreach(var f in getStreams())
            {
                var tex = new RawTextureUnknownStream(f);
                    Console.WriteLine($"read {tex.Bytes.Length} bytes");
            }
            Console.WriteLine($"took {s.ElapsedMilliseconds}");
        }
    }

    public class RawTexture
    {
        public int Width, Height;
        public byte[] Pixels;

        public static RawTexture FromStream(Stream stream)
        {
            using (Bitmap bitmap = new Bitmap(stream))
            {
                var data = bitmap.LockBits(new Rectangle(Point.Empty, bitmap.Size), ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb);

                RawTexture t = new RawTexture
                {
                    Width = bitmap.Width,
                    Height = bitmap.Height,
                    Pixels = new byte[data.Width * data.Height * 4],
                };

                unsafe
                {
                    //convert from BGRA (System.Drawing) to RGBA
                    //don't need to consider stride because we're in a raw format
                    var src = (byte*)data.Scan0;

                    Debug.Assert(src != null);

                    fixed (byte* pixels = t.Pixels)
                    {
                        var dest = pixels;

                        int length = t.Pixels.Length / 4;
                        for (int i = 0; i < length; i++)
                        {
                            //BGRA -> RGBA
                            // ReSharper disable once PossibleNullReferenceException
                            dest[0] = src[2];
                            dest[1] = src[1];
                            dest[2] = src[0];
                            dest[3] = src[3];

                            src += 4;
                            dest += 4;
                        }
                    }
                }

                bitmap.UnlockBits(data);

                return t;
            }
        }
    }
}
