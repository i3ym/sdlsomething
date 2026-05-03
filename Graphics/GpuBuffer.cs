namespace SdlSomething;

public static class GpuBuffer
{
    public static GpuBuffer<T> Create<T>(GpuDevice device, SDL.GPUBufferUsageFlags flags, ReadOnlySpan<T> span)
        where T : unmanaged
    {
        var buffer = new GpuBuffer<T>(device, span.Length, flags);
        using (var upload = new GpuTransferBuffer<T>(buffer))
            GpuTransferBuffer.UploadOnce(upload, span);

        return buffer;
    }
}

public readonly struct GpuBuffer<T> : IDisposable
    where T : unmanaged
{
    public readonly GpuDevice Device;
    public readonly nint Handle;
    public readonly int Length;
    public readonly uint BytesSize;

    public unsafe GpuBuffer(GpuDevice device, int length, SDL.GPUBufferUsageFlags flags)
    {
        Device = device;
        Length = length;
        BytesSize = (uint) sizeof(T) * (uint) length;

        Handle = SDL.CreateGPUBuffer(device.Handle, new SDL.GPUBufferCreateInfo()
        {
            Size = BytesSize,
            Usage = flags,
        });
    }

    public void Dispose() => SDL.ReleaseGPUBuffer(Device.Handle, Handle);
}

public sealed class ResizableGpuBuffer<T> : IDisposable
    where T : unmanaged
{
    public GpuDevice Device { get; }

    public T[] Arr
    {
        set
        {
            Data = value;
            Length = Data.Length;
            NeedsCopy = true;
        }
    }

    Span<T> DataSpan => Data.AsSpan(0, Length);
    public int Length { get; private set; }
    T[] Data = [];
    bool NeedsCopy = true;

    readonly SDL.GPUBufferUsageFlags Flags;
    GpuBuffer<T> Buffer;
    GpuTransferBuffer<T> TransferBuffer;

    public ResizableGpuBuffer(GpuDevice device, SDL.GPUBufferUsageFlags flags, T[] data) : this(device, flags) => Data = data;
    public ResizableGpuBuffer(GpuDevice device, SDL.GPUBufferUsageFlags flags)
    {
        Device = device;
        Flags = flags;
    }

    /// <summary>
    /// Sets the new buffer length to <paramref name="count"/> and returns a <see cref="Span{T}"/> of that region.
    /// </summary>
    public Span<T> GetWritableSpan(int count)
    {
        EnsureBufferAtLeast(count);

        NeedsCopy = true;
        Length = count;
        return DataSpan;
    }
    public void EnsureBufferAtLeast(int count)
    {
        if (Data.Length >= count) return;

        var prev = Data;
        Arr = new T[BytesExtensions.EnsureArrayLength(64, Data.Length, count)];
        prev.CopyTo(Data);
    }

    public void PrepareFrame(nint commandBuffer)
    {
        if (Length == 0)
        {
            // buffer has to exist
            if (Buffer.Length == 0)
            {
                // 4 bytes is the minimum size, wrote 32 just in case
                Buffer = new GpuBuffer<T>(Device, 32, Flags);
                TransferBuffer = new GpuTransferBuffer<T>(Buffer);
                NeedsCopy = false;
            }

            return;
        }

        if (Buffer.Length < Data.Length)
        {
            DisposeBuffers();

            Buffer = new GpuBuffer<T>(Device, Data.Length, Flags);
            TransferBuffer = new GpuTransferBuffer<T>(Buffer);

            TransferBuffer.WriteAndCopy(commandBuffer, DataSpan);
            NeedsCopy = false;
            return;
        }

        if (NeedsCopy)
        {
            TransferBuffer.WriteAndCopy(commandBuffer, DataSpan);
            NeedsCopy = false;
        }
    }
    public SDL.GPUBufferBinding GetBinding() => new SDL.GPUBufferBinding() { Buffer = Buffer.Handle };

    void DisposeBuffers()
    {
        if (Buffer.Length == 0) return;

        Buffer.Dispose();
        TransferBuffer.Dispose();
    }
    public void Dispose() => DisposeBuffers();
}
