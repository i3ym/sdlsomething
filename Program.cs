global using System.Diagnostics.CodeAnalysis;
global using SDL3;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using SdlSomething;

if (!SDL.Init(SDL.InitFlags.Video))
{
    SDL.LogError(SDL.LogCategory.System, $"SDL could not initialize: {SDL.GetError()}");
    return;
}
var window = SDL.CreateWindow("SDL3 Create Window", 2560, 1440, SDL.WindowFlags.Resizable);
var device = new GpuDevice();
if (!SDL.ClaimWindowForGPUDevice(device.Handle, window))
    throw new Exception(SDL.GetError());



using var vb = new GpuBuffer<Vertex>(device, 4, SDL.GPUBufferUsageFlags.Vertex);
using var vtb = new GpuTransferBuffer<Vertex>(vb);

using var ib = new GpuBuffer<Int16>(device, 6, SDL.GPUBufferUsageFlags.Index);
using var itb = new GpuTransferBuffer<Int16>(ib);

static nint loadShader(GpuDevice device, string path, SDL.GPUShaderStage stage)
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
var vertexShader = loadShader(device, "/home/i3ym/workspace/Projects/_test/sdlsomething/triangle.vert.spv", SDL.GPUShaderStage.Vertex);
var fragmentShader = loadShader(device, "/home/i3ym/workspace/Projects/_test/sdlsomething/triangle.frag.spv", SDL.GPUShaderStage.Fragment);

SDL.GPUTextureFormat depthStencilFormat = default;
{
    ReadOnlySpan<SDL.GPUTextureFormat> depthFormats = [
        SDL.GPUTextureFormat.D24UnormS8Uint,
        SDL.GPUTextureFormat.D32FloatS8Uint,
    ];
    foreach (var format in depthFormats)
    {
        if (!SDL.GPUTextureSupportsFormat(device.Handle, format, SDL.GPUTextureType.TextureType2D, SDL.GPUTextureUsageFlags.DepthStencilTarget))
            continue;

        depthStencilFormat = format;
        break;
    }
    if (depthStencilFormat == default)
        throw new NotSupportedException("Stencil formats not supported!");
}

var pipelineInfo = new SDL.GPUGraphicsPipelineCreateInfo()
{
    VertexShader = vertexShader,
    FragmentShader = fragmentShader,
    PrimitiveType = SDL.GPUPrimitiveType.TriangleList,
    TargetInfo = new SDL.GPUGraphicsPipelineTargetInfo()
    {
        HasDepthStencilTarget = true,
        DepthStencilFormat = depthStencilFormat,

        NumColorTargets = 1,
        ColorTargetDescriptions = SpanToPointer([
            new SDL.GPUColorTargetDescription()
            {
                Format = SDL.GetGPUSwapchainTextureFormat(device.Handle, window),
            },
        ]),
    },
    DepthStencilState = new SDL.GPUDepthStencilState
    {
        EnableDepthTest = true,
        EnableDepthWrite = true,
        CompareOp = SDL.GPUCompareOp.Less,
        WriteMask = 0xFF,
    },
    RasterizerState = new()
    {
        CullMode = SDL.GPUCullMode.Back,
    },

    VertexInputState = new()
    {
        NumVertexBuffers = 2,
        VertexBufferDescriptions = SpanToPointer([
            new SDL.GPUVertexBufferDescription()
            {
                Slot = 0,
                InputRate = SDL.GPUVertexInputRate.Vertex,
                Pitch = (uint) Unsafe.SizeOf<Vertex>(),
            },
            new SDL.GPUVertexBufferDescription()
            {
                Slot = 1,
                InputRate = SDL.GPUVertexInputRate.Instance,
                Pitch = (uint) Unsafe.SizeOf<Matrix4x4>(),
            },
        ]),

        NumVertexAttributes = 3 + 4,
        VertexAttributes = SpanToPointer([
            new SDL.GPUVertexAttribute()
            {
                BufferSlot = 0,
                Location = 0,
                Format = SDL.GPUVertexElementFormat.Float3,
                Offset = 0,
            },
            new SDL.GPUVertexAttribute()
            {
                BufferSlot = 0,
                Location = 1,
                Format = SDL.GPUVertexElementFormat.Float3,
                Offset = sizeof(float) * 3,
            },
            new SDL.GPUVertexAttribute()
            {
                BufferSlot = 0,
                Location = 2,
                Format = SDL.GPUVertexElementFormat.Float4,
                Offset = sizeof(float) * 3 + sizeof(float) * 3,
            },

            new SDL.GPUVertexAttribute()
            {
                BufferSlot = 1,
                Location = 3,
                Format = SDL.GPUVertexElementFormat.Float4,
                Offset = (uint) sizeof(float) * 4 * 0,
            },
            new SDL.GPUVertexAttribute()
            {
                BufferSlot = 1,
                Location = 4,
                Format = SDL.GPUVertexElementFormat.Float4,
                Offset = (uint) sizeof(float) * 4 * 1,
            },
            new SDL.GPUVertexAttribute()
            {
                BufferSlot = 1,
                Location = 5,
                Format = SDL.GPUVertexElementFormat.Float4,
                Offset = (uint) sizeof(float) * 4 * 2,
            },
            new SDL.GPUVertexAttribute()
            {
                BufferSlot = 1,
                Location = 6,
                Format = SDL.GPUVertexElementFormat.Float4,
                Offset = (uint) sizeof(float) * 4 * 3,
            },
        ]),
    },
};
var graphicsPipeline = SDL.CreateGPUGraphicsPipeline(device.Handle, pipelineInfo);
SDL.ReleaseGPUShader(device.Handle, vertexShader);
SDL.ReleaseGPUShader(device.Handle, fragmentShader);


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

