global using System.Diagnostics.CodeAnalysis;
global using SDL3;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using SdlSomething;
using static SdlUtils;

if (!SDL.Init(SDL.InitFlags.Video))
{
    SDL.LogError(SDL.LogCategory.System, $"SDL could not initialize: {SDL.GetError()}");
    return;
}
var window = SDL.CreateWindow("WAAA", 2560, 1440, SDL.WindowFlags.Resizable);
var device = new GpuDevice();
if (!SDL.ClaimWindowForGPUDevice(device.Handle, window))
    throw new Exception(SDL.GetError());

var depthStencilFormat = GetStencilFormat(device);
uint w, h;
{
    SDL.GetWindowSizeInPixels(window, out var ww, out var hh);
    w = (uint) ww;
    h = (uint) hh;
}

var depthStencilTexture = createDepthTexture();
nint createDepthTexture()
{
    return SDL.CreateGPUTexture(
        device.Handle,
        new SDL.GPUTextureCreateInfo()
        {
            Type = SDL.GPUTextureType.TextureType2D,
            Width = w,
            Height = h,
            LayerCountOrDepth = 1,
            NumLevels = 1,
            SampleCount = SDL.GPUSampleCount.SampleCount1,
            Format = depthStencilFormat,
            Usage = SDL.GPUTextureUsageFlags.DepthStencilTarget,
        }
    );
}

var groups = new List<IRenderGroup>();


{
    const int count = 1000;
    var _cubeMesh = new Mesh<VertexPN>(device)
    {
        VerticesArr = [
            // bottom
            new(1, 0, 0, 0, -1, 0), // 0
            new(1, 0, 1, 0, -1, 0), // 1
            new(0, 0, 1, 0, -1, 0), // 2
            new(0, 0, 0, 0, -1, 0), // 3

            // top
            new(0, 1, 0, 0, 1, 0), // 4
            new(0, 1, 1, 0, 1, 0), // 5
            new(1, 1, 1, 0, 1, 0), // 6
            new(1, 1, 0, 0, 1, 0), // 7

            // left
            new(0, 0, 1, -1, 0, 0), // 8
            new(0, 1, 1, -1, 0, 0), // 9
            new(0, 1, 0, -1, 0, 0), // 10
            new(0, 0, 0, -1, 0, 0), // 11

            // right
            new(1, 0, 0, 1, 0, 0), // 14
            new(1, 1, 0, 1, 0, 0), // 12
            new(1, 1, 1, 1, 0, 0), // 13
            new(1, 0, 1, 1, 0, 0), // 15

            // front
            new(0, 0, 0, 0, 0, 1), // 16
            new(0, 1, 0, 0, 0, 1), // 17
            new(1, 1, 0, 0, 0, 1), // 18
            new(1, 0, 0, 0, 0, 1), // 19

            // back
            new(1, 0, 1, 0, 0, -1), // 20
            new(1, 1, 1, 0, 0, -1), // 21
            new(0, 1, 1, 0, 0, -1), // 22
            new(0, 0, 1, 0, 0, -1), // 23
        ],
        IndicesArr = [
            0, 1, 2, 0, 2, 3,
            4, 5, 6, 4, 6, 7,
            8, 9, 10, 8, 10, 11,
            12, 13, 14, 12, 14, 15,
            16, 17, 18, 16, 18, 19,
            20, 21, 22, 20, 22, 23,
        ],
    };
    var material = new StandardMaterial(device, window);
    var cubes = new RenderGroup<VertexPN, StandardMaterial.InstanceData>(_cubeMesh, material);
    groups.Add(cubes);

    var instances = Enumerable.Range(0, count)
        .SelectMany(x =>
            Enumerable.Range(0, count)
                .Select(y => new StandardMaterial.InstanceData(Matrix4x4.CreateTranslation(x + (x * .1f) - count / 2, -2, y + (y * .1f) - count / 2), new Vector4(MathF.Sin(y / 2f) / 2 + .5f, MathF.Cos(x / 3f) / 2 + .5f, 1, 1)))
        );
    cubes.SetRange(instances.ToArray());
}

var sunDir = new Vector3(-.5f, -1, -.67f);

var nt = DateTime.Now + TimeSpan.FromSeconds(1);
var f = 0;

