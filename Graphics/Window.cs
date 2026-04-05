namespace SdlSomething.Graphics;

public readonly struct Window : IDisposable
{
    public readonly nint Handle;

    public Window(nint handle) => Handle = handle;
    public Window() : this(SDL.CreateWindow("WAAA", 2560, 1440, SDL.WindowFlags.Resizable)) { }

    public void ClaimGPU(GpuDevice device)
    {
        if (!SDL.ClaimWindowForGPUDevice(device.Handle, Handle))
            throw new Exception(SDL.GetError());
    }

    public (uint w, uint h) GetSize()
    {
        SDL.GetWindowSizeInPixels(Handle, out var ww, out var hh);
        return ((uint) ww, (uint) hh);
    }

    public void Dispose() => SDL.DestroyWindow(Handle);
}
