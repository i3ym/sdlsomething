namespace SdlSomething;

public readonly struct GpuBuffer<T>
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
}
