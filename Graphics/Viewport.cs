namespace SdlSomething;

public abstract class Viewport
{
    public Renderer Renderer { get; }
    public RenderWorld World { get; set; }
    public Matrix4x4 CameraMatrix { get; set; } = Matrix4x4.Identity;
    public Vector4 ClearColor { get; set; } = new(0, 0, 0, 1);

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
        var cameraMatrix = CameraMatrix * Matrix4x4.CreatePerspectiveFieldOfView(90 * (MathF.PI / 180), RealWidth / RealHeight, .01f, 500f);
        SDL.PushGPUVertexUniformData(commandBuffer, 0, StructureToPointer(cameraMatrix), sizeof(float) * 4 * 4);

        World.PrepareFrame(commandBuffer);

        var renderPass = BeginRenderPass(commandBuffer, colorTexture);
        World.Render(commandBuffer, renderPass);

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
    protected override float RealWidth => Width;
    protected override float RealHeight => Height;

    SDL.GPUViewport Info = new() { W = 1, H = 1, MinDepth = 0, MaxDepth = 1 };
    public uint X { get => (uint) Info.X; set => Info.X = value; }
    public uint Y { get => (uint) Info.Y; set => Info.Y = value; }
    public uint Width { get => (uint) Info.W; set => Info.W = value; }
    public uint Height { get => (uint) Info.H; set => Info.H = value; }

    readonly nint ClearPipeline;

    public SubViewport(Renderer renderer, RenderWorld world) : base(renderer, world)
    {
        var info = CreateClearPipelineInfo(renderer.Device, renderer.Window);
        ClearPipeline = SDL.CreateGPUGraphicsPipeline(renderer.Device.Handle, info);
        SDL.ReleaseGPUShader(renderer.Device.Handle, info.VertexShader);
        SDL.ReleaseGPUShader(renderer.Device.Handle, info.FragmentShader);
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
        SDL.SetGPUViewport(renderPass, Info);

        SDL.BindGPUGraphicsPipeline(renderPass, ClearPipeline);
        SDL.PushGPUFragmentUniformData(commandBuffer, 0, StructureToPointer(ClearColor), USizeOf<Vector4>());
        SDL.DrawGPUPrimitives(renderPass, 3, 1, 0, 0);

        return renderPass;
    }

    static SDL.GPUGraphicsPipelineCreateInfo CreateClearPipelineInfo(GpuDevice device, Window window)
    {
        var vertexShader = CompileShader(device, SDL.GPUShaderStage.Vertex);
        var fragmentShader = CompileShader(device, SDL.GPUShaderStage.Fragment);

        return new SDL.GPUGraphicsPipelineCreateInfo()
        {
            PrimitiveType = SDL.GPUPrimitiveType.TriangleList,
            VertexShader = vertexShader,
            FragmentShader = fragmentShader,
            TargetInfo = new SDL.GPUGraphicsPipelineTargetInfo()
            {
                NumColorTargets = 1,
                ColorTargetDescriptions = SDL.StructureArrayToPointer([
                    new SDL.GPUColorTargetDescription()
                    {
                        Format = SDL.GetGPUSwapchainTextureFormat(device.Handle, window.Handle),
                        BlendState = new SDL.GPUColorTargetBlendState()
                        {
                            EnableBlend = true,
                            ColorWriteMask = SDL.GPUColorComponentFlags.R | SDL.GPUColorComponentFlags.G | SDL.GPUColorComponentFlags.B | SDL.GPUColorComponentFlags.A,

                            // (src * srcA) + (dst * (1 - srcA))
                            SrcColorBlendFactor = SDL.GPUBlendFactor.SrcAlpha,
                            DstColorBlendFactor = SDL.GPUBlendFactor.OneMinusSrcAlpha,
                            ColorBlendOp = SDL.GPUBlendOp.Add,

                            // outA = (srcA * 1) + (dstA * 0)
                            SrcAlphaBlendFactor = SDL.GPUBlendFactor.One,
                            DstAlphaBlendFactor = SDL.GPUBlendFactor.Zero,
                            AlphaBlendOp = SDL.GPUBlendOp.Add,
                        },
                    },
                ]),
            },
        };
    }
    static nint CompileShader(GpuDevice device, SDL.GPUShaderStage stage)
    {
        Console.WriteLine($"Compiling flatcolor shader: {stage}");

        var args = new List<string>()
        {
            "resources/shaders/flatcolor.slang",
            "-target", "spirv",
            "-matrix-layout-column-major",
        };

        var shader = Slangc.NET.SlangCompiler.Compile([.. args]);
        var info = new SDL.GPUShaderCreateInfo()
        {
            Code = StructureToPointer(in MemoryMarshal.GetReference(shader)),
            CodeSize = (nuint) shader.Length,
            Entrypoint = "main",
            Format = SDL.GPUShaderFormat.SPIRV,
            Stage = stage,
            NumSamplers = 0,
            NumStorageBuffers = 0,
            NumStorageTextures = 0,
            NumUniformBuffers = stage == SDL.GPUShaderStage.Vertex ? 0u : 1u,
        };

        return SDL.CreateGPUShader(device.Handle, info);
    }
}