var cubeMesh = new Mesh()
{
    VerticesArr = [
        // bottom
        new(0, 0, 0, 0, -1, 0, 1, 1, 1, 1), // 0
        new(0, 0, 1, 0, -1, 0, 1, 1, 1, 1), // 1
        new(1, 0, 1, 0, -1, 0, 1, 1, 1, 1), // 2
        new(1, 0, 0, 0, -1, 0, 1, 1, 1, 1), // 3

        // top
        new(0, 1, 0, 0, 1, 0, 1, 1, 1, 1), // 4
        new(0, 1, 1, 0, 1, 0, 1, 1, 1, 1), // 5
        new(1, 1, 1, 0, 1, 0, 1, 1, 1, 1), // 6
        new(1, 1, 0, 0, 1, 0, 1, 1, 1, 1), // 7

        // left
        new(0, 0, 1, -1, 0, 0, 1, 1, 1, 1), // 8
        new(0, 1, 1, -1, 0, 0, 1, 1, 1, 1), // 9
        new(0, 1, 0, -1, 0, 0, 1, 1, 1, 1), // 10
        new(0, 0, 0, -1, 0, 0, 1, 1, 1, 1), // 11

        // right
        new(1, 0, 0, 1, 0, 0, 1, 1, 1, 1), // 14
        new(1, 1, 0, 1, 0, 0, 1, 1, 1, 1), // 12
        new(1, 1, 1, 1, 0, 0, 1, 1, 1, 1), // 13
        new(1, 0, 1, 1, 0, 0, 1, 1, 1, 1), // 15

        // front
        new(0, 0, 0, 0, 0, 1, 1, 1, 1, 1), // 16
        new(0, 1, 0, 0, 0, 1, 1, 1, 1, 1), // 17
        new(1, 1, 0, 0, 0, 1, 1, 1, 1, 1), // 18
        new(1, 0, 0, 0, 0, 1, 1, 1, 1, 1), // 19

        // back
        new(1, 0, 1, 0, 0, -1, 1, 1, 1, 1), // 20
        new(1, 1, 1, 0, 0, -1, 1, 1, 1, 1), // 21
        new(0, 1, 1, 0, 0, -1, 1, 1, 1, 1), // 22
        new(0, 0, 1, 0, 0, -1, 1, 1, 1, 1), // 23
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

const int count = 1000;
var instances = Enumerable.Range(0, count)
    .SelectMany(x =>
        Enumerable.Range(0, count)
            .Select(y => Matrix4x4.CreateTranslation(x + (x * .1f) - count / 2, -2, y + (y * .1f) - count / 2))
    )
    .ToArray();

using var instanceData = GpuBuffer.Create<Matrix4x4>(device, SDL.GPUBufferUsageFlags.Vertex, instances);

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


    var commandBuffer = SDL.AcquireGPUCommandBuffer(device.Handle);
    cubeMesh.PrepareFrame(device, commandBuffer);

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

    SDL.BindGPUGraphicsPipeline(renderPass, graphicsPipeline);

    var matrix = Matrix4x4.CreateLookAt(new(MathF.Sin(frame / 130f) * 3 + .5f, 1, MathF.Cos(frame / 130f) * 3 + .5f), new(0, 0, 0), Vector3.UnitY)
        * Matrix4x4.CreatePerspectiveFieldOfView(90 * (MathF.PI / 180), (float) w / h, .01f, 100f);

    SDL.SetGPUStencilReference(renderPass, 0);


    cubeMesh.GetBindings(out var vertb, out var indb);
    SDL.BindGPUVertexBuffers(renderPass, 0, SpanToPointer([vertb, new SDL.GPUBufferBinding() { Buffer = instanceData.Handle }]), 2);
    SDL.BindGPUIndexBuffer(renderPass, indb, SDL.GPUIndexElementSize.IndexElementSize16Bit);
    SDL.PushGPUVertexUniformData(commandBuffer, 0, StructureToPointer(in matrix), sizeof(float) * 4 * 4);
    SDL.PushGPUFragmentUniformData(commandBuffer, 0, StructureToPointer(in sunDir), sizeof(float) * 3);
    SDL.DrawGPUIndexedPrimitives(renderPass, (uint) cubeMesh.Indices.Length, (uint) instanceData.Length, 0, 0, 0);


    SDL.EndGPURenderPass(renderPass);
    SDL.SubmitGPUCommandBuffer(commandBuffer);
}

SDL.DestroyGPUDevice(device.Handle);
SDL.DestroyWindow(window);

SDL.Quit();


static unsafe nint StructureToPointer<T>(in T structure) where T : unmanaged => (nint) Unsafe.AsPointer(in structure);
static unsafe nint SpanToPointer<T>(ReadOnlySpan<T> span) where T : unmanaged => (nint) Unsafe.AsPointer(ref MemoryMarshal.GetReference(span));

readonly struct Vertex
{
    public readonly float X, Y, Z;
    public readonly float NX, NY, NZ;
    public readonly float R, G, B, A;

    public Vertex(float x, float y, float z, float nx, float ny, float nz, float r, float g, float b, float a)
    {
        X = x;
        Y = y;
        Z = z;
        NX = nx;
        NY = ny;
        NZ = nz;
        R = r;
        G = g;
        B = b;
        A = a;
    }
}


sealed class Mesh
{
    GpuBuffer<Vertex> GpuVertices;
    GpuBuffer<Int16> GpuIndices;
    bool NeedsUpload = false;

    public Vertex[] VerticesArr { set => Vertices = value; }
    public Int16[] IndicesArr { set => Indices = value; }
    public Memory<Vertex> Vertices { get => field; set { NeedsUpload = true; field = value; } }
    public Memory<Int16> Indices { get => field; set { NeedsUpload = true; field = value; } }

    internal void PrepareFrame(GpuDevice device, nint commandBuffer)
    {
        if (GpuVertices.Handle == nint.Zero || GpuIndices.Handle == nint.Zero)
        {
            if (!NeedsUpload)
            {
                GpuVertices = new GpuBuffer<Vertex>(device, 0, SDL.GPUBufferUsageFlags.Vertex);
                GpuIndices = new GpuBuffer<Int16>(device, 0, SDL.GPUBufferUsageFlags.Index);
                return;
            }

            GpuVertices = new GpuBuffer<Vertex>(device, Vertices.Length, SDL.GPUBufferUsageFlags.Vertex);
            GpuIndices = new GpuBuffer<Int16>(device, Indices.Length, SDL.GPUBufferUsageFlags.Index);
        }

        if (NeedsUpload)
        {
            NeedsUpload = false;
            if (GpuVertices.Length < Vertices.Length)
            {
                GpuVertices.Dispose();
                GpuVertices = GpuVertices = new GpuBuffer<Vertex>(device, Vertices.Length, SDL.GPUBufferUsageFlags.Vertex);
            }
            if (GpuIndices.Length < Indices.Length)
            {
                GpuIndices.Dispose();
                GpuIndices = GpuIndices = new GpuBuffer<Int16>(device, Indices.Length, SDL.GPUBufferUsageFlags.Index);
            }

            // temporary bad creation of transfer buffers
            using (var tbv = new GpuTransferBuffer<Vertex>(GpuVertices))
                tbv.WriteAndCopy(commandBuffer, Vertices.Span);
            using (var tbi = new GpuTransferBuffer<Int16>(GpuIndices))
                tbi.WriteAndCopy(commandBuffer, Indices.Span);
        }
    }
    internal void GetBindings([MaybeNullWhen(false)] out SDL.GPUBufferBinding vertices, [MaybeNullWhen(false)] out SDL.GPUBufferBinding indices)
    {
        vertices = new SDL.GPUBufferBinding() { Buffer = GpuVertices.Handle };
        indices = new SDL.GPUBufferBinding() { Buffer = GpuIndices.Handle };
    }
}
