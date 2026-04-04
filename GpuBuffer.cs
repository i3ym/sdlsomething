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
    public int Length => Data.Length;

    public ReadOnlyMemory<T> ReadonlyData => Data;
    public ref Memory<T> WritableData { get { NeedsCopy = true; return ref Data; } }

    Memory<T> Data = Array.Empty<T>();
    bool NeedsCopy = true;

    readonly SDL.GPUBufferUsageFlags Flags;
    GpuBuffer<T> Buffer;
    GpuTransferBuffer<T> TransferBuffer;

    public ResizableGpuBuffer(GpuDevice device, SDL.GPUBufferUsageFlags flags, T[] data) : this(device, flags, data.AsMemory()) { }
    public ResizableGpuBuffer(GpuDevice device, SDL.GPUBufferUsageFlags flags, Memory<T> data) : this(device, flags) => Data = data;
    public ResizableGpuBuffer(GpuDevice device, SDL.GPUBufferUsageFlags flags)
    {
        Device = device;
        Flags = flags;
    }

    internal void PrepareFrame(nint commandBuffer)
    {
        if (Data.Length == 0)
            throw new InvalidOperationException("No data");

        if (Buffer.Length < Data.Length)
        {
            DisposeBuffers();
            Buffer = new GpuBuffer<T>(Device, Data.Length, Flags);
            TransferBuffer = new GpuTransferBuffer<T>(Buffer);

            TransferBuffer.WriteAndCopy(commandBuffer, Data.Span);
            NeedsCopy = false;
            return;
        }

        if (NeedsCopy)
        {
            TransferBuffer.WriteAndCopy(commandBuffer, Data.Span);
            NeedsCopy = false;
        }
    }
    public void GetBinding(out SDL.GPUBufferBinding binding) => binding = new SDL.GPUBufferBinding() { Buffer = Buffer.Handle };

    void DisposeBuffers()
    {
        if (Buffer.Length == 0) return;

        Buffer.Dispose();
        TransferBuffer.Dispose();
    }
    public void Dispose() => DisposeBuffers();
}