var frame = 0L;
var loop = true;
while (loop)
{
    frame++;

    f++;
    if (DateTime.Now > nt)
    {
        Console.WriteLine(f);
        f = 0;
        nt = DateTime.Now + TimeSpan.FromSeconds(1);
    }

    while (SDL.PollEvent(out var e))
    {
        var type = (SDL.EventType) e.Type;

        if (type == SDL.EventType.Quit)
        {
            loop = false;
            break;
        }

        if (type == SDL.EventType.WindowResized)
        {
            w = (uint) e.Display.Data1;
            h = (uint) e.Display.Data2;

            SDL.Free(depthStencilTexture);
            depthStencilTexture = createDepthTexture();
        }
    }


    var cameraMatrix = Matrix4x4.CreateLookAt(new(MathF.Sin(frame / 130f) * 3 + .5f, 1, MathF.Cos(frame / 130f) * 3 + .5f), new(0, 0, 0), Vector3.UnitY)
        * Matrix4x4.CreatePerspectiveFieldOfView(90 * (MathF.PI / 180), (float) w / h, .01f, 100f);

    var commandBuffer = SDL.AcquireGPUCommandBuffer(device.Handle);
    foreach (var group in groups)
        group.PrepareFrame(commandBuffer);

    SDL.WaitAndAcquireGPUSwapchainTexture(commandBuffer, window, out var swapchainTexture, out w, out h);
    if (swapchainTexture == nint.Zero)
    {
        SDL.SubmitGPUCommandBuffer(commandBuffer);
        continue;
    }

    var colorTarget = new SDL.GPUColorTargetInfo()
    {
        Texture = swapchainTexture,
        ClearColor = new SDL.FColor(0 / 255f, 0 / 255f, 0 / 255f, 255 / 255f),
        LoadOp = SDL.GPULoadOp.Clear,
        StoreOp = SDL.GPUStoreOp.Store,
    };
    var stencil = new SDL.GPUDepthStencilTargetInfo()
    {
        Texture = depthStencilTexture,
        Cycle = 0,
        ClearDepth = 1,
        ClearStencil = 0,
        LoadOp = SDL.GPULoadOp.Clear,
        StoreOp = SDL.GPUStoreOp.DontCare,
        StencilLoadOp = SDL.GPULoadOp.Clear,
        StencilStoreOp = SDL.GPUStoreOp.DontCare,
    };
    var renderPass = SDL.BeginGPURenderPass(commandBuffer, StructureToPointer(colorTarget), 1, StructureToPointer(stencil));
    SDL.SetGPUStencilReference(renderPass, 0);
    SDL.PushGPUFragmentUniformData(commandBuffer, 0, StructureToPointer(in sunDir), sizeof(float) * 3);

    foreach (var group in groups)
    {
        group.BeginFrame(renderPass);
        group.GetBindings(out var vertb, out var indb, out var instab, out var indc, out var instl);
        SDL.BindGPUVertexBuffers(renderPass, 0, SpanToPointer([vertb, instab]), 2);
        SDL.BindGPUIndexBuffer(renderPass, indb, SDL.GPUIndexElementSize.IndexElementSize16Bit);
        SDL.PushGPUVertexUniformData(commandBuffer, 0, StructureToPointer(in cameraMatrix), sizeof(float) * 4 * 4);
        SDL.DrawGPUIndexedPrimitives(renderPass, indc, instl, 0, 0, 0);
    }

    SDL.EndGPURenderPass(renderPass);
    SDL.SubmitGPUCommandBuffer(commandBuffer);
}

SDL.DestroyGPUDevice(device.Handle);
SDL.DestroyWindow(window);
SDL.Quit();


readonly struct VertexPN
{
    public readonly float X, Y, Z;
    public readonly float NX, NY, NZ;

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

readonly struct MaterialPipelineProperties<T>
    where T : unmanaged
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
interface IMaterial : IDisposable
{
    void BeginFrame(nint renderPass);
}
abstract class Material<T> : IMaterial
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
sealed class StandardMaterial : Material<VertexPN>
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


    static unsafe SDL.GPUGraphicsPipelineCreateInfo BuildInfo(GpuDevice device, nint window)
    {
        var props = new MaterialPipelineProperties<VertexPN>()
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
        var info = props.CreatePipelineInfoPart(device, window);
        var dir = "/home/i3ym/workspace/Projects/_test/sdlsomething";
        info.VertexShader = LoadShader(device, Path.Combine(dir, "triangle.vert.spv"), SDL.GPUShaderStage.Vertex);
        info.FragmentShader = LoadShader(device, Path.Combine(dir, "triangle.frag.spv"), SDL.GPUShaderStage.Fragment);

        return info;
    }

    public StandardMaterial(GpuDevice device, nint window)
        : base(device, BuildInfo(device, window))
    {

    }
}

interface IMesh : IDisposable
{
    int IndicesCount { get; }

