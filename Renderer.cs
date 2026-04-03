namespace SdlSomething;

public sealed class Renderer
{
    public nint MainDepthStencilTexture { get; private set; }

    //     public ulong Frame { get; private set; }
    public uint WindowWidth { get; private set; }
    public uint WindowHeight { get; private set; }

    public Window Window { get; }
    public GpuDevice Device { get; }
    internal MainViewport MainViewport { get; }
    public List<SubViewport> Viewports { get; } = [];

    public Renderer(Window window, GpuDevice device)
    {
        Window = window;
        Device = device;

        MainViewport = new MainViewport(this, new());
    }

    internal void Resize(uint w, uint h)
    {
        WindowWidth = w;
        WindowHeight = h;
        ReleaseDepthStencilTexture();
    }

    public void Render()
    {
        var commandBuffer = SDL.AcquireGPUCommandBuffer(Device.Handle);

        SDL.WaitAndAcquireGPUSwapchainTexture(commandBuffer, Window.Handle, out var swapchainTexture, out var w, out var h);
        WindowWidth = w;
        WindowHeight = h;

        if (MainDepthStencilTexture == nint.Zero)
            MainDepthStencilTexture = CreateDepthTexture(WindowWidth, WindowHeight);

        if (swapchainTexture == nint.Zero)
        {
            SDL.SubmitGPUCommandBuffer(commandBuffer);
            return;
        }

        MainViewport.Render(commandBuffer, swapchainTexture);
        foreach (var viewport in Viewports)
            viewport.Render(commandBuffer, swapchainTexture);

        SDL.SubmitGPUCommandBuffer(commandBuffer);
    }

    void ReleaseDepthStencilTexture()
    {
        if (MainDepthStencilTexture == nint.Zero) return;

        SDL.ReleaseGPUTexture(Device.Handle, MainDepthStencilTexture);
        MainDepthStencilTexture = nint.Zero;
    }
    nint CreateDepthTexture(uint w, uint h)
    {
        return SDL.CreateGPUTexture(
            Device.Handle,
            new SDL.GPUTextureCreateInfo()
            {
                Type = SDL.GPUTextureType.TextureType2D,
                Width = w,
                Height = h,
                LayerCountOrDepth = 1,
                NumLevels = 1,
                SampleCount = SDL.GPUSampleCount.SampleCount1,
                Format = GetStencilFormat(Device),
                Usage = SDL.GPUTextureUsageFlags.DepthStencilTarget,
            }
        );
    }
}
