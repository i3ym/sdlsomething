using Hexa.NET.ImGui;
using Hexa.NET.ImGui.Backends.SDL3;

namespace SdlSomething;

public sealed class Renderer : IDisposable
{
    public SDL.GPUTextureFormat ColorTextureFormat { get; }
    public nint MainDepthStencilTexture { get; private set; }

    uint Width, Height;

    public Window Window { get; }
    public GpuDevice Device { get; }
    readonly List<Viewport> Viewports = [];

    public Renderer(Window window, GpuDevice device)
    {
        Window = window;
        Device = device;
        ColorTextureFormat = SDL.GetGPUSwapchainTextureFormat(device.Handle, window.Handle);

        ImGuiController.Initialize(device, window, ColorTextureFormat);
    }

    public int GetInstancesCount() => Viewports.Sum(c => c.World?.InstancesCount ?? 0);
    internal void AddViewport(Viewport viewport) => Viewports.Add(viewport);
    internal void RemoveViewport(Viewport viewport) => Viewports.Remove(viewport);

    internal void Resize(uint w, uint h)
    {
        Width = w;
        Height = h;
        ReleaseDepthStencilTexture();
    }

    public bool Event(ref SDL.Event evt)
    {
        var type = (SDL.EventType) evt.Type;

        if (type == SDL.EventType.WindowPixelSizeChanged)
            Resize((uint) evt.Window.Data1, (uint) evt.Window.Data2);

        if (ImGuiController.ProcessEvent(ref evt))
            return true;

        return false;
    }

    public void Render()
    {
        if (!BeginRender(out var commandBuffer, out var swapchainTexture))
            return;

        foreach (var viewport in Viewports)
            viewport.Render(commandBuffer, swapchainTexture);

        EndRender(commandBuffer, swapchainTexture);
    }

    bool BeginRender(out nint commandBuffer, out nint swapchainTexture)
    {
        commandBuffer = SDL.AcquireGPUCommandBuffer(Device.Handle);
        ImGuiController.BeginFrame(); // TODO: < move to the start of the *update* not render

        {
            ImGui.Begin("hi");
            if (ImGui.Button("sus"))
                Console.WriteLine("red");

            ImGui.End();
        }

        SDL.WaitAndAcquireGPUSwapchainTexture(commandBuffer, Window.Handle, out swapchainTexture, out Width, out Height);
        if (swapchainTexture == 0)
        {
            EndRender(commandBuffer, swapchainTexture);
            return false;
        }

        if (MainDepthStencilTexture == 0)
            MainDepthStencilTexture = CreateDepthTexture(Width, Height);

        return true;
    }
    static void EndRender(nint commandBuffer, nint swapchainTexture)
    {
        if (swapchainTexture != 0)
            ImGuiController.Render(commandBuffer, swapchainTexture);

        SDL.SubmitGPUCommandBuffer(commandBuffer);
    }

    void ReleaseDepthStencilTexture()
    {
        if (MainDepthStencilTexture == 0) return;

        SDL.ReleaseGPUTexture(Device.Handle, MainDepthStencilTexture);
        MainDepthStencilTexture = 0;
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

    public void Dispose() => ImGuiController.Quit();


    static unsafe class ImGuiController
    {
        public static void Initialize(GpuDevice device, Window window, SDL.GPUTextureFormat colorTextureFormat)
        {
            var ctx = ImGui.CreateContext();

            var io = ImGui.GetIO();
            io.ConfigFlags |=
                ImGuiConfigFlags.NavEnableKeyboard
                | ImGuiConfigFlags.NavEnableGamepad
                | ImGuiConfigFlags.DockingEnable;
            io.ConfigDpiScaleFonts = true;
            io.ConfigDpiScaleViewports = true;
            ImGui.StyleColorsDark();

            ImGuiImplSDL3.SetCurrentContext(ctx);
            ImGuiImplSDL3.InitForSDLGPU(new SDLWindowPtr((SDLWindow*) window.Handle));

            var initInfo = new ImGuiImplSDLGPU3InitInfo()
            {
                Device = (SDLGPUDevice*) device.Handle,
                ColorTargetFormat = (int) colorTextureFormat,
            };
            ImGuiImplSDL3.SDLGPU3Init(&initInfo);
        }
        public static void Quit()
        {
            ImGuiImplSDL3.Shutdown();
            ImGuiImplSDL3.SDLGPU3Shutdown();
            ImGui.DestroyContext();
        }

        public static bool ProcessEvent(ref SDL.Event evt)
        {
            fixed (SDL.Event* e = &evt)
                ImGuiImplSDL3.ProcessEvent(new SDLEventPtr((SDLEvent*) (void*) e));

            var type = (SDL.EventType) evt.Type;
            if (type is SDL.EventType.MouseButtonDown or SDL.EventType.MouseWheel && ImGui.GetIO().WantCaptureMouse)
                return true;
            if (type is SDL.EventType.KeyDown && ImGui.GetIO().WantCaptureKeyboard)
                return true;

            return false;
        }
        public static void BeginFrame()
        {
            ImGuiImplSDL3.SDLGPU3NewFrame();
            ImGuiImplSDL3.NewFrame();
            ImGui.NewFrame();
        }
        public static void Render(nint commandBuffer, nint swapchainTexture)
        {
            ImGui.Render();
            var data = ImGui.GetDrawData();
            ImGuiImplSDL3.SDLGPU3PrepareDrawData(data, new SDLGPUCommandBufferPtr((SDLGPUCommandBuffer*) commandBuffer));

            var colorTarget = new SDL.GPUColorTargetInfo()
            {
                Texture = swapchainTexture,
                ClearColor = new SDL.FColor(0 / 255f, 0 / 255f, 0 / 255f, 255 / 255f),
                LoadOp = SDL.GPULoadOp.Load,
                StoreOp = SDL.GPUStoreOp.Store,
            };
            var renderPass = SDL.BeginGPURenderPass(commandBuffer, StructureToPointer(colorTarget), 1, 0);
            ImGuiImplSDL3.SDLGPU3RenderDrawData(data, new SDLGPUCommandBufferPtr((SDLGPUCommandBuffer*) commandBuffer), new SDLGPURenderPassPtr((SDLGPURenderPass*) renderPass), null);
            SDL.EndGPURenderPass(renderPass);
        }
    }
}
