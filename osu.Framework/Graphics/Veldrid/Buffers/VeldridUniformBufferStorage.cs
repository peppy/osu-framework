// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Runtime.InteropServices;
using osu.Framework.Graphics.Veldrid.Buffers.Staging;
using osu.Framework.Platform;
using Veldrid;

namespace osu.Framework.Graphics.Veldrid.Buffers
{
    internal class VeldridUniformBufferStorage<TData>
        where TData : unmanaged, IEquatable<TData>
    {
        private readonly VeldridRenderer renderer;
        private readonly DeviceBuffer buffer;
        private readonly NativeMemoryTracker.NativeMemoryLease memoryLease;

        private ResourceSet? set;

        private readonly IStagingBuffer<TData> data;

        public VeldridUniformBufferStorage(VeldridRenderer renderer)
        {
            this.renderer = renderer;

            data = renderer.CreateStagingBuffer<TData>(1);
            buffer = renderer.Factory.CreateBuffer(new BufferDescription((uint)Marshal.SizeOf(default(TData)), BufferUsage.UniformBuffer | BufferUsage.Dynamic));
            memoryLease = NativeMemoryTracker.AddMemory(this, buffer.SizeInBytes);
        }

        public TData Data
        {
            get => data.Data[0];
            set
            {
                data.Data[0] = value;
                data.CopyTo(buffer, 0, 0, 1);
            }
        }

        public ResourceSet GetResourceSet(ResourceLayout layout) => set ??= renderer.Factory.CreateResourceSet(new ResourceSetDescription(layout, buffer));

        public void Dispose()
        {
            buffer.Dispose();
            memoryLease.Dispose();
            set?.Dispose();
        }
    }
}
