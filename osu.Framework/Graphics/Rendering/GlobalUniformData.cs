// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Runtime.InteropServices;
using osu.Framework.Graphics.Shaders.Types;

namespace osu.Framework.Graphics.Rendering
{
    // sh_GlobalUniforms.h
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct GlobalUniformData : IEquatable<GlobalUniformData>
    {
        public UniformBool GammaCorrection;
        public UniformBool BackbufferDraw;
        private readonly UniformPadding8 pad1;

        public UniformMatrix4 ProjMatrix;
        public UniformMatrix3 ToMaskingSpace;
        public UniformBool IsMasking;
        public UniformFloat CornerRadius;
        public UniformFloat CornerExponent;
        private readonly UniformPadding4 pad2;

        public UniformVector4 MaskingRect;
        public UniformFloat BorderThickness;
        private readonly UniformPadding12 pad3;

        public UniformMatrix4 BorderColour;
        public UniformFloat MaskingBlendRange;
        public UniformFloat AlphaExponent;
        public UniformVector2 EdgeOffset;
        public UniformBool DiscardInner;
        public UniformFloat InnerCornerRadius;
        public UniformInt WrapModeS;
        public UniformInt WrapModeT;

        public UniformBool IsDepthRangeZeroToOne;
        public UniformBool IsClipSpaceYInverted;
        private readonly UniformPadding8 pad4;

        public bool Equals(GlobalUniformData other)
        {
            return GammaCorrection.Equals(other.GammaCorrection) && BackbufferDraw.Equals(other.BackbufferDraw) && pad1.Equals(other.pad1) && ProjMatrix.Equals(other.ProjMatrix) && ToMaskingSpace.Equals(other.ToMaskingSpace) && IsMasking.Equals(other.IsMasking) && CornerRadius.Equals(other.CornerRadius) && CornerExponent.Equals(other.CornerExponent) && pad2.Equals(other.pad2) && MaskingRect.Equals(other.MaskingRect) && BorderThickness.Equals(other.BorderThickness) && pad3.Equals(other.pad3) && BorderColour.Equals(other.BorderColour) && MaskingBlendRange.Equals(other.MaskingBlendRange) && AlphaExponent.Equals(other.AlphaExponent) && EdgeOffset.Equals(other.EdgeOffset) && DiscardInner.Equals(other.DiscardInner) && InnerCornerRadius.Equals(other.InnerCornerRadius) && WrapModeS.Equals(other.WrapModeS) && WrapModeT.Equals(other.WrapModeT) && IsDepthRangeZeroToOne.Equals(other.IsDepthRangeZeroToOne) && IsClipSpaceYInverted.Equals(other.IsClipSpaceYInverted) && pad4.Equals(other.pad4);
        }

        public override bool Equals(object? obj)
        {
            return obj is GlobalUniformData other && Equals(other);
        }

        public override int GetHashCode()
        {
            var hashCode = new HashCode();
            hashCode.Add(GammaCorrection);
            hashCode.Add(BackbufferDraw);
            hashCode.Add(pad1);
            hashCode.Add(ProjMatrix);
            hashCode.Add(ToMaskingSpace);
            hashCode.Add(IsMasking);
            hashCode.Add(CornerRadius);
            hashCode.Add(CornerExponent);
            hashCode.Add(pad2);
            hashCode.Add(MaskingRect);
            hashCode.Add(BorderThickness);
            hashCode.Add(pad3);
            hashCode.Add(BorderColour);
            hashCode.Add(MaskingBlendRange);
            hashCode.Add(AlphaExponent);
            hashCode.Add(EdgeOffset);
            hashCode.Add(DiscardInner);
            hashCode.Add(InnerCornerRadius);
            hashCode.Add(WrapModeS);
            hashCode.Add(WrapModeT);
            hashCode.Add(IsDepthRangeZeroToOne);
            hashCode.Add(IsClipSpaceYInverted);
            hashCode.Add(pad4);
            return hashCode.ToHashCode();
        }
    }
}
