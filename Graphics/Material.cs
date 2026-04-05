namespace SdlSomething.Graphics;

public readonly struct MaterialPipelineProperties
{
    public readonly record struct VertexBufferInfo(VertexDescription Description, VertexAttribute[] Attributes);
    public readonly record struct VertexDescription(SDL.GPUVertexInputRate InputRate, uint Pitch);
    public readonly record struct VertexAttribute(SDL.GPUVertexElementFormat Format, uint Offset);

    public SDL.GPUTextureFormat? DepthStencilFormat { get; init; } = null;
    public bool BackfaceCulling { get; init; } = true;
    public SDL.GPUPrimitiveType PrimitiveType { get; init; } = SDL.GPUPrimitiveType.TriangleList;
    public required VertexBufferInfo[] VertexBuffers { get; init; }

    public MaterialPipelineProperties() { }

    public SDL.GPUGraphicsPipelineCreateInfo CreatePipelineInfoPart(GpuDevice device, nint window)
    {
        var vertexDescriptions = new List<SDL.GPUVertexBufferDescription>();
        var vertexAttributes = new List<SDL.GPUVertexAttribute>();
        var loc = 0U;
        for (var i = 0U; i < VertexBuffers.Length; i++)
        {
            var vb = VertexBuffers[i];
            vertexDescriptions.Add(new SDL.GPUVertexBufferDescription()
            {
                Slot = i,
                InputRate = vb.Description.InputRate,
                Pitch = vb.Description.Pitch,
            });

            foreach (var att in vb.Attributes)
            {
                var attribute = new SDL.GPUVertexAttribute()
                {
                    BufferSlot = i,
                    Location = loc++,
                    Format = att.Format,
                    Offset = att.Offset,
                };
                vertexAttributes.Add(attribute);
            }
        }

        return new SDL.GPUGraphicsPipelineCreateInfo()
        {
            PrimitiveType = PrimitiveType,
            TargetInfo = new SDL.GPUGraphicsPipelineTargetInfo()
            {
                HasDepthStencilTarget = DepthStencilFormat is not null,
                DepthStencilFormat = DepthStencilFormat.GetValueOrDefault(),

                NumColorTargets = 1,
                ColorTargetDescriptions = SDL.StructureArrayToPointer([
                    new SDL.GPUColorTargetDescription()
                    {
                        Format = SDL.GetGPUSwapchainTextureFormat(device.Handle, window),
                    },
                ]),
            },
            DepthStencilState = new SDL.GPUDepthStencilState()
            {
                EnableDepthTest = DepthStencilFormat is not null,
                EnableDepthWrite = true,
                CompareOp = SDL.GPUCompareOp.Less,
                WriteMask = 0xFF,
            },
            RasterizerState = new SDL.GPURasterizerState()
            {
                CullMode = BackfaceCulling ? SDL.GPUCullMode.Back : SDL.GPUCullMode.None,
            },
            VertexInputState = new SDL.GPUVertexInputState()
            {
                NumVertexBuffers = (uint) vertexDescriptions.Count,
                VertexBufferDescriptions = SDL.StructureArrayToPointer(vertexDescriptions.ToArray()),
                NumVertexAttributes = (uint) vertexAttributes.Count,
                VertexAttributes = SDL.StructureArrayToPointer(vertexAttributes.ToArray()),
            },
        };
    }
}
public interface IMaterial : IDisposable
{
    void BeginFrame(nint renderPass);
}
public abstract class Material<T> : IMaterial
    where T : unmanaged
{
    readonly GpuDevice Device;
    readonly nint GraphicsPipeline;

    protected Material(GpuDevice device, SDL.GPUGraphicsPipelineCreateInfo info)
    {
        Device = device;
        GraphicsPipeline = SDL.CreateGPUGraphicsPipeline(device.Handle, info);

        if (info.VertexShader != nint.Zero)
            SDL.ReleaseGPUShader(device.Handle, info.VertexShader);
        if (info.FragmentShader != nint.Zero)
            SDL.ReleaseGPUShader(device.Handle, info.FragmentShader);
    }

    public void BeginFrame(nint renderPass)
    {
        SDL.BindGPUGraphicsPipeline(renderPass, GraphicsPipeline);
    }

    public void Dispose() => SDL.ReleaseGPUGraphicsPipeline(Device.Handle, GraphicsPipeline);
}

public readonly struct VertexPN
{
    public readonly float X, Y, Z;
    public readonly float NX, NY, NZ;

    public VertexPN(float x, float y, float z)
    {
        X = x;
        Y = y;
        Z = z;
    }
    public VertexPN(float x, float y, float z, float nx, float ny, float nz)
    {
        X = x;
        Y = y;
        Z = z;
        NX = nx;
        NY = ny;
        NZ = nz;
    }
}
public sealed class StandardMaterial : Material<VertexPN>
{
    public readonly struct InstanceData
    {
        public readonly Matrix4x4 Matrix;
        public readonly Vector4 Albedo;

        public InstanceData(Matrix4x4 matrix, Vector4 albedo)
        {
            Matrix = matrix;
            Albedo = albedo;
        }
    }

    static unsafe SDL.GPUGraphicsPipelineCreateInfo BuildInfo(GpuDevice device, Window window)
    {
        var props = new MaterialPipelineProperties()
        {
            DepthStencilFormat = GetStencilFormat(device),
            VertexBuffers = [
                new(
                    new(SDL.GPUVertexInputRate.Vertex, (uint) sizeof(VertexPN)),
                    [
                        new(SDL.GPUVertexElementFormat.Float3, sizeof(float) * 0),
                        new(SDL.GPUVertexElementFormat.Float3, sizeof(float) * 3),
                    ]
                ),
                new(
                    new(SDL.GPUVertexInputRate.Instance, (uint) sizeof(InstanceData)),
                    [
                        new(SDL.GPUVertexElementFormat.Float4, sizeof(float) * 4 * 0),
                        new(SDL.GPUVertexElementFormat.Float4, sizeof(float) * 4 * 1),
                        new(SDL.GPUVertexElementFormat.Float4, sizeof(float) * 4 * 2),
                        new(SDL.GPUVertexElementFormat.Float4, sizeof(float) * 4 * 3),
                        new(SDL.GPUVertexElementFormat.Float4, sizeof(float) * 4 * 4), // albedo
                    ]
                ),
            ],
        };
        var info = props.CreatePipelineInfoPart(device, window.Handle);
        var dir = "/home/i3ym/workspace/Projects/_test/sdlsomething";
        info.VertexShader = LoadShader(device, Path.Combine(dir, "triangle.vert.spv"), SDL.GPUShaderStage.Vertex);
        info.FragmentShader = LoadShader(device, Path.Combine(dir, "triangle.frag.spv"), SDL.GPUShaderStage.Fragment);

        return info;
    }

    public StandardMaterial(GpuDevice device, Window window)
        : base(device, BuildInfo(device, window)) { }
}
