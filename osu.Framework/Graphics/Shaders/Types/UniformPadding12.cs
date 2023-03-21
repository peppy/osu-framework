// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Runtime.InteropServices;

namespace osu.Framework.Graphics.Shaders.Types
{
    /// <summary>
    /// A single 12-byte padding to be used for uniform block definitions.
    /// </summary>
    [StructLayout(LayoutKind.Sequential, Size = 12)]
    public struct UniformPadding12 : IEquatable<UniformPadding12>
    {
        public bool Equals(UniformPadding12 other)
        {
            return true;
        }
    }
}
