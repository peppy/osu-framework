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
    [StructLayout(LayoutKind.Sequential, Pack = 1, Size = 64)]
    public struct UniformMatrix4 : IEquatable<UniformMatrix4>
    {
        public UniformVector4 Row0;
        public UniformVector4 Row1;
        public UniformVector4 Row2;
        public UniformVector4 Row3;

        public static implicit operator Matrix4(UniformMatrix4 matrix) => new Matrix4
        {
            Row0 = matrix.Row0,
            Row1 = matrix.Row1,
            Row2 = matrix.Row2,
            Row3 = matrix.Row3
        };

        public static implicit operator UniformMatrix4(Matrix4 matrix) => new UniformMatrix4
        {
            Row0 = matrix.Row0,
            Row1 = matrix.Row1,
            Row2 = matrix.Row2,
            Row3 = matrix.Row3
        };

        public bool Equals(UniformMatrix4 other)
        {
            return Row0.Equals(other.Row0) && Row1.Equals(other.Row1) && Row2.Equals(other.Row2) && Row3.Equals(other.Row3);
        }

        public override bool Equals(object? obj)
        {
            return obj is UniformMatrix4 other && Equals(other);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(Row0, Row1, Row2, Row3);
        }
    }
}
