using Slangc.NET;

namespace SdlSomething;

public sealed class Standard3DRenderGroup : IRenderGroup
{
    public int InstancesCount => InstanceData?.InstanceCount ?? 1;
    readonly IStandard3DMesh Mesh;
    readonly Standard3DMaterial Material;
    readonly IStandard3DInstanceData? InstanceData;

    public Standard3DRenderGroup(IStandard3DMesh mesh, IStandard3DInstanceData? instanceData, Window window, Standard3DMaterialOptions? matOptions = null)
    {
        Mesh = mesh;
        InstanceData = instanceData;
        Material = new Standard3DMaterial(mesh.Device, window, mesh.ShaderOptions | (instanceData?.ShaderOptions ?? 0) | Standard3DShaderOptions.ReceiveShadow, matOptions);
    }

    public void PrepareFrame(nint commandBuffer)
    {
        Mesh.PrepareFrame(commandBuffer);
        InstanceData?.PrepareFrame(commandBuffer);
    }
    public void RenderFrame(nint renderPass)
    {
        if (InstancesCount == 0) return;

        Material.BeginFrame(renderPass);
        Mesh.RenderFrame(renderPass);
        InstanceData?.RenderFrame(renderPass);

        var indicesCount = Mesh.IndicesCount;
        if (indicesCount == 0) SDL.DrawGPUPrimitives(renderPass, (uint) Mesh.VerticesCount, (uint) InstancesCount, 0, 0);
        else SDL.DrawGPUIndexedPrimitives(renderPass, (uint) indicesCount, (uint) InstancesCount, 0, 0, 0);
    }
    public void RenderShadow(nint renderPass)
    {
        if (InstancesCount == 0) return;

        Material.BeginShadow(renderPass);
        Mesh.RenderFrame(renderPass);
        InstanceData?.RenderFrame(renderPass);

        var indicesCount = Mesh.IndicesCount;
        if (indicesCount == 0) SDL.DrawGPUPrimitives(renderPass, (uint) Mesh.VerticesCount, (uint) InstancesCount, 0, 0);
        else SDL.DrawGPUIndexedPrimitives(renderPass, (uint) indicesCount, (uint) InstancesCount, 0, 0, 0);
    }

    public void Dispose()
    {
        Material.Dispose();
        InstanceData?.Dispose();
    }
}

public interface IStandard3DInstanceData : IDisposable
{
    Standard3DShaderOptions ShaderOptions { get; }
    int InstanceCount { get; }

    void PrepareFrame(nint commandBuffer);
    void RenderFrame(nint renderPass);
}
public sealed class Standard3DInstanceDataTC : IStandard3DInstanceData
{
    public int InstanceCount => Transform.Length;
    public Standard3DShaderOptions ShaderOptions => Standard3DShaderOptions.InstanceTransform | Standard3DShaderOptions.InstanceColor;
    public ResizableGpuBuffer<Matrix4x4> Transform { get; }
    public ResizableGpuBuffer<Vector4> Color { get; }

    public Standard3DInstanceDataTC(GpuDevice device)
    {
        Transform = new(device, SDL.GPUBufferUsageFlags.Vertex);
        Color = new(device, SDL.GPUBufferUsageFlags.Vertex);
    }

    public void PrepareFrame(nint commandBuffer)
    {
        Transform.PrepareFrame(commandBuffer);
        Color.PrepareFrame(commandBuffer);
    }
    public void RenderFrame(nint renderPass)
    {
        var instat = Transform.GetBinding();
        SDL.BindGPUVertexBuffers(renderPass, 3, StructureToPointer(instat), 1);

        var instac = Color.GetBinding();
        SDL.BindGPUVertexBuffers(renderPass, 4, StructureToPointer(instac), 1);
    }

