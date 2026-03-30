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

    public void Dispose() => SDL.Free(Handle);
}
