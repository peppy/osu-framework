﻿// Copyright (c) 2007-2018 ppy Pty Ltd <contact@ppy.sh>.
// Licensed under the MIT Licence - https://raw.githubusercontent.com/ppy/osu-framework/master/LICENCE

using Cyotek.Drawing.BitmapFont;
using osu.Framework.Allocation;
using osu.Framework.Graphics.Textures;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using osu.Framework.Logging;

namespace osu.Framework.IO.Stores
{
    public class GlyphStore : IResourceStore<IRawTexture>
    {
        private readonly string assetName;

        private readonly string fontName;

        private const float default_size = 96;

        private readonly ResourceStore<byte[]> store;
        private BitmapFont font;

        private readonly TimedExpiryCache<int, RawTextureBitmap> texturePages = new TimedExpiryCache<int, RawTextureBitmap>();

        private Task fontLoadTask;

        public GlyphStore(ResourceStore<byte[]> store, string assetName = null, bool precache = false)
        {
            this.store = store;
            this.assetName = assetName;

            fontName = assetName?.Split('/').Last();

            fontLoadTask = readFontMetadataAsync(precache);
        }

        private async Task readFontMetadataAsync(bool precache)
        {
            await Task.Run(() =>
            {
                try
                {
                    font = new BitmapFont();
                    using (var s = store.GetStream($@"{assetName}.fnt"))
                        font.LoadText(s);

                    if (precache)
                        for (int i = 0; i < font.Pages.Length; i++)
                            getTexturePage(i);
                }
                catch
                {
                    Logger.Log($"Couldn't load font asset from {assetName}.");
                    throw;
                }
            });

            fontLoadTask = null;
        }

        public bool HasGlyph(char c) => font.Characters.ContainsKey(c);
        public int GetBaseHeight() => font.BaseHeight;
        public int? GetBaseHeight(string name)
        {
            if (name != fontName)
                return null;

            return font.BaseHeight;
        }

        public IRawTexture Get(string name)
        {
            lock (this)
            {


                if (name.Length > 1 && !name.StartsWith($@"{fontName}/", StringComparison.Ordinal))
                    return null;

                try
                {
                    fontLoadTask?.Wait();
                }
                catch
                {
                    return null;
                }

                if (!font.Characters.TryGetValue(name.Last(), out Character c))
                    return null;

                RawTextureBitmap page = getTexturePage(c.TexturePage);
                loadedGlyphCount++;

                int width = c.Bounds.Width + c.Offset.X + 1;
                int height = c.Bounds.Height + c.Offset.Y + 1;

                int length = width * height * 4;
                byte[] pixels = new byte[length];

                using (var locker = page.ObtainLock())
                    unsafe
                    {
                        var src = (byte*)locker.DataPointer;
                        Debug.Assert(src != null);

                        for (int y = 0; y < height; y++)
                        {
                            for (int x = 0; x < width; x++)
                            {
                                int desti = y * width * 4 + x * 4;
                                if (x >= c.Offset.X && y >= c.Offset.Y
                                                    && x - c.Offset.X < c.Bounds.Width && y - c.Offset.Y < c.Bounds.Height)
                                {
                                    int srci = (c.Bounds.Y + y - c.Offset.Y) * page.Width * 4 + (c.Bounds.X + x - c.Offset.X) * 4;
                                    pixels[desti] = src[srci];
                                    pixels[desti + 1] = src[srci + 1];
                                    pixels[desti + 2] = src[srci + 2];
                                    pixels[desti + 3] = src[srci + 3];
                                }
                                else
                                {
                                    pixels[desti] = 255;
                                    pixels[desti + 1] = 255;
                                    pixels[desti + 2] = 255;
                                    pixels[desti + 3] = 0;
                                }
                            }
                        }
                    }

                return new RawTextureBytes(pixels, new System.Drawing.Rectangle(0, 0, width, height));
                // TODO: page?.GetSubregion(new Rectangle(c.Bounds.X - c.Offset.X, c.Bounds.Y - c.Offset.Y, width, height));
            }
        }

        private RawTextureBitmap getTexturePage(int texturePage)
        {
            if (!texturePages.TryGetValue(texturePage, out RawTextureBitmap t))
            {
                loadedPageCount++;
                using (var stream = store.GetStream($@"{assetName}_{texturePage.ToString().PadLeft((font.Pages.Length - 1).ToString().Length, '0')}.png"))
                    texturePages.Add(texturePage, t = stream != null ? new RawTextureBitmap(stream) : null);
            }

            return t;
        }

        public Stream GetStream(string name)
        {
            throw new NotSupportedException();
        }

        private int loadedPageCount;
        private int loadedGlyphCount;

        public override string ToString() => $@"GlyphStore({assetName}) LoadedPages:{loadedPageCount} LoadedGlyphs:{loadedGlyphCount}";
    }

    public class FontStore : TextureStore
    {
        private readonly List<GlyphStore> glyphStores = new List<GlyphStore>();

        public FontStore()
        {
        }

        public FontStore(GlyphStore glyphStore)
            : base(glyphStore)
        {
        }

        public override void AddStore(IResourceStore<IRawTexture> store)
        {
            var gs = store as GlyphStore;
            if (gs != null)
                glyphStores.Add(gs);
            base.AddStore(store);
        }

        public override void RemoveStore(IResourceStore<IRawTexture> store)
        {
            var gs = store as GlyphStore;
            if (gs != null)
                glyphStores.Remove(gs);
            base.RemoveStore(store);
        }

        public float? GetBaseHeight(char c)
        {
            foreach (var store in glyphStores)
            {
                if (store.HasGlyph(c))
                    return store.GetBaseHeight() / ScaleAdjust;
            }
            return null;
        }

        public float? GetBaseHeight(string fontName)
        {
            foreach (var store in glyphStores)
            {
                var bh = store.GetBaseHeight(fontName);
                if (bh.HasValue)
                    return bh.Value / ScaleAdjust;
            }
            return null;
        }
    }
}
