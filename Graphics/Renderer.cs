using Hexa.NET.ImGui;
using Hexa.NET.ImGui.Backends.SDL3;

namespace SdlSomething;

public sealed class Renderer : IDisposable
{
    public nint MainDepthStencilTexture { get; private set; }

    public ulong Frame { get; private set; }
    public uint WindowWidth { get; private set; }
    public uint WindowHeight { get; private set; }

    public Window Window { get; }
    public GpuDevice Device { get; }
    public MainViewport MainViewport { get; }
    public List<SubViewport> Viewports { get; } = [];

    public Renderer(Window window, GpuDevice device)
    {
        Window = window;
        Device = device;

        MainViewport = new MainViewport(this, new());

        ImGuiController.Initialize(device, window);
    }

    void Resize(uint w, uint h)
    {
        WindowWidth = w;
        WindowHeight = h;
        ReleaseDepthStencilTexture();
    }

    public bool Event(ref SDL.Event evt)
    {
        if (ImGuiController.ProcessEvent(ref evt))
            return true;

        var type = (SDL.EventType) evt.Type;

        if (type == SDL.EventType.WindowResized)
            Resize((uint) evt.Window.Data1, (uint) evt.Window.Data2);

        return false;
    }

    public void Render()
    {
        var unused = 0L;
        Render(ref unused);
    }
    public void Render(ref long startTime)
    {
        Frame++;
        var commandBuffer = SDL.AcquireGPUCommandBuffer(Device.Handle);
        ImGuiController.BeginFrame();

        {
            ImGui.Begin("hi");
            if (ImGui.Button("sus"))
                Console.WriteLine("red");

            ImGui.End();
        }

        SDL.WaitAndAcquireGPUSwapchainTexture(commandBuffer, Window.Handle, out var swapchainTexture, out var w, out var h);
        startTime = Stopwatch.GetTimestamp();
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

        ImGuiController.Render(commandBuffer, swapchainTexture);
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

    public void Dispose() => ImGuiController.Quit();


    static unsafe class ImGuiController
    {
        public static void Initialize(GpuDevice device, Window window)
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
                ColorTargetFormat = (int) SDL.GetGPUSwapchainTextureFormat(device.Handle, window.Handle),
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
