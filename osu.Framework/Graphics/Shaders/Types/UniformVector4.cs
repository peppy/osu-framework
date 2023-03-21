// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Runtime.InteropServices;
using osuTK;

namespace osu.Framework.Graphics.Shaders.Types
{
    /// <summary>
    /// Must be aligned to a 16-byte boundary.
    /// </summary>
    [StructLayout(LayoutKind.Sequential, Pack = 1, Size = 16)]
    public struct UniformVector4 : IEquatable<UniformVector4>
    {
        public UniformFloat X;
        public UniformFloat Y;
        public UniformFloat Z;
        public UniformFloat W;

        public static implicit operator Vector4(UniformVector4 value) => new Vector4
        {
            X = value.X,
            Y = value.Y,
            Z = value.Z,
            W = value.W
        };

        public static implicit operator UniformVector4(Vector4 value) => new UniformVector4
        {
            X = value.X,
            Y = value.Y,
            Z = value.Z,
            W = value.W
        };

        public bool Equals(UniformVector4 other)
        {
            return X.Equals(other.X) && Y.Equals(other.Y) && Z.Equals(other.Z) && W.Equals(other.W);
        }

        public override bool Equals(object? obj)
        {
            return obj is UniformVector4 other && Equals(other);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(X, Y, Z, W);
        }
    }
}
