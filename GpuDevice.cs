namespace SdlSomething;

public readonly struct GpuDevice
{
    public readonly nint Handle;

    public GpuDevice() => Handle = SDL.CreateGPUDevice(SDL.GPUShaderFormat.SPIRV, true, null);
}