    public void Dispose()
    {
        Transform.Dispose();
        Color.Dispose();
    }
}
public sealed class Standard3DInstanceDataSingleT : IStandard3DInstanceData
{
    int IStandard3DInstanceData.InstanceCount => 1;
    public Standard3DShaderOptions ShaderOptions => Standard3DShaderOptions.InstanceTransform;
    public Matrix4x4 Transform
    {
        get => field;
        set
        {
            NeedsUpdate = true;
            field = value;
        }
    } = Matrix4x4.Identity;

    bool NeedsUpdate = true;
    readonly ResizableGpuBuffer<Matrix4x4> Buffer;

    public Standard3DInstanceDataSingleT(GpuDevice device, Matrix4x4 transform)
        : this(device) =>
        Transform = transform;

    public Standard3DInstanceDataSingleT(GpuDevice device) => Buffer = new(device, SDL.GPUBufferUsageFlags.Vertex);

    public void PrepareFrame(nint commandBuffer)
    {
        if (NeedsUpdate)
        {
            Buffer.GetWritableSpan(1)[0] = Transform;
            Buffer.PrepareFrame(commandBuffer);
        }
    }

    public void RenderFrame(nint renderPass)
    {
        var instat = Buffer.GetBinding();
        SDL.BindGPUVertexBuffers(renderPass, 3, StructureToPointer(instat), 1);
    }

    public void Dispose() => Buffer.Dispose();
}

public interface IStandard3DMesh : IDisposable
{
    Standard3DShaderOptions ShaderOptions { get; }

    GpuDevice Device { get; }
    int IndicesCount { get; }
    int VerticesCount { get; }

    void PrepareFrame(nint commandBuffer);
    void RenderFrame(nint renderPass);
}
public sealed class Standard3DMeshNCI : IStandard3DMesh
{
    Standard3DShaderOptions IStandard3DMesh.ShaderOptions => Standard3DShaderOptions.Normals | Standard3DShaderOptions.Color;

    public GpuDevice Device => Vertices.Device;
    public ResizableGpuBuffer<Vector3> Vertices { get; }
    public ResizableGpuBuffer<Vector3> Normals { get; }
    public ResizableGpuBuffer<Vector4> Colors { get; }
    public ResizableGpuBuffer<Int16> Indices { get; }

    public int IndicesCount => Indices.Length;
    public int VerticesCount => Vertices.Length;

    public Standard3DMeshNCI(GpuDevice device)
    {
        Vertices = new(device, SDL.GPUBufferUsageFlags.Vertex);
        Normals = new(device, SDL.GPUBufferUsageFlags.Vertex);
        Colors = new(device, SDL.GPUBufferUsageFlags.Vertex);
        Indices = new(device, SDL.GPUBufferUsageFlags.Index);
    }

    public void PrepareFrame(nint commandBuffer)
    {
        Vertices.PrepareFrame(commandBuffer);
        Normals.PrepareFrame(commandBuffer);
        Colors.PrepareFrame(commandBuffer);
        Indices.PrepareFrame(commandBuffer);
    }
    public void RenderFrame(nint renderPass)
    {
        var bindings = (ReadOnlySpan<SDL.GPUBufferBinding>) [
            Vertices.GetBinding(),
            Normals.GetBinding(),
            Colors.GetBinding(),
        ];
        SDL.BindGPUVertexBuffers(renderPass, 0, SpanToPointer(bindings), (uint) bindings.Length);

        SDL.BindGPUIndexBuffer(renderPass, Indices.GetBinding(), SDL.GPUIndexElementSize.IndexElementSize16Bit);
    }

    public void Dispose()
    {
        Vertices.Dispose();
        Normals.Dispose();
        Colors.Dispose();
        Indices.Dispose();
    }
}
public sealed class Standard3DMeshC : IStandard3DMesh
{
    Standard3DShaderOptions IStandard3DMesh.ShaderOptions => Standard3DShaderOptions.Color;

    public GpuDevice Device => Vertices.Device;
    public ResizableGpuBuffer<Vector3> Vertices { get; }
    public ResizableGpuBuffer<Vector4> Colors { get; }

