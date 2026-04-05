namespace SdlSomething.Graphics;

public abstract class Viewport
{
    public Renderer Renderer { get; }
    public RenderWorld World { get; set; }
    public Matrix4x4 CameraMatrix { get; set; } = Matrix4x4.Identity;

    protected Viewport(Renderer renderer, RenderWorld world)
    {
        Renderer = renderer;
        World = world;
    }

    protected abstract nint BeginRenderPass(nint commandBuffer, nint colorTexture);

    protected abstract float RealWidth { get; }
    protected abstract float RealHeight { get; }

    public void Render(nint commandBuffer, nint colorTexture)
    {
        var cameraMatrix = CameraMatrix * Matrix4x4.CreatePerspectiveFieldOfView(90 * (MathF.PI / 180), RealWidth / RealHeight, .01f, 100f);
        SDL.PushGPUVertexUniformData(commandBuffer, 0, StructureToPointer(cameraMatrix), sizeof(float) * 4 * 4);

        World.PrepareFrame(commandBuffer);

        var renderPass = BeginRenderPass(commandBuffer, colorTexture);
        World.Render(commandBuffer, renderPass);

        SDL.EndGPURenderPass(renderPass);
    }
}

sealed class MainViewport : Viewport
{
    protected override float RealWidth => Renderer.WindowWidth;
    protected override float RealHeight => Renderer.WindowHeight;

    public MainViewport(Renderer renderer, RenderWorld world) : base(renderer, world) { }

    protected override nint BeginRenderPass(nint commandBuffer, nint colorTexture)
    {
        var colorTarget = new SDL.GPUColorTargetInfo()
        {
            Texture = colorTexture,
            ClearColor = new SDL.FColor(0 / 255f, 0 / 255f, 0 / 255f, 255 / 255f),
            LoadOp = SDL.GPULoadOp.Clear,
            StoreOp = SDL.GPUStoreOp.Store,
        };
        var stencil = new SDL.GPUDepthStencilTargetInfo()
        {
            Texture = Renderer.MainDepthStencilTexture,
            Cycle = 0,
            ClearDepth = 1,
            ClearStencil = 0,
            LoadOp = SDL.GPULoadOp.Clear,
            StoreOp = SDL.GPUStoreOp.DontCare,
            StencilLoadOp = SDL.GPULoadOp.Clear,
            StencilStoreOp = SDL.GPUStoreOp.DontCare,
        };

        return SDL.BeginGPURenderPass(commandBuffer, StructureToPointer(colorTarget), 1, StructureToPointer(stencil));
    }
}
public sealed class SubViewport : Viewport
{
    protected override float RealWidth => Width;
    protected override float RealHeight => Height;

    SDL.GPUViewport Info = new() { W = 1, H = 1, MinDepth = 0, MaxDepth = 1 };
    public uint X { get => (uint) Info.X; set => Info.X = value; }
    public uint Y { get => (uint) Info.Y; set => Info.Y = value; }
    public uint Width { get => (uint) Info.W; set => Info.W = value; }
    public uint Height { get => (uint) Info.H; set => Info.H = value; }

    public SubViewport(Renderer renderer, RenderWorld world) : base(renderer, world) { }

    protected override nint BeginRenderPass(nint commandBuffer, nint colorTexture)
    {
        var colorTarget = new SDL.GPUColorTargetInfo()
        {
            Texture = colorTexture,
            LoadOp = SDL.GPULoadOp.Load,
            StoreOp = SDL.GPUStoreOp.Store,
        };
        var stencil = new SDL.GPUDepthStencilTargetInfo()
        {
            Texture = Renderer.MainDepthStencilTexture,
            Cycle = 0,
            ClearDepth = 1,
            ClearStencil = 0,
            LoadOp = SDL.GPULoadOp.Clear,
            StoreOp = SDL.GPUStoreOp.DontCare,
            StencilLoadOp = SDL.GPULoadOp.Clear,
            StencilStoreOp = SDL.GPUStoreOp.DontCare,
        };

        var renderPass = SDL.BeginGPURenderPass(commandBuffer, StructureToPointer(colorTarget), 1, StructureToPointer(stencil));
        SDL.SetGPUViewport(renderPass, Info);

        return renderPass;
    }
}
