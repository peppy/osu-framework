// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Runtime.InteropServices;

namespace osu.Framework.Graphics.Shaders.Types
{
    /// <summary>
    /// A single 4-byte padding to be used for uniform block definitions.
    /// </summary>
    [StructLayout(LayoutKind.Sequential, Size = 4)]
    public struct UniformPadding4 : IEquatable<UniformPadding4>
    {
        public bool Equals(UniformPadding4 other)
        {
            return true;
        }
    }
}
