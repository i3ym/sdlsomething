namespace SdlSomething;

public abstract class Viewport : GuiObject
{
    public GpuDevice Device => Renderer.Device;
    public Window Window => Renderer.Window;

    /// <summary> Add value to this <see cref="Node"/> and set it as <see cref="World"/> </summary>
    public RenderWorld WorldChild { set => World = Add(value); }

    protected Renderer Renderer { get; }
    public RenderWorld? World { get; set; }
    public Matrix4x4 CameraMatrix { get; set; } = Matrix4x4.Identity;
    public Vector4 ClearColor { get; set; } = new(0, 0, 0, 1);

    protected Viewport(Renderer renderer) => Renderer = renderer;

    protected override void PreInvalidateParent()
    {
        this.FindParentOfType<Window>()?.Renderer.RemoveViewport(this);
        base.PreInvalidateParent();
    }
    protected override void InvalidateParent()
    {
        this.FindParentOfType<Window>()?.Renderer.AddViewport(this);
        base.InvalidateParent();
    }

    protected abstract nint BeginRenderPass(nint commandBuffer, nint colorTexture);

    public void Render(nint commandBuffer, nint colorTexture)
    {
        var cameraMatrix = CameraMatrix * Matrix4x4.CreatePerspectiveFieldOfView(90 * (MathF.PI / 180), RealWidth / (float) RealHeight, .01f, 500f);
        SDL.PushGPUVertexUniformData(commandBuffer, 0, StructureToPointer(cameraMatrix), sizeof(float) * 4 * 4);

        World?.PrepareFrame(commandBuffer);

        var renderPass = BeginRenderPass(commandBuffer, colorTexture);
        World?.Render(commandBuffer, renderPass);

        SDL.EndGPURenderPass(renderPass);
    }
}

sealed class MainViewport : Viewport
{
    public MainViewport(Renderer renderer) : base(renderer)
    {
        RelativeWidth = 1;
        RelativeHeight = 1;
    }

    protected override nint BeginRenderPass(nint commandBuffer, nint colorTexture)
    {
        var colorTarget = new SDL.GPUColorTargetInfo()
        {
            Texture = colorTexture,
            ClearColor = new SDL.FColor(ClearColor.X, ClearColor.Y, ClearColor.Z, ClearColor.W),
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
    readonly nint ClearPipeline;

    public SubViewport(Renderer renderer) : base(renderer)
    {
        ClearPipeline = GraphicsPipeline.Create(renderer.Device, renderer.ColorTextureFormat, new(GraphicsPipeline.CompileShaders("flatcolor", renderer.Device))
        {
            Blending = true,
            Depth = false,
            BackfaceCulling = false,
        });
    }

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
        var viewport = new SDL.GPUViewport()
        {
            X = RealX,
            Y = RealY,
            W = RealWidth,
            H = RealHeight,
            MinDepth = 0,
            MaxDepth = 1,
        };
        SDL.SetGPUViewport(renderPass, viewport);

        SDL.BindGPUGraphicsPipeline(renderPass, ClearPipeline);
        SDL.PushGPUFragmentUniformData(commandBuffer, 0, StructureToPointer(ClearColor), USizeOf<Vector4>());
        SDL.DrawGPUPrimitives(renderPass, 3, 1, 0, 0);

        return renderPass;
    }
}
