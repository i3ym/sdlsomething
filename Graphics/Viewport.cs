namespace SdlSomething;

public abstract class Viewport
{
    public Renderer Renderer { get; }
    public RenderWorld World { get; set; }
    public Matrix4x4 CameraMatrix { get; set; } = Matrix4x4.Identity;

    readonly nint ShadowSampler;

    protected Viewport(Renderer renderer, RenderWorld world)
    {
        Renderer = renderer;
        World = world;

        ShadowSampler = SDL.CreateGPUSampler(renderer.Device.Handle, new SDL.GPUSamplerCreateInfo()
        {
            MinFilter = SDL.GPUFilter.Linear,
            MagFilter = SDL.GPUFilter.Linear,
            AddressModeU = SDL.GPUSamplerAddressMode.ClampToEdge,
            AddressModeV = SDL.GPUSamplerAddressMode.ClampToEdge,
        });
    }

    protected abstract nint BeginRenderPass(nint commandBuffer, nint colorTexture);
    protected abstract nint BeginShadowPass(nint commandBuffer, nint shadowTexture);

    protected abstract float RealWidth { get; }
    protected abstract float RealHeight { get; }

    Matrix4x4 LightMatrixNew()
    {
        var sunMatrix = Matrix4x4.CreateLookTo(CameraMatrix.Translation - (World.SunDirection * 20), World.SunDirection, Vector3.UnitY);
        var lightMatrix = sunMatrix * Matrix4x4.CreateOrthographic(100, 100, 1, 100f);

        // sunMatrix = Matrix4x4.CreateLookTo(new Vector3(14, 10, 14), World.SunDirection, Vector3.UnitY);
        // lightMatrix = sunMatrix * Matrix4x4.CreatePerspectiveFieldOfView(140 * (MathF.PI / 180), 1, 1, 100f);

        return lightMatrix;
    }

    public void Render(nint commandBuffer, nint colorTexture, nint shadowTexture)
    {
        RenderShadow(commandBuffer, shadowTexture);

        var cameraMatrix = CameraMatrix * Matrix4x4.CreatePerspectiveFieldOfView(90 * (MathF.PI / 180), RealWidth / RealHeight, .01f, 500f);
        var lightMatrix = LightMatrixNew();
        SDL.PushGPUVertexUniformData(commandBuffer, 0, SpanToPointer([cameraMatrix, lightMatrix]), sizeof(float) * 4 * 4 * 2);

        World.PrepareFrame(commandBuffer);

        var renderPass = BeginRenderPass(commandBuffer, colorTexture);

        SDL.BindGPUFragmentSamplers(renderPass, 0, StructureToPointer(new SDL.GPUTextureSamplerBinding()
        {
            Texture = shadowTexture,
            Sampler = ShadowSampler,
        }), 1);

        World.Render(commandBuffer, renderPass);

        SDL.EndGPURenderPass(renderPass);
    }
    void RenderShadow(nint commandBuffer, nint shadowTexture)
    {
        var lightMatrix = LightMatrixNew();
        SDL.PushGPUVertexUniformData(commandBuffer, 0, StructureToPointer(lightMatrix), sizeof(float) * 4 * 4);

        World.PrepareFrame(commandBuffer);

        var renderPass = BeginShadowPass(commandBuffer, shadowTexture);
        SDL.BindGPUFragmentSamplers(renderPass, 0, StructureToPointer(new SDL.GPUTextureSamplerBinding()
        {
            Texture = shadowTexture,
            Sampler = ShadowSampler,
        }), 1);
        World.RenderShadow(commandBuffer, renderPass);

        SDL.EndGPURenderPass(renderPass);
    }
}

public sealed class MainViewport : Viewport
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
    protected override nint BeginShadowPass(nint commandBuffer, nint shadowTexture)
    {
        var stencil = new SDL.GPUDepthStencilTargetInfo()
        {
            Texture = shadowTexture,
            Cycle = 0,
            ClearDepth = 1,
            ClearStencil = 0,
            LoadOp = SDL.GPULoadOp.Clear,
            StoreOp = SDL.GPUStoreOp.Store,
            StencilLoadOp = SDL.GPULoadOp.Clear,
            StencilStoreOp = SDL.GPUStoreOp.Store,
        };

        return SDL.BeginGPURenderPass(commandBuffer, nint.Zero, 0, StructureToPointer(stencil));
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
    protected override nint BeginShadowPass(nint commandBuffer, nint shadowTexture)
    {
        var stencil = new SDL.GPUDepthStencilTargetInfo()
        {
            Texture = shadowTexture,
            Cycle = 0,
            ClearDepth = 1,
            ClearStencil = 0,
            LoadOp = SDL.GPULoadOp.Clear,
            StoreOp = SDL.GPUStoreOp.Store,
            StencilLoadOp = SDL.GPULoadOp.Clear,
            StencilStoreOp = SDL.GPUStoreOp.Store,
        };

        var renderPass = SDL.BeginGPURenderPass(commandBuffer, nint.Zero, 1, StructureToPointer(stencil));
        SDL.SetGPUViewport(renderPass, Info);

        return renderPass;
    }
}
