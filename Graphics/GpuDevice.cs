namespace SdlSomething;

public readonly struct GpuDevice : IDisposable
{
    public readonly nint Handle;

    public GpuDevice() => Handle = SDL.CreateGPUDevice(SDL.GPUShaderFormat.SPIRV, true, null);

    public void Dispose() => SDL.DestroyGPUDevice(Handle);
}
