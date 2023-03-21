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
    public struct UniformBool : IEquatable<UniformBool>
    {
        public bool Value;

        public static implicit operator bool(UniformBool value) => value.Value;
        public static implicit operator UniformBool(bool value) => new UniformBool { Value = value };

        public bool Equals(UniformBool other)
        {
            return Value == other.Value;
        }

        public override bool Equals(object? obj)
        {
            return obj is UniformBool other && Equals(other);
        }

        public override int GetHashCode()
        {
            return Value.GetHashCode();
        }
    }
}