    int IStandard3DMesh.IndicesCount => 0;
    public int VerticesCount => Vertices.Length;

    public Standard3DMeshC(GpuDevice device)
    {
        Vertices = new(device, SDL.GPUBufferUsageFlags.Vertex);
        Colors = new(device, SDL.GPUBufferUsageFlags.Vertex);
    }

    public void PrepareFrame(nint commandBuffer)
    {
        Vertices.PrepareFrame(commandBuffer);
        Colors.PrepareFrame(commandBuffer);
    }
    public void RenderFrame(nint renderPass)
    {
        SDL.BindGPUVertexBuffers(renderPass, 0, StructureToPointer(Vertices.GetBinding()), 1);
        SDL.BindGPUVertexBuffers(renderPass, 2, StructureToPointer(Colors.GetBinding()), 1);
    }

    public void Dispose()
    {
        Vertices.Dispose();
        Colors.Dispose();
    }
}

[Flags]
public enum Standard3DShaderOptions
{
    None = 0,

    Normals = 1 << 0,
    Color = 1 << 1,
    InstanceTransform = 1 << 2,
    InstanceColor = 1 << 3,
    ReceiveShadow = 1 << 4,

    All = Normals | Color | InstanceTransform | InstanceColor | ReceiveShadow,
}
public sealed class Standard3DMaterialOptions
{
    public SDL.GPUPrimitiveType PrimitiveType { get; init; } = SDL.GPUPrimitiveType.TriangleList;
}
public sealed class Standard3DMaterial
{
    readonly GpuDevice Device;
    readonly nint GraphicsPipeline;
    readonly nint ShadowPipeline;

    public Standard3DMaterial(GpuDevice device, Window window, Standard3DShaderOptions shaderOptions, Standard3DMaterialOptions? matOptions = null)
    {
        Device = device;

        var info = CreatePipelineInfo(device, window, shaderOptions, matOptions ?? new());
        GraphicsPipeline = SDL.CreateGPUGraphicsPipeline(device.Handle, info);
        SDL.ReleaseGPUShader(device.Handle, info.VertexShader);
        SDL.ReleaseGPUShader(device.Handle, info.FragmentShader);

        info = CreateShadowPipelineInfo(device, window, shaderOptions, matOptions ?? new());
        ShadowPipeline = SDL.CreateGPUGraphicsPipeline(device.Handle, info);
        SDL.ReleaseGPUShader(device.Handle, info.VertexShader);
    }

    public void BeginShadow(nint renderPass) => SDL.BindGPUGraphicsPipeline(renderPass, ShadowPipeline);
    public void BeginFrame(nint renderPass) => SDL.BindGPUGraphicsPipeline(renderPass, GraphicsPipeline);
    public void Dispose() => SDL.ReleaseGPUGraphicsPipeline(Device.Handle, GraphicsPipeline);