    void PrepareFrame(nint commandBuffer);
    void GetBindings(out SDL.GPUBufferBinding vertices, out SDL.GPUBufferBinding indices);
}
sealed class Mesh<T> : IMesh
    where T : unmanaged
{
    public GpuDevice Device => GpuVertices.Device;
    readonly ResizableGpuBuffer<T> GpuVertices;
    readonly ResizableGpuBuffer<Int16> GpuIndices;

    public int IndicesCount => ReadonlyIndices.Length;

    public T[] VerticesArr { set => WritableVertices = value; }
    public Int16[] IndicesArr { set => WritableIndices = value; }
    public ReadOnlyMemory<T> ReadonlyVertices => GpuVertices.ReadonlyData;
    public ReadOnlyMemory<Int16> ReadonlyIndices => GpuIndices.ReadonlyData;
    public ref Memory<T> WritableVertices => ref GpuVertices.WritableData;
    public ref Memory<Int16> WritableIndices => ref GpuIndices.WritableData;

    public Mesh(GpuDevice device)
    {
        GpuVertices = new(device, SDL.GPUBufferUsageFlags.Vertex);
        GpuIndices = new(device, SDL.GPUBufferUsageFlags.Index);
    }

    public void PrepareFrame(nint commandBuffer)
    {
        GpuVertices.PrepareFrame(commandBuffer);
        GpuIndices.PrepareFrame(commandBuffer);
    }
    public void GetBindings(out SDL.GPUBufferBinding vertices, out SDL.GPUBufferBinding indices)
    {
        GpuVertices.GetBinding(out vertices);
        GpuIndices.GetBinding(out indices);
    }

    public void Dispose()
    {
        GpuVertices.Dispose();
        GpuIndices.Dispose();
    }
}

interface IRenderGroup : IDisposable
{
    void PrepareFrame(nint commandBuffer);
    void BeginFrame(nint renderPass);
    void GetBindings(out SDL.GPUBufferBinding vertices, out SDL.GPUBufferBinding indices, out SDL.GPUBufferBinding instances, out uint indicesCount, out uint instanceCount);
}
sealed class RenderGroup<TVertex, TInstance> : IRenderGroup
    where TVertex : unmanaged
    where TInstance : unmanaged
{
    readonly Mesh<TVertex> Mesh;
    readonly Material<TVertex> Material;
    readonly ResizableGpuBuffer<TInstance> InstanceData;

    public RenderGroup(Mesh<TVertex> mesh, Material<TVertex> material)
    {
        Mesh = mesh;
        Material = material;
        InstanceData = new(mesh.Device, SDL.GPUBufferUsageFlags.Vertex);
    }

    public ref TInstance DataFor(int index) => ref InstanceData.WritableData.Span[index];
    public Span<TInstance> GetData() => InstanceData.WritableData.Span;
    public void SetRange(Memory<TInstance> instances) => InstanceData.WritableData = instances;

    public void PrepareFrame(nint commandBuffer)
    {
        Mesh.PrepareFrame(commandBuffer);
        InstanceData.PrepareFrame(commandBuffer);
    }
    public void BeginFrame(nint renderPass)
    {
        Material.BeginFrame(renderPass);
    }
    public void GetBindings(out SDL.GPUBufferBinding vertices, out SDL.GPUBufferBinding indices, out SDL.GPUBufferBinding instances, out uint indicesCount, out uint instanceCount)
    {
        Mesh.GetBindings(out vertices, out indices);
        InstanceData.GetBinding(out instances);
        indicesCount = (uint) Mesh.IndicesCount;
        instanceCount = (uint) InstanceData.Length;
    }

    public void Dispose() => InstanceData.Dispose();
}


static class SdlUtils
{
    public static unsafe nint StructureToPointer<T>(in T structure)
        where T : unmanaged =>
        (nint) Unsafe.AsPointer(in structure);

    public static unsafe nint SpanToPointer<T>(ReadOnlySpan<T> span)
        where T : unmanaged =>
        (nint) Unsafe.AsPointer(ref MemoryMarshal.GetReference(span));

    public static nint LoadShader(GpuDevice device, string path, SDL.GPUShaderStage stage)
    {
        var codeptr = SDL.LoadFile(path, out var codeSize);
        var info = new SDL.GPUShaderCreateInfo()
        {
            Code = codeptr,
            CodeSize = codeSize,
            Entrypoint = "main",
            Format = SDL.GPUShaderFormat.SPIRV,
            Stage = stage,
            NumSamplers = 0,
            NumStorageBuffers = 0,
            NumStorageTextures = 0,
            NumUniformBuffers = 1,
        };

        var shader = SDL.CreateGPUShader(device.Handle, info);
        SDL.Free(codeptr);

        return shader;
    }

    public static SDL.GPUTextureFormat GetStencilFormat(GpuDevice device)
    {
        ReadOnlySpan<SDL.GPUTextureFormat> depthFormats = [
            SDL.GPUTextureFormat.D24UnormS8Uint,
            SDL.GPUTextureFormat.D32FloatS8Uint,
        ];
        foreach (var format in depthFormats)
        {
            if (!SDL.GPUTextureSupportsFormat(device.Handle, format, SDL.GPUTextureType.TextureType2D, SDL.GPUTextureUsageFlags.DepthStencilTarget))
                continue;

            return format;
        }

        throw new NotSupportedException("Stencil formats not supported!");
    }
}
