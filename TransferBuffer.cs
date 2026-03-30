namespace SdlSomething;

public readonly struct TransferBuffer<T>
    where T : unmanaged
{
    readonly nint Device;
    readonly nint Handle;
    readonly int Length;
    readonly uint BytesSize;

    public unsafe TransferBuffer(nint device, int length)
    {
        Device = device;
        Length = length;
        BytesSize = (uint) sizeof(T) * (uint) length;

        Handle = SDL.CreateGPUTransferBuffer(device, new SDL.GPUTransferBufferCreateInfo()
        {
            Size = BytesSize,
            Usage = SDL.GPUTransferBufferUsage.Upload,
        });
    }

    [UnscopedRef]
    public unsafe MappedTransferBuffer GetBuffer()
    {
        var bufferptr = SDL.MapGPUTransferBuffer(Device, Handle, false);
        return new(in this, new Span<T>((void*) bufferptr, Length));
    }
    public unsafe void Write(ReadOnlySpan<T> data)
    {
        var buffer = SDL.MapGPUTransferBuffer(Device, Handle, false);
        data.CopyTo(new Span<T>((void*) buffer, data.Length));
        SDL.UnmapGPUTransferBuffer(Device, Handle);
    }

    public void CopyPassTo(nint commandBuffer, nint buffer)
    {
        var copyPass = SDL.BeginGPUCopyPass(commandBuffer);

        var location = new SDL.GPUTransferBufferLocation()
        {
            TransferBuffer = Handle,
            Offset = 0,
        };
        var region = new SDL.GPUBufferRegion()
        {
            Buffer = buffer,
            Size = BytesSize,
            Offset = 0,
        };

        SDL.UploadToGPUBuffer(copyPass, location, region, true);
        SDL.EndGPUCopyPass(copyPass);
    }

    public void WriteAndCopy(nint commandBuffer, ReadOnlySpan<T> data, nint targetBuffer)
    {
        Write(data);
        CopyPassTo(commandBuffer, targetBuffer);
    }

    public readonly ref struct MappedTransferBuffer
    {
        readonly ref readonly TransferBuffer<T> TransferBuffer;
        public readonly Span<T> Span;

        public MappedTransferBuffer(in TransferBuffer<T> transferBuffer, Span<T> span)
        {
            TransferBuffer = ref transferBuffer;
            Span = span;
        }

        public void Dispose() => SDL.UnmapGPUTransferBuffer(TransferBuffer.Device, TransferBuffer.Handle);
    }
}

public static class GpuTransferBuffer
{
    public static void UploadOnce<T>(in GpuTransferBuffer<T> transferBuffer, ReadOnlySpan<T> span)
        where T : unmanaged
    {
        var commandBuffer = SDL.AcquireGPUCommandBuffer(transferBuffer.Device.Handle);
        transferBuffer.WriteAndCopy(commandBuffer, span);
        SDL.SubmitGPUCommandBuffer(commandBuffer);
    }
}

public readonly struct GpuTransferBuffer<T> : IDisposable
    where T : unmanaged
{
    public GpuDevice Device => Buffer.Device;
    public readonly GpuBuffer<T> Buffer;
    public readonly nint Handle;

    public GpuTransferBuffer(in GpuBuffer<T> buffer)
    {
        Buffer = buffer;

        Handle = SDL.CreateGPUTransferBuffer(Device.Handle, new SDL.GPUTransferBufferCreateInfo()
        {
            Size = buffer.BytesSize,
            Usage = SDL.GPUTransferBufferUsage.Upload,
        });
    }

    [UnscopedRef]
    public unsafe MappedTransferBuffer GetBuffer()
    {
        var bufferptr = SDL.MapGPUTransferBuffer(Device.Handle, Handle, false);
        return new(in this, new Span<T>((void*) bufferptr, Buffer.Length));
    }
    public unsafe void Write(ReadOnlySpan<T> data)
    {
        var buffer = SDL.MapGPUTransferBuffer(Device.Handle, Handle, false);
        data.CopyTo(new Span<T>((void*) buffer, data.Length));
        SDL.UnmapGPUTransferBuffer(Device.Handle, Handle);
    }

    public void CopyToBuffer(nint commandBuffer)
    {
        var copyPass = SDL.BeginGPUCopyPass(commandBuffer);

        var location = new SDL.GPUTransferBufferLocation()
        {
            TransferBuffer = Handle,
            Offset = 0,
        };
        var region = new SDL.GPUBufferRegion()
        {
            Buffer = Buffer.Handle,
            Size = Buffer.BytesSize,
            Offset = 0,
        };

        SDL.UploadToGPUBuffer(copyPass, location, region, true);
        SDL.EndGPUCopyPass(copyPass);
    }

    public void WriteAndCopy(nint commandBuffer, ReadOnlySpan<T> data)
    {
        Write(data);
        CopyToBuffer(commandBuffer);
    }

    public void Dispose() => SDL.Free(Handle);

    public readonly ref struct MappedTransferBuffer
    {
        readonly ref readonly GpuTransferBuffer<T> TransferBuffer;
        public readonly Span<T> Span;

        public MappedTransferBuffer(in GpuTransferBuffer<T> transferBuffer, Span<T> span)
        {
            TransferBuffer = ref transferBuffer;
            Span = span;
        }

        public void Dispose() => SDL.UnmapGPUTransferBuffer(TransferBuffer.Device.Handle, TransferBuffer.Handle);
    }
}