    static SDL.GPUGraphicsPipelineCreateInfo CreatePipelineInfo(GpuDevice device, Window window, Standard3DShaderOptions shaderOptions, Standard3DMaterialOptions matOptions)
    {
        var depthStencilFormat = GetStencilFormat(device);
        var backfaceCulling = true;
        var primitiveType = matOptions.PrimitiveType;

        var vertexDescriptions = new List<SDL.GPUVertexBufferDescription>();
        var vertexAttributes = new List<SDL.GPUVertexAttribute>();

        void addVertInput<T>(uint slot, SDL.GPUVertexElementFormat format)
            where T : unmanaged
        {
            vertexDescriptions.Add(new SDL.GPUVertexBufferDescription()
            {
                Slot = slot,
                InputRate = SDL.GPUVertexInputRate.Vertex,
                Pitch = USizeOf<T>(),
            });
            vertexAttributes.Add(new() { BufferSlot = slot, Location = slot, Format = format, });
        }

        // position
        addVertInput<Vector3>(0, SDL.GPUVertexElementFormat.Float3);

        if (shaderOptions.HasFlag(Standard3DShaderOptions.Normals))
            addVertInput<Vector3>(1, SDL.GPUVertexElementFormat.Float3);
        if (shaderOptions.HasFlag(Standard3DShaderOptions.Color))
            addVertInput<Vector4>(2, SDL.GPUVertexElementFormat.Float4);

        if (shaderOptions.HasFlag(Standard3DShaderOptions.InstanceTransform))
        {
            vertexDescriptions.Add(new SDL.GPUVertexBufferDescription()
            {
                Slot = 3,
                InputRate = SDL.GPUVertexInputRate.Instance,
                Pitch = USizeOf<Matrix4x4>(),
            });
            vertexAttributes.Add(new() { BufferSlot = 3, Location = 3, Format = SDL.GPUVertexElementFormat.Float4, Offset = sizeof(float) * 4 * 0 });
            vertexAttributes.Add(new() { BufferSlot = 3, Location = 4, Format = SDL.GPUVertexElementFormat.Float4, Offset = sizeof(float) * 4 * 1 });
            vertexAttributes.Add(new() { BufferSlot = 3, Location = 5, Format = SDL.GPUVertexElementFormat.Float4, Offset = sizeof(float) * 4 * 2 });
            vertexAttributes.Add(new() { BufferSlot = 3, Location = 6, Format = SDL.GPUVertexElementFormat.Float4, Offset = sizeof(float) * 4 * 3 });
        }
        if (shaderOptions.HasFlag(Standard3DShaderOptions.InstanceColor))
        {
            vertexDescriptions.Add(new SDL.GPUVertexBufferDescription()
            {
                Slot = 4,
                InputRate = SDL.GPUVertexInputRate.Instance,
                Pitch = USizeOf<Vector4>(),
            });
            vertexAttributes.Add(new() { BufferSlot = 4, Location = 7, Format = SDL.GPUVertexElementFormat.Float4 });
        }

        var vertexShader = Compile3DShader(device, SDL.GPUShaderStage.Vertex, shaderOptions);
        var fragmentShader = Compile3DShader(device, SDL.GPUShaderStage.Fragment, shaderOptions);

        return new SDL.GPUGraphicsPipelineCreateInfo()
        {
            PrimitiveType = primitiveType,
            VertexShader = vertexShader,
            FragmentShader = fragmentShader,
            TargetInfo = new SDL.GPUGraphicsPipelineTargetInfo()
            {
                HasDepthStencilTarget = true,
                DepthStencilFormat = depthStencilFormat,

                NumColorTargets = 1,
                ColorTargetDescriptions = SDL.StructureArrayToPointer([
                    new SDL.GPUColorTargetDescription()
                    {
                        Format = SDL.GetGPUSwapchainTextureFormat(device.Handle, window.Handle),
                    },
                ]),
            },
            DepthStencilState = new SDL.GPUDepthStencilState()
            {
                EnableDepthTest = true,
                EnableDepthWrite = true,
                CompareOp = SDL.GPUCompareOp.Less,
                WriteMask = 0xFF,
            },
            RasterizerState = new SDL.GPURasterizerState()
            {
                CullMode = backfaceCulling ? SDL.GPUCullMode.Back : SDL.GPUCullMode.None,
            },
            VertexInputState = new SDL.GPUVertexInputState()
            {
                NumVertexBuffers = (uint) vertexDescriptions.Count,
                VertexBufferDescriptions = SpanToPointer(vertexDescriptions.ToArray()),
                NumVertexAttributes = (uint) vertexAttributes.Count,
                VertexAttributes = SpanToPointer(vertexAttributes.ToArray()),
            },
        };
    }
    static SDL.GPUGraphicsPipelineCreateInfo CreateShadowPipelineInfo(GpuDevice device, Window window, Standard3DShaderOptions shaderOptions, Standard3DMaterialOptions matOptions)
    {
        var depthStencilFormat = GetStencilFormat(device);
        var backfaceCulling = true;
        var primitiveType = matOptions.PrimitiveType;

        var vertexShader = Compile3DShader(device, SDL.GPUShaderStage.Vertex, shaderOptions & Standard3DShaderOptions.InstanceTransform);
        var fragmentShader = CompileEmptyFragShader(device);

        return new SDL.GPUGraphicsPipelineCreateInfo()
        {
            PrimitiveType = primitiveType,
            VertexShader = vertexShader,
            FragmentShader = fragmentShader,
            TargetInfo = new SDL.GPUGraphicsPipelineTargetInfo()
            {
                HasDepthStencilTarget = true,
                DepthStencilFormat = depthStencilFormat,
            },
            DepthStencilState = new SDL.GPUDepthStencilState()
            {
                EnableDepthTest = true,
                EnableDepthWrite = true,
                CompareOp = SDL.GPUCompareOp.Less,
                WriteMask = 0xFF,
            },
            RasterizerState = new SDL.GPURasterizerState()
            {
                CullMode = backfaceCulling ? SDL.GPUCullMode.Back : SDL.GPUCullMode.None,
            },
        };
    }
    static nint Compile3DShader(GpuDevice device, SDL.GPUShaderStage stage, Standard3DShaderOptions options)
    {
        Console.WriteLine($"Compiling standard3d shader: {stage} ({options})");

        var args = new List<string>()
        {
            "resources/shaders/3d.slang",
            "-target", "spirv",
            "-matrix-layout-column-major",
        };

        if ((options & Standard3DShaderOptions.Normals) != 0)
            args.Add("-DHAS_NORMAL");
        if ((options & Standard3DShaderOptions.Color) != 0)
            args.Add("-DHAS_COLOR");
        if ((options & Standard3DShaderOptions.InstanceTransform) != 0)
            args.Add("-DHAS_INSTANCE_TRANSFORM");
        if ((options & Standard3DShaderOptions.InstanceColor) != 0)
            args.Add("-DHAS_INSTANCE_COLOR");
        if ((options & Standard3DShaderOptions.ReceiveShadow) != 0)
            args.Add("-DRECEIVE_SHADOW");

        if ((options & (Standard3DShaderOptions.Color | Standard3DShaderOptions.InstanceColor)) != 0)
            args.Add("-DHAS_ANY_COLOR");

        var shader = SlangCompiler.Compile([.. args]);
        var info = new SDL.GPUShaderCreateInfo()
        {
            Code = StructureToPointer(in MemoryMarshal.GetReference(shader)),
            CodeSize = (nuint) shader.Length,
            Entrypoint = "main",
            Format = SDL.GPUShaderFormat.SPIRV,
            Stage = stage,
            NumSamplers = stage == SDL.GPUShaderStage.Fragment ? 1u : 0u,
            NumStorageBuffers = 0,
            NumStorageTextures = 0,
            NumUniformBuffers = 1,
        };

        return SDL.CreateGPUShader(device.Handle, info);
    }
    static nint CompileEmptyFragShader(GpuDevice device)
    {
        Console.WriteLine($"Compiling empty frag shader: {SDL.GPUShaderStage.Fragment}");

        var args = new List<string>()
        {
            "resources/shaders/empty.frag.slang",
            "-target", "spirv",
            "-matrix-layout-column-major",
        };

        var shader = SlangCompiler.Compile([.. args]);
        var info = new SDL.GPUShaderCreateInfo()
        {
            Code = StructureToPointer(in MemoryMarshal.GetReference(shader)),
            CodeSize = (nuint) shader.Length,
            Entrypoint = "main",
            Format = SDL.GPUShaderFormat.SPIRV,
            Stage = SDL.GPUShaderStage.Fragment,
            NumSamplers = 0,
            NumStorageBuffers = 0,
            NumStorageTextures = 0,
            NumUniformBuffers = 1,
        };

        return SDL.CreateGPUShader(device.Handle, info);
    }
}
