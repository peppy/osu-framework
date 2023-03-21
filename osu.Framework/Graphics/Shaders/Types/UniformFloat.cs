// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Runtime.InteropServices;

namespace osu.Framework.Graphics.Shaders.Types
{
    /// <summary>
    /// Must be aligned to a 4-byte boundary.
    /// </summary>
    [StructLayout(LayoutKind.Sequential, Pack = 1, Size = 4)]
    public struct UniformFloat : IEquatable<UniformFloat>
    {
        public float Value;

        public static implicit operator float(UniformFloat value) => value.Value;
        public static implicit operator UniformFloat(float value) => new UniformFloat { Value = value };

        public bool Equals(UniformFloat other)
        {
            return Value.Equals(other.Value);
        }

        public override bool Equals(object? obj)
        {
            return obj is UniformFloat other && Equals(other);
        }

        public override int GetHashCode()
        {
            return Value.GetHashCode();
        }
    }
}
